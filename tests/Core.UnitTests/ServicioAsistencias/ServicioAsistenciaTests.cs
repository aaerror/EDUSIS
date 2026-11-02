using Core.ServicioAsistencias;
using Core.ServicioAsistencias.DTOs.Requests;
using Core.ServicioAsistencias.Exceptions;
using Core.UnitTests.Infraestructura;
using Domain.Asistencias;
using Domain.Divisiones;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Core.UnitTests.ServicioAsistencias;

/// <summary>
/// <see cref="IServicioAsistencia"/>: los 11 casos de uso con camino feliz y de error. Las
/// excepciones de dominio de <c>Domain/Asistencias/Exceptions/</c> son <c>internal</c> (sin
/// <c>InternalsVisibleTo</c> hacia este assembly), así que se verifican por el nombre del tipo
/// (mismo idioma que <c>Domain.UnitTests.Shared.ExcepcionesTests</c>), sin referenciarlas.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class ServicioAsistenciaTests
{
	private readonly HostDeServicios _host = new();
	private readonly IServicioAsistencia _servicio;

	public ServicioAsistenciaTests()
	{
		_servicio = _host.Resolver<IServicioAsistencia>();
	}

	#region Ayudantes de siembra
	private Division SembrarDivisionConPreceptor(Guid? preceptorID = null)
	{
		var division = new DivisionBuilder().Build();
		division.AsignarPreceptor(preceptorID ?? Guid.NewGuid());
		_host.UnidadDeTrabajo.DivisionesFake.Sembrar(division);
		return division;
	}

	private Guid SembrarCursante(Guid divisionID)
	{
		var cursante = new CursanteBuilder()
			.ConDivision(divisionID)
			.ConFechaInicio(DateTime.Today.AddMonths(-1))
			.Build();
		_host.UnidadDeTrabajo.CursantesFake.Sembrar(cursante);
		return cursante.Id;
	}

	private PlanillaAsistencia SembrarPlanilla(Guid divisionID, DateTime fecha, Guid preceptorID, IEnumerable<Guid> cursantesIDs, bool cerrada = false)
	{
		var builder = new PlanillaAsistenciaBuilder()
			.ConDivision(divisionID)
			.ConFecha(fecha)
			.ConPreceptor(preceptorID)
			.ConCursantes(cursantesIDs.ToArray());

		if (cerrada)
		{
			builder.Cerrada();
		}

		var planilla = builder.Build();
		_host.UnidadDeTrabajo.PlanillasAsistenciaFake.Sembrar(planilla);
		return planilla;
	}
	#endregion

	#region AbrirPlanillaAsync
	[Fact]
	public async Task AbrirPlanillaAsync_nace_completa_con_todos_los_cursantes_presentes()
	{
		var division = SembrarDivisionConPreceptor();
		var cursante1 = SembrarCursante(division.Id);
		var cursante2 = SembrarCursante(division.Id);
		var fecha = DateTime.Today;

		var response = await _servicio.AbrirPlanillaAsync(new AbrirPlanillaRequest(division.Id, fecha));

		response.DivisionID.ShouldBe(division.Id);
		response.PreceptorID.ShouldBe(division.Preceptor!.Value);
		response.Cerrada.ShouldBeFalse();
		response.Registros.Count.ShouldBe(2);
		response.Registros.ShouldAllBe(r => r.Tipo == TipoAsistencia.Presente);
		response.Registros.ShouldContain(r => r.CursanteID == cursante1);
		response.Registros.ShouldContain(r => r.CursanteID == cursante2);
		response.Presentes.ShouldBe(2);
		_host.UnidadDeTrabajo.PlanillasAsistenciaFake.Elementos.ShouldHaveSingleItem();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task AbrirPlanillaAsync_lanza_PlanillaDuplicadaException_si_ya_existe_una_planilla_para_la_division_y_fecha()
	{
		var division = SembrarDivisionConPreceptor();
		var fecha = DateTime.Today;
		SembrarPlanilla(division.Id, fecha, division.Preceptor!.Value, new[] { Guid.NewGuid() });

		await Should.ThrowAsync<PlanillaDuplicadaException>(() =>
			_servicio.AbrirPlanillaAsync(new AbrirPlanillaRequest(division.Id, fecha)));
	}

	[Fact]
	public async Task AbrirPlanillaAsync_con_una_division_sin_preceptor_delega_en_el_dominio_y_lanza_SinDatosPreceptorException()
	{
		var division = new DivisionBuilder().Build();
		_host.UnidadDeTrabajo.DivisionesFake.Sembrar(division);
		SembrarCursante(division.Id);

		var ex = await Should.ThrowAsync<Exception>(() =>
			_servicio.AbrirPlanillaAsync(new AbrirPlanillaRequest(division.Id, DateTime.Today)));

		ex.GetType().Name.ShouldBe("SinDatosPreceptorException");
	}

	[Fact]
	public async Task AbrirPlanillaAsync_con_una_division_inexistente_lanza_NullReferenceException()
	{
		await Should.ThrowAsync<NullReferenceException>(() =>
			_servicio.AbrirPlanillaAsync(new AbrirPlanillaRequest(Guid.NewGuid(), DateTime.Today)));
	}
	#endregion

	#region MarcarPresenteAsync
	[Fact]
	public async Task MarcarPresenteAsync_marca_al_cursante_como_presente()
	{
		var cursanteID = Guid.NewGuid();
		var planilla = SembrarPlanilla(Guid.NewGuid(), DateTime.Today, Guid.NewGuid(), new[] { cursanteID });
		planilla.MarcarAusencia(cursanteID, "Se retiró temprano");

		await _servicio.MarcarPresenteAsync(new MarcarPresenteRequest(planilla.DivisionID, planilla.Fecha, cursanteID));

		planilla.RegistroDe(cursanteID)!.Tipo.ShouldBe(TipoAsistencia.Presente);
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task MarcarPresenteAsync_en_una_planilla_cerrada_no_la_modifica_y_lanza_PlanillaCerradaException()
	{
		var cursanteID = Guid.NewGuid();
		var planilla = SembrarPlanilla(Guid.NewGuid(), DateTime.Today, Guid.NewGuid(), new[] { cursanteID }, cerrada: true);

		var ex = await Should.ThrowAsync<Exception>(() =>
			_servicio.MarcarPresenteAsync(new MarcarPresenteRequest(planilla.DivisionID, planilla.Fecha, cursanteID)));

		ex.GetType().Name.ShouldBe("PlanillaCerradaException");
		planilla.RegistroDe(cursanteID)!.Tipo.ShouldBe(TipoAsistencia.Presente);
	}
	#endregion

	#region MarcarAusenciaAsync
	[Fact]
	public async Task MarcarAusenciaAsync_marca_al_cursante_como_ausencia_con_observacion()
	{
		var cursanteID = Guid.NewGuid();
		var planilla = SembrarPlanilla(Guid.NewGuid(), DateTime.Today, Guid.NewGuid(), new[] { cursanteID });

		await _servicio.MarcarAusenciaAsync(new MarcarAusenciaRequest(planilla.DivisionID, planilla.Fecha, cursanteID, "Turno médico"));

		var registro = planilla.RegistroDe(cursanteID)!;
		registro.Tipo.ShouldBe(TipoAsistencia.Ausencia);
		registro.Observacion.ShouldBe("Turno médico");
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task MarcarAusenciaAsync_con_un_cursante_no_incluido_en_la_planilla_lanza_CursanteNoIncluidoException()
	{
		var planilla = SembrarPlanilla(Guid.NewGuid(), DateTime.Today, Guid.NewGuid(), new[] { Guid.NewGuid() });

		var ex = await Should.ThrowAsync<Exception>(() =>
			_servicio.MarcarAusenciaAsync(new MarcarAusenciaRequest(planilla.DivisionID, planilla.Fecha, Guid.NewGuid())));

		ex.GetType().Name.ShouldBe("CursanteNoIncluidoException");
	}
	#endregion

	#region MarcarInasistenciaAsync
	[Fact]
	public async Task MarcarInasistenciaAsync_marca_al_cursante_como_inasistencia()
	{
		var cursanteID = Guid.NewGuid();
		var planilla = SembrarPlanilla(Guid.NewGuid(), DateTime.Today, Guid.NewGuid(), new[] { cursanteID });

		await _servicio.MarcarInasistenciaAsync(new MarcarInasistenciaRequest(planilla.DivisionID, planilla.Fecha, cursanteID, "No justificada"));

		var registro = planilla.RegistroDe(cursanteID)!;
		registro.Tipo.ShouldBe(TipoAsistencia.Inasistencia);
		registro.Observacion.ShouldBe("No justificada");
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task MarcarInasistenciaAsync_con_una_observacion_de_mas_de_140_caracteres_lanza_ObservacionExtensaException()
	{
		var cursanteID = Guid.NewGuid();
		var planilla = SembrarPlanilla(Guid.NewGuid(), DateTime.Today, Guid.NewGuid(), new[] { cursanteID });
		var observacionLarga = new string('a', 141);

		var ex = await Should.ThrowAsync<Exception>(() =>
			_servicio.MarcarInasistenciaAsync(new MarcarInasistenciaRequest(planilla.DivisionID, planilla.Fecha, cursanteID, observacionLarga)));

		ex.GetType().Name.ShouldBe("ObservacionExtensaException");
	}
	#endregion

	#region MarcarTardanzaAsync
	[Fact]
	public async Task MarcarTardanzaAsync_marca_al_cursante_con_los_minutos_indicados()
	{
		var cursanteID = Guid.NewGuid();
		var planilla = SembrarPlanilla(Guid.NewGuid(), DateTime.Today, Guid.NewGuid(), new[] { cursanteID });
		var minutos = TimeSpan.FromMinutes(15);

		await _servicio.MarcarTardanzaAsync(new MarcarTardanzaRequest(planilla.DivisionID, planilla.Fecha, cursanteID, minutos, "Corte de colectivos"));

		var registro = planilla.RegistroDe(cursanteID)!;
		registro.Tipo.ShouldBe(TipoAsistencia.Tardanza);
		registro.Minutos.ShouldBe(minutos);
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task MarcarTardanzaAsync_con_minutos_en_cero_lanza_TardanzaSinMinutosException()
	{
		var cursanteID = Guid.NewGuid();
		var planilla = SembrarPlanilla(Guid.NewGuid(), DateTime.Today, Guid.NewGuid(), new[] { cursanteID });

		var ex = await Should.ThrowAsync<Exception>(() =>
			_servicio.MarcarTardanzaAsync(new MarcarTardanzaRequest(planilla.DivisionID, planilla.Fecha, cursanteID, TimeSpan.Zero)));

		ex.GetType().Name.ShouldBe("TardanzaSinMinutosException");
	}
	#endregion

	#region IncorporarCursanteAsync
	[Fact]
	public async Task IncorporarCursanteAsync_agrega_un_nuevo_registro_a_la_planilla_ya_abierta()
	{
		var planilla = SembrarPlanilla(Guid.NewGuid(), DateTime.Today, Guid.NewGuid(), new[] { Guid.NewGuid() });
		var nuevoCursanteID = Guid.NewGuid();

		await _servicio.IncorporarCursanteAsync(new IncorporarCursanteRequest(planilla.DivisionID, planilla.Fecha, nuevoCursanteID));

		planilla.Registros.Count.ShouldBe(2);
		planilla.RegistroDe(nuevoCursanteID)!.Tipo.ShouldBe(TipoAsistencia.Presente);
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task IncorporarCursanteAsync_con_un_cursante_ya_incluido_lanza_CursanteYaIncluidoException()
	{
		var cursanteID = Guid.NewGuid();
		var planilla = SembrarPlanilla(Guid.NewGuid(), DateTime.Today, Guid.NewGuid(), new[] { cursanteID });

		var ex = await Should.ThrowAsync<Exception>(() =>
			_servicio.IncorporarCursanteAsync(new IncorporarCursanteRequest(planilla.DivisionID, planilla.Fecha, cursanteID)));

		ex.GetType().Name.ShouldBe("CursanteYaIncluidoException");
	}
	#endregion

	#region CerrarPlanillaAsync
	[Fact]
	public async Task CerrarPlanillaAsync_cierra_una_planilla_abierta()
	{
		var planilla = SembrarPlanilla(Guid.NewGuid(), DateTime.Today, Guid.NewGuid(), new[] { Guid.NewGuid() });

		await _servicio.CerrarPlanillaAsync(new CerrarPlanillaRequest(planilla.DivisionID, planilla.Fecha));

		planilla.Cerrada.ShouldBeTrue();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task CerrarPlanillaAsync_sobre_una_planilla_ya_cerrada_lanza_PlanillaCerradaException()
	{
		var planilla = SembrarPlanilla(Guid.NewGuid(), DateTime.Today, Guid.NewGuid(), new[] { Guid.NewGuid() }, cerrada: true);

		var ex = await Should.ThrowAsync<Exception>(() =>
			_servicio.CerrarPlanillaAsync(new CerrarPlanillaRequest(planilla.DivisionID, planilla.Fecha)));

		ex.GetType().Name.ShouldBe("PlanillaCerradaException");
	}
	#endregion

	#region ReabrirPlanillaAsync
	[Fact]
	public async Task ReabrirPlanillaAsync_reabre_una_planilla_previamente_cerrada()
	{
		var planilla = SembrarPlanilla(Guid.NewGuid(), DateTime.Today, Guid.NewGuid(), new[] { Guid.NewGuid() }, cerrada: true);

		await _servicio.ReabrirPlanillaAsync(new ReabrirPlanillaRequest(planilla.DivisionID, planilla.Fecha));

		planilla.Cerrada.ShouldBeFalse();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task ReabrirPlanillaAsync_sobre_una_planilla_ya_abierta_lanza_PlanillaAbiertaException()
	{
		var planilla = SembrarPlanilla(Guid.NewGuid(), DateTime.Today, Guid.NewGuid(), new[] { Guid.NewGuid() });

		var ex = await Should.ThrowAsync<Exception>(() =>
			_servicio.ReabrirPlanillaAsync(new ReabrirPlanillaRequest(planilla.DivisionID, planilla.Fecha)));

		ex.GetType().Name.ShouldBe("PlanillaAbiertaException");
	}
	#endregion

	#region EliminarPlanillaAsync
	[Fact]
	public async Task EliminarPlanillaAsync_saca_del_repositorio_la_planilla_abierta()
	{
		var planilla = SembrarPlanilla(Guid.NewGuid(), DateTime.Today, Guid.NewGuid(), new[] { Guid.NewGuid() });

		await _servicio.EliminarPlanillaAsync(new EliminarPlanillaRequest(planilla.DivisionID, planilla.Fecha));

		_host.UnidadDeTrabajo.PlanillasAsistenciaFake.Elementos.ShouldBeEmpty();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task EliminarPlanillaAsync_sobre_una_planilla_cerrada_propaga_PlanillaCerradaNoEliminableException_y_no_la_borra()
	{
		var planilla = SembrarPlanilla(Guid.NewGuid(), DateTime.Today, Guid.NewGuid(), new[] { Guid.NewGuid() }, cerrada: true);

		var ex = await Should.ThrowAsync<Exception>(() =>
			_servicio.EliminarPlanillaAsync(new EliminarPlanillaRequest(planilla.DivisionID, planilla.Fecha)));

		ex.GetType().Name.ShouldBe("PlanillaCerradaNoEliminableException");
		_host.UnidadDeTrabajo.PlanillasAsistenciaFake.Elementos.ShouldHaveSingleItem();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(0);
	}

	[Fact]
	public async Task EliminarPlanillaAsync_con_una_planilla_inexistente_lanza_NullReferenceException()
	{
		await Should.ThrowAsync<NullReferenceException>(() =>
			_servicio.EliminarPlanillaAsync(new EliminarPlanillaRequest(Guid.NewGuid(), DateTime.Today)));
	}
	#endregion

	#region ConsultarPlanillaAsync
	[Fact]
	public async Task ConsultarPlanillaAsync_devuelve_los_registros_y_los_conteos_por_tipo()
	{
		var presente = Guid.NewGuid();
		var ausente = Guid.NewGuid();
		var tarde = Guid.NewGuid();
		var planilla = SembrarPlanilla(Guid.NewGuid(), DateTime.Today, Guid.NewGuid(), new[] { presente, ausente, tarde });
		planilla.MarcarAusencia(ausente, "Sin aviso");
		planilla.MarcarTardanza(tarde, TimeSpan.FromMinutes(10));

		var response = await _servicio.ConsultarPlanillaAsync(new ConsultarPlanillaRequest(planilla.DivisionID, planilla.Fecha));

		response.Registros.Count.ShouldBe(3);
		response.Presentes.ShouldBe(1);
		response.Ausencias.ShouldBe(1);
		response.Inasistencias.ShouldBe(0);
		response.Tardanzas.ShouldBe(1);
		response.Registros.Single(r => r.CursanteID == tarde).Minutos.ShouldBe(TimeSpan.FromMinutes(10));
	}

	[Fact]
	public async Task ConsultarPlanillaAsync_con_una_planilla_inexistente_lanza_NullReferenceException()
	{
		await Should.ThrowAsync<NullReferenceException>(() =>
			_servicio.ConsultarPlanillaAsync(new ConsultarPlanillaRequest(Guid.NewGuid(), DateTime.Today)));
	}
	#endregion

	#region ContarFaltasAsync
	[Fact]
	public async Task ContarFaltasAsync_discrimina_las_faltas_del_cursante_segun_el_tipo_de_asistencia()
	{
		var cursanteID = Guid.NewGuid();
		var otroCursanteID = Guid.NewGuid();

		var planilla1 = SembrarPlanilla(Guid.NewGuid(), DateTime.Today.AddDays(-2), Guid.NewGuid(), new[] { cursanteID, otroCursanteID });
		planilla1.MarcarInasistencia(cursanteID);

		var planilla2 = SembrarPlanilla(Guid.NewGuid(), DateTime.Today.AddDays(-1), Guid.NewGuid(), new[] { cursanteID, otroCursanteID });
		planilla2.MarcarInasistencia(cursanteID);
		planilla2.MarcarAusencia(otroCursanteID);

		var planilla3 = SembrarPlanilla(Guid.NewGuid(), DateTime.Today, Guid.NewGuid(), new[] { cursanteID });
		planilla3.MarcarAusencia(cursanteID);

		var inasistencias = await _servicio.ContarFaltasAsync(new ContarFaltasRequest(cursanteID, TipoAsistencia.Inasistencia));
		var ausencias = await _servicio.ContarFaltasAsync(new ContarFaltasRequest(cursanteID, TipoAsistencia.Ausencia));

		inasistencias.ShouldBe(2);
		ausencias.ShouldBe(1);
	}

	[Fact]
	public async Task ContarFaltasAsync_devuelve_cero_si_el_cursante_no_tiene_faltas_de_ese_tipo()
	{
		var cursanteID = Guid.NewGuid();
		var planilla = SembrarPlanilla(Guid.NewGuid(), DateTime.Today, Guid.NewGuid(), new[] { cursanteID });

		var cantidad = await _servicio.ContarFaltasAsync(new ContarFaltasRequest(cursanteID, TipoAsistencia.Inasistencia));

		cantidad.ShouldBe(0);
	}
	#endregion

	#region ListarPlanillasAsync
	[Fact]
	public async Task ListarPlanillasAsync_devuelve_las_planillas_de_la_mas_reciente_a_la_mas_vieja()
	{
		var divisionID = Guid.NewGuid();
		var hoy = DateTime.Today;
		var ayer = hoy.AddDays(-1);
		var anteayer = hoy.AddDays(-2);
		SembrarPlanilla(divisionID, anteayer, Guid.NewGuid(), new[] { Guid.NewGuid() });
		SembrarPlanilla(divisionID, hoy, Guid.NewGuid(), new[] { Guid.NewGuid() });
		SembrarPlanilla(divisionID, ayer, Guid.NewGuid(), new[] { Guid.NewGuid() });

		var response = await _servicio.ListarPlanillasAsync(new ListarPlanillasRequest(divisionID, anteayer, hoy));

		response.Select(p => p.Fecha).ShouldBe(new[] { hoy, ayer, anteayer });
	}

	[Fact]
	public async Task ListarPlanillasAsync_incluye_los_dos_extremos_del_rango_y_excluye_lo_que_queda_afuera()
	{
		var divisionID = Guid.NewGuid();
		var hoy = DateTime.Today;
		SembrarPlanilla(divisionID, hoy.AddDays(-5), Guid.NewGuid(), new[] { Guid.NewGuid() });
		SembrarPlanilla(divisionID, hoy.AddDays(-4), Guid.NewGuid(), new[] { Guid.NewGuid() });
		SembrarPlanilla(divisionID, hoy.AddDays(-3), Guid.NewGuid(), new[] { Guid.NewGuid() });
		SembrarPlanilla(divisionID, hoy.AddDays(-2), Guid.NewGuid(), new[] { Guid.NewGuid() });
		SembrarPlanilla(divisionID, hoy.AddDays(-1), Guid.NewGuid(), new[] { Guid.NewGuid() });

		var response = await _servicio.ListarPlanillasAsync(new ListarPlanillasRequest(divisionID, hoy.AddDays(-4), hoy.AddDays(-2)));

		response.Select(p => p.Fecha).ShouldBe(new[] { hoy.AddDays(-2), hoy.AddDays(-3), hoy.AddDays(-4) });
	}

	[Fact]
	public async Task ListarPlanillasAsync_devuelve_una_coleccion_vacia_si_el_rango_no_tiene_planillas()
	{
		var divisionID = Guid.NewGuid();
		var hoy = DateTime.Today;
		SembrarPlanilla(divisionID, hoy, Guid.NewGuid(), new[] { Guid.NewGuid() });

		var response = await _servicio.ListarPlanillasAsync(new ListarPlanillasRequest(divisionID, hoy.AddDays(-30), hoy.AddDays(-20)));

		response.ShouldBeEmpty();
	}

	[Fact]
	public async Task ListarPlanillasAsync_arma_cada_fila_con_los_registros_y_los_cuatro_contadores_de_la_planilla()
	{
		var divisionID = Guid.NewGuid();
		var ausente = Guid.NewGuid();
		var inasistente = Guid.NewGuid();
		var tarde = Guid.NewGuid();
		var presente1 = Guid.NewGuid();
		var presente2 = Guid.NewGuid();
		var fecha = DateTime.Today.AddDays(-1);
		var planilla = SembrarPlanilla(divisionID, fecha, Guid.NewGuid(), new[] { presente1, presente2, ausente, inasistente, tarde });
		planilla.MarcarAusencia(ausente, "Turno médico");
		planilla.MarcarInasistencia(inasistente);
		planilla.MarcarTardanza(tarde, TimeSpan.FromMinutes(20));

		var response = await _servicio.ListarPlanillasAsync(new ListarPlanillasRequest(divisionID, fecha, fecha));

		var fila = response.ShouldHaveSingleItem();
		fila.PlanillaID.ShouldBe(planilla.Id);
		fila.Fecha.ShouldBe(fecha);
		fila.Cerrada.ShouldBeFalse();
		fila.Registros.ShouldBe(5);
		fila.Presentes.ShouldBe(2);
		fila.Ausencias.ShouldBe(1);
		fila.Inasistencias.ShouldBe(1);
		fila.Tardanzas.ShouldBe(1);
	}

	[Fact]
	public async Task ListarPlanillasAsync_con_el_rango_invertido_da_vuelta_los_extremos()
	{
		var divisionID = Guid.NewGuid();
		var hoy = DateTime.Today;
		var anteayer = hoy.AddDays(-2);
		SembrarPlanilla(divisionID, anteayer, Guid.NewGuid(), new[] { Guid.NewGuid() });
		SembrarPlanilla(divisionID, hoy.AddDays(-1), Guid.NewGuid(), new[] { Guid.NewGuid() });
		SembrarPlanilla(divisionID, hoy, Guid.NewGuid(), new[] { Guid.NewGuid() });

		var bienOrdenado = await _servicio.ListarPlanillasAsync(new ListarPlanillasRequest(divisionID, anteayer, hoy));
		var invertido = await _servicio.ListarPlanillasAsync(new ListarPlanillasRequest(divisionID, hoy, anteayer));

		invertido.Select(p => p.PlanillaID).ShouldBe(bienOrdenado.Select(p => p.PlanillaID));
	}
	#endregion

	#region ConsultarResumenAsync
	[Fact]
	public async Task ConsultarResumenAsync_suma_en_Faltas_las_ausencias_y_las_inasistencias_del_alumno()
	{
		var divisionID = Guid.NewGuid();
		var cursanteID = Guid.NewGuid();
		var hoy = DateTime.Today;
		var planilla1 = SembrarPlanilla(divisionID, hoy.AddDays(-2), Guid.NewGuid(), new[] { cursanteID });
		planilla1.MarcarAusencia(cursanteID, "Turno médico");
		var planilla2 = SembrarPlanilla(divisionID, hoy.AddDays(-1), Guid.NewGuid(), new[] { cursanteID });
		planilla2.MarcarInasistencia(cursanteID);
		SembrarPlanilla(divisionID, hoy, Guid.NewGuid(), new[] { cursanteID });

		var response = await _servicio.ConsultarResumenAsync(new ConsultarResumenRequest(divisionID, hoy.AddDays(-2), hoy));

		var cursante = response.Cursantes.ShouldHaveSingleItem();
		cursante.CursanteID.ShouldBe(cursanteID);
		cursante.Ausencias.ShouldBe(1);
		cursante.Inasistencias.ShouldBe(1);
		cursante.Faltas.ShouldBe(2);
		cursante.Presentes.ShouldBe(1);
	}

	[Fact]
	public async Task ConsultarResumenAsync_cuenta_como_jornadas_solo_las_planillas_del_periodo_en_las_que_el_alumno_tiene_registro()
	{
		var divisionID = Guid.NewGuid();
		var veterano = Guid.NewGuid();
		var incorporado = Guid.NewGuid();
		var hoy = DateTime.Today;
		SembrarPlanilla(divisionID, hoy.AddDays(-2), Guid.NewGuid(), new[] { veterano });
		SembrarPlanilla(divisionID, hoy.AddDays(-1), Guid.NewGuid(), new[] { veterano });
		SembrarPlanilla(divisionID, hoy, Guid.NewGuid(), new[] { veterano, incorporado });

		var response = await _servicio.ConsultarResumenAsync(new ConsultarResumenRequest(divisionID, hoy.AddDays(-2), hoy));

		response.Planillas.ShouldBe(3);
		response.Cursantes.Single(c => c.CursanteID == veterano).Jornadas.ShouldBe(3);
		response.Cursantes.Single(c => c.CursanteID == incorporado).Jornadas.ShouldBe(1);
	}

	[Fact]
	public async Task ConsultarResumenAsync_calcula_el_porcentaje_del_alumno_contando_la_tardanza_como_jornada_asistida()
	{
		var divisionID = Guid.NewGuid();
		var cursanteID = Guid.NewGuid();
		var hoy = DateTime.Today;
		var planilla1 = SembrarPlanilla(divisionID, hoy.AddDays(-2), Guid.NewGuid(), new[] { cursanteID });
		planilla1.MarcarTardanza(cursanteID, TimeSpan.FromMinutes(10));
		var planilla2 = SembrarPlanilla(divisionID, hoy.AddDays(-1), Guid.NewGuid(), new[] { cursanteID });
		planilla2.MarcarAusencia(cursanteID);
		var planilla3 = SembrarPlanilla(divisionID, hoy, Guid.NewGuid(), new[] { cursanteID });
		planilla3.MarcarInasistencia(cursanteID);

		var response = await _servicio.ConsultarResumenAsync(new ConsultarResumenRequest(divisionID, hoy.AddDays(-2), hoy));

		var cursante = response.Cursantes.ShouldHaveSingleItem();
		cursante.Jornadas.ShouldBe(3);
		cursante.Faltas.ShouldBe(2);
		// El alumno no estuvo presente ni un solo día: la única jornada que cuenta como asistida es la
		// tardanza, porque llegó tarde pero estuvo en clase. Sin esa regla el porcentaje daría 0.
		cursante.PorcentajeAsistencia.ShouldBe(33.3m);
		response.PorcentajeAsistencia.ShouldBe(33.3m);
	}

	[Fact]
	public async Task ConsultarResumenAsync_sin_marcas_en_el_periodo_devuelve_porcentaje_cero()
	{
		var divisionID = Guid.NewGuid();
		var hoy = DateTime.Today;
		SembrarPlanilla(divisionID, hoy, Guid.NewGuid(), new[] { Guid.NewGuid() });

		var response = await _servicio.ConsultarResumenAsync(new ConsultarResumenRequest(divisionID, hoy.AddDays(-30), hoy.AddDays(-20)));

		response.Planillas.ShouldBe(0);
		response.Marcas.ShouldBe(0);
		response.PorcentajeAsistencia.ShouldBe(0m);
		response.Cursantes.ShouldBeEmpty();
	}

	[Fact]
	public async Task ConsultarResumenAsync_ordena_los_cursantes_de_mas_faltas_a_menos_y_a_igual_cantidad_primero_el_de_menos_jornadas()
	{
		var divisionID = Guid.NewGuid();
		var dosFaltas = Guid.NewGuid();
		var unaFaltaEnTresJornadas = Guid.NewGuid();
		var unaFaltaEnUnaJornada = Guid.NewGuid();
		var hoy = DateTime.Today;

		var planilla1 = SembrarPlanilla(divisionID, hoy.AddDays(-2), Guid.NewGuid(), new[] { dosFaltas, unaFaltaEnTresJornadas });
		planilla1.MarcarAusencia(dosFaltas);
		planilla1.MarcarAusencia(unaFaltaEnTresJornadas);

		var planilla2 = SembrarPlanilla(divisionID, hoy.AddDays(-1), Guid.NewGuid(), new[] { dosFaltas, unaFaltaEnTresJornadas });
		planilla2.MarcarInasistencia(dosFaltas);

		var planilla3 = SembrarPlanilla(divisionID, hoy, Guid.NewGuid(), new[] { dosFaltas, unaFaltaEnTresJornadas, unaFaltaEnUnaJornada });
		planilla3.MarcarInasistencia(unaFaltaEnUnaJornada);

		var response = await _servicio.ConsultarResumenAsync(new ConsultarResumenRequest(divisionID, hoy.AddDays(-2), hoy));

		response.Cursantes.Select(c => c.CursanteID).ShouldBe(new[] { dosFaltas, unaFaltaEnUnaJornada, unaFaltaEnTresJornadas });
	}

	[Fact]
	public async Task ConsultarResumenAsync_cuenta_en_PlanillasAbiertas_solo_las_que_no_estan_cerradas()
	{
		var divisionID = Guid.NewGuid();
		var hoy = DateTime.Today;
		SembrarPlanilla(divisionID, hoy.AddDays(-2), Guid.NewGuid(), new[] { Guid.NewGuid() }, cerrada: true);
		SembrarPlanilla(divisionID, hoy.AddDays(-1), Guid.NewGuid(), new[] { Guid.NewGuid() }, cerrada: true);
		SembrarPlanilla(divisionID, hoy, Guid.NewGuid(), new[] { Guid.NewGuid() });

		var response = await _servicio.ConsultarResumenAsync(new ConsultarResumenRequest(divisionID, hoy.AddDays(-2), hoy));

		response.Planillas.ShouldBe(3);
		response.PlanillasAbiertas.ShouldBe(1);
	}

	[Fact]
	public async Task ConsultarResumenAsync_totaliza_las_marcas_del_periodo_por_tipo()
	{
		var divisionID = Guid.NewGuid();
		var cursante1 = Guid.NewGuid();
		var cursante2 = Guid.NewGuid();
		var hoy = DateTime.Today;
		var planilla1 = SembrarPlanilla(divisionID, hoy.AddDays(-1), Guid.NewGuid(), new[] { cursante1, cursante2 });
		planilla1.MarcarAusencia(cursante1);
		planilla1.MarcarTardanza(cursante2, TimeSpan.FromMinutes(5));
		var planilla2 = SembrarPlanilla(divisionID, hoy, Guid.NewGuid(), new[] { cursante1, cursante2 });
		planilla2.MarcarInasistencia(cursante2);

		var response = await _servicio.ConsultarResumenAsync(new ConsultarResumenRequest(divisionID, hoy.AddDays(-1), hoy));

		response.Marcas.ShouldBe(4);
		response.Presentes.ShouldBe(1);
		response.Ausencias.ShouldBe(1);
		response.Inasistencias.ShouldBe(1);
		response.Tardanzas.ShouldBe(1);
		response.PorcentajeAsistencia.ShouldBe(50m);
	}

	[Fact]
	public async Task ConsultarResumenAsync_con_el_rango_invertido_da_vuelta_los_extremos()
	{
		var divisionID = Guid.NewGuid();
		var cursanteID = Guid.NewGuid();
		var hoy = DateTime.Today;
		var anteayer = hoy.AddDays(-2);
		var planilla1 = SembrarPlanilla(divisionID, anteayer, Guid.NewGuid(), new[] { cursanteID });
		planilla1.MarcarAusencia(cursanteID);
		SembrarPlanilla(divisionID, hoy.AddDays(-1), Guid.NewGuid(), new[] { cursanteID });
		SembrarPlanilla(divisionID, hoy, Guid.NewGuid(), new[] { cursanteID });

		var bienOrdenado = await _servicio.ConsultarResumenAsync(new ConsultarResumenRequest(divisionID, anteayer, hoy));
		var invertido = await _servicio.ConsultarResumenAsync(new ConsultarResumenRequest(divisionID, hoy, anteayer));

		invertido.Planillas.ShouldBe(bienOrdenado.Planillas);
		invertido.PlanillasAbiertas.ShouldBe(bienOrdenado.PlanillasAbiertas);
		invertido.Marcas.ShouldBe(bienOrdenado.Marcas);
		invertido.Presentes.ShouldBe(bienOrdenado.Presentes);
		invertido.Ausencias.ShouldBe(bienOrdenado.Ausencias);
		invertido.PorcentajeAsistencia.ShouldBe(bienOrdenado.PorcentajeAsistencia);
		invertido.Cursantes.ShouldBe(bienOrdenado.Cursantes);
	}
	#endregion
}
