using Domain.Shared;
using Domain.Usuarios;
using EDUSIS.TestSupport;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Usuarios;

/// <summary>
/// Enumeración <see cref="Rol"/>: descripciones conocidas, resolución por descripción
/// (<see cref="Enumeration.FromDescripcion{T}"/>) e igualdad por identidad.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class RolTests
{
	[Fact]
	public void Los_seis_roles_conocidos_tienen_su_descripcion()
	{
		Rol.Admin.Descripcion.ShouldBe("Administrador");
		Rol.Direccion.Descripcion.ShouldBe("Dirección");
		Rol.Docente.Descripcion.ShouldBe("Docente");
		Rol.Secretaria.Descripcion.ShouldBe("Secretaria");
		Rol.Preceptoria.Descripcion.ShouldBe("Preceptoría");
		Rol.SecretariaAcademica.Descripcion.ShouldBe("Secretaría Académica");
	}

	/// <remarks>
	/// La descripción más larga manda sobre el mapeo: <c>rol.descripcion</c> es
	/// <c>varchar(25)</c> desde la migración <c>AgregarRolesPreceptoriaYSecretariaAcademica</c>,
	/// y "Secretaría Académica" mide 20. Un rol nuevo que se pase de 25 rompe el INSERT de
	/// <c>HasData</c> sin que ninguna prueba de dominio se entere, así que se fija acá.
	/// </remarks>
	[Fact]
	public void Ninguna_descripcion_excede_el_largo_de_la_columna()
	{
		foreach (var rol in Enumeration.GetAll<Rol>())
		{
			rol.Descripcion.Length.ShouldBeLessThanOrEqualTo(25);
		}
	}

	[Fact]
	public void GetAll_devuelve_exactamente_los_seis_roles()
	{
		Enumeration.GetAll<Rol>().Count().ShouldBe(6);
	}

	[Theory]
	[InlineData("Administrador")]
	[InlineData("Dirección")]
	[InlineData("Docente")]
	[InlineData("Secretaria")]
	[InlineData("Preceptoría")]
	[InlineData("Secretaría Académica")]
	public void FromDescripcion_resuelve_el_rol_por_su_texto(string descripcion)
	{
		Enumeration.FromDescripcion<Rol>(descripcion).Descripcion.ShouldBe(descripcion);
	}

	[Fact]
	public void FromDescripcion_con_un_texto_desconocido_lanza_InvalidOperationException()
	{
		Should.Throw<InvalidOperationException>(() => Enumeration.FromDescripcion<Rol>("Bedel"));
	}

	[Fact]
	public void Dos_referencias_al_mismo_rol_son_iguales_y_uno_distinto_no()
	{
		Rol.Docente.Equals(Enumeration.FromDescripcion<Rol>("Docente")).ShouldBeTrue();
		Rol.Docente.Equals(Rol.Admin).ShouldBeFalse();
	}
}
