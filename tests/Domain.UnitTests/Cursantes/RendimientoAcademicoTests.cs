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
/// Son reglas ENTRE agregados —el traslado de división reparte las instancias de una materia entre
/// dos <see cref="Cursante"/>— y por eso tienen dueño propio en el dominio, igual que
/// <c>TopesCurriculares</c>. Las pruebas las ejercitan con listas de calificaciones armadas a mano,
/// que es exactamente cómo las llama la capa de aplicación: sin agregado de por medio.
/// </remarks>
[Trait("Categoria", Categorias.Unidad)]
public class RendimientoAcademicoTests
{
	private static readonly Guid Materia = Guid.NewGuid();

	/// <summary>
	/// Calificaciones sueltas, construidas por el único camino público que tiene el dominio.
	/// </summary>
	/// <remarks>
	/// Pasan por un <see cref="Cursante"/> porque <c>Calificacion.Crear</c> es <c>internal</c>, no
	/// porque la regla necesite uno: lo que se le entrega a la política es la colección, y de qué
	/// cursante salió cada elemento es justamente lo que no le importa.
	/// </remarks>
	private static IReadOnlyCollection<Calificacion> Notas(params double?[] notas)
	{
		var cursante = new CursanteBuilder().Build();

		foreach (var nota in notas)
		{
			if (nota is null)
			{
				cursante.RegistrarInasistenciaAExamen(Materia, DateTime.Today, Instancia.Parcial, null);
			}
			else
			{
				cursante.RegistrarCalificacion(Materia, DateTime.Today, Instancia.Parcial, nota, null);
			}
		}

		return cursante.Calificaciones;
	}

	#region Rendidas y ausencias
	[Fact]
	public void Rendidas_son_las_instancias_con_nota()
	{
		var calificaciones = Notas(8, null, 6, null);

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
		var calificaciones = Notas(10, null, 1, 7, null);

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
		var ninguna = Array.Empty<Calificacion>();

		RendimientoAcademico.Rendidas(ninguna).ShouldBeEmpty();
		RendimientoAcademico.AusenciasAExamen(ninguna).ShouldBeEmpty();
	}
	#endregion

	#region Promediar
	[Fact]
	public void Promediar_devuelve_la_media_de_las_notas()
	{
		RendimientoAcademico.Promediar(Notas(8, 6)).ShouldBe(7m);
	}

	/// <remarks>
	/// LA REGLA CENTRAL DEL PROMEDIO: la inasistencia a examen no entra. El dominio la guarda con
	/// <c>Nota</c> en nulo a propósito, así que contarla obligaría a inventarle un número. Con 8 y 6
	/// el promedio es 7, y lo sigue siendo con tres ausencias encima.
	/// </remarks>
	[Fact]
	public void Promediar_no_cuenta_las_ausencias_a_examen()
	{
		RendimientoAcademico.Promediar(Notas(8, 6, null, null, null)).ShouldBe(7m);
	}

	/// <remarks>
	/// Nulo y no cero: un alumno sin notas no tiene cero de promedio, y quien lo informe tiene que
	/// poder decir "sin calificaciones" en lugar del peor promedio posible.
	/// </remarks>
	[Fact]
	public void Promediar_sin_ninguna_nota_rendida_devuelve_nulo()
	{
		RendimientoAcademico.Promediar(Notas(null, null)).ShouldBeNull();
		RendimientoAcademico.Promediar(Array.Empty<Calificacion>()).ShouldBeNull();
	}

	[Fact]
	public void Promediar_redondea_a_dos_decimales()
	{
		// 8 + 7 + 7 = 22 / 3 = 7,3333…
		RendimientoAcademico.Promediar(Notas(8, 7, 7)).ShouldBe(7.33m);
	}

	/// <remarks>
	/// Redondeo <c>AwayFromZero</c> y no el bancario de .NET, que es el predeterminado de
	/// <c>Math.Round</c>: con el bancario, 7,125 daría 7,12 y no 7,13, y un promedio que redondea
	/// "para el lado par" no es el que nadie espera leer en un boletín.
	/// </remarks>
	[Fact]
	public void Promediar_redondea_hacia_arriba_el_punto_medio()
	{
		// 7 + 7,25 = 14,25 / 2 = 7,125 → 7,13
		RendimientoAcademico.Promediar(Notas(7, 7.25)).ShouldBe(7.13m);
	}
	#endregion

	#region EstaAprobada
	[Fact]
	public void Una_materia_con_el_promedio_en_el_umbral_esta_aprobada()
	{
		Calificacion.NOTA_APROBACION.ShouldBe(6);

		RendimientoAcademico.EstaAprobada(Notas(6)).ShouldBeTrue();
		RendimientoAcademico.EstaAprobada(Notas(7, 5)).ShouldBeTrue();
	}

	[Fact]
	public void Una_materia_con_el_promedio_debajo_del_umbral_no_esta_aprobada()
	{
		RendimientoAcademico.EstaAprobada(Notas(5.99)).ShouldBeFalse();
	}

	/// <remarks>
	/// EL CRITERIO ELEGIDO, Y LA PRUEBA QUE AVISA SI ALGUIEN LO CAMBIA SIN DECIDIRLO. Aprueba por
	/// el PROMEDIO de las instancias, no por la última nota ni por "cualquier instancia aprobada".
	/// Un 4 y después un 7 dan 5,5 y no aprueban; con los otros dos criterios aprobarían.
	/// </remarks>
	[Fact]
	public void Aprueba_por_promedio_y_no_por_la_ultima_nota_ni_por_cualquier_instancia()
	{
		var recuperada = Notas(4, 7);

		RendimientoAcademico.Promediar(recuperada).ShouldBe(5.5m);
		RendimientoAcademico.EstaAprobada(recuperada).ShouldBeFalse();

		// La última nota aprueba por sí sola y una instancia aprobada existe: con cualquiera de los
		// otros dos criterios esta materia estaría aprobada.
		recuperada.Last().EstaAprobado().ShouldBeTrue();
		recuperada.Any(x => x.EstaAprobado()).ShouldBeTrue();
	}

	/// <remarks>
	/// Sin notas no está aprobada, y el motivo importa: el promedio es nulo, no cero. Un alumno que
	/// faltó a los dos exámenes no aprobó, pero tampoco le fue mal.
	/// </remarks>
	[Fact]
	public void Una_materia_sin_ninguna_nota_rendida_no_esta_aprobada()
	{
		RendimientoAcademico.EstaAprobada(Notas(null, null)).ShouldBeFalse();
		RendimientoAcademico.Promediar(Notas(null, null)).ShouldBeNull();
	}

	/// <remarks>
	/// Las ausencias no bajan el promedio, así que tampoco pueden desaprobar una materia que el
	/// promedio aprueba. Es la consecuencia de la regla, y se fija acá para que quede explícita:
	/// quien quiera que la ausencia castigue tiene que cambiar <c>EstaAprobada</c>, no descubrirlo.
	/// </remarks>
	[Fact]
	public void Las_ausencias_a_examen_no_desaprueban_una_materia_aprobada()
	{
		RendimientoAcademico.EstaAprobada(Notas(8, null, null, null)).ShouldBeTrue();
	}
	#endregion
}
