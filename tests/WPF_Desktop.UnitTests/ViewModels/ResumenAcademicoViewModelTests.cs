using Core.ServicioAlumnos.DTOs.Responses;
using Core.ServicioCursantes.DTOs.Responses;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using System;
using WPF_Desktop.ViewModels.Alumnos;
using Xunit;

namespace WPF_Desktop.UnitTests.ViewModels;

/// <summary>
/// Los textos de <see cref="ResumenAcademicoViewModel"/>.
/// </summary>
/// <remarks>
/// <para>
/// Se prueban los tres armadores <c>static</c> y el constructor, no la pantalla: los armadores son
/// la parte con lógica —pluralización, qué se omite cuando es cero, el caso sin datos— y son puros,
/// así que se ejercitan sin DI. Es el mismo criterio con el que se prueba
/// <c>GestionAlumnosViewModel.DescribirConsecuencia</c>.
/// </para>
/// <para>
/// Importa porque estos textos son los que hacen interpretable a un número. Un "7,42" sin "6
/// materias · 5 aprobadas · 1 ausencia a examen, que no promedian" al lado no dice sobre qué se
/// calculó, y el promedio con tres ausencias encima describe algo distinto que el mismo promedio
/// sin ninguna.
/// </para>
/// </remarks>
[Trait("Categoria", "Unidad")]
public class ResumenAcademicoViewModelTests
{
	private static InscripcionHistoricaResponse Cursada(
		string division = "B",
		string grado = "Tercero",
		string nivel = "Secundaria",
		string ciclo = "2026",
		bool recursante = false,
		bool activa = true,
		DateTime? inicio = null) =>
			new(CursanteID: Guid.NewGuid(),
				DivisionID: Guid.NewGuid(),
				Division: division,
				Grado: grado,
				NivelEducativo: nivel,
				CicloLectivo: ciclo,
				FechaInicio: inicio ?? new DateTime(2026, 3, 1),
				FechaFin: activa ? null : new DateTime(2026, 7, 1),
				EsRecursante: recursante,
				EstaActiva: activa);

	private static AsistenciaAcumuladaResponse Asistencia(
		int jornadas = 0,
		int presentes = 0,
		int ausencias = 0,
		int inasistencias = 0,
		int tardanzas = 0,
		decimal porcentaje = 0m) =>
			new(jornadas, presentes, ausencias, inasistencias, tardanzas, ausencias + inasistencias, porcentaje);

	private static MateriaCursadaResponse Materia(string nombre, decimal? promedio, bool aprobada, int rendidas = 1, int ausencias = 0) =>
		new(Guid.NewGuid(), nombre, rendidas, ausencias, promedio, aprobada);

	private static ResumenAcademicoResponse Resumen(
		string? ciclo = "2026",
		InscripcionHistoricaResponse? cursada = null,
		decimal? promedio = null,
		int rendidas = 0,
		int ausenciasAExamen = 0,
		int aprobadas = 0,
		int desaprobadas = 0,
		AsistenciaAcumuladaResponse? asistencia = null,
		IReadOnlyCollection<MateriaCursadaResponse>? materias = null,
		IReadOnlyCollection<InscripcionHistoricaResponse>? trayectoria = null) =>
			new(AlumnoID: Guid.NewGuid(),
				CicloLectivo: ciclo,
				Cursada: cursada,
				Promedio: promedio,
				Rendidas: rendidas,
				AusenciasAExamen: ausenciasAExamen,
				MateriasAprobadas: aprobadas,
				MateriasDesaprobadas: desaprobadas,
				Asistencia: asistencia ?? Asistencia(),
				Materias: materias ?? new List<MateriaCursadaResponse>(),
				Trayectoria: trayectoria ?? new List<InscripcionHistoricaResponse>());

