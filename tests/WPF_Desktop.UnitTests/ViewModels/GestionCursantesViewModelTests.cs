using CommunityToolkit.Mvvm.Input;
using Core.ServicioAsistencias;
using Core.ServicioAsistencias.DTOs.Requests;
using Core.ServicioCurriculas;
using Core.ServicioCurriculas.DTOs.Requests;
using Core.ServicioCurriculas.DTOs.Responses;
using Core.ServicioCursantes;
using Core.ServicioCursantes.DTOs.Requests;
using Core.ServicioCursantes.DTOs.Responses;
using Core.Shared.DTOs.Cursadas;
using Core.ServicioCursos.DTOs.Responses;
using Core.ServicioDivisiones;
using Core.ServicioDivisiones.DTOs.Requests;
using Core.ServicioDivisiones.DTOs.Responses;
using Core.ServicioMaterias;
using Core.ServicioMaterias.DTOs.Requests;
using Core.ServicioMaterias.DTOs.Responses;
using Domain.Asistencias;
using Domain.Cursos;
using Domain.Shared;
using NSubstitute;
using Shouldly;
using System.Reflection;
using System.Text.RegularExpressions;
using WPF_Desktop.Store;
using WPF_Desktop.UnitTests.Dobles;
using WPF_Desktop.ViewModels.Cursos;
using WPF_Desktop.ViewModels.Cursos.Divisiones;
using Xunit;

namespace WPF_Desktop.UnitTests.ViewModels;

/// <summary>
/// Cubre <see cref="GestionCursantesViewModel"/> tras W5.C: <c>UI-02</c> (la carga de materias pide la curricula con
/// <c>FechaFin is null</c> y nunca <see cref="Guid.Empty"/>; <c>BuscarCursantes</c> llega a
/// <see cref="IServicioCursante.ListarCursantesAsync"/>), las calificaciones conectadas a Core, la ausencia de un comando
/// de "editar nota", el periodo del listado como periodo de los requests y las notificaciones de <c>CanExecute</c>.
/// </summary>
public class GestionCursantesViewModelTests
{
	private const string CicloDelStore = "2024";

	private sealed class Contexto
	{
		public IServicioCursante ServicioCursante { get; } = Substitute.For<IServicioCursante>();
		public IServicioMateria ServicioMateria { get; } = Substitute.For<IServicioMateria>();
		public IServicioCurricula ServicioCurricula { get; } = Substitute.For<IServicioCurricula>();

		/// <summary>UI-CUR-1: la pantalla lista las divisiones del curso para poblar el destino del traslado.</summary>
		public IServicioDivision ServicioDivision { get; } = Substitute.For<IServicioDivision>();

		/// <summary>
		/// La vista de asistencias del alumno. Cuenta y no lista porque
		/// <c>ContarFaltasAsync(cursanteID, tipo)</c> es la única consulta de asistencias que
		/// acepta un cursante: el agregado es la planilla, que es de la división y de una fecha.
		/// </summary>
		public IServicioAsistencia ServicioAsistencia { get; } = Substitute.For<IServicioAsistencia>();
		public DialogServiceFalso Dialogos { get; } = new();

		/// <summary>Salida de la pantalla: la ruta vuelve a las divisiones del curso.</summary>
		public NavegacionFalsa NavegarADivisiones { get; } = new();
		public CursoStore CursoStore { get; } = new();
		public DivisionStore DivisionStore { get; } = new();
		public CicloLectivoStore CicloLectivoStore { get; } = new();
		public Guid CursoID { get; } = Guid.NewGuid();
		public Guid DivisionID { get; } = Guid.NewGuid();
		public Guid CurriculaVigenteID { get; } = Guid.NewGuid();
		public Guid CurriculaCerradaID { get; } = Guid.NewGuid();
		public GestionCursantesViewModel ViewModel { get; }

		public Contexto(bool conDivision = true)
		{
			CursoStore.Curso = new CursoSeleccionado(CursoID, Grado.Primero.ToString(), NivelEducativo.Secundaria.ToString());
			CicloLectivoStore.CicloLectivo = CicloDelStore;

			if (conDivision)
			{
				DivisionStore.Division = new DivisionSeleccionada(DivisionID);
			}

			DevolverCurriculas(new CurriculaResponse(CursoID, CurriculaCerradaID, new DateTime(2020, 3, 1), new DateTime(2023, 12, 31), Vigente: false),
							   new CurriculaResponse(CursoID, CurriculaVigenteID, new DateTime(2024, 3, 1), null, Vigente: true));
			DevolverMaterias(CrearMateria("Matematica"), CrearMateria("Lengua"));
			DevolverCursantes();
			DevolverCalificaciones();
			DevolverDivisiones();
			DevolverHistorial();

			ViewModel = new GestionCursantesViewModel(ServicioCursante,
													  ServicioMateria,
													  ServicioCurricula,
													  ServicioDivision,
													  ServicioAsistencia,
													  Dialogos,
													  NavegarADivisiones,
													  CursoStore,
													  DivisionStore,
													  CicloLectivoStore);
		}

		public void DevolverDivisiones(params DivisionResponse[] divisiones)
		{
			ServicioDivision.ListarDivisionesAsync(Arg.Any<ListarDivisionesRequest>())
							.Returns(Task.FromResult<IReadOnlyCollection<DivisionResponse>>(divisiones.ToList()));
		}

		/// <summary>El conteo que devuelve el servicio para un tipo de asistencia.</summary>
		public void DevolverConteo(TipoAsistencia tipo, int cantidad)
		{
			ServicioAsistencia.ContarFaltasAsync(Arg.Is<ContarFaltasRequest>(r => r.Tipo == tipo))
							  .Returns(Task.FromResult(cantidad));
		}

		public void DevolverHistorial(params InscripcionHistoricaResponse[] inscripciones)
		{
			ServicioCursante.ConsultarHistorialDeDivisionesAsync(Arg.Any<ConsultarHistorialRequest>())
							.Returns(Task.FromResult<IReadOnlyCollection<InscripcionHistoricaResponse>>(inscripciones.ToList()));
		}

		public MateriaResponse CrearMateria(string descripcion)
		{
			return new MateriaResponse(CursoID, CurriculaVigenteID, Guid.NewGuid(), descripcion, 3);
		}

		public void DevolverCurriculas(params CurriculaResponse[] curriculas)
		{
			ServicioCurricula.ListarCurriculasSegunCursoAsync(Arg.Any<CursoRequest>())
							 .Returns(Task.FromResult<IReadOnlyCollection<CurriculaResponse>>(curriculas.ToList()));
		}

