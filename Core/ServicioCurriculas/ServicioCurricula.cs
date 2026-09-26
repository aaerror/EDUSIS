using Core.ServicioCurriculas.DTOs.Requests;
using Core.ServicioCurriculas.DTOs.Responses;
using Core.Shared;
using Domain.Curriculas;
using Domain.Shared;
using Microsoft.Extensions.Logging;

namespace Core.ServicioCurriculas;

internal class ServicioCurricula : IServicio, IServicioCurricula
{
	private readonly IUnitOfWork _unitOfWork;
	private readonly ILogger<ServicioCurricula> _logger;


	public ServicioCurricula(IUnitOfWork unitOfWork, ILogger<ServicioCurricula> logger)
	{
		_unitOfWork = unitOfWork;
		_logger = logger;
	}

	#region Curricula: Listar, Registrar, Eliminar
	private async Task<Curricula> BuscarCurriculaAsync(CurriculaRequest request)
	{
		_logger.LogInformation($"Buscando curricula { request.CurriculaID }...");

		var unaCurricula = await _unitOfWork.Curriculas.BuscarCurriculaAsync(request.CursoID, request.CurriculaID);
		if (unaCurricula is null)
		{
			_logger.LogInformation($"No se encontró la currícula del curso.");

			throw new ArgumentNullException("No se encontró la currícula del curso.");
		}

		return unaCurricula;
	}

	public async Task<IReadOnlyCollection<CurriculaResponse>> ListarCurriculasSegunCursoAsync(CursoRequest request)
	{
		try
		{
			var curriculas = await _unitOfWork.Curriculas.CurriculasSegunCursoAsync(request.CursoID);

			_logger.LogInformation($"Se encontraron { curriculas.Count() } curriculas en el diseño curricular.");

			return curriculas.Select(x =>
				new CurriculaResponse(
					CursoID: x.CursoID,
					CurriculaID: x.Id,
					FechaInicio: x.Periodo.FechaInicio,
					FechaFin: x.Periodo.FechaFin))
				.ToList();
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task RegistrarCurriculaAsync(RegistrarCurriculaRequest request)
	{
		try
		{
			await _unitOfWork.BeginTransactionAsync();

			var curriculas = await _unitOfWork.Curriculas.CurriculasSegunCursoAsync(request.CursoID);
			if (curriculas.Count() > 0)
			{
				var hayActivas = curriculas.Where(x => x.Periodo.EsIndeterminado())
										   .Any();
				if (hayActivas)
				{
					foreach (var curricula in curriculas)
					{
						curricula.Desafectar();
					}
				}
			}

			var nuevaCurricula = new Curricula(request.CursoID, request.FechaInicio, request.FechaFin);

			_unitOfWork.Curriculas.ModificarRango(curriculas);
			await _unitOfWork.GuardarCambiosAsync();

			await _unitOfWork.Curriculas.AgregarAsync(nuevaCurricula);
			await _unitOfWork.GuardarCambiosAsync();

			await _unitOfWork.CommitTransactionAsync();
		}
		catch (Exception ex)
		{
			await _unitOfWork.RollbackTransactionAsync();

			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task DesafectarCurriculaAsync(CurriculaRequest request)
	{
		try
		{
			var curricula = await BuscarCurriculaAsync(request);
			curricula.Desafectar();

			_unitOfWork.Curriculas.Modificar(curricula);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } diseño curricular modificado correctamente.", result);
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion
}