	#region DescribirCursada
	/// <remarks>
	/// El grado sale de <c>Grados.Decir</c>, que lee el <c>[Description]</c> del enum. El enum se
	/// llama <c>Tercero</c>, así que concatenarle el símbolo daría "Tercero° año" — el defecto que
	/// la guarda estática <c>Nadie_pega_el_simbolo_de_grado_a_un_dato</c> vigila.
	/// </remarks>
	[Fact]
	public void DescribirCursada_dice_el_grado_como_lo_declara_el_dominio()
	{
		var texto = ResumenAcademicoViewModel.DescribirCursada(Cursada());

		texto.ShouldBe("3° Año B · Secundaria");
		texto.ShouldNotContain("Tercero");
	}

	/// <remarks>
	/// Un alumno sin inscripción vigente es un estado válido —egresado, baja, alta sin inscribir—.
	/// Devolver vacío haría que se vea igual que un perfil a medio cargar.
	/// </remarks>
	[Fact]
	public void DescribirCursada_sin_inscripcion_vigente_lo_dice_en_lugar_de_quedar_vacia()
	{
		var texto = ResumenAcademicoViewModel.DescribirCursada(null);

		texto.ShouldBe("No tiene una inscripción vigente.");
	}

	/// <remarks>
	/// Un curso borrado deja el nivel vacío —<c>Core</c> lo devuelve así a propósito, para que una
	/// división de 2021 que ya no está no impida abrir el perfil—. El texto no puede quedar con un
	/// separador suelto al final.
	/// </remarks>
	[Fact]
	public void DescribirCursada_sin_nivel_educativo_no_deja_el_separador_colgando()
	{
		var texto = ResumenAcademicoViewModel.DescribirCursada(Cursada(nivel: string.Empty));

		texto.ShouldBe("3° Año B");
		texto.ShouldNotEndWith("·");
	}
	#endregion

	#region DescribirCalificaciones
	[Fact]
	public void DescribirCalificaciones_sin_ninguna_nota_dice_que_no_hay_nada_cargado()
	{
		var texto = ResumenAcademicoViewModel.DescribirCalificaciones(Resumen());

		texto.ShouldBe("Todavía no se le cargó ninguna calificación en este ciclo.");
	}

	[Fact]
	public void DescribirCalificaciones_enumera_las_materias_y_como_se_reparten()
	{
		var texto = ResumenAcademicoViewModel.DescribirCalificaciones(Resumen(
			promedio: 7.42m,
			rendidas: 12,
			aprobadas: 5,
			desaprobadas: 1,
			materias: new[]
			{
				Materia("Historia", 5m, aprobada: false),
				Materia("Lengua", 8m, aprobada: true),
				Materia("Matemática", 7m, aprobada: true),
				Materia("Física", 9m, aprobada: true),
				Materia("Química", 6m, aprobada: true),
				Materia("Biología", 7m, aprobada: true)
			}));

		texto.ShouldBe("6 materias · 5 aprobadas · 1 desaprobada.");
	}

	/// <remarks>
	/// El singular no es cosmético: "1 materias aprobadas" al lado de un promedio le baja la
	/// credibilidad al número que acompaña.
	/// </remarks>
	[Fact]
	public void DescribirCalificaciones_concuerda_el_singular()
	{
		var texto = ResumenAcademicoViewModel.DescribirCalificaciones(Resumen(
			promedio: 8m,
			rendidas: 1,
			aprobadas: 1,
			materias: new[] { Materia("Lengua", 8m, aprobada: true) }));

		texto.ShouldBe("1 materia · 1 aprobada.");
	}

	/// <remarks>
	/// Las cifras en cero no se enumeran: un alumno con todas las materias aprobadas no lleva un
	/// "0 desaprobadas" detrás.
	/// </remarks>
	[Fact]
	public void DescribirCalificaciones_omite_las_cifras_en_cero()
	{
		var texto = ResumenAcademicoViewModel.DescribirCalificaciones(Resumen(
			promedio: 9m,
			rendidas: 3,
			aprobadas: 3,
			materias: new[]
			{
				Materia("Lengua", 9m, aprobada: true),
				Materia("Matemática", 9m, aprobada: true),
				Materia("Física", 9m, aprobada: true)
			}));

		texto.ShouldNotContain("desaprobada");
		texto.ShouldNotContain("ausencia");
	}

