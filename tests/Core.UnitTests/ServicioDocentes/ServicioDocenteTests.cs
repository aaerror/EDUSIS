using Core.ServicioDocentes;
using Core.ServicioDocentes.DTOs.Requests;
using Core.Shared.DTOs.Personas.Requests;
using Core.UnitTests.Infraestructura;
using Domain.Catedras.SituacionesRevista;
using Domain.Docentes.Puestos;
using Domain.Usuarios;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Core.UnitTests.ServicioDocentes;

/// <summary>
/// <see cref="IServicioDocente"/>: los métodos públicos (los de licencias quedan comentados
/// en la interfaz, FR-013) con camino feliz y de error. Las dos bajas —<c>DesafectarDocenteAsync</c>,
/// que cierra el legajo, y <c>EliminarDocenteAsync</c>, que lo borra— salieron de partir el viejo
/// <c>QuitarDocenteAsync</c>, que decía "quitar" y desafectaba (CORE-24). Las dos devuelven
/// <see cref="Task"/> desde H-018 y propagan sus excepciones con normalidad.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class ServicioDocenteTests
{
	private readonly HostDeServicios _host = new();
	private readonly IServicioDocente _servicio;

	public ServicioDocenteTests()
	{
		_servicio = _host.Resolver<IServicioDocente>();
	}

	private Domain.Docentes.Docente SembrarDocente(bool conPuesto = false)
	{
		var builder = new DocenteBuilder();
		if (conPuesto)
		{
			builder.ConPuesto();
		}

		var docente = builder.Build();
		_host.UnidadDeTrabajo.DocentesFake.Sembrar(docente);
		return docente;
	}

	private static RegistrarDocenteRequest RegistroValido(string dni = "28444555", string legajo = "100200", string cuil = "20284445551") =>
		new(Legajo: legajo,
			CUIL: cuil,
			FechaAlta: DateTime.Today.AddYears(-1),
			DatosPersonales: new RegistrarDatosPersonalesRequest("Suárez", "Diego", dni, "Masculino", DateTime.Today.AddYears(-40), "Argentina"),
			Domicilio: new RegistrarDomicilioRequest("Rivadavia", "455", "Casa", "", "San Miguel de Tucumán", "Tucumán", "Argentina"),
			Contacto: new RegistrarContactoRequest("3814765432", "diego.suarez@correo.com"),
			Puesto: new RegistrarPuestoDocenteRequest(Guid.Empty, "Profesor", "Pendiente", DateTime.Today, null));

	#region Consultas
	[Fact]
	public async Task VerPerfilDocenteAsync_devuelve_el_perfil_del_docente_sembrado()
	{
		var docente = SembrarDocente();

		var response = await _servicio.VerPerfilDocenteAsync(new DocenteIDRequest(docente.Id));

		response.DocenteID.ShouldBe(docente.Id);
		response.InformacionPersonalDTO.DNI.ShouldBe(docente.DatosPersonales.Documento);
	}

	[Fact]
	public async Task VerPerfilDocenteAsync_con_un_Id_inexistente_lanza_NullReferenceException()
	{
		await Should.ThrowAsync<NullReferenceException>(
			() => _servicio.VerPerfilDocenteAsync(new DocenteIDRequest(Guid.NewGuid())));
	}

	[Fact]
	public async Task ListarDocentesActivosAsync_devuelve_solo_los_docentes_activos()
	{
		SembrarDocente();
		SembrarDocente();

		var docentes = await _servicio.ListarDocentesActivosAsync();

		docentes.Count.ShouldBe(2);
		docentes.ShouldAllBe(x => x.Activo);
	}

	[Fact]
	public async Task ListarDocentesActivosAsync_sin_docentes_devuelve_una_coleccion_vacia()
	{
		var docentes = await _servicio.ListarDocentesActivosAsync();

		docentes.ShouldBeEmpty();
	}

	[Fact]
	public async Task BuscarDocenteSegunNombreCompletoAsync_encuentra_por_coincidencia_parcial()
	{
		var docente = SembrarDocente();

		var docentes = await _servicio.BuscarDocenteSegunNombreCompletoAsync(
			new NombreCompletoRequest(docente.DatosPersonales.Apellido));

		docentes.ShouldHaveSingleItem();
		docentes.First().DocenteID.ShouldBe(docente.Id);
	}

	[Fact]
	public async Task BuscarDocenteSegunNombreCompletoAsync_sin_coincidencias_devuelve_coleccion_vacia()
	{
		SembrarDocente();

		var docentes = await _servicio.BuscarDocenteSegunNombreCompletoAsync(new NombreCompletoRequest("Inexistente"));

		docentes.ShouldBeEmpty();
	}

	[Fact]
	public async Task MostrarLegajoDocenteAsync_devuelve_el_legajo_del_docente()
	{
		var docente = SembrarDocente();

		var legajo = await _servicio.MostrarLegajoDocenteAsync(new DocenteIDRequest(docente.Id));

		legajo.DocenteID.ShouldBe(docente.Id);
		legajo.Legajo.ShouldBe(docente.Legajo);
	}

	[Fact]
	public async Task MostrarLegajoDocenteAsync_con_un_Id_inexistente_lanza_NullReferenceException()
	{
		await Should.ThrowAsync<NullReferenceException>(
			() => _servicio.MostrarLegajoDocenteAsync(new DocenteIDRequest(Guid.NewGuid())));
	}

	[Fact]
	public async Task ListarPreceptoresActivosAsync_devuelve_solo_los_docentes_con_puesto_de_preceptor_activo()
	{
		var preceptor = new DocenteBuilder().ConPuesto("Preceptor", "Activo").Build();
		var profesor = new DocenteBuilder().ConPuesto("Profesor", "Activo").Build();
		_host.UnidadDeTrabajo.DocentesFake.Sembrar(preceptor);
		_host.UnidadDeTrabajo.DocentesFake.Sembrar(profesor);

		var preceptores = await _servicio.ListarPreceptoresActivosAsync();

		preceptores.ShouldHaveSingleItem();
		preceptores.First().DocenteID.ShouldBe(preceptor.Id);
	}
	#endregion

	#region Alta
	[Fact]
	public async Task RegistrarDocenteAsync_da_de_alta_al_docente_con_su_cargo_y_guarda()
	{
		await _servicio.RegistrarDocenteAsync(RegistroValido());

		var docente = _host.UnidadDeTrabajo.DocentesFake.Elementos.ShouldHaveSingleItem();
		docente.Puestos.ShouldHaveSingleItem();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task RegistrarDocenteAsync_rechaza_un_DNI_ya_registrado()
	{
		var existente = SembrarDocente();

		var request = RegistroValido() with
		{
			DatosPersonales = RegistroValido().DatosPersonales with { Documento = existente.DatosPersonales.Documento }
		};

		await Should.ThrowAsync<ArgumentException>(() => _servicio.RegistrarDocenteAsync(request));
	}
	#endregion

	#region Modificaciones de persona
	[Fact]
	public async Task ActualizarContacto_cambia_email_y_telefono_y_guarda()
	{
		var docente = SembrarDocente();

		await _servicio.ActualizarContacto(new CambiarContactoRequest(docente.Id, "3815550000", "docente.nuevo@correo.com"));

		docente.Email.ShouldBe("docente.nuevo@correo.com");
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task ActualizarContacto_con_un_Id_inexistente_lanza_NullReferenceException()
	{
		await Should.ThrowAsync<NullReferenceException>(() => _servicio.ActualizarContacto(
			new CambiarContactoRequest(Guid.NewGuid(), "3815550000", "docente.nuevo@correo.com")));
	}

	[Fact]
	public async Task ActualizarDomicilio_reemplaza_el_domicilio_y_guarda()
	{
		var docente = SembrarDocente();

		await _servicio.ActualizarDomicilio(new CambiarDomicilioRequest(
			docente.Id, "Muñecas", "120", "Departamento", "1° A", "Yerba Buena", "Tucumán", "Argentina"));

		docente.Domicilio.Direccion.Calle.ShouldBe("Muñecas");
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task ActualizarDomicilio_con_un_Id_inexistente_lanza_NullReferenceException()
	{
		await Should.ThrowAsync<NullReferenceException>(() => _servicio.ActualizarDomicilio(
			new CambiarDomicilioRequest(Guid.NewGuid(), "Muñecas", "120", "Casa", "", "Yerba Buena", "Tucumán", "Argentina")));
	}

	[Fact]
	public async Task ActualizarSexo_actualiza_los_datos_personales_y_guarda()
	{
		var docente = SembrarDocente();

		await _servicio.ActualizarSexo(new CambiarSexoRequest(docente.Id, "Ñáñez", "José María", "Masculino"));

		docente.DatosPersonales.Sexo.ShouldBe(Domain.Personas.Sexo.Masculino);
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task ActualizarSexo_con_un_Id_inexistente_lanza_NullReferenceException()
	{
		await Should.ThrowAsync<NullReferenceException>(() => _servicio.ActualizarSexo(
			new CambiarSexoRequest(Guid.NewGuid(), "Ñáñez", "José María", "Masculino")));
	}
	#endregion

	#region Baja institucional
	// QuitarDocenteAsync se partió en dos el 2026-11-06 (CORE-24): decía "quitar" y desafectaba, con
	// el borrado real comentado en la línea de al lado. Estas dos pruebas pasaron a nombre nuevo sin
	// cambiar lo que afirman, porque DesafectarDocenteAsync hace exactamente lo que hacía.
	[Fact]
	public async Task DesafectarDocenteAsync_completa_la_baja_del_docente_sembrado()
	{
		var docente = SembrarDocente();

		await _servicio.DesafectarDocenteAsync(new DocenteIDRequest(docente.Id));

		docente.Activo.ShouldBeFalse();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task DesafectarDocenteAsync_con_un_docente_inexistente_propaga_NullReferenceException()
	{
		await Should.ThrowAsync<NullReferenceException>(
			() => _servicio.DesafectarDocenteAsync(new DocenteIDRequest(Guid.NewGuid())));
	}

	[Fact]
	public async Task DesafectarDocenteAsync_no_borra_el_legajo()
	{
		// La distinción que el nombre viejo escondía: desafectar conserva la fila.
		var docente = SembrarDocente();

		await _servicio.DesafectarDocenteAsync(new DocenteIDRequest(docente.Id));

		_host.UnidadDeTrabajo.DocentesFake.Elementos.ShouldHaveSingleItem();
	}
	#endregion

	#region EliminarDocenteAsync
	[Fact]
	public async Task EliminarDocenteAsync_borra_el_legajo_sus_licencias_y_su_usuario()
	{
		var docente = SembrarDocente();
		_host.UnidadDeTrabajo.LicenciasFake.Sembrar(new LicenciaBuilder().ConDocente(docente.Id).Build());
		_host.UnidadDeTrabajo.UsuariosFake.Sembrar(new UsuarioBuilder().ConDocente(docente.Id).Build());

		await _servicio.EliminarDocenteAsync(new DocenteIDRequest(docente.Id));

		_host.UnidadDeTrabajo.DocentesFake.Elementos.ShouldBeEmpty();
		_host.UnidadDeTrabajo.LicenciasFake.Elementos.ShouldBeEmpty();
		_host.UnidadDeTrabajo.UsuariosFake.Elementos.ShouldBeEmpty();
		_host.UnidadDeTrabajo.LlamadasDeTransaccion.ShouldContain("CommitTransactionAsync");
	}

	[Fact]
	public async Task EliminarDocenteAsync_vacia_el_cargo_de_preceptor_sin_borrar_la_division()
	{
		// La división NO se borra: queda con el cargo vacante, que es un estado que el dominio
		// modela. Es la diferencia entre "lo que es del docente" y "lo que es de otros".
		var docente = SembrarDocente();
		var curso = new CursoBuilder().Build();
		_host.UnidadDeTrabajo.CursosFake.Sembrar(curso);
		var division = new DivisionBuilder().ConCurso(curso.Id).Build();
		division.AsignarPreceptor(docente.Id);
		_host.UnidadDeTrabajo.DivisionesFake.Sembrar(division);

		await _servicio.EliminarDocenteAsync(new DocenteIDRequest(docente.Id));

		_host.UnidadDeTrabajo.DivisionesFake.Elementos.ShouldHaveSingleItem();
		division.EstaCargoPreceptorVacante().ShouldBeTrue();
	}

	[Fact]
	public async Task EliminarDocenteAsync_con_una_designacion_vigente_lanza_y_no_borra_nada()
	{
		var docente = SembrarDocente();
		// Se designa por el dominio y no con SituacionRevistaBuilder: ese builder arma su PROPIA
		// cátedra auxiliar —el constructor de SituacionRevista es internal— así que la designación
		// no quedaría en la cátedra que siembra la prueba.
		var catedra = new CatedraBuilder().Build();
		catedra.Designar(docente.Id, Cargo.Titular, DateTime.Today.AddMonths(-2), null);
		_host.UnidadDeTrabajo.CatedrasFake.Sembrar(catedra);

		var ex = await Should.ThrowAsync<Exception>(
			() => _servicio.EliminarDocenteAsync(new DocenteIDRequest(docente.Id)));

		ex.GetType().Name.ShouldBe("DocenteConDesignacionesVigentesException");
		_host.UnidadDeTrabajo.DocentesFake.Elementos.ShouldHaveSingleItem();

		// La guarda corre antes de abrir la transacción: nada de rollback sobre algo inexistente.
		_host.UnidadDeTrabajo.LlamadasDeTransaccion.ShouldBeEmpty();
	}

	[Fact]
	public async Task EliminarDocenteAsync_del_unico_administrador_lanza_UltimoAdministradorException()
	{
		var docente = SembrarDocente();
		_host.UnidadDeTrabajo.UsuariosFake.Sembrar(
			new UsuarioBuilder().ConDocente(docente.Id).ConRol(Rol.Admin.Descripcion).Build());

		var ex = await Should.ThrowAsync<Exception>(
			() => _servicio.EliminarDocenteAsync(new DocenteIDRequest(docente.Id)));

		ex.GetType().Name.ShouldBe("UltimoAdministradorException");
		_host.UnidadDeTrabajo.DocentesFake.Elementos.ShouldHaveSingleItem();
		_host.UnidadDeTrabajo.LlamadasDeTransaccion.ShouldBeEmpty();
	}

	[Fact]
	public async Task EliminarDocenteAsync_de_un_administrador_que_no_es_el_ultimo_se_permite()
	{
		// La guarda es sobre el ÚLTIMO, no sobre el rol: con dos cuentas Admin el borrado procede.
		var docente = SembrarDocente();
		var otroDocente = SembrarDocente();
		_host.UnidadDeTrabajo.UsuariosFake.Sembrar(
			new UsuarioBuilder().ConDocente(docente.Id).ConRol(Rol.Admin.Descripcion).Build(),
			new UsuarioBuilder().ConDocente(otroDocente.Id).ConRol(Rol.Admin.Descripcion).Build());

		await _servicio.EliminarDocenteAsync(new DocenteIDRequest(docente.Id));

		_host.UnidadDeTrabajo.UsuariosFake.Elementos.ShouldHaveSingleItem();
	}
	#endregion

	#region ConsultarImpactoEliminacionDocenteAsync
	[Fact]
	public async Task ConsultarImpactoEliminacionDocenteAsync_informa_licencias_divisiones_y_usuario()
	{
		var docente = SembrarDocente();
		_host.UnidadDeTrabajo.LicenciasFake.Sembrar(new LicenciaBuilder().ConDocente(docente.Id).Build());
		_host.UnidadDeTrabajo.UsuariosFake.Sembrar(new UsuarioBuilder().ConDocente(docente.Id).Build());

		var curso = new CursoBuilder().Build();
		_host.UnidadDeTrabajo.CursosFake.Sembrar(curso);
		var division = new DivisionBuilder().ConCurso(curso.Id).Build();
		division.AsignarPreceptor(docente.Id);
		_host.UnidadDeTrabajo.DivisionesFake.Sembrar(division);

		var impacto = await _servicio.ConsultarImpactoEliminacionDocenteAsync(new DocenteIDRequest(docente.Id));

		impacto.Licencias.ShouldBe(1);
		impacto.DivisionesQueQuedanSinPreceptor.ShouldBe(1);
		impacto.TieneUsuario.ShouldBeTrue();
		impacto.EstaBloqueado.ShouldBeFalse();
		impacto.TieneVinculos.ShouldBeTrue();
	}

	[Fact]
	public async Task ConsultarImpactoEliminacionDocenteAsync_marca_el_bloqueo_por_designacion_vigente()
	{
		var docente = SembrarDocente();
		// Se designa por el dominio y no con SituacionRevistaBuilder: ese builder arma su PROPIA
		// cátedra auxiliar —el constructor de SituacionRevista es internal— así que la designación
		// no quedaría en la cátedra que siembra la prueba.
		var catedra = new CatedraBuilder().Build();
		catedra.Designar(docente.Id, Cargo.Titular, DateTime.Today.AddMonths(-2), null);
		_host.UnidadDeTrabajo.CatedrasFake.Sembrar(catedra);

		var impacto = await _servicio.ConsultarImpactoEliminacionDocenteAsync(new DocenteIDRequest(docente.Id));

		impacto.DesignacionesVigentes.ShouldBe(1);
		impacto.EstaBloqueado.ShouldBeTrue();
	}

	[Fact]
	public async Task ConsultarImpactoEliminacionDocenteAsync_de_un_legajo_pelado_no_tiene_vinculos()
	{
		var docente = SembrarDocente();

		var impacto = await _servicio.ConsultarImpactoEliminacionDocenteAsync(new DocenteIDRequest(docente.Id));

		impacto.TieneVinculos.ShouldBeFalse();
		impacto.EstaBloqueado.ShouldBeFalse();
	}
	#endregion

	#region Puestos
	[Fact]
	public async Task ListarPuestosDocentesAsync_devuelve_los_puestos_del_docente()
	{
		var docente = SembrarDocente(conPuesto: true);

		var puestos = await _servicio.ListarPuestosDocentesAsync(new DocenteIDRequest(docente.Id));

		puestos.ShouldHaveSingleItem();
	}

	[Fact]
	public async Task ListarPuestosDocentesAsync_con_un_Id_inexistente_lanza_NullReferenceException()
	{
		await Should.ThrowAsync<NullReferenceException>(
			() => _servicio.ListarPuestosDocentesAsync(new DocenteIDRequest(Guid.NewGuid())));
	}

	[Fact]
	public async Task AgregarPuestoDocenteAsync_suma_un_cargo_al_docente_y_guarda()
	{
		var docente = SembrarDocente();

		await _servicio.AgregarPuestoDocenteAsync(new RegistrarPuestoDocenteRequest(
			docente.Id, "Preceptor", "Pendiente", DateTime.Today, null));

		docente.Puestos.ShouldHaveSingleItem();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task AgregarPuestoDocenteAsync_con_un_Id_inexistente_lanza_NullReferenceException()
	{
		await Should.ThrowAsync<NullReferenceException>(() => _servicio.AgregarPuestoDocenteAsync(
			new RegistrarPuestoDocenteRequest(Guid.NewGuid(), "Preceptor", "Pendiente", DateTime.Today, null)));
	}

	[Fact]
	public async Task EditarPuestoDocenteAsync_cambia_la_posicion_de_un_puesto_pendiente()
	{
		var docente = SembrarDocente(conPuesto: true);
		var puestoID = docente.Puestos.First().Id;

		await _servicio.EditarPuestoDocenteAsync(new EditarPuestoDocenteRequest(
			docente.Id, puestoID, "Preceptor", DateTime.Today, null));

		docente.Puestos.First().Posicion.ShouldBe(Posicion.Preceptor);
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task EditarPuestoDocenteAsync_con_un_Id_inexistente_lanza_NullReferenceException()
	{
		await Should.ThrowAsync<NullReferenceException>(() => _servicio.EditarPuestoDocenteAsync(
			new EditarPuestoDocenteRequest(Guid.NewGuid(), Guid.NewGuid(), "Preceptor", DateTime.Today, null)));
	}

	[Fact]
	public async Task RevocarPuestoDocenteAsync_rescinde_el_cargo_y_guarda()
	{
		var docente = SembrarDocente(conPuesto: true);
		var puestoID = docente.Puestos.First().Id;

		await _servicio.RevocarPuestoDocenteAsync(new RevocarPuestoDocenteRequest(docente.Id, puestoID, DateTime.Today));

		docente.Puestos.First().Estado.ShouldBe(EstadoPuesto.Inactivo);
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task RevocarPuestoDocenteAsync_con_un_Id_inexistente_lanza_NullReferenceException()
	{
		await Should.ThrowAsync<NullReferenceException>(() => _servicio.RevocarPuestoDocenteAsync(
			new RevocarPuestoDocenteRequest(Guid.NewGuid(), Guid.NewGuid(), DateTime.Today)));
	}

	[Fact]
	public async Task EliminarPuestoDocenteAsync_borra_un_puesto_pendiente_y_guarda()
	{
		var docente = SembrarDocente(conPuesto: true);
		var puestoID = docente.Puestos.First().Id;

		await _servicio.EliminarPuestoDocenteAsync(new EliminarPuestoDocenteRequest(docente.Id, puestoID));

		docente.Puestos.ShouldBeEmpty();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task EliminarPuestoDocenteAsync_con_un_Id_inexistente_lanza_NullReferenceException()
	{
		await Should.ThrowAsync<NullReferenceException>(() => _servicio.EliminarPuestoDocenteAsync(
			new EliminarPuestoDocenteRequest(Guid.NewGuid(), Guid.NewGuid())));
	}
	#endregion
}
