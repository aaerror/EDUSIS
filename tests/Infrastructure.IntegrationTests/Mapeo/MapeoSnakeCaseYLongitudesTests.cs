using Domain.Alumnos;
using Domain.Docentes;
using Domain.Personas;
using Domain.Personas.Domicilios;
using EDUSIS.TestSupport.Infraestructura;
using Infrastructure.IntegrationTests.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Infrastructure.IntegrationTests.Mapeo;

/// <summary>
/// US3-1 / Edge Cases: el mapeo es explícito, en español y con <c>snake_case</c>
/// (<c>ToTable("docente")</c>, <c>HasColumnName("fecha_nacimiento")</c>), usa TPT para la
/// jerarquía <see cref="Persona"/>/<see cref="Docente"/>/<see cref="Alumno"/> y aplica la
/// convención global <c>string → MaxLength(50)</c>. Se verifica sobre el modelo de EF Core,
/// no hace falta escribir datos.
/// </summary>
public sealed class MapeoSnakeCaseYLongitudesTests : BaseIntegracion
{
	public MapeoSnakeCaseYLongitudesTests(SqlServerFixture fixture)
		: base(fixture)
	{
	}

	[RequiereSqlServerFact]
	public void Las_tablas_de_la_jerarquia_Persona_usan_TPT_con_nombres_en_snake_case()
	{
		using var contexto = Fixture.CrearContexto();
		var modelo = contexto.Model;

		modelo.FindEntityType(typeof(Persona))!.GetTableName().ShouldBe("persona");
		modelo.FindEntityType(typeof(Alumno))!.GetTableName().ShouldBe("alumno");
		modelo.FindEntityType(typeof(Docente))!.GetTableName().ShouldBe("docente");

		// TPT: cada tipo derivado tiene su propia tabla (no comparte la de la base).
		modelo.FindEntityType(typeof(Alumno))!.GetTableName()
			.ShouldNotBe(modelo.FindEntityType(typeof(Persona))!.GetTableName());
	}

	[RequiereSqlServerFact]
	public void Las_columnas_de_los_value_objects_estan_en_snake_case()
	{
		using var contexto = Fixture.CrearContexto();
		var persona = contexto.Model.FindEntityType(typeof(Persona))!;

		var datosPersonales = persona.FindNavigation(nameof(Persona.DatosPersonales))!.TargetEntityType;
		datosPersonales.FindProperty(nameof(DatosPersonales.FechaNacimiento))!
			.GetColumnName().ShouldBe("fecha_nacimiento");

		var domicilio = persona.FindNavigation(nameof(Persona.Domicilio))!.TargetEntityType;
		var direccion = domicilio.FindNavigation(nameof(Domicilio.Direccion))!.TargetEntityType;
		direccion.FindProperty("Calle")!.GetColumnName().ShouldBe("calle");
	}

	[RequiereSqlServerFact]
	public void La_convencion_global_limita_los_string_sin_tipo_explicito_a_50_caracteres()
	{
		using var contexto = Fixture.CrearContexto();
		var alumno = contexto.Model.FindEntityType(typeof(Alumno))!;

		alumno.FindProperty(nameof(Alumno.Legajo))!.GetMaxLength().ShouldBe(50);
	}
}
