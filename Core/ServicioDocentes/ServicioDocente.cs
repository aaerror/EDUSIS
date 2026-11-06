using Core.ServicioDocentes.DTOs.Requests;
using Core.ServicioDocentes.DTOs.Responses;
using Core.Shared.DTOs.Personas.Requests;
using Core.Shared.DTOs.Personas.Responses;
using Core.Shared;
using Domain.Docentes.Puestos;
using Domain.Docentes;
using Domain.Docentes.Exceptions;
using Domain.Personas.Domicilios;
using Domain.Personas;
using Domain.Shared;
using Microsoft.Extensions.Logging;
using Core.Shared.Seguridad;
using Domain.Usuarios;
using Domain.Usuarios.Exceptions;

namespace Core.ServicioDocentes;

internal class ServicioDocente : IServicio, IServicioDocente
{
	private readonly ILogger<ServicioDocente> _logger;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ISesionActual _sesion;


	public ServicioDocente(ILogger<ServicioDocente> logger, IUnitOfWork unitOfWork, ISesionActual sesion)
	{
		_sesion = sesion;
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

			var informacionPersonal = new DatosPersonalesResponse(
				Apellido: docente.DatosPersonales.Apellido,
				Nombre: docente.DatosPersonales.Nombre,
				DNI: docente.DatosPersonales.Documento,
				Sexo: docente.DatosPersonales.Sexo.ToString(),
				FechaNacimiento: docente.DatosPersonales.FechaNacimiento.Date,
				Nacionalidad: docente.DatosPersonales.Nacionalidad);

			var domicilio = new DomicilioResponse(
				Calle: docente.Domicilio.Direccion.Calle,
				Altura: docente.Domicilio.Direccion.Altura,
				Vivienda: docente.Domicilio.Direccion.Vivienda.ToString(),
				Observacion: docente.Domicilio.Direccion.Observacion,
				Localidad: docente.Domicilio.Ubicacion.Localidad,
				Provincia: docente.Domicilio.Ubicacion.Provincia,
				Pais: docente.Domicilio.Ubicacion.Pais);

			var contacto = new ContactoResponse(
				Telefono: docente.Telefono,
				Email: docente.Email);

			return new PerfilDocenteResponse(
				DocenteID: docente.Id,
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
			var docentes = await _unitOfWork.Docentes.BuscarActivosAsync();

			_logger.LogInformation($"Se encontraron { docentes.Count() } docentes activos.");

			return docentes
				.Select(x =>
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
			var docentes = await _unitOfWork.Docentes
				.BuscarSegunNombreCompletoAsync(request.NombreCompleto);

			var log = $"Se encontraron {docentes.Count()} docentes con coincidencias en el nombre completo.";	
			_logger.LogInformation(log);

			return docentes
				.Select(x =>
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
			var preceptores = await _unitOfWork.Docentes
				.BuscarSegunPosicionAsync(Posicion.Preceptor);

			_logger.LogInformation($"Se encontraron { preceptores.Count() } preceptores activos.");

			return preceptores
				.Select(x =>
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
			var docente = await _unitOfWork.Docentes
				.BuscarPorIDAsync(request.DocenteID);

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
			_sesion.Exigir(Permiso.GestionarDocentes);

			if (request is null)
			{
				throw new NullReferenceException("Datos incompletos para registrar el docente.");
			}

			var esDNIInvalido = await _unitOfWork.Docentes
				.EsDocumentoInvalidoAsync(request.DatosPersonales.Documento);
			if (esDNIInvalido)
			{
				_logger.LogInformation($"D.N.I. del docente inválido...");
				throw new ArgumentException("El D.N.I. del docente ya se encuentra registrado.");
			}

			var esCUILInvalido = await _unitOfWork.Docentes
				.EsCuilInvalidoAsync(request.CUIL);
			if (esCUILInvalido)
			{
				_logger.LogInformation($"CUIL del docente inválido...");
				throw new ArgumentException("El CUIL del docente ya se encuentra registrado.");
			}
			
			var esLegajoInvalido = await _unitOfWork.Docentes
				.EsLegajoInvalidoAsync(request.Legajo);
			if (esLegajoInvalido)
			{
				_logger.LogInformation($"Legajo del docente inválido...");
				throw new ArgumentException("El legajo docente ya se encuentra registrado.");
			}

			var datosPersonales = DatosPersonales.Crear(
				apellido: request.DatosPersonales.Apellido,
				nombre: request.DatosPersonales.Nombre,
				dni: request.DatosPersonales.Documento,
				sexo: request.DatosPersonales.Sexo,
				fechaNacimiento: request.DatosPersonales.FechaNacimiento,
				nacionalidad: request.DatosPersonales.Nacionalidad);

			var domicilio = Domicilio.Crear(
				calle: request.Domicilio.Calle,
				altura: request.Domicilio.Altura,
				vivienda: request.Domicilio.Vivienda,
				observacion: request.Domicilio.Observacion,
				localidad: request.Domicilio.Localidad,
				provincia: request.Domicilio.Provincia,
				pais: request.Domicilio.Pais);

			var nuevoDocente = new Docente(
				legajo: request.Legajo,
				cuil: request.CUIL,
				fechaAlta: request.FechaAlta,
				datosPersonales: datosPersonales,
				domicilio: domicilio,
				email: request.Contacto.Email,
				telefono: request.Contacto.Telefono);

			nuevoDocente.AsignarCargoDocente(
				request.Puesto.Posicion,
				request.Puesto.Estado,
				request.Puesto.FechaInicio,
				request.Puesto.FechaFin);

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
			_sesion.Exigir(Permiso.GestionarDocentes);

			_logger.LogInformation($"Actualizando datos de contacto del docente...");

			var unDocente = await BuscarDocentePorIDAsync(request.PersonaID);
			unDocente.CambiarEmail(request.Email);
			unDocente.CambiarTelefono(request.Telefono);

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
			_sesion.Exigir(Permiso.GestionarDocentes);

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
			_sesion.Exigir(Permiso.GestionarDocentes);

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

	/// <summary>
	/// Da de baja al docente conservando su legajo: cierra su período y lo marca inactivo. No borra
	/// nada.
	/// </summary>
	/// <remarks>
	/// Es exactamente lo que hacía <c>QuitarDocenteAsync</c>, con el nombre que le corresponde. Ese
	/// método decía "quitar" y tenía el borrado real comentado en la línea de al lado
	/// (<c>// _unitOfWork.Docentes.Eliminar(docenteID);</c>): un soft delete con nombre de borrado,
	/// que es el defecto que CORE-24 relevó. Se partió en dos el 2026-11-06 siguiendo la regla de que
	/// <em>eliminar</em> sea siempre borrado físico y que el soft delete lleve una palabra de negocio
	/// propia — acá, <em>desafectar</em>, que es la que ya usaba el dominio
	/// (<c>Docente.Desafectar()</c>, con su <c>DocenteDesafectadoDomainEvent</c>).
	/// </remarks>
	public async Task DesafectarDocenteAsync(DocenteIDRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GestionarDocentes);

			var unDocente = await BuscarDocentePorIDAsync(request.DocenteID);
			unDocente.Desafectar();

			_unitOfWork.Docentes.Modificar(unDocente);
			await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"Docente { unDocente.Id } desafectado.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	/// <summary>
	/// Elimina el legajo del docente y lo que es suyo: sus licencias, su usuario y sus puestos.
	/// Rechaza si tiene designaciones vigentes en cátedras.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Las dos guardas corren antes de abrir la transacción</strong> y responden a la
	/// distinción que gobierna todo este caso de uso: cuando se borra un alumno se destruye SU
	/// historia; cuando se borra un docente se destruye la de OTROS.
	/// </para>
	/// <para>
	/// <strong>Designaciones vigentes → rechaza.</strong> La situación de revista es la designación
	/// del docente en una cátedra, y la cátedra pertenece a otra materia, otro agregado. Si
	/// cascadeara, una cátedra perdería su ocupante en silencio sin pasar por una sola regla de
	/// <c>Catedra</c> —que valida ocupante único en <c>Designar</c>—. Es la misma forma de guarda que
	/// usa el curso con sus divisiones: antes de borrar esto, desarmá lo que cuelga de él.
	/// </para>
	/// <para>
	/// <strong>Último administrador → rechaza.</strong> Ver <c>UltimoAdministradorException</c>: sin
	/// ninguna cuenta con <c>AdministrarUsuarios</c> y <c>AsignarRoles</c>, el sistema queda sin forma
	/// de recuperar su propia administración desde la aplicación.
	/// </para>
	/// <para>
	/// Lo que sí cascadea es lo que no tiene vida propia sin el docente: sus licencias, su usuario
	/// —con sus roles, por la cascada de <c>usuario_rol</c>— y sus puestos, que caen solos porque
	/// <c>FK_PUESTO_DOCENTE</c> es la única <c>Cascade</c> de las seis claves ajenas que apuntan a
	/// <c>docente</c>.
	/// </para>
	/// <para>
	/// Las divisiones <strong>no</strong> se borran: se les vacía el cargo de preceptor con
	/// <c>Division.QuitarPreceptor()</c>, que ya existe con su invariante. El cargo vacante es un
	/// estado que el dominio modela (<c>EstaCargoPreceptorVacante</c>), así que la división queda
	/// consistente y operativa, nada más que sin preceptor.
	/// </para>
	/// <para>
	/// Las planillas que el docente firmó tampoco se tocan: conservan su <c>PreceptorID</c> como
	/// firma histórica. Es posible porque <c>FK_PLANILLA_ASISTENCIA_DOCENTE</c> se quitó por migración
	/// —ver <c>PlanillasAsistenciaConfiguration</c>—: borrar el legajo no desfirma la planilla.
	/// </para>
	/// </remarks>
	public async Task EliminarDocenteAsync(DocenteIDRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GestionarDocentes);

			var unDocente = await BuscarDocentePorIDAsync(request.DocenteID);

			var catedras = await _unitOfWork.Catedras.CatedrasSegunDocenteAsync(unDocente.Id);
			var vigentes = catedras.SelectMany(x => x.SituacionesRevista)
								   .Count(x => x.DocenteID.Equals(unDocente.Id) && x.EsCargoVigente());
			if (vigentes > 0)
			{
				throw new DocenteConDesignacionesVigentesException(unDocente.Legajo);
			}

			var usuario = await _unitOfWork.Usuarios.BuscarPorDocenteAsync(unDocente.Id);
			if (usuario is not null && usuario.Roles.Any(x => x.Id == Rol.Admin.Id))
			{
				var administradores = await _unitOfWork.Usuarios.ContarUsuariosConRolAsync(Rol.Admin.Id);
				if (administradores <= 1)
				{
					throw new UltimoAdministradorException(unDocente.Legajo);
				}
			}

			await _unitOfWork.BeginTransactionAsync();

			try
			{
				// 1. El cargo de preceptor de cada división que tenga a cargo. No se borra la
				//    división: queda con el cargo vacante, que es un estado legítimo del dominio.
				var divisiones = await _unitOfWork.Divisiones.BuscarPorPreceptorAsync(unDocente.Id);
				foreach (var division in divisiones)
				{
					division.QuitarPreceptor();
				}

				_unitOfWork.Divisiones.ModificarRango(divisiones);
				await _unitOfWork.GuardarCambiosAsync();

				// 2. Su usuario. Los roles caen por la cascada de usuario_rol.
				if (usuario is not null)
				{
					await _unitOfWork.Usuarios.EliminarAsync(usuario.Id);
					await _unitOfWork.GuardarCambiosAsync();
				}

				// 3. Sus licencias.
				var licencias = await _unitOfWork.Licencias.BuscarLicenciasDeDocenteAsync(unDocente.Id);
				_unitOfWork.Licencias.EliminarRango(licencias);
				await _unitOfWork.GuardarCambiosAsync();

				// 4. El legajo. Sus puestos caen por FK_PUESTO_DOCENTE, que ya es Cascade.
				await _unitOfWork.Docentes.EliminarAsync(unDocente.Id);
				await _unitOfWork.GuardarCambiosAsync();

				await _unitOfWork.CommitTransactionAsync();

				_logger.LogInformation($"Docente { unDocente.Id } eliminado: { divisiones.Count } divisiones quedaron sin preceptor, { licencias.Count } licencias borradas.");
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

	/// <summary>
	/// Qué destruiría eliminar este docente, y qué lo impide si algo lo impide.
	/// </summary>
	/// <remarks>
	/// Lectura <strong>con</strong> guarda de permiso, misma asimetría deliberada frente a CORE-16
	/// que las de alumno, división y curso: existe sólo para servir al borrado.
	///
	/// <strong>No</strong> informa cuántas planillas firmó, y es una decisión. Contarlas exigiría una
	/// consulta que filtre <c>planilla_asistencia</c> por <c>preceptor_id</c>, y esa columna se quedó
	/// sin índice cuando la migración se llevó <c>FK_PLANILLA_ASISTENCIA_DOCENTE</c>: sería un recorrido
	/// de tabla entera para una cifra decorativa de un diálogo. Que las planillas conservan la firma es
	/// un hecho fijo de la operación, así que el diálogo lo dice sin número.
	/// </remarks>
	public async Task<ImpactoEliminacionDocenteResponse> ConsultarImpactoEliminacionDocenteAsync(DocenteIDRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GestionarDocentes);

			var unDocente = await BuscarDocentePorIDAsync(request.DocenteID);

			var catedras = await _unitOfWork.Catedras.CatedrasSegunDocenteAsync(unDocente.Id);
			var designaciones = catedras.SelectMany(x => x.SituacionesRevista)
										.Where(x => x.DocenteID.Equals(unDocente.Id))
										.ToList();

			var divisiones = await _unitOfWork.Divisiones.BuscarPorPreceptorAsync(unDocente.Id);
			var licencias = await _unitOfWork.Licencias.BuscarLicenciasDeDocenteAsync(unDocente.Id);
			var usuario = await _unitOfWork.Usuarios.BuscarPorDocenteAsync(unDocente.Id);

			var esElUltimoAdministrador = usuario is not null
									   && usuario.Roles.Any(x => x.Id == Rol.Admin.Id)
									   && await _unitOfWork.Usuarios.ContarUsuariosConRolAsync(Rol.Admin.Id) <= 1;

			return new ImpactoEliminacionDocenteResponse(
				DesignacionesVigentes: designaciones.Count(x => x.EsCargoVigente()),
				DesignacionesCerradas: designaciones.Count(x => !x.EsCargoVigente()),
				DivisionesQueQuedanSinPreceptor: divisiones.Count,
				Licencias: licencias.Count,
				TieneUsuario: usuario is not null,
				EsElUltimoAdministrador: esElUltimoAdministrador);
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
			var unDocente = await _unitOfWork.Docentes
				.BuscarPorIDAsync(request.DocenteID);

			return unDocente.Puestos
				.Select(x =>
					new PuestoResponse(
						PuestoID: x.Id,
						Estado: x.Estado.ToString(),
						Posicion: x.Posicion.ToString(),
						EsEventual: x.EsEventual,
						FechaInicio: x.Periodo.FechaInicio,
						FechaFin: x.Periodo.FechaFin,
						Activo: x.EstaActivo()))
				.ToList();
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
			_sesion.Exigir(Permiso.GestionarDocentes);

			var unDocente = await _unitOfWork.Docentes
				.BuscarPorIDAsync(request.DocenteID);
			unDocente.AsignarCargoDocente(
				request.Posicion,
				request.Estado,
				request.FechaInicio,
				request.FechaFin);

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
			_sesion.Exigir(Permiso.GestionarDocentes);

			var unDocente = await _unitOfWork.Docentes
				.BuscarPorIDAsync(request.DocenteID);
			unDocente.ModificarCargoDocente(
				request.PuestoID,
				request.Posicion,
				request.FechaInicio,
				request.FechaFin);

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
			_sesion.Exigir(Permiso.GestionarDocentes);

			var unDocente = await _unitOfWork.Docentes
				.BuscarPorIDAsync(request.DocenteID);
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
			_sesion.Exigir(Permiso.GestionarDocentes);

			var unDocente = await _unitOfWork.Docentes
				.BuscarPorIDAsync(request.DocenteID);
			unDocente.EliminarCargoDocente(request.PuestoID);

			var log = $"\nEliminando puesto docente...\nPuestoID: {request.PuestoID}";
			_logger.LogInformation(log);

			_unitOfWork.Docentes.Modificar(unDocente);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{result} registro modificado correctamente en el docente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
			throw;
		}
	}
	#endregion
}