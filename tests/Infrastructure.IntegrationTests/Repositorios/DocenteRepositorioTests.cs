using Domain.Docentes;
using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Infraestructura;
using Infrastructure.IntegrationTests.Infraestructura;
using Shouldly;
using Xunit;

namespace Infrastructure.IntegrationTests.Repositorios;

/// <summary>
/// Tests del repositorio de Docente con búsquedas, Include de Puestos y validaciones.
/// </summary>
public sealed class DocenteRepositorioTests : BaseIntegracion
{
	public DocenteRepositorioTests(SqlServerFixture fixture)
		: base(fixture)
	{
	}

	private async Task<Docente> SembrarDocenteAsync(string legajo, string cuil, string apellido, string nombre, bool activo = true)
	{
		var docente = new DocenteBuilder()
			.ConDatosPersonales(new DatosPersonalesBuilder()
				.ConApellidoYNombre(apellido, nombre)
				.ConDocumento("30" + legajo)
				.ConEdad(40))
			.ConLegajo(legajo)
			.ConCuil(cuil)
			.ConPuesto()
			.Build();

		if (!activo)
		{
			docente.Desafectar();
		}

		await using var contexto = Fixture.CrearContexto();
		contexto.Add(docente);
		await contexto.SaveChangesAsync();
		return docente;
	}

	[RequiereSqlServerFact]
	public async Task BuscarPorIDAsync_carga_el_docente_con_sus_puestos()
	{
		var sembrado = await SembrarDocenteAsync("100200", "20301112229", "Ibáñez", "María");

		using var uow = CrearUnidadDeTrabajo();

		var docente = await uow.Docentes.BuscarPorIDAsync(sembrado.Id);

		docente.ShouldNotBeNull();
		docente!.Puestos.Count.ShouldBe(1);
	}

	[RequiereSqlServerFact]
	public async Task BuscarActivosAsync_devuelve_solo_los_docentes_activos()
	{
		await SembrarDocenteAsync("100300", "27333444556", "Ñandú", "Carla", activo: true);
		await SembrarDocenteAsync("100400", "23444555667", "Paz", "Luis", activo: false);

		using var uow = CrearUnidadDeTrabajo();

		var activos = await uow.Docentes.BuscarActivosAsync();

		activos.Count.ShouldBe(1);
		activos.Single().DatosPersonales.Apellido.ShouldBe("Ñandú");
	}

	[RequiereSqlServerFact]
	public async Task ExisteDocenteConLegajoAsync_retorna_true_cuando_existe_docente_con_ese_legajo()
	{
		var sembrado = await SembrarDocenteAsync("100500", "20555666778", "Domínguez", "Rocío");

		using var uow = CrearUnidadDeTrabajo();

		(await uow.Docentes.ExisteDocenteConLegajoAsync(sembrado.Id, "100500")).ShouldBeTrue();
		(await uow.Docentes.ExisteDocenteConLegajoAsync(sembrado.Id, "999999")).ShouldBeFalse();
		(await uow.Docentes.ExisteDocenteConLegajoAsync(Guid.NewGuid(), "100500")).ShouldBeFalse();
	}

	[RequiereSqlServerFact]
	public async Task EsCuilInvalidoAsync_es_true_cuando_el_cuil_ya_existe()
	{
		await SembrarDocenteAsync("100600", "27333444556", "Romero", "Ana");

		using var uow = CrearUnidadDeTrabajo();

		(await uow.Docentes.EsCuilInvalidoAsync("27333444556")).ShouldBeTrue();
		(await uow.Docentes.EsCuilInvalidoAsync("20000000009")).ShouldBeFalse();
	}

	[RequiereSqlServerFact]
	public async Task BuscarSegunNombreCompletoAsync_encuentra_por_coincidencia_parcial_de_apellido()
	{
		await SembrarDocenteAsync("100700", "20666777889", "Fernández", "Pedro");

		using var uow = CrearUnidadDeTrabajo();

		var resultado = await uow.Docentes.BuscarSegunNombreCompletoAsync("fern");

		resultado.Count.ShouldBe(1);
		resultado.Single().DatosPersonales.Apellido.ShouldBe("Fernández");
	}
}
