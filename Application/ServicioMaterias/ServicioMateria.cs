using Core.ServicioMaterias.DTOs.Requests;
using Core.ServicioMaterias.DTOs.Responses;
using Infrastructure.Shared;
using Microsoft.Extensions.Logging;
using Domain.Materias.Horarios;
using Domain.Materias.CargosDocentes;
using Domain.Materias;

namespace Core.ServicioMaterias;

public class ServicioMateria : IServicioMateria
{
    private readonly IUnitOfWork _unitOfWork;
    private ILogger<ServicioMateria> _logger;


    public ServicioMateria(IUnitOfWork unitOfWork, ILogger<ServicioMateria> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    private bool EsNombreMateriaDuplicado(NombreDuplicadoRequest request)
    {
        try
        {
            var esDuplicado = _unitOfWork.Materias.NombreDuplicadoEnCurso(request.CursoID, request.MateriaID, request.Descripcion);

            return esDuplicado;
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
            throw;
        }
    }

    /*public CurriculaResponse BuscarCurriculaVigenteSegunCurso(CurriculaSegunCursoRequest request)
    {
        var curricula = _unitOfWork.Materias.BuscarCurriculaVigenteSegunCurso(request.CursoID);
        if (curricula is null)
        {
            throw new ArgumentNullException("No se encontró el diseño curricular vigente del curso.");
        }

        return new CurriculaResponse(curricula.Id, curricula.CursoID, curricula.FechaInicio, curricula.FechaFin);
    }*/

    #region Materia: Listar, Registrar, Modificar, Eliminar
    private Materia BuscarMateria(Guid cursoID, Guid materiaID)
    {
        var materia = _unitOfWork.Materias.BuscarPorID(cursoID, materiaID);
        if (materia is null)
        {
            throw new ArgumentNullException("No se encontró la materia en el diseño curricular del curso.");
        }

        return materia;
    }

    public IReadOnlyCollection<MateriaResponse> ListarMateriasSegunCurso(ListarMateriasSegunCursoRequest request)
    {
        try
        {
            List<MateriaResponse> listar = new();
            var materias = _unitOfWork.Materias.MateriasSegunCurso(request.CursoID);

            _logger.LogInformation($"Se encontraron { materias.Count() } materias en el diseño curricular.");

            // TODO: Revisar mejor implementación del código
            foreach (var materia in materias)
            {
                if (materia.Docente is null)
                {
                    listar.Add(new MateriaResponse(CursoID: materia.CursoID,
                                                   MateriaID: materia.Id,
                                                   Descripcion: materia.Descripcion,
                                                   HorasCatedra: materia.HorasCatedra,
                                                   CargosOcupados: materia.CargosOcupados(),
                                                   SituacionRevistaResponse: null));
                }
                else
                {
                    listar.Add(new MateriaResponse(CursoID: materia.CursoID,
                                                   MateriaID: materia.Id,
                                                   Descripcion: materia.Descripcion,
                                                   HorasCatedra: materia.HorasCatedra,
                                                   CargosOcupados: materia.CargosOcupados(),
                                                   SituacionRevistaResponse: new SituacionRevistaResponse(materia.Docente.DocenteID,
                                                                                                          _unitOfWork.Docentes.BuscarPorID(materia.Docente.DocenteID).InformacionPersonal
                                                                                                                              .NombreCompleto(),
                                                                                                          materia.Docente.Cargo,
                                                                                                          materia.Docente.FechaAlta,
                                                                                                          materia.Docente.FechaBaja,
                                                                                                          materia.Docente.EnFunciones)));
                }
            }
            return listar;
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
            throw;
        }
    }

    public async Task RegistrarMateria(RegistrarMateriaRequest request)
    {
        try
        {
            var esNombreDuplicado = EsNombreMateriaDuplicado(new NombreDuplicadoRequest(CursoID: request.CursoID,
                                                                                        MateriaID: null,
                                                                                        Descripcion: request.Descripcion));
            if (esNombreDuplicado)
            {
                // TODO: Enviar excepción personalizada para notificar a la vista del error en el campo
                throw new ArgumentException("Otra materia con el mismo nombre ya se encuentra registrada en el curso.");
            }

            var materia = new Materia(request.CursoID, request.Descripcion, request.HorasCatedra);

            await _unitOfWork.Materias.AgregarAsync(materia);
            var result = await _unitOfWork.GuardarCambiosAsync();

            _logger.LogInformation($"{result} materia registrada correctamente en el diseño curricular.", result);
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
            throw;
        }
    }

    public void ModificarMateria(ModificarMateriaRequest request)
    {
        try
        {
            var esNombreDuplicado = EsNombreMateriaDuplicado(new NombreDuplicadoRequest(CursoID: request.CursoID,
                                                                                        MateriaID: request.MateriaID,
                                                                                        Descripcion: request.Descripcion));
            if (esNombreDuplicado)
            {
                throw new ArgumentException("Otra materia con el mismo nombre ya se encuentra registrada en el curso", nameof(request.Descripcion));
            }

            var materia = BuscarMateria(request.CursoID, request.MateriaID);
            materia.ModificarMateria(request.Descripcion, request.HorasCatedra);

            _unitOfWork.Materias.Modificar(materia);
            var result = _unitOfWork.GuardarCambiosAsync();

            _logger.LogInformation($"{result} materia modificada correctamente en el diseño curricular.", result);
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
            throw;
        }
    }

    public void EliminarMateria(EliminarMateriaRequest request)
    {
        try
        {
            _unitOfWork.Materias.Eliminar(request.CursoID, request.MateriaID);
            var result = _unitOfWork.GuardarCambiosAsync();

            _logger.LogInformation($"{result} materia eliminada del diseño curricular", result);
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
            throw;
        }
    }
    #endregion

    #region Situacion de Revista, Listar, Registrar, Modificar, Eliminar
    public IReadOnlyCollection<SituacionRevistaResponse> ListarCargosDocenteSegunMateria(ListarCargosDocenteSegunMateriaRequest request)
    {
        try
        {
            var materia = BuscarMateria(request.CursoID, request.MateriaID);

            _logger.LogInformation($"Se encontraron {materia.Docentes.Count()} registros sobre la situacion de revista en la materia.");

            return materia.Docentes.Select(x => 
                new SituacionRevistaResponse(
                    x.DocenteID,
                    _unitOfWork.Docentes.BuscarPorID(x.DocenteID).InformacionPersonal.NombreCompleto(),
                    x.Cargo,
                    x.FechaAlta,
                    x.FechaBaja,
                    x.EnFunciones)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
            throw;
        }
    }

    public IReadOnlyCollection<SituacionRevistaResponse> ListarCargosDocenteActivosSegunMateria(ListarCargosDocenteActivoSegunMateriaRequest request)
    {
        try
        {
            var materia = BuscarMateria(request.CursoID, request.MateriaID);

            _logger.LogInformation($"Se encontraron { materia.Docentes.Count() } registros sobre la situacion de revista en la materia.");

            return materia.Docentes.Where(x => !x.FechaBaja.HasValue)
                .Select(x =>
                    new SituacionRevistaResponse(
                        x.DocenteID,
                        _unitOfWork.Docentes.BuscarPorID(x.DocenteID).InformacionPersonal.NombreCompleto(),
                        x.Cargo,
                        x.FechaAlta,
                        x.FechaBaja,
                        x.EnFunciones)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
            throw;
        }
    }

    public SituacionRevistaResponse RegistrarDocenteEnMateria(RegistrarDocenteEnMateriaRequest request)
    {
        try
        {
            var materia = BuscarMateria(request.CursoID, request.MateriaID);
            materia.RegistrarCargoDocente(request.DocenteID, (Cargo) request.Cargo, request.FechaAlta, request.EnFunciones);

            _unitOfWork.Materias.Modificar(materia);
            _unitOfWork.GuardarCambiosAsync();

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
            throw;
        }
    }

    public void QuitarDocenteDeMateria(EliminarSituacionRevistaRequest request)
    {
        try
        {
            var materia = BuscarMateria(request.CursoID, request.MateriaID);
            materia.DarBajaCargoDocente(request.DocenteID);
            
            _unitOfWork.Materias.Modificar(materia);
            var result = _unitOfWork.GuardarCambiosAsync();

            _logger.LogInformation($"{result} docente se ha quitado del cargo de la materia", result);
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
            throw;
        }
    }

    public void EstablecerDocenteDeAula(EstablecerDocenteDeAulaRequest request)
    {
        try
        {
            var materia = BuscarMateria(request.CursoID, request.MateriaID);
            materia.AsignarDocenteDeAula(request.DocenteID);


            _unitOfWork.Materias.Modificar(materia);
            var result = _unitOfWork.GuardarCambiosAsync();

            _logger.LogInformation($"{result} docente se ha establecido como docente de aula", result);
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
            throw;
        }
    }
    #endregion

    #region Horarios: Listar, Registrar, Modificar, Eliminar
    public IReadOnlyCollection<HorarioResponse> ListarHorariosDeMateria(ListarHorariosRequest request)
    {
        try
        {
            var materia = BuscarMateria(request.CursoID, request.MateriaID);
            var horarios = materia.Horarios;

            _logger.LogInformation($"Se encontraron {horarios.Count()} horarios asignados en la materia.");

            return horarios.Select(x => new HorarioResponse(x.Turno.ToString(), x.DiaSemana.ToString(), x.HoraInicio, x.HoraFin))
                           .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
            throw;
        }
    }

    public void RegistrarHorarioEnMateria(RegistrarHorarioEnMateriaRequest request)
    {
        try
        {
            var materia = BuscarMateria(request.CursoID, request.MateriaID);
            materia.AgregarHorario((Turno) request.Turno, (Dia) request.Dia, TimeOnly.Parse(request.HoraInicio), request.Duracion);

            _unitOfWork.Materias.Modificar(materia);
            var result = _unitOfWork.GuardarCambiosAsync();

            _logger.LogInformation($"Se registro {result} horario nuevo en la materia.");
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
            throw;
        }
    }
    #endregion
}