using Domain.Cursantes.Calificaciones;
using Domain.Cursantes;
using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using System;
using Xunit;

namespace Domain.UnitTests.Cursantes;

/// <summary>
/// <see cref="RendimientoAcademico"/>: las reglas que deciden cómo le fue a un alumno. Vivían como
/// métodos privados de <c>ServicioAlumno</c>, en la capa de aplicación.
/// </summary>
/// <remarks>
/// <para>
/// Son reglas ENTRE agregados —el traslado de división reparte las instancias de una materia entre
/// dos <see cref="Cursante"/>— y por eso tienen dueño propio en el dominio, igual que
/// <c>TopesCurriculares</c>. Las pruebas las ejercitan con listas de calificaciones armadas a mano,
/// que es exactamente cómo las llama la capa de aplicación: sin agregado de por medio.
/// </para>
/// <para>
/// Lo que fijan es la estructura real del ciclo lectivo: dos cuatrimestres con su recuperatorio,
/// una nota final que carga el docente, y dos períodos de recuperación. El modelo anterior tenía un
/// solo eje —<c>Instancia { Parcial, Recuperatorio }</c>— y con él la única regla posible era
/// promediar todo junto, que no es la de ninguna institución.
/// </para>
/// </remarks>
[Trait("Categoria", Categorias.Unidad)]
public class RendimientoAcademicoTests
{
	private static readonly Guid Materia = Guid.NewGuid();

	private static readonly IReadOnlyCollection<Calificacion> Ninguna = Array.Empty<Calificacion>();

	/// <summary>
	/// Las calificaciones de una materia, construidas por el único camino público que tiene el
	/// dominio y en orden cronológico.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Pasan por un <see cref="Cursante"/> porque <c>Calificacion.Crear</c> es <c>internal</c>, no
	/// porque la regla necesite uno: lo que se le entrega a la política es la colección, y de qué
	/// cursante salió cada elemento es justamente lo que no le importa.
	/// </para>
	/// <para>
	/// Cada registro avanza un día desde una fecha vieja, así que el orden en que se escriben las
	/// llamadas ES el orden cronológico. Importa porque varias reglas resuelven empates por fecha,
	/// y porque <see cref="Calificacion"/> rechaza una fecha posterior a hoy.
	/// </para>
	/// </remarks>
	private sealed class Libreta
	{
		private readonly Cursante _cursante = new CursanteBuilder().Build();
		private DateTime _fecha = DateTime.Today.AddDays(-200);

		public Libreta Nota(Periodo periodo, double nota)
		{
			_cursante.RegistrarCalificacion(Materia, Avanzar(), periodo, esRecuperatorio: false, nota, null);
			return this;
		}

		public Libreta Recuperatorio(Periodo periodo, double nota)
		{
			_cursante.RegistrarCalificacion(Materia, Avanzar(), periodo, esRecuperatorio: true, nota, null);
			return this;
		}

		public Libreta Ausente(Periodo periodo, bool esRecuperatorio = false)
		{
			_cursante.RegistrarInasistenciaAExamen(Materia, Avanzar(), periodo, esRecuperatorio, null);
			return this;
		}

		public IReadOnlyCollection<Calificacion> Calificaciones => _cursante.Calificaciones;

		private DateTime Avanzar() => _fecha = _fecha.AddDays(1);
	}

	private static Libreta Libro() => new();

	#region Rendidas y ausencias
	[Fact]
	public void Rendidas_son_las_instancias_con_nota()
	{
		var calificaciones = Libro().Nota(Periodo.PrimerCuatrimestre, 8)
									.Ausente(Periodo.PrimerCuatrimestre, esRecuperatorio: true)
									.Nota(Periodo.SegundoCuatrimestre, 6)
									.Ausente(Periodo.Final)
									.Calificaciones;

		RendimientoAcademico.Rendidas(calificaciones).Count().ShouldBe(2);
		RendimientoAcademico.AusenciasAExamen(calificaciones).Count().ShouldBe(2);
	}

