using Core.ServicioCurriculas.DTOs.Requests;
using Core.ServicioCurriculas.DTOs.Responses;
using Core.Shared;
using Domain.Curriculas;
using Domain.Licencias;
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

	/*private bool EsNombreMateriaDuplicado(NombreDuplicadoRequest request)
	{
		try
		{
			var esDuplicado = _unitOfWork.Materias.NombreDuplicadoEnCurso(request.CursoID, request.MateriaID, request.Descripcion);

			return esDuplicado;
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
			throw;
		}
	}*/

	/*public CurriculaResponse BuscarCurriculaVigenteSegunCurso(CurriculaSegunCursoRequest request)
	{
		var curricula = _unitOfWork.Materias.BuscarCurriculaVigenteSegunCurso(request.CursoID);
		if (curricula is null)
		{
			throw new ArgumentNullException("No se encontró el diseño curricular vigente del curso.");
		}

		return new CurriculaResponse(curricula.Id, curricula.CursoID, curricula.FechaInicio, curricula.FechaFin);
	}*/

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
			List<CurriculaResponse> listar = new();
			var curriculas = await _unitOfWork.Curriculas.CurriculasSegunCursoAsync(request.CursoID);

			_logger.LogInformation($"Se encontraron { curriculas.Count() } curriculas en el diseño curricular.");

			return curriculas.Select(x =>
				new CurriculaResponse(
					CursoID: x.CursoID,
					CurriculaID: x.Id,
					FechaInicio: x.Periodo.FechaInicio,
					FechaFin: x.Periodo.FechaFin,
					Materias: x.Materias.Select(m =>
						new MateriaResponse(
							CursoID: request.CursoID,
							CurriculaID: x.Id,
							MateriaID: m.Id,
							Descripcion: m.Descripcion,
							HorasCatedra: m.HorasCatedra,
							CargosOcupados: m.CargosOcupados(),
							SituacionRevistaResponse: null)
						).ToList()
				)).ToList();
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
			throw ex;
		}
	}
	#endregion

	#region Materia: Listar, Registrar, Modificar, Eliminar
	private async Task<MateriaResponse> BuscarMateriaAsync(MateriaRequest request)
	{
		var curricula = await _unitOfWork.Curriculas.BuscarCurriculaAsync(request.CursoID, request.CurriculaID);

		var materia = curricula.Materias
						.Where(x => x.Id.Equals(request.MateriaID))
						.FirstOrDefault();
		if (materia is null)
		{
			throw new ArgumentNullException("La materia no se encuentra en la currícula del curso");
		}


		var docente = await _unitOfWork.Docentes.BuscarPorIDAsync(materia.Docente.DocenteID);

		return new MateriaResponse(
			CursoID: request.CursoID,
			CurriculaID: materia.CurriculaID,
			MateriaID: materia.Id,
			Descripcion: materia.Descripcion,
			HorasCatedra: materia.HorasCatedra,
			CargosOcupados: materia.CargosOcupados(),
			SituacionRevistaResponse: new SituacionRevistaResponse(
				MateriaID: materia.Id,
				SituacionRevistaID: materia.Docente.Id,
				DocenteID: materia.Docente.DocenteID,
				Docente: docente.DatosPersonales.NombreCompleto(),
				Estado: materia.Docente.Estado.Descripcion,
				Cargo: materia.Docente.Cargo.ToString(),
				FechaAlta: materia.Docente.Periodo.FechaInicio,
				FechaBaja: materia.Docente.Periodo.FechaFin,
				EnFunciones: materia.Docente.EnFunciones));
	}

	public async Task<IReadOnlyCollection<MateriaResponse>> ListarMateriasSegunCurriculaAsync(ListarMateriasSegunCurriculaRequest request)
	{
		try
		{
			List<MateriaResponse> listar = new();
			var unaCurricula = await BuscarCurriculaAsync(new CurriculaRequest(request.CursoID, request.CurriculaID));

			_logger.LogInformation($"Se encontró una materias en el diseño curricular.");

			// TODO: Revisar mejor implementación del código
			foreach (var materia in unaCurricula.Materias)
			{
				if (materia.Docente is null)
				{
					listar.Add(new MateriaResponse(
						CursoID: request.CursoID,
						CurriculaID: unaCurricula.Id,
						MateriaID: materia.Id,
						Descripcion: materia.Descripcion,
						HorasCatedra: materia.HorasCatedra,
						CargosOcupados: materia.CargosOcupados(),
						//TODO: Recuperar situación revista;
						SituacionRevistaResponse: null));
				}
				else
				{
					var docente = await _unitOfWork.Docentes.BuscarPorIDAsync(materia.Docente.DocenteID);

					listar.Add(new MateriaResponse(
						CursoID: request.CursoID,
						CurriculaID: unaCurricula.Id,
						MateriaID: materia.Id,
						Descripcion: materia.Descripcion,
						HorasCatedra: materia.HorasCatedra,
						CargosOcupados: materia.CargosOcupados(),
						SituacionRevistaResponse: new SituacionRevistaResponse(
							MateriaID: materia.Id,
							SituacionRevistaID: materia.Docente.Id,
							DocenteID: materia.Docente.DocenteID,
							Docente: docente.DatosPersonales.NombreCompleto(),
							Estado: materia.Docente.Estado.Descripcion,
							Cargo: materia.Docente.Cargo.ToString(),
							FechaAlta: materia.Docente.Periodo.FechaInicio,
							FechaBaja: materia.Docente.Periodo.FechaFin,
							EnFunciones: materia.Docente.EnFunciones)));
				}
			}
			return listar;
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task RegistrarMateria(RegistrarMateriaRequest request)
	{
		try
		{
			/*var esNombreDuplicado = EsNombreMateriaDuplicado(new NombreDuplicadoRequest(CursoID: request.CursoID,
																						MateriaID: null,
																						Descripcion: request.Descripcion));*/
			var curricula = await BuscarCurriculaAsync(new CurriculaRequest(request.CursoID, request.CurriculaID));
			curricula.AgregarMateria(request.Descripcion, request.HorasCatedra);

			_unitOfWork.Curriculas.Modificar(curricula);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } materia registrada correctamente en el diseño curricular.", result);
			
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
			throw;
		}
	}

	public async Task ModificarMateriaAsync(ModificarMateriaRequest request)
	{
		try
		{
			var curricula = await BuscarCurriculaAsync(new CurriculaRequest(request.CursoID, request.CurriculaID));
			curricula.ActualizarMateria(request.MateriaID, request.Descripcion, request.HorasCatedra);

			/*var materia = BuscarMateria(request.CursoID, request.MateriaID);
			materia.ModificarMateria(request.Descripcion, request.HorasCatedra);*/

			_unitOfWork.Curriculas.Modificar(curricula);
			var result = _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{result} materia modificada correctamente en el diseño curricular.", result);
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
			throw;
		}
	}

	public async Task EliminarMateriaAsync(EliminarMateriaRequest request)
	{
		try
		{
			var curricula = await BuscarCurriculaAsync(new CurriculaRequest(request.CursoID, request.CurriculaID));
			curricula.QuitarMateria(request.MateriaID);


			_unitOfWork.Curriculas.Modificar(curricula);
			var result = _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{result} materia eliminada del diseño curricular", result);
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
			throw;
		}
	}
	#endregion

	#region Situacion de Revista: Listar, Registrar, Modificar, Eliminar
	public async Task<IReadOnlyCollection<SituacionRevistaResponse>> ListarCargosDocenteSegunMateriaAsync(ListarCargosDocenteSegunMateriaRequest request)
	{
		try
		{
			var unaCurricula = await BuscarCurriculaAsync(new CurriculaRequest(request.CursoID, request.CurriculaID));

			var listSituacionRevistas = unaCurricula.Materias
				.Where(x => x.Id.Equals(request.MateriaID))
				.SelectMany(x => x.Docentes)
				.ToList();

			_logger.LogInformation($"Se encontraron {listSituacionRevistas.Count()} registros sobre la situacion de revista en la materia.\nMateriaID: {request.MateriaID}");

			var response = await Task.WhenAll(listSituacionRevistas.Select(async x =>
			{
				var docente = await _unitOfWork.Docentes.BuscarPorIDAsync(x.DocenteID);

				return new SituacionRevistaResponse(
					MateriaID: x.MateriaID,
					SituacionRevistaID: x.Id,
					DocenteID: x.DocenteID,
					Docente: docente.DatosPersonales.NombreCompleto(),
					Estado: x.Estado.Descripcion,
					Cargo: x.Cargo.ToString(),
					FechaAlta: x.Periodo.FechaInicio,
					FechaBaja: x.Periodo.FechaFin,
					EnFunciones: x.EnFunciones);
			}));

			return response;
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
			throw;
		}
	}

	public async Task RegistrarDocenteEnMateriaAsync(RegistrarDocenteEnMateriaRequest request)
	{
		_logger.LogInformation($"Registrando un docente, { request.DocenteID } en la materia, { request.MateriaID }...");
		try
		{
			var unaCurricula = await BuscarCurriculaAsync(new CurriculaRequest(request.CursoID, request.CurriculaID));
			_logger.LogInformation("Asignando docente en la materia...");

			unaCurricula.AsignarDocenteEnMateria(request.MateriaID, request.DocenteID, request.Cargo, request.FechaAlta, request.FechaBaja, request.EnFunciones);
			_logger.LogInformation($"Actualizando cambios...");

			_unitOfWork.Curriculas.Modificar(unaCurricula);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"Cambios realizados: { result }");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"Excepción generada: { ex.Message }");
			throw;
		}
	}

	public async Task RelevarDocenteDeFuncionesEnMateriaAsync(RelevarDocenteDeAulaRequest request)
	{
		_logger.LogInformation($"Relevar docente de funciones...\n{ request }");
		try
		{
			var unaCurricula = await BuscarCurriculaAsync(new CurriculaRequest(request.CursoID, request.CurriculaID));
			unaCurricula.RelevarDocenteDeMateria(request.MateriaID);

			_unitOfWork.Curriculas.Modificar(unaCurricula);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"Cambios realizados: {result}");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
			throw;
		}
	}

	public async Task RescindirCargoDocenteDeMateriaAsync(RescindirCargoDocenteRequest request)
	{
		_logger.LogInformation($"Rescindiendo cargo docente...\n{ request }");
		try
		{
			var unaCurricula = await BuscarCurriculaAsync(new CurriculaRequest(request.CursoID, request.CurriculaID));
			unaCurricula.RescindirDocenteDeMateria(request.MateriaID, request.SituacionRevistaID);

			_unitOfWork.Curriculas.Modificar(unaCurricula);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"Cambios realizados: {result}");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
			throw;
		}
	}

	public async Task EstablecerDocenteDeAulaAsync(EstablecerDocenteDeAulaRequest request)
	{
		try
		{
			var unaCurricula = await BuscarCurriculaAsync(new CurriculaRequest(request.CursoID, request.CurriculaID));
			unaCurricula.EstablecerDocenteEnFuncionesEnMateria(request.MateriaID, request.SituacionRevistaID);

			_unitOfWork.Curriculas.Modificar(unaCurricula);
			var result = await _unitOfWork.GuardarCambiosAsync();
			
			_logger.LogInformation($"Cambios realizados: { result }");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task EliminarCargoDocenteAsync(EliminarCargoDocenteRequest request)
	{
		_logger.LogInformation($"Eliminando cargo docente del materia...\n{ request }");
		try
		{
			var unaCurricula = await BuscarCurriculaAsync(new CurriculaRequest(request.CursoID, request.CurriculaID));
			unaCurricula.EliminarCargoDocenteDeMateria(request.MateriaID, request.SituacionRevistaID);

			_unitOfWork.Curriculas.Modificar(unaCurricula);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"Cambios realizados: { result }");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion
}
