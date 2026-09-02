using System.Net;
using Core.ServicioAutenticaciones;
using Core.ServicioAutenticaciones.DTOs.Requests;
using Core.UnitTests.Infraestructura;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Core.UnitTests.ServicioAutenticaciones;

/// <summary>
/// <see cref="IServicioAutenticacion"/>: <c>Login</c> valida credenciales contra el hash guardado
/// del usuario y delega el armado del perfil en <see cref="Core.ServicioUsuarios.IServicioUsuario"/>.
/// <c>Logout</c> no está implementado (H-014).
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class ServicioAutenticacionTests
{
	private readonly HostDeServicios _host = new();
	private readonly IServicioAutenticacion _servicio;

	public ServicioAutenticacionTests()
	{
		_servicio = _host.Resolver<IServicioAutenticacion>();
	}

	private Domain.Usuarios.Usuario SembrarUsuarioConClave(string username, string clave)
	{
		var docente = new DocenteBuilder().Build();
		var credenciales = _host.Seguridad.HashPassword(clave);

		var usuario = new UsuarioBuilder()
			.ConDocente(docente.Id)
			.ConUsername(username)
			.ConPasswordSalt(credenciales["salt"])
			.ConPasswordHash(credenciales["hash"])
			.Build();

		_host.UnidadDeTrabajo.DocentesFake.Sembrar(docente);
		_host.UnidadDeTrabajo.UsuariosFake.Sembrar(usuario);
		return usuario;
	}

	#region Login
	[Fact]
	public async Task Login_con_credenciales_validas_devuelve_el_perfil_del_usuario()
	{
		var usuario = SembrarUsuarioConClave("maria.gonzalez", "Secreta-123");

		var response = await _servicio.Login(new LoginRequest(new NetworkCredential("maria.gonzalez", "Secreta-123")));

		response.UsuarioID.ShouldBe(usuario.Id);
		response.DocenteID.ShouldBe(usuario.DocenteID);
		response.Usuario.ShouldBe("maria.gonzalez");
	}

	[Fact]
	public async Task Login_con_un_usuario_inexistente_lanza_ArgumentException()
	{
		await Should.ThrowAsync<ArgumentException>(
			() => _servicio.Login(new LoginRequest(new NetworkCredential("no.existe", "cualquiera"))));
	}

	[Fact]
	public async Task Login_con_una_clave_incorrecta_lanza_ArgumentException()
	{
		SembrarUsuarioConClave("maria.gonzalez", "clave-correcta");

		await Should.ThrowAsync<ArgumentException>(
			() => _servicio.Login(new LoginRequest(new NetworkCredential("maria.gonzalez", "clave-incorrecta"))));
	}
	#endregion

	#region Logout
	[Fact]
	public void Logout_no_esta_implementado_y_lanza_NotImplementedException()
	{
		// Documenta el estado actual (H-014): el método está declarado en la interfaz pero su
		// cuerpo es `throw new NotImplementedException()`.
		Should.Throw<NotImplementedException>(() => _servicio.Logout(new LogoutRequest(Guid.NewGuid())));
	}

	[Fact(Skip = "H-014: ServicioAutenticacion.Logout no tiene implementación (throw new NotImplementedException). Ver hallazgos.md.")]
	public void Logout_cierra_la_sesion_del_usuario()
	{
	}
	#endregion
}