	/// <remarks>
	/// Las dos consultas parten el conjunto sin solaparse ni perder nada: una instancia es rendida
	/// o es ausencia, no hay un tercer estado. Si alguna vez lo hubiera, esta prueba lo avisa.
	/// </remarks>
	[Fact]
	public void Rendidas_y_ausencias_reparten_todas_las_calificaciones_sin_solaparse()
	{
		var calificaciones = Libro().Nota(Periodo.PrimerCuatrimestre, 10)
									.Ausente(Periodo.PrimerCuatrimestre, esRecuperatorio: true)
									.Nota(Periodo.SegundoCuatrimestre, 1)
									.Recuperatorio(Periodo.SegundoCuatrimestre, 7)
									.Ausente(Periodo.PrimeraRecuperacion)
									.Calificaciones;

		int rendidas = RendimientoAcademico.Rendidas(calificaciones).Count();
		int ausencias = RendimientoAcademico.AusenciasAExamen(calificaciones).Count();

		(rendidas + ausencias).ShouldBe(calificaciones.Count);
		RendimientoAcademico.Rendidas(calificaciones)
							.Intersect(RendimientoAcademico.AusenciasAExamen(calificaciones))
							.ShouldBeEmpty();
	}

	[Fact]
	public void Sin_calificaciones_las_dos_consultas_devuelven_vacio()
	{
		RendimientoAcademico.Rendidas(Ninguna).ShouldBeEmpty();
		RendimientoAcademico.AusenciasAExamen(Ninguna).ShouldBeEmpty();
	}
	#endregion

	#region Nota de un período
	/// <remarks>
	/// LA REGLA DEL RECUPERATORIO: reemplaza a la nota del período. No promedian ni queda la mejor.
	/// Un 4 recuperado con 7 deja el cuatrimestre en 7, y el 4 no vuelve a aparecer en ninguna
	/// cuenta.
	/// </remarks>
	[Fact]
	public void El_recuperatorio_reemplaza_la_nota_del_periodo()
	{
		var calificaciones = Libro().Nota(Periodo.PrimerCuatrimestre, 4)
									.Recuperatorio(Periodo.PrimerCuatrimestre, 7)
									.Calificaciones;

		RendimientoAcademico.NotaDelPeriodo(calificaciones, Periodo.PrimerCuatrimestre).ShouldBe(7m);
	}

	/// <remarks>
	/// LA CONTRACARA, Y LA QUE DISTINGUE "REEMPLAZA" DE "VALE LA MEJOR": el recuperatorio también
	/// reemplaza cuando es peor. Con 8 y un recuperatorio de 6 el cuatrimestre vale 6. Si alguien
	/// cambiara la regla por "la más alta", esta prueba es la única que lo detecta.
	/// </remarks>
	[Fact]
	public void El_recuperatorio_reemplaza_aunque_sea_peor_que_la_nota_original()
	{
		var calificaciones = Libro().Nota(Periodo.PrimerCuatrimestre, 8)
									.Recuperatorio(Periodo.PrimerCuatrimestre, 6)
									.Calificaciones;

		RendimientoAcademico.NotaDelPeriodo(calificaciones, Periodo.PrimerCuatrimestre).ShouldBe(6m);
	}

	/// <remarks>
	/// Faltar al recuperatorio no borra lo que ya se había rendido: una inasistencia no es una nota,
	/// así que el período se queda con su nota original. Lo contrario castigaría dos veces.
	/// </remarks>
	[Fact]
	public void Faltar_al_recuperatorio_deja_vigente_la_nota_original()
	{
		var calificaciones = Libro().Nota(Periodo.PrimerCuatrimestre, 4)
									.Ausente(Periodo.PrimerCuatrimestre, esRecuperatorio: true)
									.Calificaciones;

		RendimientoAcademico.NotaDelPeriodo(calificaciones, Periodo.PrimerCuatrimestre).ShouldBe(4m);
	}

