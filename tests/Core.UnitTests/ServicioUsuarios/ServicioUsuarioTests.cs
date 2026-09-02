using Core.ServicioUsuarios;
using Core.ServicioUsuarios.DTOs.Requests;
using Core.UnitTests.Infraestructura;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Core.UnitTests.ServicioUsuarios;

/// <summary>
/// <see cref="IServicioUsuario"/>: los 5 métodos públicos con camino feliz y de error.
/// <c>ActualizarRol</c> no está implementado (H-015).
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

		await _servicio.RegistrarUsuarioAsync(new RegistrarUsuarioRequest(docente.Id, "Docente"));

		_host.UnidadDeTrabajo.UsuariosFake.Elementos.ShouldHaveSingleItem();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task RegistrarUsuarioAsync_falla_si_el_docente_ya_tiene_usuario()
	{
		var (docente, _) = SembrarDocenteConUsuario();

		await Should.ThrowAsync<ArgumentException>(
			() => _servicio.RegistrarUsuarioAsync(new RegistrarUsuarioRequest(docente.Id, "Docente")));
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

	#region ActualizarRol
	[Fact]
	public void ActualizarRol_no_esta_implementado_y_lanza_NotImplementedException()
	{
		// Documenta el estado actual (H-015): cuerpo `throw new NotImplementedException()`.
		Should.Throw<NotImplementedException>(
			() => _servicio.ActualizarRol(new ActualizarRolRequest(Guid.NewGuid(), "Docente")));
	}

	[Fact(Skip = "H-015: ServicioUsuario.ActualizarRol no tiene implementación (throw new NotImplementedException). Ver hallazgos.md.")]
	public void ActualizarRol_asigna_el_rol_indicado_al_usuario()
	{
	}
	#endregion
}