		public void DevolverMaterias(params MateriaResponse[] materias)
		{
			ServicioMateria.ListarMateriasSegunCurriculaAsync(Arg.Any<ListarMateriasSegunCurriculaRequest>())
						   .Returns(Task.FromResult<IReadOnlyCollection<MateriaResponse>>(materias.ToList()));
		}

		public void DevolverCursantes(params CursanteResponse[] cursantes)
		{
			ServicioCursante.ListarCursantesAsync(Arg.Any<BuscarListadoRequest>())
							.Returns(Task.FromResult<IReadOnlyCollection<CursanteResponse>>(cursantes.ToList()));
		}

		public void DevolverCalificaciones(params CalificacionResponse[] calificaciones)
		{
			ServicioCursante.ListarCalificacionesAsync(Arg.Any<ListarCalificacionesRequest>())
							.Returns(Task.FromResult<IReadOnlyCollection<CalificacionResponse>>(calificaciones.ToList()));
		}

		/// <summary>Busca el listado con el ciclo lectivo del store y selecciona al primer cursante.</summary>
		public async Task<CursanteResponse> BuscarYSeleccionarAsync()
		{
			var cursante = new CursanteResponse(Guid.NewGuid(), Guid.NewGuid(), "Perez, Ana", "30111222", 15, false);
			DevolverCursantes(cursante);

			await ViewModel.BuscarCommandAsync.ExecuteAsync(null);
			ViewModel.Cursante = ViewModel.Cursantes[0];

			return cursante;
		}

		/// <summary>Deja abierto el formulario de alta con una materia elegida.</summary>
		public async Task<CursanteResponse> PrepararAltaAsync()
		{
			await ViewModel.CargarCommandAsync.ExecuteAsync(null);
			var cursante = await BuscarYSeleccionarAsync();

			ViewModel.NuevaCalificacionCommand.Execute(null);
			ViewModel.Materia = ViewModel.Materias[0];

			return cursante;
		}
	}

	private static int ContarNotificaciones(IRelayCommand comando, Action accion)
	{
		var veces = 0;
		comando.CanExecuteChanged += (_, _) => veces++;
		accion();
		return veces;
	}