	/// <remarks>
	/// El recuperatorio de un cuatrimestre no toca al otro. Es lo que el modelo anterior no podía
	/// expresar: con un solo <c>Recuperatorio</c> no había forma de saber a qué cuatrimestre
	/// pertenecía, y los cuatro recuperatorios de un ciclo se confundían entre sí.
	/// </remarks>
	[Fact]
	public void El_recuperatorio_de_un_cuatrimestre_no_afecta_al_otro()
	{
		var calificaciones = Libro().Nota(Periodo.PrimerCuatrimestre, 4)
									.Recuperatorio(Periodo.PrimerCuatrimestre, 9)
									.Nota(Periodo.SegundoCuatrimestre, 5)
									.Calificaciones;

		RendimientoAcademico.NotaDelPeriodo(calificaciones, Periodo.PrimerCuatrimestre).ShouldBe(9m);
		RendimientoAcademico.NotaDelPeriodo(calificaciones, Periodo.SegundoCuatrimestre).ShouldBe(5m);
	}

	[Fact]
	public void Con_mas_de_una_nota_en_el_mismo_periodo_vale_la_ultima_por_fecha()
	{
		var calificaciones = Libro().Nota(Periodo.PrimerCuatrimestre, 2)
									.Nota(Periodo.PrimerCuatrimestre, 9)
									.Calificaciones;

		RendimientoAcademico.NotaDelPeriodo(calificaciones, Periodo.PrimerCuatrimestre).ShouldBe(9m);
	}

	[Fact]
	public void Un_periodo_sin_nada_rendido_no_tiene_nota()
	{
		var calificaciones = Libro().Ausente(Periodo.SegundoCuatrimestre).Calificaciones;

		RendimientoAcademico.NotaDelPeriodo(calificaciones, Periodo.SegundoCuatrimestre).ShouldBeNull();
		RendimientoAcademico.NotaDelPeriodo(calificaciones, Periodo.Final).ShouldBeNull();
		RendimientoAcademico.NotaDelPeriodo(Ninguna, Periodo.PrimerCuatrimestre).ShouldBeNull();
	}
	#endregion

	#region Promedio de cuatrimestres
	[Fact]
	public void El_promedio_de_cuatrimestres_es_la_media_de_los_dos()
	{
		var calificaciones = Libro().Nota(Periodo.PrimerCuatrimestre, 8)
									.Nota(Periodo.SegundoCuatrimestre, 6)
									.Calificaciones;

		RendimientoAcademico.PromedioDeCuatrimestres(calificaciones).ShouldBe(7m);
	}

	/// <remarks>
	/// Promedia los cuatrimestres que EXISTEN y no divide siempre por dos: a mitad de año el
	/// segundo no se tomó, y dividir por dos daría la mitad de la nota del primero.
	/// </remarks>
	[Fact]
	public void A_mitad_de_ciclo_el_promedio_es_la_nota_del_unico_cuatrimestre_rendido()
	{
		var calificaciones = Libro().Nota(Periodo.PrimerCuatrimestre, 8).Calificaciones;

		RendimientoAcademico.PromedioDeCuatrimestres(calificaciones).ShouldBe(8m);
	}

	/// <remarks>
	/// Los períodos de recuperación y la nota final NO entran al promedio de cuatrimestres: son
	/// otra cosa, y mezclarlos es exactamente el defecto que tenía la versión anterior.
	/// </remarks>
	[Fact]
	public void El_promedio_de_cuatrimestres_ignora_la_nota_final_y_las_recuperaciones()
	{
		var calificaciones = Libro().Nota(Periodo.PrimerCuatrimestre, 8)
									.Nota(Periodo.SegundoCuatrimestre, 6)
									.Nota(Periodo.Final, 10)
									.Nota(Periodo.PrimeraRecuperacion, 1)
									.Calificaciones;

		RendimientoAcademico.PromedioDeCuatrimestres(calificaciones).ShouldBe(7m);
	}

	[Fact]
	public void Sin_cuatrimestres_rendidos_el_promedio_es_nulo()
	{
		RendimientoAcademico.PromedioDeCuatrimestres(Libro().Ausente(Periodo.PrimerCuatrimestre).Calificaciones).ShouldBeNull();
		RendimientoAcademico.PromedioDeCuatrimestres(Ninguna).ShouldBeNull();
	}

