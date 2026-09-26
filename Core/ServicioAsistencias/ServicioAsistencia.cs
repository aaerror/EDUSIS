using Core.ServicioAsistencias.DTOs.Requests;
using Core.ServicioAsistencias.DTOs.Responses;
using Core.ServicioAsistencias.Exceptions;
using Core.Shared;
using Domain.Asistencias;
using Domain.Shared;
using Microsoft.Extensions.Logging;

namespace Core.ServicioAsistencias;

internal class ServicioAsistencia : IServicio, IServicioAsistencia
{
	private readonly ILogger<ServicioAsistencia> _logger;
	private readonly IUnitOfWork _unitOfWork;


	public ServicioAsistencia(ILogger<ServicioAsistencia> logger, IUnitOfWork unitOfWork)
	{
		_logger = logger;
		_unitOfWork = unitOfWork;
	}

	#region Abrir planilla
	public async Task<PlanillaAsistenciaResponse> AbrirPlanillaAsync(AbrirPlanillaRequest request)
	{
		try
		{
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
	#endregion

	#region Privados
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
