using Domain.Usuarios;
using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Infraestructura;
using Infrastructure.IntegrationTests.Infraestructura;
using Shouldly;
using Xunit;

namespace Infrastructure.IntegrationTests.Repositorios;

/// <summary>
/// US3-2: consultas de <c>UsuarioRepository</c> sobre datos sembrados.
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
	public async Task ExisteUsuarioDelDocente_es_true_cuando_el_docente_ya_tiene_usuario()
	{
		var docenteID = await SembrarDocenteAsync();
		await SembrarUsuarioAsync(docenteID, "rocio.dominguez");

		using var uow = CrearUnidadDeTrabajo();

		uow.Usuarios.ExisteUsuarioDelDocente(docenteID).ShouldBeTrue();
		uow.Usuarios.ExisteUsuarioDelDocente(Guid.NewGuid()).ShouldBeFalse();
	}

	[RequiereSqlServerFact]
	public async Task BuscarPorEmail_devuelve_el_usuario_por_su_nombre_de_usuario_sin_distinguir_mayusculas()
	{
		var docenteID = await SembrarDocenteAsync();
		var sembrado = await SembrarUsuarioAsync(docenteID, "maria.gonzalez");

		using var uow = CrearUnidadDeTrabajo();

		var encontrado = uow.Usuarios.BuscarPorEmail("MARIA.GONZALEZ");

		encontrado.ShouldNotBeNull();
		encontrado.Id.ShouldBe(sembrado.Id);
	}
}
