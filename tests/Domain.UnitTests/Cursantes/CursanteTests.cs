using Domain.Cursantes;
using Domain.Cursantes.Asistencias;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Cursantes;

/// <summary>
/// Agregado <see cref="Cursante"/>: alta con su ciclo lectivo y registro de asistencias con el
/// conteo por tipo de <see cref="Falta"/>. <c>AsistenciaRegistradaException</c> es
/// <c>internal</c>: se verifica por el nombre del tipo.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class CursanteTests
{
	#region Alta
	[Fact]
	public void Un_cursante_nuevo_guarda_alumno_ciclo_lectivo_y_fecha_de_inicio()
	{
		var alumnoID = Guid.NewGuid();
		var ciclo = new CicloLectivoBuilder().ConAño(2026).Build();

		var cursante = new CursanteBuilder().ConAlumno(alumnoID).ConCicloLectivo(ciclo).ConFechaInicio(new DateTime(2026, 3, 1)).Build();

		cursante.AlumnoID.ShouldBe(alumnoID);
		cursante.CicloLectivo.ShouldBe(ciclo);
		cursante.FechaInicio.ShouldBe(new DateTime(2026, 3, 1));
		cursante.EsRecursante.ShouldBeFalse();
		cursante.Asistencias.ShouldBeEmpty();
	}

	[Fact]
	public void Un_cursante_recursante_queda_marcado_como_tal()
	{
		var cursante = new CursanteBuilder().ComoRecursante().Build();

		cursante.EsRecursante.ShouldBeTrue();
	}

	[Fact]
	public void Un_cursante_sin_alumno_lanza_NullReferenceException()
	{
		var ciclo = new CicloLectivoBuilder().Build();

		Should.Throw<NullReferenceException>(() => new Cursante(Guid.Empty, ciclo, DateTime.Today));
	}

	[Fact]
	public void Un_cursante_sin_ciclo_lectivo_lanza_NullReferenceException()
	{
		Should.Throw<NullReferenceException>(() => new Cursante(Guid.NewGuid(), null!, DateTime.Today));
	}
	#endregion

	#region Asistencias
	[Fact]
	public void RegistrarAsistencia_clasifica_cada_falta_en_su_contador()
	{
		var cursante = new CursanteBuilder().Build();

		cursante.RegistrarAsistencia(new DateTime(2026, 3, 10), Falta.Ausencia, null, "Sin aviso");
		cursante.RegistrarAsistencia(new DateTime(2026, 3, 11), Falta.Inasistencia, null, "Con aviso");
		cursante.RegistrarAsistencia(new DateTime(2026, 3, 12), Falta.Tardanza, TimeSpan.FromMinutes(15), "Colectivo");

		cursante.Ausencias.ShouldBe(1);
		cursante.Inasistencias.ShouldBe(1);
		cursante.Tardanzas.ShouldBe(1);
		cursante.Asistencias.Count.ShouldBe(3);
	}

	[Fact]
	public void RegistrarAsistencia_dos_veces_en_la_misma_fecha_lanza_AsistenciaRegistradaException()
	{
		var cursante = new CursanteBuilder().Build();
		cursante.RegistrarAsistencia(new DateTime(2026, 3, 10), Falta.Ausencia, null, "Sin aviso");

		var ex = Should.Throw<Exception>(() =>
			cursante.RegistrarAsistencia(new DateTime(2026, 3, 10), Falta.Tardanza, TimeSpan.FromMinutes(5), "Otra"));

		ex.GetType().Name.ShouldBe("AsistenciaRegistradaException");
	}

	[Fact]
	public void El_builder_precarga_las_asistencias_configuradas()
	{
		var cursante = new CursanteBuilder()
			.ConAsistencia(new AsistenciaBuilder().ConFecha(new DateTime(2026, 4, 1)).ComoAusencia().Build())
			.ConAsistencia(new AsistenciaBuilder().ConFecha(new DateTime(2026, 4, 2)).ComoTardanza(TimeSpan.FromMinutes(10)).Build())
			.Build();

		cursante.Ausencias.ShouldBe(1);
		cursante.Tardanzas.ShouldBe(1);
	}
	#endregion
}
