using Core.ServicioAlumnos.DTO;
using Core.Shared;
using Domain.Alumno;
using Domain.Personas.Domicilios;
using Domain.Personas;
using Infrastructure;
using Infrastructure.Shared;
using Azure.Core;

namespace Core.ServicioAlumnos;

public class ServicioAlumno : IServicio, IServicioAlumno
{
    private readonly IUnitOfWork _unitOfWork;


    public ServicioAlumno()
    {
        _unitOfWork = new UnitOfWork();
    }

    private Alumno BuscarAlumno(Guid persona_id)
    {
        // TODO: Corroborar null
        var alumno = _unitOfWork.Alumnos.BuscarPorID(persona_id);
        return alumno;
    }

    public void RegistrarAlumno(InformacionPersonalRequest informacionPersonalRequest, DomicilioRequest domicilioRequest, ContactoRequest? contactoRequest)
    {
        var informacionPersonal = InformacionPersonal.Crear(informacionPersonalRequest.Apellido, informacionPersonalRequest.Nombre, informacionPersonalRequest.Documento, informacionPersonalRequest.Sexo, informacionPersonalRequest.FechaNacimiento.Date, informacionPersonalRequest.Nacionalidad);
        var domicilio = Domicilio.Crear(domicilioRequest.Calle, domicilioRequest.Altura, domicilioRequest.Vivienda, domicilioRequest.Observacion, domicilioRequest.Localidad, domicilioRequest.Provincia, domicilioRequest.Pais);
        
        var alumno = new Alumno(Guid.NewGuid().ToString().GetHashCode().ToString("x"), informacionPersonal, domicilio);

        if (contactoRequest != null )
        {
            var contacto = Contacto.Crear(contactoRequest.TipoContacto, contactoRequest.Descripcion);
            alumno.AgregarContacto(contacto);
        }

        _unitOfWork.Alumnos.Agregar(alumno);
        _unitOfWork.GuardarCambios();
    }

    public void AgregarContacto(ContactoRequest request)
    {
        var contacto = Contacto.Crear(request.TipoContacto, request.Descripcion);
    }

    public void CambiarDireccion(Guid personaId, DireccionRequest request)
    {
        var alumno = BuscarAlumno(personaId);
        if (alumno != null)
        {
            alumno.Domicilio.CambiarDireccion(request.Calle, request.Altura, request.Vivienda, request.Observacion);
            _unitOfWork.GuardarCambios();
        }
    }

    public void CambiarDomicilio(Guid personaId, DomicilioRequest request)
    {
        var alumno = BuscarAlumno(personaId);
        var nuevoDomicilio = Domicilio.Crear(request.Calle, request.Altura, request.Vivienda, request.Observacion, request.Localidad, request.Provincia, request.Pais);
        alumno.ActualizarDomicilio(nuevoDomicilio);
        _unitOfWork.GuardarCambios();
    }

    public bool EsDocumentoValido(string documento)
    {
        bool esValido = false;
        if (!string.IsNullOrWhiteSpace(documento))
        {
            esValido = _unitOfWork.Alumnos.EsDocumentoValido(documento);
        }

        return esValido;
    }
}
