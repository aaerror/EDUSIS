using Domain.Catedras.Horarios;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Curriculas;

/// <summary>
/// Value object <see cref="Horario"/> con los enumerados <see cref="Turno"/>, <see cref="Dia"/>
/// y <see cref="DuracionHoraCatedra"/>: camino válido, invariantes (día hábil, franja del turno,
/// duración) e igualdad estructural. <c>DuracionHoraCatedraException</c> es <c>internal</c>: se
/// verifica por el nombre del tipo.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class HorarioTests
{
	#region Camino válido
	[Fact]
	public void Crear_un_horario_valido_guarda_turno_dia_y_horas()
	{
		var horario = new HorarioBuilder()
			.ConTurno(Turno.Mañana)
			.ConDia(Dia.Lunes)
			.ConHoraInicio(new TimeOnly(8, 0))
			.ConDuracionHoraCatedra((int)DuracionHoraCatedra.Cuarenta)
			.Build();

		horario.Turno.ShouldBe(Turno.Mañana);
		horario.DiaSemana.ShouldBe(Dia.Lunes);
		horario.HoraInicio.ShouldBe(new TimeOnly(8, 0));
		horario.HoraFin.ShouldBe(new TimeOnly(8, 40));
	}

	[Fact]
	public void El_turno_tarde_acepta_una_hora_de_inicio_dentro_de_su_franja()
	{
		var builder = new HorarioBuilder().ConTurno(Turno.Tarde).ConHoraInicio(new TimeOnly(14, 0));

		Should.NotThrow(() => builder.Build());
	}

	[Fact]
	public void El_turno_noche_acepta_una_hora_de_inicio_dentro_de_su_franja()
	{
		var builder = new HorarioBuilder().ConTurno(Turno.Noche).ConHoraInicio(new TimeOnly(19, 30));

		Should.NotThrow(() => builder.Build());
	}

	[Fact]
	public void El_turno_noche_rechaza_una_hora_de_inicio_posterior_a_su_franja()
	{
		var builder = new HorarioBuilder().ConTurno(Turno.Noche).ConHoraInicio(new TimeOnly(23, 30));

		Should.Throw<ArgumentException>(() => builder.Build());
	}
	#endregion

	#region Invariantes
	[Theory]
	[InlineData(Dia.Sabado)]
	[InlineData(Dia.Domingo)]
	public void Un_horario_en_fin_de_semana_lanza_ArgumentException(Dia dia)
	{
		var builder = new HorarioBuilder().ConDia(dia);

		Should.Throw<ArgumentException>(() => builder.Build());
	}

	[Fact]
	public void Una_hora_de_inicio_fuera_de_la_franja_del_turno_lanza_ArgumentException()
	{
		var builder = new HorarioBuilder().ConTurno(Turno.Mañana).ConHoraInicio(new TimeOnly(15, 0));

		Should.Throw<ArgumentException>(() => builder.Build());
	}

	[Fact]
	public void Una_duracion_que_no_es_multiplo_de_5_lanza_DuracionHoraCatedraException()
	{
		var builder = new HorarioBuilder().ConDuracionHoraCatedra(42);

		var ex = Should.Throw<Exception>(() => builder.Build());

		ex.GetType().Name.ShouldBe("DuracionHoraCatedraException");
	}

	[Fact]
	public void Una_duracion_menor_a_30_minutos_lanza_ArgumentException()
	{
		var builder = new HorarioBuilder().ConDuracionHoraCatedra(25);

		Should.Throw<ArgumentException>(() => builder.Build());
	}
	#endregion

	#region Igualdad estructural
	[Fact]
	public void Dos_horarios_con_los_mismos_componentes_son_iguales()
	{
		var uno = new HorarioBuilder().Build();
		var otro = new HorarioBuilder().Build();

		uno.Equals(otro).ShouldBeTrue();
		uno.GetHashCode().ShouldBe(otro.GetHashCode());
	}

	[Fact]
	public void Cambiar_el_dia_rompe_la_igualdad()
	{
		var uno = new HorarioBuilder().ConDia(Dia.Lunes).Build();
		var otro = new HorarioBuilder().ConDia(Dia.Martes).Build();

		uno.Equals(otro).ShouldBeFalse();
	}
	#endregion

	#region Duración real
	[Fact]
	public void La_hora_fin_respeta_la_duracion_configurada_no_los_40_minutos_por_defecto()
	{
		// Regresión: el turno Noche (H-009) y este cálculo compartían el mismo código; con una
		// duración de 40 (el valor por defecto del builder) esta prueba pasaría igual contra un
		// cálculo hardcodeado a "+40". Usar 45 prueba que HoraFin realmente usa el parámetro.
		var horario = new HorarioBuilder()
			.ConHoraInicio(new TimeOnly(8, 0))
			.ConDuracionHoraCatedra((int)DuracionHoraCatedra.CuarentaCinco)
			.Build();

		horario.HoraFin.ShouldBe(new TimeOnly(8, 45));
	}

	[Fact]
	public void DuracionHoraCatedra_devuelve_los_minutos_configurados()
	{
		var horario = new HorarioBuilder().ConDuracionHoraCatedra((int)DuracionHoraCatedra.Cincuenta).Build();

		horario.DuracionHoraCatedra().ShouldBe(50);
	}
	#endregion

	#region Superposición
	[Fact]
	public void SeSuperponeCon_en_dias_distintos_no_se_superpone()
	{
		var lunes = new HorarioBuilder().ConDia(Dia.Lunes).Build();
		var martes = new HorarioBuilder().ConDia(Dia.Martes).Build();

		lunes.SeSuperponeCon(martes).ShouldBeFalse();
	}

	[Fact]
	public void SeSuperponeCon_con_bordes_que_se_tocan_no_se_superpone()
	{
		var primero = new HorarioBuilder().ConHoraInicio(new TimeOnly(8, 0)).ConDuracionHoraCatedra(40).Build();
		var segundo = new HorarioBuilder().ConHoraInicio(new TimeOnly(8, 40)).ConDuracionHoraCatedra(40).Build();

		primero.SeSuperponeCon(segundo).ShouldBeFalse();
	}

	[Fact]
	public void SeSuperponeCon_con_contencion_total_se_superpone()
	{
		var contenedor = new HorarioBuilder().ConHoraInicio(new TimeOnly(8, 0)).ConDuracionHoraCatedra(50).Build();
		var contenido = new HorarioBuilder().ConHoraInicio(new TimeOnly(8, 10)).ConDuracionHoraCatedra(30).Build();

		contenedor.SeSuperponeCon(contenido).ShouldBeTrue();
	}

	[Fact]
	public void SeSuperponeCon_con_solapamiento_parcial_se_superpone_en_ambos_sentidos()
	{
		var primero = new HorarioBuilder().ConHoraInicio(new TimeOnly(8, 0)).ConDuracionHoraCatedra(40).Build();
		var segundo = new HorarioBuilder().ConHoraInicio(new TimeOnly(8, 20)).ConDuracionHoraCatedra(40).Build();

		primero.SeSuperponeCon(segundo).ShouldBeTrue();
		segundo.SeSuperponeCon(primero).ShouldBeTrue();
	}
	#endregion
}
