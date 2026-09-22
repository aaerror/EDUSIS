using System.Reflection;
using Domain.Catedras;
using Domain.Catedras.SituacionesRevista;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Curriculas;

/// <summary>
/// Entidad <see cref="SituacionRevista"/> (cargo docente sobre una cátedra) y el enumerado
/// <see cref="EstadoSituacionRevista"/>: invariantes del constructor y finalización. El
/// constructor y <c>Crear</c> son <c>internal</c> — sólo <c>Domain.Catedras.Catedra</c>
/// instancia situaciones de revista —, así que <see cref="SituacionRevistaBuilder"/> designa
/// sobre una <c>Catedra</c> auxiliar y expone el resultado. Las funciones de aula (el slot
/// "en funciones") y los horarios pasaron a <c>Domain.Catedras.Catedra</c>: su cobertura vive en
/// <c>CatedraTests</c>. Las excepciones del módulo son <c>internal</c>: se verifican por el
/// nombre del tipo.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class SituacionRevistaTests
{
	#region Alta
	[Fact]
	public void El_constructor_sin_docente_lanza_ArgumentNullException()
	{
		Should.Throw<ArgumentNullException>(() => new SituacionRevistaBuilder().ConDocente(Guid.Empty).Build());
	}

	[Fact]
	public void Una_situacion_de_revista_titular_indeterminada_arranca_aceptada()
	{
		var situacion = new SituacionRevistaBuilder().ConCargo(Cargo.Titular).Build();

		situacion.Estado.ShouldBe(EstadoSituacionRevista.Aceptado);
		situacion.Cargo.ShouldBe(Cargo.Titular);
		situacion.EsTemporal().ShouldBeFalse();
		situacion.EsCargoVigente().ShouldBeTrue();
		situacion.ReemplazaA.ShouldBeNull();
	}

	[Theory]
	[InlineData(nameof(Cargo.Interino))]
	[InlineData(nameof(Cargo.Suplente))]
	public void Un_cargo_temporal_sin_fecha_de_fin_lanza_CargoTemporalSinFechaFinalizacionException(string cargo)
	{
		var builder = new SituacionRevistaBuilder().ConCargo(Enum.Parse<Cargo>(cargo)).ConFechaFin(null);

		var ex = Should.Throw<Exception>(() => builder.Build());

		ex.GetType().Name.ShouldBe("CargoTemporalSinFechaFinalizacionException");
	}
	#endregion

	#region Finalización
	[Fact]
	public void EstablecerFechaFinalizacion_sobre_un_cargo_temporal_lanza_CargoConFechaFinalizacionException()
	{
		var catedra = new CatedraBuilder().Build();
		var designacion = catedra.Designar(Guid.NewGuid(), Cargo.Titular, DateTime.Today, DateTime.Today.AddMonths(3));

		var ex = Should.Throw<Exception>(() => catedra.EstablecerFinDeDesignacion(designacion, DateTime.Today.AddMonths(6)));

		ex.GetType().Name.ShouldBe("CargoConFechaFinalizacionException");
	}

	[Fact]
	public void Finalizar_un_cargo_vigente_lo_deja_finalizado_y_cerrado_hoy()
	{
		var catedra = new CatedraBuilder().Build();
		var designacion = catedra.Designar(Guid.NewGuid(), Cargo.Titular, DateTime.Today, null);

		catedra.FinalizarDesignacion(designacion);

		var situacion = catedra.SituacionesRevista.Single(x => x.Id.Equals(designacion));
		situacion.Estado.ShouldBe(EstadoSituacionRevista.Finalizado);
		situacion.Periodo.FechaFin.ShouldBe(DateTime.Today);
	}

	[Fact]
	public void Finalizar_un_cargo_que_todavia_no_inicio_lanza_CargoNoIniciadoException()
	{
		var catedra = new CatedraBuilder().Build();
		var designacion = catedra.Designar(Guid.NewGuid(), Cargo.Titular, DateTime.Today.AddDays(5), null);

		var ex = Should.Throw<Exception>(() => catedra.FinalizarDesignacion(designacion));

		ex.GetType().Name.ShouldBe("CargoNoIniciadoException");
	}

	[Fact]
	public void Los_mutadores_de_la_designacion_no_son_alcanzables_sin_pasar_por_la_catedra()
	{
		// La raíz es el único punto de entrada: los mutadores son internal.
		var publicos = typeof(SituacionRevista)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
			.Select(metodo => metodo.Name)
			.ToList();

		publicos.ShouldNotContain("Finalizar");
		publicos.ShouldNotContain("EstablecerFechaFinalizacion");
	}
	#endregion

	#region EstadoSituacionRevista
	[Fact]
	public void EstadoSituacionRevista_expone_los_estados_conocidos_con_su_descripcion()
	{
		EstadoSituacionRevista.EMPTY.Descripcion.ShouldBe("EMPTY");
		EstadoSituacionRevista.Aceptado.Descripcion.ShouldBe("Aceptado");
		EstadoSituacionRevista.Finalizado.Descripcion.ShouldBe("Finalizado");
	}

	[Fact]
	public void EstadoSituacionRevista_compara_por_identidad_de_enumeracion()
	{
		EstadoSituacionRevista.Aceptado.ShouldBe(EstadoSituacionRevista.Aceptado);
		EstadoSituacionRevista.Aceptado.Equals(EstadoSituacionRevista.Finalizado).ShouldBeFalse();
	}
	#endregion
}
