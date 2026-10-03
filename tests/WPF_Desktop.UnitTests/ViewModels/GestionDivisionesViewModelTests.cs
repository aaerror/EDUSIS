using Core.ServicioCursos.DTOs.Responses;
using Core.ServicioDivisiones;
using Core.ServicioDivisiones.DTOs.Requests;
using Core.ServicioDivisiones.DTOs.Responses;
using Core.ServicioDocentes;
using Core.ServicioDocentes.DTOs.Responses;
using Domain.Cursos;
using Domain.Shared;
using NSubstitute;
using Shouldly;
using WPF_Desktop.Store;
using WPF_Desktop.UnitTests.Dobles;
using WPF_Desktop.ViewModels.Cursos;
using WPF_Desktop.ViewModels.Cursos.Divisiones;
using WPF_Desktop.ViewModels.Docentes;
using Xunit;

namespace WPF_Desktop.UnitTests.ViewModels;

/// <summary>
/// Cubre el remapeo de la tabla D.2 de las cinco llamadas de <see cref="GestionDivisionesViewModel"/>
/// (ListarDivisionesAsync, AgregarDivisionAsync, EliminarDivisionAsync, AsignarPreceptorAsync, QuitarPreceptorAsync),
/// el origen del ciclo lectivo y el orden store-antes-de-navegar.
/// </summary>
public class GestionDivisionesViewModelTests
{
	private const string CicloDelStore = "1999";

	private sealed class Contexto
	{
		public IServicioDivision ServicioDivision { get; } = Substitute.For<IServicioDivision>();
		public IServicioDocente ServicioDocente { get; } = Substitute.For<IServicioDocente>();
		public NavegacionFalsa NavegarACursos { get; } = new();
		public NavegacionFalsa NavegarACursantes { get; } = new();
		public CursoStore CursoStore { get; } = new();
		public DivisionStore DivisionStore { get; } = new();
		public CicloLectivoStore CicloLectivoStore { get; } = new();
		public DialogServiceFalso Dialogos { get; } = new();
		public Guid CursoID { get; } = Guid.NewGuid();
		public GestionDivisionesViewModel ViewModel { get; }

		public Contexto()
		{
			CursoStore.Curso = new CursoViewModel(new CursoResponse(CursoID, Grado.Primero, NivelEducativo.Secundaria));
			CicloLectivoStore.CicloLectivo = CicloDelStore;

			DevolverDivisiones();

			ViewModel = new GestionDivisionesViewModel(ServicioDivision,
													   ServicioDocente,
													   NavegarACursos,
													   NavegarACursantes,
													   CursoStore,
													   DivisionStore,
													   CicloLectivoStore,
													   Dialogos);
		}

		public void DevolverDivisiones(params DivisionResponse[] divisiones)
		{
			ServicioDivision.ListarDivisionesAsync(Arg.Any<ListarDivisionesRequest>())
							.Returns(Task.FromResult<IReadOnlyCollection<DivisionResponse>>(divisiones.ToList()));
		}
	}

	private static DivisionResponse CrearDivision(string letra = "A", Guid? preceptorID = null, string? preceptor = null, int cursantes = 0)
	{
		return new DivisionResponse(Guid.NewGuid(), letra, preceptorID, preceptor, cursantes);
	}

