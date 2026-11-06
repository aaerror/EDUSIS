using Core.ServicioUsuarios;
using Core.ServicioUsuarios.DTOs.Requests;
using Core.UnitTests.Infraestructura;
using Domain.Shared;
using Domain.Usuarios;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Core.UnitTests.ServicioUsuarios;

/// <summary>
/// <see cref="IServicioUsuario"/>: los métodos públicos con camino feliz y de error.
/// <c>ActualizarRol</c> se reemplazó por <c>AsignarRolAsync</c> / <c>QuitarRolAsync</c> (H-015).
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class ServicioUsuarioTests
{
	private readonly HostDeServicios _host = new();
	private readonly IServicioUsuario _servicio;

	public ServicioUsuarioTests()
	{
		_servicio = _host.Resolver<IServicioUsuario>();
	}

	private (Domain.Docentes.Docente docente, Domain.Usuarios.Usuario usuario) SembrarDocenteConUsuario(string username = "maria.gonzalez")
	{
		var docente = new DocenteBuilder().Build();
		var usuario = new UsuarioBuilder().ConDocente(docente.Id).ConUsername(username).Build();
		_host.UnidadDeTrabajo.DocentesFake.Sembrar(docente);
		_host.UnidadDeTrabajo.UsuariosFake.Sembrar(usuario);
		return (docente, usuario);
	}

	#region BuscarUsuarioPorIDAsync
	[Fact]
	public async Task BuscarUsuarioPorIDAsync_devuelve_el_perfil_del_usuario_y_su_docente()
	{
		var (docente, usuario) = SembrarDocenteConUsuario();

		var response = await _servicio.BuscarUsuarioPorIDAsync(new UsuarioIDRequest(usuario.Id));

		response.UsuarioID.ShouldBe(usuario.Id);
		response.DocenteID.ShouldBe(docente.Id);
		response.NombreCompleto.ShouldBe(docente.DatosPersonales.NombreCompleto());
	}

	[Fact]
	public async Task BuscarUsuarioPorIDAsync_con_un_Id_inexistente_lanza_NullReferenceException()
	{
		await Should.ThrowAsync<NullReferenceException>(
			() => _servicio.BuscarUsuarioPorIDAsync(new UsuarioIDRequest(Guid.NewGuid())));
	}
	#endregion

	#region RegistrarUsuarioAsync
	[Fact]
	public async Task RegistrarUsuarioAsync_crea_el_usuario_del_docente_y_guarda()
	{
		var docente = new DocenteBuilder().Build();
		_host.UnidadDeTrabajo.DocentesFake.Sembrar(docente);

		await _servicio.RegistrarUsuarioAsync(new RegistrarUsuarioRequest(docente.Id, Rol.Docente.Id));

		_host.UnidadDeTrabajo.UsuariosFake.Elementos.ShouldHaveSingleItem();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task RegistrarUsuarioAsync_falla_si_el_docente_ya_tiene_usuario()
	{
		var (docente, _) = SembrarDocenteConUsuario();

		await Should.ThrowAsync<ArgumentException>(
			() => _servicio.RegistrarUsuarioAsync(new RegistrarUsuarioRequest(docente.Id, Rol.Docente.Id)));
	}

	/// <remarks>
	/// <strong>La prueba del bug de UI-46(a).</strong> Hasta el 2026-11-06 el caso de uso recibía el
	/// rol en el request y <strong>no lo usaba</strong>: construía el <c>Usuario</c> con el ctor sin
	/// roles, así que toda cuenta creada nacía sin un solo rol y por lo tanto sin un solo permiso —
	/// entraba al sistema con el menú entero apagado y sin forma de arreglarlo, porque los dos casos
	/// de uso de roles no tenían pantalla.
	///
	/// La prueba que había sólo afirmaba que el usuario existía y que se guardó, así que pasaba en
	/// verde con el defecto puesto. Lo que hay que mirar es el rol.
	/// </remarks>
	[Theory]
	[InlineData(1)]
	[InlineData(3)]
	[InlineData(5)]
	public async Task RegistrarUsuarioAsync_le_pone_al_usuario_el_rol_que_viene_en_el_request(int rolID)
	{
		var docente = new DocenteBuilder().Build();
		_host.UnidadDeTrabajo.DocentesFake.Sembrar(docente);

		await _servicio.RegistrarUsuarioAsync(new RegistrarUsuarioRequest(docente.Id, rolID));

		var usuario = _host.UnidadDeTrabajo.UsuariosFake.Elementos.ShouldHaveSingleItem();
		usuario.Roles.ShouldHaveSingleItem().Id.ShouldBe(rolID);

		// Y por lo tanto tiene permisos: es la consecuencia que importaba, no la fila en la tabla.
		usuario.TienePermiso(Enumeration.FromValue<Rol>(rolID).Permisos.First()).ShouldBeTrue();
	}

	[Fact]
	public async Task RegistrarUsuarioAsync_con_un_rol_inexistente_lanza_y_no_crea_la_cuenta()
	{
		// Mejor fallar que crear una cuenta sin permisos, que es lo que hacía antes con CUALQUIER rol.
		var docente = new DocenteBuilder().Build();
		_host.UnidadDeTrabajo.DocentesFake.Sembrar(docente);

		await Should.ThrowAsync<Exception>(
			() => _servicio.RegistrarUsuarioAsync(new RegistrarUsuarioRequest(docente.Id, 99)));

		_host.UnidadDeTrabajo.UsuariosFake.Elementos.ShouldBeEmpty();
	}
	#endregion

	#region ListarUsuariosAsync
	[Fact]
	public async Task ListarUsuariosAsync_devuelve_cada_cuenta_con_su_docente_y_sus_roles()
	{
		// Siembra su propio usuario CON rol: el helper compartido crea cuentas sin ninguno, y lo que
		// esta prueba necesita afirmar es justamente que los roles llegan al Response —es la columna
		// que la grilla de la pantalla de usuarios muestra—.
		var docente = new DocenteBuilder().Build();
		var usuario = new UsuarioBuilder()
			.ConDocente(docente.Id)
			.ConUsername("ana.lopez")
			.ConRol(Rol.Preceptoria.Descripcion)
			.Build();
		_host.UnidadDeTrabajo.DocentesFake.Sembrar(docente);
		_host.UnidadDeTrabajo.UsuariosFake.Sembrar(usuario);

		var usuarios = await _servicio.ListarUsuariosAsync();

		var response = usuarios.ShouldHaveSingleItem();
		response.DocenteID.ShouldBe(docente.Id);
		response.NombreCompleto.ShouldBe(docente.DatosPersonales.NombreCompleto());
		response.RolesIDs.ShouldHaveSingleItem().ShouldBe(Rol.Preceptoria.Id);
	}

	[Fact]
	public async Task ListarUsuariosAsync_sin_cuentas_devuelve_vacio()
	{
		var usuarios = await _servicio.ListarUsuariosAsync();

		usuarios.ShouldBeEmpty();
	}
	#endregion

	#region RestablecerAccesoAsync
	[Fact]
	public async Task RestablecerAccesoAsync_cambia_salt_y_hash_del_usuario_y_guarda()
	{
		var (_, usuario) = SembrarDocenteConUsuario("maria.gonzalez");
		var saltPrevio = usuario.PasswordSalt;
		var hashPrevio = usuario.PasswordHash;

		await _servicio.RestablecerAccesoAsync(new RestablecerAccesoRequest("maria.gonzalez"));

		usuario.PasswordSalt.ShouldNotBe(saltPrevio);
		usuario.PasswordHash.ShouldNotBe(hashPrevio);
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task RestablecerAccesoAsync_con_un_email_sin_usuario_lanza_ArgumentException()
	{
		await Should.ThrowAsync<ArgumentException>(
			() => _servicio.RestablecerAccesoAsync(new RestablecerAccesoRequest("desconocido@correo.com")));
	}
	#endregion

	#region SolicitarAccesoAsync
	[Fact]
	public async Task SolicitarAccesoAsync_crea_el_acceso_cuando_docente_y_legajo_coinciden()
	{
		var docente = new DocenteBuilder().ConLegajo("100200").Build();
		_host.UnidadDeTrabajo.DocentesFake.Sembrar(docente);

		await _servicio.SolicitarAccesoAsync(new SolicitarAccesoRequest(docente.Id, "100200"));

		_host.UnidadDeTrabajo.UsuariosFake.Elementos.ShouldHaveSingleItem();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task SolicitarAccesoAsync_con_datos_de_docente_incorrectos_lanza_ArgumentException()
	{
		await Should.ThrowAsync<ArgumentException>(
			() => _servicio.SolicitarAccesoAsync(new SolicitarAccesoRequest(Guid.NewGuid(), "999999")));
	}
	#endregion

	#region AsignarRolAsync / QuitarRolAsync
	[Fact]
	public async Task ActualizarRol_asigna_el_rol_indicado_al_usuario()
	{
		var (_, usuario) = SembrarDocenteConUsuario();

		await _servicio.AsignarRolAsync(new ActualizarRolRequest(usuario.Id, Rol.Docente.Id));

		usuario.Roles.ShouldContain(Domain.Usuarios.Rol.Docente);
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task AsignarRolAsync_con_un_usuario_inexistente_lanza_NullReferenceException()
	{
		await Should.ThrowAsync<NullReferenceException>(
			() => _servicio.AsignarRolAsync(new ActualizarRolRequest(Guid.NewGuid(), Rol.Docente.Id)));
	}

	[Fact]
	public async Task QuitarRolAsync_quita_el_rol_indicado_al_usuario()
	{
		var (_, usuario) = SembrarDocenteConUsuario();
		usuario.AgregarRol("Docente");

		await _servicio.QuitarRolAsync(new ActualizarRolRequest(usuario.Id, Rol.Docente.Id));

		usuario.Roles.ShouldNotContain(Domain.Usuarios.Rol.Docente);
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}
	#endregion
}
