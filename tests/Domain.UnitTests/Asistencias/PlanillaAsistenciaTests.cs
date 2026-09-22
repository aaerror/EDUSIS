using Domain.Asistencias;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Asistencias;

/// <summary>
/// Agregado raíz <see cref="PlanillaAsistencia"/>: apertura con cursantes en Presente,
/// marcado de asistencias, incorporación de cursantes, cierre y reapertura con eventos,
/// y consultas de conteo.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class PlanillaAsistenciaTests
{
	#region Apertura
	[Fact]
	public void Una_planilla_nueva_comienza_con_todos_los_cursantes_en_Presente()
	{
		var divisionID = Guid.NewGuid();
		var preceptorID = Guid.NewGuid();
		var cursantes = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

		var planilla = PlanillaAsistencia.Abrir(divisionID, new DateTime(2026, 3, 10), preceptorID, cursantes);

		planilla.Registros.Count.ShouldBe(3);
		planilla.Registros.All(r => r.Tipo == TipoAsistencia.Presente).ShouldBeTrue();
	}

	[Fact]
	public void Los_cursantes_duplicados_se_ignoran_al_abrir()
	{
		var divisionID = Guid.NewGuid();
		var preceptorID = Guid.NewGuid();
		var cursanteID = Guid.NewGuid();
		var cursantes = new[] { cursanteID, cursanteID, Guid.NewGuid() };

		var planilla = PlanillaAsistencia.Abrir(divisionID, new DateTime(2026, 3, 10), preceptorID, cursantes);

		planilla.Registros.Count.ShouldBe(2);
	}

	[Fact]
	public void Abrir_sin_division_lanza_SinDatosDivisionException()
	{
		var preceptorID = Guid.NewGuid();
		var cursantes = new[] { Guid.NewGuid() };

		var ex = Should.Throw<Exception>(() =>
			PlanillaAsistencia.Abrir(Guid.Empty, new DateTime(2026, 3, 10), preceptorID, cursantes));

		ex.GetType().Name.ShouldBe("SinDatosDivisionException");
	}

	[Fact]
	public void Abrir_sin_preceptor_lanza_SinDatosPreceptorException()
	{
		var divisionID = Guid.NewGuid();
		var cursantes = new[] { Guid.NewGuid() };

		var ex = Should.Throw<Exception>(() =>
			PlanillaAsistencia.Abrir(divisionID, new DateTime(2026, 3, 10), Guid.Empty, cursantes));

		ex.GetType().Name.ShouldBe("SinDatosPreceptorException");
	}

	[Fact]
	public void Abrir_con_fecha_futura_lanza_FechaPlanillaFuturaException()
	{
		var divisionID = Guid.NewGuid();
		var preceptorID = Guid.NewGuid();
		var cursantes = new[] { Guid.NewGuid() };
		var fechaFutura = DateTime.Today.AddDays(1);

		var ex = Should.Throw<Exception>(() =>
			PlanillaAsistencia.Abrir(divisionID, fechaFutura, preceptorID, cursantes));

		ex.GetType().Name.ShouldBe("FechaPlanillaFuturaException");
	}

	[Fact]
	public void Abrir_con_cursante_vacio_lanza_SinDatosCursanteException()
	{
		var divisionID = Guid.NewGuid();
		var preceptorID = Guid.NewGuid();
		var cursantes = new[] { Guid.NewGuid(), Guid.Empty };

		var ex = Should.Throw<Exception>(() =>
			PlanillaAsistencia.Abrir(divisionID, new DateTime(2026, 3, 10), preceptorID, cursantes));

		ex.GetType().Name.ShouldBe("SinDatosCursanteException");
	}

	[Fact]
	public void Abrir_sin_cursantes_lanza_PlanillaSinCursantesException()
	{
		var divisionID = Guid.NewGuid();
		var preceptorID = Guid.NewGuid();
		var cursantes = Array.Empty<Guid>();

		var ex = Should.Throw<Exception>(() =>
			PlanillaAsistencia.Abrir(divisionID, new DateTime(2026, 3, 10), preceptorID, cursantes));

		ex.GetType().Name.ShouldBe("PlanillaSinCursantesException");
	}
	#endregion

	#region Marcado
	[Fact]
	public void MarcarPresente_cambia_el_estado_a_Presente()
	{
		var cursanteID = Guid.NewGuid();
		var planilla = new PlanillaAsistenciaBuilder().ConCursantes(cursanteID).Build();

		planilla.MarcarPresente(cursanteID);

		var registro = planilla.RegistroDe(cursanteID);
		registro!.Tipo.ShouldBe(TipoAsistencia.Presente);
		registro.Minutos.ShouldBeNull();
		registro.Observacion.ShouldBeNull();
	}

	[Fact]
	public void MarcarAusencia_cambia_el_estado_a_Ausencia()
	{
		var cursanteID = Guid.NewGuid();
		var planilla = new PlanillaAsistenciaBuilder().ConCursantes(cursanteID).Build();

		planilla.MarcarAusencia(cursanteID, "Sin aviso");

		var registro = planilla.RegistroDe(cursanteID);
		registro!.Tipo.ShouldBe(TipoAsistencia.Ausencia);
		registro.Minutos.ShouldBeNull();
		registro.Observacion.ShouldBe("Sin aviso");
	}

	[Fact]
	public void MarcarInasistencia_cambia_el_estado_a_Inasistencia()
	{
		var cursanteID = Guid.NewGuid();
		var planilla = new PlanillaAsistenciaBuilder().ConCursantes(cursanteID).Build();

		planilla.MarcarInasistencia(cursanteID, "Con aviso");

		var registro = planilla.RegistroDe(cursanteID);
		registro!.Tipo.ShouldBe(TipoAsistencia.Inasistencia);
		registro.Minutos.ShouldBeNull();
		registro.Observacion.ShouldBe("Con aviso");
	}

	[Fact]
	public void MarcarTardanza_cambia_el_estado_a_Tardanza_con_minutos()
	{
		var cursanteID = Guid.NewGuid();
		var planilla = new PlanillaAsistenciaBuilder().ConCursantes(cursanteID).Build();
		var minutos = TimeSpan.FromMinutes(15);

		planilla.MarcarTardanza(cursanteID, minutos, "Colectivo");

		var registro = planilla.RegistroDe(cursanteID);
		registro!.Tipo.ShouldBe(TipoAsistencia.Tardanza);
		registro.Minutos.ShouldBe(minutos);
		registro.Observacion.ShouldBe("Colectivo");
	}

	[Fact]
	public void MarcarTardanza_sin_minutos_lanza_TardanzaSinMinutosException()
	{
		var cursanteID = Guid.NewGuid();
		var planilla = new PlanillaAsistenciaBuilder().ConCursantes(cursanteID).Build();

		var ex = Should.Throw<Exception>(() =>
			planilla.MarcarTardanza(cursanteID, TimeSpan.Zero, "Cero"));

		ex.GetType().Name.ShouldBe("TardanzaSinMinutosException");
	}

	[Fact]
	public void Marcar_un_cursante_no_incluido_lanza_CursanteNoIncluidoException()
	{
		var planilla = new PlanillaAsistenciaBuilder().ConCursantes(Guid.NewGuid()).Build();
		var cursanteNoIncluido = Guid.NewGuid();

		var ex = Should.Throw<Exception>(() =>
			planilla.MarcarAusencia(cursanteNoIncluido));

		ex.GetType().Name.ShouldBe("CursanteNoIncluidoException");
	}

	[Fact]
	public void Marcar_con_la_planilla_cerrada_lanza_PlanillaCerradaException()
	{
		var cursanteID = Guid.NewGuid();
		var planilla = new PlanillaAsistenciaBuilder().ConCursantes(cursanteID).Cerrada().Build();

		var ex = Should.Throw<Exception>(() =>
			planilla.MarcarAusencia(cursanteID));

		ex.GetType().Name.ShouldBe("PlanillaCerradaException");
	}

	[Fact]
	public void La_observacion_no_puede_exceder_140_caracteres()
	{
		var cursanteID = Guid.NewGuid();
		var planilla = new PlanillaAsistenciaBuilder().ConCursantes(cursanteID).Build();
		var observacionLarga = new string('x', 141);

		var ex = Should.Throw<Exception>(() =>
			planilla.MarcarAusencia(cursanteID, observacionLarga));

		ex.GetType().Name.ShouldBe("ObservacionExtensaException");
	}
	#endregion

	#region Incorporar
	[Fact]
	public void IncorporarCursante_agrega_un_nuevo_cursante_en_Presente()
	{
		var cursanteID1 = Guid.NewGuid();
		var cursanteID2 = Guid.NewGuid();
		var planilla = new PlanillaAsistenciaBuilder().ConCursantes(cursanteID1).Build();

		planilla.IncorporarCursante(cursanteID2);

		planilla.Registros.Count.ShouldBe(2);
		var registro = planilla.RegistroDe(cursanteID2);
		registro!.Tipo.ShouldBe(TipoAsistencia.Presente);
	}

	[Fact]
	public void IncorporarCursante_duplicado_lanza_CursanteYaIncluidoException()
	{
		var cursanteID = Guid.NewGuid();
		var planilla = new PlanillaAsistenciaBuilder().ConCursantes(cursanteID).Build();

		var ex = Should.Throw<Exception>(() =>
			planilla.IncorporarCursante(cursanteID));

		ex.GetType().Name.ShouldBe("CursanteYaIncluidoException");
	}

	[Fact]
	public void IncorporarCursante_con_planilla_cerrada_lanza_PlanillaCerradaException()
	{
		var cursanteID1 = Guid.NewGuid();
		var cursanteID2 = Guid.NewGuid();
		var planilla = new PlanillaAsistenciaBuilder().ConCursantes(cursanteID1).Cerrada().Build();

		var ex = Should.Throw<Exception>(() =>
			planilla.IncorporarCursante(cursanteID2));

		ex.GetType().Name.ShouldBe("PlanillaCerradaException");
	}

	[Fact]
	public void IncorporarCursante_vacio_lanza_SinDatosCursanteException()
	{
		var planilla = new PlanillaAsistenciaBuilder().Build();

		var ex = Should.Throw<Exception>(() =>
			planilla.IncorporarCursante(Guid.Empty));

		ex.GetType().Name.ShouldBe("SinDatosCursanteException");
	}
	#endregion

	#region Ciclo
	[Fact]
	public void Cerrar_genera_el_evento_PlanillaAsistenciaCerradaEvent()
	{
		var planilla = new PlanillaAsistenciaBuilder().Build();

		planilla.Cerrar();

		planilla.Cerrada.ShouldBeTrue();
		planilla.Eventos.ShouldHaveSingleItem();
		planilla.Eventos.First().GetType().Name.ShouldBe("PlanillaAsistenciaCerradaEvent");
	}

	[Fact]
	public void Cerrar_dos_veces_lanza_PlanillaCerradaException()
	{
		var planilla = new PlanillaAsistenciaBuilder().Build();
		planilla.Cerrar();

		var ex = Should.Throw<Exception>(() => planilla.Cerrar());

		ex.GetType().Name.ShouldBe("PlanillaCerradaException");
	}

	[Fact]
	public void Reabrir_genera_el_evento_PlanillaAsistenciaReabiertaEvent()
	{
		var planilla = new PlanillaAsistenciaBuilder().Cerrada().Build();
		planilla.LiberarEventos();

		planilla.Reabrir();

		planilla.Cerrada.ShouldBeFalse();
		planilla.Eventos.ShouldHaveSingleItem();
		planilla.Eventos.First().GetType().Name.ShouldBe("PlanillaAsistenciaReabiertaEvent");
	}

	[Fact]
	public void Reabrir_una_planilla_abierta_lanza_PlanillaAbiertaException()
	{
		var planilla = new PlanillaAsistenciaBuilder().Build();

		var ex = Should.Throw<Exception>(() => planilla.Reabrir());

		ex.GetType().Name.ShouldBe("PlanillaAbiertaException");
	}

	[Fact]
	public void Se_puede_marcar_despues_de_reabrir()
	{
		var cursanteID = Guid.NewGuid();
		var planilla = new PlanillaAsistenciaBuilder().ConCursantes(cursanteID).Cerrada().Build();

		planilla.Reabrir();
		planilla.MarcarAusencia(cursanteID);

		var registro = planilla.RegistroDe(cursanteID);
		registro!.Tipo.ShouldBe(TipoAsistencia.Ausencia);
	}
	#endregion

	#region Consultas
	[Fact]
	public void CantidadCon_cuenta_los_registros_por_tipo_de_falta()
	{
		var cursantes = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
		var planilla = new PlanillaAsistenciaBuilder().ConCursantes(cursantes).Build();

		planilla.MarcarPresente(cursantes[0]);
		planilla.MarcarAusencia(cursantes[1]);
		planilla.MarcarTardanza(cursantes[2], TimeSpan.FromMinutes(10));

		planilla.CantidadCon(TipoAsistencia.Presente).ShouldBe(1);
		planilla.CantidadCon(TipoAsistencia.Ausencia).ShouldBe(1);
		planilla.CantidadCon(TipoAsistencia.Inasistencia).ShouldBe(0);
		planilla.CantidadCon(TipoAsistencia.Tardanza).ShouldBe(1);
	}

	[Fact]
	public void RegistroDe_retorna_el_registro_del_cursante_o_nulo()
	{
		var cursanteID = Guid.NewGuid();
		var planilla = new PlanillaAsistenciaBuilder().ConCursantes(cursanteID).Build();

		var registro = planilla.RegistroDe(cursanteID);

		registro.ShouldNotBeNull();
		registro.CursanteID.ShouldBe(cursanteID);

		var registroInexistente = planilla.RegistroDe(Guid.NewGuid());
		registroInexistente.ShouldBeNull();
	}
	#endregion
}