	/// <remarks>
	/// Redondeo <c>AwayFromZero</c> y no el bancario de .NET, que es el predeterminado de
	/// <c>Math.Round</c>: con el bancario, 7,125 daría 7,12 y no 7,13, y un promedio que redondea
	/// "para el lado par" no es el que nadie espera leer en un boletín.
	/// </remarks>
	[Fact]
	public void El_promedio_redondea_hacia_arriba_el_punto_medio()
	{
		// 7 + 7,25 = 14,25 / 2 = 7,125 → 7,13
		var calificaciones = Libro().Nota(Periodo.PrimerCuatrimestre, 7)
									.Nota(Periodo.SegundoCuatrimestre, 7.25)
									.Calificaciones;

		RendimientoAcademico.PromedioDeCuatrimestres(calificaciones).ShouldBe(7.13m);
	}
	#endregion

	#region Nota de recuperación
	/// <remarks>
	/// Las dos recuperaciones dan un solo número y vale la segunda: a ella se llega sólo después de
	/// no aprobar la primera, así que es la última palabra sobre la materia.
	/// </remarks>
	[Fact]
	public void La_segunda_recuperacion_le_gana_a_la_primera()
	{
		var calificaciones = Libro().Nota(Periodo.PrimeraRecuperacion, 4)
									.Nota(Periodo.SegundaRecuperacion, 7)
									.Calificaciones;

		RendimientoAcademico.NotaDeRecuperacion(calificaciones).ShouldBe(7m);
	}

	[Fact]
	public void Con_una_sola_recuperacion_vale_esa()
	{
		var calificaciones = Libro().Nota(Periodo.PrimeraRecuperacion, 6).Calificaciones;

		RendimientoAcademico.NotaDeRecuperacion(calificaciones).ShouldBe(6m);
	}

	/// <remarks>
	/// Un recuperatorio DENTRO de un período de recuperación también reemplaza: esa instancia
	/// admite nota y recuperatorio, igual que un cuatrimestre.
	/// </remarks>
	[Fact]
	public void El_recuperatorio_de_una_recuperacion_reemplaza_su_nota()
	{
		var calificaciones = Libro().Nota(Periodo.PrimeraRecuperacion, 4)
									.Recuperatorio(Periodo.PrimeraRecuperacion, 8)
									.Calificaciones;

		RendimientoAcademico.NotaDeRecuperacion(calificaciones).ShouldBe(8m);
	}

	[Fact]
	public void Sin_recuperaciones_rendidas_no_hay_nota_de_recuperacion()
	{
		RendimientoAcademico.NotaDeRecuperacion(Libro().Nota(Periodo.Final, 8).Calificaciones).ShouldBeNull();
		RendimientoAcademico.NotaDeRecuperacion(Ninguna).ShouldBeNull();
	}
	#endregion

	#region Nota de cierre
	/// <remarks>
	/// LA PRECEDENCIA, QUE ES LA REGLA CENTRAL: cierra el período más avanzado que tenga nota, y
	/// recién si no hay ninguno el promedio de los cuatrimestres. El orden sigue al calendario, así
	/// que lo último que se rindió es lo que vale.
	/// </remarks>
	[Fact]
	public void Sin_nota_final_ni_recuperaciones_cierra_el_promedio_de_cuatrimestres()
	{
		var calificaciones = Libro().Nota(Periodo.PrimerCuatrimestre, 8)
									.Nota(Periodo.SegundoCuatrimestre, 6)
									.Calificaciones;

		RendimientoAcademico.NotaDeCierre(calificaciones).ShouldBe(7m);
	}

	/// <remarks>
	/// La nota final la CARGA el docente y pisa al promedio incluso cuando es distinta: con 4 y 8 el
	/// promedio sería 6 y acá cerró en 9. No es una cuenta del sistema sino su decisión, y por eso
	/// el modelo la guarda como una calificación más en lugar de derivarla.
	/// </remarks>
	[Fact]
	public void La_nota_final_le_gana_al_promedio_de_cuatrimestres()
	{
		var calificaciones = Libro().Nota(Periodo.PrimerCuatrimestre, 4)
									.Nota(Periodo.SegundoCuatrimestre, 8)
									.Nota(Periodo.Final, 9)
									.Calificaciones;

		RendimientoAcademico.PromedioDeCuatrimestres(calificaciones).ShouldBe(6m);
		RendimientoAcademico.NotaDeCierre(calificaciones).ShouldBe(9m);
	}

