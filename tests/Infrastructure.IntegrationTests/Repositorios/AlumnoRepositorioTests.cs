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

	/// <remarks>
	/// <para>
	/// Dos defectos de este helper salieron a la luz la primera vez que la suite se ejecutó contra
	/// SQL Server (2026-11-06), y los dos lo hacían inservible:
	/// </para>
	/// <para>
	/// (1) <c>legajo</c> venía con <c>""</c> por defecto y <c>ConLegajo</c> lo aplicaba igual,
	/// <strong>pisando el <c>AL0001</c> válido del builder</strong>: el constructor de
	/// <see cref="Alumno"/> exige <c>^[A-Z]{2}\d{4}$</c>, así que las cinco pruebas que no pasaban
	/// legajo morían en un <see cref="FormatException"/> antes de llegar a la base. Ahora el valor
	/// nulo significa "el del builder" y no "vacío".
	/// </para>
	/// <para>
	/// (2) No tocaba el email, así que los dos alumnos de una misma prueba nacían con el
	/// <c>maria.gonzalez@correo.com</c> del <c>PersonaBuilder</c> y el segundo chocaba contra
	/// <c>IX_persona_email</c>, que es único. Se deriva del documento, que ya es distinto por prueba
	/// y es el dato por el que estas pruebas buscan.
	/// </para>
	/// <para>
	/// Es el mismo cuidado que ya tienen los helpers compartidos de <c>ColeccionSqlServer</c>, que
	/// derivan legajo, CUIL, email y documento de un contador. Estos helpers locales existen porque
	/// las pruebas de este repositorio necesitan fijar apellido y documento; lo que les faltaba era
	/// la unicidad.
	/// </para>
	/// </remarks>
	private async Task<Alumno> SembrarAlumnoAsync(string documento, string apellido, string nombre, string? legajo = null)
	{
		var builder = new AlumnoBuilder()
			.ConDatosPersonales(new DatosPersonalesBuilder()
				.ConApellidoYNombre(apellido, nombre)
				.ConDocumento(documento))
			.ConEmail($"alumno.{documento}@correo.com");

		if (legajo is not null)
		{
			builder.ConLegajo(legajo);
		}

		var alumno = builder.Build();

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
		await SembrarAlumnoAsync("29333444", "Rodríguez", "Carlos", "AL9001");

		// "ALU001" no es un legajo válido —el formato es dos letras y cuatro dígitos— así que esta
		// prueba nunca había llegado a insertar nada.

		using var uow = CrearUnidadDeTrabajo();

		(await uow.Alumnos.EsLegajoInvalidoAsync("AL9001")).ShouldBeTrue();
		(await uow.Alumnos.EsLegajoInvalidoAsync("AL9999")).ShouldBeFalse();
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
