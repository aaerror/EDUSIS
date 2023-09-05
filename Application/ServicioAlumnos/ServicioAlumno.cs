using Core.Shared;
using Domain.Alumnos;
using Domain.Personas.Domicilios;
using Domain.Personas;
using Infrastructure.Shared;
using Core.ServicioAlumnos.DTO.Request;
using Core.ServicioAlumnos.DTO.Response;

namespace Core.ServicioAlumnos;

public class ServicioAlumno : IServicio, IServicioAlumno
{
    private readonly IUnitOfWork _unitOfWork;


    public ServicioAlumno(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private Alumno BuscarAlumnoPorId(Guid persona_id)
    {
        var alumno = _unitOfWork.Alumnos.BuscarPorID(persona_id);

        if (alumno == null)
        {
            throw new NullReferenceException($"No se encontró el alumno con el siguiente Id: { persona_id }");
        }

        return alumno;
    }

    public PersonaResponse BuscarPorDNI(string documento)
    {
        var alumno = _unitOfWork.Alumnos.Buscar(x => x.InformacionPersonal.Documento == documento).FirstOrDefault();
        if (alumno is null)
        {
            throw new NullReferenceException($"No se encontró el alumno con el DNI: { documento }");
        }

        return new PersonaResponse
        {
            PersonaId = alumno.Id,
            NombreCompleto = alumno.InformacionPersonal.NombreCompleto(),
            Documento = alumno.InformacionPersonal.Documento,
            Sexo = alumno.InformacionPersonal.Sexo.ToString(),
            FechaNacimiento = alumno.InformacionPersonal.FechaNacimiento.Date,
            Nacionalidad = alumno.InformacionPersonal.Nacionalidad
        };
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

    public void RegistrarAlumno(InformacionPersonalRequest informacionPersonalRequest, DomicilioRequest domicilioRequest, ContactoRequest contactoRequest)
    {
        var informacionPersonal = InformacionPersonal.Crear(informacionPersonalRequest.Apellido, informacionPersonalRequest.Nombre, informacionPersonalRequest.Documento, informacionPersonalRequest.Sexo, informacionPersonalRequest.FechaNacimiento.Date, informacionPersonalRequest.Nacionalidad);
        var domicilio = Domicilio.Crear(domicilioRequest.Calle, domicilioRequest.Altura, domicilioRequest.Vivienda, domicilioRequest.Observacion, domicilioRequest.Localidad, domicilioRequest.Provincia, domicilioRequest.Pais);

        var alumno = new Alumno(Guid.NewGuid().ToString().GetHashCode().ToString("x"), informacionPersonal, domicilio, contactoRequest.Email, contactoRequest.Telefono);

        _unitOfWork.Alumnos.Agregar(alumno);
        _unitOfWork.GuardarCambios();
    }

    public void ModificarNombreCompleto(Guid alumnoId, string nuevoApellido, string nuevoNombre)
    {
        var alumno = BuscarAlumnoPorId(alumnoId);

        alumno.CambiarNombreCompleto(nuevoApellido, nuevoNombre);

        _unitOfWork.Alumnos.ActualizarDatos(alumno);
        _unitOfWork.GuardarCambios();
    }

    public void ModificarSexo(Guid alumnoId, CambiarSexoRequest cambiarSexoRequest)
    {
        var alumno = BuscarAlumnoPorId(alumnoId);

        alumno.CambiarSexo(cambiarSexoRequest.Apellido, cambiarSexoRequest.Nombre, cambiarSexoRequest.Sexo);

        _unitOfWork.Alumnos.ActualizarDatos(alumno);
        _unitOfWork.GuardarCambios();
    }

    public void ActualizarDomicilio(Guid alumnoId, DomicilioRequest domicilioRequest)
    {
        throw new NotImplementedException();
    }

    public void ActualizarDireccion(Guid personaId, DireccionRequest request)
    {
        var alumno = BuscarAlumnoPorId(personaId);

        var nuevaDireccion = Direccion.Crear(request.Calle, request.Altura, request.Vivienda, request.Observacion);

        alumno.CambiarDireccion(nuevaDireccion);

        _unitOfWork.Alumnos.ActualizarDatos(alumno);
        _unitOfWork.GuardarCambios();
    }
}
