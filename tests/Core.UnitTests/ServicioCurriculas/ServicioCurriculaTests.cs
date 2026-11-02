using Core.ServicioCurriculas;
using Core.ServicioCurriculas.DTOs.Requests;
using Core.UnitTests.Infraestructura;
using Domain.Curriculas.Exceptions;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Core.UnitTests.ServicioCurriculas;

/// <summary>
/// <see cref="IServicioCurricula"/>: <c>ListarCurriculasSegunCursoAsync</c>,
/// <c>RegistrarCurriculaAsync</c> y <c>DesafectarCurriculaAsync</c> son los únicos casos de uso
/// que quedan en este servicio; materias y situación de revista se reimplementan en servicios
/// nuevos por agregado (<c>ServicioMateria</c>, <c>ServicioCatedra</c>).
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class ServicioCurriculaTests
{
	private readonly HostDeServicios _host = new();
	private readonly IServicioCurricula _servicio;

	public ServicioCurriculaTests()
	{
		_servicio = _host.Resolver<IServicioCurricula>();
	}

	private Domain.Curriculas.Curricula SembrarCurricula(Guid cursoID)
	{
		var curricula = new CurriculaBuilder().ConCurso(cursoID).Build();
		_host.UnidadDeTrabajo.CurriculasFake.Sembrar(curricula);
		return curricula;
	}

	/// <summary>
	/// Siembra materias de la currícula: los contadores del repo fake son reales, así que la
	/// ocupación se arma sembrando el contenido y no amañando un número.
	/// </summary>
	private void SembrarMaterias(Guid curriculaID, int cantidad, int horasCatedra)
	{
		for (var i = 0; i < cantidad; i++)
		{
			_host.UnidadDeTrabajo.MateriasFake.Sembrar(new MateriaBuilder()
				.ConCurricula(curriculaID)
				.ConDescripcion($"Materia { i }")
				.ConHorasCatedra(horasCatedra)
				.Build());
		}
	}

	#region Currículas
	[Fact]
	public async Task ListarCurriculasSegunCursoAsync_devuelve_las_curriculas_del_curso()
	{
		var cursoID = Guid.NewGuid();
		SembrarCurricula(cursoID);
		SembrarCurricula(Guid.NewGuid());

		var curriculas = await _servicio.ListarCurriculasSegunCursoAsync(new CursoRequest(cursoID));

		curriculas.ShouldHaveSingleItem();
		curriculas.First().CursoID.ShouldBe(cursoID);
	}

	[Fact]
	public async Task ListarCurriculasSegunCursoAsync_sin_curriculas_devuelve_coleccion_vacia()
	{
		var curriculas = await _servicio.ListarCurriculasSegunCursoAsync(new CursoRequest(Guid.NewGuid()));

		curriculas.ShouldBeEmpty();
	}

	[Fact]
	public async Task ListarCurriculasSegunCursoAsync_marca_vigente_una_curricula_con_fecha_de_fin_futura()
	{
		// La deducción vieja -Vigente = FechaFin is null- la daba por cerrada: Vigente sale de
		// Curricula.EstaVigente(), que es la misma regla con la que el dominio acepta materias.
		var cursoID = Guid.NewGuid();
		var curricula = new CurriculaBuilder()
			.ConCurso(cursoID)
			.ConFechaInicio(DateTime.Today.AddMonths(-1))
			.ConFechaFin(DateTime.Today.AddMonths(6))
			.Build();
		_host.UnidadDeTrabajo.CurriculasFake.Sembrar(curricula);

		var curriculas = await _servicio.ListarCurriculasSegunCursoAsync(new CursoRequest(cursoID));

		var response = curriculas.ShouldHaveSingleItem();
		response.FechaFin.ShouldNotBeNull();
		response.Vigente.ShouldBeTrue();
	}

	[Fact]
	public async Task ListarCurriculasSegunCursoAsync_marca_no_vigente_una_curricula_con_fecha_de_fin_pasada()
	{
		var cursoID = Guid.NewGuid();
		var curricula = new CurriculaBuilder()
			.ConCurso(cursoID)
			.ConFechaInicio(DateTime.Today.AddYears(-2))
			.ConFechaFin(DateTime.Today.AddYears(-1))
			.Build();
		_host.UnidadDeTrabajo.CurriculasFake.Sembrar(curricula);

		var curriculas = await _servicio.ListarCurriculasSegunCursoAsync(new CursoRequest(cursoID));

		curriculas.ShouldHaveSingleItem().Vigente.ShouldBeFalse();
	}

	[Fact]
	public async Task ListarCurriculasSegunCursoAsync_marca_vigente_una_curricula_sin_fecha_de_fin()
	{
		var cursoID = Guid.NewGuid();
		SembrarCurricula(cursoID);

		var curriculas = await _servicio.ListarCurriculasSegunCursoAsync(new CursoRequest(cursoID));

		var response = curriculas.ShouldHaveSingleItem();
		response.FechaFin.ShouldBeNull();
		response.Vigente.ShouldBeTrue();
	}

	[Fact]
	public async Task RegistrarCurriculaAsync_da_de_alta_la_curricula_dentro_de_una_transaccion()
	{
		var cursoID = Guid.NewGuid();

		await _servicio.RegistrarCurriculaAsync(new RegistrarCurriculaRequest(cursoID, DateTime.Today, null));

		_host.UnidadDeTrabajo.CurriculasFake.Elementos.ShouldHaveSingleItem();
		_host.UnidadDeTrabajo.LlamadasDeTransaccion.ShouldContain("BeginTransactionAsync");
		_host.UnidadDeTrabajo.LlamadasDeTransaccion.ShouldContain("CommitTransactionAsync");
	}

	[Fact]
	public async Task RegistrarCurriculaAsync_ante_un_error_hace_rollback_y_propaga()
	{
		// FechaFin anterior a FechaInicio: el constructor de RangoFechas revienta dentro del try.
		await Should.ThrowAsync<Exception>(() => _servicio.RegistrarCurriculaAsync(
			new RegistrarCurriculaRequest(Guid.NewGuid(), DateTime.Today, DateTime.Today.AddDays(-10))));

		_host.UnidadDeTrabajo.LlamadasDeTransaccion.ShouldContain("RollbackTransactionAsync");
	}

	[Fact]
	public async Task DesafectarCurriculaAsync_cierra_el_periodo_de_la_curricula_y_guarda()
	{
		var cursoID = Guid.NewGuid();
		var curricula = SembrarCurricula(cursoID);

		await _servicio.DesafectarCurriculaAsync(new CurriculaRequest(cursoID, curricula.Id));

		curricula.Periodo.FechaFin.ShouldNotBeNull();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task DesafectarCurriculaAsync_con_una_curricula_inexistente_lanza_CurriculaNoEncontradaException()
	{
		await Should.ThrowAsync<CurriculaNoEncontradaException>(() => _servicio.DesafectarCurriculaAsync(
			new CurriculaRequest(Guid.NewGuid(), Guid.NewGuid())));
	}
	#endregion

	#region ConsultarOcupacionAsync
	[Fact]
	public async Task ConsultarOcupacionAsync_devuelve_los_dos_consumos_y_los_dos_topes()
	{
		var cursoID = Guid.NewGuid();
		var curricula = SembrarCurricula(cursoID);
		SembrarMaterias(curricula.Id, cantidad: 3, horasCatedra: 5);
		// Materia de otra currícula: no entra en ninguno de los dos contadores.
		SembrarMaterias(Guid.NewGuid(), cantidad: 1, horasCatedra: 8);

		var ocupacion = await _servicio.ConsultarOcupacionAsync(new CurriculaRequest(cursoID, curricula.Id));

		ocupacion.EspaciosOcupados.ShouldBe(3);
		ocupacion.EspaciosMaximos.ShouldBe(14);
		ocupacion.HorasOcupadas.ShouldBe(15);
		ocupacion.HorasMaximas.ShouldBe(40);
		ocupacion.EspaciosDisponibles.ShouldBe(11);
		ocupacion.HorasDisponibles.ShouldBe(25);
		ocupacion.AdmiteOtraMateria.ShouldBeTrue();
	}

	[Fact]
	public async Task ConsultarOcupacionAsync_de_una_curricula_vacia_devuelve_los_topes_enteros_disponibles()
	{
		var cursoID = Guid.NewGuid();
		var curricula = SembrarCurricula(cursoID);

		var ocupacion = await _servicio.ConsultarOcupacionAsync(new CurriculaRequest(cursoID, curricula.Id));

		ocupacion.EspaciosOcupados.ShouldBe(0);
		ocupacion.HorasOcupadas.ShouldBe(0);
		ocupacion.EspaciosDisponibles.ShouldBe(14);
		ocupacion.HorasDisponibles.ShouldBe(40);
		ocupacion.AdmiteOtraMateria.ShouldBeTrue();
	}

	[Fact]
	public async Task ConsultarOcupacionAsync_con_un_espacio_y_una_hora_libres_admite_otra_materia()
	{
		var cursoID = Guid.NewGuid();
		var curricula = SembrarCurricula(cursoID);
		// 13 espacios de 3 horas: queda justo un espacio y una hora.
		SembrarMaterias(curricula.Id, cantidad: 13, horasCatedra: 3);

		var ocupacion = await _servicio.ConsultarOcupacionAsync(new CurriculaRequest(cursoID, curricula.Id));

		ocupacion.EspaciosDisponibles.ShouldBe(1);
		ocupacion.HorasDisponibles.ShouldBe(1);
		ocupacion.AdmiteOtraMateria.ShouldBeTrue();
	}

	[Fact]
	public async Task ConsultarOcupacionAsync_sin_espacios_libres_no_admite_otra_materia()
	{
		var cursoID = Guid.NewGuid();
		var curricula = SembrarCurricula(cursoID);
		SembrarMaterias(curricula.Id, cantidad: 14, horasCatedra: 1);

		var ocupacion = await _servicio.ConsultarOcupacionAsync(new CurriculaRequest(cursoID, curricula.Id));

		ocupacion.EspaciosDisponibles.ShouldBe(0);
		ocupacion.HorasDisponibles.ShouldBe(26);
		ocupacion.AdmiteOtraMateria.ShouldBeFalse();
	}

	[Fact]
	public async Task ConsultarOcupacionAsync_sin_horas_libres_no_admite_otra_materia()
	{
		var cursoID = Guid.NewGuid();
		var curricula = SembrarCurricula(cursoID);
		SembrarMaterias(curricula.Id, cantidad: 10, horasCatedra: 4);

		var ocupacion = await _servicio.ConsultarOcupacionAsync(new CurriculaRequest(cursoID, curricula.Id));

		ocupacion.HorasDisponibles.ShouldBe(0);
		ocupacion.EspaciosDisponibles.ShouldBe(4);
		ocupacion.AdmiteOtraMateria.ShouldBeFalse();
	}

	[Fact]
	public async Task ConsultarOcupacionAsync_con_los_espacios_por_encima_del_tope_no_devuelve_disponibles_negativos()
	{
		var cursoID = Guid.NewGuid();
		var curricula = SembrarCurricula(cursoID);
		// 15 espacios sobre un tope de 14: el excedente es dato viejo que el tope de hoy ya no admite.
		SembrarMaterias(curricula.Id, cantidad: 15, horasCatedra: 1);

		var ocupacion = await _servicio.ConsultarOcupacionAsync(new CurriculaRequest(cursoID, curricula.Id));

		ocupacion.EspaciosOcupados.ShouldBe(15);
		ocupacion.EspaciosDisponibles.ShouldBe(0);
		ocupacion.AdmiteOtraMateria.ShouldBeFalse();
	}

	[Fact]
	public async Task ConsultarOcupacionAsync_con_las_horas_por_encima_del_tope_no_devuelve_disponibles_negativos()
	{
		var cursoID = Guid.NewGuid();
		var curricula = SembrarCurricula(cursoID);
		// 44 horas sobre un tope de 40, con los espacios todavía holgados.
		SembrarMaterias(curricula.Id, cantidad: 11, horasCatedra: 4);

		var ocupacion = await _servicio.ConsultarOcupacionAsync(new CurriculaRequest(cursoID, curricula.Id));

		ocupacion.HorasOcupadas.ShouldBe(44);
		ocupacion.HorasDisponibles.ShouldBe(0);
		ocupacion.EspaciosDisponibles.ShouldBe(3);
		ocupacion.AdmiteOtraMateria.ShouldBeFalse();
	}

	[Fact]
	public async Task ConsultarOcupacionAsync_con_una_curricula_inexistente_lanza_CurriculaNoEncontradaException()
	{
		await Should.ThrowAsync<CurriculaNoEncontradaException>(() => _servicio.ConsultarOcupacionAsync(
			new CurriculaRequest(Guid.NewGuid(), Guid.NewGuid())));
	}

	[Fact]
	public async Task ConsultarOcupacionAsync_con_una_curricula_de_otro_curso_lanza_CurriculaNoEncontradaException()
	{
		var curricula = SembrarCurricula(Guid.NewGuid());
		SembrarMaterias(curricula.Id, cantidad: 2, horasCatedra: 4);

		// El número sería correcto, pero sobre el objeto equivocado: la currícula no es del curso.
		await Should.ThrowAsync<CurriculaNoEncontradaException>(() => _servicio.ConsultarOcupacionAsync(
			new CurriculaRequest(Guid.NewGuid(), curricula.Id)));
	}
	#endregion
}
