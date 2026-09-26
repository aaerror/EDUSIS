using Core.ServicioCatedras;
using Core.ServicioCatedras.DTOs.Requests;
using Core.ServicioCatedras.Exceptions;
using Core.UnitTests.Infraestructura;
using Domain.Catedras;
using Domain.Catedras.SituacionesRevista;
using Domain.Curriculas;
using Domain.Divisiones;
using Domain.Docentes;
using Domain.Materias;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Core.UnitTests.ServicioCatedras;

/// <summary>
/// <see cref="IServicioCatedra"/>: los casos de uso de cátedra (crear, designar, poner/relevar de
/// funciones, establecer fin/finalizar designación, agregar/quitar horario, listados) con camino
/// feliz y de error, más una prueba por cada excepción propia de <c>Core</c>
/// (<see cref="CatedraDuplicadaException"/>, <see cref="ColisionHorariaEnDivisionException"/>,
/// <see cref="DocenteEnDosAulasException"/>): ninguna de esas reglas vive en el agregado.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class ServicioCatedraTests
{
	private readonly HostDeServicios _host = new();
	private readonly IServicioCatedra _servicio;

	public ServicioCatedraTests()
	{
		_servicio = _host.Resolver<IServicioCatedra>();
	}

	#region Sembrado
	private Curricula SembrarCurriculaVigente()
	{
		var curricula = new CurriculaBuilder().Build();
		_host.UnidadDeTrabajo.CurriculasFake.Sembrar(curricula);
		return curricula;
	}

	private Curricula SembrarCurriculaNoVigente()
	{
		var curricula = new CurriculaBuilder()
			.ConFechaInicio(DateTime.Today.AddYears(-2))
			.ConFechaFin(DateTime.Today.AddYears(-1))
			.Build();
		_host.UnidadDeTrabajo.CurriculasFake.Sembrar(curricula);
		return curricula;
	}

	private Materia SembrarMateria(Guid curriculaID, int horasCatedra = 4)
	{
		var materia = new MateriaBuilder().ConCurricula(curriculaID).ConHorasCatedra(horasCatedra).Build();
		_host.UnidadDeTrabajo.MateriasFake.Sembrar(materia);
		return materia;
	}

	private Division SembrarDivision()
	{
		var division = new DivisionBuilder().Build();
		_host.UnidadDeTrabajo.DivisionesFake.Sembrar(division);
		return division;
	}

	private Docente SembrarDocente()
	{
		var docente = new DocenteBuilder().Build();
		_host.UnidadDeTrabajo.DocentesFake.Sembrar(docente);
		return docente;
	}

	private Catedra SembrarCatedra(Guid materiaID, Guid divisionID, int cargaHoraria = 4)
	{
		var catedra = new CatedraBuilder()
			.ConMateria(materiaID)
			.ConDivision(divisionID)
			.ConCargaHoraria(cargaHoraria)
			.Build();
		_host.UnidadDeTrabajo.CatedrasFake.Sembrar(catedra);
		return catedra;
	}
	#endregion

	#region CrearCatedraAsync
	[Fact]
	public async Task CrearCatedraAsync_congela_la_carga_horaria_de_la_materia_y_guarda()
	{
		var curricula = SembrarCurriculaVigente();
		var materia = SembrarMateria(curricula.Id, horasCatedra: 6);
		var division = SembrarDivision();

		var catedraID = await _servicio.CrearCatedraAsync(new CrearCatedraRequest(materia.Id, division.Id));

		var catedra = _host.UnidadDeTrabajo.CatedrasFake.Elementos.ShouldHaveSingleItem();
		catedra.Id.ShouldBe(catedraID);
		catedra.MateriaID.ShouldBe(materia.Id);
		catedra.DivisionID.ShouldBe(division.Id);
		catedra.CargaHoraria.ShouldBe(6);
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task CrearCatedraAsync_con_una_materia_inexistente_lanza_ArgumentNullException()
	{
		var division = SembrarDivision();

		await Should.ThrowAsync<ArgumentNullException>(() => _servicio.CrearCatedraAsync(
			new CrearCatedraRequest(Guid.NewGuid(), division.Id)));
	}

	[Fact]
	public async Task CrearCatedraAsync_con_la_curricula_de_la_materia_no_vigente_lanza_ArgumentException()
	{
		var curricula = SembrarCurriculaNoVigente();
		var materia = SembrarMateria(curricula.Id);
		var division = SembrarDivision();

		await Should.ThrowAsync<ArgumentException>(() => _servicio.CrearCatedraAsync(
			new CrearCatedraRequest(materia.Id, division.Id)));
	}

	[Fact]
	public async Task CrearCatedraAsync_duplicada_para_la_misma_materia_y_division_lanza_CatedraDuplicadaException()
	{
		var curricula = SembrarCurriculaVigente();
		var materia = SembrarMateria(curricula.Id);
		var division = SembrarDivision();
		SembrarCatedra(materia.Id, division.Id);

		await Should.ThrowAsync<CatedraDuplicadaException>(() => _servicio.CrearCatedraAsync(
			new CrearCatedraRequest(materia.Id, division.Id)));
	}
	#endregion

	#region DesignarDocenteAsync
	[Fact]
	public async Task DesignarDocenteAsync_agrega_la_situacion_de_revista_a_la_catedra_y_guarda()
	{
		var catedra = SembrarCatedra(Guid.NewGuid(), Guid.NewGuid());
		var docente = SembrarDocente();

		var situacionID = await _servicio.DesignarDocenteAsync(
			new DesignarDocenteRequest(catedra.Id, docente.Id, "Titular", DateTime.Today, null, null));

		catedra.SituacionesRevista.ShouldContain(x => x.Id.Equals(situacionID) && x.DocenteID.Equals(docente.Id));
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task DesignarDocenteAsync_con_una_catedra_inexistente_lanza_ArgumentNullException()
	{
		var docente = SembrarDocente();

		await Should.ThrowAsync<ArgumentNullException>(() => _servicio.DesignarDocenteAsync(
			new DesignarDocenteRequest(Guid.NewGuid(), docente.Id, "Titular", DateTime.Today, null, null)));
	}

	[Fact]
	public async Task DesignarDocenteAsync_con_un_docente_inexistente_lanza_ArgumentNullException()
	{
		var catedra = SembrarCatedra(Guid.NewGuid(), Guid.NewGuid());

		await Should.ThrowAsync<ArgumentNullException>(() => _servicio.DesignarDocenteAsync(
			new DesignarDocenteRequest(catedra.Id, Guid.NewGuid(), "Titular", DateTime.Today, null, null)));
	}

	[Fact]
	public async Task DesignarDocenteAsync_con_un_cargo_invalido_lanza_ArgumentException()
	{
		var catedra = SembrarCatedra(Guid.NewGuid(), Guid.NewGuid());
		var docente = SembrarDocente();

		// Punto de fricción documentado en el informe: el cargo se parsea acá porque
		// SituacionRevista.Crear(string) -la factoría que valida el cargo y lanza
		// CargoInexistenteException- es internal e inalcanzable desde Core.
		await Should.ThrowAsync<ArgumentException>(() => _servicio.DesignarDocenteAsync(
			new DesignarDocenteRequest(catedra.Id, docente.Id, "Bedel", DateTime.Today, null, null)));
	}
	#endregion

	#region PonerEnFuncionesAsync / RelevarDeFuncionesAsync
	[Fact]
	public async Task PonerEnFuncionesAsync_establece_la_situacion_en_funciones_y_guarda()
	{
		var catedra = SembrarCatedra(Guid.NewGuid(), Guid.NewGuid());
		var situacionID = catedra.Designar(Guid.NewGuid(), Cargo.Titular, DateTime.Today, null);

		await _servicio.PonerEnFuncionesAsync(new PonerEnFuncionesRequest(catedra.Id, situacionID));

		catedra.SituacionEnFuncionesID.ShouldBe(situacionID);
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task PonerEnFuncionesAsync_con_una_catedra_inexistente_lanza_ArgumentNullException()
	{
		await Should.ThrowAsync<ArgumentNullException>(() => _servicio.PonerEnFuncionesAsync(
			new PonerEnFuncionesRequest(Guid.NewGuid(), Guid.NewGuid())));
	}

	[Fact]
	public async Task RelevarDeFuncionesAsync_saca_al_docente_de_funciones_y_guarda()
	{
		var catedra = SembrarCatedra(Guid.NewGuid(), Guid.NewGuid());
		var situacionID = catedra.Designar(Guid.NewGuid(), Cargo.Titular, DateTime.Today, null);
		catedra.PonerEnFunciones(situacionID);

		await _servicio.RelevarDeFuncionesAsync(new RelevarDeFuncionesRequest(catedra.Id));

		catedra.SituacionEnFuncionesID.ShouldBeNull();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task RelevarDeFuncionesAsync_sin_nadie_en_funciones_lanza_DocenteSinCargoException()
	{
		var catedra = SembrarCatedra(Guid.NewGuid(), Guid.NewGuid());

		var ex = await Should.ThrowAsync<Exception>(() => _servicio.RelevarDeFuncionesAsync(
			new RelevarDeFuncionesRequest(catedra.Id)));

		ex.GetType().Name.ShouldBe("DocenteSinCargoException");
	}
	#endregion

	#region EstablecerFinDeDesignacionAsync / FinalizarDesignacionAsync
	[Fact]
	public async Task EstablecerFinDeDesignacionAsync_fija_la_fecha_de_fin_de_la_situacion_y_guarda()
	{
		var catedra = SembrarCatedra(Guid.NewGuid(), Guid.NewGuid());
		var situacionID = catedra.Designar(Guid.NewGuid(), Cargo.Titular, DateTime.Today.AddYears(-1), null);
		var fechaFin = DateTime.Today.AddDays(30);

		await _servicio.EstablecerFinDeDesignacionAsync(new EstablecerFinDeDesignacionRequest(catedra.Id, situacionID, fechaFin));

		var situacion = catedra.SituacionesRevista.Single(x => x.Id.Equals(situacionID));
		situacion.Periodo.FechaFin.ShouldBe(fechaFin);
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task EstablecerFinDeDesignacionAsync_con_una_catedra_inexistente_lanza_ArgumentNullException()
	{
		await Should.ThrowAsync<ArgumentNullException>(() => _servicio.EstablecerFinDeDesignacionAsync(
			new EstablecerFinDeDesignacionRequest(Guid.NewGuid(), Guid.NewGuid(), DateTime.Today.AddDays(30))));
	}

	[Fact]
	public async Task FinalizarDesignacionAsync_finaliza_la_situacion_de_revista_y_guarda()
	{
		var catedra = SembrarCatedra(Guid.NewGuid(), Guid.NewGuid());
		var situacionID = catedra.Designar(Guid.NewGuid(), Cargo.Titular, DateTime.Today.AddYears(-1), null);

		await _servicio.FinalizarDesignacionAsync(new FinalizarDesignacionRequest(catedra.Id, situacionID));

		var situacion = catedra.SituacionesRevista.Single(x => x.Id.Equals(situacionID));
		situacion.Estado.ToString().ShouldBe("Finalizado");
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task FinalizarDesignacionAsync_con_una_catedra_inexistente_lanza_ArgumentNullException()
	{
		await Should.ThrowAsync<ArgumentNullException>(() => _servicio.FinalizarDesignacionAsync(
			new FinalizarDesignacionRequest(Guid.NewGuid(), Guid.NewGuid())));
	}
	#endregion

	#region AgregarHorarioAsync / QuitarHorarioAsync
	[Fact]
	public async Task AgregarHorarioAsync_agrega_el_horario_a_la_catedra_y_guarda()
	{
		var catedra = SembrarCatedra(Guid.NewGuid(), Guid.NewGuid());

		await _servicio.AgregarHorarioAsync(new AgregarHorarioRequest(catedra.Id, "Mañana", "Lunes", new TimeOnly(8, 0), 40));

		catedra.Horarios.ShouldHaveSingleItem();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task AgregarHorarioAsync_con_una_catedra_inexistente_lanza_ArgumentNullException()
	{
		await Should.ThrowAsync<ArgumentNullException>(() => _servicio.AgregarHorarioAsync(
			new AgregarHorarioRequest(Guid.NewGuid(), "Mañana", "Lunes", new TimeOnly(8, 0), 40)));
	}

	[Fact]
	public async Task AgregarHorarioAsync_con_un_turno_invalido_lanza_ArgumentException()
	{
		var catedra = SembrarCatedra(Guid.NewGuid(), Guid.NewGuid());

		await Should.ThrowAsync<ArgumentException>(() => _servicio.AgregarHorarioAsync(
			new AgregarHorarioRequest(catedra.Id, "Madrugada", "Lunes", new TimeOnly(8, 0), 40)));
	}

	[Fact]
	public async Task AgregarHorarioAsync_superpuesto_con_otra_catedra_de_la_misma_division_lanza_ColisionHorariaEnDivisionException()
	{
		var divisionID = Guid.NewGuid();
		var catedraExistente = SembrarCatedra(Guid.NewGuid(), divisionID);
		catedraExistente.AgregarHorario(new HorarioBuilder().Build());

		var catedraNueva = SembrarCatedra(Guid.NewGuid(), divisionID);

		await Should.ThrowAsync<ColisionHorariaEnDivisionException>(() => _servicio.AgregarHorarioAsync(
			new AgregarHorarioRequest(catedraNueva.Id, "Mañana", "Lunes", new TimeOnly(8, 0), 40)));
	}

	[Fact]
	public async Task AgregarHorarioAsync_con_el_docente_en_funciones_ocupado_en_otra_catedra_lanza_DocenteEnDosAulasException()
	{
		var docente = SembrarDocente();

		var catedraExistente = SembrarCatedra(Guid.NewGuid(), Guid.NewGuid());
		var situacionID = catedraExistente.Designar(docente.Id, Cargo.Titular, DateTime.Today.AddYears(-1), null);
		catedraExistente.PonerEnFunciones(situacionID);
		catedraExistente.AgregarHorario(new HorarioBuilder().Build());

		var catedraNueva = SembrarCatedra(Guid.NewGuid(), Guid.NewGuid());
		var otraSituacionID = catedraNueva.Designar(docente.Id, Cargo.Titular, DateTime.Today.AddYears(-1), null);
		catedraNueva.PonerEnFunciones(otraSituacionID);

		await Should.ThrowAsync<DocenteEnDosAulasException>(() => _servicio.AgregarHorarioAsync(
			new AgregarHorarioRequest(catedraNueva.Id, "Mañana", "Lunes", new TimeOnly(8, 0), 40)));
	}

	[Fact]
	public async Task QuitarHorarioAsync_quita_el_horario_de_la_catedra_y_guarda()
	{
		var catedra = SembrarCatedra(Guid.NewGuid(), Guid.NewGuid());
		catedra.AgregarHorario(new HorarioBuilder().Build());

		await _servicio.QuitarHorarioAsync(new QuitarHorarioRequest(catedra.Id, "Mañana", "Lunes", new TimeOnly(8, 0), 40));

		catedra.Horarios.ShouldBeEmpty();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task QuitarHorarioAsync_con_una_catedra_inexistente_lanza_ArgumentNullException()
	{
		await Should.ThrowAsync<ArgumentNullException>(() => _servicio.QuitarHorarioAsync(
			new QuitarHorarioRequest(Guid.NewGuid(), "Mañana", "Lunes", new TimeOnly(8, 0), 40)));
	}

	[Fact]
	public async Task QuitarHorarioAsync_de_un_horario_no_asignado_lanza_HorarioNoAsignadoException()
	{
		var catedra = SembrarCatedra(Guid.NewGuid(), Guid.NewGuid());

		var ex = await Should.ThrowAsync<Exception>(() => _servicio.QuitarHorarioAsync(
			new QuitarHorarioRequest(catedra.Id, "Mañana", "Lunes", new TimeOnly(8, 0), 40)));

		ex.GetType().Name.ShouldBe("HorarioNoAsignadoException");
	}
	#endregion

	#region Listar cátedras
	[Fact]
	public async Task ListarCatedrasSegunMateriaAsync_devuelve_las_catedras_de_la_materia()
	{
		var materiaID = Guid.NewGuid();
		SembrarCatedra(materiaID, Guid.NewGuid());
		SembrarCatedra(materiaID, Guid.NewGuid());
		SembrarCatedra(Guid.NewGuid(), Guid.NewGuid());

		var catedras = await _servicio.ListarCatedrasSegunMateriaAsync(new ListarCatedrasSegunMateriaRequest(materiaID));

		catedras.Count.ShouldBe(2);
		catedras.ShouldAllBe(x => x.MateriaID.Equals(materiaID));
	}

	[Fact]
	public async Task ListarCatedrasSegunDivisionAsync_devuelve_las_catedras_de_la_division()
	{
		var divisionID = Guid.NewGuid();
		SembrarCatedra(Guid.NewGuid(), divisionID);
		SembrarCatedra(Guid.NewGuid(), Guid.NewGuid());

		var catedras = await _servicio.ListarCatedrasSegunDivisionAsync(new ListarCatedrasSegunDivisionRequest(divisionID));

		var catedra = catedras.ShouldHaveSingleItem();
		catedra.DivisionID.ShouldBe(divisionID);
	}

	[Fact]
	public async Task ListarCatedrasSegunDocenteAsync_devuelve_las_catedras_designadas_al_docente()
	{
		var docente = SembrarDocente();
		var catedraConDocente = SembrarCatedra(Guid.NewGuid(), Guid.NewGuid());
		catedraConDocente.Designar(docente.Id, Cargo.Titular, DateTime.Today, null);
		SembrarCatedra(Guid.NewGuid(), Guid.NewGuid());

		var catedras = await _servicio.ListarCatedrasSegunDocenteAsync(new ListarCatedrasSegunDocenteRequest(docente.Id));

		var catedra = catedras.ShouldHaveSingleItem();
		catedra.CatedraID.ShouldBe(catedraConDocente.Id);
	}
	#endregion

	#region ListarSituacionesRevistaAsync / ListarHorariosSegunCatedraAsync
	[Fact]
	public async Task ListarSituacionesRevistaAsync_devuelve_las_situaciones_marcando_la_que_esta_en_funciones()
	{
		var catedra = SembrarCatedra(Guid.NewGuid(), Guid.NewGuid());
		var docente = SembrarDocente();
		var situacionID = catedra.Designar(docente.Id, Cargo.Titular, DateTime.Today, null);
		catedra.PonerEnFunciones(situacionID);

		var situaciones = await _servicio.ListarSituacionesRevistaAsync(new ListarSituacionesRevistaRequest(catedra.Id));

		var situacion = situaciones.ShouldHaveSingleItem();
		situacion.SituacionRevistaID.ShouldBe(situacionID);
		situacion.DocenteID.ShouldBe(docente.Id);
		situacion.EnFunciones.ShouldBeTrue();
	}

	[Fact]
	public async Task ListarSituacionesRevistaAsync_con_una_catedra_inexistente_lanza_ArgumentNullException()
	{
		await Should.ThrowAsync<ArgumentNullException>(() => _servicio.ListarSituacionesRevistaAsync(
			new ListarSituacionesRevistaRequest(Guid.NewGuid())));
	}

	[Fact]
	public async Task ListarHorariosSegunCatedraAsync_devuelve_los_horarios_de_la_catedra()
	{
		var catedra = SembrarCatedra(Guid.NewGuid(), Guid.NewGuid());
		catedra.AgregarHorario(new HorarioBuilder().Build());

		var horarios = await _servicio.ListarHorariosSegunCatedraAsync(new ListarHorariosRequest(catedra.Id));

		horarios.ShouldHaveSingleItem();
	}

	[Fact]
	public async Task ListarHorariosSegunCatedraAsync_con_una_catedra_inexistente_lanza_ArgumentNullException()
	{
		await Should.ThrowAsync<ArgumentNullException>(() => _servicio.ListarHorariosSegunCatedraAsync(
			new ListarHorariosRequest(Guid.NewGuid())));
	}
	#endregion
}
