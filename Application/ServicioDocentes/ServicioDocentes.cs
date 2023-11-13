using Core.ServicioDocentes.DTOs.Requests;
using Domain.Docentes;
using Domain.Personas.Domicilios;
using Domain.Personas;
using Infrastructure.Shared;
using Core.ServicioDocentes.DTOs.Responses;
using Core.Shared.DTOs.Personas;
using Core.Shared.DTOs.Personas.Requests;
using Domain.Docentes.Licencias;

namespace Core.ServicioDocentes;

public class ServicioDocentes : IServicioDocentes
{
    private readonly IUnitOfWork _unitOfWork;


    public ServicioDocentes(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private Docente BuscarDocentePorID(Guid docenteID)
    {
        Docente docente = _unitOfWork.Docentes.BuscarPorID(docenteID);
        if (docente == null)
        {
            throw new NullReferenceException($"No se encontró el docente.");
        }

        return docente;
    }

    public DocenteConDetalleResponse BuscarDocenteConDetalle(Guid docenteID)
    {
        try
        {
            Docente docente = BuscarDocentePorID(docenteID);

            var informacionPersonal = new InformacionPersonalDTO(docente.InformacionPersonal.Apellido,
                                                                 docente.InformacionPersonal.Nombre,
                                                                 docente.InformacionPersonal.Documento,
                                                                 (int) docente.InformacionPersonal.Sexo,
                                                                 docente.InformacionPersonal.FechaNacimiento,
                                                                 docente.InformacionPersonal.Nacionalidad);

            var domicilio = new DomicilioDTO(docente.Domicilio.Direccion.Calle,
                                             docente.Domicilio.Direccion.Altura,
                                             (int) docente.Domicilio.Direccion.Vivienda,
                                             docente.Domicilio.Direccion.Observacion,
                                             docente.Domicilio.Ubicacion.Localidad,
                                             docente.Domicilio.Ubicacion.Provincia,
                                             docente.Domicilio.Ubicacion.Pais);

            var contacto = new ContactoDTO(docente.Telefono, docente.Email);

            return new DocenteConDetalleResponse(docente.Id,
                                                 informacionPersonal,
                                                 domicilio,
                                                 contacto,
                                                 docente.Puestos.Select(x => new PuestoResponse((int) x.Posicion, x.Posicion.ToString(), x.FechaInicio, x.FechaFin))
                                                                .ToList()
                                                                .AsReadOnly());
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public DocenteInfoResponse BuscarDocentePorDNI(string documento)
    {
        Docente docente = _unitOfWork.Docentes.Buscar(x => x.InformacionPersonal.Documento == documento)
                                              .FirstOrDefault();
        if (docente is null)
        {
            throw new NullReferenceException($"No se encontró ningún docente con el D.N.I. { documento }");
        }

        return new DocenteInfoResponse(docente.Id,
                                       docente.InformacionPersonal.NombreCompleto(),
                                       new DocenteIntsitucionalResponse(docente.Id,
                                                                        docente.InformacionPersonal.Documento,
                                                                        docente.FechaAlta,
                                                                        docente.FechaBaja,
                                                                        docente.Legajo,
                                                                        docente.CUIL,
                                                                        docente.EstaActivo));
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
                                                         docente.Puestos.Select(x => new PuestoResponse((int) x.Posicion, x.Posicion.ToString(), x.FechaInicio, x.FechaFin)).ToList());
            return response;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public bool EsCuilInvalido(string cuil)
    {
        bool esInvalido = true;
        if (!string.IsNullOrWhiteSpace(cuil))
        {
            esInvalido = _unitOfWork.Docentes.EsCuilInvalido(cuil);
        }

        return esInvalido;
    }

    public bool EsDocumentoInvalido(string documento)
    {
        bool esInvalido = true;
        if (!string.IsNullOrWhiteSpace(documento))
        {
            esInvalido = _unitOfWork.Docentes.EsDocumentoInvalido(documento);
        }

        return esInvalido;
    }

    public bool EsLegajoInvalido(string legajo)
    {
        bool esInvalido = true;
        if (!string.IsNullOrWhiteSpace(legajo))
        {
            esInvalido = _unitOfWork.Docentes.EsLegajoInvalido(legajo);
        }

        return esInvalido;
    }

    public void RegistrarDocente(RegistrarDocenteRequest request)
    {
        try
        {
            if (request is null)
            {
                throw new NullReferenceException("Datos incompletos para registrar el docente.");
            }

            var informacionPersonal = InformacionPersonal.Crear(request.InformacionPersonalDTO.Apellido,
                                                                request.InformacionPersonalDTO.Nombre,
                                                                request.InformacionPersonalDTO.Documento,
                                                                request.InformacionPersonalDTO.Sexo,
                                                                request.InformacionPersonalDTO.FechaNacimiento,
                                                                request.InformacionPersonalDTO.Nacionalidad);

            var domicilio = Domicilio.Crear(request.DomicilioDTO.Calle,
                                            request.DomicilioDTO.Altura,
                                            request.DomicilioDTO.Vivienda,
                                            request.DomicilioDTO.Observacion,
                                            request.DomicilioDTO.Localidad,
                                            request.DomicilioDTO.Provincia,
                                            request.DomicilioDTO.Pais);

            var nuevoDocente = new Docente(request.Legajo,
                                           request.CUIL,
                                           informacionPersonal,
                                           domicilio,
                                           request.ContactoDTO.Email,
                                           request.ContactoDTO.Telefono);

            nuevoDocente.AgregarPuesto((Posicion) request.Puesto.Posicion, request.Puesto.FechaInicio);

            _unitOfWork.Docentes.Agregar(nuevoDocente);
            _unitOfWork.GuardarCambios();
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public void ModificarContacto(CambiarContactoRequest request)
    {
        try
        {
            Docente docente = BuscarDocentePorID(request.PersonaID);
            docente.CambiarContacto(request.Email, request.Telefono);

            _unitOfWork.Docentes.ActualizarDatos(docente);
            _unitOfWork.GuardarCambios();
        }
        catch (Exception ex)
        {
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

            _unitOfWork.Docentes.ActualizarDatos(docente);
            _unitOfWork.GuardarCambios();
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public void ModificarSexo(CambiarSexoRequest request)
    {
        try
        {
            Docente docente = BuscarDocentePorID(request.PersonaID);
            docente.CambiarSexo(request.Apellido, request.Nombre, request.Sexo);

            _unitOfWork.Docentes.ActualizarDatos(docente);
            _unitOfWork.GuardarCambios();
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public void QuitarDocente(Guid docenteID)
    {
        try
        {
            _unitOfWork.Docentes.BorrarDatos(docenteID);
            _unitOfWork.GuardarCambios();
        }
        catch (Exception ex)
        {
            throw;
        }
    }

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
            throw;
        }
    }

    public LicenciaResponse RegistrarLicencia(RegistrarLicenciaDocenteRequest request)
    {
        try
        {
            Docente docente = BuscarDocentePorID(request.DocenteID);

            var licenciaRegistrada = docente.RegistrarLicencia(request.Articulo, request.Dias, request.FechaInicio, request.Observacion);

            _unitOfWork.Docentes.ActualizarDatos(docente);
            _unitOfWork.GuardarCambios();

            return new LicenciaResponse((int) licenciaRegistrada.Articulo,
                                        (int) licenciaRegistrada.Estado,
                                        licenciaRegistrada.Dias,
                                        licenciaRegistrada.FechaInicio,
                                        licenciaRegistrada.FechaFin,
                                        licenciaRegistrada.Observacion);
        }
        catch (Exception ex)
        {
            throw;
        }
        
    }

    public void AprobarLicencia(EditarEstadoLicenciaRequest request)
    {
        try
        {
            Docente docente = BuscarDocentePorID(request.DocenteID);

            docente.AprobarLicencia(request.Articulo, request.Dias, request.FechaInicio);

            _unitOfWork.Docentes.ActualizarDatos(docente);
            _unitOfWork.GuardarCambios();
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

            _unitOfWork.Docentes.ActualizarDatos(docente);
            _unitOfWork.GuardarCambios();
        }
        catch (Exception ex)
        {
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

            _unitOfWork.Docentes.ActualizarDatos(docente);
            _unitOfWork.GuardarCambios();

            return new LicenciaResponse((int) unaLicencia.Articulo,
                                        (int) unaLicencia.Estado,
                                        unaLicencia.Dias,
                                        unaLicencia.FechaInicio,
                                        unaLicencia.FechaFin,
                                        unaLicencia.Observacion);
        }
        catch (Exception ex)
        {
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
            throw ex;
        }
    }

    public PuestoResponse AsignarPuestoDocente(CrearPuestoDocenteRequest request)
    {
        try
        {
            Docente docente = BuscarDocentePorID(request.DocenteID);
            var puestoAgregado = docente.AgregarPuesto((Posicion) request.Posicion, request.FechaInicio);

            _unitOfWork.Docentes.ActualizarDatos(docente);
            _unitOfWork.GuardarCambios();

            return new PuestoResponse((int) puestoAgregado.Posicion,
                                             puestoAgregado.Posicion.ToString(),
                                             puestoAgregado.FechaInicio,
                                             puestoAgregado.FechaFin);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public PuestoResponse CambiarPuestoDocente(CrearPuestoDocenteRequest request)
    {
        try
        {
            Docente docente = BuscarDocentePorID(request.DocenteID);
            var puestoModificado = docente.CambiarPuesto((Posicion) request.Posicion, request.FechaInicio);

            _unitOfWork.Docentes.ActualizarDatos(docente);
            _unitOfWork.GuardarCambios();

            return new PuestoResponse((int)puestoModificado.Posicion,
                                             puestoModificado.Posicion.ToString(),
                                             puestoModificado.FechaInicio,
                                             puestoModificado.FechaFin);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public PuestoResponse QuitarPuestoDocente(EliminarPuestoDocenteRequest request)
    {
        try
        {
            Docente docente = BuscarDocentePorID(request.DocenteID);

            var puestoEliminado = docente.QuitarPuesto(request.Puesto, request.FechaInicio);

            _unitOfWork.Docentes.ActualizarDatos(docente);
            _unitOfWork.GuardarCambios();

            return new PuestoResponse((int)puestoEliminado.Posicion,
                                             puestoEliminado.Posicion.ToString(),
                                             puestoEliminado.FechaInicio,
                                             puestoEliminado.FechaFin);
        }
        catch (Exception ex)
        {
            throw;
        }
    }
    #endregion
}
