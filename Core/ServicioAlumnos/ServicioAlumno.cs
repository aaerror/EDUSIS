using Core.ServicioAlumnos.DTOs.Requests;
using Core.ServicioAlumnos.DTOs.Responses;
using Core.Shared.DTOs.Personas.Requests;
using Core.Shared.DTOs.Personas.Response;
using Core.Shared;
using Domain.Alumnos;
using Domain.Personas.Domicilios;
using Domain.Personas;
using Domain.Shared;
using Microsoft.Extensions.Logging;
using Core.Shared.Seguridad;
using Domain.Usuarios;

namespace Core.ServicioAlumnos;

internal class ServicioAlumno : IServicio, IServicioAlumno
{
	private readonly ILogger<ServicioAlumno> _logger;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ISesionActual _sesion;


	public ServicioAlumno(ILogger<ServicioAlumno> logger, IUnitOfWork unitOfWork, ISesionActual sesion)
	{
		_sesion = sesion;
		_logger = logger;
		_unitOfWork = unitOfWork;
	}

	private async Task<Alumno> BuscarAlumnoPorIDAsync(Guid personaID)
	{
		var alumno = await _unitOfWork.Alumnos.BuscarPorIDAsync(personaID);
		if (alumno is null)
		{
			var msg = $"No se encontró el alumno con el siguiente Id: { personaID }";
			throw new NullReferenceException(msg);
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
			_sesion.Exigir(Permiso.GestionarAlumnos);

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

			var unAlumno = new Alumno(
				Alumno.SiguienteLegajo(await _unitOfWork.Alumnos.BuscarUltimoLegajoAsync()),
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
		_sesion.Exigir(Permiso.GestionarAlumnos);

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
			_sesion.Exigir(Permiso.GestionarAlumnos);

			_logger.LogInformation($"Actualizando datos de contacto del alumno...");

			var unAlumno = await BuscarAlumnoPorIDAsync(request.PersonaID);
			unAlumno.CambiarEmail(request.Email);
			unAlumno.CambiarTelefono(request.Telefono);

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
			_sesion.Exigir(Permiso.GestionarAlumnos);

			_logger.LogInformation($"Actualizando domicilio del alumno...");

			var unAlumno = await BuscarAlumnoPorIDAsync(request.PersonaID);

			var nuevoDomicilio = Domicilio.Crear(
				request.Calle,
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
			_sesion.Exigir(Permiso.GestionarAlumnos);

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
		_sesion.Exigir(Permiso.GestionarAlumnos);

		var alumno = await BuscarAlumnoPorIDAsync(personaId);

		var nuevaDireccion = Direccion.Crear(
			request.Calle,
			request.Altura,
			request.Vivienda,
			request.Observacion);

		alumno.CambiarDireccion(nuevaDireccion);

		_unitOfWork.Alumnos.Modificar(alumno);
		await _unitOfWork.GuardarCambiosAsync();
	}

	/// <summary>
	/// Qué se lleva puesto el borrado del alumno, sin borrar nada. La consume el diálogo de
	/// confirmación para poder decir cifras en lugar de generalidades.
	/// </summary>
	/// <remarks>
	/// Es una lectura y lleva guarda, a diferencia del resto de las consultas de <c>Core</c>, que
	/// quedan abiertas a propósito (CORE-16). Dos razones: existe únicamente para servir a
	/// <see cref="EliminarAlumnoAsync"/>, que exige <c>GestionarAlumnos</c>, y lo que devuelve es el
	/// volumen de la trayectoria académica de una persona. Ninguna pantalla la usa para poblar un
	/// combo, así que no corre el riesgo que motivó dejar las lecturas abiertas.
	/// </remarks>
	public async Task<ImpactoEliminacionAlumnoResponse> ConsultarImpactoEliminacionAsync(EliminarAlumnoRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GestionarAlumnos);

			var alumno = await BuscarAlumnoPorIDAsync(request.AlumnoID);

			var cursantes = await _unitOfWork.Cursantes.BuscarInscripcionesDelAlumnoAsync(alumno.Id);
			var cursantesIDs = cursantes.Select(x => x.Id).ToList();
			var planillas = await _unitOfWork.PlanillasAsistencia.BuscarPorCursantesAsync(cursantesIDs);

			var registros = planillas.SelectMany(p => p.Registros)
									 .Count(r => cursantesIDs.Contains(r.CursanteID));

			// Una planilla queda vacía si TODOS sus registros son de este alumno. Se informa porque
			// borrarla es destruir la jornada completa de una división, no sólo la fila del alumno.
			var planillasVacias = planillas.Count(p => p.Registros.All(r => cursantesIDs.Contains(r.CursanteID)));

			return new ImpactoEliminacionAlumnoResponse(
				Inscripciones: cursantes.Count,
				Calificaciones: cursantes.Sum(x => x.Calificaciones.Count),
				RegistrosAsistencia: registros,
				PlanillasQueQuedanVacias: planillasVacias,
				CiclosLectivos: cursantes.Select(x => x.CicloLectivo.Periodo)
										 .Distinct()
										 .OrderBy(x => x)
										 .ToList());
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	/// <summary>
	/// Borrado físico del alumno y de toda su trayectoria: inscripciones, calificaciones y registros
	/// de asistencia.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>La cascada se hace acá y no con <c>DeleteBehavior.Cascade</c></strong>, y es una
	/// decisión, no una omisión. <c>FK_CURSANTE_ALUMNO</c> y <c>FK_REGISTRO_ASISTENCIA_CURSANTE</c>
	/// siguen en <c>Restrict</c> por tres motivos: (1) <c>registro_asistencia</c> ya recibe una
	/// cascada desde <c>planilla_asistencia</c>, y sumarle una segunda desde <c>cursante</c> entra en
	/// el terreno donde SQL Server rechaza por "multiple cascade paths" —un fallo que no aparece al
	/// generar la migración sino al aplicarla—; (2) con <c>Restrict</c>, cualquier borrado que NO pase
	/// por este caso de uso sigue protegido por la base; (3) las reglas quedan acá, legibles y
	/// cubiertas por pruebas, en lugar de en metadatos de clave ajena.
	/// </para>
	/// <para>
	/// El orden es obligatorio y lo impone el grafo de claves ajenas: registros de asistencia →
	/// cursantes (que cascadea a sus calificaciones por <c>FK_CALIFICACION_CURSANTE</c>) → alumno.
	/// Invertir dos pasos da una violación de integridad.
	/// </para>
	/// <para>
	/// La transacción envuelve todo porque el borrado parcial es el peor resultado posible: un alumno
	/// sin inscripciones pero con registros de asistencia apuntando a cursantes que ya no existen.
	/// El <c>try</c> del rollback arranca <strong>después</strong> de <c>BeginTransactionAsync</c>, por
	/// lo mismo que documenta <c>ServicioCurricula.RegistrarCurriculaAsync</c>: si la guarda de
	/// permiso falla, no hay transacción que revertir y el rollback taparía el
	/// <c>PermisoDenegadoException</c> con un error de transacción. Ver CORE-19.
	/// </para>
	/// </remarks>
	public async Task EliminarAlumnoAsync(EliminarAlumnoRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GestionarAlumnos);

			var alumno = await BuscarAlumnoPorIDAsync(request.AlumnoID);

			await _unitOfWork.BeginTransactionAsync();

			try
			{
				var cursantes = await _unitOfWork.Cursantes.BuscarInscripcionesDelAlumnoAsync(alumno.Id);
				var cursantesIDs = cursantes.Select(x => x.Id).ToHashSet();

				// 1. Los registros de asistencia, a través de su raíz. RegistroAsistencia es entidad
				//    hija de PlanillaAsistencia: no se toca por fuera del agregado.
				var planillas = await _unitOfWork.PlanillasAsistencia.BuscarPorCursantesAsync(cursantesIDs);

				var planillasVaciadas = new List<Domain.Asistencias.PlanillaAsistencia>();
				var planillasModificadas = new List<Domain.Asistencias.PlanillaAsistencia>();

				foreach (var planilla in planillas)
				{
					foreach (var cursanteID in planilla.Registros
													   .Where(r => cursantesIDs.Contains(r.CursanteID))
													   .Select(r => r.CursanteID)
													   .ToList())
					{
						planilla.EliminarRegistroDe(cursanteID);
					}

					// Una planilla sin registros no documenta ninguna jornada: se va con el alumno.
					if (planilla.Registros.Count is 0)
					{
						planillasVaciadas.Add(planilla);
					}
					else
					{
						planillasModificadas.Add(planilla);
					}
				}

				_unitOfWork.PlanillasAsistencia.ModificarRango(planillasModificadas);
				_unitOfWork.PlanillasAsistencia.EliminarRango(planillasVaciadas);
				await _unitOfWork.GuardarCambiosAsync();

				// 2. Las inscripciones. Sus calificaciones caen por FK_CALIFICACION_CURSANTE, que sí
				//    es Cascade: son entidades hijas del cursante y no tienen vida propia.
				_unitOfWork.Cursantes.EliminarRango(cursantes);
				await _unitOfWork.GuardarCambiosAsync();

				// 3. El legajo.
				await _unitOfWork.Alumnos.EliminarAsync(alumno.Id);
				await _unitOfWork.GuardarCambiosAsync();

				await _unitOfWork.CommitTransactionAsync();

				_logger.LogInformation($"Alumno { alumno.Id } eliminado con { cursantes.Count } inscripciones.");
			}
			catch
			{
				await _unitOfWork.RollbackTransactionAsync();
				throw;
			}
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
}
