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

	/// <summary>
	/// Los largos del modelo son los que el mapeo declara, y donde hay <c>HasMaxLength</c> explícito
	/// gana el explícito por encima de la convención global.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Esta prueba afirmaba <c>Alumno.Legajo → 50</c> como ejemplo de "string sin tipo explícito", y
	/// era <strong>falso en los dos sentidos</strong>: <c>AlumnosConfiguration:30</c> le pone
	/// <c>HasMaxLength(6)</c> —exactamente los seis caracteres de su formato <c>^[A-Z]{2}\d{4}$</c>—
	/// así que el legajo ni mide 50 ni es un caso de convención. Nunca se detectó porque la suite se
	/// omitía sin SQL Server.
	/// </para>
	/// <para>
	/// Al corregirla apareció algo que vale la pena saber: <strong>hoy ninguna propiedad
	/// <c>string</c> del modelo depende de la convención global</strong>. Las ocho columnas que miden
	/// 50 —<c>calle</c>, <c>localidad</c>, <c>provincia</c>, <c>pais</c>, <c>descripcion</c>,
	/// <c>apellido</c>, <c>nombre</c>, <c>email</c>— lo declaran con <c>HasMaxLength(50)</c> una por
	/// una. El <c>HaveMaxLength(50)</c> de <c>EdusisDBContext:78</c> es una red de seguridad que nada
	/// ejercita, así que no hay propiedad sobre la que observarla sin inventar una. Queda registrado
	/// en INFRA-20; lo que esta prueba fija es lo que sí es verificable.
	/// </para>
	/// </remarks>
	[RequiereSqlServerFact]
	public void Los_largos_explicitos_del_mapeo_ganan_sobre_la_convencion_global()
	{
		using var contexto = Fixture.CrearContexto();
		var modelo = contexto.Model;

		// El legajo mide lo que mide su formato, no lo que dice la convención.
		modelo.FindEntityType(typeof(Alumno))!
			  .FindProperty(nameof(Alumno.Legajo))!
			  .GetMaxLength().ShouldBe(6);

		// Y una que coincide con la convención, pero porque su mapeo lo declara.
		modelo.FindEntityType(typeof(Persona))!
			  .FindProperty(nameof(Persona.Email))!
			  .GetMaxLength().ShouldBe(50);
	}
}
