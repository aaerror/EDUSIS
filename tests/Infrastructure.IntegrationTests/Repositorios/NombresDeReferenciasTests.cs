using EDUSIS.TestSupport.Infraestructura;
using Infrastructure.IntegrationTests.Infraestructura;
using Shouldly;
using Xunit;

namespace Infrastructure.IntegrationTests.Repositorios;

/// <summary>
/// Consultas de lectura que resuelven el nombre de lo que un agregado referencia por
/// identificador (docentes, divisiones, materias), en una sola ida a la base por listado.
/// </summary>
public sealed class NombresDeReferenciasTests : BaseIntegracion
{
	public NombresDeReferenciasTests(SqlServerFixture fixture)
		: base(fixture)
	{
	}

	[RequiereSqlServerFact]
	public async Task NombresAsync_trae_el_nombre_de_cada_docente_pedido_y_omite_los_inexistentes()
	{
		var docenteID = await SembrarDocenteAsync();
		var inexistente = Guid.NewGuid();

		using var uow = CrearUnidadDeTrabajo();
		var docente = await uow.Docentes.BuscarPorIDAsync(docenteID);
		var nombres = await uow.Docentes.NombresAsync(new[] { docenteID, docenteID, inexistente });

		nombres.Count.ShouldBe(1);
		nombres[docenteID].ShouldBe(docente!.DatosPersonales.NombreCompleto());
	}

	[RequiereSqlServerFact]
	public async Task NombresAsync_incluye_a_un_docente_dado_de_baja()
	{
		var docenteID = await SembrarDocenteAsync();
		using (var baja = CrearUnidadDeTrabajo())
		{
			var docente = await baja.Docentes.BuscarPorIDAsync(docenteID);
			docente!.Desafectar();
			baja.Docentes.Modificar(docente);
			await baja.GuardarCambiosAsync();
		}

		using var uow = CrearUnidadDeTrabajo();
		var nombres = await uow.Docentes.NombresAsync(new[] { docenteID });

		nombres.Keys.ShouldContain(docenteID);
	}

	[RequiereSqlServerFact]
	public async Task DescripcionesAsync_de_divisiones_trae_la_letra_de_cada_una()
	{
		var cursoID = await SembrarCursoAsync();
		var divisionA = await SembrarDivisionAsync(cursoID, "A");
		var divisionB = await SembrarDivisionAsync(cursoID, "B");

		using var uow = CrearUnidadDeTrabajo();
		var letras = await uow.Divisiones.DescripcionesAsync(new[] { divisionA, divisionB, Guid.NewGuid() });

		letras.Count.ShouldBe(2);
		letras[divisionA].ShouldBe("A");
		letras[divisionB].ShouldBe("B");
	}

	[RequiereSqlServerFact]
	public async Task DescripcionesAsync_de_materias_trae_el_nombre_de_cada_una()
	{
		var curriculaID = await SembrarCurriculaAsync(await SembrarCursoAsync());
		var materiaID = await SembrarMateriaAsync(curriculaID, "Historia");

		using var uow = CrearUnidadDeTrabajo();
		var nombres = await uow.Materias.DescripcionesAsync(new[] { materiaID, Guid.NewGuid() });

		nombres.Count.ShouldBe(1);
		nombres[materiaID].ShouldBe("Historia");
	}
}