	/// <remarks>
	/// LA FRASE "QUE NO PROMEDIAN" ES EL PUNTO DE TODO EL TEXTO. La inasistencia a examen la guarda
	/// el dominio con <c>Nota</c> en nulo, así que no entra al promedio; sin decirlo, alguien lee
	/// 7,42 con tres ausencias y supone que ya están castigadas.
	/// </remarks>
	[Fact]
	public void DescribirCalificaciones_aclara_que_las_ausencias_a_examen_no_promedian()
	{
		var texto = ResumenAcademicoViewModel.DescribirCalificaciones(Resumen(
			promedio: 7.42m,
			rendidas: 4,
			ausenciasAExamen: 3,
			aprobadas: 2,
			materias: new[]
			{
				Materia("Lengua", 8m, aprobada: true),
				Materia("Matemática", 7m, aprobada: true, ausencias: 3)
			}));

		texto.ShouldContain("3 ausencias a examen, que no promedian");
	}

	/// <remarks>
	/// Un alumno que sólo faltó a los exámenes TIENE algo que informar —las ausencias— aunque no
	/// tenga promedio. Si este caso cayera en la rama de "no se le cargó nada", las ausencias
	/// desaparecerían de la pantalla.
	/// </remarks>
	[Fact]
	public void DescribirCalificaciones_informa_las_ausencias_aunque_no_haya_ninguna_nota()
	{
		var texto = ResumenAcademicoViewModel.DescribirCalificaciones(Resumen(
			ausenciasAExamen: 1,
			desaprobadas: 1,
			materias: new[] { Materia("Lengua", null, aprobada: false, rendidas: 0, ausencias: 1) }));

		texto.ShouldContain("1 ausencia a examen, que no promedian");
		texto.ShouldNotContain("Todavía no");
	}
	#endregion

	#region DescribirAsistencia
	[Fact]
	public void DescribirAsistencia_sin_jornadas_dice_que_no_se_tomo_asistencia()
	{
		var texto = ResumenAcademicoViewModel.DescribirAsistencia(Asistencia());

		texto.ShouldBe("Todavía no se le tomó asistencia en este ciclo.");
	}

	[Fact]
	public void DescribirAsistencia_sin_faltas_no_enumera_ceros()
	{
		var texto = ResumenAcademicoViewModel.DescribirAsistencia(
			Asistencia(jornadas: 118, presentes: 118, porcentaje: 100m));

		texto.ShouldBe("118 jornadas.");
	}

	/// <remarks>
	/// Las faltas van sumadas y repartidas: las dos son ausencia de clase para el porcentaje y
	/// decisiones distintas para el preceptor, porque una está justificada y la otra no.
	/// </remarks>
	[Fact]
	public void DescribirAsistencia_reparte_las_faltas_entre_justificadas_e_injustificadas()
	{
		var texto = ResumenAcademicoViewModel.DescribirAsistencia(
			Asistencia(jornadas: 118, presentes: 108, ausencias: 5, inasistencias: 2, tardanzas: 3, porcentaje: 94.1m));

		texto.ShouldBe("118 jornadas · 7 faltas (5 justificadas) · 3 tardanzas, que no descuentan.");
	}

	/// <remarks>
	/// Sin inasistencias injustificadas el reparto no aporta nada: "3 faltas (3 justificadas)"
	/// repite el mismo número dos veces.
	/// </remarks>
	[Fact]
	public void DescribirAsistencia_no_reparte_cuando_todas_las_faltas_son_del_mismo_tipo()
	{
		var texto = ResumenAcademicoViewModel.DescribirAsistencia(
			Asistencia(jornadas: 40, presentes: 37, ausencias: 3, porcentaje: 92.5m));

		texto.ShouldBe("40 jornadas · 3 faltas.");
		texto.ShouldNotContain("justificada");
	}

