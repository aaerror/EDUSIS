using Core.ServicioCursos.DTOs.Requests;
using Core.ServicioCursos.DTOs.Responses;
using Core.ServicioCursos.Exceptions;
using Core.ServicioCusos.DTOs.Responses;
using Domain.Cursos;
using Infrastructure.Shared;
using Microsoft.Extensions.Logging;

namespace Core.ServicioCursos;

public class ServicioCurso : IServicioCurso
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ServicioCurso> _logger;


    public ServicioCurso(IUnitOfWork unitOfWork, ILogger<ServicioCurso> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    private Curso BuscarCursoPorID(Guid unCurso)
    {
        if (Guid.Empty.Equals(unCurso))
        {
            throw new NullReferenceException($"Datos del curso incompletos o inexistentes. Curso: {unCurso}");
        }

        var cursoBuscado = _unitOfWork.Cursos.BuscarPorID(unCurso);
        if (cursoBuscado is null)
        {
            throw new NullReferenceException($"No se encontró el curso buscado: {cursoBuscado}");
        }

        return cursoBuscado;
    }

    public IReadOnlyCollection<CursoResponse> BuscarCursos()
    {
        try
        {
            var cursos = _unitOfWork.Cursos.CursosConDivisionesMaterias();
            return cursos.Select(x => new CursoResponse
                                {
                                    CursoID = x.Id,
                                    Descripcion = x.Descripcion.ToString(),
                                    NivelEducativo = x.NivelEducativo.ToString(),
                                    Divisiones = x.Divisiones.Count(),
                                    Materias = x.Materias.Count(),
                                    Alumnos = x.Divisiones.Select(d => d.TotalAlumnos).FirstOrDefault()
                                })
                         .OrderBy(x => x.NivelEducativo)
                         .ThenBy(x => x.Descripcion)
                         .ToList();
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    #region Registrar, Modificar, Eliminar
    public async Task RegistrarCurso(RegistrarCursoRequest request)
    {
        try
        {
            var existeCurso = _unitOfWork.Cursos.Buscar(x => x.Descripcion == request.Descripcion && x.NivelEducativo.Equals((NivelEducativo) request.NivelEducativo))
                                                .Any();
            if (existeCurso)
            {
                throw new CursoDuplicadoException();
            }

            Curso nuevoCurso = new(request.Descripcion, (NivelEducativo) request.NivelEducativo);

            _unitOfWork.Cursos.AgregarAsync(nuevoCurso);
            var result = await _unitOfWork.GuardarCambiosAsync();
            _logger.LogInformation($"Resultado de registrar materia: {result}", result);
        }
        catch (Exception ex)
        {
            throw ex.InnerException;
        }
    }
    #endregion

    #region Calificacion
    public void RegistrarCalificacion(CrearCalificationRequest request)
    {
        try
        {
            Curso curso = BuscarCursoPorID(request.Curso);


        }
        catch (Exception ex)
        {
            throw;
        }
    }
    #endregion

    #region Divisiones
    public IReadOnlyCollection<DivisionResponse> BuscarDivisiones(Guid unCurso)
    {
        try
        {
            Curso curso = BuscarCursoPorID(unCurso);

            var div = _unitOfWork.Cursos.BuscarDivisiones(unCurso);
            return div.Select(x => new DivisionResponse(curso.Id,
                                                        curso.Descripcion,
                                                        curso.NivelEducativo.ToString(),
                                                        x.Id,
                                                        x.Descripcion,
                                                        x.TotalAlumnos))
                             .OrderBy(x => x.DivisionDescripcion)
                             .ToList()
                             .AsReadOnly();
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public IReadOnlyCollection<CursanteResponse> BuscarListado(BuscarListadoRequest request)
    {
        try
        {
            Curso curso = BuscarCursoPorID(request.CursoID);
            var alumnos = curso.CursantesPorPeriodo(request.DivisionID, request.Periodo);

            return alumnos.Select(x => new CursanteResponse(x,
                                                            _unitOfWork.Alumnos.BuscarPorID(x).InformacionPersonal.NombreCompleto(),
                                                            _unitOfWork.Alumnos.BuscarPorID(x).InformacionPersonal.Documento,
                                                            _unitOfWork.Alumnos.BuscarPorID(x).InformacionPersonal.Edad().ToString()))
                          .ToList()
                          .AsReadOnly();
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public void InscribirAlumnoEnDivision(CrearCursanteRequest request)
    {
        try
        {
            Curso curso = BuscarCursoPorID(request.CursoID);

            curso.AgregarAlumnoAlListadoDefinitivo(request.DivisionID, request.AlumnoID, request.Periodo);

            _unitOfWork.Cursos.Modificar(curso);
            _unitOfWork.GuardarCambiosAsync();
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public void AgregarDivisionAlCurso(Guid unCurso)
    {
        try
        {
            Curso curso = BuscarCursoPorID(unCurso);
            curso.AgregarDivision();

            _unitOfWork.Cursos.Modificar(curso);

            _unitOfWork.GuardarCambiosAsync();
        }
        catch (Exception ex)
        {
            throw ex.InnerException;
        }

    }

    public void QuitarDivisiosDelCurso(EliminarDivisionRequest request)
    {
        try
        {
            Curso curso = BuscarCursoPorID(request.CursoID);

            curso.QuitarDivision(request.DivisionID);

            _unitOfWork.Cursos.Modificar(curso);
            _unitOfWork.GuardarCambiosAsync();
        }
        catch (Exception ex)
        {
            throw;
        }
    }
    #endregion
}
