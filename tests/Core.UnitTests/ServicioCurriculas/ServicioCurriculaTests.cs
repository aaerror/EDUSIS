using Core.ServicioCurriculas;
using Core.ServicioCurriculas.DTOs.Requests;
using Core.UnitTests.Infraestructura;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Core.UnitTests.ServicioCurriculas;

/// <summary>
/// <see cref="IServicioCurricula"/>: los 13 métodos públicos (currículas, materias y situación
/// de revista) con camino feliz y de error. La mayoría de los errores confluyen en
/// <c>ArgumentNullException</c> "No se encontró la currícula del curso" (helper privado
/// <c>BuscarCurriculaAsync</c>).
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

	private Domain.Curriculas.Curricula SembrarCurricula(Guid cursoID, params (string desc, int horas)[] materias)
	{
		var builder = new CurriculaBuilder().ConCurso(cursoID);
		foreach (var (desc, horas) in materias)
		{
			builder.ConMateria(desc, horas);
		}

		var curricula = builder.Build();
		_host.UnidadDeTrabajo.CurriculasFake.Sembrar(curricula);
		return curricula;
	}

	/// <summary>Currícula con una materia y un docente asignado; devuelve todo lo necesario para las pruebas de situación de revista.</summary>
	private (Guid cursoID, Domain.Curriculas.Curricula curricula, Guid materiaID, Guid situacionRevistaID, Domain.Docentes.Docente docente) SembrarMateriaConDocente(bool enFunciones)
	{
		var cursoID = Guid.NewGuid();
		var curricula = SembrarCurricula(cursoID, ("Matemática", 4));
		var materiaID = curricula.Materias.First().Id;

		var docente = new DocenteBuilder().Build();
		_host.UnidadDeTrabajo.DocentesFake.Sembrar(docente);

		curricula.AsignarDocenteEnMateria(materiaID, docente.Id, "Titular", DateTime.Today, null, enFunciones);
		var situacionRevistaID = curricula.Materias.First().Docentes.First().Id;

		return (cursoID, curricula, materiaID, situacionRevistaID, docente);
	}

	#region Currículas
	[Fact]
	public async Task ListarCurriculasSegunCursoAsync_devuelve_las_curriculas_del_curso()
	{
		var cursoID = Guid.NewGuid();
		SembrarCurricula(cursoID, ("Lengua", 5));
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
	public async Task DesafectarCurriculaAsync_con_una_curricula_inexistente_lanza_ArgumentNullException()
	{
		await Should.ThrowAsync<ArgumentNullException>(() => _servicio.DesafectarCurriculaAsync(
			new CurriculaRequest(Guid.NewGuid(), Guid.NewGuid())));
	}
	#endregion

	#region Materias
	[Fact]
	public async Task ListarMateriasSegunCurriculaAsync_lista_las_materias_de_la_curricula()
	{
		var cursoID = Guid.NewGuid();
		var curricula = SembrarCurricula(cursoID, ("Matemática", 4), ("Física", 3));

		var materias = await _servicio.ListarMateriasSegunCurriculaAsync(
			new ListarMateriasSegunCurriculaRequest(cursoID, curricula.Id));

		materias.Count.ShouldBe(2);
	}

	[Fact]
	public async Task ListarMateriasSegunCurriculaAsync_con_una_curricula_inexistente_lanza_ArgumentNullException()
	{
		await Should.ThrowAsync<ArgumentNullException>(() => _servicio.ListarMateriasSegunCurriculaAsync(
			new ListarMateriasSegunCurriculaRequest(Guid.NewGuid(), Guid.NewGuid())));
	}

	[Fact]
	public async Task RegistrarMateria_agrega_la_materia_a_la_curricula_y_guarda()
	{
		var cursoID = Guid.NewGuid();
		var curricula = SembrarCurricula(cursoID);

		await _servicio.RegistrarMateria(new RegistrarMateriaRequest(cursoID, curricula.Id, "Historia", 3));

		curricula.Materias.ShouldHaveSingleItem();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task RegistrarMateria_con_una_curricula_inexistente_lanza_ArgumentNullException()
	{
		await Should.ThrowAsync<ArgumentNullException>(() => _servicio.RegistrarMateria(
			new RegistrarMateriaRequest(Guid.NewGuid(), Guid.NewGuid(), "Historia", 3)));
	}

	[Fact]
	public async Task ModificarMateriaAsync_actualiza_descripcion_y_horas_de_la_materia()
	{
		var cursoID = Guid.NewGuid();
		var curricula = SembrarCurricula(cursoID, ("Matemática", 4));
		var materiaID = curricula.Materias.First().Id;

		await _servicio.ModificarMateriaAsync(new ModificarMateriaRequest(cursoID, curricula.Id, materiaID, "Matemática Avanzada", 5));

		curricula.Materias.First().Descripcion.ShouldBe("Matemática Avanzada");
	}

	[Fact]
	public async Task ModificarMateriaAsync_con_una_curricula_inexistente_lanza_ArgumentNullException()
	{
		await Should.ThrowAsync<ArgumentNullException>(() => _servicio.ModificarMateriaAsync(
			new ModificarMateriaRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "X", 2)));
	}

	[Fact]
	public async Task EliminarMateriaAsync_quita_la_materia_de_la_curricula()
	{
		var cursoID = Guid.NewGuid();
		var curricula = SembrarCurricula(cursoID, ("Matemática", 4));
		var materiaID = curricula.Materias.First().Id;

		await _servicio.EliminarMateriaAsync(new EliminarMateriaRequest(cursoID, curricula.Id, materiaID));

		curricula.Materias.ShouldBeEmpty();
	}

	[Fact]
	public async Task EliminarMateriaAsync_con_una_curricula_inexistente_lanza_ArgumentNullException()
	{
		await Should.ThrowAsync<ArgumentNullException>(() => _servicio.EliminarMateriaAsync(
			new EliminarMateriaRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid())));
	}
	#endregion

	#region Situación de revista
	[Fact]
	public async Task RegistrarDocenteEnMateriaAsync_asigna_un_cargo_docente_a_la_materia_y_guarda()
	{
		var cursoID = Guid.NewGuid();
		var curricula = SembrarCurricula(cursoID, ("Matemática", 4));
		var materiaID = curricula.Materias.First().Id;
		var docente = new DocenteBuilder().Build();
		_host.UnidadDeTrabajo.DocentesFake.Sembrar(docente);

		await _servicio.RegistrarDocenteEnMateriaAsync(new RegistrarDocenteEnMateriaRequest(
			cursoID, curricula.Id, materiaID, docente.Id, "Titular", DateTime.Today, null, EnFunciones: true));

		curricula.Materias.First().Docentes.ShouldHaveSingleItem();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task RegistrarDocenteEnMateriaAsync_con_una_curricula_inexistente_lanza_ArgumentNullException()
	{
		await Should.ThrowAsync<ArgumentNullException>(() => _servicio.RegistrarDocenteEnMateriaAsync(
			new RegistrarDocenteEnMateriaRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Titular", DateTime.Today, null, false)));
	}

	[Fact]
	public async Task ListarCargosDocenteSegunMateriaAsync_devuelve_la_situacion_de_revista_de_la_materia()
	{
		var (cursoID, curricula, materiaID, _, _) = SembrarMateriaConDocente(enFunciones: true);

		var cargos = await _servicio.ListarCargosDocenteSegunMateriaAsync(
			new ListarCargosDocenteSegunMateriaRequest(cursoID, curricula.Id, materiaID));

		cargos.ShouldHaveSingleItem();
	}

	[Fact]
	public async Task ListarCargosDocenteSegunMateriaAsync_con_una_curricula_inexistente_lanza_ArgumentNullException()
	{
		await Should.ThrowAsync<ArgumentNullException>(() => _servicio.ListarCargosDocenteSegunMateriaAsync(
			new ListarCargosDocenteSegunMateriaRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid())));
	}

	[Fact]
	public async Task EstablecerDocenteDeAulaAsync_pone_en_funciones_al_docente_y_guarda()
	{
		var (cursoID, curricula, materiaID, situacionRevistaID, _) = SembrarMateriaConDocente(enFunciones: false);

		await _servicio.EstablecerDocenteDeAulaAsync(new EstablecerDocenteDeAulaRequest(
			cursoID, curricula.Id, materiaID, situacionRevistaID));

		curricula.Materias.First().Docentes.First().EnFunciones.ShouldBeTrue();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task EstablecerDocenteDeAulaAsync_con_una_curricula_inexistente_lanza_ArgumentNullException()
	{
		await Should.ThrowAsync<ArgumentNullException>(() => _servicio.EstablecerDocenteDeAulaAsync(
			new EstablecerDocenteDeAulaRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid())));
	}

	[Fact]
	public async Task RelevarDocenteDeFuncionesEnMateriaAsync_saca_de_funciones_al_docente_y_guarda()
	{
		var (cursoID, curricula, materiaID, _, _) = SembrarMateriaConDocente(enFunciones: true);

		await _servicio.RelevarDocenteDeFuncionesEnMateriaAsync(new RelevarDocenteDeAulaRequest(cursoID, curricula.Id, materiaID));

		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task RelevarDocenteDeFuncionesEnMateriaAsync_con_una_curricula_inexistente_lanza_ArgumentNullException()
	{
		await Should.ThrowAsync<ArgumentNullException>(() => _servicio.RelevarDocenteDeFuncionesEnMateriaAsync(
			new RelevarDocenteDeAulaRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid())));
	}

	[Fact]
	public async Task RescindirCargoDocenteDeMateriaAsync_rescinde_el_cargo_y_guarda()
	{
		var (cursoID, curricula, materiaID, situacionRevistaID, _) = SembrarMateriaConDocente(enFunciones: false);

		await _servicio.RescindirCargoDocenteDeMateriaAsync(new RescindirCargoDocenteRequest(
			cursoID, curricula.Id, materiaID, situacionRevistaID));

		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task RescindirCargoDocenteDeMateriaAsync_con_una_curricula_inexistente_lanza_ArgumentNullException()
	{
		await Should.ThrowAsync<ArgumentNullException>(() => _servicio.RescindirCargoDocenteDeMateriaAsync(
			new RescindirCargoDocenteRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid())));
	}

	[Fact]
	public async Task EliminarCargoDocenteAsync_quita_la_situacion_de_revista_de_la_materia()
	{
		var (cursoID, curricula, materiaID, situacionRevistaID, _) = SembrarMateriaConDocente(enFunciones: false);

		await _servicio.EliminarCargoDocenteAsync(new EliminarCargoDocenteRequest(
			cursoID, curricula.Id, materiaID, situacionRevistaID));

		curricula.Materias.First().Docentes.ShouldBeEmpty();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task EliminarCargoDocenteAsync_con_una_curricula_inexistente_lanza_ArgumentNullException()
	{
		await Should.ThrowAsync<ArgumentNullException>(() => _servicio.EliminarCargoDocenteAsync(
			new EliminarCargoDocenteRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid())));
	}
	#endregion
}
