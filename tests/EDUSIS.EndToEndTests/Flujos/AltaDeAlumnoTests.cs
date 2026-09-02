using Core.ServicioAlumnos;
using Core.ServicioAlumnos.DTOs.Requests;
using Core.Shared.DTOs.Personas.Requests;
using EDUSIS.EndToEndTests.Infraestructura;
using EDUSIS.TestSupport.Infraestructura;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace EDUSIS.EndToEndTests.Flujos;

/// <summary>
/// Flujo e2e 1 (SC-003 / data-model.md §6): alta de alumno desde la fachada
/// <see cref="IServicioAlumno.RegistrarAlumnoAsync"/> hasta SQL Server real, con la composición
/// de dependencias de producción. Se verifica en un <em>scope</em> nuevo —como haría otra
/// pantalla— que el alumno quedó persistido con todos sus datos, incluidos acentos y <c>ñ</c>.
/// </summary>
public sealed class AltaDeAlumnoTests : BaseE2E
{
	public AltaDeAlumnoTests(SqlServerFixture fixture)
		: base(fixture)
	{
	}

	private static RegistrarAlumnoRequest AltaConAcentos() =>
		new(Apellido: "Ñáñez Piñón",
			Nombre: "José María",
			DNI: "40222333",
			Sexo: "Femenino",
			FechaNacimiento: DateTime.Today.AddYears(-15),
			Nacionalidad: "Argentina",
			Telefono: "3814123456",
			Email: "jose.maria@correo.com",
			Calle: "Pasaje Ñuñorco",
			Altura: "1234",
			Vivienda: "Casa",
			Observacion: "Casa con portón ñandú",
			Localidad: "Yerba Buena",
			Provincia: "Tucumán",
			Pais: "Argentina");

	[RequiereSqlServerFact]
	public async Task RegistrarAlumnoAsync_persiste_al_alumno_y_BuscarPorIDAsync_lo_devuelve_completo()
	{
		var request = AltaConAcentos();

		var alumnoID = await EnUnScope(sp =>
			sp.GetRequiredService<IServicioAlumno>().RegistrarAlumnoAsync(request));

		alumnoID.ShouldNotBe(Guid.Empty);

		// Scope nuevo: se relee con otra unidad de trabajo / otro DbContext, sin caché de por medio.
		var perfil = await EnUnScope(sp =>
			sp.GetRequiredService<IServicioAlumno>().BuscarPorIDAsync(new PersonaRequest(alumnoID)));

		perfil.PersonaID.ShouldBe(alumnoID);
		perfil.Apellido.ShouldBe("Ñáñez Piñón");
		perfil.Nombre.ShouldBe("José María");
		perfil.Documento.ShouldBe("40222333");
		perfil.Sexo.ShouldBe("Femenino");
		perfil.Nacionalidad.ShouldBe("Argentina");
		perfil.Email.ShouldBe("jose.maria@correo.com");
		perfil.Telefono.ShouldBe("3814123456");
		perfil.Calle.ShouldBe("Pasaje Ñuñorco");
		perfil.Altura.ShouldBe("1234");
		perfil.Observacion.ShouldBe("Casa con portón ñandú");
		perfil.Localidad.ShouldBe("Yerba Buena");
		perfil.Provincia.ShouldBe("Tucumán");
		perfil.Pais.ShouldBe("Argentina");
		perfil.FechaNacimiento.ShouldNotBeNullOrWhiteSpace();
	}
}
