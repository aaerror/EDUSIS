using Core.ServicioDocentes.DTOs.Requests;
using Core.ServicioDocentes.DTOs.Responses;
using Core.Shared.DTOs.Personas.Requests;
using Core.Shared.DTOs.Personas.Responses;
using Core.Shared;
using Domain.Docentes.Puestos;
using Domain.Docentes;
using Domain.Personas.Domicilios;
using Domain.Personas;
using Domain.Shared;
using Microsoft.Extensions.Logging;

namespace Core.ServicioDocentes;

internal class ServicioDocente : IServicio, IServicioDocente
{
	private readonly ILogger<ServicioDocente> _logger;
	private readonly IUnitOfWork _unitOfWork;


	public ServicioDocente(ILogger<ServicioDocente> logger, IUnitOfWork unitOfWork)
	{
		_logger = logger;
		_unitOfWork = unitOfWork;
	}

	private async Task<Docente> BuscarDocentePorIDAsync(Guid docenteID)
	{
		try
		{
			var docente = await _unitOfWork.Docentes.BuscarPorIDAsync(docenteID);

			if (docente is null)
			{
				throw new NullReferenceException($"No se encontró el docente.");
			}

			return docente;
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
			throw;
		}
	}

	#region Docente: Listar, Registrar, Modificar, Eliminar
	public async Task<PerfilDocenteResponse> VerPerfilDocenteAsync(DocenteIDRequest request)
	{
		try
		{
			var docente = await BuscarDocentePorIDAsync(request.DocenteID);

			var informacionPersonal = new DatosPersonalesResponse(Apellido: docente.DatosPersonales.Apellido,
																  Nombre: docente.DatosPersonales.Nombre,
																  DNI: docente.DatosPersonales.Documento,
																  Sexo: docente.DatosPersonales.Sexo.ToString(),
																  FechaNacimiento: docente.DatosPersonales.FechaNacimiento.Date,
																  Nacionalidad: docente.DatosPersonales.Nacionalidad);

			var domicilio = new DomicilioResponse(Calle: docente.Domicilio.Direccion.Calle,
												  Altura: docente.Domicilio.Direccion.Altura,
												  Vivienda: docente.Domicilio.Direccion.Vivienda.ToString(),
												  Observacion: docente.Domicilio.Direccion.Observacion,
												  Localidad: docente.Domicilio.Ubicacion.Localidad,
												  Provincia: docente.Domicilio.Ubicacion.Provincia,
												  Pais: docente.Domicilio.Ubicacion.Pais);

			var contacto = new ContactoResponse(Telefono: docente.Telefono,
												Email: docente.Email);

			return new PerfilDocenteResponse(DocenteID: docente.Id,
											InformacionPersonalDTO: informacionPersonal,
											DomicilioDTO: domicilio,
											ContactoDTO: contacto);
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
			throw;
		}
	}

	/*public LegajoDocenteResponse BuscarLegajoDocentePorDNI(string documento)
	{
		try
		{
			var docente = _unitOfWork.Docentes.Buscar(x => x.DatosPersonales.Documento == documento)
											  .FirstOrDefault();
			if (docente is null)
			{
				throw new NullReferenceException($"No se encontró ningún docente con el D.N.I. { documento }");
			}

			return new LegajoDocenteResponse(DocenteID: docente.Id,
											 NombreCompleto: docente.DatosPersonales.NombreCompleto(),
				Legajo: docente.Legajo,
				FechaAlta: docente.FechaAlta,
				FechaBaja: docente.FechaBaja,
				CUIL: docente.CUIL,
				EstaActivo: docente.EstaActivo,
				Puestos: docente.Puestos.Select(x =>
					new PuestoResponse(
						Posicion: x.Posicion,
						FechaInicio: x.FechaInicio,
						FechaFin: x.FechaFin))
				.ToList());
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
			throw;
		}
	}*/

	//TODO: ListarDocentesXCargo

