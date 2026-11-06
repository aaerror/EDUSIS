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

	private async Task<Usuario> SembrarUsuarioDocenteAsync(Guid docenteID, string username)
	{
		var usuario = new UsuarioBuilder()
			.ConDocente(docenteID)
			.ConUsername(username)
			.ConRol("Docente")
			.Build();

		await SembrarUsuarioAsync(usuario);
		return usuario;
	}

	[RequiereSqlServerFact]
	public async Task BuscarPorUsernameAsync_devuelve_el_usuario_sin_distinguir_mayusculas()
	{
		var docenteID = await SembrarDocenteAsync();
		var sembrado = await SembrarUsuarioDocenteAsync(docenteID, "maria.gonzalez");

		using var uow = CrearUnidadDeTrabajo();

		var encontrado = await uow.Usuarios.BuscarPorUsernameAsync("MARIA.GONZALEZ");

		encontrado.ShouldNotBeNull();
		encontrado!.Id.ShouldBe(sembrado.Id);
	}

	[RequiereSqlServerFact]
	public async Task BuscarPorDocenteAsync_devuelve_el_usuario_del_docente()
	{
		var docenteID = await SembrarDocenteAsync();
		var sembrado = await SembrarUsuarioDocenteAsync(docenteID, "rocio.dominguez");

		using var uow = CrearUnidadDeTrabajo();

		var usuario = await uow.Usuarios.BuscarPorDocenteAsync(docenteID);

		usuario.ShouldNotBeNull();
		usuario!.Id.ShouldBe(sembrado.Id);
	}

	[RequiereSqlServerFact]
	public async Task ExisteUsuarioDelDocenteAsync_retorna_true_cuando_el_docente_tiene_usuario()
	{
		var docenteID = await SembrarDocenteAsync();
		await SembrarUsuarioDocenteAsync(docenteID, "pedro.martinez");

		using var uow = CrearUnidadDeTrabajo();

		(await uow.Usuarios.ExisteUsuarioDelDocenteAsync(docenteID)).ShouldBeTrue();
		(await uow.Usuarios.ExisteUsuarioDelDocenteAsync(Guid.NewGuid())).ShouldBeFalse();
	}

	[RequiereSqlServerFact]
	public async Task EsUsernameInvalidoAsync_retorna_true_cuando_el_username_ya_existe()
	{
		var docenteID = await SembrarDocenteAsync();
		await SembrarUsuarioDocenteAsync(docenteID, "ana.lopez");

		using var uow = CrearUnidadDeTrabajo();

		(await uow.Usuarios.EsUsernameInvalidoAsync("ana.lopez")).ShouldBeTrue();
		(await uow.Usuarios.EsUsernameInvalidoAsync("ANA.LOPEZ")).ShouldBeTrue();
		(await uow.Usuarios.EsUsernameInvalidoAsync("nuevo.usuario")).ShouldBeFalse();
	}

	/// <remarks>
	/// <para>
	/// La consume la guarda del último administrador de <c>EliminarDocenteAsync</c>, y es una
	/// consulta que <strong>sólo</strong> se puede verificar contra la base: cuenta sobre la
	/// navegación <c>Usuario.Roles</c>, que vive en la tabla de unión <c>usuario_rol</c>. Sin el
	/// <c>Include</c> de <c>Consulta()</c> la navegación llega vacía y el conteo daría cero para
	/// todos los roles — que es el modo de fallar peor posible acá, porque la guarda dejaría borrar
	/// al último administrador en silencio. El fake no puede detectarlo: en memoria la colección
	/// siempre está poblada.
	/// </para>
	/// <para>
	/// Por <c>Id</c> y no por descripción, siguiendo la decisión #6 de <c>plan/rbac-y-seed.md</c>.
	/// </para>
	/// </remarks>
	[RequiereSqlServerFact]
	public async Task ContarUsuariosConRolAsync_cuenta_por_Id_leyendo_los_roles_de_la_tabla_de_union()
	{
		var primerAdmin = new UsuarioBuilder()
			.ConDocente(await SembrarDocenteAsync())
			.ConUsername("admin.uno")
			.ConRol(Rol.Admin.Descripcion)
			.Build();
		var segundoAdmin = new UsuarioBuilder()
			.ConDocente(await SembrarDocenteAsync())
			.ConUsername("admin.dos")
			.ConRol(Rol.Admin.Descripcion)
			.Build();

		await SembrarUsuarioAsync(primerAdmin);
		await SembrarUsuarioAsync(segundoAdmin);
		await SembrarUsuarioDocenteAsync(await SembrarDocenteAsync(), "solo.docente");

		using var uow = CrearUnidadDeTrabajo();

		(await uow.Usuarios.ContarUsuariosConRolAsync(Rol.Admin.Id)).ShouldBe(2);
		(await uow.Usuarios.ContarUsuariosConRolAsync(Rol.Docente.Id)).ShouldBe(1);
		(await uow.Usuarios.ContarUsuariosConRolAsync(Rol.Preceptoria.Id)).ShouldBe(0);
	}
}
