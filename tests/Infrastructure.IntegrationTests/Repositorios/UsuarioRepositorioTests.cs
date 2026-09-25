using Domain.Usuarios;
using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Infraestructura;
using Infrastructure.IntegrationTests.Infraestructura;
using Shouldly;
using Xunit;

namespace Infrastructure.IntegrationTests.Repositorios;

/// <summary>
/// Tests del repositorio de Usuario con búsquedas y validaciones.
/// </summary>
public sealed class UsuarioRepositorioTests : BaseIntegracion
{
	public UsuarioRepositorioTests(SqlServerFixture fixture)
		: base(fixture)
	{
	}

	private async Task<Usuario> SembrarUsuarioAsync(Guid docenteID, string username)
	{
		var usuario = new UsuarioBuilder()
			.ConDocente(docenteID)
			.ConUsername(username)
			.ConRol("Docente")
			.Build();

		await using var contexto = Fixture.CrearContexto();
		contexto.Add(usuario);
		await contexto.SaveChangesAsync();
		return usuario;
	}

	[RequiereSqlServerFact]
	public async Task BuscarPorUsernameAsync_devuelve_el_usuario_sin_distinguir_mayusculas()
	{
		var docenteID = await SembrarDocenteAsync();
		var sembrado = await SembrarUsuarioAsync(docenteID, "maria.gonzalez");

		using var uow = CrearUnidadDeTrabajo();

		var encontrado = await uow.Usuarios.BuscarPorUsernameAsync("MARIA.GONZALEZ");

		encontrado.ShouldNotBeNull();
		encontrado!.Id.ShouldBe(sembrado.Id);
	}

	[RequiereSqlServerFact]
	public async Task BuscarPorDocenteAsync_devuelve_el_usuario_del_docente()
	{
		var docenteID = await SembrarDocenteAsync();
		var sembrado = await SembrarUsuarioAsync(docenteID, "rocio.dominguez");

		using var uow = CrearUnidadDeTrabajo();

		var usuario = await uow.Usuarios.BuscarPorDocenteAsync(docenteID);

		usuario.ShouldNotBeNull();
		usuario!.Id.ShouldBe(sembrado.Id);
	}

	[RequiereSqlServerFact]
	public async Task ExisteUsuarioDelDocenteAsync_retorna_true_cuando_el_docente_tiene_usuario()
	{
		var docenteID = await SembrarDocenteAsync();
		await SembrarUsuarioAsync(docenteID, "pedro.martinez");

		using var uow = CrearUnidadDeTrabajo();

		(await uow.Usuarios.ExisteUsuarioDelDocenteAsync(docenteID)).ShouldBeTrue();
		(await uow.Usuarios.ExisteUsuarioDelDocenteAsync(Guid.NewGuid())).ShouldBeFalse();
	}

	[RequiereSqlServerFact]
	public async Task EsUsernameInvalidoAsync_retorna_true_cuando_el_username_ya_existe()
	{
		var docenteID = await SembrarDocenteAsync();
		await SembrarUsuarioAsync(docenteID, "ana.lopez");

		using var uow = CrearUnidadDeTrabajo();

		(await uow.Usuarios.EsUsernameInvalidoAsync("ana.lopez")).ShouldBeTrue();
		(await uow.Usuarios.EsUsernameInvalidoAsync("ANA.LOPEZ")).ShouldBeTrue();
		(await uow.Usuarios.EsUsernameInvalidoAsync("nuevo.usuario")).ShouldBeFalse();
	}
}