	/// <remarks>
	/// LA TARDANZA NO DESCUENTA y el texto lo dice. El alumno llegó tarde pero estuvo en clase, y
	/// sin la aclaración un 94% con tres tardanzas al lado se lee como si las hubiera descontado.
	/// </remarks>
	[Fact]
	public void DescribirAsistencia_aclara_que_la_tardanza_no_descuenta()
	{
		var texto = ResumenAcademicoViewModel.DescribirAsistencia(
			Asistencia(jornadas: 20, presentes: 19, tardanzas: 1, porcentaje: 100m));

		texto.ShouldBe("20 jornadas · 1 tardanza, que no descuentan.");
	}
	#endregion

	#region Constructor
	[Fact]
	public void El_promedio_nulo_se_muestra_como_una_raya_y_no_como_cero()
	{
		var vm = new ResumenAcademicoViewModel(Resumen(promedio: null));

		vm.TienePromedio.ShouldBeFalse();
		vm.Promedio.ShouldBe("—");
	}

	[Fact]
	public void Sin_registro_de_asistencia_el_porcentaje_se_muestra_como_una_raya()
	{
		var vm = new ResumenAcademicoViewModel(Resumen(asistencia: Asistencia()));

		vm.TieneAsistencia.ShouldBeFalse();
		vm.PorcentajeAsistencia.ShouldBe("—");
	}

	[Fact]
	public void Un_alumno_sin_ninguna_inscripcion_no_declara_ciclo_lectivo()
	{
		var vm = new ResumenAcademicoViewModel(Resumen(ciclo: null));

		vm.TieneTrayectoria.ShouldBeFalse();
		vm.EstaCursando.ShouldBeFalse();
		vm.CicloLectivo.ShouldBeEmpty();
	}

	/// <remarks>
	/// De la más reciente a la más vieja: el perfil se abre para ver dónde está el alumno hoy, no
	/// dónde empezó. <c>Core</c> las devuelve al revés, porque el historial de la división se lee
	/// en orden de cursada.
	/// </remarks>
	[Fact]
	public void La_trayectoria_se_ordena_de_la_inscripcion_mas_reciente_a_la_mas_vieja()
	{
		var vm = new ResumenAcademicoViewModel(Resumen(trayectoria: new[]
		{
			Cursada(ciclo: "2024", division: "A", activa: false, inicio: new DateTime(2024, 3, 1)),
			Cursada(ciclo: "2025", division: "B", activa: false, inicio: new DateTime(2025, 3, 1)),
			Cursada(ciclo: "2026", division: "C", inicio: new DateTime(2026, 3, 1))
		}));

		vm.Trayectoria.Select(x => x.CicloLectivo).ShouldBe(new[] { "2026", "2025", "2024" });
	}

	/// <remarks>
	/// La fila cerrada por traslado se rotula "Cerrada" y la vigente "Vigente": el vocabulario lo
	/// decide <c>InscripcionHistoricaViewModel.Estado</c>, no la grilla.
	/// </remarks>
	[Fact]
	public void La_trayectoria_distingue_la_inscripcion_vigente_de_las_cerradas()
	{
		var vm = new ResumenAcademicoViewModel(Resumen(trayectoria: new[]
		{
			Cursada(ciclo: "2026", division: "A", activa: false, inicio: new DateTime(2026, 3, 1)),
			Cursada(ciclo: "2026", division: "B", inicio: new DateTime(2026, 7, 1))
		}));

		vm.Trayectoria.Select(x => x.Estado).ShouldBe(new[] { "Vigente", "Cerrada" });
	}

	[Fact]
	public void Un_resumen_nulo_no_revienta_y_deja_la_ficha_en_su_estado_vacio()
	{
		var vm = new ResumenAcademicoViewModel(null);

		vm.TieneTrayectoria.ShouldBeFalse();
		vm.Promedio.ShouldBe("—");
		vm.PorcentajeAsistencia.ShouldBe("—");
		vm.Materias.ShouldBeEmpty();
		vm.Trayectoria.ShouldBeEmpty();
	}
	#endregion
}
