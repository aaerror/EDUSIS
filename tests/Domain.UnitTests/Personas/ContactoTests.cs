using Domain.Personas;
using Domain.Personas.Exceptions;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Personas;

/// <summary>
/// Value object <see cref="Contacto"/> y <see cref="ContactoDuplicadoException"/>.
/// Una prueba queda <c>Skip</c> por un defecto de producción registrado en
/// <c>specs/001-automated-test-suite/hallazgos.md</c> (H-001).
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class ContactoTests
{
	#region Camino válido
	[Fact]
	public void CrearEmail_produce_un_contacto_de_tipo_Email()
	{
		var contacto = Contacto.CrearEmail("maria.gonzalez@correo.com");

		contacto.TipoContacto.ShouldBe(TipoContacto.Email);
		contacto.Descripcion.ShouldBe("maria.gonzalez@correo.com");
	}

	[Fact]
	public void CrearTelefono_produce_un_contacto_de_tipo_Telefono()
	{
		var contacto = Contacto.CrearTelefono("3814123456");

		contacto.TipoContacto.ShouldBe(TipoContacto.Telefono);
		contacto.Descripcion.ShouldBe("3814123456");
	}

	[Fact]
	public void El_constructor_recorta_los_espacios_de_la_descripcion()
	{
		var contacto = new ContactoBuilder().ConDescripcion("  correo@dominio.com  ").Build();

		contacto.Descripcion.ShouldBe("correo@dominio.com");
	}

	[Fact]
	public void ModificarContacto_conserva_el_tipo_y_cambia_la_descripcion()
	{
		var original = Contacto.CrearEmail("maria.gonzalez@correo.com");

		var modificado = original.ModificarContacto("nuevo@correo.com");

		modificado.TipoContacto.ShouldBe(TipoContacto.Email);
		modificado.Descripcion.ShouldBe("nuevo@correo.com");
	}
	#endregion

	#region Invariantes
	[Theory]
	[InlineData("   ")]
	[InlineData("\t")]
	public void El_constructor_con_descripcion_en_blanco_lanza_ArgumentNullException(string descripcion)
	{
		Should.Throw<ArgumentNullException>(() => new Contacto(TipoContacto.Email, descripcion));
	}
	#endregion

	#region Igualdad estructural
	[Fact]
	public void Dos_contactos_con_el_mismo_tipo_y_descripcion_son_iguales()
	{
		var uno = Contacto.CrearEmail("maria.gonzalez@correo.com");
		var otro = Contacto.CrearEmail("maria.gonzalez@correo.com");

		uno.Equals(otro).ShouldBeTrue();
		uno.GetHashCode().ShouldBe(otro.GetHashCode());
	}

	[Fact]
	public void Dos_contactos_con_distinta_descripcion_no_son_iguales()
	{
		var uno = Contacto.CrearEmail("maria.gonzalez@correo.com");
		var otro = Contacto.CrearEmail("otra.persona@correo.com");

		uno.Equals(otro).ShouldBeFalse();
	}

	[Fact]
	public void Dos_contactos_con_distinto_tipo_no_son_iguales()
	{
		var email = new Contacto(TipoContacto.Email, "11 2345 6789");
		var telefono = new Contacto(TipoContacto.Telefono, "11 2345 6789");

		email.Equals(telefono).ShouldBeFalse();
	}
	#endregion

	#region ContactoDuplicadoException
	[Fact]
	public void ContactoDuplicadoException_antepone_su_mensaje_de_error_constante()
	{
		var ex = new ContactoDuplicadoException("email");

		ex.Message.ShouldContain("ya se encuentra almacenado");
		ex.Message.ShouldContain("email");
	}

	[Fact(Skip = "H-001: ContactoDuplicadoException no se lanza desde ningún agregado (código sin cablear). Ver hallazgos.md.")]
	public void Agregar_dos_veces_el_mismo_contacto_lanza_ContactoDuplicadoException()
	{
		// No hay agregado en el dominio que administre una colección de contactos y aplique la
		// regla de unicidad; la excepción existe pero nunca se dispara.
	}
	#endregion
}
