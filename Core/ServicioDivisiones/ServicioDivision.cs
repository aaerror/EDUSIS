using Core.ServicioDivisiones.DTOs.Requests;
using Core.ServicioDivisiones.DTOs.Responses;
using Core.ServicioDivisiones.Exceptions;
using Core.Shared;
using Domain.Cursantes;
using Domain.Divisiones;
using Domain.Shared;
using Microsoft.Extensions.Logging;

namespace Core.ServicioDivisiones;

internal class ServicioDivision : IServicio, IServicioDivision
{
	private readonly ILogger<ServicioDivision> _logger;
	private readonly IUnitOfWork _unitOfWork;


	public ServicioDivision(ILogger<ServicioDivision> logger, IUnitOfWork unitOfWork)
	{
		_logger = logger;
		_unitOfWork = unitOfWork;
	}

	#region Listado
	public async Task<IReadOnlyCollection<DivisionResponse>> ListarDivisionesAsync(ListarDivisionesRequest request)
	{
		try
		{
			var curso = await _unitOfWork.Cursos.BuscarPorIDAsync(request.CursoID);
			if (curso is null)
			{
				throw new InvalidOperationException("El curso no existe.");
			}

			var cicloLectivo = CicloLectivo.Crear(request.CicloLectivo);
			var divisiones = await _unitOfWork.Divisiones.DivisionesDelCursoAsync(request.CursoID);

			var respuesta = new List<DivisionResponse>();
			foreach (var division in divisiones)
			{
				var cantidadCursantes = await _unitOfWork.Cursantes.ContarCursantesDeDivisionAsync(division.Id, cicloLectivo);

				string? preceptor = null;
				if (division.Preceptor is not null)
				{
					var docente = await _unitOfWork.Docentes.BuscarPorIDAsync(division.Preceptor.Value);
					preceptor = docente?.DatosPersonales.NombreCompleto();
				}

				respuesta.Add(new DivisionResponse(
					DivisionID: division.Id,
					Descripcion: division.Descripcion,
					PreceptorID: division.Preceptor,
					Preceptor: preceptor,
					Cursantes: cantidadCursantes));
			}

			return respuesta.AsReadOnly();
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion

	#region Agregar, Eliminar
	public async Task AgregarDivisionAsync(AgregarDivisionRequest request)
	{
		try
		{
			var curso = await _unitOfWork.Cursos.BuscarPorIDAsync(request.CursoID);
			if (curso is null)
			{
				throw new InvalidOperationException("El curso no existe.");
			}

			var descripciones = await _unitOfWork.Divisiones.DescripcionesDelCursoAsync(request.CursoID);
			var nuevaDivision = Division.Siguiente(request.CursoID, descripciones);

			var existeDivision = await _unitOfWork.Divisiones.ExisteDivisionConDescripcionAsync(request.CursoID, nuevaDivision.Descripcion);
			if (existeDivision)
			{
				throw new DivisionDuplicadaException();
			}

			await _unitOfWork.Divisiones.AgregarAsync(nuevaDivision);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } división registrada correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task EliminarDivisionAsync(EliminarDivisionRequest request)
	{
		try
		{
			var curso = await _unitOfWork.Cursos.BuscarPorIDAsync(request.CursoID);
			if (curso is null)
			{
				throw new InvalidOperationException("El curso no existe.");
			}

			var division = await _unitOfWork.Divisiones.BuscarPorIDAsync(request.DivisionID);
			if (division is null)
			{
				throw new NullReferenceException("La división no existe.");
			}

			var cicloLectivo = CicloLectivo.Crear(request.CicloLectivo);
			var cantidadCursantes = await _unitOfWork.Cursantes.ContarCursantesDeDivisionAsync(division.Id, cicloLectivo);

			division.ValidarSePuedeEliminar(curso.NivelEducativo, cantidadCursantes);

			await _unitOfWork.Divisiones.EliminarAsync(division.Id);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } división eliminada correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion

	#region Preceptor
	public async Task AsignarPreceptorAsync(RegistrarPreceptorRequest request)
	{
		try
		{
			var division = await _unitOfWork.Divisiones.BuscarPorIDAsync(request.DivisionID);
			if (division is null)
			{
				throw new NullReferenceException("La división no existe.");
			}

			var existePreceptor = await _unitOfWork.Divisiones.ExistePreceptorAsignadoAsync(request.DocenteID);
			if (existePreceptor)
			{
				throw new PreceptorAsignadoException();
			}

			division.AsignarPreceptor(request.DocenteID);

			_unitOfWork.Divisiones.Modificar(division);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } preceptor asignado correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task QuitarPreceptorAsync(EliminarPreceptorRequest request)
	{
		try
		{
			var division = await _unitOfWork.Divisiones.BuscarPorIDAsync(request.DivisionID);
			if (division is null)
			{
				throw new NullReferenceException("La división no existe.");
			}

			division.QuitarPreceptor();

			_unitOfWork.Divisiones.Modificar(division);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } preceptor removido correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion
}
