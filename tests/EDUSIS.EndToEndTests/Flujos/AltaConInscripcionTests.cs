using Core.ServicioAlumnos;
using Core.ServicioAlumnos.DTOs.Requests;
using Core.ServicioDivisiones.Exceptions;
using EDUSIS.EndToEndTests.Infraestructura;
using EDUSIS.TestSupport.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace EDUSIS.EndToEndTests.Flujos;

/// <summary>
/// <see cref="IServicioAlumno.RegistrarEInscribirAlumnoAsync"/> contra SQL Server real: el alta
/// del alumno y su inscripción son dos raíces y dos guardados dentro de una transacción. El fake
/// de <c>Core.UnitTests</c> sólo registra la secuencia; acá se ve qué queda en la base.
/// </summary>
public sealed class AltaConInscripcionTests : BaseE2E
{
	public AltaConInscripcionTests(SqlServerFixture fixture)
		: base(fixture)
	{
	}

	private static RegistrarAlumnoRequest Alta(string dni) =>
		new(Apellido: "Gómez", Nombre: "Lucía", DNI: dni, Sexo: "Femenino",
			FechaNacimiento: DateTime.Today.AddYears(-13), Nacionalidad: "Argentina",
			Telefono: "3814123456", Email: $"lucia.{ dni }@correo.com",
			Calle: "San Martín", Altura: "1200", Vivienda: "Casa", Observacion: "",
			Localidad: "San Miguel de Tucumán", Provincia: "Tucumán", Pais: "Argentina");

	[RequiereSqlServerFact]
	public async Task Registrar_e_inscribir_persiste_el_alumno_y_su_inscripcion()
	{
		var curso = await SembrarCursoAsync();
		var division = await SembrarDivisionAsync(curso.Id);

		var alumnoID = await EnUnScope(sp => sp.GetRequiredService<IServicioAlumno>().RegistrarEInscribirAlumnoAsync(
			new RegistrarEInscribirAlumnoRequest(Alta("41000111"), curso.Id, division.Id, DateTime.Today.Year.ToString(), DateTime.Today)));

		await using var contexto = Fixture.CrearContexto();
		(await contexto.Set<Domain.Alumnos.Alumno>().AnyAsync(x => x.Id == alumnoID)).ShouldBeTrue();
		(await contexto.Set<Domain.Cursantes.Cursante>().AnyAsync(x => x.AlumnoID == alumnoID && x.DivisionID == division.Id)).ShouldBeTrue();
	}

	/// <remarks>
	/// El caso que motivó el caso de uso: antes la pantalla daba de alta al alumno en el paso 1 y,
	/// si la inscripción del paso 2 fallaba, el alumno quedaba registrado sin curso.
	/// </remarks>
	[RequiereSqlServerFact]
	public async Task Si_la_inscripcion_falla_el_alumno_no_queda_en_la_base()
	{
		var curso = await SembrarCursoAsync();

		await Should.ThrowAsync<DivisionNoEncontradaException>(() => EnUnScope(sp =>
			sp.GetRequiredService<IServicioAlumno>().RegistrarEInscribirAlumnoAsync(
				new RegistrarEInscribirAlumnoRequest(Alta("41000222"), curso.Id, Guid.NewGuid(), DateTime.Today.Year.ToString(), DateTime.Today))));

		await using var contexto = Fixture.CrearContexto();
		(await contexto.Set<Domain.Alumnos.Alumno>().AnyAsync(x => x.DatosPersonales.Documento == "41000222")).ShouldBeFalse();
	}
}
