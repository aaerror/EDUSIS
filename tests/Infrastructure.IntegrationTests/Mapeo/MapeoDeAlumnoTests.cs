using Domain.Alumnos;
using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Infraestructura;
using Infrastructure.IntegrationTests.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Infrastructure.IntegrationTests.Mapeo;

/// <summary>
/// US3-1: persistir y releer un <see cref="Alumno"/> contra SQL Server real. Verifica la
/// jerarquía TPT <c>persona</c>/<c>alumno</c>, el value object <c>Domicilio</c> (<c>OwnsOne</c>)
/// y que los datos con acentos y <c>ñ</c> sobreviven el ida y vuelta sin corrupción.
/// </summary>
public sealed class MapeoDeAlumnoTests : BaseIntegracion
{
	public MapeoDeAlumnoTests(SqlServerFixture fixture)
		: base(fixture)
	{
	}

	[RequiereSqlServerFact]
	public async Task Un_alumno_persistido_se_relee_con_todos_sus_datos_incluidos_acentos_y_ñ()
	{
		var alumno = new AlumnoBuilder()
			.ConDatosPersonales(new DatosPersonalesBuilder()
				.ConApellidoYNombre("Ñáñez Piñón", "José María")
				.ConDocumento("30111222"))
			.ConDomicilio(new DomicilioBuilder()
				.ConCalle("Pasaje Ñuñorco")
				.ConLocalidad("Yerba Buena"))
			.ConLegajo("A-000123")
			.Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.Add(alumno);
			await contexto.SaveChangesAsync();
		}

		await using (var contexto = Fixture.CrearContexto())
		{
			var releido = await contexto.Set<Alumno>()
				.SingleAsync(x => x.Id == alumno.Id);

			releido.Legajo.ShouldBe("A-000123");
			releido.DatosPersonales.Apellido.ShouldBe("Ñáñez Piñón");
			releido.DatosPersonales.Nombre.ShouldBe("José María");
			releido.DatosPersonales.Documento.ShouldBe("30111222");
			releido.Domicilio.Direccion.Calle.ShouldBe("Pasaje Ñuñorco");
			releido.Domicilio.Ubicacion.Localidad.ShouldBe("Yerba Buena");
			releido.Periodo.FechaInicio.ShouldBe(alumno.Periodo.FechaInicio);
		}
	}
}
