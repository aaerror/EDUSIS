using Core.ServicioAlumnos.DTOs.Responses;
using Shouldly;
using System;
using WPF_Desktop.ViewModels.Alumnos;
using Xunit;

namespace WPF_Desktop.UnitTests.ViewModels;

/// <summary>
/// El texto de la confirmación destructiva de <see cref="GestionAlumnosViewModel"/>.
/// </summary>
/// <remarks>
/// <para>
/// Se prueba <c>DescribirConsecuencia</c> y no el comando completo: es la parte con lógica
/// —pluralización, enumeración en castellano, el caso sin historial— y es <c>static</c> y pura, así
/// que se ejercita sin armar el ViewModel con sus cinco dependencias. El comando en sí es
/// orquestación: consultar, preguntar, borrar, limpiar.
/// </para>
/// <para>
/// Importa porque el texto que reemplaza <strong>mentía</strong>. Decía que "las inscripciones y
/// calificaciones ya registradas quedan en el historial de la división", cuando el borrado ni
/// siquiera llegaba a ejecutarse: <c>FK_CURSANTE_ALUMNO</c> es <c>Restrict</c> y fallaba contra la
/// clave ajena. Un diálogo destructivo que informa mal es peor que uno que no informa.
/// </para>
/// </remarks>
[Trait("Categoria", "Unidad")]
public class GestionAlumnosViewModelTests
{
	private const string DOCUMENTO = "40222333";

	private static ImpactoEliminacionAlumnoResponse Impacto(
		int inscripciones = 0,
		int calificaciones = 0,
		int registros = 0,
		int planillasVacias = 0,
		params string[] ciclos) =>
			new(inscripciones, calificaciones, registros, planillasVacias, ciclos);

	#region Sin trayectoria
	[Fact]
	public void DescribirConsecuencia_sin_historial_no_enumera_ceros()
	{
		var texto = GestionAlumnosViewModel.DescribirConsecuencia(Impacto(), DOCUMENTO);

		texto.ShouldContain("No tiene inscripciones ni calificaciones registradas");
		texto.ShouldContain(DOCUMENTO);
		texto.ShouldContain("no se puede deshacer");
		texto.ShouldNotContain("Se borran");
		texto.ShouldNotContain("0 ");
	}
	#endregion

	#region Con trayectoria
	[Fact]
	public void DescribirConsecuencia_enumera_las_tres_cifras_y_los_ciclos()
	{
		var texto = GestionAlumnosViewModel.DescribirConsecuencia(
			Impacto(inscripciones: 3, calificaciones: 47, registros: 312, ciclos: new[] { "2023", "2024", "2025" }),
			DOCUMENTO);

		texto.ShouldContain("Se borran 3 inscripciones, 47 calificaciones y 312 registros de asistencia");
		texto.ShouldContain("de los ciclos 2023, 2024 y 2025");
		texto.ShouldContain("no se puede deshacer");
	}

	/// <remarks>
	/// El singular no es cosmético: "1 inscripciones" en un diálogo que pide confirmar un borrado
	/// irreversible le baja la credibilidad a todo el texto.
	/// </remarks>
	[Fact]
	public void DescribirConsecuencia_usa_el_singular_cuando_hay_uno_de_cada()
	{
		var texto = GestionAlumnosViewModel.DescribirConsecuencia(
			Impacto(inscripciones: 1, calificaciones: 1, registros: 1, ciclos: new[] { "2025" }),
			DOCUMENTO);

		texto.ShouldContain("1 inscripción, 1 calificación y 1 registro de asistencia");
		texto.ShouldContain("del ciclo 2025");
		texto.ShouldNotContain("de los ciclos");
	}

	/// <remarks>
	/// Un alumno inscripto sin una sola nota ni un solo día de asistencia: la frase no puede quedar
	/// con una "y" suelta ni con cifras en cero.
	/// </remarks>
	[Fact]
	public void DescribirConsecuencia_omite_las_cifras_en_cero()
	{
		var texto = GestionAlumnosViewModel.DescribirConsecuencia(
			Impacto(inscripciones: 2, ciclos: new[] { "2025" }),
			DOCUMENTO);

		// Arranca con la cifra y pasa directo al ciclo: sin "y" suelta ni cifras en cero. No se
		// afirma la ausencia de " y " en todo el texto porque la frase del legajo tiene una
		// legítima ("su domicilio y su contacto").
		texto.ShouldStartWith("Se borran 2 inscripciones del ciclo 2025.");
		texto.ShouldNotContain("calificacion");
		texto.ShouldNotContain("registro");
	}
	#endregion

	#region Planillas que quedan vacías
	/// <remarks>
	/// Se informa aparte porque es consecuencia de otro orden: borrar la planilla es destruir la
	/// jornada completa de una división, no sólo la fila del alumno.
	/// </remarks>
	[Fact]
	public void DescribirConsecuencia_avisa_de_las_planillas_que_se_eliminan()
	{
		var texto = GestionAlumnosViewModel.DescribirConsecuencia(
			Impacto(inscripciones: 1, registros: 5, planillasVacias: 5, ciclos: new[] { "2025" }),
			DOCUMENTO);

		texto.ShouldContain("5 planillas de asistencia quedan sin ningún registro y también se eliminan.");
	}

	[Fact]
	public void DescribirConsecuencia_concuerda_el_singular_de_la_planilla_vaciada()
	{
		var texto = GestionAlumnosViewModel.DescribirConsecuencia(
			Impacto(inscripciones: 1, registros: 1, planillasVacias: 1, ciclos: new[] { "2025" }),
			DOCUMENTO);

		texto.ShouldContain("1 planilla de asistencia queda sin ningún registro y también se elimina.");
	}

	[Fact]
	public void DescribirConsecuencia_no_menciona_planillas_cuando_ninguna_queda_vacia()
	{
		var texto = GestionAlumnosViewModel.DescribirConsecuencia(
			Impacto(inscripciones: 1, registros: 9, planillasVacias: 0, ciclos: new[] { "2025" }),
			DOCUMENTO);

		texto.ShouldNotContain("sin ningún registro");
	}
	#endregion
}
