using Core.ServicioAlumnos.DTOs.Requests;
using Core.Shared.DTOs.Personas.Requests;
using Core.Shared.DTOs.Personas.Response;
using Core.Shared;
using Domain.Alumnos;
using Domain.Personas.Domicilios;
using Domain.Personas;
using Domain.Shared;
using Microsoft.Extensions.Logging;

namespace Core.ServicioAlumnos;

internal class ServicioAlumno : IServicio, IServicioAlumno
{
	private readonly ILogger<ServicioAlumno> _logger;
	private readonly IUnitOfWork _unitOfWork;


	public ServicioAlumno(ILogger<ServicioAlumno> logger, IUnitOfWork unitOfWork)
	{
		_logger = logger;
		_unitOfWork = unitOfWork;
	}

	private async Task<Alumno> BuscarAlumnoPorIDAsync(Guid personaID)
	{
		var alumno = await _unitOfWork.Alumnos.BuscarPorIDAsync(personaID);
		if (alumno is null)
		{
			throw new NullReferenceException($"No se encontró el alumno con el siguiente Id: { personaID }");
		}

		return alumno;
	}


	public async Task<PersonaConDetallesResponse> BuscarPorIDAsync(PersonaRequest request)
	{
		var response = await BuscarAlumnoPorIDAsync(request.PersonaID);
		if (response is null)
		{
			throw new NullReferenceException($"No se encontró el alumno.");
		}

		return new PersonaConDetallesResponse(
			PersonaID: response.Id,
			Apellido: response.DatosPersonales.Apellido,
			Nombre: response.DatosPersonales.Nombre,
			Documento: response.DatosPersonales.Documento,
			Sexo: response.DatosPersonales.Sexo.ToString(),
			FechaNacimiento: response.DatosPersonales.FechaNacimiento.ToString("D"),
			Nacionalidad: response.DatosPersonales.Nacionalidad,
			Telefono: response.Telefono,
			Email: response.Email,
			Calle: response.Domicilio.Direccion.Calle,
			Altura: response.Domicilio.Direccion.Altura,
			Vivienda: response.Domicilio.Direccion.Vivienda.ToString(),
			Observacion: response.Domicilio.Direccion.Observacion,
			Localidad: response.Domicilio.Ubicacion.Localidad,
			Provincia: response.Domicilio.Ubicacion.Provincia,
			Pais: response.Domicilio.Ubicacion.Pais);
	}

	public async Task<PersonaResponse?> BuscarPorNombreCompletoAsync(NombreCompletoRequest request)
	{
		var alumno = await _unitOfWork.Alumnos.BuscarPorNombreCompletoAsync(request.NombreCompleto);

		if (alumno is not null)
		{
			return new PersonaResponse(
				PersonaID: alumno.Id,
				Apellido: alumno.DatosPersonales.Apellido,
				Nombre: alumno.DatosPersonales.Nombre,
				Documento: alumno.DatosPersonales.Documento,
				Sexo: alumno.DatosPersonales.Sexo.ToString(),
				FechaNacimiento: alumno.DatosPersonales.FechaNacimiento.ToString("D"),
				Nacionalidad: alumno.DatosPersonales.Nacionalidad);
		}

		return null;
	}

	public async Task<PersonaResponse?> BuscarPorDNIAsync(DocumentoRequest request)
	{
		var alumno = await _unitOfWork.Alumnos.BuscarPorDocumentoAsync(request.Documento);

		if (alumno is not null)
		{
			return new PersonaResponse(
				PersonaID: alumno.Id,
				Apellido: alumno.DatosPersonales.Apellido,
				Nombre: alumno.DatosPersonales.Nombre,
				Documento: alumno.DatosPersonales.Documento,
				Sexo: alumno.DatosPersonales.Sexo.ToString(),
				FechaNacimiento: alumno.DatosPersonales.FechaNacimiento.ToString("D"),
				Nacionalidad: alumno.DatosPersonales.Nacionalidad);
		}

		// throw new NullReferenceException($"No se encontró el alumno con el D.N.I. { request.Documento }");
		return null;
	}