	[Fact]
	public void La_recuperacion_le_gana_a_la_nota_final()
	{
		var calificaciones = Libro().Nota(Periodo.PrimerCuatrimestre, 2)
									.Nota(Periodo.SegundoCuatrimestre, 2)
									.Nota(Periodo.Final, 2)
									.Nota(Periodo.PrimeraRecuperacion, 6)
									.Calificaciones;

		RendimientoAcademico.NotaDeCierre(calificaciones).ShouldBe(6m);
	}

	/// <remarks>
	/// Y le gana incluso cuando es PEOR: a una segunda recuperación no se llega habiendo aprobado,
	/// así que si hay nota ahí es porque es la que decide. El caso no debería existir en los datos,
	/// y la regla no depende de que no exista.
	/// </remarks>
	[Fact]
	public void La_segunda_recuperacion_le_gana_a_todo()
	{
		var calificaciones = Libro().Nota(Periodo.PrimerCuatrimestre, 10)
									.Nota(Periodo.SegundoCuatrimestre, 10)
									.Nota(Periodo.Final, 10)
									.Nota(Periodo.PrimeraRecuperacion, 9)
									.Nota(Periodo.SegundaRecuperacion, 4)
									.Calificaciones;

		RendimientoAcademico.NotaDeCierre(calificaciones).ShouldBe(4m);
	}

	[Fact]
	public void Sin_nada_rendido_no_hay_nota_de_cierre()
	{
		RendimientoAcademico.NotaDeCierre(Libro().Ausente(Periodo.Final).Calificaciones).ShouldBeNull();
		RendimientoAcademico.NotaDeCierre(Ninguna).ShouldBeNull();
	}
	#endregion

	#region EstaAprobada
	[Fact]
	public void Una_materia_que_cierra_en_el_umbral_esta_aprobada()
	{
		Calificacion.NOTA_APROBACION.ShouldBe(6);

		RendimientoAcademico.EstaAprobada(Libro().Nota(Periodo.Final, 6).Calificaciones).ShouldBeTrue();
		RendimientoAcademico.EstaAprobada(Libro().Nota(Periodo.PrimerCuatrimestre, 7)
												 .Nota(Periodo.SegundoCuatrimestre, 5)
												 .Calificaciones).ShouldBeTrue();
	}

	[Fact]
	public void Una_materia_que_cierra_debajo_del_umbral_no_esta_aprobada()
	{
		RendimientoAcademico.EstaAprobada(Libro().Nota(Periodo.Final, 5.99).Calificaciones).ShouldBeFalse();
	}

	/// <remarks>
	/// APROBAR EN EL RECUPERATORIO ES APROBAR: el 4 del cuatrimestre fue reemplazado, así que no
	/// arrastra nada. Es la regla que el boletín ya venía aplicando por su cuenta en
	/// <c>ContextoAcademico</c> —"alguna calificación aprobada aprueba la materia"— mientras el
	/// perfil del alumno promediaba los dos intentos y la daba por desaprobada.
	/// </remarks>
	[Fact]
	public void Aprobar_en_el_recuperatorio_del_cuatrimestre_aprueba_la_materia()
	{
		var calificaciones = Libro().Nota(Periodo.PrimerCuatrimestre, 4)
									.Recuperatorio(Periodo.PrimerCuatrimestre, 7)
									.Calificaciones;

		RendimientoAcademico.EstaAprobada(calificaciones).ShouldBeTrue();
	}

