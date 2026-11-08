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
	/// <c>nombre</c>, con <c>LIKE '%…%'</c> en las dos.
	/// </summary>
	/// <remarks>
	/// Reemplaza a las dos pruebas de <c>BuscarPorNombreCompletoAsync</c>, una de las cuales
	/// afirmaba que la coincidencia parcial NO encuentra nada. Era cierto y era el defecto: ese
	/// contrato exigía "Pérez, Juan" con su coma y sus tildes, y es el que dejaba a la pantalla de
	/// Alumnos sin índice.
	/// </remarks>
	[RequiereSqlServerFact]
	public async Task BuscarSegunNombreCompletoAsync_encuentra_por_coincidencia_parcial_del_apellido()
	{
		var juan = await SembrarAlumnoAsync("27555444", "Ramirez", "Juan", "AL9110");
		var juana = await SembrarAlumnoAsync("27555445", "Ramirez", "Juana", "AL9111");
		await SembrarAlumnoAsync("30111222", "Gomez", "Lucia", "AL9112");

		using var uow = CrearUnidadDeTrabajo();

		var encontrados = await uow.Alumnos.BuscarSegunNombreCompletoAsync("amir");

		encontrados.Select(x => x.Id).ShouldBe(new[] { juan.Id, juana.Id }, ignoreOrder: true);
	}

	/// <summary>
	/// Busca en el nombre y no sólo en el apellido.
	/// </summary>
	/// <remarks>
	/// Es lo que el contrato anterior no podía hacer: concatenaba "Apellido, Nombre", así que un
	/// término que coincidía sólo con el nombre quedaba detrás del apellido y no encontraba nada.
	/// </remarks>
	[RequiereSqlServerFact]
	public async Task BuscarSegunNombreCompletoAsync_tambien_coincide_en_el_nombre()
	{
		var lucia = await SembrarAlumnoAsync("30111222", "Gomez", "Lucia", "AL9120");

		using var uow = CrearUnidadDeTrabajo();

		var encontrados = await uow.Alumnos.BuscarSegunNombreCompletoAsync("Luc");

		encontrados.ShouldHaveSingleItem().Id.ShouldBe(lucia.Id);
	}

	/// <summary>
	/// Ordena por apellido y después por nombre, en la base y no en memoria.
	/// </summary>
	/// <remarks>
	/// <para>
	/// El orden importa porque es el del índice de la pantalla, y ordenarlo del lado del cliente
	/// obligaría a traer todas las filas antes de poder paginar nada.
	/// </para>
	/// <para>
	/// Los tres apellidos son ASCII a propósito: lo que se afirma es que el <c>ORDER BY</c> está en
	/// la consulta, no cómo ordena la colación de la base las tildes y la eñe. Una prueba que
	/// mezclara las dos cosas fallaría por la colación del servidor y se leería como un defecto de
	/// la consulta.
	/// </para>
	/// </remarks>
	[RequiereSqlServerFact]
	public async Task BuscarSegunNombreCompletoAsync_ordena_por_apellido_y_nombre()
	{
		await SembrarAlumnoAsync("27555445", "Ramirez", "Juana", "AL9130");
		await SembrarAlumnoAsync("27555444", "Ramirez", "Ana", "AL9131");
		await SembrarAlumnoAsync("29333445", "Ramos", "Zoe", "AL9132");

		using var uow = CrearUnidadDeTrabajo();

		var encontrados = await uow.Alumnos.BuscarSegunNombreCompletoAsync("Ram");

		encontrados.Select(x => $"{ x.DatosPersonales.Apellido } { x.DatosPersonales.Nombre }")
				   .ShouldBe(new[] { "Ramirez Ana", "Ramirez Juana", "Ramos Zoe" });
	}

	[RequiereSqlServerFact]
	public async Task BuscarSegunNombreCompletoAsync_sin_coincidencias_devuelve_vacio()
	{
		await SembrarAlumnoAsync("27555444", "Ramirez", "Juan", "AL9140");

		using var uow = CrearUnidadDeTrabajo();

		var encontrados = await uow.Alumnos.BuscarSegunNombreCompletoAsync("Nadie");

		encontrados.ShouldBeEmpty();
	}
}
