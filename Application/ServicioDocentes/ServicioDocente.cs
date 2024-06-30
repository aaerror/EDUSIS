using Core.ServicioDocentes.DTOs.Requests;
using Domain.Docentes;
using Domain.Personas.Domicilios;
using Domain.Personas;
using Infrastructure.Shared;
using Core.ServicioDocentes.DTOs.Responses;
using Core.Shared.DTOs.Personas.Requests;
using Domain.Docentes.Licencias;
using Microsoft.EntityFrameworkCore;
using Core.Shared.DTOs.Personas.Responses;
using Microsoft.Extensions.Logging;

namespace Core.ServicioDocentes;

public class ServicioDocente : IServicioDocente
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ServicioDocente> _logger;


    public ServicioDocente(IUnitOfWork unitOfWork, ILogger<ServicioDocente> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    private Docente BuscarDocentePorID(Guid docenteID)
    {
        try
        {
            var docente = _unitOfWork.Docentes.BuscarPorID(docenteID);
            if (docente is null)
            {
                throw new NullReferenceException($"No se encontró el docente.");
            }

            return docente;
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
            throw;
        }
    }

    public bool EsCuilInvalido(string cuil)
    {
        try
        {
            bool esInvalido = true;
            if (!string.IsNullOrWhiteSpace(cuil))
            {
                esInvalido = _unitOfWork.Docentes.EsCuilInvalido(cuil);
            }

            return esInvalido;
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
            throw;
        }
    }

    public bool EsDocumentoInvalido(string documento)
    {
        try
        {
            bool esInvalido = true;
            if (!string.IsNullOrWhiteSpace(documento))
            {
                esInvalido = _unitOfWork.Docentes.EsDocumentoInvalido(documento);
            }

            return esInvalido;
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
            throw;
        }
    }

    public bool EsLegajoInvalido(string legajo)
    {
        try
        {
            bool esInvalido = true;
            if (!string.IsNullOrWhiteSpace(legajo))
            {
                esInvalido = _unitOfWork.Docentes.EsLegajoInvalido(legajo);
            }

            return esInvalido;
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
            throw;
        }
    }

    #region Docente: Listar, Registrar, Modificar, Eliminar
    public PerfilPersonalDeDocenteResponse BuscarPerfilPersonalDelDocente(Guid docenteID)
    {
        try
        {
            Docente docente = BuscarDocentePorID(docenteID);

            var informacionPersonal = new InformacionPersonalResponse(
                Apellido: docente.InformacionPersonal.Apellido,
                Nombre: docente.InformacionPersonal.Nombre,
                DNI: docente.InformacionPersonal.Documento,
                Sexo: (int) docente.InformacionPersonal.Sexo,
                FechaNacimiento: docente.InformacionPersonal.FechaNacimiento,
                Nacionalidad: docente.InformacionPersonal.Nacionalidad);

            var domicilio = new DomicilioResponse(
                Calle: docente.Domicilio.Direccion.Calle,
                Altura: docente.Domicilio.Direccion.Altura,
                Vivienda: (int) docente.Domicilio.Direccion.Vivienda,
                Observacion: docente.Domicilio.Direccion.Observacion,
                Localidad: docente.Domicilio.Ubicacion.Localidad,
                Provincia: docente.Domicilio.Ubicacion.Provincia,
                Pais: docente.Domicilio.Ubicacion.Pais);

            var contacto = new ContactoResponse(
                Telefono: docente.Telefono, Email: docente.Email);

            return new PerfilPersonalDeDocenteResponse(
                DocenteID: docente.Id,
                InformacionPersonalDTO: informacionPersonal,
                DomicilioDTO: domicilio,
                ContactoDTO: contacto);
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
            throw;
        }
    }

    public LegajoDocenteResponse BuscarLegajoDocentePorDNI(string documento)
    {
        try
        {
            var docente = _unitOfWork.Docentes.Buscar(x => x.InformacionPersonal.Documento == documento)
                                              .FirstOrDefault();
            if (docente is null)
            {
                throw new NullReferenceException($"No se encontró ningún docente con el D.N.I. { documento }");
            }

            return new LegajoDocenteResponse(
                DocenteID: docente.Id,
                NombreCompleto: docente.InformacionPersonal.NombreCompleto(),
                Legajo: docente.Legajo,
                FechaAlta: docente.FechaAlta,
                FechaBaja: docente.FechaBaja,
                CUIL: docente.CUIL,
                EstaActivo: docente.EstaActivo,
                Puestos: docente.Puestos.Select(x =>
                    new PuestoResponse(
                        Posicion: (int)x.Posicion,
                        PosicionDescripcion: x.Posicion.ToString(),
                        FechaInicio: x.FechaInicio,
                        FechaFin: x.FechaFin))
                .ToList());
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
            throw;
        }
    }

    public IReadOnlyCollection<LegajoDocenteResponse> BuscarLegajoDocentePorApellidoNombre(BuscarDocentePorApellidoNombreRequest request)
    {
        try
        {
            var docentes = _unitOfWork.Docentes.Buscar(x =>
                EF.Functions.Like(x.InformacionPersonal.Nombre.Trim().ToLower(), $"%{ request.NombreCompleto.Trim().ToLower() }%") ||
                EF.Functions.Like(x.InformacionPersonal.Apellido.Trim().ToLower(), $"%{ request.NombreCompleto.Trim().ToLower() }%"));

            return docentes.Select(x =>
                new LegajoDocenteResponse(DocenteID: x.Id,
                                          NombreCompleto: x.InformacionPersonal.NombreCompleto(),
                                          Legajo: x.Legajo,
                                          FechaAlta: x.FechaAlta,
                                          FechaBaja: x.FechaBaja,
                                          CUIL: x.CUIL,
                                          EstaActivo: x.EstaActivo,
                                          Puestos: x.Puestos.Select(p =>
                                            new PuestoResponse(Posicion: (int)p.Posicion,
                                                               PosicionDescripcion: p.Posicion.ToString(),
                                                               FechaInicio: p.FechaInicio.Date,
                                                               FechaFin: p.FechaFin))
                                          .ToList()))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
            throw;
        }
    }

    public DocenteConPuestosResponse BuscarDocenteConPuestos(Guid docenteID)
    {
        try
        {
            Docente docente = BuscarDocentePorID(docenteID);

            var response = new DocenteConPuestosResponse(docenteID,
                                                         docente.Legajo,
                                                         docente.InformacionPersonal.Documento,
                                                         docente.InformacionPersonal.NombreCompleto(),
                                                         docente.Puestos.Select(x => new PuestoResponse((int)x.Posicion, x.Posicion.ToString(), x.FechaInicio, x.FechaFin)).ToList());
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
            throw;
        }
    }

    public void RegistrarDocente(RegistrarDocenteRequest request)
    {
        try
        {
            if (request is null)
            {
                throw new NullReferenceException("Datos incompletos para registrar el docente.");
            }

            var informacionPersonal = InformacionPersonal.Crear(
                apellido: request.Apellido,
                nombre: request.Nombre,
                dni: request.DNI,
                sexo: request.Sexo,
                fechaNacimiento: request.FechaNacimiento,
                nacionalidad: request.Nacionalidad);

            var domicilio = Domicilio.Crear(
                calle: request.Calle,
                altura: request.Altura,
                vivienda: request.Vivienda,
                observacion: request.Observacion,
                localidad: request.Localidad,
                provincia: request.Provincia,
                pais: request.Pais);

            var nuevoDocente = new Docente(
                legajo: request.Legajo,
                cuil: request.CUIL,
                fechaAlta: request.FechaAlta,
                informacionPersonal: informacionPersonal,
                domicilio: domicilio,
                email: request.Email,
                telefono: request.Telefono);

            nuevoDocente.AgregarPuesto((Posicion) request.Puesto.Posicion, request.Puesto.FechaInicio);

            _unitOfWork.Docentes.AgregarAsync(nuevoDocente);
            _unitOfWork.GuardarCambiosAsync();
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
            throw;
        }
    }

    public void ModificarContacto(CambiarContactoRequest request)
    {
        try
        {
            Docente docente = BuscarDocentePorID(request.PersonaID);
            docente.CambiarContacto(request.Email, request.Telefono);

            _unitOfWork.Docentes.Modificar(docente);
            _unitOfWork.GuardarCambiosAsync();
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
            throw;
        }
    }

    public void ModificarDomicilio(CambiarDomicilioRequest request)
    {
        try
        {
            Docente docente = BuscarDocentePorID(request.PersonaID);
            var nuevoDomicilio = Domicilio.Crear(request.Calle, request.Altura, request.Vivienda, request.Observacion, request.Localidad, request.Provincia, request.Pais);
            docente.CambiarDomicilio(nuevoDomicilio);

            _unitOfWork.Docentes.Modificar(docente);
            _unitOfWork.GuardarCambiosAsync();
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
            throw;
        }
    }

    public void ModificarSexo(CambiarSexoRequest request)
    {
        try
        {
            Docente docente = BuscarDocentePorID(request.PersonaID);
            docente.CambiarSexo(request.Apellido, request.Nombre, request.Sexo);

            _unitOfWork.Docentes.Modificar(docente);
            _unitOfWork.GuardarCambiosAsync();
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
            throw;
        }
    }

    public void QuitarDocente(Guid docenteID)
    {
        try
        {
            _unitOfWork.Docentes.Eliminar(docenteID);
            _unitOfWork.GuardarCambiosAsync();
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
            throw;
        }
    }
    #endregion

    #region Licencias
    public IReadOnlyCollection<LicenciaResponse> BuscarLicencias(Guid docenteID)
    {
        try
        {
            Docente docente = BuscarDocentePorID(docenteID);
            return docente.Licencias.Select(x => new LicenciaResponse((int) x.Articulo,
                                                                      (int) x.Estado,
                                                                      x.Dias,
                                                                      x.FechaInicio,
                                                                      x.FechaFin,
                                                                      x.Observacion)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
            throw;
        }
    }

    public LicenciaResponse RegistrarLicencia(RegistrarLicenciaDocenteRequest request)
    {
        try
        {
            Docente docente = BuscarDocentePorID(request.DocenteID);

            var licenciaRegistrada = docente.RegistrarLicencia(request.Articulo, request.Dias, request.FechaInicio, request.Observacion);

            _unitOfWork.Docentes.Modificar(docente);
            _unitOfWork.GuardarCambiosAsync();

            return new LicenciaResponse((int) licenciaRegistrada.Articulo,
                                        (int) licenciaRegistrada.Estado,
                                        licenciaRegistrada.Dias,
                                        licenciaRegistrada.FechaInicio,
                                        licenciaRegistrada.FechaFin,
                                        licenciaRegistrada.Observacion);
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
            throw;
        }
        
    }

    public void AprobarLicencia(EditarEstadoLicenciaRequest request)
    {
        try
        {
            Docente docente = BuscarDocentePorID(request.DocenteID);

            docente.AprobarLicencia(request.Articulo, request.Dias, request.FechaInicio);

            _unitOfWork.Docentes.Modificar(docente);
            _unitOfWork.GuardarCambiosAsync();
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public void CancelarLicencia(EditarEstadoLicenciaRequest request)
    {
        try
        {
            Docente docente = BuscarDocentePorID(request.DocenteID);

            docente.CancelarLicencia(request.Articulo, request.Dias, request.FechaInicio, request.Observacion);

            _unitOfWork.Docentes.Modificar(docente);
            _unitOfWork.GuardarCambiosAsync();
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
            throw;
        }
    }

    public LicenciaResponse ModificarLicencia(EditarLicenciaRequest request)
    {
        try
        {
            Docente docente = BuscarDocentePorID(request.DocenteID);

            Licencia unaLicencia = Licencia.Crear((Articulo) request.Licencia.Articulo, request.Licencia.Dias, request.Licencia.FechaInicio, request.Licencia.Observacion);
            unaLicencia = docente.ActualizarLicencia(unaLicencia, request.Articulo, request.Dias, request.FechaInicio, request.Observacion);

            _unitOfWork.Docentes.Modificar(docente);
            _unitOfWork.GuardarCambiosAsync();

            return new LicenciaResponse((int) unaLicencia.Articulo,
                                        (int) unaLicencia.Estado,
                                        unaLicencia.Dias,
                                        unaLicencia.FechaInicio,
                                        unaLicencia.FechaFin,
                                        unaLicencia.Observacion);
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
            throw;
        }
    }
    #endregion

    #region Puestos
    public IReadOnlyCollection<PuestoResponse> BuscarPuestos(Guid docenteID)
    {
        try
        {
            Docente docente = BuscarDocentePorID(docenteID);
            return docente.Puestos.Select(x => new PuestoResponse((int) x.Posicion,
                                                                  x.Posicion.ToString(),
                                                                  x.FechaInicio,
                                                                  x.FechaFin)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
            throw;
        }
    }

    public PuestoResponse AsignarPuestoDocente(CrearPuestoDocenteRequest request)
    {
        try
        {
            Docente docente = BuscarDocentePorID(request.DocenteID);
            var puestoAgregado = docente.AgregarPuesto((Posicion) request.Posicion, request.FechaInicio);

            _unitOfWork.Docentes.Modificar(docente);
            _unitOfWork.GuardarCambiosAsync();

            return new PuestoResponse((int) puestoAgregado.Posicion,
                                             puestoAgregado.Posicion.ToString(),
                                             puestoAgregado.FechaInicio,
                                             puestoAgregado.FechaFin);
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
            throw;
        }
    }

    public PuestoResponse CambiarPuestoDocente(CrearPuestoDocenteRequest request)
    {
        try
        {
            Docente docente = BuscarDocentePorID(request.DocenteID);
            var puestoModificado = docente.CambiarPuesto((Posicion) request.Posicion, request.FechaInicio);

            _unitOfWork.Docentes.Modificar(docente);
            _unitOfWork.GuardarCambiosAsync();

            return new PuestoResponse((int)puestoModificado.Posicion,
                                             puestoModificado.Posicion.ToString(),
                                             puestoModificado.FechaInicio,
                                             puestoModificado.FechaFin);
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
            throw;
        }
    }

    public PuestoResponse QuitarPuestoDocente(EliminarPuestoDocenteRequest request)
    {
        try
        {
            Docente docente = BuscarDocentePorID(request.DocenteID);

            var puestoEliminado = docente.QuitarPuesto(request.Puesto, request.FechaInicio);

            _unitOfWork.Docentes.Modificar(docente);
            _unitOfWork.GuardarCambiosAsync();

            return new PuestoResponse((int) puestoEliminado.Posicion,
                                            puestoEliminado.Posicion.ToString(),
                                            puestoEliminado.FechaInicio,
                                            puestoEliminado.FechaFin);
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
            throw;
        }
    }
    #endregion
}
