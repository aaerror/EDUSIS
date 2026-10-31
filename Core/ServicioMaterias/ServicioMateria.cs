using Core.ServicioMaterias.DTOs.Requests;
using Core.ServicioMaterias.DTOs.Responses;
using Core.ServicioMaterias.Exceptions;
using Core.Shared;
using Core.Shared.Seguridad;
using Domain.Curriculas;
using Domain.Curriculas.Exceptions;
using Domain.Materias;
using Domain.Materias.Exceptions;
using Domain.Shared;
using Domain.Usuarios;
using Microsoft.Extensions.Logging;

namespace Core.ServicioMaterias;

internal class ServicioMateria : IServicio, IServicioMateria
{
	private readonly ILogger<ServicioMateria> _logger;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ISesionActual _sesion;


	public ServicioMateria(ILogger<ServicioMateria> logger, IUnitOfWork unitOfWork, ISesionActual sesion)
	{
		_sesion = sesion;
		_logger = logger;
		_unitOfWork = unitOfWork;
	}

	#region Búsquedas privadas
	private async Task<Curricula> BuscarCurriculaAsync(Guid unCurso, Guid unaCurricula)
	{
		var curricula = await _unitOfWork.Curriculas.BuscarCurriculaAsync(unCurso, unaCurricula);
		if (curricula is null)
		{
			throw new CurriculaNoEncontradaException(nameof(unaCurricula));
		}

		return curricula;
	}

	/// <summary>
	/// Guarda de los casos de uso que MODIFICAN el contenido de la currícula. Una currícula
	/// desafectada es el registro histórico de un diseño que ya no rige: cambiarle las materias
	/// reescribe el pasado. El listado no pasa por acá, porque consultar el histórico es legítimo.
	/// </summary>
	private async Task<Curricula> BuscarCurriculaVigenteAsync(Guid unCurso, Guid unaCurricula)
	{
		var curricula = await BuscarCurriculaAsync(unCurso, unaCurricula);
		if (!curricula.EstaVigente())
		{
			throw new CurriculaNoVigenteException(nameof(unaCurricula));
		}

		return curricula;
	}

	private async Task<Materia> BuscarMateriaAsync(MateriaRequest request)
	{
		await BuscarCurriculaVigenteAsync(request.CursoID, request.CurriculaID);

		var materia = await _unitOfWork.Materias.BuscarPorIDAsync(request.MateriaID);
		if (materia is null || !materia.CurriculaID.Equals(request.CurriculaID))
		{
			throw new MateriaNoEncontradaException(nameof(request.MateriaID));
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
			_sesion.Exigir(Permiso.GestionarCurriculas);

			await BuscarCurriculaVigenteAsync(request.CursoID, request.CurriculaID);
			await ValidarNombreNoDuplicadoAsync(new NombreDuplicadoRequest(request.CurriculaID, null, request.Descripcion));

			// Los totales se calculan acá porque requieren consultar el repositorio; la REGLA sobre
			// esos totales es del dominio y la decide TopesCurriculares.
			var totalEspacios = await _unitOfWork.Materias.TotalEspaciosSegunCurriculaAsync(request.CurriculaID);
			var totalHoras = await _unitOfWork.Materias.TotalHorasCatedraSegunCurriculaAsync(request.CurriculaID);

			TopesCurriculares.ExigirAltaPosible(totalEspacios, totalHoras, request.HorasCatedra);

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
			_sesion.Exigir(Permiso.GestionarCurriculas);

			var materia = await BuscarMateriaAsync(new MateriaRequest(request.CursoID, request.CurriculaID, request.MateriaID));

			await ValidarNombreNoDuplicadoAsync(new NombreDuplicadoRequest(request.CurriculaID, materia.Id, request.Descripcion));

			var totalHorasSinEsta = await _unitOfWork.Materias.TotalHorasCatedraSegunCurriculaAsync(request.CurriculaID) - materia.HorasCatedra;

			TopesCurriculares.ExigirCargaHorariaPosible(totalHorasSinEsta, request.HorasCatedra);

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
			_sesion.Exigir(Permiso.GestionarCurriculas);

			var materia = await BuscarMateriaAsync(new MateriaRequest(request.CursoID, request.CurriculaID, request.MateriaID));

			// El borrado va en dos guardados y el orden NO es decorativo. Materia.Eliminar() sólo
			// encola MateriaEliminadaEvent; el primer GuardarCambiosAsync lo despacha (los eventos se
			// publican ANTES de SaveChangesAsync) y MateriaEliminadaEventHandler limpia las cátedras
			// huérfanas con la materia TODAVÍA viva. Si se marcara el borrado antes —con
			// Materias.EliminarAsync, como estaba— el guardado intermedio del handler vería la relación
			// requerida Materia→Catedra rota, y como Catedra.MateriaID no admite null y la FK está en
			// Restrict, EF aborta con "The association between entity types 'Materia' and 'Catedra'
			// has been severed".
			//
			// Son dos transacciones deliberadamente, una por agregado: Catedra se consolida por su
			// cuenta en el handler y Materia acá. Es consistencia eventual, no atomicidad. Si el
			// segundo guardado falla, las cátedras ya están borradas y la materia queda sin ellas:
			// un estado válido en la BD (nadie referencia una fila inexistente) pero incompleto
			// respecto de la intención, y sin compensación automática — reintentar el borrado lo cierra.
			materia.Eliminar();
			await _unitOfWork.GuardarCambiosAsync();

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
