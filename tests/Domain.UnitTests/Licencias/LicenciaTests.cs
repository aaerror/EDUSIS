using Domain.Licencias.Exceptions;
using Domain.Licencias;
using Domain.Licencias.DomainEvents;
using Domain.Shared;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Licencias;

/// <summary>
/// Agregado <see cref="Licencia"/> con los enumerados <see cref="Articulo"/> y <see cref="Estado"/>:
/// solicitud, aprobación, rechazo y finalización, cada transición con su evento de dominio.
/// Las excepciones del módulo son <c>internal</c>: se verifican por el nombre del tipo.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class LicenciaTests
{
	#region Solicitud
	[Fact]
	public void Una_licencia_nueva_arranca_pendiente_y_encola_LicenciaSolicitadaEvent()
	{
		var docenteID = Guid.NewGuid();

		var licencia = new LicenciaBuilder().ConDocente(docenteID).ConArticulo(nameof(Articulo.AccidenteDeTrabajo)).Build();

		licencia.DocenteID.ShouldBe(docenteID);
		licencia.Articulo.ShouldBe(Articulo.AccidenteDeTrabajo);
		licencia.Estado.ShouldBe(Estado.Pendiente);
		var evento = licencia.Eventos.ShouldHaveSingleItem().ShouldBeOfType<LicenciaSolicitadaEvent>();
		evento.LicenciaID.ShouldBe(licencia.Id);
		evento.DocenteID.ShouldBe(docenteID);
	}

	[Fact]
	public void Una_licencia_con_fecha_de_inicio_futura_lanza_FechaDeInicioDeLicenciaFuturaException()
	{
		var builder = new LicenciaBuilder().ConFechaInicio(DateTime.Today.AddDays(1));

		var ex = Should.Throw<Exception>(() => builder.Build());

		ex.GetType().Name.ShouldBe("FechaDeInicioDeLicenciaFuturaException");
	}
	#endregion

	#region Transiciones de estado
	[Fact]
	public void AprobarLicencia_la_pone_activa_y_encola_LicenciaActivadaEvent()
	{
		var licencia = new LicenciaBuilder().Build();
		licencia.LiberarEventos();

		licencia.AprobarLicencia("Aprobada por dirección");

		licencia.Estado.ShouldBe(Estado.Activa);
		licencia.Eventos.ShouldHaveSingleItem().ShouldBeOfType<LicenciaActivadaEvent>();
	}

	[Fact]
	public void RechazarLicencia_la_pone_rechazada_y_encola_LicenciaRechazadaEvent()
	{
		var licencia = new LicenciaBuilder().Build();
		licencia.LiberarEventos();

		licencia.RechazarLicencia("Sin certificado médico");

		licencia.Estado.ShouldBe(Estado.Rechazada);
		licencia.Observacion.ShouldBe("Sin certificado médico");
		licencia.Eventos.ShouldHaveSingleItem().ShouldBeOfType<LicenciaRechazadaEvent>();
	}

	[Fact]
	public void Aprobar_o_rechazar_una_licencia_que_no_esta_pendiente_lanza_LicenciaNoPendiente()
	{
		var licencia = new LicenciaBuilder().Aprobada().Build();

		Should.Throw<LicenciaNoPendienteException>(() => licencia.AprobarLicencia(null));
		Should.Throw<LicenciaNoPendienteException>(() => licencia.RechazarLicencia(null));
	}

	[Fact]
	public void FinalizarLicencia_sobre_una_licencia_activa_la_finaliza_y_encola_LicenciaFinalizadaEvent()
	{
		var licencia = new LicenciaBuilder().Aprobada().Build();
		licencia.LiberarEventos();

		licencia.FinalizarLicencia();

		licencia.Estado.ShouldBe(Estado.Finalizada);
		licencia.Periodo.FechaFin.ShouldBe(DateTime.Today);
		licencia.Eventos.ShouldHaveSingleItem().ShouldBeOfType<LicenciaFinalizadaEvent>();
	}

	[Fact]
	public void FinalizarLicencia_sobre_una_licencia_pendiente_lanza_LicenciaInactivaException()
	{
		var licencia = new LicenciaBuilder().Build();

		var ex = Should.Throw<Exception>(() => licencia.FinalizarLicencia());

		ex.GetType().Name.ShouldBe("LicenciaInactivaException");
	}

	[Fact]
	public void FinalizarLicencia_antes_de_su_fecha_de_fin_hace_de_hoy_su_ultimo_dia()
	{
		var licencia = new LicenciaBuilder()
			.ConFechaInicio(DateTime.Today.AddDays(-3))
			.ConFechaFin(DateTime.Today.AddDays(10))
			.Aprobada()
			.Build();

		licencia.FinalizarLicencia();

		licencia.Periodo.FechaFin.ShouldBe(DateTime.Today);
	}

	/// <remarks>
	/// El cierre es manual: una licencia vencida sigue activa hasta que alguien registra el
	/// reintegro, y al hacerlo conserva la fecha de fin real y no la del día en que se la cerró.
	/// </remarks>
	[Fact]
	public void FinalizarLicencia_vencida_conserva_su_fecha_de_fin()
	{
		var fechaFin = DateTime.Today.AddDays(-5);
		var licencia = new LicenciaBuilder()
			.ConFechaInicio(DateTime.Today.AddDays(-20))
			.ConFechaFin(fechaFin)
			.Aprobada()
			.Build();

		licencia.EstaVencida().ShouldBeTrue();

		licencia.FinalizarLicencia();

		licencia.Estado.ShouldBe(Estado.Finalizada);
		licencia.Periodo.FechaFin.ShouldBe(fechaFin);
		licencia.EstaVencida().ShouldBeFalse();
	}

	[Fact]
	public void FinalizarLicencia_ya_finalizada_lanza_LicenciaInactivaException()
	{
		var licencia = new LicenciaBuilder().Aprobada().Build();
		licencia.FinalizarLicencia();

		var ex = Should.Throw<Exception>(() => licencia.FinalizarLicencia());

		ex.GetType().Name.ShouldBe("LicenciaInactivaException");
	}

	[Fact]
	public void Ni_una_licencia_en_su_ultimo_dia_ni_una_pendiente_estan_vencidas()
	{
		new LicenciaBuilder().ConFechaFin(DateTime.Today).Aprobada().Build().EstaVencida().ShouldBeFalse();
		new LicenciaBuilder().ConFechaInicio(DateTime.Today.AddDays(-9)).ConFechaFin(DateTime.Today.AddDays(-1)).Build().EstaVencida().ShouldBeFalse();
	}
	#endregion

	#region Días y superposición
	[Fact]
	public void La_fecha_de_fin_es_el_ultimo_dia_de_licencia()
	{
		var licencia = new Licencia(Guid.NewGuid(), nameof(Articulo.EnfermedadCortoTratamiento), new DateTime(2026, 3, 1), 15, null);

		licencia.Periodo.FechaFin.ShouldBe(new DateTime(2026, 3, 15));
		licencia.Dias.ShouldBe(15);
	}

	[Fact]
	public void Una_licencia_de_un_dia_empieza_y_termina_el_mismo_dia()
	{
		var licencia = new Licencia(Guid.NewGuid(), nameof(Articulo.AtencionDeFamiliar), DateTime.Today, 1, null);

		licencia.Periodo.FechaFin.ShouldBe(DateTime.Today);
		licencia.Dias.ShouldBe(1);
	}

	[Fact]
	public void Una_licencia_indefinida_tiene_cero_dias()
	{
		new LicenciaBuilder().Build().Dias.ShouldBe(0);
	}

	/// <summary>Contra una licencia del 01/03 al 15/03; los desplazamientos son desde el 01/03.</summary>
	[Theory]
	[InlineData(-9, 0, true)]    // termina el día en que empieza la otra: comparten ese día
	[InlineData(-9, -1, false)]  // termina el día anterior
	[InlineData(4, 7, true)]     // contenida
	[InlineData(14, 19, true)]   // empieza el último día de la otra
	[InlineData(15, 19, false)]  // empieza al día siguiente del último
	public void SeSuperponeCon_toma_los_dos_extremos_como_dias_de_licencia(int desde, int hasta, bool seSuperpone)
	{
		var primeroDeMarzo = new DateTime(2026, 3, 1);
		var licencia = new Licencia(Guid.NewGuid(), nameof(Articulo.EnfermedadCortoTratamiento), primeroDeMarzo, 15, null);
		var otro = RangoFechas.Create(primeroDeMarzo.AddDays(desde), primeroDeMarzo.AddDays(hasta));

		licencia.SeSuperponeCon(otro).ShouldBe(seSuperpone);
	}

	[Fact]
	public void Un_periodo_sin_fin_se_superpone_con_todo_lo_posterior_a_su_inicio()
	{
		var indefinida = new LicenciaBuilder().ConFechaInicio(new DateTime(2026, 3, 1)).Build();

		indefinida.SeSuperponeCon(RangoFechas.Create(new DateTime(2030, 1, 1), new DateTime(2030, 1, 2))).ShouldBeTrue();
		indefinida.SeSuperponeCon(RangoFechas.Create(new DateTime(2026, 1, 1), new DateTime(2026, 2, 28))).ShouldBeFalse();
		indefinida.SeSuperponeCon(RangoFechas.Create(new DateTime(2026, 1, 1))).ShouldBeTrue();
	}

	[Fact]
	public void Solo_la_licencia_rechazada_deja_libre_su_periodo()
	{
		var rechazada = new LicenciaBuilder().Build();
		rechazada.RechazarLicencia(null);

		new LicenciaBuilder().Build().OcupaSuPeriodo().ShouldBeTrue();
		new LicenciaBuilder().Aprobada().Build().OcupaSuPeriodo().ShouldBeTrue();
		rechazada.OcupaSuPeriodo().ShouldBeFalse();
	}
	#endregion

	#region Modificación del período
	[Fact]
	public void ModificarSolicitud_sobre_una_licencia_no_activa_ajusta_articulo_e_intervalo()
	{
		var licencia = new LicenciaBuilder().Build();

		licencia.ModificarSolicitud(nameof(Articulo.EnfermedadCortoTratamiento), new DateTime(2026, 5, 1), 10);

		licencia.Articulo.ShouldBe(Articulo.EnfermedadCortoTratamiento);
		licencia.Periodo.FechaInicio.ShouldBe(new DateTime(2026, 5, 1));
		licencia.Periodo.FechaFin.ShouldBe(new DateTime(2026, 5, 10));
	}

	/// <remarks>
	/// Antes sólo se rechazaba la licencia vigente: una rechazada, una finalizada o una aprobada ya
	/// vencida se podían reescribir.
	/// </remarks>
	[Fact]
	public void ModificarSolicitud_sobre_una_licencia_que_no_esta_pendiente_lanza_LicenciaNoPendienteException()
	{
		var aprobada = new LicenciaBuilder().Aprobada().Build();
		var rechazada = new LicenciaBuilder().Build();
		rechazada.RechazarLicencia(null);
		var vencida = new LicenciaBuilder().ConFechaInicio(DateTime.Today.AddDays(-20)).ConFechaFin(DateTime.Today.AddDays(-5)).Aprobada().Build();

		foreach (var licencia in new[] { aprobada, rechazada, vencida })
		{
			Should.Throw<LicenciaNoPendienteException>(() => licencia.ModificarSolicitud(nameof(Articulo.OtrasCausas), DateTime.Today, 5));
		}
	}

	[Fact]
	public void ModificarSolicitud_que_viola_el_plazo_legal_no_cambia_la_licencia()
	{
		var licencia = new LicenciaBuilder().Build();

		Should.Throw<PlazoFijoDeLicenciaIncumplidoException>(() =>
			licencia.ModificarSolicitud(nameof(Articulo.MatrimonioPropio), DateTime.Today, 3));

		licencia.Articulo.ShouldBe(Articulo.AccidenteDeTrabajo);
		licencia.EsIndefinida().ShouldBeTrue();
	}

	[Fact]
	public void EstablecerLicenciaIndefinida_sobre_una_licencia_activa_temporal_le_quita_la_fecha_de_fin()
	{
		var licencia = new LicenciaBuilder().ConFechaFin(DateTime.Today.AddDays(20)).Aprobada().Build();

		licencia.EstablecerLicenciaIndefinida();

		licencia.Periodo.FechaFin.ShouldBeNull();
		licencia.EsIndefinida().ShouldBeTrue();
	}

	[Fact]
	public void EstablecerLicenciaIndefinida_sobre_una_licencia_ya_indefinida_lanza_LicenciaIndefinidaException()
	{
		var licencia = new LicenciaBuilder().Aprobada().Build();

		var ex = Should.Throw<Exception>(() => licencia.EstablecerLicenciaIndefinida());

		ex.GetType().Name.ShouldBe("LicenciaIndefinidaException");
	}

	[Fact]
	public void EstablecerFechaFinalizacion_con_una_fecha_no_posterior_a_hoy_lanza_FechaDeFinalizacionNoPosteriorAHoyException()
	{
		var licencia = new LicenciaBuilder().Aprobada().Build();

		var ex = Should.Throw<Exception>(() => licencia.EstablecerFechaFinalizacion(DateTime.Today));

		ex.GetType().Name.ShouldBe("FechaDeFinalizacionNoPosteriorAHoyException");
	}

	[Fact]
	public void EstablecerFechaFinalizacion_sobre_una_licencia_que_ya_tiene_fecha_de_fin_lanza_LicenciaConFechaDeFinalizacionException()
	{
		var licencia = new LicenciaBuilder().ConFechaFin(DateTime.Today.AddDays(10)).Aprobada().Build();

		var ex = Should.Throw<Exception>(() => licencia.EstablecerFechaFinalizacion(DateTime.Today.AddDays(20)));

		ex.GetType().Name.ShouldBe("LicenciaConFechaDeFinalizacionException");
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-3)]
	public void ExtenderLicencia_sin_una_cantidad_de_dias_positiva_lanza_ExtensionSinDiasException(int cantidadDias)
	{
		var licencia = new LicenciaBuilder().ConFechaFin(DateTime.Today.AddDays(10)).Aprobada().Build();

		var ex = Should.Throw<Exception>(() => licencia.ExtenderLicencia(cantidadDias));

		ex.GetType().Name.ShouldBe("ExtensionSinDiasException");
	}

	[Fact]
	public void ExtenderLicencia_prolonga_la_fecha_de_fin_de_una_licencia_activa_temporal()
	{
		var fechaFin = DateTime.Today.AddDays(10);
		var licencia = new LicenciaBuilder().ConFechaFin(fechaFin).Aprobada().Build();

		licencia.ExtenderLicencia(5);

		licencia.Periodo.FechaFin.ShouldBe(fechaFin.AddDays(5));
	}

	[Fact]
	public void ExtenderLicencia_vencida_y_sin_finalizar_la_prorroga()
	{
		var fechaFin = DateTime.Today.AddDays(-2);
		var licencia = new LicenciaBuilder().ConFechaInicio(DateTime.Today.AddDays(-10)).ConFechaFin(fechaFin).Aprobada().Build();

		licencia.ExtenderLicencia(5);

		licencia.Periodo.FechaFin.ShouldBe(fechaFin.AddDays(5));
		licencia.EstaVencida().ShouldBeFalse();
	}

	[Fact]
	public void ExtenderLicencia_finalizada_lanza_LicenciaInactivaException()
	{
		var licencia = new LicenciaBuilder().ConFechaFin(DateTime.Today.AddDays(10)).Aprobada().Build();
		licencia.FinalizarLicencia();

		var ex = Should.Throw<Exception>(() => licencia.ExtenderLicencia(5));

		ex.GetType().Name.ShouldBe("LicenciaInactivaException");
	}

	[Fact]
	public void ExtenderLicencia_sobre_una_licencia_indefinida_lanza_LicenciaIndefinidaException()
	{
		var licencia = new LicenciaBuilder().Aprobada().Build();

		var ex = Should.Throw<Exception>(() => licencia.ExtenderLicencia(5));

		ex.GetType().Name.ShouldBe("LicenciaIndefinidaException");
	}
	#endregion

	#region Plazo legal
	[Fact]
	public void Todo_articulo_tiene_plazo_legal_con_denominacion_y_norma()
	{
		foreach (var articulo in Enum.GetValues<Articulo>())
		{
			var plazo = PlazoLegal.De(articulo);

			plazo.Denominacion.ShouldNotBeNullOrWhiteSpace();
			plazo.Norma.ShouldNotBeNullOrWhiteSpace();
			(plazo.Tipo == TipoDePlazo.Abierto || plazo.Dias > 0).ShouldBeTrue($"{ articulo } tiene plazo sin días");
		}
	}

	[Fact]
	public void Una_licencia_de_plazo_fijo_con_sus_dias_exactos_se_solicita()
	{
		var licencia = new Licencia(Guid.NewGuid(), nameof(Articulo.MatrimonioPropio), DateTime.Today, 15, null);

		licencia.Periodo.FechaFin.ShouldBe(DateTime.Today.AddDays(14));
		licencia.Dias.ShouldBe(15);
	}

	[Theory]
	[InlineData(14)]
	[InlineData(16)]
	public void Una_licencia_de_plazo_fijo_con_otros_dias_lanza_PlazoFijoDeLicenciaIncumplidoException(int dias)
	{
		Should.Throw<PlazoFijoDeLicenciaIncumplidoException>(() =>
			new Licencia(Guid.NewGuid(), nameof(Articulo.MatrimonioPropio), DateTime.Today, dias, null));
	}

	[Fact]
	public void Una_licencia_de_plazo_maximo_hasta_el_tope_se_solicita()
	{
		var licencia = new Licencia(Guid.NewGuid(), nameof(Articulo.AtencionDeFamiliar), DateTime.Today, 10, null);

		licencia.Periodo.FechaFin.ShouldBe(DateTime.Today.AddDays(9));
		licencia.Dias.ShouldBe(10);
	}

	[Fact]
	public void Una_licencia_que_supera_el_tope_lanza_PlazoMaximoDeLicenciaExcedidoException()
	{
		var ex = Should.Throw<PlazoMaximoDeLicenciaExcedidoException>(() =>
			new Licencia(Guid.NewGuid(), nameof(Articulo.AtencionDeFamiliar), DateTime.Today, 11, null));

		ex.Message.ShouldContain("10 días");
	}

	[Theory]
	[InlineData(nameof(Articulo.EnfermedadCortoTratamiento))]
	[InlineData(nameof(Articulo.MatrimonioPropio))]
	public void Un_articulo_con_plazo_sin_fecha_de_fin_lanza_LicenciaIndefinidaNoAdmitidaException(string articulo)
	{
		Should.Throw<LicenciaIndefinidaNoAdmitidaException>(() =>
			new Licencia(Guid.NewGuid(), articulo, DateTime.Today, 0, null));
	}

	[Theory]
	[InlineData(nameof(Articulo.AccidenteDeTrabajo))]
	[InlineData(nameof(Articulo.OtrasCausas))]
	[InlineData(nameof(Articulo.EstudioEncomendado))]
	public void Un_articulo_de_plazo_abierto_admite_quedar_sin_fecha_de_fin(string articulo)
	{
		var licencia = new Licencia(Guid.NewGuid(), articulo, DateTime.Today, 0, null);

		licencia.EsIndefinida().ShouldBeTrue();
	}

	[Fact]
	public void ExtenderLicencia_mas_alla_del_tope_lanza_PlazoMaximoDeLicenciaExcedidoException_y_no_la_cambia()
	{
		var licencia = new LicenciaBuilder()
			.ConArticulo(nameof(Articulo.AtencionDeFamiliar))
			.ConFechaFin(DateTime.Today.AddDays(8))
			.Aprobada()
			.Build();

		Should.Throw<PlazoMaximoDeLicenciaExcedidoException>(() => licencia.ExtenderLicencia(3));

		licencia.Periodo.FechaFin.ShouldBe(DateTime.Today.AddDays(8));
	}

	[Fact]
	public void EstablecerLicenciaIndefinida_en_un_articulo_con_plazo_lanza_LicenciaIndefinidaNoAdmitidaException()
	{
		var licencia = new LicenciaBuilder()
			.ConArticulo(nameof(Articulo.EnfermedadCortoTratamiento))
			.ConFechaFin(DateTime.Today.AddDays(5))
			.Aprobada()
			.Build();

		Should.Throw<LicenciaIndefinidaNoAdmitidaException>(() => licencia.EstablecerLicenciaIndefinida());
	}
	#endregion
}
