using Core.ServicioDivisiones.DTOs.Requests;
using Core.ServicioDivisiones.DTOs.Responses;
using Core.ServicioDivisiones.Exceptions;
using Core.Shared;
using Core.Shared.Seguridad;
using Domain.Cursantes;
using Domain.Divisiones;
using Domain.Shared;
using Domain.Usuarios;
using Microsoft.Extensions.Logging;

namespace Core.ServicioDivisiones;

internal class ServicioDivision : IServicio, IServicioDivision
{
	private readonly ILogger<ServicioDivision> _logger;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ISesionActual _sesion;


	public ServicioDivision(ILogger<ServicioDivision> logger, IUnitOfWork unitOfWork, ISesionActual sesion)
	{
		_sesion = sesion;
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

	#region Proponer descripción
	/// <summary>
	/// Letra que el formulario de alta precarga: la que sigue a la última división del curso. El
	/// usuario la puede cambiar, así que es una propuesta y no la descripción definitiva. Es una
	/// <strong>lectura</strong>: sin <c>Exigir</c> y sin <c>GuardarCambiosAsync</c>, porque la
	/// <see cref="Division"/> que construye <c>Division.Siguiente</c> nunca entra al repositorio.
	/// </summary>
	public async Task<string> ProponerDescripcionDivisionAsync(ProponerDescripcionRequest request)
	{
		try
		{
			var curso = await _unitOfWork.Cursos.BuscarPorIDAsync(request.CursoID);
			if (curso is null)
			{
				throw new InvalidOperationException("El curso no existe.");
			}

			var descripciones = await _unitOfWork.Divisiones.DescripcionesDelCursoAsync(request.CursoID);

			return Division.Siguiente(request.CursoID, descripciones).Descripcion;
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
			_sesion.Exigir(Permiso.GestionarCursos);

			var curso = await _unitOfWork.Cursos.BuscarPorIDAsync(request.CursoID);
			if (curso is null)
			{
				throw new InvalidOperationException("El curso no existe.");
			}

			// La letra la elige el usuario sobre la propuesta de ProponerDescripcionDivisionAsync:
			// el constructor es el que valida que sea una única letra.
			var nuevaDivision = new Division(request.CursoID, request.Descripcion);

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
			_sesion.Exigir(Permiso.GestionarCursos);

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

			// El orden de las dos guardas que siguen NO es indiferente, y la de abajo contiene a
			// esta: toda división con matrícula vigente tiene inscripciones, así que la que corra
			// primero deja muerta a la otra. La regla de negocio va adelante porque tiene el
			// mensaje que el usuario necesita cuando la división todavía tiene alumnos cursando
			// ("la matrícula alcanza para mantenerla abierta"), más preciso que "dar de baja o
			// trasladar". Invertirlas compila, no rompe ningún test y vuelve inalcanzables
			// DivisionNoEliminableException y DivisionConMatriculaSuficienteException.
			division.ValidarSePuedeEliminar(curso.NivelEducativo, cantidadCursantes);

			// Guarda de integridad, no regla de negocio: atrapa lo que la validación de arriba no
			// ve, que son las inscripciones cerradas y las de otros ciclos lectivos. Hace falta
			// porque FK_CURSANTE_DIVISION está declarada con OnDelete(DeleteBehavior.Restrict)
			// (Infrastructure/EntityConfigurations/CursantesConfiguration.cs), así que sin ella el
			// DELETE falla en SQL Server y el usuario ve el nombre de una clave ajena en vez de qué
			// tiene que hacer. Ningún test lo detectaría: el fake en memoria no tiene claves ajenas.
			var inscripciones = await _unitOfWork.Cursantes.ContarInscripcionesDeDivisionAsync(division.Id);
			if (inscripciones > 0)
			{
				throw new DivisionConInscripcionesException();
			}

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

	// Las tres operaciones de preceptor exigen GestionarPreceptores y no GestionarCursos: el
	// diagrama de Secretaría (docs/uml/) le da CU-04/05/06 al Encargado de Secretaría, mientras
	// que el alta y la baja de divisiones —acá arriba, con GestionarCursos— son de Secretaría
	// Académica. Eran el mismo permiso y concedían los dos grupos juntos.
	#region Preceptor
	public async Task AsignarPreceptorAsync(RegistrarPreceptorRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GestionarPreceptores);

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

	public async Task ModificarPreceptorAsync(ModificarPreceptorRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GestionarPreceptores);

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

			division.CambiarPreceptor(request.DocenteID);

			_unitOfWork.Divisiones.Modificar(division);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } preceptor modificado correctamente.");
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
			_sesion.Exigir(Permiso.GestionarPreceptores);

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
