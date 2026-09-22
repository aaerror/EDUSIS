using Domain.Cursantes;
using Domain.Cursantes.Calificaciones;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Cursantes;

/// <summary>
/// Entidad hija <see cref="Calificacion"/> (del agregado <see cref="Cursante"/>) y el enumerado
/// <see cref="Instancia"/>: caminos válidos (asistencia / inasistencia), invariantes (fecha, nota,
/// materia, observación) e identidad. Sólo el <see cref="Cursante"/> crea calificaciones, así que
/// se construyen a través de él. <c>NotaIncorrectaException</c> y <c>SinDatosMateriaException</c>
/// son <c>internal</c>: se verifican por el nombre del tipo.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class CalificacionTests
{
	private static readonly Guid Materia = Guid.NewGuid();

	private static Calificacion Registrar(double? nota, string? observacion = null, DateTime? fecha = null, Guid? materia = null)
	{
		var cursante = new CursanteBuilder().Build();
		var id = cursante.RegistrarCalificacion(materia ?? Materia, fecha ?? DateTime.Today, Instancia.Parcial, nota, observacion);

		return cursante.Calificaciones.Single(x => x.Id.Equals(id));
	}

	#region Camino válido
	[Fact]
	public void Crear_con_asistencia_redondea_la_nota_a_dos_decimales()
	{
		var calificacion = Registrar(7.126, "Muy bien");

		calificacion.Rindio.ShouldBeTrue();
		calificacion.Nota!.Value.ShouldBe(7.13, 0.0001);
		calificacion.Instancia.ShouldBe(Instancia.Parcial);
		calificacion.MateriaID.ShouldBe(Materia);
	}

	[Fact]
	public void Inasistencia_no_lleva_nota()
	{
		var cursante = new CursanteBuilder().Build();

		var id = cursante.RegistrarInasistenciaAExamen(Materia, DateTime.Today, Instancia.Recuperatorio, "Ausente con aviso");

		var calificacion = cursante.Calificaciones.Single(x => x.Id.Equals(id));
		calificacion.Rindio.ShouldBeFalse();
		calificacion.Nota.ShouldBeNull();
	}

	[Theory]
	[InlineData(6, true)]
	[InlineData(10, true)]
	[InlineData(5.99, false)]
	[InlineData(5.5, false)]
	[InlineData(1, false)]
	public void EstaAprobado_requiere_nota_minima_de_6(double nota, bool aprobado)
	{
		Registrar(nota).EstaAprobado().ShouldBe(aprobado);
	}

	[Fact]
	public void EstaAprobado_es_false_si_no_se_presento_al_examen()
	{
		var cursante = new CursanteBuilder().Build();

		var id = cursante.RegistrarInasistenciaAExamen(Materia, DateTime.Today, Instancia.Parcial, null);

		cursante.Calificaciones.Single(x => x.Id.Equals(id)).EstaAprobado().ShouldBeFalse();
	}
	#endregion

	#region Invariantes
	[Fact]
	public void Una_fecha_de_examen_futura_lanza_ArgumentException()
	{
		Should.Throw<ArgumentException>(() => Registrar(8, fecha: DateTime.Today.AddDays(1)));
	}

	[Fact]
	public void Registrar_asistencia_sin_nota_lanza_ArgumentNullException()
	{
		Should.Throw<ArgumentNullException>(() => Registrar(null));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(11)]
	[InlineData(-3)]
	public void Una_nota_fuera_del_rango_1_a_10_lanza_NotaIncorrectaException(double nota)
	{
		var ex = Should.Throw<Exception>(() => Registrar(nota));

		ex.GetType().Name.ShouldBe("NotaIncorrectaException");
	}

	[Fact]
	public void Una_materia_vacia_lanza_SinDatosMateriaException()
	{
		var ex = Should.Throw<Exception>(() => Registrar(8, materia: Guid.Empty));

		ex.GetType().Name.ShouldBe("SinDatosMateriaException");
	}

	[Fact]
	public void Una_observacion_de_mas_de_140_caracteres_lanza_ArgumentException()
	{
		Should.Throw<ArgumentException>(() => Registrar(8, new string('á', 141)));
	}
	#endregion

	#region Identidad
	[Fact]
	public void Dos_calificaciones_con_los_mismos_datos_son_distintas()
	{
		var cursante = new CursanteBuilder().Build();

		var una = cursante.RegistrarCalificacion(Materia, DateTime.Today, Instancia.Parcial, 8, "obs");
		var otra = cursante.RegistrarCalificacion(Materia, DateTime.Today, Instancia.Parcial, 8, "obs");

		una.ShouldNotBe(otra);
		cursante.Calificaciones.Count.ShouldBe(2);
	}
	#endregion
}
