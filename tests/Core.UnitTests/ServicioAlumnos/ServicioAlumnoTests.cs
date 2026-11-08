using Core.ServicioAlumnos;
using Core.ServicioAlumnos.DTOs.Requests;
using Core.Shared.DTOs.Personas.Requests;
using Core.UnitTests.Infraestructura;
using Domain.Cursantes.Calificaciones;
using Domain.Cursantes;
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

	/// <summary>Un alumno con apellido, nombre y documento elegidos, para las pruebas de búsqueda.</summary>
	private Domain.Alumnos.Alumno SembrarAlumnoLlamado(string apellido, string nombre, string documento)
	{
		var alumno = new AlumnoBuilder()
			.ConDatosPersonales(new DatosPersonalesBuilder().ConEdad(16)
														   .ConApellidoYNombre(apellido, nombre)
														   .ConDocumento(documento))
			.ConLegajo($"AL{ documento[^4..] }")
			.Build();

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

	#region BuscarAlumnosAsync
	/// <remarks>
	/// Reemplaza a las dos pruebas de <c>BuscarPorNombreCompletoAsync</c>, que ejercitaban un
	/// contrato de nombre completo EXACTO —con su coma y sus tildes— que devolvía un solo alumno.
	/// Lo que estas prueban es la decisión que ese método no tomaba: qué significa el término.
	/// </remarks>
	[Fact]
	public async Task BuscarAlumnosAsync_con_un_termino_de_digitos_busca_el_documento_exacto()
	{
		var alumno = SembrarAlumnoLlamado("Pérez", "Juan", "27555444");
		SembrarAlumnoLlamado("Pérez", "Juana", "27555445");

		var encontrados = await _servicio.BuscarAlumnosAsync(new BuscarAlumnoRequest("27555444"));

		encontrados.ShouldHaveSingleItem().PersonaID.ShouldBe(alumno.Id);
	}

	/// <remarks>
	/// El documento va exacto justamente para que esto NO devuelva nada: con un <c>LIKE</c> parcial,
	/// tipear dos dígitos listaría media institución y la lista dejaría de servir para elegir.
	/// </remarks>
	[Fact]
	public async Task BuscarAlumnosAsync_con_digitos_no_hace_coincidencia_parcial_del_documento()
	{
		SembrarAlumnoLlamado("Pérez", "Juan", "27555444");

		var encontrados = await _servicio.BuscarAlumnosAsync(new BuscarAlumnoRequest("275"));

		encontrados.ShouldBeEmpty();
	}

	[Fact]
	public async Task BuscarAlumnosAsync_con_un_termino_de_letras_coincide_parcialmente_en_el_apellido()
	{
		SembrarAlumnoLlamado("Pérez", "Juan", "27555444");
		SembrarAlumnoLlamado("Pérez", "Juana", "27555445");
		SembrarAlumnoLlamado("Gómez", "Lucía", "30111222");

		var encontrados = await _servicio.BuscarAlumnosAsync(new BuscarAlumnoRequest("ére"));

		encontrados.Count.ShouldBe(2);
		encontrados.Select(x => x.Nombre).ShouldBe(new[] { "Juan", "Juana" });
	}

	/// <remarks>
	/// Busca en las DOS columnas. El contrato anterior concatenaba "Apellido, Nombre", así que un
	/// término que sólo coincidía con el nombre no encontraba nada: el apellido iba adelante.
	/// </remarks>
	[Fact]
	public async Task BuscarAlumnosAsync_tambien_coincide_en_el_nombre_y_no_solo_en_el_apellido()
	{
		var alumno = SembrarAlumnoLlamado("Gómez", "Lucía", "30111222");

		var encontrados = await _servicio.BuscarAlumnosAsync(new BuscarAlumnoRequest("Luc"));

		encontrados.ShouldHaveSingleItem().PersonaID.ShouldBe(alumno.Id);
	}

	[Fact]
	public async Task BuscarAlumnosAsync_sin_coincidencias_devuelve_vacio()
	{
		SembrarAlumnoLlamado("Pérez", "Juan", "27555444");

		var encontrados = await _servicio.BuscarAlumnosAsync(new BuscarAlumnoRequest("Nadie"));

		encontrados.ShouldBeEmpty();
	}

	/// <remarks>
	/// Vacío y no la matrícula entera: la búsqueda va contra la base, y "ver a todos" sería una
	/// consulta sin filtro que nadie pidió.
	/// </remarks>
	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public async Task BuscarAlumnosAsync_con_un_termino_vacio_no_consulta_nada(string termino)
	{
		SembrarAlumnoLlamado("Pérez", "Juan", "27555444");

		var encontrados = await _servicio.BuscarAlumnosAsync(new BuscarAlumnoRequest(termino));

		encontrados.ShouldBeEmpty();
	}
	#endregion

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
		cursante.RegistrarCalificacion(Guid.NewGuid(), DateTime.Today, Periodo.PrimerCuatrimestre, false, 8, null);
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
		cursante.RegistrarCalificacion(Guid.NewGuid(), DateTime.Today, Periodo.PrimerCuatrimestre, false, 8, null);
		cursante.RegistrarCalificacion(Guid.NewGuid(), DateTime.Today, Periodo.PrimerCuatrimestre, true, 6, null);
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

	#region Resumen académico
	/// <summary>
	/// Siembra un alumno inscripto en una división de un curso, con el ciclo lectivo indicado.
	/// </summary>
	/// <remarks>
	/// Siembra TAMBIÉN la división y el curso porque el resumen los resuelve para nombrar lo que el
	/// alumno cursa. Sin ellos la consulta no falla —devuelve el nombre vacío a propósito, para que
	/// una división borrada de 2021 no impida abrir el perfil— y por eso una prueba que los omita
	/// pasaría sin verificar nada.
	/// </remarks>
	private (Domain.Alumnos.Alumno Alumno, Domain.Cursantes.Cursante Cursante) SembrarCursada(
		string periodo = "2026",
		bool recursante = false,
		DateTime? fechaInicio = null,
		DateTime? fechaBaja = null)
	{
		var alumno = SembrarAlumno();

		var curso = new CursoBuilder().ConGrado("Tercero").ConNivelEducativo("Secundaria").Build();
		_host.UnidadDeTrabajo.CursosFake.Sembrar(curso);

		var division = new DivisionBuilder().ConCurso(curso.Id).ConDescripcion("B").Build();
		_host.UnidadDeTrabajo.DivisionesFake.Sembrar(division);

		var constructor = new CursanteBuilder()
			.ConAlumno(alumno.Id)
			.ConDivision(division.Id)
			.ConCicloLectivo(CicloLectivo.Crear(periodo))
			.ConFechaInicio(fechaInicio ?? DateTime.Today.AddMonths(-6))
			.ComoRecursante(recursante);

		if (fechaBaja is not null)
		{
			constructor.DadoDeBajaEl(fechaBaja.Value);
		}

		var cursante = constructor.Build();
		_host.UnidadDeTrabajo.CursantesFake.Sembrar(cursante);

		return (alumno, cursante);
	}

	private Guid SembrarMateria(string descripcion)
	{
		var materia = new MateriaBuilder().ConDescripcion(descripcion).Build();
		_host.UnidadDeTrabajo.MateriasFake.Sembrar(materia);

		return materia.Id;
	}

	[Fact]
	public async Task ConsultarResumenAcademicoAsync_de_un_alumno_sin_inscripciones_no_informa_ciclo()
	{
		var alumno = SembrarAlumno();

		var resumen = await _servicio.ConsultarResumenAcademicoAsync(new PersonaRequest(alumno.Id));

		resumen.CicloLectivo.ShouldBeNull();
		resumen.EstaCursando.ShouldBeFalse();
		resumen.Promedio.ShouldBeNull();
		resumen.TieneCalificaciones.ShouldBeFalse();
		resumen.Asistencia.TieneRegistro.ShouldBeFalse();
		resumen.Trayectoria.ShouldBeEmpty();
	}

	[Fact]
	public async Task ConsultarResumenAcademicoAsync_con_un_Id_inexistente_lanza()
	{
		await Should.ThrowAsync<NullReferenceException>(
			() => _servicio.ConsultarResumenAcademicoAsync(new PersonaRequest(Guid.NewGuid())));
	}

	[Fact]
	public async Task ConsultarResumenAcademicoAsync_nombra_el_curso_y_la_division_que_cursa()
	{
		var (alumno, cursante) = SembrarCursada(periodo: "2026", recursante: true);

		var resumen = await _servicio.ConsultarResumenAcademicoAsync(new PersonaRequest(alumno.Id));

		resumen.EstaCursando.ShouldBeTrue();
		resumen.CicloLectivo.ShouldBe("2026");
		resumen.Cursada.ShouldNotBeNull();
		resumen.Cursada!.CursanteID.ShouldBe(cursante.Id);
		resumen.Cursada.Division.ShouldBe("B");
		resumen.Cursada.Grado.ShouldBe("Tercero");
		resumen.Cursada.NivelEducativo.ShouldBe("Secundaria");
		resumen.Cursada.EsRecursante.ShouldBeTrue();
	}

	/// <remarks>
	/// El criterio del promedio: la inasistencia a examen NO entra. El dominio la guarda con
	/// <c>Nota</c> en nulo a propósito, así que contarla obligaría a inventarle un número. Se informa
	/// aparte, y acá se verifica que esté contada y que no haya movido el promedio: con 8 y 6 el
	/// promedio es 7, y lo seguiría siendo con diez ausencias encima.
	/// </remarks>
	[Fact]
	public async Task ConsultarResumenAcademicoAsync_promedia_los_cuatrimestres_y_excluye_las_ausencias_a_examen()
	{
		var (alumno, cursante) = SembrarCursada();
		var matematica = SembrarMateria("Matemática");

		cursante.RegistrarCalificacion(matematica, DateTime.Today, Periodo.PrimerCuatrimestre, false, 8, null);
		cursante.RegistrarCalificacion(matematica, DateTime.Today, Periodo.SegundoCuatrimestre, false, 6, null);
		cursante.RegistrarInasistenciaAExamen(matematica, DateTime.Today, Periodo.SegundoCuatrimestre, true, null);

		var resumen = await _servicio.ConsultarResumenAcademicoAsync(new PersonaRequest(alumno.Id));

		resumen.Promedio.ShouldBe(7m);
		resumen.Rendidas.ShouldBe(2);
		resumen.AusenciasAExamen.ShouldBe(1);
		resumen.TieneCalificaciones.ShouldBeTrue();
	}

	[Fact]
	public async Task ConsultarResumenAcademicoAsync_sin_ninguna_nota_rendida_deja_el_promedio_en_nulo()
	{
		var (alumno, cursante) = SembrarCursada();
		var matematica = SembrarMateria("Matemática");

		cursante.RegistrarInasistenciaAExamen(matematica, DateTime.Today, Periodo.PrimerCuatrimestre, false, null);

		var resumen = await _servicio.ConsultarResumenAcademicoAsync(new PersonaRequest(alumno.Id));

		resumen.Promedio.ShouldBeNull();
		resumen.AusenciasAExamen.ShouldBe(1);
		resumen.Materias.ShouldHaveSingleItem().NotaDeCierre.ShouldBeNull();
		resumen.Materias.First().Aprobada.ShouldBeFalse();
	}

	/// <remarks>
	/// <para>
	/// EL CRITERIO NO SE PRUEBA ACÁ: vive en <c>RendimientoAcademico</c>, en el dominio, y lo fija
	/// <c>Domain.UnitTests/Cursantes/RendimientoAcademicoTests</c>. Lo que esta prueba afirma es que
	/// el caso de uso <strong>delega</strong> en esa regla en lugar de aplicar la suya: el 4 del
	/// primer cuatrimestre desaparece de la cuenta porque su recuperatorio lo reemplazó, y si
	/// alguien volviera a escribir el criterio en <c>Core</c> —promediando los dos intentos, que es
	/// lo que este caso de uso hacía antes de que el modelo distinguiera los períodos— el número
	/// que llega al <c>Response</c> sería 5,5 y la materia saldría desaprobada.
	/// </para>
	/// <para>
	/// Las dos pruebas son necesarias y no se solapan: la de dominio fija la regla, esta fija el
	/// cableado. Borrar la de acá dejaría pasar un <c>Core</c> que calcula bien por su cuenta,
	/// que es el defecto que la migración vino a cerrar.
	/// </para>
	/// </remarks>
	[Fact]
	public async Task ConsultarResumenAcademicoAsync_el_recuperatorio_reemplaza_la_nota_del_cuatrimestre()
	{
		var (alumno, cursante) = SembrarCursada();
		var lengua = SembrarMateria("Lengua");

		cursante.RegistrarCalificacion(lengua, DateTime.Today.AddMonths(-2), Periodo.PrimerCuatrimestre, false, 4, null);
		cursante.RegistrarCalificacion(lengua, DateTime.Today, Periodo.PrimerCuatrimestre, true, 7, null);

		var resumen = await _servicio.ConsultarResumenAcademicoAsync(new PersonaRequest(alumno.Id));

		var materia = resumen.Materias.ShouldHaveSingleItem();
		materia.PrimerCuatrimestre.ShouldBe(7m);
		materia.NotaDeCierre.ShouldBe(7m);
		materia.Aprobada.ShouldBeTrue();
		materia.Rendidas.ShouldBe(2);
		resumen.MateriasAprobadas.ShouldBe(1);
		resumen.MateriasDesaprobadas.ShouldBe(0);
	}

	/// <remarks>
	/// La contracara de la regla, y la que explica por qué no es "vale la mejor": el recuperatorio
	/// reemplaza incluso cuando es PEOR. Con un 8 recuperado con 6 el cuatrimestre vale 6.
	/// </remarks>
	[Fact]
	public async Task ConsultarResumenAcademicoAsync_el_recuperatorio_reemplaza_aunque_sea_peor()
	{
		var (alumno, cursante) = SembrarCursada();
		var lengua = SembrarMateria("Lengua");

		cursante.RegistrarCalificacion(lengua, DateTime.Today.AddMonths(-2), Periodo.PrimerCuatrimestre, false, 8, null);
		cursante.RegistrarCalificacion(lengua, DateTime.Today, Periodo.PrimerCuatrimestre, true, 6, null);

		var resumen = await _servicio.ConsultarResumenAcademicoAsync(new PersonaRequest(alumno.Id));

		resumen.Materias.ShouldHaveSingleItem().PrimerCuatrimestre.ShouldBe(6m);
		resumen.Promedio.ShouldBe(6m);
	}

	/// <remarks>
	/// La nota final la carga el docente y PISA al promedio de los cuatrimestres: con 4 y 8 el
	/// promedio sería 6, y si el docente cerró en 9 la materia cierra en 9. No es una cuenta del
	/// sistema sino su decisión, y por eso el modelo la guarda como una calificación más.
	/// </remarks>
	[Fact]
	public async Task ConsultarResumenAcademicoAsync_la_nota_final_reemplaza_al_promedio_de_cuatrimestres()
	{
		var (alumno, cursante) = SembrarCursada();
		var lengua = SembrarMateria("Lengua");

		cursante.RegistrarCalificacion(lengua, DateTime.Today.AddMonths(-4), Periodo.PrimerCuatrimestre, false, 4, null);
		cursante.RegistrarCalificacion(lengua, DateTime.Today.AddMonths(-1), Periodo.SegundoCuatrimestre, false, 8, null);
		cursante.RegistrarCalificacion(lengua, DateTime.Today, Periodo.Final, false, 9, null);

		var resumen = await _servicio.ConsultarResumenAcademicoAsync(new PersonaRequest(alumno.Id));

		var materia = resumen.Materias.ShouldHaveSingleItem();
		materia.Final.ShouldBe(9m);
		materia.NotaDeCierre.ShouldBe(9m);
		resumen.Promedio.ShouldBe(9m);
	}

	/// <remarks>
	/// Y la recuperación pisa a la nota final, porque a un período de recuperación se llega después
	/// de no haberla alcanzado: es lo último que se rindió y lo que cierra la materia.
	/// </remarks>
	[Fact]
	public async Task ConsultarResumenAcademicoAsync_la_recuperacion_reemplaza_a_la_nota_final()
	{
		var (alumno, cursante) = SembrarCursada();
		var lengua = SembrarMateria("Lengua");

		cursante.RegistrarCalificacion(lengua, DateTime.Today.AddMonths(-2), Periodo.Final, false, 4, null);
		cursante.RegistrarCalificacion(lengua, DateTime.Today, Periodo.PrimeraRecuperacion, false, 6, null);

		var resumen = await _servicio.ConsultarResumenAcademicoAsync(new PersonaRequest(alumno.Id));

		var materia = resumen.Materias.ShouldHaveSingleItem();
		materia.Recuperacion.ShouldBe(6m);
		materia.NotaDeCierre.ShouldBe(6m);
		materia.Aprobada.ShouldBeTrue();
	}

	[Fact]
	public async Task ConsultarResumenAcademicoAsync_agrupa_una_fila_por_materia_en_orden_alfabetico()
	{
		var (alumno, cursante) = SembrarCursada();
		var matematica = SembrarMateria("Matemática");
		var historia = SembrarMateria("Historia");

		cursante.RegistrarCalificacion(matematica, DateTime.Today, Periodo.PrimerCuatrimestre, false, 9, null);
		cursante.RegistrarCalificacion(matematica, DateTime.Today, Periodo.PrimerCuatrimestre, true, 7, null);
		cursante.RegistrarCalificacion(historia, DateTime.Today, Periodo.PrimerCuatrimestre, false, 5, null);

		var resumen = await _servicio.ConsultarResumenAcademicoAsync(new PersonaRequest(alumno.Id));

		resumen.Materias.Select(x => x.Materia).ShouldBe(new[] { "Historia", "Matemática" });
		resumen.Materias.First(x => x.Materia == "Matemática").Rendidas.ShouldBe(2);
		resumen.Materias.First(x => x.Materia == "Matemática").NotaDeCierre.ShouldBe(7m);
		resumen.MateriasAprobadas.ShouldBe(1);
		resumen.MateriasDesaprobadas.ShouldBe(1);
		// Una materia pesa UNA vez en el promedio general, con la nota que la cerró: 7 y 5 dan 6,
		// no el promedio de las tres instancias cargadas, que daría 7.
		resumen.Promedio.ShouldBe(6m);
	}

	/// <remarks>
	/// La tardanza NO descuenta —el alumno estuvo en clase— y la falta justificada SÍ, igual que en
	/// <c>ResumenAsistenciaResponse</c>. Con 4 jornadas, 1 ausencia y 1 inasistencia el porcentaje es
	/// 50: las dos faltas cuentan y la tardanza no.
	/// </remarks>
	[Fact]
	public async Task ConsultarResumenAcademicoAsync_cuenta_la_asistencia_sin_descontar_las_tardanzas()
	{
		var (alumno, cursante) = SembrarCursada();

		var planilla = new PlanillaAsistenciaBuilder().ConCursantes(cursante.Id).Build();
		planilla.MarcarPresente(cursante.Id);
		_host.UnidadDeTrabajo.PlanillasAsistenciaFake.Sembrar(planilla);

		var conTardanza = new PlanillaAsistenciaBuilder().ConCursantes(cursante.Id)
														.ConFecha(DateTime.Today.AddDays(-1))
														.Build();
		conTardanza.MarcarTardanza(cursante.Id, TimeSpan.FromMinutes(10));
		_host.UnidadDeTrabajo.PlanillasAsistenciaFake.Sembrar(conTardanza);

		var conAusencia = new PlanillaAsistenciaBuilder().ConCursantes(cursante.Id)
														.ConFecha(DateTime.Today.AddDays(-2))
														.Build();
		conAusencia.MarcarAusencia(cursante.Id);
		_host.UnidadDeTrabajo.PlanillasAsistenciaFake.Sembrar(conAusencia);

		var conInasistencia = new PlanillaAsistenciaBuilder().ConCursantes(cursante.Id)
															.ConFecha(DateTime.Today.AddDays(-3))
															.Build();
		conInasistencia.MarcarInasistencia(cursante.Id);
		_host.UnidadDeTrabajo.PlanillasAsistenciaFake.Sembrar(conInasistencia);

		var resumen = await _servicio.ConsultarResumenAcademicoAsync(new PersonaRequest(alumno.Id));

		resumen.Asistencia.Jornadas.ShouldBe(4);
		resumen.Asistencia.Presentes.ShouldBe(1);
		resumen.Asistencia.Tardanzas.ShouldBe(1);
		resumen.Asistencia.Ausencias.ShouldBe(1);
		resumen.Asistencia.Inasistencias.ShouldBe(1);
		resumen.Asistencia.Faltas.ShouldBe(2);
		resumen.Asistencia.Porcentaje.ShouldBe(50m);
	}

	/// <remarks>
	/// La planilla es de la división entera, así que la consulta la trae completa: adentro vienen
	/// las marcas de los compañeros. Sin el filtro por <c>CursanteID</c> el alumno heredaría las
	/// faltas de los demás, que es un defecto que ningún número en pantalla delataría.
	/// </remarks>
	[Fact]
	public async Task ConsultarResumenAcademicoAsync_no_cuenta_las_marcas_de_los_companeros_de_division()
	{
		var (alumno, cursante) = SembrarCursada();
		var companiero = Guid.NewGuid();

		var compartida = new PlanillaAsistenciaBuilder().ConCursantes(cursante.Id, companiero).Build();
		compartida.MarcarPresente(cursante.Id);
		compartida.MarcarAusencia(companiero);
		_host.UnidadDeTrabajo.PlanillasAsistenciaFake.Sembrar(compartida);

		var resumen = await _servicio.ConsultarResumenAcademicoAsync(new PersonaRequest(alumno.Id));

		resumen.Asistencia.Jornadas.ShouldBe(1);
		resumen.Asistencia.Faltas.ShouldBe(0);
		resumen.Asistencia.Porcentaje.ShouldBe(100m);
	}

	/// <remarks>
	/// EL CASO DEL TRASLADO. <c>Calificacion</c> es entidad hija de <c>Cursante</c>, así que las
	/// notas anteriores a un cambio de división viven en la inscripción CERRADA. Un promedio que
	/// mirara sólo la vigente las perdería en silencio: acá serían 9 en vez de 7.
	/// </remarks>
	[Fact]
	public async Task ConsultarResumenAcademicoAsync_suma_las_notas_de_la_inscripcion_cerrada_por_traslado()
	{
		var (alumno, origen) = SembrarCursada(periodo: "2026",
											  fechaInicio: DateTime.Today.AddMonths(-6),
											  fechaBaja: DateTime.Today.AddMonths(-3));
		var matematica = SembrarMateria("Matemática");

		origen.RegistrarCalificacion(matematica, DateTime.Today.AddMonths(-4), Periodo.PrimerCuatrimestre, false, 5, null);

		var destino = new CursanteBuilder().ConAlumno(alumno.Id)
										   .ConCicloLectivo(CicloLectivo.Crear("2026"))
										   .ConFechaInicio(DateTime.Today.AddMonths(-3))
										   .Build();
		// Un cuatrimestre a cada lado del traslado, y no dos intentos del mismo: si fueran del
		// mismo período el recuperatorio taparía al 5 y el promedio daría 9 con o sin el arreglo,
		// así que la prueba no detectaría nada.
		destino.RegistrarCalificacion(matematica, DateTime.Today, Periodo.SegundoCuatrimestre, false, 9, null);
		_host.UnidadDeTrabajo.CursantesFake.Sembrar(destino);

		var resumen = await _servicio.ConsultarResumenAcademicoAsync(new PersonaRequest(alumno.Id));

		resumen.Promedio.ShouldBe(7m);
		resumen.Rendidas.ShouldBe(2);
		resumen.Trayectoria.Count.ShouldBe(2);
		resumen.Cursada!.CursanteID.ShouldBe(destino.Id);
	}

	/// <remarks>
	/// Las notas de otro año NO se mezclan. El promedio es del ciclo de la inscripción vigente; sin
	/// el filtro por período, un alumno con cinco años de cursada mostraría un número que no
	/// describe a nadie.
	/// </remarks>
	[Fact]
	public async Task ConsultarResumenAcademicoAsync_no_mezcla_las_notas_de_otros_ciclos_lectivos()
	{
		var (alumno, vigente) = SembrarCursada(periodo: "2026");
		var matematica = SembrarMateria("Matemática");

		vigente.RegistrarCalificacion(matematica, DateTime.Today, Periodo.PrimerCuatrimestre, false, 8, null);

		var anterior = new CursanteBuilder().ConAlumno(alumno.Id)
											.ConCicloLectivo(CicloLectivo.Crear("2025"))
											.ConFechaInicio(DateTime.Today.AddYears(-1))
											.DadoDeBajaEl(DateTime.Today.AddMonths(-9))
											.Build();
		anterior.RegistrarCalificacion(matematica, DateTime.Today.AddMonths(-10), Periodo.PrimerCuatrimestre, false, 2, null);
		_host.UnidadDeTrabajo.CursantesFake.Sembrar(anterior);

		var resumen = await _servicio.ConsultarResumenAcademicoAsync(new PersonaRequest(alumno.Id));

		resumen.CicloLectivo.ShouldBe("2026");
		resumen.Promedio.ShouldBe(8m);
		resumen.Rendidas.ShouldBe(1);
		// La trayectoria sí trae los dos: es el historial completo, no el ciclo informado.
		resumen.Trayectoria.Count.ShouldBe(2);
	}

	/// <remarks>
	/// Sin inscripción vigente el resumen informa el ciclo de la MÁS RECIENTE y no se queda en
	/// blanco: un egresado tiene notas y asistencia que siguen siendo su legajo. La pantalla rotula
	/// el número con ese año, así que el dato no cambia de significado.
	/// </remarks>
	[Fact]
	public async Task ConsultarResumenAcademicoAsync_sin_inscripcion_vigente_informa_el_ultimo_ciclo()
	{
		var (alumno, cerrada) = SembrarCursada(periodo: "2025",
											   fechaInicio: DateTime.Today.AddYears(-1),
											   fechaBaja: DateTime.Today.AddMonths(-6));
		var matematica = SembrarMateria("Matemática");

		cerrada.RegistrarCalificacion(matematica, DateTime.Today.AddMonths(-7), Periodo.PrimerCuatrimestre, false, 6, null);

		var resumen = await _servicio.ConsultarResumenAcademicoAsync(new PersonaRequest(alumno.Id));

		resumen.EstaCursando.ShouldBeFalse();
		resumen.Cursada.ShouldBeNull();
		resumen.CicloLectivo.ShouldBe("2025");
		resumen.Promedio.ShouldBe(6m);
	}

	/// <remarks>
	/// Es una lectura y no exige permiso, igual que las otras ~30 consultas de <c>Core</c>
	/// (CORE-16). Las cuatro excepciones declaradas son las de impacto de borrado.
	/// </remarks>
	[Fact]
	public async Task ConsultarResumenAcademicoAsync_es_una_lectura_y_no_exige_permiso()
	{
		// Patrón vigente del repositorio de código (CORE-16): las lecturas quedan abiertas.
		var alumno = new AlumnoBuilder().Build();
		using var host = new HostDeServicios(sembrar: uow => uow.AlumnosFake.Sembrar(alumno),
											 sesion: SesionDePrueba.SinPermisos());

		var resumen = await host.Resolver<IServicioAlumno>()
			.ConsultarResumenAcademicoAsync(new PersonaRequest(alumno.Id));

		resumen.AlumnoID.ShouldBe(alumno.Id);
	}

	/// <remarks>
	/// Misma guarda para la búsqueda: es la consulta que alimenta el índice de la pantalla, y una
	/// guarda acá dejaría la pantalla de Alumnos inservible para cualquier rol que la pueda abrir.
	/// </remarks>
	[Fact]
	public async Task BuscarAlumnosAsync_es_una_lectura_y_no_exige_permiso()
	{
		using var host = new HostDeServicios(sesion: SesionDePrueba.SinPermisos());

		var encontrados = await host.Resolver<IServicioAlumno>()
			.BuscarAlumnosAsync(new BuscarAlumnoRequest("Pérez"));

		encontrados.ShouldBeEmpty();
	}
	#endregion
}
