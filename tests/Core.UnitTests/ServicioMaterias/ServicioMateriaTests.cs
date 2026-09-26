using Core.ServicioMaterias;
using Core.ServicioMaterias.DTOs.Requests;
using Core.ServicioMaterias.Exceptions;
using Core.UnitTests.Infraestructura;
using Domain.Curriculas;
using Domain.Materias;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Core.UnitTests.ServicioMaterias;

/// <summary>
/// <see cref="IServicioMateria"/>: los 4 casos de uso (listar, registrar, modificar, eliminar)
/// con camino feliz y de error, más una prueba por cada excepción propia de <c>Core</c>
/// (<see cref="NombreMateriaDuplicadoException"/>, <see cref="LimiteEspaciosCurricularesException"/>,
/// <see cref="LimiteHorasSemanalesException"/>): ninguna de esas tres reglas vive en el agregado.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class ServicioMateriaTests
{
	private readonly HostDeServicios _host = new();
	private readonly IServicioMateria _servicio;

	public ServicioMateriaTests()
	{
		_servicio = _host.Resolver<IServicioMateria>();
	}

	private Curricula SembrarCurriculaVigente(Guid? cursoID = null)
	{
		var curricula = new CurriculaBuilder().ConCurso(cursoID ?? Guid.NewGuid()).Build();
		_host.UnidadDeTrabajo.CurriculasFake.Sembrar(curricula);
		return curricula;
	}

	private Materia SembrarMateria(Guid curriculaID, string descripcion = "Matemática", int horasCatedra = 4)
	{
		var materia = new MateriaBuilder()
			.ConCurricula(curriculaID)
			.ConDescripcion(descripcion)
			.ConHorasCatedra(horasCatedra)
			.Build();
		_host.UnidadDeTrabajo.MateriasFake.Sembrar(materia);
		return materia;
	}

	#region ListarMateriasSegunCurriculaAsync
	[Fact]
	public async Task ListarMateriasSegunCurriculaAsync_lista_las_materias_de_la_curricula()
	{
		var curricula = SembrarCurriculaVigente();
		SembrarMateria(curricula.Id, "Matemática");
		SembrarMateria(curricula.Id, "Lengua");
		SembrarMateria(Guid.NewGuid(), "De otra currícula");

		var materias = await _servicio.ListarMateriasSegunCurriculaAsync(
			new ListarMateriasSegunCurriculaRequest(curricula.CursoID, curricula.Id));

		materias.Count.ShouldBe(2);
		materias.ShouldAllBe(x => x.CurriculaID == curricula.Id);
	}

	[Fact]
	public async Task ListarMateriasSegunCurriculaAsync_con_una_curricula_inexistente_lanza_ArgumentNullException()
	{
		await Should.ThrowAsync<ArgumentNullException>(() => _servicio.ListarMateriasSegunCurriculaAsync(
			new ListarMateriasSegunCurriculaRequest(Guid.NewGuid(), Guid.NewGuid())));
	}
	#endregion

	#region RegistrarMateriaAsync
	[Fact]
	public async Task RegistrarMateriaAsync_agrega_la_materia_a_la_curricula_y_guarda()
	{
		var curricula = SembrarCurriculaVigente();

		var materiaID = await _servicio.RegistrarMateriaAsync(
			new RegistrarMateriaRequest(curricula.CursoID, curricula.Id, "Matemática", 4));

		var materia = _host.UnidadDeTrabajo.MateriasFake.Elementos.ShouldHaveSingleItem();
		materia.Id.ShouldBe(materiaID);
		materia.CurriculaID.ShouldBe(curricula.Id);
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task RegistrarMateriaAsync_con_una_curricula_inexistente_lanza_ArgumentNullException()
	{
		await Should.ThrowAsync<ArgumentNullException>(() => _servicio.RegistrarMateriaAsync(
			new RegistrarMateriaRequest(Guid.NewGuid(), Guid.NewGuid(), "Matemática", 4)));
	}

	[Fact]
	public async Task RegistrarMateriaAsync_con_nombre_duplicado_lanza_NombreMateriaDuplicadoException()
	{
		var curricula = SembrarCurriculaVigente();
		SembrarMateria(curricula.Id, "Matemática");

		await Should.ThrowAsync<NombreMateriaDuplicadoException>(() => _servicio.RegistrarMateriaAsync(
			new RegistrarMateriaRequest(curricula.CursoID, curricula.Id, "Matemática", 4)));
	}

	[Fact]
	public async Task RegistrarMateriaAsync_al_superar_el_limite_de_espacios_curriculares_lanza_LimiteEspaciosCurricularesException()
	{
		var curricula = SembrarCurriculaVigente();
		for (var i = 0; i < 14; i++)
		{
			SembrarMateria(curricula.Id, $"Materia { i }", horasCatedra: 1);
		}

		await Should.ThrowAsync<LimiteEspaciosCurricularesException>(() => _servicio.RegistrarMateriaAsync(
			new RegistrarMateriaRequest(curricula.CursoID, curricula.Id, "Materia extra", 1)));
	}

	[Fact]
	public async Task RegistrarMateriaAsync_al_superar_el_limite_de_horas_catedra_semanales_lanza_LimiteHorasSemanalesException()
	{
		var curricula = SembrarCurriculaVigente();
		SembrarMateria(curricula.Id, "Matemática", 40);

		await Should.ThrowAsync<LimiteHorasSemanalesException>(() => _servicio.RegistrarMateriaAsync(
			new RegistrarMateriaRequest(curricula.CursoID, curricula.Id, "Física", 1)));
	}
	#endregion

	#region ModificarMateriaAsync
	[Fact]
	public async Task ModificarMateriaAsync_actualiza_descripcion_y_horas_de_la_materia()
	{
		var curricula = SembrarCurriculaVigente();
		var materia = SembrarMateria(curricula.Id, "Matemática", 4);

		await _servicio.ModificarMateriaAsync(
			new ModificarMateriaRequest(curricula.CursoID, curricula.Id, materia.Id, "Matemática II", 6));

		materia.Descripcion.ShouldBe("Matemática II");
		materia.HorasCatedra.ShouldBe(6);
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task ModificarMateriaAsync_con_una_curricula_inexistente_lanza_ArgumentNullException()
	{
		await Should.ThrowAsync<ArgumentNullException>(() => _servicio.ModificarMateriaAsync(
			new ModificarMateriaRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Matemática", 4)));
	}

	[Fact]
	public async Task ModificarMateriaAsync_no_cuenta_las_horas_actuales_de_la_misma_materia_contra_el_limite()
	{
		var curricula = SembrarCurriculaVigente();
		var materia = SembrarMateria(curricula.Id, "Matemática", 10);
		SembrarMateria(curricula.Id, "Lengua", 30);

		// Total actual: 40 (límite). Si el servicio no excluyera la propia materia del cálculo,
		// esto lanzaría LimiteHorasSemanalesException aunque el valor final no cambie.
		await _servicio.ModificarMateriaAsync(
			new ModificarMateriaRequest(curricula.CursoID, curricula.Id, materia.Id, "Matemática", 10));

		materia.HorasCatedra.ShouldBe(10);
	}
	#endregion

	#region EliminarMateriaAsync
	[Fact]
	public async Task EliminarMateriaAsync_quita_la_materia_de_la_curricula()
	{
		var curricula = SembrarCurriculaVigente();
		var materia = SembrarMateria(curricula.Id);

		await _servicio.EliminarMateriaAsync(new EliminarMateriaRequest(curricula.CursoID, curricula.Id, materia.Id));

		_host.UnidadDeTrabajo.MateriasFake.Elementos.ShouldBeEmpty();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task EliminarMateriaAsync_con_una_curricula_inexistente_lanza_ArgumentNullException()
	{
		await Should.ThrowAsync<ArgumentNullException>(() => _servicio.EliminarMateriaAsync(
			new EliminarMateriaRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid())));
	}
	#endregion
}
