using Domain.Cursos;
using Domain.Shared;
using EDUSIS.TestSupport;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Cursos;

/// <summary>
/// <see cref="EdadEscolar"/>: la regla que relaciona la edad del alumno con el grado al que se
/// inscribe. Advierte y no bloquea, así que lo que se afirma son veredictos y no excepciones.
/// </summary>
/// <remarks>
/// Nació de dirimir <c>H-004</c> / <c>DOM-11</c> el 2026-11-12: la excepción que esa fila
/// registraba, <c>AlumnoMayorDeEdadException</c>, nombraba una condición <em>absoluta</em> ("el
/// alumno cumplió la mayoría de edad") para una regla que es <em>relacional</em> — un alumno de 18
/// en sexto de secundaria es normal y en primero no—, y además ya estaba cubierta como consulta por
/// <c>DatosPersonales.EsMayorDeEdad()</c>. Se borró junto con su prueba placeholder vacía.
/// </remarks>
[Trait("Categoria", Categorias.Unidad)]
public class EdadEscolarTests
{
	#region EdadEsperada
	// La duración del nivel no entra en la cuenta: primero es a los 6 en una primaria de 6 años y
	// en una de 7. Lo que cambia entre provincias es hasta qué grado llega el nivel.
	[Theory]
	[InlineData(Grado.Primero, 6)]
	[InlineData(Grado.Sexto, 11)]
	[InlineData(Grado.Septimo, 12)]
	public void La_edad_esperada_de_primaria_arranca_en_6_y_sube_con_el_grado(Grado grado, int esperada)
	{
		EdadEscolar.EdadEsperada(NivelEducativo.Primaria, grado).ShouldBe(esperada);
	}

	// Coincide con el dato real: ingreso a los 12/13 y finalización entre los 17 y 18.
	[Theory]
	[InlineData(Grado.Primero, 12)]
	[InlineData(Grado.Quinto, 16)]
	[InlineData(Grado.Sexto, 17)]
	public void La_edad_esperada_de_secundaria_arranca_en_12_y_sube_con_el_grado(Grado grado, int esperada)
	{
		EdadEscolar.EdadEsperada(NivelEducativo.Secundaria, grado).ShouldBe(esperada);
	}

	[Fact]
	public void La_edad_de_ingreso_sale_del_nivel()
	{
		EdadEscolar.EdadDeIngreso(NivelEducativo.Primaria).ShouldBe(6);
		EdadEscolar.EdadDeIngreso(NivelEducativo.Secundaria).ShouldBe(12);
	}
	#endregion

	#region Evaluar — banda normal
	[Fact]
	public void La_edad_esperada_esta_encuadrada()
	{
		EdadEscolar.Evaluar(12, NivelEducativo.Secundaria, Grado.Primero)
			.ShouldBe(EncuadreEtario.Encuadrado);
	}

	/// <remarks>
	/// Un año por debajo NO es desvío, y no por tolerancia de criterio sino porque
	/// <c>DatosPersonales.Edad()</c> mide contra hoy y no contra el inicio del ciclo: el alumno
	/// nacido en diciembre tiene 11 durante casi todo su primer año de secundaria. Si esta prueba
	/// se pone en rojo, el veredicto pasó a depender del mes en que se lo mire.
	/// </remarks>
	[Fact]
	public void Un_anio_por_debajo_sigue_encuadrado_por_el_mes_de_nacimiento()
	{
		EdadEscolar.Evaluar(11, NivelEducativo.Secundaria, Grado.Primero)
			.ShouldBe(EncuadreEtario.Encuadrado);
	}

	[Fact]
	public void La_sobreedad_tolerada_entera_sigue_encuadrada()
	{
		// 12 esperada + 4 tolerados: dos repeticiones, o una repetición y un reingreso.
		EdadEscolar.Evaluar(16, NivelEducativo.Secundaria, Grado.Primero)
			.ShouldBe(EncuadreEtario.Encuadrado);
	}
	#endregion

	#region Evaluar — desvíos
	[Fact]
	public void Un_anio_mas_que_la_sobreedad_tolerada_advierte_sobreedad()
	{
		EdadEscolar.Evaluar(17, NivelEducativo.Secundaria, Grado.Primero)
			.ShouldBe(EncuadreEtario.ConSobreedad);
	}

	// Los dos casos que motivaron la regla, los dos +6 sobre la esperada.
	[Fact]
	public void Un_alumno_de_12_en_primero_de_primaria_advierte_sobreedad()
	{
		EdadEscolar.Evaluar(12, NivelEducativo.Primaria, Grado.Primero)
			.ShouldBe(EncuadreEtario.ConSobreedad);
	}

	[Fact]
	public void Un_alumno_de_18_en_primero_de_secundaria_advierte_sobreedad()
	{
		EdadEscolar.Evaluar(18, NivelEducativo.Secundaria, Grado.Primero)
			.ShouldBe(EncuadreEtario.ConSobreedad);
	}

	[Fact]
	public void Dos_anios_por_debajo_advierte_edad_insuficiente()
	{
		EdadEscolar.Evaluar(10, NivelEducativo.Secundaria, Grado.Primero)
			.ShouldBe(EncuadreEtario.PorDebajoDeLaEdadEsperada);
	}

	[Fact]
	public void Un_alumno_de_6_en_primero_de_secundaria_advierte_edad_insuficiente()
	{
		EdadEscolar.Evaluar(6, NivelEducativo.Secundaria, Grado.Primero)
			.ShouldBe(EncuadreEtario.PorDebajoDeLaEdadEsperada);
	}

	// El mismo alumno de 18 que en primero es un desvío, en sexto está encuadrado: es lo que la
	// excepción absoluta que esta regla reemplaza no podía expresar.
	[Fact]
	public void Un_alumno_de_18_en_sexto_de_secundaria_esta_encuadrado()
	{
		EdadEscolar.Evaluar(18, NivelEducativo.Secundaria, Grado.Sexto)
			.ShouldBe(EncuadreEtario.Encuadrado);
	}
	#endregion

	#region AniosDeDesvio
	[Fact]
	public void El_desvio_se_mide_contra_la_esperada_y_no_contra_el_borde_de_la_banda()
	{
		// 18 en primero de secundaria: +6 sobre la esperada, no +2 sobre el borde.
		EdadEscolar.AniosDeDesvio(18, NivelEducativo.Secundaria, Grado.Primero).ShouldBe(6);
		EdadEscolar.AniosDeDesvio(10, NivelEducativo.Secundaria, Grado.Primero).ShouldBe(-2);
		EdadEscolar.AniosDeDesvio(12, NivelEducativo.Secundaria, Grado.Primero).ShouldBe(0);
	}
	#endregion

	#region Constantes
	[Fact]
	public void Las_constantes_de_la_politica_son_las_declaradas()
	{
		EdadEscolar.EDAD_INGRESO_PRIMARIA.ShouldBe(6);
		EdadEscolar.EDAD_INGRESO_SECUNDARIA.ShouldBe(12);
		EdadEscolar.SOBREEDAD_TOLERADA.ShouldBe(4);
		EdadEscolar.ANIOS_DE_VARIANZA_POR_MES_DE_NACIMIENTO.ShouldBe(1);
	}
	#endregion
}
