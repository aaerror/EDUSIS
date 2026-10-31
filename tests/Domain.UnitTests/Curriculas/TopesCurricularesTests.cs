using Domain.Curriculas;
using Domain.Curriculas.Exceptions;
using EDUSIS.TestSupport;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Curriculas;

/// <summary>
/// <see cref="TopesCurriculares"/>: las dos invariantes que gobiernan cuánto contenido admite una
/// currícula. Vivían como constantes privadas de <c>ServicioMateria</c>, en la capa de aplicación.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class TopesCurricularesTests
{
	#region ExigirAltaPosible
	[Fact]
	public void Un_alta_dentro_de_los_dos_topes_no_lanza()
	{
		Should.NotThrow(() => TopesCurriculares.ExigirAltaPosible(espaciosOcupados: 5, horasOcupadas: 20, horasAIncorporar: 4));
	}

	[Fact]
	public void El_espacio_catorce_todavia_entra()
	{
		Should.NotThrow(() => TopesCurriculares.ExigirAltaPosible(espaciosOcupados: 13, horasOcupadas: 0, horasAIncorporar: 1));
	}

	[Fact]
	public void El_espacio_quince_lanza_LimiteEspaciosCurricularesException()
	{
		Should.Throw<LimiteEspaciosCurricularesException>(
			() => TopesCurriculares.ExigirAltaPosible(espaciosOcupados: 14, horasOcupadas: 0, horasAIncorporar: 1));
	}

	[Fact]
	public void Un_alta_que_excede_las_horas_lanza_LimiteHorasSemanalesException()
	{
		Should.Throw<LimiteHorasSemanalesException>(
			() => TopesCurriculares.ExigirAltaPosible(espaciosOcupados: 1, horasOcupadas: 38, horasAIncorporar: 3));
	}

	[Fact]
	public void El_tope_de_espacios_se_verifica_antes_que_el_de_horas()
	{
		// Con los dos topes excedidos gana el de espacios: es la condición más fuerte, porque sin
		// espacio disponible la carga horaria ya no importa.
		Should.Throw<LimiteEspaciosCurricularesException>(
			() => TopesCurriculares.ExigirAltaPosible(espaciosOcupados: 14, horasOcupadas: 40, horasAIncorporar: 5));
	}
	#endregion

	#region ExigirCargaHorariaPosible
	[Fact]
	public void Completar_exactamente_las_cuarenta_horas_no_lanza()
	{
		Should.NotThrow(() => TopesCurriculares.ExigirCargaHorariaPosible(horasOcupadas: 36, horasAIncorporar: 4));
	}

	[Fact]
	public void La_hora_cuarenta_y_uno_lanza_LimiteHorasSemanalesException()
	{
		Should.Throw<LimiteHorasSemanalesException>(
			() => TopesCurriculares.ExigirCargaHorariaPosible(horasOcupadas: 36, horasAIncorporar: 5));
	}
	#endregion

	[Fact]
	public void Los_topes_conservan_los_valores_que_tenia_ServicioMateria()
	{
		TopesCurriculares.MAXIMO_ESPACIOS_CURRICULARES.ShouldBe(14);
		TopesCurriculares.MAXIMO_HORAS_CATEDRA_SEMANALES.ShouldBe(40);
	}
}