	#region Construccion
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_viewmodel_se_puede_construir()
	{
		// El ctor asigna CicloLectivo (un [ObservableProperty] con [NotifyCanExecuteChangedFor(BuscarCommandAsync)]) ANTES
		// de crear los comandos: el setter generado llama a BuscarCommandAsync.NotifyCanExecuteChanged() y, si el comando
		// todavia es null, lanza NullReferenceException.
		GestionCursantesViewModel? viewModel = null;

		Should.NotThrow(() => { viewModel = new Contexto().ViewModel; });

		viewModel.ShouldNotBeNull();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_ciclo_lectivo_arranca_con_el_del_store()
	{
		var contexto = new Contexto();

		contexto.ViewModel.CicloLectivo.ShouldBe(CicloDelStore);
		contexto.CicloLectivoStore.CicloLectivo.ShouldBe(CicloDelStore);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Todos_los_comandos_estan_creados_tras_construir()
	{
		var vm = new Contexto().ViewModel;

		vm.CargarCommandAsync.ShouldNotBeNull();
		vm.BuscarCommandAsync.ShouldNotBeNull();
		vm.CargarCalificacionesCommandAsync.ShouldNotBeNull();
		vm.GuardarCalificacionCommandAsync.ShouldNotBeNull();
		vm.RegistrarInasistenciaCommandAsync.ShouldNotBeNull();
		vm.QuitarCalificacionCommandAsync.ShouldNotBeNull();
		vm.ModificarObservacionCommandAsync.ShouldNotBeNull();
		vm.NuevaCalificacionCommand.ShouldNotBeNull();
		vm.CancelarCalificacionCommand.ShouldNotBeNull();
	}
	#endregion

	#region UI-02, primera mitad: carga de materias
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_pide_las_curriculas_del_curso_del_store()
	{
		var contexto = new Contexto();

		await contexto.ViewModel.CargarCommandAsync.ExecuteAsync(null);

		_ = contexto.ServicioCurricula.Received(1).ListarCurriculasSegunCursoAsync(new CursoRequest(contexto.CursoID));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_pide_las_materias_de_la_curricula_vigente_con_FechaFin_nula()
	{
		var contexto = new Contexto();

		await contexto.ViewModel.CargarCommandAsync.ExecuteAsync(null);

		_ = contexto.ServicioMateria.Received(1).ListarMateriasSegunCurriculaAsync(new ListarMateriasSegunCurriculaRequest(contexto.CursoID, contexto.CurriculaVigenteID));
		_ = contexto.ServicioMateria.DidNotReceive().ListarMateriasSegunCurriculaAsync(Arg.Is<ListarMateriasSegunCurriculaRequest>(r => r.CurriculaID == contexto.CurriculaCerradaID));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_nunca_pide_la_curricula_Guid_Empty()
	{
		var contexto = new Contexto();

		await contexto.ViewModel.CargarCommandAsync.ExecuteAsync(null);

		_ = contexto.ServicioMateria.DidNotReceive().ListarMateriasSegunCurriculaAsync(Arg.Is<ListarMateriasSegunCurriculaRequest>(r => r.CurriculaID == Guid.Empty));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_elige_la_vigente_aunque_venga_primera_en_la_lista()
	{
		var contexto = new Contexto();
		contexto.DevolverCurriculas(new CurriculaResponse(contexto.CursoID, contexto.CurriculaVigenteID, new DateTime(2024, 3, 1), null, Vigente: true),
									new CurriculaResponse(contexto.CursoID, contexto.CurriculaCerradaID, new DateTime(2020, 3, 1), new DateTime(2023, 12, 31), Vigente: false));

		await contexto.ViewModel.CargarCommandAsync.ExecuteAsync(null);

		_ = contexto.ServicioMateria.Received(1).ListarMateriasSegunCurriculaAsync(Arg.Is<ListarMateriasSegunCurriculaRequest>(r => r.CurriculaID == contexto.CurriculaVigenteID));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Despues_de_cargar_Materias_no_esta_vacia_y_refleja_las_del_servicio()
	{
		var contexto = new Contexto();

		await contexto.ViewModel.CargarCommandAsync.ExecuteAsync(null);

		contexto.ViewModel.Materias.ShouldNotBeEmpty();
		contexto.ViewModel.Materias.Select(m => m.Descripcion).ShouldBe(new[] { "Matematica", "Lengua" });
		contexto.Dialogos.Errores.ShouldBeEmpty();
		contexto.Dialogos.Advertencias.ShouldBeEmpty();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Sin_curricula_vigente_Materias_queda_vacia_se_advierte_y_no_se_piden_materias()
	{
		var contexto = new Contexto();
		contexto.DevolverCurriculas(new CurriculaResponse(contexto.CursoID, contexto.CurriculaCerradaID, new DateTime(2020, 3, 1), new DateTime(2023, 12, 31), Vigente: false));

		await contexto.ViewModel.CargarCommandAsync.ExecuteAsync(null);

		contexto.ViewModel.Materias.ShouldBeEmpty();
		contexto.Dialogos.Advertencias.Count.ShouldBe(1);
		_ = contexto.ServicioMateria.DidNotReceiveWithAnyArgs().ListarMateriasSegunCurriculaAsync(default!);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Si_el_servicio_de_curriculas_falla_se_muestra_el_error_y_no_lanza()
	{
		var contexto = new Contexto();
		contexto.ServicioCurricula.ListarCurriculasSegunCursoAsync(Arg.Any<CursoRequest>())
								  .Returns(Task.FromException<IReadOnlyCollection<CurriculaResponse>>(new InvalidOperationException("sin conexion")));

		await contexto.ViewModel.CargarCommandAsync.ExecuteAsync(null);

		contexto.Dialogos.Errores.Single().Mensaje.ShouldBe("sin conexion");
	}
	#endregion

	#region UI-02, segunda mitad: BuscarCursantes llega a Core
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Buscar_llega_a_ListarCursantesAsync_con_curso_division_y_ciclo()
	{
		var contexto = new Contexto();

		await contexto.ViewModel.BuscarCommandAsync.ExecuteAsync(null);

		_ = contexto.ServicioCursante.Received(1).ListarCursantesAsync(new BuscarListadoRequest(contexto.CursoID, contexto.DivisionID, CicloDelStore));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Buscar_devuelve_los_cursantes_del_servicio_y_no_una_lista_vacia_hardcodeada()
	{
		var contexto = new Contexto();
		contexto.DevolverCursantes(new CursanteResponse(Guid.NewGuid(), Guid.NewGuid(), "Perez, Ana", "30111222", 15, false),
								   new CursanteResponse(Guid.NewGuid(), Guid.NewGuid(), "Gomez, Luis", "30333444", 16, true));

		await contexto.ViewModel.BuscarCommandAsync.ExecuteAsync(null);

		contexto.ViewModel.Cursantes.Count.ShouldBe(2);
		contexto.ViewModel.Cursantes.Select(c => c.NombreCompleto).ShouldBe(new[] { "Perez, Ana", "Gomez, Luis" });
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Buscar_usa_el_ciclo_lectivo_escrito_por_el_usuario()
	{
		var contexto = new Contexto();
		contexto.ViewModel.CicloLectivo = "2022";

		await contexto.ViewModel.BuscarCommandAsync.ExecuteAsync(null);

		_ = contexto.ServicioCursante.Received(1).ListarCursantesAsync(new BuscarListadoRequest(contexto.CursoID, contexto.DivisionID, "2022"));
		contexto.CicloLectivoStore.CicloLectivo.ShouldBe("2022");
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Buscar_sin_division_en_el_store_advierte_y_no_llama_al_servicio()
	{
		var contexto = new Contexto(conDivision: false);

		await contexto.ViewModel.BuscarCommandAsync.ExecuteAsync(null);

		contexto.Dialogos.Advertencias.Count.ShouldBe(1);
		_ = contexto.ServicioCursante.DidNotReceiveWithAnyArgs().ListarCursantesAsync(default!);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Un_ciclo_con_formato_invalido_lo_informa_el_dominio_y_la_UI_solo_lo_muestra_como_advertencia()
	{
		var contexto = new Contexto();
		contexto.ServicioCursante.ListarCursantesAsync(Arg.Any<BuscarListadoRequest>())
								 .Returns(Task.FromException<IReadOnlyCollection<CursanteResponse>>(new FormatException("ciclo invalido")));
		contexto.ViewModel.CicloLectivo = "abc";

		await contexto.ViewModel.BuscarCommandAsync.ExecuteAsync(null);

		contexto.Dialogos.Advertencias.Single().Mensaje.ShouldBe("ciclo invalido");
		contexto.Dialogos.Errores.ShouldBeEmpty();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Un_error_inesperado_al_buscar_se_muestra_como_error()
	{
		var contexto = new Contexto();
		contexto.ServicioCursante.ListarCursantesAsync(Arg.Any<BuscarListadoRequest>())
								 .Returns(Task.FromException<IReadOnlyCollection<CursanteResponse>>(new InvalidOperationException("caida")));

		await contexto.ViewModel.BuscarCommandAsync.ExecuteAsync(null);

		contexto.Dialogos.Errores.Single().Mensaje.ShouldBe("caida");
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void La_UI_no_duplica_la_validacion_del_formato_del_ciclo_lectivo()
	{
		var codigo = RaizDelRepositorio.Leer("WPF_Desktop/ViewModels/Cursos/Divisiones/GestionCursantesViewModel.cs");

		codigo.ShouldNotContain("^(20)");
		codigo.ShouldNotContain("Regex");
	}
	#endregion

	#region Calificaciones: alta, inasistencia, quitar, observacion
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Seleccionar_un_cursante_carga_sus_calificaciones_con_el_periodo_del_listado()
	{
		var contexto = new Contexto();
		var calificacion = new CalificacionResponse(Guid.NewGuid(), Guid.NewGuid(), "Matematica", new DateTime(2024, 5, 2), "Parcial", true, 7, true, "ok");
		contexto.DevolverCalificaciones(calificacion);

		var cursante = await contexto.BuscarYSeleccionarAsync();

		_ = contexto.ServicioCursante.Received().ListarCalificacionesAsync(new ListarCalificacionesRequest(cursante.AlumnoID, CicloDelStore));
		contexto.ViewModel.Calificaciones.Count.ShouldBe(1);
		contexto.ViewModel.Calificaciones[0].CalificacionID.ShouldBe(calificacion.CalificacionID);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_Nota_abre_el_formulario_con_Parcial_por_defecto()
	{
		var contexto = new Contexto();
		await contexto.BuscarYSeleccionarAsync();

		contexto.ViewModel.NuevaCalificacionCommand.Execute(null);

		contexto.ViewModel.MostrarCalificacionView.ShouldBeTrue();
		contexto.ViewModel.Calificacion.ShouldNotBeNull();
		contexto.ViewModel.Calificacion!.Instancia.ShouldBe("Parcial");
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Guardar_nota_llama_a_RegistrarCalificacionAsync_con_todos_los_campos()
	{
		var contexto = new Contexto();
		var cursante = await contexto.PrepararAltaAsync();
		var materia = contexto.ViewModel.Materias[0];
		contexto.ViewModel.Calificacion!.Nota = 8;
		contexto.ViewModel.Calificacion.Observacion = "bien";
		var fecha = contexto.ViewModel.Calificacion.Fecha;

		await contexto.ViewModel.GuardarCalificacionCommandAsync.ExecuteAsync(null);

		_ = contexto.ServicioCursante.Received(1).RegistrarCalificacionAsync(new CrearCalificationRequest(cursante.AlumnoID, CicloDelStore, materia.MateriaID, fecha, "Parcial", 8, "bien"));
		contexto.Dialogos.Informaciones.Count.ShouldBe(1);
		contexto.ViewModel.MostrarCalificacionView.ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Guardar_nota_sin_nota_advierte_y_no_llega_al_servicio()
	{
		var contexto = new Contexto();
		await contexto.PrepararAltaAsync();
		contexto.ViewModel.Calificacion!.Nota = null;

		await contexto.ViewModel.GuardarCalificacionCommandAsync.ExecuteAsync(null);

		contexto.Dialogos.Advertencias.Count.ShouldBe(1);
		_ = contexto.ServicioCursante.DidNotReceiveWithAnyArgs().RegistrarCalificacionAsync(default!);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Guardar_nota_sin_instancia_advierte_y_no_llega_al_servicio()
	{
		var contexto = new Contexto();
		await contexto.PrepararAltaAsync();
		contexto.ViewModel.Calificacion!.Nota = 6;
		contexto.ViewModel.Calificacion.Instancia = string.Empty;

		await contexto.ViewModel.GuardarCalificacionCommandAsync.ExecuteAsync(null);

		contexto.Dialogos.Advertencias.Count.ShouldBe(1);
		_ = contexto.ServicioCursante.DidNotReceiveWithAnyArgs().RegistrarCalificacionAsync(default!);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Un_error_del_servicio_al_guardar_nota_se_muestra_y_el_formulario_sigue_abierto()
	{
		var contexto = new Contexto();
		await contexto.PrepararAltaAsync();
		contexto.ViewModel.Calificacion!.Nota = 6;
		contexto.ServicioCursante.RegistrarCalificacionAsync(Arg.Any<CrearCalificationRequest>())
								 .Returns(Task.FromException<Guid>(new InvalidOperationException("nota repetida")));

		await contexto.ViewModel.GuardarCalificacionCommandAsync.ExecuteAsync(null);

		contexto.Dialogos.Errores.Single().Mensaje.ShouldBe("nota repetida");
		contexto.ViewModel.MostrarCalificacionView.ShouldBeTrue();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Registrar_inasistencia_llama_a_RegistrarInasistenciaAExamenAsync()
	{
		var contexto = new Contexto();
		var cursante = await contexto.PrepararAltaAsync();
		var materia = contexto.ViewModel.Materias[0];
		contexto.ViewModel.Calificacion!.Observacion = "no vino";
		var fecha = contexto.ViewModel.Calificacion.Fecha;

		await contexto.ViewModel.RegistrarInasistenciaCommandAsync.ExecuteAsync(null);

		_ = contexto.ServicioCursante.Received(1).RegistrarInasistenciaAExamenAsync(new RegistrarInasistenciaRequest(cursante.AlumnoID, CicloDelStore, materia.MateriaID, fecha, "Parcial", "no vino"));
		_ = contexto.ServicioCursante.DidNotReceiveWithAnyArgs().RegistrarCalificacionAsync(default!);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Quitar_calificacion_confirmada_llama_a_QuitarCalificacionAsync()
	{
		var contexto = new Contexto();
		var calificacion = new CalificacionResponse(Guid.NewGuid(), Guid.NewGuid(), "Matematica", DateTime.Today, "Parcial", true, 4, false, null);
		contexto.DevolverCalificaciones(calificacion);
		var cursante = await contexto.BuscarYSeleccionarAsync();
		contexto.ViewModel.CalificacionSeleccionada = contexto.ViewModel.Calificaciones[0];

		await contexto.ViewModel.QuitarCalificacionCommandAsync.ExecuteAsync(null);

		_ = contexto.ServicioCursante.Received(1).QuitarCalificacionAsync(new EliminarCalificacionRequest(cursante.AlumnoID, CicloDelStore, calificacion.CalificacionID));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Quitar_calificacion_no_confirmada_no_llega_al_servicio()
	{
		var contexto = new Contexto();
		contexto.DevolverCalificaciones(new CalificacionResponse(Guid.NewGuid(), Guid.NewGuid(), "Matematica", DateTime.Today, "Parcial", true, 4, false, null));
		await contexto.BuscarYSeleccionarAsync();
		contexto.ViewModel.CalificacionSeleccionada = contexto.ViewModel.Calificaciones[0];
		contexto.Dialogos.RespuestaDeConfirmacion = false;

		await contexto.ViewModel.QuitarCalificacionCommandAsync.ExecuteAsync(null);

		_ = contexto.ServicioCursante.DidNotReceiveWithAnyArgs().QuitarCalificacionAsync(default!);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Seleccionar_una_calificacion_copia_su_observacion_al_campo_editable()
	{
		var contexto = new Contexto();
		contexto.DevolverCalificaciones(new CalificacionResponse(Guid.NewGuid(), Guid.NewGuid(), "Matematica", DateTime.Today, "Parcial", true, 4, false, "original"));
		await contexto.BuscarYSeleccionarAsync();

		contexto.ViewModel.CalificacionSeleccionada = contexto.ViewModel.Calificaciones[0];

		contexto.ViewModel.ObservacionEdicion.ShouldBe("original");
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Editar_observacion_envia_la_observacion_nueva_de_la_calificacion_seleccionada()
	{
		var contexto = new Contexto();
		var calificacion = new CalificacionResponse(Guid.NewGuid(), Guid.NewGuid(), "Matematica", DateTime.Today, "Parcial", true, 4, false, "original");
		contexto.DevolverCalificaciones(calificacion);
		var cursante = await contexto.BuscarYSeleccionarAsync();
		contexto.ViewModel.CalificacionSeleccionada = contexto.ViewModel.Calificaciones[0];
		contexto.ViewModel.ObservacionEdicion = "corregida";

		await contexto.ViewModel.ModificarObservacionCommandAsync.ExecuteAsync(null);

		_ = contexto.ServicioCursante.Received(1).ModificarObservacionCalificacionAsync(new ModificarObservacionCalificacionRequest(cursante.AlumnoID, CicloDelStore, calificacion.CalificacionID, "corregida"));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Los_requests_usan_el_periodo_del_listado_y_no_el_texto_que_el_usuario_edito_despues()
	{
		var contexto = new Contexto();
		var calificacion = new CalificacionResponse(Guid.NewGuid(), Guid.NewGuid(), "Matematica", DateTime.Today, "Parcial", true, 4, false, null);
		contexto.DevolverCalificaciones(calificacion);
		await contexto.BuscarYSeleccionarAsync();
		contexto.ViewModel.CalificacionSeleccionada = contexto.ViewModel.Calificaciones[0];
		contexto.ViewModel.CicloLectivo = "1999"; // el usuario tipea otro ciclo pero no vuelve a buscar

		await contexto.ViewModel.QuitarCalificacionCommandAsync.ExecuteAsync(null);

		_ = contexto.ServicioCursante.Received(1).QuitarCalificacionAsync(Arg.Is<EliminarCalificacionRequest>(r => r.Periodo == CicloDelStore));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cambiar_de_cursante_cierra_el_formulario_y_limpia_la_seleccion()
	{
		var contexto = new Contexto();
		await contexto.PrepararAltaAsync();
		contexto.ViewModel.MostrarCalificacionView.ShouldBeTrue();

		contexto.ViewModel.Cursante = null;

		contexto.ViewModel.MostrarCalificacionView.ShouldBeFalse();
		contexto.ViewModel.Calificacion.ShouldBeNull();
		contexto.ViewModel.CalificacionSeleccionada.ShouldBeNull();
	}
	#endregion

	#region No existe "editar nota"
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void No_existe_ningun_comando_ni_metodo_publico_para_editar_o_corregir_una_nota()
	{
		var patron = new Regex("(Editar|Modificar|Corregir|Actualizar|Cambiar).*Nota|Nota.*(Editar|Modificar|Corregir|Actualizar|Cambiar)", RegexOptions.IgnoreCase);

		var miembros = typeof(GestionCursantesViewModel)
			.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
			.Select(m => m.Name)
			.Where(nombre => patron.IsMatch(nombre))
			.ToList();

		miembros.ShouldBeEmpty();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Los_unicos_comandos_de_modificacion_son_la_observacion_y_la_condicion_de_recursante()
	{
		// La lista es cerrada a propósito y cada entrada corresponde a una corrección que el
		// dominio realmente ofrece: la observación de una calificación (lo único editable de una
		// nota) y la condición de recursante (lo único editable de una inscripción, CUR-2).
		// Lo que NO se puede modificar sigue sin comando: la nota, y la fecha de inicio o la
		// división de una inscripción, porque la baja la cierra y el traslado la cierra y abre otra
		// (de ahí que se llamen RegistrarBaja y CambiarDivision, y no "ModificarInscripcion").
		// Un "Modificar*" nuevo acá exige poder nombrar el método de dominio que lo respalda.
		var comandosDeModificacion = typeof(GestionCursantesViewModel)
			.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(p => typeof(IRelayCommand).IsAssignableFrom(p.PropertyType))
			.Select(p => p.Name)
			.Where(n => n.Contains("Modificar") || n.Contains("Editar"))
			.ToList();

		comandosDeModificacion.ShouldBe(new[]
		{
			nameof(GestionCursantesViewModel.ModificarObservacionCommandAsync),
			nameof(GestionCursantesViewModel.ModificarRecursanteCommandAsync)
		});
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_servicio_de_cursantes_tampoco_ofrece_corregir_una_nota()
	{
		var metodos = typeof(IServicioCursante).GetMethods().Select(m => m.Name).ToList();

		metodos.ShouldContain("ModificarObservacionCalificacionAsync");
		metodos.Where(n => Regex.IsMatch(n, "(Modificar|Corregir|Editar).*Nota", RegexOptions.IgnoreCase)).ShouldBeEmpty();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_XAML_ofrece_Editar_Observacion_y_no_Editar_Nota()
	{
		var xaml = RaizDelRepositorio.Leer("WPF_Desktop/Views/Cursos/Divisiones/GestionCursantesView.xaml");

		xaml.ShouldContain("Editar Observación");
		Regex.IsMatch(xaml, "Content=\"(Editar|Modificar|Corregir)[^\"]*Nota", RegexOptions.IgnoreCase).ShouldBeFalse();
	}
	#endregion

	#region [NotifyCanExecuteChangedFor]
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void CicloLectivo_notifica_a_Buscar_y_Buscar_se_deshabilita_sin_texto()
	{
		var contexto = new Contexto();
		var vm = contexto.ViewModel;

		vm.BuscarCommandAsync.CanExecute(null).ShouldBeTrue();
		ContarNotificaciones(vm.BuscarCommandAsync, () => { vm.CicloLectivo = string.Empty; }).ShouldBeGreaterThan(0);
		vm.BuscarCommandAsync.CanExecute(null).ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cursante_notifica_a_Nueva_calificacion_e_Inasistencia()
	{
		var contexto = new Contexto();
		await contexto.ViewModel.BuscarCommandAsync.ExecuteAsync(null);
		var cursante = new CursanteViewModel(new CursanteResponse(Guid.NewGuid(), Guid.NewGuid(), "Perez, Ana", "30111222", 15, false));

		ContarNotificaciones(contexto.ViewModel.NuevaCalificacionCommand, () => { contexto.ViewModel.Cursante = cursante; }).ShouldBeGreaterThan(0);
		contexto.ViewModel.Cursante = null;
		ContarNotificaciones(contexto.ViewModel.RegistrarInasistenciaCommandAsync, () => { contexto.ViewModel.Cursante = cursante; }).ShouldBeGreaterThan(0);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Materia_notifica_a_Guardar_nota_e_Inasistencia()
	{
		var contexto = new Contexto();
		await contexto.ViewModel.CargarCommandAsync.ExecuteAsync(null);
		var materia = contexto.ViewModel.Materias[0];

		ContarNotificaciones(contexto.ViewModel.GuardarCalificacionCommandAsync, () => { contexto.ViewModel.Materia = materia; }).ShouldBeGreaterThan(0);
		contexto.ViewModel.Materia = null;
		ContarNotificaciones(contexto.ViewModel.RegistrarInasistenciaCommandAsync, () => { contexto.ViewModel.Materia = materia; }).ShouldBeGreaterThan(0);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task MostrarCalificacionView_notifica_a_Guardar_Cancelar_e_Inasistencia()
	{
		var contexto = new Contexto();
		await contexto.BuscarYSeleccionarAsync();
		var vm = contexto.ViewModel;
		var comandos = new IRelayCommand[] { vm.GuardarCalificacionCommandAsync, vm.CancelarCalificacionCommand, vm.RegistrarInasistenciaCommandAsync };

		foreach (var comando in comandos)
		{
			vm.MostrarCalificacionView = false;
			ContarNotificaciones(comando, () => { vm.MostrarCalificacionView = true; }).ShouldBeGreaterThan(0);
		}
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task CalificacionSeleccionada_notifica_a_Quitar_y_Modificar_observacion()
	{
		var contexto = new Contexto();
		contexto.DevolverCalificaciones(new CalificacionResponse(Guid.NewGuid(), Guid.NewGuid(), "Matematica", DateTime.Today, "Parcial", true, 4, false, null));
		await contexto.BuscarYSeleccionarAsync();
		var vm = contexto.ViewModel;
		var calificacion = vm.Calificaciones[0];

		ContarNotificaciones(vm.QuitarCalificacionCommandAsync, () => { vm.CalificacionSeleccionada = calificacion; }).ShouldBeGreaterThan(0);
		vm.CalificacionSeleccionada = null;
		ContarNotificaciones(vm.ModificarObservacionCommandAsync, () => { vm.CalificacionSeleccionada = calificacion; }).ShouldBeGreaterThan(0);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Guardar_nota_solo_se_habilita_con_formulario_cursante_y_materia()
	{
		var contexto = new Contexto();
		await contexto.ViewModel.CargarCommandAsync.ExecuteAsync(null);
		var vm = contexto.ViewModel;
		vm.GuardarCalificacionCommandAsync.CanExecute(null).ShouldBeFalse();

		await contexto.BuscarYSeleccionarAsync();
		vm.NuevaCalificacionCommand.Execute(null);
		vm.GuardarCalificacionCommandAsync.CanExecute(null).ShouldBeFalse(); // todavia sin materia

		vm.Materia = vm.Materias[0];
		vm.GuardarCalificacionCommandAsync.CanExecute(null).ShouldBeTrue();

		vm.CancelarCalificacionCommand.Execute(null);
		vm.GuardarCalificacionCommandAsync.CanExecute(null).ShouldBeFalse();
	}
	#endregion

	#region UI-CUR-1: recursante, baja, traslado e historial
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Los_cuatro_comandos_de_inscripcion_exigen_un_alumno_elegido()
	{
		var contexto = new Contexto();
		var vm = contexto.ViewModel;

		vm.ModificarRecursanteCommandAsync.CanExecute(null).ShouldBeFalse();
		vm.RegistrarBajaCommandAsync.CanExecute(null).ShouldBeFalse();
		vm.CambiarDivisionCommandAsync.CanExecute(null).ShouldBeFalse();
		vm.CargarHistorialCommandAsync.CanExecute(null).ShouldBeFalse();

		await contexto.BuscarYSeleccionarAsync();

		vm.ModificarRecursanteCommandAsync.CanExecute(null).ShouldBeTrue();
		vm.RegistrarBajaCommandAsync.CanExecute(null).ShouldBeTrue();
		vm.CargarHistorialCommandAsync.CanExecute(null).ShouldBeTrue();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Elegir_un_alumno_trae_su_condicion_de_recursante_al_formulario()
	{
		var contexto = new Contexto();
		contexto.DevolverCursantes(new CursanteResponse(Guid.NewGuid(), Guid.NewGuid(), "Perez, Ana", "30111222", 15, true));
		await contexto.ViewModel.BuscarCommandAsync.ExecuteAsync(null);

		contexto.ViewModel.Cursante = contexto.ViewModel.Cursantes[0];

		contexto.ViewModel.EsRecursante.ShouldBeTrue();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Modificar_la_condicion_de_recursante_llega_al_servicio_con_el_cursante_elegido()
	{
		var contexto = new Contexto();
		var cursante = await contexto.BuscarYSeleccionarAsync();
		contexto.ViewModel.EsRecursante = true;

		await contexto.ViewModel.ModificarRecursanteCommandAsync.ExecuteAsync(null);

		_ = contexto.ServicioCursante.Received(1).ModificarCondicionDeRecursanteAsync(new ModificarCondicionRecursanteRequest(cursante.CursanteID, true));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Dar_de_baja_llega_al_servicio_con_la_fecha_que_eligio_el_usuario()
	{
		var contexto = new Contexto();
		var cursante = await contexto.BuscarYSeleccionarAsync();
		var baja = DateTime.Today.AddDays(-10);
		contexto.ViewModel.FechaBaja = baja;

		await contexto.ViewModel.RegistrarBajaCommandAsync.ExecuteAsync(null);

		_ = contexto.ServicioCursante.Received(1).RegistrarBajaDeCursanteAsync(new RegistrarBajaCursanteRequest(cursante.CursanteID, baja));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Dar_de_baja_sin_confirmar_no_llega_al_servicio()
	{
		// La baja es destructiva: cierra la inscripción y el alumno desaparece del listado.
		var contexto = new Contexto();
		await contexto.BuscarYSeleccionarAsync();
		contexto.Dialogos.RespuestaDeConfirmacion = false;

		await contexto.ViewModel.RegistrarBajaCommandAsync.ExecuteAsync(null);

		_ = contexto.ServicioCursante.DidNotReceive().RegistrarBajaDeCursanteAsync(Arg.Any<RegistrarBajaCursanteRequest>());
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Dar_de_baja_recarga_el_listado_porque_el_alumno_deja_de_estar_en_la_division()
	{
		var contexto = new Contexto();
		await contexto.BuscarYSeleccionarAsync();

		await contexto.ViewModel.RegistrarBajaCommandAsync.ExecuteAsync(null);

		// Dos veces: la búsqueda inicial y la recarga posterior a la baja.
		_ = contexto.ServicioCursante.Received(2).ListarCursantesAsync(Arg.Any<BuscarListadoRequest>());
		contexto.ViewModel.Cursante.ShouldBeNull();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cambiar_de_division_a_la_division_actual_no_esta_habilitado()
	{
		// El dominio lo rechaza con DivisionDestinoInvalidaException: es mejor no habilitar la
		// acción que mostrar el error después de que el usuario la intentó.
		var contexto = new Contexto();
		contexto.DevolverDivisiones(new DivisionResponse(contexto.DivisionID, "A", null, null, 10),
									new DivisionResponse(Guid.NewGuid(), "B", null, null, 8));
		await contexto.ViewModel.CargarCommandAsync.ExecuteAsync(null);
		await contexto.BuscarYSeleccionarAsync();

		contexto.ViewModel.DivisionDestino = contexto.ViewModel.DivisionesDestino.Single(x => x.Descripcion == "A");
		contexto.ViewModel.CambiarDivisionCommandAsync.CanExecute(null).ShouldBeFalse();

		contexto.ViewModel.DivisionDestino = contexto.ViewModel.DivisionesDestino.Single(x => x.Descripcion == "B");
		contexto.ViewModel.CambiarDivisionCommandAsync.CanExecute(null).ShouldBeTrue();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cambiar_de_division_viaja_con_el_alumno_el_periodo_del_listado_y_el_destino()
	{
		var contexto = new Contexto();
		var destinoID = Guid.NewGuid();
		contexto.DevolverDivisiones(new DivisionResponse(destinoID, "B", null, null, 8));
		await contexto.ViewModel.CargarCommandAsync.ExecuteAsync(null);
		var cursante = await contexto.BuscarYSeleccionarAsync();
		var cambio = DateTime.Today.AddDays(-3);
		contexto.ViewModel.DivisionDestino = contexto.ViewModel.DivisionesDestino[0];
		contexto.ViewModel.FechaCambio = cambio;

		await contexto.ViewModel.CambiarDivisionCommandAsync.ExecuteAsync(null);

		// AlumnoID y no CursanteID: el traslado abre una inscripción nueva, así que el cursante de
		// destino todavía no existe. Y el periodo es el del listado, no el del DatePicker.
		_ = contexto.ServicioCursante.Received(1).CambiarDeDivisionAsync(new CambiarDivisionCursanteRequest(cursante.AlumnoID, CicloDelStore, destinoID, cambio));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cambiar_de_division_sin_confirmar_no_llega_al_servicio()
	{
		var contexto = new Contexto();
		contexto.DevolverDivisiones(new DivisionResponse(Guid.NewGuid(), "B", null, null, 8));
		await contexto.ViewModel.CargarCommandAsync.ExecuteAsync(null);
		await contexto.BuscarYSeleccionarAsync();
		contexto.ViewModel.DivisionDestino = contexto.ViewModel.DivisionesDestino[0];
		contexto.Dialogos.RespuestaDeConfirmacion = false;

		await contexto.ViewModel.CambiarDivisionCommandAsync.ExecuteAsync(null);

		_ = contexto.ServicioCursante.DidNotReceive().CambiarDeDivisionAsync(Arg.Any<CambiarDivisionCursanteRequest>());
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task El_historial_expone_las_inscripciones_del_alumno_con_su_estado()
	{
		var contexto = new Contexto();
		var cursante = await contexto.BuscarYSeleccionarAsync();
		contexto.DevolverHistorial(
			new InscripcionHistoricaResponse(Guid.NewGuid(), Guid.NewGuid(), "A", "Primero", "Secundaria", "2024", new DateTime(2024, 3, 1), new DateTime(2024, 7, 1), false, false),
			new InscripcionHistoricaResponse(Guid.NewGuid(), Guid.NewGuid(), "B", "Primero", "Secundaria", "2024", new DateTime(2024, 7, 1), null, false, true));

		await contexto.ViewModel.CargarHistorialCommandAsync.ExecuteAsync(null);

		_ = contexto.ServicioCursante.Received(1).ConsultarHistorialDeDivisionesAsync(new ConsultarHistorialRequest(cursante.AlumnoID));
		contexto.ViewModel.Historial.Count.ShouldBe(2);
		contexto.ViewModel.Historial[0].Division.ShouldBe("A");
		contexto.ViewModel.Historial[0].EstaActiva.ShouldBeFalse();
		contexto.ViewModel.Historial[0].Estado.ShouldBe("Cerrada");
		contexto.ViewModel.Historial[1].EstaActiva.ShouldBeTrue();
		contexto.ViewModel.Historial[1].Estado.ShouldBe("Vigente");
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cambiar_de_alumno_limpia_el_historial_y_el_destino_del_anterior()
	{
		var contexto = new Contexto();
		contexto.DevolverDivisiones(new DivisionResponse(Guid.NewGuid(), "B", null, null, 8));
		await contexto.ViewModel.CargarCommandAsync.ExecuteAsync(null);
		contexto.DevolverCursantes(new CursanteResponse(Guid.NewGuid(), Guid.NewGuid(), "Perez, Ana", "30111222", 15, true),
								   new CursanteResponse(Guid.NewGuid(), Guid.NewGuid(), "Luna, Juan", "30111223", 16, false));
		contexto.DevolverHistorial(new InscripcionHistoricaResponse(Guid.NewGuid(), Guid.NewGuid(), "A", "Primero", "Secundaria", "2024", new DateTime(2024, 3, 1), null, false, true));
		await contexto.ViewModel.BuscarCommandAsync.ExecuteAsync(null);
		contexto.ViewModel.Cursante = contexto.ViewModel.Cursantes[0];
		contexto.ViewModel.DivisionDestino = contexto.ViewModel.DivisionesDestino[0];
		await contexto.ViewModel.CargarHistorialCommandAsync.ExecuteAsync(null);
		contexto.ViewModel.Historial.ShouldNotBeEmpty();

		contexto.ViewModel.Cursante = contexto.ViewModel.Cursantes[1];

		contexto.ViewModel.Historial.ShouldBeEmpty();
		contexto.ViewModel.DivisionDestino.ShouldBeNull();
		contexto.ViewModel.EsRecursante.ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Si_fallan_las_divisiones_de_destino_la_pantalla_avisa_y_sigue_cargando_las_materias()
	{
		var contexto = new Contexto();
		contexto.ServicioDivision
				.ListarDivisionesAsync(Arg.Any<ListarDivisionesRequest>())
				.Returns<IReadOnlyCollection<DivisionResponse>>(_ => throw new InvalidOperationException("sin divisiones"));

		await contexto.ViewModel.CargarCommandAsync.ExecuteAsync(null);

		contexto.ViewModel.Materias.Count.ShouldBe(2);
		contexto.ViewModel.DivisionesDestino.ShouldBeEmpty();
		contexto.Dialogos.Advertencias.Count.ShouldBe(1);
	}
	#endregion

	#region Vistas del alumno
	/// <summary>
	/// El defecto que esto fija: el historial se pedía con un botón y la sección aparecía para
	/// quedarse. No había forma de volver a las calificaciones sin cambiar de alumno.
	/// </summary>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Abrir_el_historial_lo_carga_y_se_puede_volver_a_las_calificaciones()
	{
		var contexto = new Contexto();
		var cursante = await contexto.BuscarYSeleccionarAsync();
		contexto.DevolverHistorial(
			new InscripcionHistoricaResponse(Guid.NewGuid(), Guid.NewGuid(), "A", "Primero", "Secundaria", "2024", new DateTime(2024, 3, 1), null, false, true));

		contexto.ViewModel.VistaDelAlumno.ShouldBe(GestionCursantesViewModel.VISTA_CALIFICACIONES);

		contexto.ViewModel.VerCommand.Execute(GestionCursantesViewModel.VISTA_HISTORIAL);

		contexto.ViewModel.VistaDelAlumno.ShouldBe(GestionCursantesViewModel.VISTA_HISTORIAL);
		_ = contexto.ServicioCursante.Received(1).ConsultarHistorialDeDivisionesAsync(new ConsultarHistorialRequest(cursante.AlumnoID));
		contexto.ViewModel.Historial.Count.ShouldBe(1);

		contexto.ViewModel.VerCommand.Execute(GestionCursantesViewModel.VISTA_CALIFICACIONES);

		contexto.ViewModel.VistaDelAlumno.ShouldBe(GestionCursantesViewModel.VISTA_CALIFICACIONES);
	}

	/// <summary>El historial no cambia mientras se mira al mismo alumno: se trae una sola vez.</summary>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Volver_al_historial_del_mismo_alumno_no_lo_vuelve_a_consultar()
	{
		var contexto = new Contexto();
		await contexto.BuscarYSeleccionarAsync();
		contexto.DevolverHistorial(
			new InscripcionHistoricaResponse(Guid.NewGuid(), Guid.NewGuid(), "A", "Primero", "Secundaria", "2024", new DateTime(2024, 3, 1), null, false, true));

		contexto.ViewModel.VerCommand.Execute(GestionCursantesViewModel.VISTA_HISTORIAL);
		contexto.ViewModel.VerCommand.Execute(GestionCursantesViewModel.VISTA_CALIFICACIONES);
		contexto.ViewModel.VerCommand.Execute(GestionCursantesViewModel.VISTA_HISTORIAL);

		_ = contexto.ServicioCursante.Received(1).ConsultarHistorialDeDivisionesAsync(Arg.Any<ConsultarHistorialRequest>());
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Abrir_las_asistencias_cuenta_los_cuatro_tipos_del_alumno_elegido()
	{
		var contexto = new Contexto();
		var cursante = await contexto.BuscarYSeleccionarAsync();
		contexto.DevolverConteo(TipoAsistencia.Presente, 142);
		contexto.DevolverConteo(TipoAsistencia.Ausencia, 6);
		contexto.DevolverConteo(TipoAsistencia.Inasistencia, 3);
		contexto.DevolverConteo(TipoAsistencia.Tardanza, 4);

		contexto.ViewModel.VerCommand.Execute(GestionCursantesViewModel.VISTA_ASISTENCIAS);

		contexto.ViewModel.Presentes.ShouldBe(142);
		contexto.ViewModel.Ausencias.ShouldBe(6);
		contexto.ViewModel.Inasistencias.ShouldBe(3);
		contexto.ViewModel.Tardanzas.ShouldBe(4);
		_ = contexto.ServicioAsistencia.Received(1).ContarFaltasAsync(new ContarFaltasRequest(cursante.CursanteID, TipoAsistencia.Presente));
	}

	/// <summary>
	/// Si cambiar de alumno dejara abierta la vista de asistencias, los contadores recién
	/// puestos en cero se leerían como los del alumno nuevo.
	/// </summary>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cambiar_de_alumno_vuelve_a_calificaciones_y_pone_los_contadores_en_cero()
	{
		var contexto = new Contexto();
		contexto.DevolverCursantes(new CursanteResponse(Guid.NewGuid(), Guid.NewGuid(), "Perez, Ana", "30111222", 15, false),
								   new CursanteResponse(Guid.NewGuid(), Guid.NewGuid(), "Gomez, Luis", "30111333", 16, false));
		await contexto.ViewModel.BuscarCommandAsync.ExecuteAsync(null);
		contexto.ViewModel.Cursante = contexto.ViewModel.Cursantes[0];
		contexto.DevolverConteo(TipoAsistencia.Presente, 142);

		contexto.ViewModel.VerCommand.Execute(GestionCursantesViewModel.VISTA_ASISTENCIAS);
		contexto.ViewModel.Presentes.ShouldBe(142);

		contexto.ViewModel.Cursante = contexto.ViewModel.Cursantes[1];

		contexto.ViewModel.VistaDelAlumno.ShouldBe(GestionCursantesViewModel.VISTA_CALIFICACIONES);
		contexto.ViewModel.Presentes.ShouldBe(0);
		contexto.ViewModel.Ausencias.ShouldBe(0);
		contexto.ViewModel.Inasistencias.ShouldBe(0);
		contexto.ViewModel.Tardanzas.ShouldBe(0);
	}

	/// <summary>El alta es de una calificación: abrirla desde otra vista vuelve a su grilla.</summary>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_una_nota_desde_otra_vista_vuelve_a_calificaciones()
	{
		var contexto = new Contexto();
		await contexto.BuscarYSeleccionarAsync();
		contexto.ViewModel.VerCommand.Execute(GestionCursantesViewModel.VISTA_INSCRIPCION);

		contexto.ViewModel.NuevaCalificacionCommand.Execute(null);

		contexto.ViewModel.VistaDelAlumno.ShouldBe(GestionCursantesViewModel.VISTA_CALIFICACIONES);
		contexto.ViewModel.MostrarCalificacionView.ShouldBeTrue();
	}

	/// <summary>Sin alumno elegido no hay vista que abrir: el conmutador está apagado.</summary>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Sin_alumno_elegido_no_se_puede_cambiar_de_vista()
	{
		var contexto = new Contexto();

		contexto.ViewModel.VerCommand.CanExecute(GestionCursantesViewModel.VISTA_ASISTENCIAS).ShouldBeFalse();
	}
	#endregion
}
