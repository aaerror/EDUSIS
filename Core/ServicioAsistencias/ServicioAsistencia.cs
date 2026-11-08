using Core.ServicioAsistencias.DTOs.Requests;
using Core.ServicioAsistencias.DTOs.Responses;
using Core.ServicioAsistencias.Exceptions;
using Core.Shared;
using Core.Shared.Seguridad;
using Domain.Asistencias;
using Domain.Shared;
using Domain.Usuarios;
using Microsoft.Extensions.Logging;

namespace Core.ServicioAsistencias;

internal class ServicioAsistencia : IServicio, IServicioAsistencia
{
	private readonly ILogger<ServicioAsistencia> _logger;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ISesionActual _sesion;


	public ServicioAsistencia(ILogger<ServicioAsistencia> logger, IUnitOfWork unitOfWork, ISesionActual sesion)
	{
		_sesion = sesion;
		_logger = logger;
		_unitOfWork = unitOfWork;
	}

	#region Abrir planilla
	public async Task<PlanillaAsistenciaResponse> AbrirPlanillaAsync(AbrirPlanillaRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.TomarAsistencia);

			var existePlanilla = await _unitOfWork.PlanillasAsistencia.ExistePlanillaAsync(request.DivisionID, request.Fecha);
			if (existePlanilla)
			{
				throw new PlanillaDuplicadaException();
			}

			var division = await _unitOfWork.Divisiones.BuscarPorIDAsync(request.DivisionID);
			if (division is null)
			{
				throw new NullReferenceException("División no encontrada.");
			}

			var cursantesIDs = await _unitOfWork.Cursantes.BuscarInscriptosEnFechaAsync(request.DivisionID, request.Fecha);

			// Si la división no tiene preceptor asignado, se pasa Guid.Empty: el propio dominio
			// rechaza la apertura con SinDatosPreceptorException, sin duplicar la validación acá.
			var planilla = PlanillaAsistencia.Abrir(request.DivisionID, request.Fecha, division.Preceptor ?? Guid.Empty, cursantesIDs);

