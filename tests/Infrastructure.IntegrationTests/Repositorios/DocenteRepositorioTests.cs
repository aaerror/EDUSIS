using Domain.Docentes;
using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Infraestructura;
using Infrastructure.IntegrationTests.Infraestructura;
using Shouldly;
using Xunit;

namespace Infrastructure.IntegrationTests.Repositorios;

/// <summary>
/// US3-2: consultas de <c>DocenteRepository</c> con <c>Include</c>, <c>SelectMany</c> y
/// <c>EF.Functions.Like</c> sobre datos sembrados.
/// </summary>
public sealed class DocenteRepositorioTests : BaseIntegracion
{
	public DocenteRepositorioTests(SqlServerFixture fixture)
		: base(fixture)
	{
	}

	private async Task<Docente> SembrarDocenteConPuestoAsync(string legajo, string cuil, string apellido, string nombre)
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

		await using var contexto = Fixture.CrearContexto();
		contexto.Add(docente);
		await contexto.SaveChangesAsync();
		return docente;
	}

	[RequiereSqlServerFact]
	public async Task BuscarDocentePorIDConPuestosAsync_incluye_la_coleccion_de_puestos()
	{
		var sembrado = await SembrarDocenteConPuestoAsync("100200", "20301112229", "Ibáñez", "María");

		using var uow = CrearUnidadDeTrabajo();

		var docente = await uow.Docentes.BuscarDocentePorIDConPuestosAsync(sembrado.Id);

		docente.ShouldNotBeNull();
		docente!.Puestos.Count.ShouldBe(1);
	}

	[RequiereSqlServerFact]
	public async Task EsCuilInvalidoAsync_es_true_cuando_el_cuil_ya_existe()
	{
		await SembrarDocenteConPuestoAsync("100300", "27333444556", "Ñandú", "Carla");

		using var uow = CrearUnidadDeTrabajo();

		(await uow.Docentes.EsCuilInvalidoAsync("27333444556")).ShouldBeTrue();
		(await uow.Docentes.EsCuilInvalidoAsync("20000000009")).ShouldBeFalse();
	}

	[RequiereSqlServerFact]
	public async Task PuestosPorDocenteAsync_devuelve_los_puestos_del_docente()
	{
		var sembrado = await SembrarDocenteConPuestoAsync("100400", "23444555667", "Paz", "Luis");

		using var uow = CrearUnidadDeTrabajo();

		var puestos = await uow.Docentes.PuestosPorDocenteAsync(sembrado.Id);

		puestos.Count.ShouldBe(1);
		puestos.Single().DocenteID.ShouldBe(sembrado.Id);
	}

	[RequiereSqlServerFact]
	public async Task BuscarSegunNombreCompletoAsync_encuentra_por_coincidencia_parcial_de_apellido()
	{
		await SembrarDocenteConPuestoAsync("100500", "20555666778", "Domínguez", "Rocío");

		using var uow = CrearUnidadDeTrabajo();

		var resultado = await uow.Docentes.BuscarSegunNombreCompletoAsync("domín");

		resultado.Count.ShouldBe(1);
		resultado.Single().DatosPersonales.Apellido.ShouldBe("Domínguez");
	}
}
