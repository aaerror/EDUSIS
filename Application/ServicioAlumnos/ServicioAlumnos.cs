using Core.Shared;
using Domain.Alumnos;
using Domain.Personas.Domicilios;
using Domain.Personas;
using Infrastructure.Shared;
using Core.ServicioAlumnos.DTOs.Requests;
using Core.ServicioAlumnos.DTOs.Responses;

namespace Core.ServicioAlumnos;

public class ServicioAlumnos : IServicio, IServicioAlumnos
{
    private readonly IUnitOfWork _unitOfWork;


    public ServicioAlumnos(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private Alumno BuscarAlumnoPorId(Guid persona_id)
    {
        Alumno alumno = _unitOfWork.Alumnos.BuscarPorID(persona_id);
        if (alumno == null)
        {
            throw new NullReferenceException($"No se encontró el alumno con el siguiente Id: { persona_id }");
        }

        return alumno;
    }

    public PersonaResponse BuscarPorDNI(string documento)
    {
        Alumno alumno = _unitOfWork.Alumnos.Buscar(x => x.InformacionPersonal.Documento == documento).FirstOrDefault();
        if (alumno is null)
        {
            throw new NullReferenceException($"No se encontró el alumno con el D.N.I. { documento }");
        }

        return new PersonaResponse(alumno.Id,
                                   alumno.InformacionPersonal.Apellido,
                                   alumno.InformacionPersonal.Nombre,
                                   alumno.InformacionPersonal.Documento,
                                   (int) alumno.InformacionPersonal.Sexo,
                                   alumno.InformacionPersonal.FechaNacimiento.ToString("D"),
                                   alumno.InformacionPersonal.Nacionalidad);
    }

    public PersonaConDetallesResponse BuscarPorDNIConDetalles(string documento)
    {
        Alumno alumnoConDetalles = _unitOfWork.Alumnos.Buscar(x => x.InformacionPersonal.Documento == documento).FirstOrDefault();
        if (alumnoConDetalles is null)
        {
            throw new NullReferenceException($"No se encontró el alumno con el D.N.I. { documento }");
        }

        return new PersonaConDetallesResponse
        {
            PersonaId = alumnoConDetalles.Id,
            Apellido = alumnoConDetalles.InformacionPersonal.Apellido,
            Nombre = alumnoConDetalles.InformacionPersonal.Nombre,
            Documento = alumnoConDetalles.InformacionPersonal.Documento,
            Sexo = (int) alumnoConDetalles.InformacionPersonal.Sexo,
            FechaNacimiento = alumnoConDetalles.InformacionPersonal.FechaNacimiento.ToString("D"),
            Nacionalidad = alumnoConDetalles.InformacionPersonal.Nacionalidad,
            Telefono = alumnoConDetalles.Telefono,
            Email = alumnoConDetalles.Email,
            Calle = alumnoConDetalles.Domicilio.Direccion.Calle,
            Altura = alumnoConDetalles.Domicilio.Direccion.Altura,
            Vivienda = (int)alumnoConDetalles.Domicilio.Direccion.Vivienda,
            Observacion = alumnoConDetalles.Domicilio.Direccion.Observacion,
            Localidad = alumnoConDetalles.Domicilio.Ubicacion.Localidad,
            Provincia = alumnoConDetalles.Domicilio.Ubicacion.Provincia,
            Pais = alumnoConDetalles.Domicilio.Ubicacion.Pais
        };
    }

    public bool EsDocumentoInvalido(string documento)
    {
        bool esValido = false;
        if (!string.IsNullOrWhiteSpace(documento))
        {
            esValido = _unitOfWork.Alumnos.EsDocumentoInvalido(documento);
        }

        return esValido;
    }

    public Guid RegistrarAlumno(RegistrarAlumnoRequest request)
    {
        try
        {
            if (request is null)
            {
                throw new ArgumentNullException("Datos incompletos para registrar un alumno.");
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
            
            Alumno alumno = new Alumno(Guid.NewGuid().ToString().GetHashCode().ToString("x"),
                                       informacionPersonal,
                                       domicilio,
                                       request.ContactoDTO.Email,
                                       request.ContactoDTO.Telefono);

            _unitOfWork.Alumnos.Agregar(alumno);
            _unitOfWork.GuardarCambios();

            return alumno.Id;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public void ModificarNombreCompleto(Guid alumnoId, string nuevoApellido, string nuevoNombre)
    {
        Alumno alumnoBuscado = BuscarAlumnoPorId(alumnoId);

        try
        {
            alumnoBuscado.CambiarNombreCompleto(nuevoApellido, nuevoNombre);

            _unitOfWork.Alumnos.ActualizarDatos(alumnoBuscado);
            _unitOfWork.GuardarCambios();
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public void ModificarContacto(Guid alumnoId, ContactoRequest cambiarContactoRequest)
    {
        Alumno alumnoBuscado = BuscarAlumnoPorId(alumnoId);

        try
        {
            alumnoBuscado.CambiarContacto(cambiarContactoRequest.Email, cambiarContactoRequest.Telefono);

            _unitOfWork.Alumnos.ActualizarDatos(alumnoBuscado);
            _unitOfWork.GuardarCambios();
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public void ModificarSexo(Guid alumnoId, CambiarSexoRequest cambiarSexoRequest)
    {
        Alumno alumno = BuscarAlumnoPorId(alumnoId);

        try
        {
            alumno.CambiarSexo(cambiarSexoRequest.Apellido, cambiarSexoRequest.Nombre, cambiarSexoRequest.Sexo);

            _unitOfWork.Alumnos.ActualizarDatos(alumno);
            _unitOfWork.GuardarCambios();
        }
        catch(Exception ex)
        {
            throw;
        }
    }

    public void ModificarDomicilio(Guid alumnoId, DomicilioRequest domicilioRequest)
    {
        Alumno alumno = BuscarAlumnoPorId(alumnoId);

        try
        {
            Domicilio nuevoDomicilio = Domicilio.Crear(domicilioRequest.Calle,
                                                       domicilioRequest.Altura,
                                                       domicilioRequest.Vivienda,
                                                       domicilioRequest.Observacion,
                                                       domicilioRequest.Localidad,
                                                       domicilioRequest.Provincia,
                                                       domicilioRequest.Pais);
            alumno.CambiarDomicilio(nuevoDomicilio);

            _unitOfWork.Alumnos.ActualizarDatos(alumno);
            _unitOfWork.GuardarCambios();
        }
        catch(Exception ex)
        {
            throw;
        }
    }

    public void ActualizarDireccion(Guid personaId, DireccionRequest request)
    {
        var alumno = BuscarAlumnoPorId(personaId);

        var nuevaDireccion = Direccion.Crear(request.Calle, request.Altura, request.Vivienda, request.Observacion);

        alumno.CambiarDireccion(nuevaDireccion);

        _unitOfWork.Alumnos.ActualizarDatos(alumno);
        _unitOfWork.GuardarCambios();
    }

    public void QuitarAlumno(Guid personaId)
    {
        try
        {
            _unitOfWork.Alumnos.BorrarDatos(personaId);
            _unitOfWork.GuardarCambios();
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}
