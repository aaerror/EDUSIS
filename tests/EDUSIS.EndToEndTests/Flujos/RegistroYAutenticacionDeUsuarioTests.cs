using System.Net;
using Core.ServicioAutenticaciones;
using Core.ServicioAutenticaciones.DTOs.Requests;
using Core.ServicioUsuarios;
using Core.ServicioUsuarios.DTOs.Requests;
using EDUSIS.EndToEndTests.Infraestructura;
using EDUSIS.TestSupport.Infraestructura;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace EDUSIS.EndToEndTests.Flujos;

/// <summary>
/// Flujo e2e 4 (SC-003 / data-model.md §6): registro de un usuario para un docente
/// (<see cref="IServicioUsuario.RegistrarUsuarioAsync"/>) y autenticación posterior
/// (<see cref="IServicioAutenticacion.Login"/>) con credenciales válidas e inválidas, desde la
/// fachada hasta SQL Server real con la composición de dependencias de producción (hash/salt
/// reales de <c>ServicioSeguridad</c>).
/// </summary>
public sealed class RegistroYAutenticacionDeUsuarioTests : BaseE2E
{
	public RegistroYAutenticacionDeUsuarioTests(SqlServerFixture fixture)
		: base(fixture)
	{
	}

	[RequiereSqlServerFact]
	public async Task RegistrarUsuario_y_Login_con_credenciales_validas_devuelve_el_perfil_del_docente()
	{
		var docente = await SembrarDocenteAsync();
		// RegistrarUsuarioAsync toma el usuario del email del docente y la clave del documento.
		var usuario = docente.Email;
		var clave = docente.DatosPersonales.Documento;

		await EnUnScope(sp => sp.GetRequiredService<IServicioUsuario>().RegistrarUsuarioAsync(
			new RegistrarUsuarioRequest(docente.Id, "Docente")));

		var perfil = await EnUnScope(sp => sp.GetRequiredService<IServicioAutenticacion>().Login(
			new LoginRequest(new NetworkCredential(usuario, clave))));

		perfil.DocenteID.ShouldBe(docente.Id);
		perfil.Usuario.ShouldBe(usuario);
		perfil.NombreCompleto.ShouldBe(docente.DatosPersonales.NombreCompleto());
	}

	[RequiereSqlServerFact]
	public async Task Login_con_la_clave_incorrecta_del_usuario_registrado_lanza_ArgumentException()
	{
		var docente = await SembrarDocenteAsync();

		await EnUnScope(sp => sp.GetRequiredService<IServicioUsuario>().RegistrarUsuarioAsync(
			new RegistrarUsuarioRequest(docente.Id, "Docente")));

		await Should.ThrowAsync<ArgumentException>(() => EnUnScope(sp =>
			sp.GetRequiredService<IServicioAutenticacion>().Login(
				new LoginRequest(new NetworkCredential(docente.Email, "clave-incorrecta")))));
	}

	[RequiereSqlServerFact]
	public async Task Login_con_un_usuario_que_no_existe_lanza_ArgumentException()
	{
		await Should.ThrowAsync<ArgumentException>(() => EnUnScope(sp =>
			sp.GetRequiredService<IServicioAutenticacion>().Login(
				new LoginRequest(new NetworkCredential("no.existe@correo.com", "cualquiera")))));
	}
}
