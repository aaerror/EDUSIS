using Core.ServicioMaterias.DTOs.Requests;
using Core.ServicioMaterias.DTOs.Responses;
using Core.ServicioMaterias.Exceptions;
using Core.Shared;
using Domain.Curriculas;
using Domain.Materias;
using Domain.Shared;
using Microsoft.Extensions.Logging;

namespace Core.ServicioMaterias;

internal class ServicioMateria : IServicio, IServicioMateria
{
	// Tope de espacios curriculares por currícula: 14 espacios es lo habitual en un año de
	// secundaria argentina (7mo a 12vo año, orientaciones incluidas).
	private const int LIMITE_ESPACIOS_CURRICULARES = 14;

	// Tope de horas cátedra semanales por currícula: 40 horas cátedra cubre una jornada semanal
	// completa (turno simple, 8 horas cátedra por día en 5 días hábiles).
	private const int LIMITE_HORAS_CATEDRA_SEMANALES = 40;

	private readonly ILogger<ServicioMateria> _logger;
	private readonly IUnitOfWork _unitOfWork;


	public ServicioMateria(ILogger<ServicioMateria> logger, IUnitOfWork unitOfWork)
	{
		_logger = logger;
		_unitOfWork = unitOfWork;
	}

	#region Búsquedas privadas
	private async Task<Curricula> BuscarCurriculaAsync(Guid unCurso, Guid unaCurricula)
	{
		var curricula = await _unitOfWork.Curriculas.BuscarCurriculaAsync(unCurso, unaCurricula);
		if (curricula is null)
		{
			throw new ArgumentNullException(nameof(unaCurricula), "No se encontró la currícula del curso.");
		}

		return curricula;
	}

	private async Task<Materia> BuscarMateriaAsync(MateriaRequest request)
	{
		await BuscarCurriculaAsync(request.CursoID, request.CurriculaID);

		var materia = await _unitOfWork.Materias.BuscarPorIDAsync(request.MateriaID);
		if (materia is null || !materia.CurriculaID.Equals(request.CurriculaID))
		{
			throw new ArgumentNullException(nameof(request.MateriaID), "No se encontró la materia de la currícula.");
		}

		return materia;
	}

	private async Task ValidarNombreNoDuplicadoAsync(NombreDuplicadoRequest request)
	{
		var existeDuplicado = request.MateriaID.HasValue
			? await _unitOfWork.Materias.ExisteNombreMateriaEnCurriculaAsync(request.CurriculaID, request.Descripcion, request.MateriaID.Value)
			: await _unitOfWork.Materias.ExisteNombreMateriaEnCurriculaAsync(request.CurriculaID, request.Descripcion);

		if (existeDuplicado)
		{
			throw new NombreMateriaDuplicadoException();
		}
	}
	#endregion

	#region Listar materias
	public async Task<IReadOnlyCollection<MateriaResponse>> ListarMateriasSegunCurriculaAsync(ListarMateriasSegunCurriculaRequest request)
	{
		try
		{
			await BuscarCurriculaAsync(request.CursoID, request.CurriculaID);

			var materias = await _unitOfWork.Materias.BuscarMateriasSegunCurriculaAsync(request.CurriculaID);

			return materias.Select(x =>
				new MateriaResponse(
					CursoID: request.CursoID,
					CurriculaID: request.CurriculaID,
					MateriaID: x.Id,
					Descripcion: x.Descripcion,
					HorasCatedra: x.HorasCatedra))
				.ToList();
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion

	#region Registrar materia
	public async Task<Guid> RegistrarMateriaAsync(RegistrarMateriaRequest request)
	{
		try
		{
			await BuscarCurriculaAsync(request.CursoID, request.CurriculaID);
			await ValidarNombreNoDuplicadoAsync(new NombreDuplicadoRequest(request.CurriculaID, null, request.Descripcion));

			var totalEspacios = await _unitOfWork.Materias.TotalEspaciosSegunCurriculaAsync(request.CurriculaID);
			if (totalEspacios + 1 > LIMITE_ESPACIOS_CURRICULARES)
			{
				throw new LimiteEspaciosCurricularesException();
			}

			var totalHoras = await _unitOfWork.Materias.TotalHorasCatedraSegunCurriculaAsync(request.CurriculaID);
			if (totalHoras + request.HorasCatedra > LIMITE_HORAS_CATEDRA_SEMANALES)
			{
				throw new LimiteHorasSemanalesException();
			}

			var materia = new Materia(request.CurriculaID, request.Descripcion, request.HorasCatedra);

			await _unitOfWork.Materias.AgregarAsync(materia);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } materia registrada correctamente.");

			return materia.Id;
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion

	#region Modificar materia
	public async Task ModificarMateriaAsync(ModificarMateriaRequest request)
	{
		try
		{
			var materia = await BuscarMateriaAsync(new MateriaRequest(request.CursoID, request.CurriculaID, request.MateriaID));

			await ValidarNombreNoDuplicadoAsync(new NombreDuplicadoRequest(request.CurriculaID, materia.Id, request.Descripcion));

			var totalHorasSinEsta = await _unitOfWork.Materias.TotalHorasCatedraSegunCurriculaAsync(request.CurriculaID) - materia.HorasCatedra;
			if (totalHorasSinEsta + request.HorasCatedra > LIMITE_HORAS_CATEDRA_SEMANALES)
			{
				throw new LimiteHorasSemanalesException();
			}

			materia.ModificarMateria(request.Descripcion, request.HorasCatedra);

			_unitOfWork.Materias.Modificar(materia);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } materia modificada correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion

	#region Eliminar materia
	public async Task EliminarMateriaAsync(EliminarMateriaRequest request)
	{
		try
		{
			var materia = await BuscarMateriaAsync(new MateriaRequest(request.CursoID, request.CurriculaID, request.MateriaID));

			materia.Eliminar();

			await _unitOfWork.Materias.EliminarAsync(materia.Id);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } materia eliminada correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion
}
