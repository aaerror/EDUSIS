using Core.ServicioDocentes.DTOs.Requests;
using Core.ServicioLicencias.DTOs.Requests;
using Core.ServicioLicencias.DTOs.Responses;
using Core.Shared;
using Domain.Licencias;
using Domain.Shared;
using Microsoft.Extensions.Logging;

namespace Core.ServicioLicencias;

internal class ServicioLicencia : IServicio, IServicioLicencia
{
	private readonly ILogger<ServicioLicencia> _logger;
	private readonly IUnitOfWork _unitOfWork;


	public ServicioLicencia(ILogger<ServicioLicencia> logger, IUnitOfWork unitOfWork)
	{
		_logger = logger;
		_unitOfWork = unitOfWork;
	}

	public async Task<IReadOnlyCollection<LicenciaResponse>> BuscarLicenciasSegunDocenteAsync(DocenteIDRequest request)
	{
		try
		{
			var licencias = await _unitOfWork.Licencias.BuscarLicenciasDeDocenteAsync(request.DocenteID);
			return licencias.Select(x =>
				new LicenciaResponse(
					LicenciaID: x.Id,
					DocenteID: x.DocenteID,
					Articulo: x.Articulo.ToString(),
					Estado: x.Estado.ToString(),
					Dias: Convert.ToInt32(x.DuracionEn(Domain.Shared.Tiempo.Dia)),
					FechaInicio: x.Periodo.FechaInicio.Date,
					FechaFin: x.Periodo.FechaFin,
					Observacion: x.Observacion))
			.ToList();
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task AprobarLicencia(ActualizarEstadoLicenciaRequest request)
	{
		try
		{
			var licencia = await _unitOfWork.Licencias.BuscarPorIDAsync(request.LicenciaID, request.DocenteID);
			if (licencia is null)
			{
				throw new NullReferenceException("Licencia no encontrada.");
			}

			licencia.AprobarLicencia(request.Observacion);

			_unitOfWork.Licencias.Modificar(licencia);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } licencia aprobada correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task RechazarLicencia(ActualizarEstadoLicenciaRequest request)
	{
		try
		{
			var licencia = await _unitOfWork.Licencias.BuscarPorIDAsync(request.LicenciaID, request.DocenteID);
			if (licencia is null)
			{
				throw new NullReferenceException("Licencia no encontrada.");
			}

			licencia.CancelarLicencia(request.Observacion);

			_unitOfWork.Licencias.Modificar(licencia);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } licencia cancelada correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task ModificarLicencia(ModificarLicenciaRequest request)
	{
		try
		{
			var licencia = await _unitOfWork.Licencias.BuscarPorIDAsync(request.LicenciaID, request.DocenteID);
			if (licencia is null)
			{
				throw new NullReferenceException("Licencia no encontrada.");
			}

			licencia.ModificarPeriodo(request.FechaInicio, request.Dias);
			licencia.ModificarObservaciones(request.Observacion);

			_unitOfWork.Licencias.Modificar(licencia);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } licencia modificada correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task EliminarLicencia(EliminarLicenciaRequest request)
	{
		try
		{
			var licencia = await _unitOfWork.Licencias.BuscarPorIDAsync(request.LicenciaID, request.DocenteID);
			if (licencia is null)
			{
				throw new NullReferenceException("Licencia no encontrada.");
			}

			var estado = licencia.Estado;
			if (!estado.Equals(Estado.Pendiente))
			{
				throw new ArgumentException("Sólo se pueden eliminar licencias que se encuentran pendientes de aprobar.");
			}

			await _unitOfWork.Licencias.Eliminar(licencia.Id, licencia.DocenteID);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } licencia eliminada correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
			throw;
		}
	}

	public async Task SolicitarLicencia(NuevaSolicitudLicenciaRequest request)
	{
		try
		{
			var existeDocente = await _unitOfWork.Docentes.ExisteIDAsync(request.DocenteID);
			if (!existeDocente)
			{
				throw new NullReferenceException("Datos incompletos para registrar la licencia.");
			}

			var licencia = new Licencia(request.DocenteID, request.Articulo, request.FechaInicio, request.Observacion);
			if (request.Dias > 0)
			{
				var fechaFin = request.FechaInicio.AddDays(request.Dias);
				licencia.EstablecerFechaFinalizacion(fechaFin);
			}

			await _unitOfWork.Licencias.AgregarAsync(licencia);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } licencia creada correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
}