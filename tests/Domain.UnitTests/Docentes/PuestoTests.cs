using Domain.Docentes.Puestos;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Docentes;

/// <summary>
/// Entidad <see cref="Puesto"/> (cargo docente) y sus enumerados <see cref="EstadoPuesto"/> y
/// <see cref="Posicion"/>: alta, invariantes del período y de la posición, y ciclo de vida
/// (rescisión, paso a puesto fijo).
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class PuestoTests
{
	#region Alta
	[Fact]
	public void Un_puesto_nuevo_guarda_posicion_estado_y_periodo()
	{
		var puesto = new PuestoBuilder()
			.ConPosicion(nameof(Posicion.Preceptor))
			.ConEstado(nameof(EstadoPuesto.Pendiente))
			.ConDesde(DateTime.Today)
			.Build();

		puesto.Posicion.ShouldBe(Posicion.Preceptor);
		puesto.Estado.ShouldBe(EstadoPuesto.Pendiente);
		puesto.Periodo.FechaInicio.ShouldBe(DateTime.Today);
		puesto.EsEventual.ShouldBeFalse();
		puesto.EstaActivo().ShouldBeTrue();
	}

	[Fact]
	public void Un_puesto_con_fecha_de_fin_queda_marcado_como_eventual()
	{
		var puesto = new PuestoBuilder().ConHasta(DateTime.Today.AddMonths(6)).Build();

		puesto.EsEventual.ShouldBeTrue();
	}

	[Fact]
	public void Un_puesto_con_fecha_de_inicio_anterior_a_hoy_lanza_ArgumentException()
	{
		var builder = new PuestoBuilder().ConDesde(DateTime.Today.AddDays(-1));

		Should.Throw<ArgumentException>(() => builder.Build());
	}

	[Fact]
	public void Un_estado_de_puesto_fuera_del_enum_lanza_ArgumentException()
	{
		var builder = new PuestoBuilder().ConEstado("Vacaciones");

		Should.Throw<ArgumentException>(() => builder.Build());
	}

	[Fact]
	public void Una_posicion_fuera_del_enum_lanza_ArgumentException()
	{
		var builder = new PuestoBuilder().ConPosicion("Bibliotecario");

		Should.Throw<ArgumentException>(() => builder.Build());
	}
	#endregion

	#region Ciclo de vida
	[Fact]
	public void Rescindir_sin_fecha_pasa_el_puesto_a_inactivo_y_lo_cierra_hoy()
	{
		var puesto = new PuestoBuilder().Build();

		puesto.Rescindir();

		puesto.Estado.ShouldBe(EstadoPuesto.Inactivo);
		puesto.Periodo.FechaFin.ShouldBe(DateTime.Today);
	}

	[Fact]
	public void Rescindir_con_fecha_cierra_el_puesto_en_esa_fecha()
	{
		var puesto = new PuestoBuilder().Build();
		var fin = DateTime.Today.AddDays(10);

		puesto.Rescindir(fin);

		puesto.Estado.ShouldBe(EstadoPuesto.Inactivo);
		puesto.Periodo.FechaFin.ShouldBe(fin);
	}

	[Fact]
	public void EstablecerComoPuestoFijo_sobre_un_puesto_eventual_activo_lo_vuelve_indeterminado()
	{
		var puesto = new PuestoBuilder().ConHasta(DateTime.Today.AddMonths(6)).Build();

		puesto.EstablecerComoPuestoFijo();

		puesto.EsEventual.ShouldBeFalse();
		puesto.Periodo.FechaFin.ShouldBeNull();
	}

	[Fact]
	public void EstablecerComoPuestoFijo_sobre_un_puesto_indeterminado_lanza_ArgumentException()
	{
		var puesto = new PuestoBuilder().Build();

		Should.Throw<ArgumentException>(() => puesto.EstablecerComoPuestoFijo());
	}

	[Fact]
	public void ActualizarPuestoDocente_cambia_posicion_y_periodo()
	{
		var puesto = new PuestoBuilder().ConPosicion(nameof(Posicion.Profesor)).Build();

		puesto.ActualizarPuestoDocente(nameof(Posicion.Auxiliar), DateTime.Today, DateTime.Today.AddMonths(3));

		puesto.Posicion.ShouldBe(Posicion.Auxiliar);
		puesto.EsEventual.ShouldBeTrue();
	}
	#endregion
}
