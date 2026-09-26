using Core.ServicioCursos.DTOs.Requests;
using Core.ServicioCursos.DTOs.Responses;
using Core.ServicioCursos.Exceptions;
using Core.Shared;
using Domain.Cursos;
using Domain.Shared;
using Microsoft.Extensions.Logging;

namespace Core.ServicioCursos;

internal class ServicioCurso : IServicio, IServicioCurso
{
	private readonly IUnitOfWork _unitOfWork;
	private readonly ILogger<ServicioCurso> _logger;


	public ServicioCurso(IUnitOfWork unitOfWork, ILogger<ServicioCurso> logger)
	{
		_unitOfWork = unitOfWork;
		_logger = logger;
	}

	#region Registrar, Modificar, Eliminar
	public async Task<IReadOnlyCollection<CursoResponse>> ListarCursosAsync()
	{
		try
		{
			var cursos = await _unitOfWork.Cursos.BuscarTodosAsync();

			return cursos
				.Select(x =>
					new CursoResponse(
						CursoID: x.Id,
						Grado: x.Grado,
						NivelEducativo: x.NivelEducativo))
				.OrderBy(x => x.Grado)
				.ThenBy(x => x.NivelEducativo)
				.ToList()
				.AsReadOnly();
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task RegistrarCurso(RegistrarCursoRequest request)
	{
		try
		{
			var resultG = Enum.TryParse<Grado>(request.Grado, true, out Grado grado);
			var resultNE = Enum.TryParse<NivelEducativo>(request.NivelEducativo, true, out NivelEducativo nivelEducativo);
			if (resultG && resultNE)
			{
				var existeCurso = await _unitOfWork.Cursos.ExisteCursoAsync(grado, nivelEducativo);
				if (existeCurso)
				{
					throw new CursoDuplicadoException();
				}

				var nuevoCurso = new Curso(request.Grado, request.NivelEducativo);
				await _unitOfWork.Cursos.AgregarAsync(nuevoCurso);

				var result = await _unitOfWork.GuardarCambiosAsync();

				_logger.LogInformation($"{result} curso registrado correctamente.", result);
			}
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task EliminarCurso(EliminarCursoRequest request)
	{
		try
		{
			await _unitOfWork.Cursos.EliminarAsync(request.CursoID);
			// TODO: Eliminar todas las materias y divisiones con este curso asociado
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } curso eliminado correctamente", result);
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion
}
