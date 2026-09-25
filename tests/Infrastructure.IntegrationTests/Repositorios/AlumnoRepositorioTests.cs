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

	private async Task<Alumno> SembrarAlumnoAsync(string documento, string apellido, string nombre, string legajo = "")
	{
		var alumno = new AlumnoBuilder()
			.ConDatosPersonales(new DatosPersonalesBuilder()
				.ConApellidoYNombre(apellido, nombre)
				.ConDocumento(documento))
			.ConLegajo(legajo)
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

	[RequiereSqlServerFact]
	public async Task EsLegajoInvalidoAsync_retorna_true_cuando_el_legajo_ya_existe()
	{
		await SembrarAlumnoAsync("29333444", "Rodríguez", "Carlos", "ALU001");

		using var uow = CrearUnidadDeTrabajo();

		(await uow.Alumnos.EsLegajoInvalidoAsync("ALU001")).ShouldBeTrue();
		(await uow.Alumnos.EsLegajoInvalidoAsync("ALU999")).ShouldBeFalse();
	}

	/// <summary>
	/// H-020 (corregido): la búsqueda se traduce a SQL sobre las columnas <c>apellido</c> y
	/// <c>nombre</c> con el mismo formato que <c>DatosPersonales.NombreCompleto()</c>.
	/// </summary>
	[RequiereSqlServerFact]
	public async Task BuscarPorNombreCompletoAsync_devuelve_el_alumno_por_apellido_y_nombre()
	{
		var alumno = await SembrarAlumnoAsync("27555444", "Pérez", "Juan");

		using var uow = CrearUnidadDeTrabajo();

		var encontrado = await uow.Alumnos.BuscarPorNombreCompletoAsync("Pérez, Juan");

		encontrado.ShouldNotBeNull();
		encontrado.Id.ShouldBe(alumno.Id);
	}

	[RequiereSqlServerFact]
	public async Task BuscarPorNombreCompletoAsync_no_encuentra_por_coincidencia_parcial()
	{
		await SembrarAlumnoAsync("27555445", "Pérez", "Juana");

		using var uow = CrearUnidadDeTrabajo();

		var encontrado = await uow.Alumnos.BuscarPorNombreCompletoAsync("Pérez, Juan");

		encontrado.ShouldBeNull();
	}
}
