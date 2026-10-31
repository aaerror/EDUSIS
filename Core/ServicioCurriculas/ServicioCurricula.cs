using Core.ServicioCurriculas.DTOs.Requests;
using Core.ServicioCurriculas.DTOs.Responses;
using Core.Shared;
using Core.Shared.Seguridad;
using Domain.Curriculas;
using Domain.Curriculas.Exceptions;
using Domain.Shared;
using Domain.Usuarios;
using Microsoft.Extensions.Logging;

namespace Core.ServicioCurriculas;

internal class ServicioCurricula : IServicio, IServicioCurricula
{
	private readonly IUnitOfWork _unitOfWork;
	private readonly ILogger<ServicioCurricula> _logger;
	private readonly ISesionActual _sesion;


	public ServicioCurricula(IUnitOfWork unitOfWork, ILogger<ServicioCurricula> logger, ISesionActual sesion)
	{
		_sesion = sesion;
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

			throw new CurriculaNoEncontradaException();
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
			_sesion.Exigir(Permiso.GestionarCurriculas);

			await _unitOfWork.BeginTransactionAsync();

			// El rollback va en un try PROPIO que arranca después de BeginTransactionAsync, y el
			// tramo no es decorativo. UnitOfWork.RollbackTransactionAsync pasa por TransaccionActual,
			// que lanza InvalidOperationException("No hay una transacción iniciada.") si no hay
			// ninguna: con un solo catch alrededor de todo el método, un PermisoDenegadoException de
			// la guarda de arriba —o un "Ya hay una transacción iniciada" del propio Begin— entraba
			// al rollback, reventaba ahí y llegaba al ViewModel disfrazado de error de transacción.
			// Justo la clase de fallo que conviene que llegue nítido: es RBAC. Ver CORE-19.
			try
			{
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

	public async Task DesafectarCurriculaAsync(CurriculaRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GestionarCurriculas);

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
