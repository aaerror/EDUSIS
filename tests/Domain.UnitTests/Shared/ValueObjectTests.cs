using Domain.Shared;
using EDUSIS.TestSupport;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Shared;

/// <summary>
/// Contrato de <see cref="ValueObject"/>: igualdad estructural y <see cref="object.GetHashCode"/>
/// derivados de <c>GetEqualityCommponents()</c> (el typo está en el código de producción y se
/// respeta). Se ejercita con dos value objects de prueba definidos en este archivo.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class ValueObjectTests
{
	#region Igualdad estructural
	[Fact]
	public void Dos_value_objects_con_los_mismos_componentes_son_iguales()
	{
		var uno = new PuntoDePrueba(1, "ñandú");
		var otro = new PuntoDePrueba(1, "ñandú");

		uno.Equals(otro).ShouldBeTrue();
		(uno == otro).ShouldBeTrue();
		(uno != otro).ShouldBeFalse();
		uno.GetHashCode().ShouldBe(otro.GetHashCode());
	}

	[Fact]
	public void Difieren_si_cambia_cualquier_componente()
	{
		var uno = new PuntoDePrueba(1, "ñandú");

		uno.Equals(new PuntoDePrueba(2, "ñandú")).ShouldBeFalse();
		uno.Equals(new PuntoDePrueba(1, "yacaré")).ShouldBeFalse();
	}

	#endregion

	#region Distinción por tipo y por null
	[Fact]
	public void Value_objects_de_distinto_tipo_concreto_nunca_son_iguales()
	{
		var punto = new PuntoDePrueba(1, "1");
		var par = new ParInvertidoDePrueba(1, "1");

		punto.Equals(par).ShouldBeFalse();
	}

	[Fact]
	public void Un_value_object_no_es_igual_a_null()
	{
		var punto = new PuntoDePrueba(1, "1");
		PuntoDePrueba? ninguno = null;

		punto.Equals(ninguno).ShouldBeFalse();
		(punto == ninguno).ShouldBeFalse();
	}
	#endregion

	#region GetHashCode
	[Fact]
	public void GetHashCode_es_estable_entre_llamadas()
	{
		var punto = new PuntoDePrueba(7, "árbol");

		punto.GetHashCode().ShouldBe(punto.GetHashCode());
	}

	[Fact]
	public void GetHashCode_difiere_para_componentes_distintos()
	{
		var uno = new PuntoDePrueba(7, "árbol");
		var otro = new PuntoDePrueba(8, "árbol");

		uno.GetHashCode().ShouldNotBe(otro.GetHashCode());
	}
	#endregion
}

#region Dobles de prueba
internal sealed class PuntoDePrueba : ValueObject
{
	public PuntoDePrueba(int x, string y)
	{
		X = x;
		Y = y;
	}

	public int X { get; }

	public string Y { get; }

	public override IEnumerable<object> GetEqualityCommponents()
	{
		yield return X;
		yield return Y;
	}
}

internal sealed class ParInvertidoDePrueba : ValueObject
{
	public ParInvertidoDePrueba(int x, string y)
	{
		X = x;
		Y = y;
	}

	public int X { get; }

	public string Y { get; }

	public override IEnumerable<object> GetEqualityCommponents()
	{
		yield return Y;
		yield return X;
	}
}
#endregion
