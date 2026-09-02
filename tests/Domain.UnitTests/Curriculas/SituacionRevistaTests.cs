using Domain.Curriculas.Materias.CargosDocentes;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Curriculas;

/// <summary>
/// Entidad <see cref="SituacionRevista"/> (cargo docente sobre una materia) y el enumerado
/// <see cref="EstadoSituacionRevista"/>: invariantes del constructor, funciones de aula y
/// finalización. Las excepciones del módulo son <c>internal</c>: se verifican por el nombre del
/// tipo.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class SituacionRevistaTests
{
	#region Alta
	[Fact]
	public void Una_situacion_de_revista_titular_indeterminada_arranca_aceptada_y_sin_funciones()
	{
		var situacion = new SituacionRevistaBuilder().ConCargo(Cargo.Titular).Build();

		situacion.Estado.ShouldBe(EstadoSituacionRevista.Aceptado);
		situacion.Cargo.ShouldBe(Cargo.Titular);
		situacion.EnFunciones.ShouldBeFalse();
		situacion.EsTemporal().ShouldBeFalse();
		situacion.EsCargoVigente().ShouldBeTrue();
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

	[Fact]
	public void Poner_en_funciones_un_cargo_temporal_desde_el_constructor_lanza_CargoNoVigenteException()
	{
		var builder = new SituacionRevistaBuilder()
			.ConCargo(Cargo.Titular)
			.ConFechaFin(DateTime.Today.AddMonths(3))
			.EnFunciones();

		var ex = Should.Throw<Exception>(() => builder.Build());

		ex.GetType().Name.ShouldBe("CargoNoVigenteException");
	}
	#endregion

	#region Funciones de aula
	[Fact]
	public void EstablecerEnFuncionesDeAula_marca_al_docente_al_frente_del_curso()
	{
		var situacion = new SituacionRevistaBuilder().Build();

		situacion.EstablecerEnFuncionesDeAula();

		situacion.EnFunciones.ShouldBeTrue();
	}

	[Fact]
	public void RelevarFuncionesDeAula_lo_quita_del_frente_del_curso()
	{
		var situacion = new SituacionRevistaBuilder().Build();
		situacion.EstablecerEnFuncionesDeAula();

		situacion.RelevarFuncionesDeAula();

		situacion.EnFunciones.ShouldBeFalse();
	}

	[Fact]
	public void EstablecerEnFuncionesDeAula_sobre_un_cargo_no_vigente_lanza_CargoNoVigenteException()
	{
		var situacion = new SituacionRevistaBuilder()
			.ConCargo(Cargo.Titular)
			.ConFechaInicio(new DateTime(2020, 1, 1))
			.ConFechaFin(new DateTime(2020, 6, 1))
			.Build();

		var ex = Should.Throw<Exception>(() => situacion.EstablecerEnFuncionesDeAula());

		ex.GetType().Name.ShouldBe("CargoNoVigenteException");
	}
	#endregion

	#region Finalización
	[Fact]
	public void EstablecerFechaFinalizacion_sobre_un_cargo_temporal_lanza_CargoConFechaFinalizacionException()
	{
		var situacion = new SituacionRevistaBuilder()
			.ConCargo(Cargo.Titular)
			.ConFechaFin(DateTime.Today.AddMonths(3))
			.Build();

		var ex = Should.Throw<Exception>(() => situacion.EstablecerFechaFinalizacion(DateTime.Today.AddMonths(6)));

		ex.GetType().Name.ShouldBe("CargoConFechaFinalizacionException");
	}

	[Fact]
	public void Finalizar_un_cargo_vigente_lo_deja_finalizado_y_cerrado_hoy()
	{
		var situacion = new SituacionRevistaBuilder().ConCargo(Cargo.Titular).Build();

		situacion.Finalizar();

		situacion.Estado.ShouldBe(EstadoSituacionRevista.Finalizado);
		situacion.Periodo.FechaFin.ShouldBe(DateTime.Today);
		situacion.EnFunciones.ShouldBeFalse();
	}

	[Fact]
	public void Finalizar_un_cargo_que_todavia_no_inicio_lanza_CargoNoIniciadoException()
	{
		var situacion = new SituacionRevistaBuilder()
			.ConCargo(Cargo.Titular)
			.ConFechaInicio(DateTime.Today.AddDays(5))
			.Build();

		var ex = Should.Throw<Exception>(() => situacion.Finalizar());

		ex.GetType().Name.ShouldBe("CargoNoIniciadoException");
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
