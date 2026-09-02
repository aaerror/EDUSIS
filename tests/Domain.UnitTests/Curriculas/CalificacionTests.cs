using Domain.Curriculas.Materias;
using EDUSIS.TestSupport;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Curriculas;

/// <summary>
/// Value object <see cref="Calificacion"/> y el enumerado <see cref="Instancia"/>: caminos
/// válidos (asistencia / inasistencia), invariantes (fecha, nota, observación) e igualdad
/// estructural. <c>NotaIncorrectaException</c> es <c>internal</c>: se verifica por el nombre del
/// tipo.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class CalificacionTests
{
	private static readonly Guid Cursante = Guid.NewGuid();

	#region Camino válido
	[Fact]
	public void Crear_con_asistencia_redondea_la_nota_a_dos_decimales()
	{
		var calificacion = Calificacion.Crear(Cursante, DateTime.Today, Instancia.Parcial, 7.126, "Muy bien");

		calificacion.Asistencia.ShouldBeTrue();
		calificacion.Nota!.Value.ShouldBe(7.13, 0.0001);
		calificacion.Instancia.ShouldBe(Instancia.Parcial);
	}

	[Fact]
	public void Inasistencia_no_lleva_nota()
	{
		var calificacion = Calificacion.Inasistencia(Cursante, DateTime.Today, Instancia.Recuperatorio, "Ausente con aviso");

		calificacion.Asistencia.ShouldBeFalse();
		calificacion.Nota.ShouldBeNull();
	}

	[Fact]
	public void EstaAprobado_es_true_solo_con_asistencia_y_nota_mayor_a_5()
	{
		Calificacion.Crear(Cursante, DateTime.Today, Instancia.Parcial, 6, null).EstaAprobado().ShouldBeTrue();
		Calificacion.Crear(Cursante, DateTime.Today, Instancia.Parcial, 5, null).EstaAprobado().ShouldBeFalse();
		Calificacion.Inasistencia(Cursante, DateTime.Today, Instancia.Parcial, null).EstaAprobado().ShouldBeFalse();
	}

	[Fact]
	public void ModificarObservaciones_devuelve_una_copia_con_la_nueva_observacion()
	{
		var original = Calificacion.Crear(Cursante, DateTime.Today, Instancia.Parcial, 8, "Primera");

		var modificada = original.ModificarObservaciones("Segunda");

		modificada.Observacion.ShouldBe("Segunda");
		modificada.Nota.ShouldBe(original.Nota);
	}
	#endregion

	#region Invariantes
	[Fact]
	public void Una_fecha_de_examen_futura_lanza_ArgumentException()
	{
		Should.Throw<ArgumentException>(() =>
			Calificacion.Crear(Cursante, DateTime.Today.AddDays(1), Instancia.Parcial, 8, null));
	}

	[Fact]
	public void Registrar_asistencia_sin_nota_lanza_ArgumentNullException()
	{
		Should.Throw<ArgumentNullException>(() =>
			Calificacion.Crear(Cursante, DateTime.Today, Instancia.Parcial, null, null));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(11)]
	[InlineData(-3)]
	public void Una_nota_fuera_del_rango_1_a_10_lanza_NotaIncorrectaException(double nota)
	{
		var ex = Should.Throw<Exception>(() =>
			Calificacion.Crear(Cursante, DateTime.Today, Instancia.Parcial, nota, null));

		ex.GetType().Name.ShouldBe("NotaIncorrectaException");
	}

	[Fact]
	public void Una_observacion_de_mas_de_140_caracteres_lanza_ArgumentException()
	{
		var observacion = new string('á', 141);

		Should.Throw<ArgumentException>(() =>
			Calificacion.Crear(Cursante, DateTime.Today, Instancia.Parcial, 8, observacion));
	}
	#endregion

	#region Igualdad estructural
	[Fact]
	public void Dos_calificaciones_con_los_mismos_componentes_son_iguales()
	{
		var fecha = DateTime.Today;

		var una = Calificacion.Crear(Cursante, fecha, Instancia.Parcial, 8, "obs");
		var otra = Calificacion.Crear(Cursante, fecha, Instancia.Parcial, 8, "obs");

		una.Equals(otra).ShouldBeTrue();
		una.GetHashCode().ShouldBe(otra.GetHashCode());
	}

	[Fact]
	public void Cambiar_la_nota_rompe_la_igualdad()
	{
		var fecha = DateTime.Today;

		var una = Calificacion.Crear(Cursante, fecha, Instancia.Parcial, 8, null);
		var otra = Calificacion.Crear(Cursante, fecha, Instancia.Parcial, 9, null);

		una.Equals(otra).ShouldBeFalse();
	}
	#endregion
}
