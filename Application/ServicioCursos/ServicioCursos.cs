using Core.ServicioCursos.DTOs.Requests;
using Core.ServicioCursos.DTOs.Responses;
using Core.ServicioCursos.Exceptions;
using Domain.Cursos;
using Domain.Cursos.Materias;
using Infrastructure.Shared;

namespace Core.ServicioCursos;

public class ServicioCursos : IServicioCursos
{
    private readonly IUnitOfWork _unityOfWork;


    public ServicioCursos(IUnitOfWork unitOfWork)
    {
        _unityOfWork = unitOfWork;
    }

    private Curso BuscarCurso(Guid unCurso)
    {
        if (Guid.Empty.Equals(unCurso))
        {
            throw new NullReferenceException($"Datos del curso incompletos o inexistentes. Curso: {unCurso}");
        }

        var cursoBuscado = _unityOfWork.Cursos.BuscarPorID(unCurso);
        if (cursoBuscado == null)
        {
            throw new NullReferenceException($"No se encontró el curso buscado: {cursoBuscado}");
        }

        return cursoBuscado;
    }

    public List<CursoResponse> BuscarCursos()
    {
        List<CursoResponse> cursos = new List<CursoResponse>();

        try
        {
            foreach (Curso curso in _unityOfWork.Cursos.CursosConDivisionesMaterias())
            {
                cursos.Add(new CursoResponse
                {
                    CursoId = curso.Id,
                    Descripcion = curso.Descripcion.ToString(),
                    NivelEducativo = curso.NivelEducativo.ToString(),
                    Divisiones = curso.Divisiones.Count(),
                    Materias = curso.Materias.Count(),
                    Alumnos = curso.Divisiones.Select(d => d.TotalAlumnos).FirstOrDefault()
                });
            }
        }
        catch (Exception ex)
        {
            throw;
        }

        return cursos;
    }

    public void RegistrarCurso(CrearCursoRequest request)
    {
        try
        {
            var existeCurso = _unityOfWork.Cursos.Buscar(x => x.Descripcion == request.Descripcion && x.NivelEducativo.Equals((NivelEducativo) request.NivelEducativo))
                                                 .Any();
            if (existeCurso)
            {
                throw new CursoDuplicadoException();
            }

            Curso nuevoCurso = new(request.Descripcion, (NivelEducativo) request.NivelEducativo);

            _unityOfWork.Cursos.Agregar(nuevoCurso);
            _unityOfWork.GuardarCambios();
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    #region Divisiones
    public void AgregarDivisionAlCurso(Guid unCurso)
    {
        try
        {
            Curso curso = BuscarCurso(unCurso);
            curso.AgregarDivision();

            _unityOfWork.Cursos.ActualizarDatos(curso);

            _unityOfWork.GuardarCambios();
        }
        catch (Exception ex)
        {
            throw ex.InnerException;
        }

    }

    public void QuitarDivisiosDelCurso(Guid unCurso, Guid unaDivision)
    {
        try
        {
            Curso curso = BuscarCurso(unCurso);

            curso.QuitarDivision(unaDivision);

            _unityOfWork.Cursos.ActualizarDatos(curso);
            _unityOfWork.GuardarCambios();
        }
        catch (Exception ex)
        {
            throw;
        }
    }
    #endregion

    #region Materia
    public IReadOnlyCollection<MateriaResponse> BuscarMaterias(Guid unCurso)
    {
        List<MateriaResponse> materias = new List<MateriaResponse>();

        try
        {
            Curso curso = BuscarCurso(unCurso);

            return curso.Materias.Select(x =>
                new MateriaResponse(unCurso,
                                    x.Id,
                                    x.Descripcion,
                                    x.HorasCatedra,
                                    x.Profesor,
                                    x.Profesores.Select(st =>
                                        new SituacionRevistaProfesorResponse(st.ProfesorId,
                                                                             _unityOfWork.Profesores.BuscarPorID(st.ProfesorId).InformacionPersonal.NombreCompleto(),
                                                                             st.Cargo.ToString()))
                                                .ToList(),
                                    x.Horarios.Select(h =>
                                        new HorarioResponse(h.Turno.ToString(),
                                                            h.DiaSemana.ToString(),
                                                            h.HoraInicio.ToString(),
                                                            h.HoraFin.ToString()))
                                              .ToList()))
                                 .ToList();
        }
        catch (Exception ex)
        {
            throw ex.InnerException;
        }
    }

    public void RegistrarMateriaEnCurso(Guid unCurso, CrearMateriaRequest request)
    {
        try
        {
            Curso curso = BuscarCurso(unCurso);

            curso.AgregarMateria(request.Descripcion, request.HorasCatedra);

            _unityOfWork.Cursos.ActualizarDatos(curso);
            _unityOfWork.GuardarCambios();
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public void ActualizarMateria(EditarMateriaRequest request)
    {
        try
        {
            Curso curso = BuscarCurso(request.Curso);

            curso.ActualizarMateria(request.Materia, request.Descripcion, request.HorasCatedra);

            _unityOfWork.Cursos.ActualizarDatos(curso);
            _unityOfWork.GuardarCambios();
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public void AsignarHorarioAMateriaDelCurso(CrearHorarioRequest request)
    {
        try
        {
            Curso curso = BuscarCurso(request.Curso);
            var turno = (Turno) request.Turno;
            var dia = (Dia) request.DiaSemana;

            var nuevoHorario = Horario.Crear(turno, dia, TimeOnly.Parse(request.HoraInicio), request.Duracion);

            curso.AgregarHorarioAMateria(request.Materia, nuevoHorario);

            _unityOfWork.Cursos.ActualizarDatos(curso);
            _unityOfWork.GuardarCambios();
        }
        catch (Exception e)
        {
            throw;
        }

    }

    public void QuitarMateriaDelCurso(EliminarMateriaRequest request)
    {
        try
        {
            Curso curso = BuscarCurso(request.Curso);

            curso.QuitarMateria(request.Materia);

            _unityOfWork.Cursos.ActualizarDatos(curso);
            _unityOfWork.GuardarCambios();
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public void InscribirProfesorEnMateria(CrearSituacionRevistaProfesorRequest request)
    {
        try
        {
            Curso curso = BuscarCurso(request.CursoId);

            curso.AgregarProfesorEnMateria(request.MateriaId, request.ProfesorId, (Cargo) request.Cargo, request.FechaAlta);

            _unityOfWork.Cursos.ActualizarDatos(curso);
            _unityOfWork.GuardarCambios();
        }
        catch (Exception ex)
        {
            throw;
        }
    }
    #endregion
}
