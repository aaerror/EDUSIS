using Domain.Usuarios;
using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Infraestructura;
using Infrastructure.IntegrationTests.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Infrastructure.IntegrationTests.Mapeo;

/// <summary>
/// US3-1: persistir y releer un <see cref="Usuario"/> con roles. Verifica la relación
/// muchos-a-muchos <c>usuario</c> ↔ <c>roles</c> a través de la tabla puente <c>usuario_rol</c>.
/// </summary>
public sealed class MapeoDeUsuarioTests : BaseIntegracion
{
	public MapeoDeUsuarioTests(SqlServerFixture fixture)
		: base(fixture)
	{
	}

	[RequiereSqlServerFact]
	public async Task Un_usuario_con_rol_se_relee_con_su_rol()
	{
		var usuario = new UsuarioBuilder()
			.ConDocente(await SembrarDocenteAsync())
			.ConUsername("maría.gonzález")
			.ConRol("Docente")
			.Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.Add(usuario);
			await contexto.SaveChangesAsync();
		}

		await using (var contexto = Fixture.CrearContexto())
		{
			var releido = await contexto.Set<Usuario>()
				.Include(x => x.Roles)
				.SingleAsync(x => x.Id == usuario.Id);

			releido.Username.ShouldBe("maría.gonzález");
			releido.PasswordSalt.ShouldBe(usuario.PasswordSalt);
			releido.PasswordHash.ShouldBe(usuario.PasswordHash);
			releido.Roles.ShouldContain(Rol.Docente);
		}
	}
}
