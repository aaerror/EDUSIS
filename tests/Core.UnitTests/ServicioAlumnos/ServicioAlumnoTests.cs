using Core.ServicioAlumnos;
using Core.ServicioAlumnos.DTOs.Requests;
using Core.Shared.DTOs.Personas.Requests;
using Core.UnitTests.Infraestructura;
using Domain.Cursantes.Calificaciones;
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
		var nombreCompleto = alumno.DatosPersonales.NombreCompleto();

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
	public async Task RegistrarAlumnoAsync_asigna_legajos_correlativos_en_dos_altas_seguidas()
	{
		await _servicio.RegistrarAlumnoAsync(RegistroValido());
		await _servicio.RegistrarAlumnoAsync(RegistroValido() with { DNI = "40222334" });

		var legajos = _host.UnidadDeTrabajo.AlumnosFake.Elementos.Select(x => x.Legajo).ToList();

		legajos.ShouldBe(new[] { "AL0001", "AL0002" });
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
	/// <remarks>
	/// Tres guardados y no uno: el borrado va por pasos obligados por el grafo de claves ajenas
	/// —registros de asistencia, después cursantes, después el legajo—, cada uno con su
	/// <c>GuardarCambiosAsync</c>, todo dentro de una transacción.
	/// </remarks>
	[Fact]
	public async Task EliminarAlumnoAsync_quita_al_alumno_del_repositorio_y_guarda()
	{
		var alumno = SembrarAlumno();

		await _servicio.EliminarAlumnoAsync(new EliminarAlumnoRequest(alumno.Id));

		_host.UnidadDeTrabajo.AlumnosFake.Elementos.ShouldBeEmpty();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(3);
	}

	/// <remarks>
	/// <strong>Cambio de comportamiento deliberado (2026-11-05).</strong> Antes era un no-op
	/// silencioso, porque el servicio delegaba directo en <c>EliminarAsync(id)</c>, que no hace nada
	/// si el Id no existe. Ahora el caso de uso necesita cargar el alumno para recorrer su
	/// trayectoria, así que un Id inexistente se reporta en lugar de pasar desapercibido: pedir el
	/// borrado de alguien que no está es un error del llamador, no un éxito.
	///
	/// Sigue lanzando <c>NullReferenceException</c> por <c>BuscarAlumnoPorIDAsync</c>, que es el
	/// helper compartido de los otros siete métodos del servicio. Es CORE-20 y se arregla para los 31
	/// sitios a la vez, no acá: ser consistente con el resto del archivo vale más que limpiar uno.
	/// </remarks>
	[Fact]
	public async Task EliminarAlumnoAsync_con_un_Id_inexistente_falla_y_no_borra_nada()
	{
		SembrarAlumno();

		await Should.ThrowAsync<NullReferenceException>(
			() => _servicio.EliminarAlumnoAsync(new EliminarAlumnoRequest(Guid.NewGuid())));

		_host.UnidadDeTrabajo.AlumnosFake.Elementos.ShouldHaveSingleItem();
	}

	[Fact]
	public async Task EliminarAlumnoAsync_arrastra_inscripciones_calificaciones_y_registros()
	{
		var alumno = SembrarAlumno();
		var cursante = new CursanteBuilder().ConAlumno(alumno.Id).Build();
		cursante.RegistrarCalificacion(Guid.NewGuid(), DateTime.Today, Instancia.Parcial, 8, null);
		_host.UnidadDeTrabajo.CursantesFake.Sembrar(cursante);

		// Dos cursantes en la planilla: al irse el del alumno, la planilla sobrevive con el otro.
		var planilla = new PlanillaAsistenciaBuilder().ConCursantes(cursante.Id, Guid.NewGuid()).Build();
		_host.UnidadDeTrabajo.PlanillasAsistenciaFake.Sembrar(planilla);

		await _servicio.EliminarAlumnoAsync(new EliminarAlumnoRequest(alumno.Id));

		_host.UnidadDeTrabajo.AlumnosFake.Elementos.ShouldBeEmpty();
		_host.UnidadDeTrabajo.CursantesFake.Elementos.ShouldBeEmpty();
		_host.UnidadDeTrabajo.PlanillasAsistenciaFake.Elementos.ShouldHaveSingleItem();
		planilla.RegistroDe(cursante.Id).ShouldBeNull();
	}

	/// <remarks>
	/// Una planilla cuyo único cursante era el alumno borrado no documenta ninguna jornada: se va con
	/// él. La que conserva registros de otros alumnos se queda.
	/// </remarks>
	[Fact]
	public async Task EliminarAlumnoAsync_elimina_la_planilla_que_queda_sin_registros()
	{
		var alumno = SembrarAlumno();
		var cursante = new CursanteBuilder().ConAlumno(alumno.Id).Build();
		_host.UnidadDeTrabajo.CursantesFake.Sembrar(cursante);

		var soloDelAlumno = new PlanillaAsistenciaBuilder().ConCursantes(cursante.Id)
														  .ConFecha(DateTime.Today.AddDays(-1))
														  .Build();
		var compartida = new PlanillaAsistenciaBuilder().ConCursantes(cursante.Id, Guid.NewGuid())
													   .ConFecha(DateTime.Today)
													   .Build();
		_host.UnidadDeTrabajo.PlanillasAsistenciaFake.Sembrar(soloDelAlumno);
		_host.UnidadDeTrabajo.PlanillasAsistenciaFake.Sembrar(compartida);

		await _servicio.EliminarAlumnoAsync(new EliminarAlumnoRequest(alumno.Id));

		_host.UnidadDeTrabajo.PlanillasAsistenciaFake.Elementos
			 .ShouldHaveSingleItem()
			 .Id.ShouldBe(compartida.Id);
	}

	/// <remarks>
	/// La planilla cerrada es registro histórico y no se puede eliminar por la vía normal
	/// (<c>PlanillaCerradaNoEliminableException</c>), pero el registro del alumno borrado sí sale.
	/// Es lo que evita que un día cerrado vuelva indestructible al alumno.
	/// </remarks>
	[Fact]
	public async Task EliminarAlumnoAsync_tambien_saca_el_registro_de_una_planilla_cerrada()
	{
		var alumno = SembrarAlumno();
		var cursante = new CursanteBuilder().ConAlumno(alumno.Id).Build();
		_host.UnidadDeTrabajo.CursantesFake.Sembrar(cursante);

		var cerrada = new PlanillaAsistenciaBuilder().ConCursantes(cursante.Id, Guid.NewGuid())
													 .Cerrada()
													 .Build();
		_host.UnidadDeTrabajo.PlanillasAsistenciaFake.Sembrar(cerrada);

		await Should.NotThrowAsync(() => _servicio.EliminarAlumnoAsync(new EliminarAlumnoRequest(alumno.Id)));

		cerrada.RegistroDe(cursante.Id).ShouldBeNull();
		cerrada.Cerrada.ShouldBeTrue();
	}
	#endregion

	#region Impacto del borrado
	[Fact]
	public async Task ConsultarImpactoEliminacionAsync_cuenta_la_trayectoria_sin_borrar_nada()
	{
		var alumno = SembrarAlumno();
		var cursante = new CursanteBuilder().ConAlumno(alumno.Id).Build();
		cursante.RegistrarCalificacion(Guid.NewGuid(), DateTime.Today, Instancia.Parcial, 8, null);
		cursante.RegistrarCalificacion(Guid.NewGuid(), DateTime.Today, Instancia.Recuperatorio, 6, null);
		_host.UnidadDeTrabajo.CursantesFake.Sembrar(cursante);

		var soloDelAlumno = new PlanillaAsistenciaBuilder().ConCursantes(cursante.Id)
														  .ConFecha(DateTime.Today.AddDays(-1))
														  .Build();
		var compartida = new PlanillaAsistenciaBuilder().ConCursantes(cursante.Id, Guid.NewGuid())
													   .ConFecha(DateTime.Today)
													   .Build();
		_host.UnidadDeTrabajo.PlanillasAsistenciaFake.Sembrar(soloDelAlumno);
		_host.UnidadDeTrabajo.PlanillasAsistenciaFake.Sembrar(compartida);

		var impacto = await _servicio.ConsultarImpactoEliminacionAsync(new EliminarAlumnoRequest(alumno.Id));

		impacto.Inscripciones.ShouldBe(1);
		impacto.Calificaciones.ShouldBe(2);
		impacto.RegistrosAsistencia.ShouldBe(2);
		impacto.PlanillasQueQuedanVacias.ShouldBe(1);
		impacto.CiclosLectivos.ShouldHaveSingleItem().ShouldBe(cursante.CicloLectivo.Periodo);
		impacto.TieneHistorial.ShouldBeTrue();

		// Lo que importa de una consulta de impacto: no toca nada.
		_host.UnidadDeTrabajo.AlumnosFake.Elementos.ShouldHaveSingleItem();
		_host.UnidadDeTrabajo.CursantesFake.Elementos.ShouldHaveSingleItem();
		_host.UnidadDeTrabajo.PlanillasAsistenciaFake.Elementos.Count.ShouldBe(2);
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(0);
	}

	[Fact]
	public async Task ConsultarImpactoEliminacionAsync_sin_trayectoria_informa_que_no_hay_historial()
	{
		var alumno = SembrarAlumno();

		var impacto = await _servicio.ConsultarImpactoEliminacionAsync(new EliminarAlumnoRequest(alumno.Id));

		impacto.TieneHistorial.ShouldBeFalse();
		impacto.Inscripciones.ShouldBe(0);
		impacto.Calificaciones.ShouldBe(0);
		impacto.RegistrosAsistencia.ShouldBe(0);
		impacto.CiclosLectivos.ShouldBeEmpty();
	}
	#endregion
}
