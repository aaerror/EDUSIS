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

	/// <summary>
	/// La otra mitad del mismo invariante. "El cargo a término es interino o suplente" es una
	/// equivalencia, y el alta tenía escrita sólo la dirección de arriba: podía crear un titular con
	/// fecha de fin, que es exactamente el estado que <c>PasarAInterinoHasta</c> rechaza.
	/// </summary>
	[Fact]
	public void Un_titular_con_fecha_de_fin_lanza_TitularConFechaFinalizacionException()
	{
		var builder = new SituacionRevistaBuilder().ConCargo(Cargo.Titular).ConFechaFin(DateTime.Today.AddMonths(3));

		var ex = Should.Throw<Exception>(() => builder.Build());

		ex.GetType().Name.ShouldBe("TitularConFechaFinalizacionException");
	}

	/// <summary>El titular SÍ se designa sin fecha de fin: su período es indeterminado.</summary>
	[Fact]
	public void Un_titular_sin_fecha_de_fin_se_designa_por_tiempo_indeterminado()
	{
		var situacion = new SituacionRevistaBuilder().ConCargo(Cargo.Titular).ConFechaFin(null).Build();

		situacion.EsTemporal().ShouldBeFalse();
		situacion.Periodo.FechaFin.ShouldBeNull();
	}
	#endregion

	#region Cargo ocupante
	[Theory]
	[InlineData(nameof(Cargo.Titular))]
	[InlineData(nameof(Cargo.Interino))]
	public void EsCargoOcupante_es_true_para_el_titular_y_el_interino(string cargo)
	{
		SituacionRevista.EsCargoOcupante(Enum.Parse<Cargo>(cargo)).ShouldBeTrue();
	}

	[Fact]
	public void EsCargoOcupante_es_false_para_el_suplente()
	{
		// El suplente no ocupa el puesto: reemplaza a quien lo ocupa mientras dura la ausencia.
		SituacionRevista.EsCargoOcupante(Cargo.Suplente).ShouldBeFalse();
	}

	[Fact]
	public void EsOcupante_de_una_designacion_titular_es_true()
	{
		var situacion = new SituacionRevistaBuilder().ConCargo(Cargo.Titular).Build();

		situacion.EsOcupante().ShouldBeTrue();
	}

	[Fact]
	public void EsOcupante_de_una_designacion_interina_es_true()
	{
		var situacion = new SituacionRevistaBuilder()
			.ConCargo(Cargo.Interino)
			.ConFechaFin(DateTime.Today.AddMonths(6))
			.Build();

		situacion.EsOcupante().ShouldBeTrue();
	}

	[Fact]
	public void EsOcupante_de_una_suplencia_es_false()
	{
		var situacion = new SituacionRevistaBuilder()
			.ConCargo(Cargo.Suplente)
			.ConFechaFin(DateTime.Today.AddMonths(1))
			.Build();

		situacion.EsOcupante().ShouldBeFalse();
	}

	[Fact]
	public void EsOcupante_depende_del_cargo_y_no_de_la_vigencia_del_periodo()
	{
		var situacion = new SituacionRevistaBuilder()
			.ConCargo(Cargo.Interino)
			.ConFechaInicio(DateTime.Today.AddYears(-3))
			.ConFechaFin(DateTime.Today.AddYears(-2))
			.Build();

		situacion.EsCargoVigente().ShouldBeFalse();
		situacion.EsOcupante().ShouldBeTrue();
	}
	#endregion

	#region Pase a interino
	/// <summary>
	/// Un cargo a término es interino: ponerle fecha de fin a una designación indeterminada no es
	/// sólo mover el período, es cambiar de qué cargo se trata.
	/// </summary>
	[Fact]
	public void PasarAInterinoHasta_le_pone_termino_al_cargo_y_lo_deja_interino()
	{
		var catedra = new CatedraBuilder().Build();
		var designacion = catedra.Designar(Guid.NewGuid(), Cargo.Titular, DateTime.Today, null);
		var termino = DateTime.Today.AddMonths(6);

		catedra.PasarAInterinoHasta(designacion, termino);

		var situacion = catedra.SituacionesRevista.Single(x => x.Id.Equals(designacion));
		situacion.Cargo.ShouldBe(Cargo.Interino);
		situacion.Periodo.FechaFin.ShouldBe(termino);
		situacion.EsTemporal().ShouldBeTrue();
	}

	/// <summary>
	/// La contracara de <see cref="Un_cargo_temporal_sin_fecha_de_fin_lanza_CargoTemporalSinFechaFinalizacionException"/>:
	/// el interino y el suplente ya nacen con término, así que el pase no tiene nada que hacer sobre ellos.
	/// </summary>
	[Fact]
	public void PasarAInterinoHasta_sobre_un_cargo_temporal_lanza_CargoConFechaFinalizacionException()
	{
		// Interino y no Titular: desde que el constructor cierra el invariante en las dos
		// direcciones, un titular con fecha de fin ya no se puede crear ni para armar el escenario.
		var catedra = new CatedraBuilder().Build();
		var designacion = catedra.Designar(Guid.NewGuid(), Cargo.Interino, DateTime.Today, DateTime.Today.AddMonths(3));

		var ex = Should.Throw<Exception>(() => catedra.PasarAInterinoHasta(designacion, DateTime.Today.AddMonths(6)));

		ex.GetType().Name.ShouldBe("CargoConFechaFinalizacionException");
	}

	/// <summary>
	/// Un término que cae hoy o antes no agenda nada: cierra la designación en el acto, que es lo que
	/// hace <c>FinalizarDesignacion</c>. Mismo criterio que <c>Licencia.EstablecerFechaFinalizacion</c>.
	/// </summary>
	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void PasarAInterinoHasta_con_un_termino_que_no_es_posterior_a_hoy_lanza_ArgumentException(int dias)
	{
		var catedra = new CatedraBuilder().Build();
		var designacion = catedra.Designar(Guid.NewGuid(), Cargo.Titular, DateTime.Today.AddYears(-1), null);

		Should.Throw<ArgumentException>(() => catedra.PasarAInterinoHasta(designacion, DateTime.Today.AddDays(dias)));
	}
	#endregion

	#region Finalización

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
		publicos.ShouldNotContain("PasarAInterinoHasta");
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
