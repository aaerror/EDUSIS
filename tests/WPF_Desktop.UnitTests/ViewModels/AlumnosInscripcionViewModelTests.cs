using Core.ServicioAlumnos;
using Core.ServicioAlumnos.DTOs.Requests;
using Core.ServicioCursantes;
using Core.ServicioCursantes.DTOs.Requests;
using Core.ServicioCursos;
using Core.ServicioCursos.DTOs.Responses;
using Core.ServicioDivisiones;
using Core.ServicioDivisiones.DTOs.Requests;
using Core.ServicioDivisiones.DTOs.Responses;
using Domain.Cursos;
using Domain.Shared;
using NSubstitute;
using Shouldly;
using WPF_Desktop.Store;
using WPF_Desktop.UnitTests.Dobles;
using WPF_Desktop.ViewModels.Alumnos;
using Xunit;

namespace WPF_Desktop.UnitTests.ViewModels;

/// <summary>
/// Cubre <c>UI-05</c> (<see cref="InscripcionAlumnoViewModel"/>) y la parte testeable de <c>UI-04</c>
/// (<see cref="RegistrarAlumnoViewModel"/>): la inscripcion llega a <see cref="IServicioCursante.InscribirCursanteAsync"/>,
/// el periodo sale de <see cref="CicloLectivoStore"/>, el alumno de <see cref="LegajoStore"/> y la UI no reimplementa la
/// validacion de cupo (la hace <c>Core</c>: la UI solo muestra la excepcion).
/// <para>
/// <c>RegistrarAlumnoViewModel</c> ya no usa <c>MessageBox.Show</c>: avisa por <c>IDialogService</c>, que acá se dobla
/// con <see cref="Dobles.DialogServiceFalso"/>. El comentario anterior decia que su guardado solo se podia cubrir por
/// guarda sobre el codigo fuente justamente porque el MessageBox bloqueaba el hilo de la prueba.
/// </para>
/// <para>
/// Por eso la region "UI-33" cubre el alta por <strong>ejecucion</strong> y no solo por guarda sobre el codigo
/// fuente. La guarda estatica se conserva para que la migracion no se revierta por descuido.
/// </para>
/// </summary>
public class AlumnosInscripcionViewModelTests
{
	private const string CicloDelStore = "2031";

	private sealed class Contexto
	{
		public IServicioCurso ServicioCursos { get; } = Substitute.For<IServicioCurso>();
		public IServicioCursante ServicioCursantes { get; } = Substitute.For<IServicioCursante>();
		public IServicioDivision ServicioDivisiones { get; } = Substitute.For<IServicioDivision>();
		public LegajoStore LegajoStore { get; } = new();
		public CicloLectivoStore CicloLectivoStore { get; } = new();

		/// <summary>
		/// Doble del servicio de diálogos: las dos pantallas pasaron a avisar por acá en lugar de
		/// abrir un <c>MessageBox</c>, así que la prueba puede leer lo que le dijeron al usuario.
		/// </summary>
		public DialogServiceFalso Dialogos { get; } = new();

		public NavegacionFalsa NavegarAAlumnos { get; } = new();

		public Guid AlumnoID { get; } = Guid.NewGuid();
		public CursoResponse Primero { get; } = new(Guid.NewGuid(), Grado.Primero, NivelEducativo.Secundaria);
		public CursoResponse Segundo { get; } = new(Guid.NewGuid(), Grado.Segundo, NivelEducativo.Secundaria);
		public DivisionResponse DivisionA { get; } = new(Guid.NewGuid(), "A", null, null, 0);
		public DivisionResponse DivisionB { get; } = new(Guid.NewGuid(), "B", null, null, 0);

		public Contexto()
		{
			CicloLectivoStore.CicloLectivo = CicloDelStore;
			LegajoStore.PersonaID = AlumnoID;

			ServicioCursos.ListarCursosAsync()
						  .Returns(Task.FromResult<IReadOnlyCollection<CursoResponse>>(new List<CursoResponse> { Primero, Segundo }));
			ServicioDivisiones.ListarDivisionesAsync(Arg.Any<ListarDivisionesRequest>())
							  .Returns(Task.FromResult<IReadOnlyCollection<DivisionResponse>>(new List<DivisionResponse> { DivisionA, DivisionB }));
		}