	public async Task<Guid> RegistrarAlumnoAsync(RegistrarAlumnoRequest request)
	{
		try
		{
			if (request is null)
			{
				throw new ArgumentNullException("Datos incompletos para registrar un alumno.");
			}

			var datosPersonales = DatosPersonales.Crear(
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

			var unAlumno = new Alumno(Guid.NewGuid().ToString().GetHashCode().ToString("x"),
									  datosPersonales,
									  domicilio,
									  request.Email,
									  request.Telefono);

			await _unitOfWork.Alumnos.AgregarAsync(unAlumno);
			await _unitOfWork.GuardarCambiosAsync();

			return unAlumno.Id;
		}
		catch (Exception ex)
		{
			throw;
		}
	}

	public async Task<bool> EsDocumentoInvalidoAsync(DocumentoRequest request)
	{
		bool esValido = false;
		esValido = await _unitOfWork.Alumnos.EsDocumentoInvalidoAsync(request.Documento);

		return esValido;
	}

	public async Task ModificarNombreCompleto(Guid alumnoID, string nuevoApellido, string nuevoNombre)
	{
		var alumno = await BuscarAlumnoPorIDAsync(alumnoID);

		try
		{
			alumno.CambiarNombreCompleto(nuevoApellido, nuevoNombre);

			_unitOfWork.Alumnos.Modificar(alumno);
			await _unitOfWork.GuardarCambiosAsync();
		}
		catch (Exception ex)
		{
			throw;
		}
	}

	public async Task ActualizarContacto(CambiarContactoRequest request)
	{
		try
		{
			_logger.LogInformation($"Actualizando datos de contacto del alumno...");

			var unAlumno = await BuscarAlumnoPorIDAsync(request.PersonaID);
			unAlumno.CambiarContacto(request.Email, request.Telefono);

			_unitOfWork.Alumnos.Modificar(unAlumno);
			await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"Datos de contactos actualizados correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task ActualizarDomicilio(CambiarDomicilioRequest request)
	{
		try
		{
			_logger.LogInformation($"Actualizando domicilio del alumno...");

			var unAlumno = await BuscarAlumnoPorIDAsync(request.PersonaID);

			var nuevoDomicilio = Domicilio.Crear(request.Calle,
												 request.Altura,
												 request.Vivienda,
												 request.Observacion,
												 request.Localidad,
												 request.Provincia,
												 request.Pais);
			unAlumno.CambiarDomicilio(nuevoDomicilio);

			_unitOfWork.Alumnos.Modificar(unAlumno);
			await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"Domicilio actualizado correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
			throw;
		}
	}

	public async Task ActualizarSexo(CambiarSexoRequest request)
	{
		try
		{
			_logger.LogInformation($"Actualizando datos de sexo del alumno...");

			var alumno = await BuscarAlumnoPorIDAsync(request.PersonaID);
			alumno.CambiarSexo(request.Apellido, request.Nombre, request.Sexo);

			_unitOfWork.Alumnos.Modificar(alumno);
			await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"Sexo actualizado correctamente.");
		}
		catch(Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}


	public async Task ActualizarDireccion(Guid personaId, DireccionRequest request)
	{
		var alumno = await BuscarAlumnoPorIDAsync(personaId);

		var nuevaDireccion = Direccion.Crear(request.Calle,
											 request.Altura,
											 request.Vivienda,
											 request.Observacion);

		alumno.CambiarDireccion(nuevaDireccion);

		_unitOfWork.Alumnos.Modificar(alumno);
		await _unitOfWork.GuardarCambiosAsync();
	}

	public async Task EliminarAlumnoAsync(EliminarAlumnoRequest request)
	{
		try
		{
			await _unitOfWork.Alumnos.Eliminar(request.AlumnoID);

			await _unitOfWork.GuardarCambiosAsync();
		}
		catch (Exception ex)
		{
			throw;
		}
	}
}
