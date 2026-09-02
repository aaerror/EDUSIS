using Domain.Alumnos;
using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Infraestructura;
using Infrastructure.IntegrationTests.Infraestructura;
using Shouldly;
using Xunit;

namespace Infrastructure.IntegrationTests.Repositorios;

/// <summary>
/// US3-2: las consultas concretas de <c>AlumnoRepository</c> (<c>EF.Functions.Like</c> sobre el
/// value object <c>DatosPersonales</c>) devuelven el conjunto esperado sobre datos sembrados.
/// </summary>
public sealed class AlumnoRepositorioTests : BaseIntegracion
{
	public AlumnoRepositorioTests(SqlServerFixture fixture)
		: base(fixture)
	{
	}

	private async Task<Alumno> SembrarAlumnoAsync(string documento, string apellido, string nombre)
	{
		var alumno = new AlumnoBuilder()
			.ConDatosPersonales(new DatosPersonalesBuilder()
				.ConApellidoYNombre(apellido, nombre)
				.ConDocumento(documento))
			.Build();

		await using var contexto = Fixture.CrearContexto();
		contexto.Add(alumno);
		await contexto.SaveChangesAsync();
		return alumno;
	}

	[RequiereSqlServerFact]
	public async Task EsDocumentoInvalidoAsync_es_true_cuando_el_documento_ya_esta_registrado()
	{
		await SembrarAlumnoAsync("30111222", "Ñáñez", "José");

		using var uow = CrearUnidadDeTrabajo();

		(await uow.Alumnos.EsDocumentoInvalidoAsync("30111222")).ShouldBeTrue();
		(await uow.Alumnos.EsDocumentoInvalidoAsync("99999999")).ShouldBeFalse();
	}

	[RequiereSqlServerFact]
	public async Task BuscarPorDocumentoAsync_devuelve_el_alumno_con_ese_documento()
	{
		var sembrado = await SembrarAlumnoAsync("28777666", "Gómez", "Ana");

		using var uow = CrearUnidadDeTrabajo();

		var encontrado = await uow.Alumnos.BuscarPorDocumentoAsync("28777666");

		encontrado.ShouldNotBeNull();
		encontrado!.Id.ShouldBe(sembrado.Id);
		encontrado.DatosPersonales.Apellido.ShouldBe("Gómez");
	}

	/// <summary>
	/// Defecto H-020: <c>AlumnoRepository.BuscarPorNombreCompletoAsync</c> compara contra
	/// <c>x.DatosPersonales.NombreCompleto()</c> — una llamada a método del dominio que EF Core
	/// no puede traducir a SQL. La consulta revienta con "could not be translated". Ver
	/// <c>hallazgos.md</c>.
	/// </summary>
	[Fact(Skip = "Defecto H-020: BuscarPorNombreCompletoAsync usa DatosPersonales.NombreCompleto() no traducible; ver hallazgos.md")]
	public async Task BuscarPorNombreCompletoAsync_devuelve_el_alumno_por_apellido_y_nombre()
	{
		await SembrarAlumnoAsync("27555444", "Pérez", "Juan");

		using var uow = CrearUnidadDeTrabajo();

		var encontrado = await uow.Alumnos.BuscarPorNombreCompletoAsync("Pérez, Juan");

		encontrado.ShouldNotBeNull();
	}
}
