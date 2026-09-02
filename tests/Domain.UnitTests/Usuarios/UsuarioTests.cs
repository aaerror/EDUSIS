using Domain.Usuarios;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Usuarios;

/// <summary>
/// Agregado <see cref="Usuario"/>: alta con validación de docente, usuario y credenciales, y
/// gestión de <see cref="Rol"/> (alta/baja y unicidad). El módulo no define excepciones
/// propias: usa <see cref="ArgumentException"/> / <see cref="ArgumentNullException"/>.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class UsuarioTests
{
	#region Alta
	[Fact]
	public void Un_usuario_nuevo_guarda_docente_username_y_credenciales_sin_roles()
	{
		var docenteID = Guid.NewGuid();

		var usuario = new UsuarioBuilder().ConDocente(docenteID).ConUsername("maria.gonzalez").Build();

		usuario.DocenteID.ShouldBe(docenteID);
		usuario.Username.ShouldBe("maria.gonzalez");
		usuario.PasswordSalt.ShouldNotBeNullOrWhiteSpace();
		usuario.PasswordHash.ShouldNotBeNullOrWhiteSpace();
		usuario.Roles.ShouldBeEmpty();
	}

	[Fact]
	public void Un_usuario_sin_docente_lanza_ArgumentNullException()
	{
		Should.Throw<ArgumentNullException>(() => new Usuario(Guid.Empty, "maria.gonzalez", "salt", "hash"));
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void Un_usuario_sin_nombre_lanza_ArgumentNullException(string username)
	{
		Should.Throw<ArgumentNullException>(() => new Usuario(Guid.NewGuid(), username, "salt", "hash"));
	}

	[Fact]
	public void Un_usuario_sin_hash_o_sin_salt_lanza_ArgumentNullException()
	{
		Should.Throw<ArgumentNullException>(() => new Usuario(Guid.NewGuid(), "maria.gonzalez", null!, "hash"));
		Should.Throw<ArgumentNullException>(() => new Usuario(Guid.NewGuid(), "maria.gonzalez", "salt", null!));
	}
	#endregion

	#region Roles
	[Fact]
	public void AgregarRol_asigna_el_rol_por_su_descripcion()
	{
		var usuario = new UsuarioBuilder().Build();

		usuario.AgregarRol("Administrador");

		usuario.Roles.ShouldHaveSingleItem().ShouldBe(Rol.Admin);
	}

	[Fact]
	public void AgregarRol_dos_veces_el_mismo_rol_lanza_ArgumentException()
	{
		var usuario = new UsuarioBuilder().ConRol("Docente").Build();

		Should.Throw<ArgumentException>(() => usuario.AgregarRol("Docente"));
	}

	[Fact]
	public void AgregarRol_con_una_descripcion_desconocida_lanza_InvalidOperationException()
	{
		var usuario = new UsuarioBuilder().Build();

		Should.Throw<InvalidOperationException>(() => usuario.AgregarRol("Bedel"));
	}

	[Fact]
	public void QuitarRol_desasigna_un_rol_existente()
	{
		var usuario = new UsuarioBuilder().ConRol("Docente").ConRol("Secretaria").Build();

		usuario.QuitarRol("Docente");

		usuario.Roles.ShouldHaveSingleItem().ShouldBe(Rol.Secretaria);
	}

	[Fact]
	public void QuitarRol_de_un_rol_no_asignado_lanza_ArgumentException()
	{
		var usuario = new UsuarioBuilder().Build();

		Should.Throw<ArgumentException>(() => usuario.QuitarRol("Dirección"));
	}
	#endregion

	#region Credenciales
	[Fact]
	public void CambiarPassword_actualiza_salt_y_hash()
	{
		var usuario = new UsuarioBuilder().Build();

		usuario.CambiarPassword("nuevo-salt", "nuevo-hash");

		usuario.PasswordSalt.ShouldBe("nuevo-salt");
		usuario.PasswordHash.ShouldBe("nuevo-hash");
	}
	#endregion
}