		public InscripcionAlumnoViewModel CrearInscripcion()
		{
			return new InscripcionAlumnoViewModel(ServicioCursos, ServicioCursantes, ServicioDivisiones, Dialogos, NavegarAAlumnos, LegajoStore, CicloLectivoStore);
		}

		/// <summary>
		/// El doble del servicio de alumnos es del contexto y no una instancia anónima dentro de
		/// <c>CrearRegistro</c>: las pruebas de la región UI-33 necesitan configurarlo antes de
		/// construir el ViewModel y leer qué recibió después.
		/// </summary>
		public IServicioAlumno ServicioAlumnos { get; } = Substitute.For<IServicioAlumno>();

		public RegistrarAlumnoViewModel CrearRegistro()
		{
			return new RegistrarAlumnoViewModel(ServicioAlumnos, ServicioCursos, ServicioCursantes, ServicioDivisiones, Dialogos, NavegarAAlumnos, CicloLectivoStore);
		}
	}

	#region UI-05: InscripcionAlumnoViewModel
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_periodo_sale_del_CicloLectivoStore()
	{
		var contexto = new Contexto();

		var viewModel = contexto.CrearInscripcion();

		viewModel.Periodo.ShouldBe(CicloDelStore);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_periodo_acompania_al_store_sin_estado_propio()
	{
		var contexto = new Contexto();
		var viewModel = contexto.CrearInscripcion();

		contexto.CicloLectivoStore.CicloLectivo = "2040";

		viewModel.Periodo.ShouldBe("2040");
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Los_cursos_se_cargan_desde_IServicioCurso()
	{
		var contexto = new Contexto();

		var viewModel = contexto.CrearInscripcion();

		viewModel.Cursos.Select(c => c.CursoID).ShouldBe(new[] { contexto.Primero.CursoID, contexto.Segundo.CursoID });
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Elegir_un_curso_pide_las_divisiones_de_ese_curso_en_el_ciclo_del_store()
	{
		var contexto = new Contexto();
		var viewModel = contexto.CrearInscripcion();

		viewModel.Curso = viewModel.Cursos[1];

		contexto.ServicioDivisiones.Received(1).ListarDivisionesAsync(Arg.Is<ListarDivisionesRequest>(r =>
			r.CursoID == contexto.Segundo.CursoID &&
			r.CicloLectivo == CicloDelStore));
		viewModel.Divisiones.Select(d => d.DivisionID).ShouldBe(new[] { contexto.DivisionA.DivisionID, contexto.DivisionB.DivisionID });
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Cambiar_de_curso_descarta_la_division_elegida()
	{
		var contexto = new Contexto();
		var viewModel = contexto.CrearInscripcion();
		viewModel.Curso = viewModel.Cursos[0];
		viewModel.Division = viewModel.Divisiones[0];

		viewModel.Curso = viewModel.Cursos[1];

		viewModel.Division.ShouldBeNull();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Inscribir_solo_se_habilita_con_curso_y_division()
	{
		var contexto = new Contexto();
		var viewModel = contexto.CrearInscripcion();
		viewModel.InscribirCommand.CanExecute(null).ShouldBeFalse();

		viewModel.Curso = viewModel.Cursos[0];
		viewModel.InscribirCommand.CanExecute(null).ShouldBeFalse();

		viewModel.Division = viewModel.Divisiones[0];
		viewModel.InscribirCommand.CanExecute(null).ShouldBeTrue();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Inscribir_llega_a_InscribirCursanteAsync_con_curso_division_alumno_del_store_periodo_y_recursante()
	{
		var contexto = new Contexto();
		var viewModel = contexto.CrearInscripcion();
		viewModel.Curso = viewModel.Cursos[0];
		viewModel.Division = viewModel.Divisiones[1];
		viewModel.EsRecursante = true;

		await viewModel.InscribirCommand.ExecuteAsync(null);

		await contexto.ServicioCursantes.Received(1).InscribirCursanteAsync(
			new RegistrarCursanteRequest(contexto.Primero.CursoID, contexto.DivisionB.DivisionID, contexto.AlumnoID, CicloDelStore, DateTime.Today, true));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void La_fecha_de_inicio_viene_precargada_con_la_de_hoy()
	{
		var contexto = new Contexto();

		var viewModel = contexto.CrearInscripcion();

		viewModel.FechaInicio.ShouldBe(DateTime.Today);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Inscribir_manda_la_fecha_de_inicio_que_ingreso_el_usuario_y_no_la_de_hoy()
	{
		// CUR-1: la fecha de inicio la decide el usuario. Antes la forzaba Core con DateTime.Today.
		var contexto = new Contexto();
		var viewModel = contexto.CrearInscripcion();
		viewModel.Curso = viewModel.Cursos[0];
		viewModel.Division = viewModel.Divisiones[0];
		var fecha = DateTime.Today.AddDays(-30);
		viewModel.FechaInicio = fecha;

		await viewModel.InscribirCommand.ExecuteAsync(null);

		await contexto.ServicioCursantes.Received(1).InscribirCursanteAsync(
			Arg.Is<RegistrarCursanteRequest>(r => r.FechaInicio == fecha));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Inscribir_sin_marcar_recursante_envia_EsRecursante_en_false()
	{
		var contexto = new Contexto();
		var viewModel = contexto.CrearInscripcion();
		viewModel.Curso = viewModel.Cursos[0];
		viewModel.Division = viewModel.Divisiones[0];

		await viewModel.InscribirCommand.ExecuteAsync(null);

		await contexto.ServicioCursantes.Received(1).InscribirCursanteAsync(Arg.Is<RegistrarCursanteRequest>(r => r.EsRecursante == false));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Una_inscripcion_exitosa_avisa_y_limpia_la_seleccion()
	{
		var contexto = new Contexto();
		var viewModel = contexto.CrearInscripcion();
		viewModel.Curso = viewModel.Cursos[0];
		viewModel.Division = viewModel.Divisiones[0];
		viewModel.EsRecursante = true;

		await viewModel.InscribirCommand.ExecuteAsync(null);

		// El éxito dejó de escribirse en el aviso ámbar de la pantalla —donde se veía igual que
		// un error— y sale como aviso al pie, que se va solo.
		contexto.Dialogos.Informaciones.ShouldHaveSingleItem().Titulo.ShouldBe("Alumno inscripto");
		viewModel.HabilitarMessage.ShouldBeFalse();
		viewModel.Message.ShouldBeEmpty();
		viewModel.Curso.ShouldBeNull();
		viewModel.Division.ShouldBeNull();
		viewModel.EsRecursante.ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Si_Core_rechaza_por_cupo_la_UI_solo_muestra_la_excepcion_y_no_la_propaga()
	{
		var contexto = new Contexto();
		contexto.ServicioCursantes.InscribirCursanteAsync(Arg.Any<RegistrarCursanteRequest>())
				.Returns(Task.FromException(new InvalidOperationException("La division no tiene cupo disponible.")));
		var viewModel = contexto.CrearInscripcion();
		viewModel.Curso = viewModel.Cursos[0];
		viewModel.Division = viewModel.Divisiones[0];

		await Should.NotThrowAsync(async () =>
		{
			await viewModel.InscribirCommand.ExecuteAsync(null);
		});

		// El rechazo de Core va al diálogo, que exige acuse: un aviso al pie que se desvanece a
		// los cinco segundos es lo último que conviene para la única pista de por qué no se
		// pudo inscribir.
		contexto.Dialogos.Errores.ShouldHaveSingleItem().Mensaje.ShouldBe("La division no tiene cupo disponible.");
		contexto.Dialogos.Informaciones.ShouldBeEmpty();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task La_UI_no_reimplementa_el_cupo_y_solo_consulta_divisiones_para_el_combo()
	{
		var contexto = new Contexto();
		var viewModel = contexto.CrearInscripcion();
		viewModel.Curso = viewModel.Cursos[0];
		viewModel.Division = viewModel.Divisiones[0];

		await viewModel.InscribirCommand.ExecuteAsync(null);

		var metodosLlamados = contexto.ServicioDivisiones.ReceivedCalls().Select(c => c.GetMethodInfo().Name).Distinct().ToList();
		metodosLlamados.ShouldBe(new[] { nameof(IServicioDivision.ListarDivisionesAsync) });
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Inscribir_sin_curso_o_sin_division_no_llega_al_servicio()
	{
		var contexto = new Contexto();
		var viewModel = contexto.CrearInscripcion();
		viewModel.Curso = viewModel.Cursos[0];

		await viewModel.InscribirCommand.ExecuteAsync(null);

		await contexto.ServicioCursantes.DidNotReceive().InscribirCursanteAsync(Arg.Any<RegistrarCursanteRequest>());
	}
	#endregion

	#region UI-04: RegistrarAlumnoViewModel
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void RegistrarAlumno_toma_el_periodo_inicial_del_CicloLectivoStore()
	{
		var contexto = new Contexto();

		var viewModel = contexto.CrearRegistro();

		viewModel.Periodo.ShouldBe(CicloDelStore);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void RegistrarAlumno_carga_los_cursos_desde_IServicioCurso()
	{
		var contexto = new Contexto();

		var viewModel = contexto.CrearRegistro();

		viewModel.Cursos.Select(c => c.CursoID).ShouldBe(new[] { contexto.Primero.CursoID, contexto.Segundo.CursoID });
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void RegistrarAlumno_al_elegir_curso_pide_las_divisiones_del_ciclo_del_store()
	{
		var contexto = new Contexto();
		var viewModel = contexto.CrearRegistro();

		viewModel.Curso = viewModel.Cursos[0];

		contexto.ServicioDivisiones.Received(1).ListarDivisionesAsync(Arg.Is<ListarDivisionesRequest>(r =>
			r.CursoID == contexto.Primero.CursoID &&
			r.CicloLectivo == CicloDelStore));
		viewModel.Divisiones.Count.ShouldBe(2);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void RegistrarAlumno_Guardar_exige_curso_y_division()
	{
		var contexto = new Contexto();
		var viewModel = contexto.CrearRegistro();
		viewModel.GuardarCommand.CanExecute(null).ShouldBeFalse();

		viewModel.Curso = viewModel.Cursos[0];
		viewModel.GuardarCommand.CanExecute(null).ShouldBeFalse();

		viewModel.Division = viewModel.Divisiones[0];
		viewModel.GuardarCommand.CanExecute(null).ShouldBeTrue();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void RegistrarAlumno_la_inscripcion_llega_a_IServicioCursante_y_no_reimplementa_el_cupo()
	{
		var fuente = RaizDelRepositorio.Leer("WPF_Desktop/ViewModels/Alumnos/RegistrarAlumnoViewModel.cs");

		fuente.ShouldContain("await _servicioCursantes.InscribirCursanteAsync(");
		fuente.ShouldNotContain("ContarCursantes");
		fuente.ShouldNotContain("ValidarCupo");
		fuente.ShouldNotContain("//TODO: Refactorizar");
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void InscripcionAlumno_la_inscripcion_llega_a_IServicioCursante_y_no_reimplementa_el_cupo()
	{
		var fuente = RaizDelRepositorio.Leer("WPF_Desktop/ViewModels/Alumnos/InscripcionAlumnoViewModel.cs");

		fuente.ShouldContain("await _servicioCursantes.InscribirCursanteAsync(");
		fuente.ShouldContain("_legajoStore.PersonaID");
		fuente.ShouldNotContain("ContarCursantes");
		fuente.ShouldNotContain("ValidarCupo");
	}
	#endregion

	#region UI-33: el alta de alumno, ahora ejecutable
	/// <summary>
	/// Deja los tres sub-ViewModels con datos válidos, que es la precondición de
	/// <c>ContinuarCommand</c>: sin esto corta en la guarda de campos vacíos.
	/// </summary>
	private static void CompletarFormulario(RegistrarAlumnoViewModel viewModel)
	{
		viewModel.InformacionPersonal.Apellido = "Perez";
		viewModel.InformacionPersonal.Nombre = "Ana";
		viewModel.InformacionPersonal.DocumentoNacionalIdentidad = "33998731";
		viewModel.InformacionPersonal.Sexo = "Femenino";
		viewModel.InformacionPersonal.Nacionalidad = "Argentina";
		viewModel.Domicilio.Calle = "Rivadavia";
		viewModel.Domicilio.Vivienda = "Casa";
		viewModel.Domicilio.Localidad = "Moron";
		viewModel.Domicilio.Provincia = "Buenos Aires";
		viewModel.Contacto.Telefono = "1144556677";
		viewModel.Contacto.Email = "ana.perez@ejemplo.com";
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Continuar_con_un_campo_vacio_avisa_y_NO_borra_lo_que_el_usuario_cargo()
	{
		// El defecto que cubre esta prueba: la rama de campos incompletos reemplazaba los tres
		// sub-ViewModels por instancias vacías, así que olvidarse el teléfono borraba también el
		// nombre, el DNI y el domicilio ya tipeados. Avisar no puede costar el formulario entero.
		var contexto = new Contexto();
		var viewModel = contexto.CrearRegistro();
		CompletarFormulario(viewModel);
		viewModel.Contacto.Telefono = string.Empty;

		await viewModel.ContinuarCommand.ExecuteAsync(null);

		viewModel.InformacionPersonal.Apellido.ShouldBe("Perez");
		viewModel.InformacionPersonal.Nombre.ShouldBe("Ana");
		viewModel.InformacionPersonal.DocumentoNacionalIdentidad.ShouldBe("33998731");
		viewModel.Domicilio.Calle.ShouldBe("Rivadavia");
		viewModel.Contacto.Email.ShouldBe("ana.perez@ejemplo.com");

		// Falta un dato, no se rompió nada: es una advertencia y nombra el campo que falta.
		contexto.Dialogos.Advertencias.ShouldHaveSingleItem();
		contexto.Dialogos.Advertencias[0].Mensaje.ShouldContain("teléfono");
		await contexto.ServicioAlumnos.DidNotReceive().RegistrarAlumnoAsync(Arg.Any<RegistrarAlumnoRequest>());
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Continuar_registra_el_alumno_y_pasa_a_la_solapa_de_inscripcion()
	{
		var contexto = new Contexto();
		contexto.ServicioAlumnos.EsDocumentoInvalidoAsync(Arg.Any<DocumentoRequest>()).Returns(Task.FromResult(false));
		contexto.ServicioAlumnos.RegistrarAlumnoAsync(Arg.Any<RegistrarAlumnoRequest>()).Returns(Task.FromResult(Guid.NewGuid()));
		var viewModel = contexto.CrearRegistro();
		CompletarFormulario(viewModel);

		await viewModel.ContinuarCommand.ExecuteAsync(null);

		await contexto.ServicioAlumnos.Received(1).RegistrarAlumnoAsync(Arg.Is<RegistrarAlumnoRequest>(r =>
			r.Apellido == "Perez" && r.Nombre == "Ana" && r.DNI == "33998731" && r.Email == "ana.perez@ejemplo.com"));
		viewModel.Tab.ShouldBe(1);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Continuar_no_pregunta_nada_antes_de_registrar()
	{
		// La contracara de la prueba de master `Continuar_sin_confirmar_no_registra_nada`: acá el
		// paso 1 NO confirma. El botón dice "Continuar" y lo que hace es continuar; un alta no es
		// una acción destructiva y preguntarlo de nuevo no agregaba información. Si alguien vuelve
		// a meter un diálogo acá, esta prueba lo marca.
		var contexto = new Contexto();
		contexto.Dialogos.RespuestaDeConfirmacion = false;
		contexto.ServicioAlumnos.EsDocumentoInvalidoAsync(Arg.Any<DocumentoRequest>()).Returns(Task.FromResult(false));
		contexto.ServicioAlumnos.RegistrarAlumnoAsync(Arg.Any<RegistrarAlumnoRequest>()).Returns(Task.FromResult(Guid.NewGuid()));
		var viewModel = contexto.CrearRegistro();
		CompletarFormulario(viewModel);

		await viewModel.ContinuarCommand.ExecuteAsync(null);

		contexto.Dialogos.Confirmaciones.ShouldBeEmpty();
		contexto.Dialogos.Solicitudes.ShouldBeEmpty();
		await contexto.ServicioAlumnos.Received(1).RegistrarAlumnoAsync(Arg.Any<RegistrarAlumnoRequest>());
		viewModel.Tab.ShouldBe(1);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Un_documento_duplicado_avisa_sin_borrar_lo_tipeado_y_no_registra()
	{
		// Master borraba el documento al avisar. Acá queda: el caso corriente es un dígito mal
		// tipeado, y vaciar el campo obliga a escribir los ocho de nuevo para corregir uno.
		var contexto = new Contexto();
		contexto.ServicioAlumnos.EsDocumentoInvalidoAsync(Arg.Any<DocumentoRequest>()).Returns(Task.FromResult(true));
		var viewModel = contexto.CrearRegistro();
		CompletarFormulario(viewModel);

		await viewModel.ContinuarCommand.ExecuteAsync(null);

		viewModel.InformacionPersonal.DocumentoNacionalIdentidad.ShouldBe("33998731");
		viewModel.InformacionPersonal.Apellido.ShouldBe("Perez");
		contexto.Dialogos.Advertencias.ShouldHaveSingleItem();
		await contexto.ServicioAlumnos.DidNotReceive().RegistrarAlumnoAsync(Arg.Any<RegistrarAlumnoRequest>());
		viewModel.Tab.ShouldBe(0);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Si_Core_rechaza_el_alta_la_UI_muestra_el_error_y_no_lo_propaga()
	{
		// Con async void esta excepción se perdía en el SynchronizationContext en vez de llegar acá.
		var contexto = new Contexto();
		contexto.ServicioAlumnos.EsDocumentoInvalidoAsync(Arg.Any<DocumentoRequest>()).Returns(Task.FromResult(false));
		contexto.ServicioAlumnos.RegistrarAlumnoAsync(Arg.Any<RegistrarAlumnoRequest>())
				.Returns(Task.FromException<Guid>(new FormatException("Verificar el legajo del alumno: legajo.")));
		var viewModel = contexto.CrearRegistro();
		CompletarFormulario(viewModel);

		await Should.NotThrowAsync(async () => await viewModel.ContinuarCommand.ExecuteAsync(null));

		contexto.Dialogos.Errores.ShouldHaveSingleItem();
		contexto.Dialogos.Errores[0].Mensaje.ShouldBe("Verificar el legajo del alumno: legajo.");
		viewModel.Tab.ShouldBe(0);
	}

	[Theory]
	[Trait("Categoria", "Unidad")]
	[InlineData("Sexo")]
	[InlineData("Vivienda")]
	public async Task Sin_elegir_en_los_combos_avisa_de_campos_incompletos_y_no_revienta(string combo)
	{
		// Sexo y Vivienda salen de combos que arrancan sin seleccion, asi que quedan en null. La
		// guarda original no los miraba y mas abajo se les llama .ToString(): el resultado era un
		// NullReferenceException, no el aviso de campos incompletos. Con async void ademas se perdia.
		var contexto = new Contexto();
		var viewModel = contexto.CrearRegistro();
		CompletarFormulario(viewModel);
		if (combo == "Sexo")
		{
			viewModel.InformacionPersonal.Sexo = null!;
		}
		else
		{
			viewModel.Domicilio.Vivienda = null!;
		}

		await Should.NotThrowAsync(async () => await viewModel.ContinuarCommand.ExecuteAsync(null));

		contexto.Dialogos.Advertencias.ShouldHaveSingleItem();
		contexto.Dialogos.Advertencias[0].Mensaje.ShouldContain(combo == "Sexo" ? "sexo" : "tipo de vivienda");
		await contexto.ServicioAlumnos.DidNotReceive().RegistrarAlumnoAsync(Arg.Any<RegistrarAlumnoRequest>());
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Continuar_se_reevalua_cuando_cambian_los_errores_de_un_sub_ViewModel()
	{
		// CanExecuteContinuarCommand lee HasErrors de los tres sub-ViewModels. Sin la suscripción a
		// ErrorsChanged el botón conservaba el estado de la última reasignación de la propiedad.
		var contexto = new Contexto();
		var viewModel = contexto.CrearRegistro();
		var avisos = 0;
		viewModel.ContinuarCommand.CanExecuteChanged += (_, _) => avisos++;

		viewModel.Contacto.Email = "no-es-un-email";

		avisos.ShouldBeGreaterThan(0);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void RegistrarAlumno_no_usa_MessageBox_ni_async_void_ni_construye_requests_que_descarta()
	{
		var fuente = RaizDelRepositorio.Leer("WPF_Desktop/ViewModels/Alumnos/RegistrarAlumnoViewModel.cs");

		// La guarda mira el código y no los comentarios. El <remarks> del paso 1 explica que ANTES
		// era "async void", que es información que conviene conservar: sin este filtro, documentar
		// el defecto corregido hace saltar la guarda que lo prohíbe.
		var codigo = string.Join("\n", fuente.Split('\n').Where(linea => !linea.TrimStart().StartsWith("//")));

		// "MessageBox.Show" y no "MessageBox": ShouldNotContain ignora mayusculas y chocaria con la variable local messageBoxText.
		codigo.ShouldNotContain("MessageBox.Show");
		codigo.ShouldNotContain("async void");
		codigo.ShouldNotContain("new RegistrarDomicilioRequest(");
		codigo.ShouldNotContain("new RegistrarContactoRequest(");
		codigo.ShouldNotContain("Registrar un Docente");
	}
	#endregion
}
