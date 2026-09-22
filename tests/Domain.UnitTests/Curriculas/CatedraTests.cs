using System.Linq;
using Domain.Catedras;
using Domain.Catedras.DomainEvents;
using Domain.Catedras.Horarios;
using Domain.Catedras.SituacionesRevista;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Curriculas;

/// <summary>
/// Agregado <see cref="Catedra"/> (materia dictada en una división): construcción (sólo
/// identificadores y carga horaria como valor, sin conocer el tipo <c>Materia</c>), designación
/// de <see cref="SituacionRevista"/>, el slot único "en funciones" y la carga horaria de
/// <see cref="Horario"/>. Las excepciones del módulo son <c>internal</c>: se verifican por el
/// nombre del tipo.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class CatedraTests
{
	#region Construcción
	[Fact]
	public void Una_catedra_nueva_guarda_materia_division_y_carga_horaria()
	{
		var materiaID = Guid.NewGuid();
		var divisionID = Guid.NewGuid();

		var catedra = new Catedra(materiaID, divisionID, 4);

		catedra.MateriaID.ShouldBe(materiaID);
		catedra.DivisionID.ShouldBe(divisionID);
		catedra.CargaHoraria.ShouldBe(4);
		catedra.SituacionEnFuncionesID.ShouldBeNull();
	}

	[Fact]
	public void Una_catedra_sin_materia_lanza_SinDatosMateriaException()
	{
		var ex = Should.Throw<Exception>(() => new Catedra(Guid.Empty, Guid.NewGuid(), 4));

		ex.GetType().Name.ShouldBe("SinDatosMateriaException");
	}

	[Fact]
	public void Una_catedra_sin_division_lanza_SinDatosDivisionException()
	{
		var ex = Should.Throw<Exception>(() => new Catedra(Guid.NewGuid(), Guid.Empty, 4));

		ex.GetType().Name.ShouldBe("SinDatosDivisionException");
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void Una_catedra_con_carga_horaria_no_positiva_lanza_CargaHorariaInvalidaException(int cargaHoraria)
	{
		var ex = Should.Throw<Exception>(() => new Catedra(Guid.NewGuid(), Guid.NewGuid(), cargaHoraria));

		ex.GetType().Name.ShouldBe("CargaHorariaInvalidaException");
	}
	#endregion

	#region Designar
	[Fact]
	public void Designar_agrega_una_situacion_de_revista_vigente()
	{
		var catedra = new CatedraBuilder().Build();
		var docente = Guid.NewGuid();

		catedra.Designar(docente, Cargo.Titular, DateTime.Today, null);

		var situacion = catedra.SituacionesRevista.ShouldHaveSingleItem();
		situacion.DocenteID.ShouldBe(docente);
		situacion.Cargo.ShouldBe(Cargo.Titular);
	}

	[Fact]
	public void Designar_al_mismo_docente_con_un_cargo_vigente_lanza_DocenteRegistradoException()
	{
		var catedra = new CatedraBuilder().Build();
		var docente = Guid.NewGuid();
		catedra.Designar(docente, Cargo.Titular, DateTime.Today, null);

		var ex = Should.Throw<Exception>(() =>
			catedra.Designar(docente, Cargo.Suplente, DateTime.Today, DateTime.Today.AddMonths(1)));

		ex.GetType().Name.ShouldBe("DocenteRegistradoException");
	}

	[Fact]
	public void Designar_un_cargo_ya_ocupado_por_otro_docente_vigente_lanza_CargoOcupadoException()
	{
		var catedra = new CatedraBuilder().Build();
		catedra.Designar(Guid.NewGuid(), Cargo.Titular, DateTime.Today, null);

		var ex = Should.Throw<Exception>(() => catedra.Designar(Guid.NewGuid(), Cargo.Titular, DateTime.Today, null));

		ex.GetType().Name.ShouldBe("CargoOcupadoException");
	}

	[Fact]
	public void Un_interino_no_puede_convivir_con_un_titular_vigente()
	{
		var catedra = new CatedraBuilder().Build();
		catedra.Designar(Guid.NewGuid(), Cargo.Titular, DateTime.Today, null);

		var ex = Should.Throw<Exception>(() =>
			catedra.Designar(Guid.NewGuid(), Cargo.Interino, DateTime.Today, DateTime.Today.AddMonths(6)));

		ex.GetType().Name.ShouldBe("CargoOcupadoException");
	}

	[Fact]
	public void La_cadena_de_suplencias_admite_varios_suplentes_vigentes()
	{
		var catedra = new CatedraBuilder().Build();
		var titular = catedra.Designar(Guid.NewGuid(), Cargo.Titular, DateTime.Today.AddYears(-1), null);
		var suplente = catedra.Designar(Guid.NewGuid(), Cargo.Suplente, DateTime.Today.AddDays(-10), DateTime.Today.AddMonths(2), titular);

		var suplenteDelSuplente = catedra.Designar(Guid.NewGuid(), Cargo.Suplente, DateTime.Today.AddDays(-1), DateTime.Today.AddDays(10), suplente);

		catedra.SituacionesRevista.Count.ShouldBe(3);
		catedra.SituacionesRevista.Single(situacion => situacion.Id.Equals(suplenteDelSuplente)).ReemplazaA.ShouldBe(suplente);
	}

	[Fact]
	public void Una_suplencia_sin_reemplazado_lanza_SuplenciaSinReemplazoException()
	{
		var catedra = new CatedraBuilder().Build();
		catedra.Designar(Guid.NewGuid(), Cargo.Titular, DateTime.Today.AddYears(-1), null);

		var ex = Should.Throw<Exception>(() =>
			catedra.Designar(Guid.NewGuid(), Cargo.Suplente, DateTime.Today, DateTime.Today.AddMonths(1)));

		ex.GetType().Name.ShouldBe("SuplenciaSinReemplazoException");
	}

	[Fact]
	public void Un_ocupante_que_indica_a_quien_reemplaza_lanza_OcupanteConReemplazoException()
	{
		var catedra = new CatedraBuilder().Build();
		var titular = catedra.Designar(Guid.NewGuid(), Cargo.Titular, DateTime.Today.AddYears(-1), null);

		var ex = Should.Throw<Exception>(() =>
			catedra.Designar(Guid.NewGuid(), Cargo.Interino, DateTime.Today, DateTime.Today.AddMonths(6), titular));

		ex.GetType().Name.ShouldBe("OcupanteConReemplazoException");
	}

	[Fact]
	public void Dos_suplencias_vigentes_sobre_el_mismo_docente_lanzan_DocenteYaReemplazadoException()
	{
		var catedra = new CatedraBuilder().Build();
		var titular = catedra.Designar(Guid.NewGuid(), Cargo.Titular, DateTime.Today.AddYears(-1), null);
		catedra.Designar(Guid.NewGuid(), Cargo.Suplente, DateTime.Today.AddDays(-5), DateTime.Today.AddMonths(1), titular);

		var ex = Should.Throw<Exception>(() =>
			catedra.Designar(Guid.NewGuid(), Cargo.Suplente, DateTime.Today, DateTime.Today.AddMonths(1), titular));

		ex.GetType().Name.ShouldBe("DocenteYaReemplazadoException");
	}

	[Fact]
	public void Una_suplencia_sobre_una_designacion_ajena_lanza_SituacionRevistaNoEncontradaException()
	{
		var catedra = new CatedraBuilder().Build();
		catedra.Designar(Guid.NewGuid(), Cargo.Titular, DateTime.Today.AddYears(-1), null);

		var ex = Should.Throw<Exception>(() =>
			catedra.Designar(Guid.NewGuid(), Cargo.Suplente, DateTime.Today, DateTime.Today.AddMonths(1), Guid.NewGuid()));

		ex.GetType().Name.ShouldBe("SituacionRevistaNoEncontradaException");
	}
	#endregion

	#region Slot en funciones
	[Fact]
	public void PonerEnFunciones_a_un_segundo_docente_reemplaza_al_primero_y_nunca_hay_dos()
	{
		var catedra = new CatedraBuilder().Build();
		var primera = catedra.Designar(Guid.NewGuid(), Cargo.Titular, DateTime.Today, null);
		catedra.PonerEnFunciones(primera);

		var suplente = Guid.NewGuid();
		var segunda = catedra.Designar(suplente, Cargo.Suplente, DateTime.Today, DateTime.Today.AddMonths(3), primera);
		catedra.PonerEnFunciones(segunda);

		catedra.SituacionEnFuncionesID.ShouldBe(segunda);
		catedra.DocenteEnFunciones().ShouldBe(suplente);
	}

	[Fact]
	public void PonerEnFunciones_sobre_una_situacion_que_no_pertenece_a_la_catedra_lanza_SituacionRevistaNoEncontradaException()
	{
		var catedra = new CatedraBuilder().Build();

		var ex = Should.Throw<Exception>(() => catedra.PonerEnFunciones(Guid.NewGuid()));

		ex.GetType().Name.ShouldBe("SituacionRevistaNoEncontradaException");
	}

	[Fact]
	public void PonerEnFunciones_sobre_un_cargo_no_vigente_lanza_CargoNoVigenteException()
	{
		var catedra = new CatedraBuilder().Build();
		catedra.Designar(Guid.NewGuid(), Cargo.Titular, new DateTime(2020, 1, 1), new DateTime(2020, 6, 1));
		var situacion = catedra.SituacionesRevista.Single();

		var ex = Should.Throw<Exception>(() => catedra.PonerEnFunciones(situacion.Id));

		ex.GetType().Name.ShouldBe("CargoNoVigenteException");
	}

	[Fact]
	public void PonerEnFunciones_dos_veces_sobre_la_misma_situacion_lanza_DocenteEnFuncionesException()
	{
		var catedra = new CatedraBuilder().Build();
		catedra.Designar(Guid.NewGuid(), Cargo.Titular, DateTime.Today, null);
		var situacion = catedra.SituacionesRevista.Single();
		catedra.PonerEnFunciones(situacion.Id);

		var ex = Should.Throw<Exception>(() => catedra.PonerEnFunciones(situacion.Id));

		ex.GetType().Name.ShouldBe("DocenteEnFuncionesException");
	}

	[Fact]
	public void RelevarDeFunciones_deja_el_slot_vacio_y_emite_CatedraSinDocenteEnFuncionesEvent()
	{
		var catedra = new CatedraBuilder().Build();
		catedra.Designar(Guid.NewGuid(), Cargo.Titular, DateTime.Today, null);
		catedra.PonerEnFunciones(catedra.SituacionesRevista.Single().Id);

		catedra.RelevarDeFunciones();

		catedra.SituacionEnFuncionesID.ShouldBeNull();
		var evento = catedra.Eventos.ShouldHaveSingleItem().ShouldBeOfType<CatedraSinDocenteEnFuncionesEvent>();
		evento.CatedraID.ShouldBe(catedra.Id);
	}

	[Fact]
	public void RelevarDeFunciones_sin_nadie_en_funciones_lanza_DocenteSinCargoException()
	{
		var catedra = new CatedraBuilder().Build();

		var ex = Should.Throw<Exception>(() => catedra.RelevarDeFunciones());

		ex.GetType().Name.ShouldBe("DocenteSinCargoException");
	}

	[Fact]
	public void FinalizarDesignacion_de_la_situacion_en_funciones_limpia_el_slot()
	{
		var catedra = new CatedraBuilder().Build();
		catedra.Designar(Guid.NewGuid(), Cargo.Titular, DateTime.Today.AddDays(-1), null);
		var situacion = catedra.SituacionesRevista.Single();
		catedra.PonerEnFunciones(situacion.Id);

		catedra.FinalizarDesignacion(situacion.Id);

		catedra.SituacionEnFuncionesID.ShouldBeNull();
		situacion.Estado.ShouldBe(EstadoSituacionRevista.Finalizado);
		var evento = catedra.Eventos.ShouldHaveSingleItem().ShouldBeOfType<CatedraSinDocenteEnFuncionesEvent>();
		evento.CatedraID.ShouldBe(catedra.Id);
	}

	[Fact]
	public void FinalizarDesignacion_de_una_situacion_que_no_esta_en_funciones_no_toca_el_slot()
	{
		var catedra = new CatedraBuilder().Build();
		var enFunciones = catedra.Designar(Guid.NewGuid(), Cargo.Titular, DateTime.Today.AddDays(-1), null);
		catedra.PonerEnFunciones(enFunciones);
		var otra = catedra.Designar(Guid.NewGuid(), Cargo.Suplente, DateTime.Today.AddDays(-1), DateTime.Today.AddMonths(1), enFunciones);

		catedra.FinalizarDesignacion(otra);

		catedra.SituacionEnFuncionesID.ShouldBe(enFunciones);
		catedra.Eventos.ShouldBeNull();
	}
	#endregion

	#region Horarios
	[Fact]
	public void AgregarHorario_por_debajo_de_la_carga_horaria_no_bloquea_el_alta_y_TieneHorasCompletas_es_false()
	{
		var catedra = new CatedraBuilder().ConCargaHoraria(4).Build();

		catedra.AgregarHorario(new HorarioBuilder().Build());

		catedra.HorasAsignadas.ShouldBe(1);
		catedra.HorasSinAsignar.ShouldBe(3);
		catedra.TieneHorasCompletas().ShouldBeFalse();
	}

	[Fact]
	public void AgregarHorario_mas_alla_de_la_carga_horaria_lanza_HorasCatedraCompletasException()
	{
		var catedra = new CatedraBuilder().ConCargaHoraria(1).Build();
		catedra.AgregarHorario(new HorarioBuilder().ConDia(Dia.Lunes).Build());

		var ex = Should.Throw<Exception>(() => catedra.AgregarHorario(new HorarioBuilder().ConDia(Dia.Martes).Build()));

		ex.GetType().Name.ShouldBe("HorasCatedraCompletasException");
		catedra.TieneHorasCompletas().ShouldBeTrue();
	}

	[Fact]
	public void AgregarHorario_duplicado_lanza_HorarioDuplicadoException()
	{
		var catedra = new CatedraBuilder().ConCargaHoraria(4).Build();
		var horario = new HorarioBuilder().Build();
		catedra.AgregarHorario(horario);

		var ex = Should.Throw<Exception>(() => catedra.AgregarHorario(horario));

		ex.GetType().Name.ShouldBe("HorarioDuplicadoException");
	}

	[Fact]
	public void QuitarHorario_no_asignado_lanza_HorarioNoAsignadoException()
	{
		var catedra = new CatedraBuilder().Build();

		var ex = Should.Throw<Exception>(() => catedra.QuitarHorario(new HorarioBuilder().Build()));

		ex.GetType().Name.ShouldBe("HorarioNoAsignadoException");
	}

	[Fact]
	public void QuitarHorario_asignado_lo_quita_de_la_coleccion()
	{
		var catedra = new CatedraBuilder().Build();
		var horario = new HorarioBuilder().Build();
		catedra.AgregarHorario(horario);

		catedra.QuitarHorario(horario);

		catedra.Horarios.ShouldBeEmpty();
	}
	#endregion
}
