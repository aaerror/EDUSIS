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

    private Curso BuscarCurso(Guid unCurso)
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

    #region Registrar, Modificar, Eliminar

    public IReadOnlyCollection<CursoResponse> ListarCursos()
    {
        try
        {
            var cursos = _unitOfWork.Cursos.BuscarTodos();

            return cursos.Select(x =>
                new CursoResponse(CursoID: x.Id,
                                  Grado: (int) x.Grado,
                                  GradoDescripcion: x.Grado.ToString(),
                                  NivelEducativo: (int) x.NivelEducativo,
                                  NivelEducativoDescripcion: x.NivelEducativo.ToString(),
                                  Divisiones: x.Divisiones.Count(),
                                  Alumnos: x.CantidadAlumnos))
                .OrderBy(x => x.Grado)
                .ThenBy(x => x.NivelEducativo)
                .ToList();
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task RegistrarCurso(RegistrarCursoRequest request)
    {
        try
        {
            var existeCurso = _unitOfWork.Cursos
                .Buscar(x => x.Grado.Equals((Grado) request.Grado) && x.NivelEducativo.Equals((NivelEducativo) request.NivelEducativo))
                .Any();

            if (existeCurso)
            {
                throw new CursoDuplicadoException();
            }

            Curso nuevoCurso = new((Grado) request.Grado, (NivelEducativo) request.NivelEducativo);

            await _unitOfWork.Cursos.AgregarAsync(nuevoCurso);
            var result = await _unitOfWork.GuardarCambiosAsync();

            _logger.LogInformation($"Resultado de registrar materia: {result}", result);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public void EliminarCurso(EliminarCursoRequest request)
    {
        try
        {
            _unitOfWork.Cursos.Eliminar(request.CursoID);
            // TODO: Eliminar todas las materias con este curso asociado
            _unitOfWork.GuardarCambiosAsync();
        }
        catch (Exception ex)
        {
            throw;
        }
    }
    #endregion

    #region Calificacion
    public void RegistrarCalificacion(CrearCalificationRequest request)
    {
        try
        {
            Curso curso = BuscarCurso(request.Curso);


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
            var divisiones = _unitOfWork.Cursos.DivisionesDelCurso(unCurso);
            return divisiones.Select(x => new DivisionResponse(x.Id,
                                                               x.Descripcion,
                                                               _unitOfWork.Docentes.BuscarPorID(x.Preceptor ?? Guid.Empty)?.InformacionPersonal.NombreCompleto(),
                                                               x.TotalAlumnos))
                             .OrderBy(x => x.Descripcion)
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
            Curso curso = BuscarCurso(request.CursoID);
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
            Curso curso = BuscarCurso(request.CursoID);

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
            Curso curso = BuscarCurso(unCurso);
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
            Curso curso = _unitOfWork.Cursos.CursoConDivisiones(request.CursoID);

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