	/// <remarks>
	/// Pero NO alcanza con haber aprobado alguna instancia, que es el criterio que aplicaba el
	/// boletín: si el recuperatorio dejó el período debajo del umbral, la materia no está aprobada
	/// aunque la nota original la alcanzara.
	/// </remarks>
	[Fact]
	public void Haber_aprobado_una_instancia_no_alcanza_si_el_cierre_no_llega()
	{
		var calificaciones = Libro().Nota(Periodo.PrimerCuatrimestre, 8)
									.Recuperatorio(Periodo.PrimerCuatrimestre, 2)
									.Calificaciones;

		calificaciones.First().EstaAprobado().ShouldBeTrue();
		RendimientoAcademico.EstaAprobada(calificaciones).ShouldBeFalse();
	}

	/// <remarks>
	/// Sin notas no está aprobada, y el motivo importa: la nota de cierre es nula, no cero. Un
	/// alumno que faltó a todos los exámenes no aprobó, pero tampoco le fue mal.
	/// </remarks>
	[Fact]
	public void Una_materia_sin_ninguna_nota_rendida_no_esta_aprobada()
	{
		var calificaciones = Libro().Ausente(Periodo.PrimerCuatrimestre)
									.Ausente(Periodo.SegundoCuatrimestre)
									.Calificaciones;

		RendimientoAcademico.EstaAprobada(calificaciones).ShouldBeFalse();
		RendimientoAcademico.NotaDeCierre(calificaciones).ShouldBeNull();
	}

	/// <remarks>
	/// Las ausencias no bajan ninguna nota de período, así que tampoco pueden desaprobar una
	/// materia que el cierre aprueba. Es la consecuencia de la regla y se fija acá para que quede
	/// explícita: quien quiera que la ausencia castigue tiene que cambiar la regla, no descubrirlo.
	/// </remarks>
	[Fact]
	public void Las_ausencias_a_examen_no_desaprueban_una_materia_aprobada()
	{
		var calificaciones = Libro().Nota(Periodo.PrimerCuatrimestre, 8)
									.Ausente(Periodo.SegundoCuatrimestre)
									.Ausente(Periodo.Final)
									.Calificaciones;

		RendimientoAcademico.EstaAprobada(calificaciones).ShouldBeTrue();
	}
	#endregion

	#region Promedio general
	/// <remarks>
	/// UNA MATERIA PESA UNA VEZ, con la nota que la cerró. Promediar las instancias sueltas le daría
	/// más peso a la materia con más exámenes tomados: acá Matemática aporta tres notas y Lengua
	/// una, y el promedio de las cuatro daría 8,5 en lugar de 7.
	/// </remarks>
	[Fact]
	public void El_promedio_general_promedia_las_notas_de_cierre_y_no_las_instancias()
	{
		var matematica = Libro().Nota(Periodo.PrimerCuatrimestre, 10)
								.Nota(Periodo.SegundoCuatrimestre, 10)
								.Nota(Periodo.Final, 8)
								.Calificaciones;
		var lengua = Libro().Nota(Periodo.Final, 6).Calificaciones;

		RendimientoAcademico.PromediarNotasDeCierre(new[] { matematica, lengua }).ShouldBe(7m);
	}

	/// <remarks>
	/// Una materia sin nota de cierre no entra ni arrastra: no es un cero. Con una cerrada en 8 y
	/// otra sin rendir nada, el promedio es 8 y no 4.
	/// </remarks>
	[Fact]
	public void Una_materia_sin_cierre_no_entra_al_promedio_general()
	{
		var cerrada = Libro().Nota(Periodo.Final, 8).Calificaciones;
		var sinRendir = Libro().Ausente(Periodo.PrimerCuatrimestre).Calificaciones;

		RendimientoAcademico.PromediarNotasDeCierre(new[] { cerrada, sinRendir }).ShouldBe(8m);
	}

	[Fact]
	public void Sin_ninguna_materia_cerrada_el_promedio_general_es_nulo()
	{
		RendimientoAcademico.PromediarNotasDeCierre(Array.Empty<IReadOnlyCollection<Calificacion>>()).ShouldBeNull();
		RendimientoAcademico.PromediarNotasDeCierre(new[] { Ninguna }).ShouldBeNull();
	}
	#endregion
}