			await _unitOfWork.PlanillasAsistencia.AgregarAsync(planilla);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } planilla de asistencia abierta correctamente.");

			return MapearAResponse(planilla);
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion

	#region Marcado de asistencias
	public async Task MarcarPresenteAsync(MarcarPresenteRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.TomarAsistencia);

			var planilla = await BuscarPlanillaAsync(request.DivisionID, request.Fecha);

			planilla.MarcarPresente(request.CursanteID);

			_unitOfWork.PlanillasAsistencia.Modificar(planilla);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } asistencia marcada como presente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task MarcarAusenciaAsync(MarcarAusenciaRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.TomarAsistencia);

			var planilla = await BuscarPlanillaAsync(request.DivisionID, request.Fecha);

			planilla.MarcarAusencia(request.CursanteID, request.Observacion);

			_unitOfWork.PlanillasAsistencia.Modificar(planilla);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } asistencia marcada como ausencia.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task MarcarInasistenciaAsync(MarcarInasistenciaRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.TomarAsistencia);

			var planilla = await BuscarPlanillaAsync(request.DivisionID, request.Fecha);

			planilla.MarcarInasistencia(request.CursanteID, request.Observacion);

			_unitOfWork.PlanillasAsistencia.Modificar(planilla);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } asistencia marcada como inasistencia.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task MarcarTardanzaAsync(MarcarTardanzaRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.TomarAsistencia);

			var planilla = await BuscarPlanillaAsync(request.DivisionID, request.Fecha);

			planilla.MarcarTardanza(request.CursanteID, request.Minutos, request.Observacion);

			_unitOfWork.PlanillasAsistencia.Modificar(planilla);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } asistencia marcada como tardanza.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion

	#region Incorporar cursante
	public async Task IncorporarCursanteAsync(IncorporarCursanteRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.TomarAsistencia);

			var planilla = await BuscarPlanillaAsync(request.DivisionID, request.Fecha);

			planilla.IncorporarCursante(request.CursanteID);

			_unitOfWork.PlanillasAsistencia.Modificar(planilla);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } cursante incorporado a la planilla.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion

	#region Ciclo (cerrar/reabrir)
	public async Task CerrarPlanillaAsync(CerrarPlanillaRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.TomarAsistencia);

			var planilla = await BuscarPlanillaAsync(request.DivisionID, request.Fecha);

			planilla.Cerrar();

			_unitOfWork.PlanillasAsistencia.Modificar(planilla);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } planilla cerrada correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task ReabrirPlanillaAsync(ReabrirPlanillaRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.TomarAsistencia);

			var planilla = await BuscarPlanillaAsync(request.DivisionID, request.Fecha);

			planilla.Reabrir();

			_unitOfWork.PlanillasAsistencia.Modificar(planilla);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } planilla reabierta correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion

	#region Eliminar planilla
	public async Task EliminarPlanillaAsync(EliminarPlanillaRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.TomarAsistencia);

			var planilla = await BuscarPlanillaAsync(request.DivisionID, request.Fecha);

			// La regla ("una planilla cerrada es registro histórico y no se borra") vive en el
			// dominio: acá sólo se la invoca. Los registro_asistencia hijos los borra la base por
			// el OnDelete(Cascade) de PlanillasAsistenciaConfiguration, así que no hay que
			// recorrerlos ni vaciar la colección antes de eliminar la raíz.
			planilla.ValidarSePuedeEliminar();

			await _unitOfWork.PlanillasAsistencia.EliminarAsync(planilla.Id);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } planilla eliminada correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion

	#region Consultas
	public async Task<PlanillaAsistenciaResponse> ConsultarPlanillaAsync(ConsultarPlanillaRequest request)
	{
		try
		{
			var planilla = await BuscarPlanillaAsync(request.DivisionID, request.Fecha);

			return MapearAResponse(planilla);
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task<int> ContarFaltasAsync(ContarFaltasRequest request)
	{
		try
		{
			return await _unitOfWork.PlanillasAsistencia.ContarFaltasAsync(request.CursanteID, request.Tipo);
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	/// <remarks>
	/// Ordena de la más reciente a la más vieja, al revés que el repositorio. Es un historial: lo que
	/// se busca en él es "qué cargué estos días", y la planilla de ayer está más cerca de esa pregunta
	/// que la del primer día de clase. El repositorio ordena ascendente porque su otro consumidor es la
	/// emisión de certificados, que lee el período como una línea de tiempo.
	/// </remarks>
	public async Task<IReadOnlyCollection<PlanillaResumenResponse>> ListarPlanillasAsync(ListarPlanillasRequest request)
	{
		try
		{
			var planillas = await BuscarRangoAsync(request.DivisionID, request.Desde, request.Hasta);

			return planillas
				.OrderByDescending(p => p.Fecha)
				.Select(p => new PlanillaResumenResponse(
					PlanillaID: p.Id,
					Fecha: p.Fecha,
					Cerrada: p.Cerrada,
					Registros: p.Registros.Count,
					Presentes: p.CantidadCon(TipoAsistencia.Presente),
					Ausencias: p.CantidadCon(TipoAsistencia.Ausencia),
					Inasistencias: p.CantidadCon(TipoAsistencia.Inasistencia),
					Tardanzas: p.CantidadCon(TipoAsistencia.Tardanza)))
				.ToList();
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	/// <remarks>
	/// Recorre las planillas del período UNA vez y agrupa sus registros por cursante. La alternativa
	/// —preguntarle al repositorio por cada alumno— no existe: <c>ContarFaltasAsync</c> es por cursante
	/// y por tipo, no acepta fechas, y daría cuatro consultas por alumno contando toda su historia en
	/// lugar del período pedido.
	/// </remarks>
	public async Task<ResumenAsistenciaResponse> ConsultarResumenAsync(ConsultarResumenRequest request)
	{
		try
		{
			var (desde, hasta) = Ordenar(request.Desde, request.Hasta);

			var planillas = await BuscarRangoAsync(request.DivisionID, desde, hasta);

			var registros = planillas.SelectMany(p => p.Registros).ToList();

			var cursantes = registros
				.GroupBy(r => r.CursanteID)
				.Select(grupo =>
				{
					var ausencias = grupo.Count(r => r.Tipo.Equals(TipoAsistencia.Ausencia));
					var inasistencias = grupo.Count(r => r.Tipo.Equals(TipoAsistencia.Inasistencia));

					return new ResumenCursanteResponse(
						CursanteID: grupo.Key,
						Jornadas: grupo.Count(),
						Presentes: grupo.Count(r => r.Tipo.Equals(TipoAsistencia.Presente)),
						Ausencias: ausencias,
						Inasistencias: inasistencias,
						Tardanzas: grupo.Count(r => r.Tipo.Equals(TipoAsistencia.Tardanza)),
						Faltas: ausencias + inasistencias,
						PorcentajeAsistencia: Porcentaje(asistidas: grupo.Count() - ausencias - inasistencias,
														sobre: grupo.Count()));
				})
				// De más faltas a menos: la primera fila es el alumno del que hay que ocuparse. A
				// igual cantidad de faltas, primero el que menos jornadas tuvo, porque las mismas
				// faltas sobre menos días son más graves.
				.OrderByDescending(c => c.Faltas)
				.ThenBy(c => c.Jornadas)
				.ToList();

			var ausenciasTotales = registros.Count(r => r.Tipo.Equals(TipoAsistencia.Ausencia));
			var inasistenciasTotales = registros.Count(r => r.Tipo.Equals(TipoAsistencia.Inasistencia));

			return new ResumenAsistenciaResponse(
				Desde: desde,
				Hasta: hasta,
				Planillas: planillas.Count,
				PlanillasAbiertas: planillas.Count(p => !p.Cerrada),
				Marcas: registros.Count,
				Presentes: registros.Count(r => r.Tipo.Equals(TipoAsistencia.Presente)),
				Ausencias: ausenciasTotales,
				Inasistencias: inasistenciasTotales,
				Tardanzas: registros.Count(r => r.Tipo.Equals(TipoAsistencia.Tardanza)),
				PorcentajeAsistencia: Porcentaje(asistidas: registros.Count - ausenciasTotales - inasistenciasTotales,
												 sobre: registros.Count),
				Cursantes: cursantes);
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion

	#region Privados
	/// <summary>
	/// Las planillas del período, con los extremos ya ordenados. Si vienen al revés se los da vuelta en
	/// lugar de devolver vacío: un rango invertido es casi siempre un par de fechas elegidas en el orden
	/// equivocado, y una lista vacía no se lo diría a nadie.
	/// </summary>
	private async Task<IReadOnlyCollection<PlanillaAsistencia>> BuscarRangoAsync(Guid divisionID, DateTime desde, DateTime hasta)
	{
		var (inicio, fin) = Ordenar(desde, hasta);

		return await _unitOfWork.PlanillasAsistencia.BuscarPorDivisionYRangoAsync(divisionID, inicio, fin);
	}

	/// <summary>
	/// Los dos extremos en orden, sin hora.
	/// </summary>
	/// <remarks>
	/// Lo usan la búsqueda Y el armado del response, y ése es el punto: el resumen informa el período
	/// que efectivamente consultó. Normalizar sólo para buscar devolvía los números correctos dentro de
	/// una cabecera que se describía al revés —"del 02/11 al 31/10"—, y la pantalla muestra esa
	/// cabecera.
	/// </remarks>
	private static (DateTime Desde, DateTime Hasta) Ordenar(DateTime desde, DateTime hasta)
	{
		return desde.Date <= hasta.Date
			? (desde.Date, hasta.Date)
			: (hasta.Date, desde.Date);
	}

	/// <summary>
	/// El porcentaje de asistencia. La fórmula se mudó a <see cref="PorcentajeDeAsistencia"/> al
	/// necesitarla también el resumen académico del alumno: dos copias significarían que el
	/// preceptor y el perfil pueden informar números distintos sobre los mismos registros, que es
	/// el defecto que <c>Calificacion.NOTA_APROBACION</c> evita por el mismo motivo.
	/// </summary>
	private static decimal Porcentaje(int asistidas, int sobre) =>
		PorcentajeDeAsistencia.Calcular(asistidas, sobre);

	private async Task<PlanillaAsistencia> BuscarPlanillaAsync(Guid divisionID, DateTime fecha)
	{
		var planilla = await _unitOfWork.PlanillasAsistencia.BuscarPorDivisionYFechaAsync(divisionID, fecha);
		if (planilla is null)
		{
			throw new NullReferenceException("Planilla de asistencia no encontrada.");
		}

		return planilla;
	}

	private static PlanillaAsistenciaResponse MapearAResponse(PlanillaAsistencia planilla)
	{
		return new PlanillaAsistenciaResponse(
			PlanillaID: planilla.Id,
			DivisionID: planilla.DivisionID,
			Fecha: planilla.Fecha,
			PreceptorID: planilla.PreceptorID,
			Cerrada: planilla.Cerrada,
			Registros: planilla.Registros
				.Select(r => new RegistroAsistenciaResponse(
					CursanteID: r.CursanteID,
					Tipo: r.Tipo,
					Minutos: r.Minutos,
					Observacion: r.Observacion))
				.ToList(),
			Presentes: planilla.CantidadCon(TipoAsistencia.Presente),
			Ausencias: planilla.CantidadCon(TipoAsistencia.Ausencia),
			Inasistencias: planilla.CantidadCon(TipoAsistencia.Inasistencia),
			Tardanzas: planilla.CantidadCon(TipoAsistencia.Tardanza));
	}
	#endregion
}
