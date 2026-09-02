using Domain.Docentes;
using Domain.Docentes.Puestos;
using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Infraestructura;
using Infrastructure.IntegrationTests.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Infrastructure.IntegrationTests.Mapeo;

/// <summary>
/// US3-1: persistir y releer un <see cref="Docente"/> con un <see cref="Puesto"/> asignado.
/// Verifica la jerarquía TPT <c>persona</c>/<c>docente</c>, la colección <c>Puestos</c>
/// (relación con tabla <c>puesto</c>) y el value object <c>Periodo</c> (<c>OwnsOne</c>).
/// </summary>
public sealed class MapeoDeDocenteTests : BaseIntegracion
{
	public MapeoDeDocenteTests(SqlServerFixture fixture)
		: base(fixture)
	{
	}

	[RequiereSqlServerFact]
	public async Task Un_docente_con_puesto_se_relee_completo()
	{
		var docente = new DocenteBuilder()
			.ConDatosPersonales(new DatosPersonalesBuilder()
				.ConApellidoYNombre("Ibáñez Ñandú", "María José")
				.ConDocumento("28999888")
				.ConSexo(Domain.Personas.Sexo.Femenino)
				.ConEdad(40))
			.ConLegajo("100200")
			.ConCuil("27289998883")
			.ConPuesto(posicion: "Profesor", estado: "Pendiente")
			.Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.Add(docente);
			await contexto.SaveChangesAsync();
		}

		await using (var contexto = Fixture.CrearContexto())
		{
			var releido = await contexto.Set<Docente>()
				.Include(x => x.Puestos)
				.SingleAsync(x => x.Id == docente.Id);

			releido.Legajo.ShouldBe("100200");
			releido.CUIL.ShouldBe("27289998883");
			releido.DatosPersonales.Apellido.ShouldBe("Ibáñez Ñandú");
			releido.Periodo.FechaInicio.ShouldBe(docente.Periodo.FechaInicio);
			releido.Puestos.Count.ShouldBe(1);
			releido.Puestos.Single().Posicion.ShouldBe(Posicion.Profesor);
			releido.Puestos.Single().DocenteID.ShouldBe(docente.Id);
		}
	}
}
