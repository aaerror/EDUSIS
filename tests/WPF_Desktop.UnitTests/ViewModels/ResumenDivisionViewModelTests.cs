using Core.ServicioDivisiones.DTOs.Responses;
using Shouldly;
using WPF_Desktop.ViewModels.Cursos.Divisiones;
using Xunit;

namespace WPF_Desktop.UnitTests.ViewModels;

/// <summary>
/// Los textos de la ficha de la división: qué se nombra, cómo se pluraliza y qué se calla cuando no
/// hay nada que decir.
/// </summary>
[Trait("Categoria", "Unidad")]
public class ResumenDivisionViewModelTests
{
	private static ResumenDivisionResponse Resumen(
		int inscriptos = 20,
		int recursantes = 0,
		int bajas = 0,
		bool enCurso = true,
		EstadoPlanillaDelDia? planillaDeHoy = EstadoPlanillaDelDia.SinAbrir,
		bool? preceptorDeLicencia = false,
		int? catedrasSinDocente = 0,
		int? catedrasConSuplente = 0,
		int? horasSinAsignar = 0,
		string[]? materiasSinCatedra = null,
		int planillas = 0,
		int planillasSinCerrar = 0,
		decimal? porcentaje = null,
		int faltas = 0,
		int tardanzas = 0,
		decimal? promedio = null,
		int desaprobados = 0,
		int ausenciasAExamen = 0,
		params AlumnoConFaltasResponse[] conFaltas) =>
		new(CicloLectivo: "2026",
			EsCicloEnCurso: enCurso,
			Inscriptos: inscriptos,
			Cupo: 35,
			Recursantes: recursantes,
			Bajas: bajas,
			PlanillaDeHoy: planillaDeHoy,
			PreceptorDeLicencia: preceptorDeLicencia,
			CatedrasSinDocente: catedrasSinDocente,
			CatedrasConSuplente: catedrasConSuplente,
			HorasSinAsignar: horasSinAsignar,
			MateriasSinCatedra: materiasSinCatedra ?? (enCurso ? Array.Empty<string>() : null),
			Planillas: planillas,
			PlanillasSinCerrar: planillasSinCerrar,
			PorcentajeAsistencia: porcentaje,
			Faltas: faltas,
			Tardanzas: tardanzas,
			AlumnosConMasFaltas: conFaltas,
			Promedio: promedio,
			AlumnosConMateriasDesaprobadas: desaprobados,
			AusenciasAExamen: ausenciasAExamen);

	[Fact]
	public void La_matricula_se_informa_contra_el_cupo_y_pluraliza_recursantes_y_bajas()
	{
		var ficha = new ResumenDivisionViewModel(Resumen(inscriptos: 28, recursantes: 1, bajas: 2));

		ficha.Matricula.ShouldBe("28 / 35");
		ficha.DetalleMatricula.ShouldBe("1 recursante · 2 bajas en el ciclo");
	}

	[Fact]
	public void La_matricula_completa_avisa_el_cupo_completo()
	{
		ResumenDivisionViewModel.DescribirMatricula(Resumen(inscriptos: 35)).ShouldBe("cupo completo");
	}

	[Fact]
	public void Sin_planillas_la_asistencia_es_una_raya_y_lo_dice()
	{
		var ficha = new ResumenDivisionViewModel(Resumen());

		ficha.PorcentajeAsistencia.ShouldBe("—");
		ficha.DetalleAsistencia.ShouldBe("Todavía no se tomó asistencia en este ciclo.");
	}

	[Fact]
	public void La_asistencia_aclara_que_las_tardanzas_no_descuentan()
	{
		ResumenDivisionViewModel.DescribirAsistencia(Resumen(planillas: 40, porcentaje: 92.5m, faltas: 1, tardanzas: 5))
			.ShouldBe("sobre 40 planillas · 1 falta · 5 tardanzas, que no descuentan");
	}

	[Fact]
	public void Sin_notas_de_cierre_el_promedio_es_una_raya_y_lo_dice()
	{
		var ficha = new ResumenDivisionViewModel(Resumen());

		ficha.Promedio.ShouldBe("—");
		ficha.DetalleRendimiento.ShouldBe("Todavía no hay notas de cierre en este ciclo.");
	}

	[Fact]
	public void El_rendimiento_dice_que_es_el_promedio_de_los_alumnos_y_nombra_los_desaprobados()
	{
		ResumenDivisionViewModel.DescribirRendimiento(Resumen(promedio: 7m, desaprobados: 3, ausenciasAExamen: 1))
			.ShouldBe("promedio de los alumnos · 3 alumnos con materias desaprobadas · 1 ausencia a examen");
	}

	/// <remarks>
	/// Sólo se nombra lo que existe, y en orden de gravedad: lo que impide tomar asistencia primero,
	/// las tareas administrativas al final.
	/// </remarks>
	[Fact]
	public void Los_pendientes_nombran_solo_lo_que_hay_de_lo_mas_grave_a_lo_menos()
	{
		var pendientes = ResumenDivisionViewModel.DescribirPendientes(Resumen(
			preceptorDeLicencia: true,
			catedrasSinDocente: 2,
			horasSinAsignar: 1,
			materiasSinCatedra: new[] { "Historia", "Lengua" },
			planillasSinCerrar: 1));

		pendientes.ShouldBe(new[]
		{
			"El preceptor está de licencia.",
			"2 cátedras sin docente en funciones.",
			"1 planilla anterior sin cerrar.",
			"1 hora de cátedra sin horario.",
			"Sin cátedra: Historia, Lengua."
		});
	}

	[Fact]
	public void Sin_pendientes_la_lista_queda_vacia()
	{
		var ficha = new ResumenDivisionViewModel(Resumen());

		ficha.Pendientes.ShouldBeEmpty();
		ficha.TienePendientes.ShouldBeFalse();
	}

	/// <remarks>
	/// En un ciclo cerrado Core no informa el estado de hoy, pero las planillas que quedaron abiertas
	/// siguen siendo un pendiente.
	/// </remarks>
	[Fact]
	public void En_un_ciclo_pasado_no_hay_planilla_de_hoy_pero_si_planillas_sin_cerrar()
	{
		var ficha = new ResumenDivisionViewModel(Resumen(
			enCurso: false,
			planillaDeHoy: null,
			preceptorDeLicencia: null,
			catedrasSinDocente: null,
			catedrasConSuplente: null,
			horasSinAsignar: null,
			planillasSinCerrar: 3));

		ficha.EsCicloEnCurso.ShouldBeFalse();
		ficha.PlanillaDeHoy.ShouldBeEmpty();
		ficha.Pendientes.ShouldBe(new[] { "3 planillas anteriores sin cerrar." });
	}

	[Fact]
	public void La_planilla_de_hoy_se_dice_con_palabras()
	{
		new ResumenDivisionViewModel(Resumen(planillaDeHoy: EstadoPlanillaDelDia.Abierta)).PlanillaDeHoy.ShouldBe("Abierta");
	}
}