	public async Task<IReadOnlyCollection<LegajoDocenteResponse>> ListarDocentesActivosAsync()
	{
		try
		{
			// TODO: Mejorar performance de la consulta
			var docentes = await _unitOfWork.Docentes.BuscarAsync(x => x.Activo);

			_logger.LogInformation($"Se encontraron { docentes.Count() } docentes activos.");

			return docentes.Select(x =>
				new LegajoDocenteResponse(
					DocenteID: x.Id,
					NombreCompleto: x.DatosPersonales.NombreCompleto(),
					DNI: x.DatosPersonales.Documento,
					CUIL: x.CUIL,
					Legajo: x.Legajo,
					FechaInicio: x.Periodo.FechaInicio,
					FechaFin: x.Periodo.FechaFin,
					Activo: x.Activo))
				.ToList();
				// Puestos: _unitOfWork.Docentes.PuestosPorDocente(x.Id)));
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"Excepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task<IReadOnlyCollection<LegajoDocenteResponse>> BuscarDocenteSegunNombreCompletoAsync(NombreCompletoRequest request)
	{
		try
		{
			var docentes = await _unitOfWork.Docentes.BuscarSegunNombreCompletoAsync(request.NombreCompleto);

			_logger.LogInformation($"Se encontraron { docentes.Count() } docentes con coincidencias en el nombre completo.");

			return docentes.Select(x =>
				new LegajoDocenteResponse(
					DocenteID: x.Id,
					NombreCompleto: x.DatosPersonales.NombreCompleto(),
					DNI: x.DatosPersonales.Documento,
					CUIL: x.CUIL,
					Legajo: x.Legajo,
					FechaInicio: x.Periodo.FechaInicio,
					FechaFin: x.Periodo.FechaFin,
					Activo: x.Activo))
				.ToList();
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
			throw;
		}
	}

	public async Task<IReadOnlyCollection<LegajoDocenteResponse>> ListarPreceptoresActivosAsync()
	{
		try
		{
			var preceptores = await _unitOfWork.Docentes.BuscarAsync(x => x.Puesto.Posicion.Equals(Posicion.Preceptor) && x.Puesto.EstaActivo());

			_logger.LogInformation($"Se encontraron { preceptores.Count() } preceptores activos.");

			return preceptores.Select(x =>
				new LegajoDocenteResponse(
					DocenteID: x.Id,
					NombreCompleto: x.DatosPersonales.NombreCompleto(),
					DNI: x.DatosPersonales.Documento,
					CUIL: x.CUIL,
					Legajo: x.Legajo,
					FechaInicio: x.Periodo.FechaInicio,
					FechaFin: x.Periodo.FechaFin,
					Activo: x.Activo))
				.ToList();
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"Excepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task<LegajoDocenteResponse> MostrarLegajoDocenteAsync(DocenteIDRequest request)
	{
		try
		{
			var docente = await _unitOfWork.Docentes.BuscarDocentePorIDConPuestosAsync(request.DocenteID);

			return new LegajoDocenteResponse(
				DocenteID: docente.Id,
				NombreCompleto: docente.DatosPersonales.NombreCompleto(),
				DNI: docente.DatosPersonales.Documento,
				CUIL: docente.CUIL,
				Legajo: docente.Legajo,
				FechaInicio: docente.Periodo.FechaInicio,
				FechaFin: docente.Periodo.FechaFin,
				Activo: docente.Activo);
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
			throw;
		}
	}

	public async Task RegistrarDocenteAsync(RegistrarDocenteRequest request)
	{
		try
		{
			if (request is null)
			{
				throw new NullReferenceException("Datos incompletos para registrar el docente.");
			}

			var esDNIInvalido = await _unitOfWork.Docentes.EsDocumentoInvalidoAsync(request.DatosPersonales.Documento);
			if (esDNIInvalido)
			{
				_logger.LogInformation($"D.N.I. del docente inválido...");
				throw new ArgumentException("El D.N.I. del docente ya se encuentra registrado.");
			}

			var esCUILInvalido = await _unitOfWork.Docentes.EsCuilInvalidoAsync(request.CUIL);
			if (esCUILInvalido)
			{
				_logger.LogInformation($"CUIL del docente inválido...");
				throw new ArgumentException("El CUIL del docente ya se encuentra registrado.");
			}
			
			var esLegajoInvalido = await _unitOfWork.Docentes.EsLegajoInvalidoAsync(request.Legajo);
			if (esLegajoInvalido)
			{
				_logger.LogInformation($"Legajo del docente inválido...");
				throw new ArgumentException("El legajo docente ya se encuentra registrado.");
			}

			var datosPersonales = DatosPersonales.Crear(apellido: request.DatosPersonales.Apellido,
														nombre: request.DatosPersonales.Nombre,
														dni: request.DatosPersonales.Documento,
														sexo: request.DatosPersonales.Sexo,
														fechaNacimiento: request.DatosPersonales.FechaNacimiento,
														nacionalidad: request.DatosPersonales.Nacionalidad);

			var domicilio = Domicilio.Crear(calle: request.Domicilio.Calle,
											altura: request.Domicilio.Altura,
											vivienda: request.Domicilio.Vivienda,
											observacion: request.Domicilio.Observacion,
											localidad: request.Domicilio.Localidad,
											provincia: request.Domicilio.Provincia,
											pais: request.Domicilio.Pais);

			var nuevoDocente = new Docente(legajo: request.Legajo,
										   cuil: request.CUIL,
										   fechaAlta: request.FechaAlta,
										   datosPersonales: datosPersonales,
										   domicilio: domicilio,
										   email: request.Contacto.Email,
										   telefono: request.Contacto.Telefono);

			nuevoDocente.AsignarCargoDocente(request.Puesto.Posicion, request.Puesto.Estado, request.Puesto.FechaInicio, request.Puesto.FechaFin);

			await _unitOfWork.Docentes.AgregarAsync(nuevoDocente);
			await _unitOfWork.GuardarCambiosAsync();
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
			throw;
		}
	}

	public async Task ActualizarContacto(CambiarContactoRequest request)
	{
		try
		{
			_logger.LogInformation($"Actualizando datos de contacto del docente...");

			var unDocente = await BuscarDocentePorIDAsync(request.PersonaID);
			unDocente.CambiarContacto(request.Email, request.Telefono);

			_unitOfWork.Docentes.Modificar(unDocente);
			await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"Datos de contactos actualizados correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
			throw;
		}
	}

	public async Task ActualizarDomicilio(CambiarDomicilioRequest request)
	{
		try
		{
			_logger.LogInformation($"Actualizando domicilio del docente...");

			var unDocente = await BuscarDocentePorIDAsync(request.PersonaID);

			var nuevoDomicilio = Domicilio.Crear(
				request.Calle,
				request.Altura,
				request.Vivienda,
				request.Observacion,
				request.Localidad,
				request.Provincia,
				request.Pais);
			unDocente.CambiarDomicilio(nuevoDomicilio);

			_unitOfWork.Docentes.Modificar(unDocente);
			await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"Domicilio actualizado correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
			throw;
		}
	}

	public async Task ActualizarSexo(CambiarSexoRequest request)
	{
		try
		{
			_logger.LogInformation($"Actualizando datos de sexo del docente...");

			var unDocente = await BuscarDocentePorIDAsync(request.PersonaID);
			unDocente.CambiarSexo(request.Apellido, request.Nombre, request.Sexo);

			_unitOfWork.Docentes.Modificar(unDocente);
			await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"Sexo actualizado correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
			throw;
		}
	}

	public async void QuitarDocente(DocenteIDRequest request)
	{
		try
		{
			var unDocente = await BuscarDocentePorIDAsync(request.DocenteID);
			unDocente.Desafectar();

			// _unitOfWork.Docentes.Eliminar(docenteID);
			_unitOfWork.Docentes.Modificar(unDocente);
			await _unitOfWork.GuardarCambiosAsync();
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion

	/*
	#region Licencias

	public LicenciaResponse RegistrarLicencia(RegistrarLicenciaDocenteRequest request)
	{
		try
		{
			Docente docente = BuscarDocentePorID(request.DocenteID);

			var licenciaRegistrada = docente.RegistrarLicencia(request.Articulo, request.Dias, request.FechaInicio, request.Observacion);

			_unitOfWork.Docentes.Modificar(docente);
			_unitOfWork.GuardarCambiosAsync();

			return new LicenciaResponse((int) licenciaRegistrada.Articulo,
										(int) licenciaRegistrada.Estado,
										licenciaRegistrada.Dias,
										licenciaRegistrada.FechaInicio,
										licenciaRegistrada.FechaFin,
										licenciaRegistrada.Observacion);
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
			throw;
		}
		
	}

	public void AprobarLicencia(EditarEstadoLicenciaRequest request)
	{
		try
		{
			Docente docente = BuscarDocentePorID(request.DocenteID);

			docente.AprobarLicencia(request.Articulo, request.Dias, request.FechaInicio);

			_unitOfWork.Docentes.Modificar(docente);
			_unitOfWork.GuardarCambiosAsync();
		}
		catch (Exception ex)
		{
			throw;
		}
	}

	public void CancelarLicencia(EditarEstadoLicenciaRequest request)
	{
		try
		{
			Docente docente = BuscarDocentePorID(request.DocenteID);

			docente.CancelarLicencia(request.Articulo, request.Dias, request.FechaInicio, request.Observacion);

			_unitOfWork.Docentes.Modificar(docente);
			_unitOfWork.GuardarCambiosAsync();
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
			throw;
		}
	}

	public LicenciaResponse ModificarLicencia(EditarLicenciaRequest request)
	{
		try
		{
			Docente docente = BuscarDocentePorID(request.DocenteID);

			Licencia unaLicencia = Licencia.Crear((Articulo) request.Licencia.Articulo, request.Licencia.Dias, request.Licencia.FechaInicio, request.Licencia.Observacion);
			unaLicencia = docente.ActualizarLicencia(unaLicencia, request.Articulo, request.Dias, request.FechaInicio, request.Observacion);

			_unitOfWork.Docentes.Modificar(docente);
			_unitOfWork.GuardarCambiosAsync();

			return new LicenciaResponse((int) unaLicencia.Articulo,
										(int) unaLicencia.Estado,
										unaLicencia.Dias,
										unaLicencia.FechaInicio,
										unaLicencia.FechaFin,
										unaLicencia.Observacion);
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
			throw;
		}
	}
	#endregion
	*/

	#region Puestos
	public async Task<IReadOnlyCollection<PuestoResponse>> ListarPuestosDocentesAsync(DocenteIDRequest request)
	{
		try
		{
			var unDocente = await _unitOfWork.Docentes.BuscarDocentePorIDConPuestosAsync(request.DocenteID);

			return unDocente.Puestos.Select(x =>
				new PuestoResponse(
					PuestoID: x.Id,
					Estado: x.Estado.ToString(),
					Posicion: x.Posicion.ToString(),
					EsEventual: x.EsEventual,
					FechaInicio: x.Periodo.FechaInicio,
					FechaFin: x.Periodo.FechaFin,
					Activo: x.EstaActivo())).ToList();
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
			throw;
		}
	}

	public async Task AgregarPuestoDocenteAsync(RegistrarPuestoDocenteRequest request)
	{
		try
		{
			var unDocente = await _unitOfWork.Docentes.BuscarDocentePorIDConPuestosAsync(request.DocenteID);
			unDocente.AsignarCargoDocente(request.Posicion, request.Estado, request.FechaInicio, request.FechaFin);

			_unitOfWork.Docentes.Modificar(unDocente);
			await _unitOfWork.GuardarCambiosAsync();
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task EditarPuestoDocenteAsync(EditarPuestoDocenteRequest request)
	{
		try
		{
			var unDocente = await _unitOfWork.Docentes.BuscarDocentePorIDConPuestosAsync(request.DocenteID);
			unDocente.ModificarCargoDocente(request.PuestoID, request.Posicion, request.FechaInicio, request.FechaFin);

			_unitOfWork.Docentes.Modificar(unDocente);
			await _unitOfWork.GuardarCambiosAsync();
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
			throw;
		}
	}

	/*public PuestoResponse CambiarPuestoDocente(NuevoPuestoDocenteRequest request)
	{
		try
		{
			Docente docente = BuscarDocentePorID(request.DocenteID);
			var puestoModificado = docente.ModificarCargoDocente((Posicion) request.Posicion, request.FechaInicio);

			_unitOfWork.Docentes.Modificar(docente);
			_unitOfWork.GuardarCambiosAsync();

			return new PuestoResponse(puestoModificado.Posicion,
									  puestoModificado.FechaInicio,
									  puestoModificado.FechaFin);
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
			throw;
		}
	}*/

	public async Task RevocarPuestoDocenteAsync(RevocarPuestoDocenteRequest request)
	{
		try
		{
			var unDocente = await _unitOfWork.Docentes.BuscarDocentePorIDConPuestosAsync(request.DocenteID);
			unDocente.RescindirCargoDocente(request.PuestoID, request.FechaFin);

			_logger.LogInformation($"\nRevocando puesto docente...\nPuestoID: {request.PuestoID}");

			_unitOfWork.Docentes.Modificar(unDocente);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } registro modificado correctamente en el docente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task EliminarPuestoDocenteAsync(EliminarPuestoDocenteRequest request)
	{
		try
		{
			var unDocente = await _unitOfWork.Docentes.BuscarDocentePorIDConPuestosAsync(request.DocenteID);
			unDocente.EliminarCargoDocente(request.PuestoID);

			_logger.LogInformation($"\nEliminando puesto docente...\nPuestoID: { request.PuestoID }");

			_unitOfWork.Docentes.Modificar(unDocente);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } registro modificado correctamente en el docente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
			throw;
		}
	}
	#endregion
}
