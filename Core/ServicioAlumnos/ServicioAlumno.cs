using Core.ServicioAlumnos.DTOs.Requests;
using Core.ServicioAlumnos.DTOs.Responses;
using Core.ServicioCursantes.DTOs.Responses;
using Core.Shared.DTOs.Personas.Requests;
using Core.Shared.DTOs.Personas.Response;
using Core.Shared;
using Domain.Alumnos;
using Domain.Asistencias;
using Domain.Cursantes.Calificaciones;
using Domain.Cursantes;
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

	/// <summary>
	/// Un término, dos búsquedas distintas.
	/// </summary>
	/// <remarks>
	/// <para>
	/// La decisión de qué hacer con el término vive acá y no en el ViewModel: es política de
	/// aplicación —qué significa el dato que alguien tipeó— y no presentación, así que se prueba en
	/// <c>Core.UnitTests</c> sin armar una pantalla.
	/// </para>
	/// <para>
	/// El documento se busca <strong>exacto</strong> y el nombre <strong>parcial</strong>, y la
	/// asimetría es a propósito: el documento identifica a una sola persona, así que un
	/// <c>LIKE '%40%'</c> devolvería a toda la institución por tipear dos dígitos. El apellido, en
	/// cambio, lo comparten hermanos y homónimos, y quien busca "perez" no sabe de antemano cuántos
	/// hay.
	/// </para>
	/// </remarks>
	public async Task<IReadOnlyCollection<PersonaResponse>> BuscarAlumnosAsync(BuscarAlumnoRequest request)
	{
		try
		{
			string termino = request?.Termino?.Trim() ?? string.Empty;

			// Un término vacío devuelve vacío y no la matrícula entera: acá la búsqueda es contra la
			// base, y "ver a todos" sería una consulta sin filtro que nadie pidió.
			if (string.IsNullOrEmpty(termino))
			{
				return Array.Empty<PersonaResponse>();
			}

			if (termino.All(char.IsDigit))
			{
				var porDocumento = await _unitOfWork.Alumnos.BuscarPorDocumentoAsync(termino);

				return porDocumento is null
					? Array.Empty<PersonaResponse>()
					: new[] { Describir(porDocumento) };
			}

			var encontrados = await _unitOfWork.Alumnos.BuscarSegunNombreCompletoAsync(termino);

			_logger.LogInformation($"Se encontraron { encontrados.Count } alumnos con coincidencias en el nombre o el apellido.");

			return encontrados.Select(Describir).ToList();
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task<PersonaResponse?> BuscarPorDNIAsync(DocumentoRequest request)
	{
		var alumno = await _unitOfWork.Alumnos.BuscarPorDocumentoAsync(request.Documento);

		// throw new NullReferenceException($"No se encontró el alumno con el D.N.I. { request.Documento }");
		return alumno is null ? null : Describir(alumno);
	}

	/// <summary>
	/// La ficha mínima de un alumno. Estaba escrita dos veces, idéntica, en las dos búsquedas.
	/// </summary>
	private static PersonaResponse Describir(Alumno alumno) =>
		new(PersonaID: alumno.Id,
			Apellido: alumno.DatosPersonales.Apellido,
			Nombre: alumno.DatosPersonales.Nombre,
			Documento: alumno.DatosPersonales.Documento,
			Sexo: alumno.DatosPersonales.Sexo.ToString(),
			FechaNacimiento: alumno.DatosPersonales.FechaNacimiento.ToString("D"),
			Nacionalidad: alumno.DatosPersonales.Nacionalidad);

	#region Resumen académico
	/// <summary>
	/// Qué cursa el alumno, cómo le va y cuánto asiste.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Es una lectura y no exige permiso, igual que las otras ~30 consultas de <c>Core</c>
	/// (CORE-16). Las cuatro excepciones declaradas son las consultas de impacto de borrado, que
	/// existen sólo para servir a su borrado; esta sirve a una pantalla.
	/// </para>
	/// <para>
	/// Cruza cuatro módulos —<c>Cursantes</c> por las inscripciones y las notas, <c>Divisiones</c> y
	/// <c>Cursos</c> por el nombre de lo que cursa, <c>Materias</c> por el nombre de cada materia y
	/// <c>Asistencias</c> por las planillas— y vive igual en <c>ServicioAlumno</c>: el sujeto de la
	/// consulta es el alumno, y la alternativa era que el ViewModel del perfil recibiera cinco
	/// servicios por constructor y armara la proyección a mano, que es exactamente lo que
	/// <c>ResumenAsistenciaResponse</c> documenta como el defecto que vino a cerrar.
	/// </para>
	/// <para>
	/// NO delega en <c>IServicioCursante.ConsultarHistorialDeDivisionesAsync</c>, que devuelve lo
	/// mismo que <see cref="DescribirTrayectoriaAsync"/>: ese método vuelve a consultar las
	/// inscripciones, y acá ya están cargadas con sus calificaciones —que es el dato que el
	/// historial no trae y el promedio necesita—. Delegar costaría la misma consulta dos veces para
	/// ahorrar diez líneas de mapeo.
	/// </para>
	/// <para>
	/// <strong>Acá no se decide nada sobre el rendimiento.</strong> Qué cuenta como instancia
	/// rendida, cómo se promedia y cuándo una materia está aprobada son reglas de
	/// <c>RendimientoAcademico</c>, en <c>Domain</c>. Este método elige el ciclo del que informar,
	/// reúne las calificaciones que la regla necesita y mapea el resultado; lo que decide es de
	/// qué período se habla, que es política de aplicación.
	/// </para>
	/// </remarks>
	public async Task<ResumenAcademicoResponse> ConsultarResumenAcademicoAsync(PersonaRequest request)
	{
		try
		{
			// Valida que el alumno exista antes de informar sobre él: un resumen vacío de un Id
			// inexistente se lee igual que el de un alumno recién registrado.
			var alumno = await BuscarAlumnoPorIDAsync(request.PersonaID);

			var inscripciones = await _unitOfWork.Cursantes.BuscarInscripcionesDelAlumnoAsync(alumno.Id);
			if (inscripciones.Count is 0)
			{
				return ResumenAcademicoResponse.SinTrayectoria(alumno.Id);
			}

			var trayectoria = await DescribirTrayectoriaAsync(inscripciones);

			// El ciclo del que se informa: el de la inscripción vigente, o el de la más reciente si
			// el alumno no está cursando. Nunca la mezcla de todos.
			var cursada = trayectoria.FirstOrDefault(x => x.EstaActiva);
			string ciclo = cursada?.CicloLectivo
						?? trayectoria.OrderByDescending(x => x.FechaInicio).First().CicloLectivo;

			// Todas las inscripciones del ciclo, activas Y CERRADAS: Calificacion es entidad hija de
			// Cursante, así que las notas anteriores a un traslado de división viven en la
			// inscripción cerrada, y promediar sólo la vigente las perdería sin avisar.
			var delCiclo = inscripciones.Where(x => x.CicloLectivo.Periodo.Equals(ciclo)).ToList();
			var calificaciones = delCiclo.SelectMany(x => x.Calificaciones).ToList();

			var materias = await DescribirMateriasAsync(calificaciones);

			return new ResumenAcademicoResponse(
				AlumnoID: alumno.Id,
				CicloLectivo: ciclo,
				Cursada: cursada,
				Promedio: RendimientoAcademico.Promediar(calificaciones),
				Rendidas: RendimientoAcademico.Rendidas(calificaciones).Count(),
				AusenciasAExamen: RendimientoAcademico.AusenciasAExamen(calificaciones).Count(),
				MateriasAprobadas: materias.Count(x => x.Aprobada),
				MateriasDesaprobadas: materias.Count(x => !x.Aprobada),
				Asistencia: await ResumirAsistenciaAsync(delCiclo.Select(x => x.Id).ToList()),
				Materias: materias,
				Trayectoria: trayectoria);
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	/// <summary>
	/// Cada inscripción con el nombre de lo que cursaba, en el orden en que las devuelve el
	/// repositorio: de la más vieja a la más nueva.
	/// </summary>
	/// <remarks>
	/// Una división o un curso borrados dejan el nombre vacío en lugar de hacer fallar la consulta:
	/// la inscripción pasó igual, y un perfil que no se puede abrir porque una división de 2021 ya
	/// no está es peor que uno que la nombra en blanco.
	/// </remarks>
	private async Task<IReadOnlyCollection<InscripcionHistoricaResponse>> DescribirTrayectoriaAsync(IReadOnlyCollection<Cursante> inscripciones)
	{
		var trayectoria = new List<InscripcionHistoricaResponse>();

		foreach (var inscripcion in inscripciones)
		{
			var division = await _unitOfWork.Divisiones.BuscarPorIDAsync(inscripcion.DivisionID);
			var curso = division is null ? null : await _unitOfWork.Cursos.BuscarPorIDAsync(division.CursoID);

			trayectoria.Add(new InscripcionHistoricaResponse(
				CursanteID: inscripcion.Id,
				DivisionID: inscripcion.DivisionID,
				Division: division?.Descripcion ?? string.Empty,
				Grado: curso?.Grado.ToString() ?? string.Empty,
				NivelEducativo: curso?.NivelEducativo.ToString() ?? string.Empty,
				CicloLectivo: inscripcion.CicloLectivo.Periodo,
				FechaInicio: inscripcion.FechaInicio,
				FechaFin: inscripcion.FechaFin,
				EsRecursante: inscripcion.EsRecursante,
				EstaActiva: inscripcion.EstaActiva()));
		}

		return trayectoria;
	}

	/// <summary>
	/// Una fila por materia, alfabética, agrupando todas las instancias que el alumno tiene en ella.
	/// </summary>
	/// <remarks>
	/// Lo único que decide este método es el ORDEN y el NOMBRE: el orden porque es el de la tabla
	/// que lo consume, y el nombre porque resolverlo exige el repositorio de materias, que es
	/// trabajo de aplicación. Cuánto rindió, cuánto promedia y si aprobó lo decide
	/// <see cref="RendimientoAcademico"/>, en el dominio.
	/// </remarks>
	private async Task<IReadOnlyCollection<MateriaCursadaResponse>> DescribirMateriasAsync(IReadOnlyCollection<Calificacion> calificaciones)
	{
		var materias = new List<MateriaCursadaResponse>();

		foreach (var grupo in calificaciones.GroupBy(x => x.MateriaID))
		{
			var materia = await _unitOfWork.Materias.BuscarPorIDAsync(grupo.Key);

			materias.Add(new MateriaCursadaResponse(
				MateriaID: grupo.Key,
				Materia: materia?.Descripcion ?? string.Empty,
				Rendidas: RendimientoAcademico.Rendidas(grupo).Count(),
				AusenciasAExamen: RendimientoAcademico.AusenciasAExamen(grupo).Count(),
				Promedio: RendimientoAcademico.Promediar(grupo),
				Aprobada: RendimientoAcademico.EstaAprobada(grupo)));
		}

		return materias.OrderBy(x => x.Materia).ToList();
	}

	/// <summary>
	/// El acumulado de asistencia de esas inscripciones.
	/// </summary>
	/// <remarks>
	/// Recibe los cursantes del ciclo y no el alumno porque la asistencia se registra contra la
	/// inscripción, no contra la persona: un alumno trasladado tiene dos cursantes en el mismo
	/// ciclo, y su asistencia del año es la suma de los dos.
	/// </remarks>
	private async Task<AsistenciaAcumuladaResponse> ResumirAsistenciaAsync(IReadOnlyCollection<Guid> cursantesIDs)
	{
		var planillas = await _unitOfWork.PlanillasAsistencia.BuscarPorCursantesAsync(cursantesIDs);

		// El filtro por CursanteID NO es redundante: la consulta trae la planilla completa porque
		// alguno de estos cursantes tiene registro en ella, y adentro vienen también las marcas de
		// todos sus compañeros de división.
		var registros = planillas
			.SelectMany(x => x.Registros)
			.Where(x => cursantesIDs.Contains(x.CursanteID))
			.ToList();

		if (registros.Count is 0)
		{
			return AsistenciaAcumuladaResponse.SinRegistro();
		}

		int ausencias = registros.Count(x => x.Tipo.Equals(TipoAsistencia.Ausencia));
		int inasistencias = registros.Count(x => x.Tipo.Equals(TipoAsistencia.Inasistencia));

		return new AsistenciaAcumuladaResponse(
			Jornadas: registros.Count,
			Presentes: registros.Count(x => x.Tipo.Equals(TipoAsistencia.Presente)),
			Ausencias: ausencias,
			Inasistencias: inasistencias,
			Tardanzas: registros.Count(x => x.Tipo.Equals(TipoAsistencia.Tardanza)),
			Faltas: ausencias + inasistencias,
			Porcentaje: PorcentajeDeAsistencia.Calcular(asistidas: registros.Count - ausencias - inasistencias,
													   sobre: registros.Count));
	}
	#endregion


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
