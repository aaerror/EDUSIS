using Domain.Cursantes;
using EDUSIS.TestSupport;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Cursantes;

/// <summary>
/// Value object <see cref="CicloLectivo"/>: formato del período (4 dígitos), año en curso e
/// igualdad estructural.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class CicloLectivoTests
{
	#region Camino válido
	[Fact]
	public void Crear_con_un_periodo_de_4_digitos_lo_conserva()
	{
		var ciclo = CicloLectivo.Crear("2026");

		ciclo.Periodo.ShouldBe("2026");
	}

	[Fact]
	public void AñoEnCurso_es_true_para_el_año_actual()
	{
		CicloLectivo.Crear(DateTime.Now.Year.ToString()).AñoEnCurso().ShouldBeTrue();
	}

	[Fact]
	public void AñoEnCurso_es_false_para_otro_año()
	{
		CicloLectivo.Crear("1999").AñoEnCurso().ShouldBeFalse();
	}
	#endregion

	#region Invariantes
	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void Crear_sin_periodo_lanza_ArgumentNullException(string periodo)
	{
		Should.Throw<ArgumentNullException>(() => CicloLectivo.Crear(periodo));
	}

	[Theory]
	[InlineData("26")]
	[InlineData("20260")]
	[InlineData("dosmil")]
	[InlineData("20 6")]
	public void Crear_con_un_periodo_que_no_son_4_digitos_lanza_FormatException(string periodo)
	{
		Should.Throw<FormatException>(() => CicloLectivo.Crear(periodo));
	}
	#endregion

	#region Igualdad estructural
	[Fact]
	public void Dos_ciclos_lectivos_con_el_mismo_periodo_son_iguales()
	{
		var uno = CicloLectivo.Crear("2026");
		var otro = CicloLectivo.Crear("2026");

		uno.Equals(otro).ShouldBeTrue();
		uno.GetHashCode().ShouldBe(otro.GetHashCode());
	}

	[Fact]
	public void Ciclos_lectivos_de_distinto_periodo_no_son_iguales()
	{
		CicloLectivo.Crear("2025").Equals(CicloLectivo.Crear("2026")).ShouldBeFalse();
	}
	#endregion
}
