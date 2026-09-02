using Core.ServicioAlumnos;
using Core.ServicioAlumnos.DTOs.Requests;
using Core.Shared.DTOs.Personas.Requests;
using Core.UnitTests.Infraestructura;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Fakes;
using Shouldly;
using Xunit;

namespace Core.UnitTests.ServicioAlumnos;

/// <summary>
/// <see cref="IServicioAlumno"/> sobre <see cref="UnitOfWorkFake"/>: los 11 métodos públicos con
/// su camino feliz y su camino de error (agregado inexistente, <c>Request</c> inválido o
/// excepción de dominio que sube sin transformarse).
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class ServicioAlumnoTests
{
	private readonly HostDeServicios _host = new();
	private readonly IServicioAlumno _servicio;

	public ServicioAlumnoTests()
	{
		_servicio = _host.Resolver<IServicioAlumno>();
	}

	private Domain.Alumnos.Alumno SembrarAlumno()
	{
		var alumno = new AlumnoBuilder().Build();
		_host.UnidadDeTrabajo.AlumnosFake.Sembrar(alumno);
		return alumno;
	}

	private static RegistrarAlumnoRequest RegistroValido() =>
		new(Apellido: "Gómez", Nombre: "Lucía", DNI: "40222333", Sexo: "Femenino",
			FechaNacimiento: DateTime.Today.AddYears(-15), Nacionalidad: "Argentina",
			Telefono: "3814123456", Email: "lucia.gomez@correo.com",
			Calle: "San Martín", Altura: "1200", Vivienda: "Casa", Observacion: "",
			Localidad: "San Miguel de Tucumán", Provincia: "Tucumán", Pais: "Argentina");

	#region Consultas
	[Fact]
	public async Task BuscarPorIDAsync_devuelve_el_perfil_completo_del_alumno_sembrado()
	{
		var alumno = SembrarAlumno();

		var response = await _servicio.BuscarPorIDAsync(new PersonaRequest(alumno.Id));

		response.PersonaID.ShouldBe(alumno.Id);
		response.Documento.ShouldBe(alumno.DatosPersonales.Documento);
	}

	[Fact]
	public async Task BuscarPorIDAsync_con_un_Id_inexistente_lanza_NullReferenceException()
	{
		await Should.ThrowAsync<NullReferenceException>(
			() => _servicio.BuscarPorIDAsync(new PersonaRequest(Guid.NewGuid())));
	}

	[Fact]
	public async Task BuscarPorDNIAsync_devuelve_el_alumno_cuyo_documento_coincide()
	{
		var alumno = SembrarAlumno();

		var response = await _servicio.BuscarPorDNIAsync(new DocumentoRequest(alumno.DatosPersonales.Documento));

		response.ShouldNotBeNull();
		response!.PersonaID.ShouldBe(alumno.Id);
	}

	[Fact]
	public async Task BuscarPorDNIAsync_con_un_documento_sin_coincidencias_devuelve_null()
	{
		var response = await _servicio.BuscarPorDNIAsync(new DocumentoRequest("99999999"));

		response.ShouldBeNull();
	}

	[Fact]
	public async Task BuscarPorNombreCompletoAsync_encuentra_al_alumno_por_apellido_y_nombre()
	{
		var alumno = SembrarAlumno();
		var nombreCompleto = $"{alumno.DatosPersonales.Apellido} {alumno.DatosPersonales.Nombre}";

		var response = await _servicio.BuscarPorNombreCompletoAsync(new NombreCompletoRequest(nombreCompleto));

		response.ShouldNotBeNull();
		response!.PersonaID.ShouldBe(alumno.Id);
	}

	[Fact]
	public async Task BuscarPorNombreCompletoAsync_sin_coincidencias_devuelve_null()
	{
		var response = await _servicio.BuscarPorNombreCompletoAsync(new NombreCompletoRequest("Nadie Existente"));

		response.ShouldBeNull();
	}

	[Fact]
	public async Task EsDocumentoInvalidoAsync_es_false_para_un_documento_libre()
	{
		var esInvalido = await _servicio.EsDocumentoInvalidoAsync(new DocumentoRequest("41555666"));

		esInvalido.ShouldBeFalse();
	}

	[Fact]
	public async Task EsDocumentoInvalidoAsync_es_true_cuando_el_documento_ya_pertenece_a_un_alumno()
	{
		var alumno = SembrarAlumno();

		var esInvalido = await _servicio.EsDocumentoInvalidoAsync(new DocumentoRequest(alumno.DatosPersonales.Documento));

		esInvalido.ShouldBeTrue();
	}
	#endregion

	#region Alta
	[Fact]
	public async Task RegistrarAlumnoAsync_da_de_alta_al_alumno_y_persiste_los_cambios()
	{
		var id = await _servicio.RegistrarAlumnoAsync(RegistroValido());

		id.ShouldNotBe(Guid.Empty);
		_host.UnidadDeTrabajo.AlumnosFake.Elementos.ShouldHaveSingleItem();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task RegistrarAlumnoAsync_con_un_DNI_mal_formado_propaga_la_FormatException_del_dominio()
	{
		var request = RegistroValido() with { DNI = "no-es-dni" };

		await Should.ThrowAsync<FormatException>(() => _servicio.RegistrarAlumnoAsync(request));
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(0);
	}
	#endregion

	#region Modificaciones
	[Fact]
	public async Task ActualizarContacto_cambia_email_y_telefono_y_guarda()
	{
		var alumno = SembrarAlumno();

		await _servicio.ActualizarContacto(new CambiarContactoRequest(alumno.Id, "3815550000", "nuevo.mail@correo.com"));

		alumno.Email.ShouldBe("nuevo.mail@correo.com");
		alumno.Telefono.ShouldBe("3815550000");
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task ActualizarContacto_con_un_email_invalido_propaga_la_excepcion_de_dominio_sin_transformarla()
	{
		var alumno = SembrarAlumno();

		// US2-2: el servicio hace catch → LogDebug → throw; la excepción de dominio sube tal cual.
		var ex = await Should.ThrowAsync<Exception>(
			() => _servicio.ActualizarContacto(new CambiarContactoRequest(alumno.Id, "3815550000", "esto-no-es-un-email")));

		ex.GetType().Name.ShouldBe("FormatoEmailInvalidoException");
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(0);
	}

	[Fact]
	public async Task ActualizarDomicilio_reemplaza_el_domicilio_y_guarda()
	{
		var alumno = SembrarAlumno();

		await _servicio.ActualizarDomicilio(new CambiarDomicilioRequest(
			alumno.Id, "Belgrano", "850", "Departamento", "3° B", "Yerba Buena", "Tucumán", "Argentina"));

		alumno.Domicilio.Direccion.Calle.ShouldBe("Belgrano");
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task ActualizarDomicilio_con_un_Id_inexistente_lanza_NullReferenceException()
	{
		await Should.ThrowAsync<NullReferenceException>(() => _servicio.ActualizarDomicilio(
			new CambiarDomicilioRequest(Guid.NewGuid(), "Belgrano", "850", "Casa", "", "Yerba Buena", "Tucumán", "Argentina")));
	}

	[Fact]
	public async Task ActualizarSexo_actualiza_los_datos_personales_y_guarda()
	{
		var alumno = SembrarAlumno();

		await _servicio.ActualizarSexo(new CambiarSexoRequest(alumno.Id, "Ñáñez", "José María", "Masculino"));

		alumno.DatosPersonales.Sexo.ShouldBe(Domain.Personas.Sexo.Masculino);
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task ActualizarSexo_con_un_Id_inexistente_lanza_NullReferenceException()
	{
		await Should.ThrowAsync<NullReferenceException>(() => _servicio.ActualizarSexo(
			new CambiarSexoRequest(Guid.NewGuid(), "Ñáñez", "José María", "Masculino")));
	}

	[Fact]
	public async Task ModificarNombreCompleto_cambia_apellido_y_nombre_y_guarda()
	{
		var alumno = SembrarAlumno();

		await _servicio.ModificarNombreCompleto(alumno.Id, "Paz", "Renata");

		alumno.DatosPersonales.Apellido.ShouldBe("Paz");
		alumno.DatosPersonales.Nombre.ShouldBe("Renata");
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task ModificarNombreCompleto_con_un_Id_inexistente_lanza_NullReferenceException()
	{
		await Should.ThrowAsync<NullReferenceException>(
			() => _servicio.ModificarNombreCompleto(Guid.NewGuid(), "Paz", "Renata"));
	}

	[Fact]
	public async Task ActualizarDireccion_reemplaza_la_direccion_conservando_la_ubicacion()
	{
		var alumno = SembrarAlumno();
		var ubicacionPrevia = alumno.Domicilio.Ubicacion.Localidad;

		await _servicio.ActualizarDireccion(alumno.Id, new DireccionRequest("Laprida", "42", "Casa", ""));

		alumno.Domicilio.Direccion.Calle.ShouldBe("Laprida");
		alumno.Domicilio.Ubicacion.Localidad.ShouldBe(ubicacionPrevia);
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task ActualizarDireccion_con_un_Id_inexistente_lanza_NullReferenceException()
	{
		await Should.ThrowAsync<NullReferenceException>(
			() => _servicio.ActualizarDireccion(Guid.NewGuid(), new DireccionRequest("Laprida", "42", "Casa", "")));
	}
	#endregion

	#region Baja
	[Fact]
	public async Task EliminarAlumnoAsync_quita_al_alumno_del_repositorio_y_guarda()
	{
		var alumno = SembrarAlumno();

		await _servicio.EliminarAlumnoAsync(new EliminarAlumnoRequest(alumno.Id));

		_host.UnidadDeTrabajo.AlumnosFake.Elementos.ShouldBeEmpty();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task EliminarAlumnoAsync_con_un_Id_inexistente_no_falla_y_no_borra_nada()
	{
		SembrarAlumno();

		// Eliminar(params object[]) es un no-op cuando el Id no existe; documenta la semántica actual.
		await Should.NotThrowAsync(() => _servicio.EliminarAlumnoAsync(new EliminarAlumnoRequest(Guid.NewGuid())));
		_host.UnidadDeTrabajo.AlumnosFake.Elementos.ShouldHaveSingleItem();
	}
	#endregion
}