	#region ListarDivisionesAsync
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_pide_las_divisiones_con_el_curso_y_el_ciclo_lectivo_del_store()
	{
		var contexto = new Contexto();

		await contexto.ViewModel.CargarDivisionesCommandAsync.ExecuteAsync(null);

		_ = contexto.ServicioDivision.Received(1).ListarDivisionesAsync(new ListarDivisionesRequest(contexto.CursoID, CicloDelStore));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_con_divisiones_las_expone_y_cuenta_el_total()
	{
		var contexto = new Contexto();
		contexto.DevolverDivisiones(CrearDivision("A"), CrearDivision("B"));

		await contexto.ViewModel.CargarDivisionesCommandAsync.ExecuteAsync(null);

		contexto.ViewModel.TotalDivisiones.ShouldBe(2);
		contexto.ViewModel.ListCollectionDocentes.Count.ShouldBe(2);
		contexto.ViewModel.HabilitarDivisiones.ShouldBeTrue();
		contexto.ViewModel.HabilitarNotificacion.ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_sin_divisiones_avisa_en_pantalla_y_no_habilita_la_grilla()
	{
		var contexto = new Contexto();

		await contexto.ViewModel.CargarDivisionesCommandAsync.ExecuteAsync(null);

		contexto.ViewModel.TotalDivisiones.ShouldBe(0);
		contexto.ViewModel.HabilitarDivisiones.ShouldBeFalse();
		contexto.ViewModel.HabilitarNotificacion.ShouldBeTrue();
		contexto.ViewModel.Message.ShouldNotBeNullOrWhiteSpace();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Un_error_del_servicio_al_cargar_se_informa_en_pantalla_y_no_se_propaga()
	{
		var contexto = new Contexto();
		contexto.ServicioDivision.ListarDivisionesAsync(Arg.Any<ListarDivisionesRequest>())
								 .Returns(Task.FromException<IReadOnlyCollection<DivisionResponse>>(new InvalidOperationException("sin conexion")));

		await contexto.ViewModel.CargarDivisionesCommandAsync.ExecuteAsync(null);

		contexto.ViewModel.HabilitarNotificacion.ShouldBeTrue();
		contexto.ViewModel.Message.ShouldContain("sin conexion");
	}
	#endregion

	#region EliminarDivisionAsync
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Eliminar_una_division_llama_a_EliminarDivisionAsync_con_el_ciclo_lectivo_del_store()
	{
		var contexto = new Contexto();
		var division = CrearDivision("A");
		contexto.ViewModel.Division = new DivisionViewModel(division);

		await contexto.ViewModel.EliminarCommandAsync.ExecuteAsync("Division");

		_ = contexto.ServicioDivision.Received(1).EliminarDivisionAsync(new EliminarDivisionRequest(contexto.CursoID, division.DivisionID, CicloDelStore));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Eliminar_una_division_no_usa_el_ano_de_DateTime_Today_como_ciclo_lectivo()
	{
		var contexto = new Contexto();
		contexto.CicloLectivoStore.CicloLectivo = "1850";
		contexto.ViewModel.Division = new DivisionViewModel(CrearDivision("A"));

		await contexto.ViewModel.EliminarCommandAsync.ExecuteAsync("Division");

		var anioActual = DateTime.Today.Year.ToString();
		_ = contexto.ServicioDivision.DidNotReceive().EliminarDivisionAsync(Arg.Is<EliminarDivisionRequest>(r => r.CicloLectivo == anioActual));
		_ = contexto.ServicioDivision.Received(1).EliminarDivisionAsync(Arg.Is<EliminarDivisionRequest>(r => r.CicloLectivo == "1850"));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Eliminar_una_division_sin_confirmar_no_llega_al_servicio()
	{
		var contexto = new Contexto();
		contexto.Dialogos.RespuestaDeConfirmacion = false;
		contexto.ViewModel.Division = new DivisionViewModel(CrearDivision("A"));

		await contexto.ViewModel.EliminarCommandAsync.ExecuteAsync("Division");

		contexto.Dialogos.Confirmaciones.Count.ShouldBe(1);
		_ = contexto.ServicioDivision.DidNotReceive().EliminarDivisionAsync(Arg.Any<EliminarDivisionRequest>());
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Eliminar_una_division_espera_la_recarga_antes_de_terminar()
	{
		// Regresion de los tres CargarDivisionesAsync() sin await de W4.A: si la recarga no se espera,
		// el Task del comando termina antes de que el listado se haya pedido.
		var contexto = new Contexto();
		contexto.ViewModel.Division = new DivisionViewModel(CrearDivision("A"));

		await contexto.ViewModel.EliminarCommandAsync.ExecuteAsync("Division");

		_ = contexto.ServicioDivision.Received(1).ListarDivisionesAsync(new ListarDivisionesRequest(contexto.CursoID, CicloDelStore));
		contexto.Dialogos.Informaciones.Count.ShouldBe(1);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Un_error_del_servicio_al_eliminar_se_muestra_por_el_dialogo_y_no_se_propaga()
	{
		var contexto = new Contexto();
		contexto.ServicioDivision.EliminarDivisionAsync(Arg.Any<EliminarDivisionRequest>())
								 .Returns(Task.FromException(new InvalidOperationException("tiene cursantes")));
		contexto.ViewModel.Division = new DivisionViewModel(CrearDivision("A"));

		await contexto.ViewModel.EliminarCommandAsync.ExecuteAsync("Division");

		contexto.Dialogos.Errores.Count.ShouldBe(1);
		contexto.Dialogos.Errores[0].Mensaje.ShouldBe("tiene cursantes");
		contexto.Dialogos.Informaciones.ShouldBeEmpty();
	}
	#endregion

	#region AgregarDivisionAsync
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Registrar_una_division_llama_a_AgregarDivisionAsync_con_el_curso_del_store()
	{
		var contexto = new Contexto();

		await contexto.ViewModel.RegistrarCommandAsync.ExecuteAsync("Division");

		_ = contexto.ServicioDivision.Received(1).AgregarDivisionAsync(new AgregarDivisionRequest(contexto.CursoID));
		contexto.Dialogos.Informaciones.Count.ShouldBe(1);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Registrar_una_division_sin_confirmar_no_llega_al_servicio()
	{
		var contexto = new Contexto();
		contexto.Dialogos.RespuestaDeConfirmacion = false;

		await contexto.ViewModel.RegistrarCommandAsync.ExecuteAsync("Division");

		_ = contexto.ServicioDivision.DidNotReceive().AgregarDivisionAsync(Arg.Any<AgregarDivisionRequest>());
	}
	#endregion

	#region AsignarPreceptorAsync / QuitarPreceptorAsync
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Guardar_asigna_el_preceptor_con_el_curso_la_division_y_el_docente_elegidos()
	{
		var contexto = new Contexto();
		var division = CrearDivision("A");
		var docenteID = Guid.NewGuid();
		contexto.ViewModel.Division = new DivisionViewModel(division);
		contexto.ViewModel.Docente = new LegajoDocenteViewModel(new LegajoDocenteResponse(docenteID, "Perez, Ana", "30111222", "20301112229", "L-1", new DateTime(2020, 3, 1), null, true));

		await contexto.ViewModel.GuardarCommandAsync.ExecuteAsync("Insert");

		_ = contexto.ServicioDivision.Received(1).AsignarPreceptorAsync(new RegistrarPreceptorRequest(contexto.CursoID, division.DivisionID, docenteID));
		contexto.ViewModel.Docente.ShouldBeNull();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Quitar_el_preceptor_llama_a_QuitarPreceptorAsync_con_el_curso_y_la_division()
	{
		var contexto = new Contexto();
		var division = CrearDivision("A", Guid.NewGuid(), "Perez, Ana");
		contexto.ViewModel.Division = new DivisionViewModel(division);

		await contexto.ViewModel.EliminarCommandAsync.ExecuteAsync("Preceptor");

		_ = contexto.ServicioDivision.Received(1).QuitarPreceptorAsync(new EliminarPreceptorRequest(contexto.CursoID, division.DivisionID));
		_ = contexto.ServicioDivision.DidNotReceive().EliminarDivisionAsync(Arg.Any<EliminarDivisionRequest>());
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Quitar_el_preceptor_solo_se_habilita_si_la_division_tiene_preceptor()
	{
		// Regresion del defecto latente de DivisionViewModel._preceptorID (Guid? inicializado en Guid.Empty).
		var contexto = new Contexto();

		contexto.ViewModel.Division = new DivisionViewModel(CrearDivision("A", preceptorID: null, preceptor: null));
		contexto.ViewModel.EliminarCommandAsync.CanExecute("Preceptor").ShouldBeFalse();

		contexto.ViewModel.Division = new DivisionViewModel(CrearDivision("B", Guid.NewGuid(), "Perez, Ana"));
		contexto.ViewModel.EliminarCommandAsync.CanExecute("Preceptor").ShouldBeTrue();
	}
	#endregion

	#region Navegacion
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Navegar_a_cursantes_deja_la_division_en_el_DivisionStore_antes_de_llamar_a_Navigate()
	{
		var contexto = new Contexto();
		var division = new DivisionViewModel(CrearDivision("A"));
		contexto.ViewModel.Division = division;
		DivisionViewModel? divisionAlNavegar = null;
		contexto.NavegarACursantes.AlNavegar = () => divisionAlNavegar = contexto.DivisionStore.Division;

		contexto.ViewModel.NavigationCommand.Execute("Cursantes");

		contexto.NavegarACursantes.Llamadas.ShouldBe(1);
		divisionAlNavegar.ShouldBeSameAs(division);
		contexto.DivisionStore.Division.ShouldBeSameAs(division);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Navegar_a_cursantes_usa_el_servicio_de_cursantes_y_no_el_de_cursos()
	{
		// Violacion #7 del plan: los dos INavigationService se distinguen solo por su orden en el constructor.
		var contexto = new Contexto();
		contexto.ViewModel.Division = new DivisionViewModel(CrearDivision("A"));

		contexto.ViewModel.NavigationCommand.Execute("Cursantes");

		contexto.NavegarACursantes.Llamadas.ShouldBe(1);
		contexto.NavegarACursos.Llamadas.ShouldBe(0);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Navegar_a_curso_usa_el_servicio_de_cursos_y_no_toca_el_DivisionStore()
	{
		var contexto = new Contexto();

		contexto.ViewModel.NavigationCommand.Execute("Curso");

		contexto.NavegarACursos.Llamadas.ShouldBe(1);
		contexto.NavegarACursantes.Llamadas.ShouldBe(0);
		contexto.DivisionStore.Division.ShouldBeNull();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Navegar_a_cursantes_sin_division_seleccionada_no_esta_habilitado()
	{
		var contexto = new Contexto();

		contexto.ViewModel.NavigationCommand.CanExecute("Cursantes").ShouldBeFalse();
	}

	// El defecto que esta prueba documentaba quedo corregido en el lote de R4: Division ya declara
	// [NotifyCanExecuteChangedFor] hacia NavigationCommand y EliminarCommandAsync.
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Seleccionar_una_division_notifica_el_cambio_de_CanExecute_de_la_navegacion_y_la_baja()
	{
		var contexto = new Contexto();
		var navegacion = 0;
		var baja = 0;
		contexto.ViewModel.NavigationCommand.CanExecuteChanged += (_, _) => navegacion++;
		contexto.ViewModel.EliminarCommandAsync.CanExecuteChanged += (_, _) => baja++;

		contexto.ViewModel.Division = new DivisionViewModel(CrearDivision("A"));

		navegacion.ShouldBeGreaterThan(0);
		baja.ShouldBeGreaterThan(0);
	}
	#endregion
}
