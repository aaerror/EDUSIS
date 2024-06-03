using Core.ServicioMaterias.DTOs.Requests;
using Core.ServicioMaterias.DTOs.Responses;
using Domain.Materias;
using Domain.Materias.Horarios;
using Domain.Materias.SituacionRevistaDocente;
using Infrastructure.Shared;
using Microsoft.Extensions.Logging;

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

    public MateriaResponse BuscarMateria(BuscarMateriaRequest request)
    {
        try
        {
            var materia = _unitOfWork.Materias.BuscarPorID(request.MateriaID);
            if (materia is null)
            {
                throw new NullReferenceException();
            }

            var response = new MateriaResponse(materia.CursoID,
                                               materia.Id,
                                               materia.Descripcion,
                                               materia.HorasCatedra,
                                               materia.ProfesorID,
                                               _unitOfWork.Docentes.BuscarPorID(materia.ProfesorID).InformacionPersonal.NombreCompleto());
                                               /*materia.Profesores.Select(st => new SituacionRevistaResponse(st.ProfesorID,
                                                                                                            _unitOfWork.Docentes.BuscarPorID(st.ProfesorID).InformacionPersonal.NombreCompleto(),
                                                                                                            (int)st.Cargo,
                                                                                                            st.Cargo.ToString(),
                                                                                                            st.FechaAlta,
                                                                                                            st.FechaBaja,
                                                                                                            st.EnFunciones)).ToList(),
                                               materia.Horarios.Select(h => new HorarioResponse(h.Turno.ToString(),
                                                                                                h.DiaSemana.ToString(),
                                                                                                h.HoraInicio.ToString(),
                                                                                                h.HoraFin.ToString())).ToList());*/

            return response;
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    public IReadOnlyCollection<MateriaResponse> BuscarMateriaSegunCurso(BuscarMateriaSegunCursoRequest request)
    {
        try
        {
            var materias = _unitOfWork.Materias.Buscar(x => x.CursoID.Equals(request.CursoID));

            var response = materias.Select(x => new MateriaResponse(x.CursoID,
                                                                    x.Id,
                                                                    x.Descripcion,
                                                                    x.HorasCatedra,
                                                                    x.ProfesorID,
                                                                    _unitOfWork.Docentes.BuscarPorID(x.ProfesorID)?.InformacionPersonal.NombreCompleto())).ToList();
            return response;
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    public bool EsNombreDuplicadoMateria(NombreDuplicadoRequest request)
    {
        try
        {
            return _unitOfWork.Materias.NombreDuplicado(request.CursoID, request.Descripcion);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Excepción: {ex.Message}");
            throw ex;
        }
    }

    #region Registrar, Modificar, Eliminar
    public async Task RegistrarMateria(RegistrarMateriaRequest request)
    {
        try
        {
            var nuevaMateria = new Materia(request.CursoID, request.Descripcion, request.HorasCatedra);

            _unitOfWork.Materias.AgregarAsync(nuevaMateria);
            var result = await _unitOfWork.GuardarCambiosAsync();
            _logger.LogInformation($"{result} materia registrada correctamente", result);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public void ModificarMateria(ModificarMateriaRequest request)
    {
        try
        {
            var materia = _unitOfWork.Materias.BuscarPorID(request.MateriaID);

            materia.ModificarMateria(request.Descripcion, request.HorasCatedra);

            _unitOfWork.Materias.Modificar(materia);
            _unitOfWork.GuardarCambiosAsync();
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public void EliminarMateria(EliminarMateriaRequest request)
    {
        try
        {
            var materia = _unitOfWork.Materias.BuscarPorID(request.MateriaID);

            _unitOfWork.Materias.Eliminar(materia.Id);
            _unitOfWork.GuardarCambiosAsync();
        }
        catch (Exception ex)
        {
            throw;
        }
    }
    #endregion

    #region Situacion de Revista
    public IReadOnlyCollection<SituacionRevistaResponse> HistoricoSituacionRevista(HistoricoSituacionRevistaRequest request)
    {
        try
        {
            var historico = _unitOfWork.Materias.HistoricoSituacionRevista(request.cursoID, request.materiaID);

            return historico.Select(x => new SituacionRevistaResponse(x.ProfesorID,
                                                                      _unitOfWork.Docentes.BuscarPorID(x.ProfesorID).InformacionPersonal.NombreCompleto(),
                                                                      (int) x.Cargo,
                                                                      x.Cargo.ToString(),
                                                                      x.FechaAlta,
                                                                      x.FechaBaja,
                                                                      x.EnFunciones))
                            .ToList();
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public SituacionRevistaResponse RegistrarDocenteEnMateria(RegistrarSituacionRevistaRequest request)
    {
        try
        {
            var materia = _unitOfWork.Materias.BuscarPorID(request.MateriaID);

            materia.RegistrarNuevaSituacionRevista(request.ProfesorID, (Cargo)request.Cargo, request.FechaAlta, request.EnFunciones);

            _unitOfWork.Materias.Modificar(materia);
            _unitOfWork.GuardarCambiosAsync();

            SituacionRevista nuevoCargo = materia.BuscarSituacionRevista(request.ProfesorID);
            var response = new SituacionRevistaResponse(nuevoCargo.ProfesorID,
                                                        _unitOfWork.Docentes.BuscarPorID(nuevoCargo.ProfesorID).InformacionPersonal.NombreCompleto(),
                                                        (int)nuevoCargo.Cargo,
                                                        nuevoCargo.Cargo.ToString(),
                                                        nuevoCargo.FechaAlta,
                                                        nuevoCargo.FechaBaja,
                                                        nuevoCargo.EnFunciones);
            return response;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public void QuitarDocenteDeMateria(EliminarSituacionRevistaRequest request)
    {
        try
        {
            var materia = _unitOfWork.Materias.BuscarPorID(request.MateriaID);
            materia.QuitarDocenteDelCargo(request.DocenteID, request.FechaBaja);

            _unitOfWork.Materias.Modificar(materia);
            _unitOfWork.GuardarCambiosAsync();
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public void EstablecerDocenteEnFunciones(RegistrarDocenteEnFuncionesRequest request)
    {
        try
        {
            var materia = _unitOfWork.Materias.BuscarPorID(request.MateriaID);
            materia.EstablecerDocenteEnFunciones(request.MateriaID);

            _unitOfWork.Materias.Modificar(materia);
            _unitOfWork.GuardarCambiosAsync();
        }
        catch (Exception ex)
        {
            throw;
        }
    }
    #endregion

    #region Horarios
    public void RegistrarHorario(RegistrarHorarioEnMateria request)
    {
        try
        {
            var materia = _unitOfWork.Materias.BuscarPorID(request.MateriaID);
            var nuevoHorario = Horario.Crear((Turno)request.Turno, (Dia)request.DiaSemana, TimeOnly.Parse(request.HoraInicio), request.Duracion);

            materia.AgregarHorario(nuevoHorario);

            _unitOfWork.Materias.Modificar(materia);
            _unitOfWork.GuardarCambiosAsync();
        }
        catch (Exception e)
        {
            throw;
        }

    }
    #endregion
}