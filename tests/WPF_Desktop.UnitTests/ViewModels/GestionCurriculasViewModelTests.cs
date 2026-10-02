using CommunityToolkit.Mvvm.Input;
using Core.ServicioCurriculas;
using Core.ServicioCurriculas.DTOs.Requests;
using Core.ServicioCurriculas.DTOs.Responses;
using Core.ServicioCursos.DTOs.Responses;
using Core.ServicioMaterias;
using Core.ServicioMaterias.DTOs.Requests;
using Core.ServicioMaterias.DTOs.Responses;
using Domain.Cursos;
using Domain.Shared;
using NSubstitute;
using Shouldly;
using WPF_Desktop.Store;
using WPF_Desktop.UnitTests.Dobles;
using WPF_Desktop.ViewModels.Cursos;
using WPF_Desktop.ViewModels.Cursos.Curriculas;
using WPF_Desktop.ViewModels.Cursos.Curriculas.Materias;
using Xunit;

namespace WPF_Desktop.UnitTests.ViewModels;

/// <summary>
/// Cubre <see cref="GestionCurriculasViewModel"/> tras la ola W5: <c>UI-01</c> (el numero que el combo muestra es el que
/// viaja en el request y el que se persiste, sin suma ni resta), <c>UI-08</c> (un solo camino de alta de materia), la
/// particion de dependencias (<see cref="IServicioMateria"/> para materias, <see cref="IServicioCurricula"/> para curriculas),
/// el destino de la navegacion y las notificaciones de <c>CanExecute</c>.
/// El combo liga <c>SelectedItem</c> a un valor entero: asignar <c>CargaHoraria</c> o <c>HorasCatedra</c> es exactamente lo
/// que hace el enlace cuando el usuario elige un item. Que el primer item del combo sea 1 se afirma sobre el XAML en
/// <see cref="GuardasEstaticasW5Tests"/>.
/// </summary>
public class GestionCurriculasViewModelTests
{
	private sealed class Contexto
	{
		public IServicioCurricula ServicioCurricula { get; } = Substitute.For<IServicioCurricula>();
		public IServicioMateria ServicioMateria { get; } = Substitute.For<IServicioMateria>();
		public NavegacionFalsa NavegarACursos { get; } = new();
		public NavegacionFalsa NavegarACatedras { get; } = new();
		public CursoStore CursoStore { get; } = new();
		public MateriaStore MateriaStore { get; } = new();
		public DialogServiceFalso Dialogos { get; } = new();
		public Guid CursoID { get; } = Guid.NewGuid();
		public Guid CurriculaID { get; } = Guid.NewGuid();
		public GestionCurriculasViewModel ViewModel { get; }

		public Contexto()
		{
			CursoStore.Curso = new CursoViewModel(new CursoResponse(CursoID, Grado.Primero, NivelEducativo.Secundaria));

			DevolverCurriculas(new CurriculaResponse(CursoID, CurriculaID, new DateTime(2024, 3, 1), null));
			DevolverMaterias();

			ViewModel = new GestionCurriculasViewModel(NavegarACursos,
													   NavegarACatedras,
													   ServicioCurricula,
													   ServicioMateria,
													   Dialogos,
													   CursoStore,
													   MateriaStore);
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

		public MateriaResponse CrearMateria(string descripcion = "Matematica", int horas = 3)
		{
			return new MateriaResponse(CursoID, CurriculaID, Guid.NewGuid(), descripcion, horas);
		}

		/// <summary>Deja cargada y seleccionada la curricula vigente, como lo hace la vista al abrirse.</summary>
		public async Task SeleccionarCurriculaAsync()
		{
			await ViewModel.CargarCurriculasCommandAsync.ExecuteAsync(null);
			ViewModel.Curricula = ViewModel.Curriculas[0];
		}

		/// <summary>Deja cargadas las materias y seleccionada la primera, como el ListView de la pantalla.</summary>
		public async Task SeleccionarMateriaAsync(MateriaResponse materia)
		{
			DevolverMaterias(materia);
			await SeleccionarCurriculaAsync();
			await ViewModel.CargarMateriasCommandAsync.ExecuteAsync(null);
			ViewModel.Materia = ViewModel.Materias[0];
		}
	}

	private static int ContarNotificaciones(IRelayCommand comando, Action accion)
	{
		var veces = 0;
		comando.CanExecuteChanged += (_, _) => veces++;
		accion();
		return veces;
	}

	#region UI-01: alta de materia
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void La_carga_horaria_inicial_es_1_y_no_0()
	{
		var contexto = new Contexto();

		contexto.ViewModel.CargaHoraria.ShouldBe(1);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Elegir_el_primer_valor_del_combo_registra_HorasCatedra_1()
	{
		var contexto = new Contexto();
		await contexto.SeleccionarCurriculaAsync();
		contexto.ViewModel.Descripcion = "Matematica";
		contexto.ViewModel.CargaHoraria = 3;
		contexto.ViewModel.CargaHoraria = 1; // el usuario vuelve a elegir el primer item del combo

		await contexto.ViewModel.GuardarCommandAsync.ExecuteAsync("Materia");

		_ = contexto.ServicioMateria.Received(1).RegistrarMateriaAsync(new RegistrarMateriaRequest(contexto.CursoID, contexto.CurriculaID, "Matematica", 1));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Guardar_sin_tocar_el_combo_registra_HorasCatedra_1_y_nunca_0()
	{
		var contexto = new Contexto();
		await contexto.SeleccionarCurriculaAsync();
		contexto.ViewModel.Descripcion = "Lengua";

		await contexto.ViewModel.GuardarCommandAsync.ExecuteAsync("Materia");

		_ = contexto.ServicioMateria.Received(1).RegistrarMateriaAsync(Arg.Is<RegistrarMateriaRequest>(r => r.HorasCatedra == 1));
		_ = contexto.ServicioMateria.DidNotReceive().RegistrarMateriaAsync(Arg.Is<RegistrarMateriaRequest>(r => r.HorasCatedra == 0));
	}

	[Theory]
	[Trait("Categoria", "Unidad")]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	[InlineData(4)]
	[InlineData(5)]
	public async Task El_valor_elegido_en_el_combo_es_el_que_viaja_en_el_request(int horas)
	{
		var contexto = new Contexto();
		await contexto.SeleccionarCurriculaAsync();
		contexto.ViewModel.Descripcion = "Historia";
		contexto.ViewModel.CargaHoraria = horas;

		await contexto.ViewModel.GuardarCommandAsync.ExecuteAsync("Materia");

		_ = contexto.ServicioMateria.Received(1).RegistrarMateriaAsync(new RegistrarMateriaRequest(contexto.CursoID, contexto.CurriculaID, "Historia", horas));
	}

	[Theory]
	[Trait("Categoria", "Unidad")]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	[InlineData(4)]
	[InlineData(5)]
	public async Task El_dialogo_de_confirmacion_muestra_el_mismo_numero_que_viaja_en_el_request(int horas)
	{
		var contexto = new Contexto();
		await contexto.SeleccionarCurriculaAsync();
		contexto.ViewModel.Descripcion = "Geografia";
		contexto.ViewModel.CargaHoraria = horas;
		RegistrarMateriaRequest? enviado = null;
		_ = contexto.ServicioMateria.RegistrarMateriaAsync(Arg.Do<RegistrarMateriaRequest>(r => enviado = r));

		await contexto.ViewModel.GuardarCommandAsync.ExecuteAsync("Materia");

		var mensaje = contexto.Dialogos.Confirmaciones.Single().Mensaje;
		enviado.ShouldNotBeNull();
		enviado!.HorasCatedra.ShouldBe(horas);
		mensaje.ShouldContain($"Carga horaria: {horas} horas");
		mensaje.ShouldNotContain($"Carga horaria: {horas + 1} horas");
		mensaje.ShouldNotContain($"Carga horaria: {horas - 1} horas");
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Despues_de_registrar_el_formulario_vuelve_a_1_y_a_descripcion_vacia()
	{
		var contexto = new Contexto();
		await contexto.SeleccionarCurriculaAsync();
		contexto.ViewModel.Descripcion = "Fisica";
		contexto.ViewModel.CargaHoraria = 4;

		await contexto.ViewModel.GuardarCommandAsync.ExecuteAsync("Materia");

		contexto.ViewModel.CargaHoraria.ShouldBe(1);
		contexto.ViewModel.Descripcion.ShouldBe(string.Empty);
		contexto.Dialogos.Informaciones.Count.ShouldBe(1);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Si_el_alta_falla_el_error_se_muestra_y_el_formulario_tambien_vuelve_a_1()
	{
		var contexto = new Contexto();
		await contexto.SeleccionarCurriculaAsync();
		contexto.ServicioMateria.RegistrarMateriaAsync(Arg.Any<RegistrarMateriaRequest>())
								.Returns(Task.FromException<Guid>(new InvalidOperationException("duplicada")));
		contexto.ViewModel.Descripcion = "Quimica";
		contexto.ViewModel.CargaHoraria = 5;

		await contexto.ViewModel.GuardarCommandAsync.ExecuteAsync("Materia");

		contexto.Dialogos.Errores.Single().Mensaje.ShouldBe("duplicada");
		contexto.ViewModel.CargaHoraria.ShouldBe(1);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Si_el_usuario_no_confirma_no_se_registra_nada()
	{
		var contexto = new Contexto();
		await contexto.SeleccionarCurriculaAsync();
		contexto.Dialogos.RespuestaDeConfirmacion = false;
		contexto.ViewModel.Descripcion = "Musica";

		await contexto.ViewModel.GuardarCommandAsync.ExecuteAsync("Materia");

		_ = contexto.ServicioMateria.DidNotReceiveWithAnyArgs().RegistrarMateriaAsync(default!);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Una_descripcion_vacia_se_informa_y_no_llega_al_servicio()
	{
		var contexto = new Contexto();
		await contexto.SeleccionarCurriculaAsync();
		contexto.ViewModel.Descripcion = "   ";

		await contexto.ViewModel.GuardarCommandAsync.ExecuteAsync("Materia");

		contexto.Dialogos.Advertencias.Count.ShouldBe(1);
		contexto.Dialogos.Confirmaciones.ShouldBeEmpty();
		_ = contexto.ServicioMateria.DidNotReceiveWithAnyArgs().RegistrarMateriaAsync(default!);
	}
	#endregion

	#region UI-01: edicion de materia
	[Theory]
	[Trait("Categoria", "Unidad")]
	[InlineData(2, 4)]
	[InlineData(5, 1)]
	[InlineData(3, 5)]
	public async Task Editar_la_carga_horaria_envia_el_valor_nuevo_y_no_el_que_tenia(int antes, int despues)
	{
		var contexto = new Contexto();
		var materia = contexto.CrearMateria("Matematica", antes);
		await contexto.SeleccionarMateriaAsync(materia);

		contexto.ViewModel.Materia.HorasCatedra = despues; // el usuario elige otro item en el combo de MateriaView

		await contexto.ViewModel.GuardarCommandAsync.ExecuteAsync("Update");

		_ = contexto.ServicioMateria.Received(1).ModificarMateriaAsync(new ModificarMateriaRequest(contexto.CursoID, contexto.CurriculaID, materia.MateriaID, "Matematica", despues));
		_ = contexto.ServicioMateria.DidNotReceive().ModificarMateriaAsync(Arg.Is<ModificarMateriaRequest>(r => r.HorasCatedra == antes));
	}

	[Theory]
	[Trait("Categoria", "Unidad")]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	[InlineData(4)]
	[InlineData(5)]
	public async Task Guardar_una_materia_sin_tocar_su_carga_horaria_no_le_suma_ni_le_resta_una_hora(int horas)
	{
		var contexto = new Contexto();
		var materia = contexto.CrearMateria("Matematica", horas);
		await contexto.SeleccionarMateriaAsync(materia);

		await contexto.ViewModel.GuardarCommandAsync.ExecuteAsync("Update");

		_ = contexto.ServicioMateria.Received(1).ModificarMateriaAsync(Arg.Is<ModificarMateriaRequest>(r => r.HorasCatedra == horas));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Guardar_dos_veces_seguidas_sin_tocar_nada_no_acumula_horas()
	{
		var contexto = new Contexto();
		var materia = contexto.CrearMateria("Matematica", 3);
		await contexto.SeleccionarMateriaAsync(materia);
		var enviados = new List<int>();
		_ = contexto.ServicioMateria.ModificarMateriaAsync(Arg.Do<ModificarMateriaRequest>(r => enviados.Add(r.HorasCatedra)));

		await contexto.ViewModel.GuardarCommandAsync.ExecuteAsync("Update");
		contexto.ViewModel.Materia = contexto.ViewModel.Materias[0]; // el ListView vuelve a seleccionar tras recargar
		await contexto.ViewModel.GuardarCommandAsync.ExecuteAsync("Update");

		enviados.ShouldBe(new[] { 3, 3 });
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Editar_conserva_la_descripcion_nueva_en_el_request()
	{
		var contexto = new Contexto();
		var materia = contexto.CrearMateria("Matematica", 3);
		await contexto.SeleccionarMateriaAsync(materia);
		contexto.ViewModel.Materia.Descripcion = "Matematica II";

		await contexto.ViewModel.GuardarCommandAsync.ExecuteAsync("Update");

		_ = contexto.ServicioMateria.Received(1).ModificarMateriaAsync(Arg.Is<ModificarMateriaRequest>(r => r.Descripcion == "Matematica II" && r.HorasCatedra == 3));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Si_el_usuario_no_confirma_la_edicion_no_se_modifica_nada()
	{
		var contexto = new Contexto();
		await contexto.SeleccionarMateriaAsync(contexto.CrearMateria());
		contexto.Dialogos.RespuestaDeConfirmacion = false;

		await contexto.ViewModel.GuardarCommandAsync.ExecuteAsync("Update");

		_ = contexto.ServicioMateria.DidNotReceiveWithAnyArgs().ModificarMateriaAsync(default!);
	}
	#endregion

	#region Altas y bajas pegan en IServicioMateria
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Alta_modificacion_y_baja_de_materia_pegan_solo_en_IServicioMateria()
	{
		var contexto = new Contexto();
		var materia = contexto.CrearMateria("Matematica", 2);
		await contexto.SeleccionarMateriaAsync(materia);
		contexto.ViewModel.Descripcion = "Nueva";

		await contexto.ViewModel.GuardarCommandAsync.ExecuteAsync("Materia");
		await contexto.ViewModel.GuardarCommandAsync.ExecuteAsync("Update");
		await contexto.ViewModel.EliminarCommandAsync.ExecuteAsync("Materia");

		_ = contexto.ServicioMateria.ReceivedWithAnyArgs(1).RegistrarMateriaAsync(default!);
		_ = contexto.ServicioMateria.ReceivedWithAnyArgs(1).ModificarMateriaAsync(default!);
		_ = contexto.ServicioMateria.ReceivedWithAnyArgs(1).EliminarMateriaAsync(default!);

		var llamadasACurricula = contexto.ServicioCurricula.ReceivedCalls().Select(c => c.GetMethodInfo().Name).ToList();
		llamadasACurricula.ShouldNotContain(nameof(IServicioCurricula.RegistrarCurriculaAsync));
		llamadasACurricula.ShouldNotContain(nameof(IServicioCurricula.DesafectarCurriculaAsync));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Eliminar_envia_los_tres_identificadores_de_la_materia_seleccionada()
	{
		var contexto = new Contexto();
		var materia = contexto.CrearMateria();
		await contexto.SeleccionarMateriaAsync(materia);

		await contexto.ViewModel.EliminarCommandAsync.ExecuteAsync("Materia");

		_ = contexto.ServicioMateria.Received(1).EliminarMateriaAsync(new EliminarMateriaRequest(contexto.CursoID, contexto.CurriculaID, materia.MateriaID));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Si_el_usuario_no_confirma_la_baja_no_se_elimina_nada()
	{
		var contexto = new Contexto();
		await contexto.SeleccionarMateriaAsync(contexto.CrearMateria());
		contexto.Dialogos.RespuestaDeConfirmacion = false;

		await contexto.ViewModel.EliminarCommandAsync.ExecuteAsync("Materia");

		_ = contexto.ServicioMateria.DidNotReceiveWithAnyArgs().EliminarMateriaAsync(default!);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Una_baja_que_falla_se_muestra_en_el_dialogo_y_no_lanza()
	{
		var contexto = new Contexto();
		await contexto.SeleccionarMateriaAsync(contexto.CrearMateria());
		contexto.ServicioMateria.EliminarMateriaAsync(Arg.Any<EliminarMateriaRequest>())
								.Returns(Task.FromException(new InvalidOperationException("con catedras")));

		await contexto.ViewModel.EliminarCommandAsync.ExecuteAsync("Materia");

		contexto.Dialogos.Errores.Single().Mensaje.ShouldBe("con catedras");
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Crear_una_curricula_pega_en_IServicioCurricula_con_el_curso_del_store_y_sin_fecha_de_fin()
	{
		var contexto = new Contexto();
		await contexto.SeleccionarCurriculaAsync();

		await contexto.ViewModel.RegistrarCommand.ExecuteAsync("Curricula");

		_ = contexto.ServicioCurricula.Received(1).RegistrarCurriculaAsync(new RegistrarCurriculaRequest(contexto.CursoID, DateTime.Today, null));
		_ = contexto.ServicioMateria.DidNotReceiveWithAnyArgs().RegistrarMateriaAsync(default!);
	}
	#endregion

	#region Carga
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_curriculas_pide_las_del_curso_del_store()
	{
		var contexto = new Contexto();

		await contexto.ViewModel.CargarCurriculasCommandAsync.ExecuteAsync(null);

		_ = contexto.ServicioCurricula.Received(1).ListarCurriculasSegunCursoAsync(new CursoRequest(contexto.CursoID));
		contexto.ViewModel.Curriculas.Count.ShouldBe(1);
		contexto.ViewModel.HabilitarCurriculas.ShouldBeTrue();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Sin_curriculas_se_muestra_la_notificacion_y_no_se_piden_materias()
	{
		var contexto = new Contexto();
		contexto.DevolverCurriculas();

		await contexto.ViewModel.CargarCurriculasCommandAsync.ExecuteAsync(null);

		contexto.ViewModel.HabilitarNotificacion.ShouldBeTrue();
		contexto.ViewModel.HabilitarCurriculas.ShouldBeFalse();
		_ = contexto.ServicioMateria.DidNotReceiveWithAnyArgs().ListarMateriasSegunCurriculaAsync(default!);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_materias_pide_las_de_la_curricula_seleccionada_a_IServicioMateria()
	{
		var contexto = new Contexto();
		contexto.DevolverMaterias(contexto.CrearMateria("A", 1), contexto.CrearMateria("B", 2));
		await contexto.SeleccionarCurriculaAsync();

		await contexto.ViewModel.CargarMateriasCommandAsync.ExecuteAsync(null);

		_ = contexto.ServicioMateria.Received(1).ListarMateriasSegunCurriculaAsync(new ListarMateriasSegunCurriculaRequest(contexto.CursoID, contexto.CurriculaID));
		contexto.ViewModel.Materias.Select(m => m.Descripcion).ShouldBe(new[] { "A", "B" });
		contexto.ViewModel.Materias.Select(m => m.HorasCatedra).ShouldBe(new[] { 1, 2 });
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Los_comandos_de_carga_son_asincronos_y_estan_expuestos_para_el_trigger_de_la_vista()
	{
		var contexto = new Contexto();

		contexto.ViewModel.CargarCurriculasCommandAsync.ShouldBeAssignableTo<IAsyncRelayCommand>();
		contexto.ViewModel.CargarMateriasCommandAsync.ShouldBeAssignableTo<IAsyncRelayCommand>();
	}
	#endregion

	#region Navegacion
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Catedras_navega_al_segundo_servicio_y_no_al_de_cursos()
	{
		var contexto = new Contexto();
		await contexto.SeleccionarMateriaAsync(contexto.CrearMateria());

		contexto.ViewModel.NavigationCommand.Execute("Catedras");

		contexto.NavegarACatedras.Llamadas.ShouldBe(1);
		contexto.NavegarACursos.Llamadas.ShouldBe(0);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Catedras_escribe_la_materia_en_el_store_antes_de_navegar()
	{
		var contexto = new Contexto();
		var materia = contexto.CrearMateria();
		await contexto.SeleccionarMateriaAsync(materia);
		Guid? materiaEnElStore = null;
		contexto.NavegarACatedras.AlNavegar = () => { materiaEnElStore = contexto.MateriaStore.Materia?.MateriaID; };

		contexto.ViewModel.NavigationCommand.Execute("Catedras");

		materiaEnElStore.ShouldBe(materia.MateriaID);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Cursos_navega_al_primer_servicio()
	{
		var contexto = new Contexto();

		contexto.ViewModel.NavigationCommand.Execute("Cursos");

		contexto.NavegarACursos.Llamadas.ShouldBe(1);
		contexto.NavegarACatedras.Llamadas.ShouldBe(0);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_parametro_SituacionRevista_ya_no_existe()
	{
		var contexto = new Contexto();

		contexto.ViewModel.NavigationCommand.CanExecute("SituacionRevista").ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Catedras_no_se_puede_ejecutar_sin_materia_seleccionada_ni_editando()
	{
		var contexto = new Contexto();
		contexto.ViewModel.NavigationCommand.CanExecute("Catedras").ShouldBeFalse();

		await contexto.SeleccionarMateriaAsync(contexto.CrearMateria());
		contexto.ViewModel.NavigationCommand.CanExecute("Catedras").ShouldBeTrue();

		contexto.ViewModel.HabilitarEditarMateria = true;
		contexto.ViewModel.NavigationCommand.CanExecute("Catedras").ShouldBeFalse();
	}
	#endregion

	#region [NotifyCanExecuteChangedFor]: cada propiedad que un CanExecute lee notifica a su comando
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Materia_notifica_a_Navigation_Editar_y_Eliminar()
	{
		var contexto = new Contexto();
		var materia = new MateriaViewModel(contexto.CrearMateria());

		ContarNotificaciones(contexto.ViewModel.NavigationCommand, () => { contexto.ViewModel.Materia = materia; }).ShouldBeGreaterThan(0);
		contexto.ViewModel.Materia = null!;
		ContarNotificaciones(contexto.ViewModel.EditarCommand, () => { contexto.ViewModel.Materia = materia; }).ShouldBeGreaterThan(0);
		contexto.ViewModel.Materia = null!;
		ContarNotificaciones(contexto.ViewModel.EliminarCommandAsync, () => { contexto.ViewModel.Materia = materia; }).ShouldBeGreaterThan(0);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Curricula_notifica_a_Registrar()
	{
		var contexto = new Contexto();
		var curricula = new CurriculaViewModel(new CurriculaResponse(contexto.CursoID, contexto.CurriculaID, DateTime.Today, null));

		ContarNotificaciones(contexto.ViewModel.RegistrarCommand, () => { contexto.ViewModel.Curricula = curricula; }).ShouldBeGreaterThan(0);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void HabilitarEditarMateria_notifica_a_los_seis_comandos_que_la_leen()
	{
		var contexto = new Contexto();
		var vm = contexto.ViewModel;
		var comandos = new IRelayCommand[] { vm.CancelarCommand, vm.EditarCommand, vm.EliminarCommandAsync, vm.GuardarCommandAsync, vm.RegistrarCommand, vm.NavigationCommand };

		foreach (var comando in comandos)
		{
			vm.HabilitarEditarMateria = false;
			ContarNotificaciones(comando, () => { vm.HabilitarEditarMateria = true; }).ShouldBeGreaterThan(0);
		}
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void HabilitarRegistrarMateria_notifica_a_Cancelar_Registrar_y_Navigation()
	{
		var contexto = new Contexto();
		var vm = contexto.ViewModel;
		var comandos = new IRelayCommand[] { vm.CancelarCommand, vm.RegistrarCommand, vm.NavigationCommand };

		foreach (var comando in comandos)
		{
			vm.HabilitarRegistrarMateria = false;
			ContarNotificaciones(comando, () => { vm.HabilitarRegistrarMateria = true; }).ShouldBeGreaterThan(0);
		}
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Descripcion_y_CargaHoraria_notifican_a_Guardar()
	{
		var contexto = new Contexto();

		ContarNotificaciones(contexto.ViewModel.GuardarCommandAsync, () => { contexto.ViewModel.Descripcion = "Algo"; }).ShouldBeGreaterThan(0);
		ContarNotificaciones(contexto.ViewModel.GuardarCommandAsync, () => { contexto.ViewModel.CargaHoraria = 4; }).ShouldBeGreaterThan(0);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Cancelar_se_habilita_con_las_banderas_correspondientes()
	{
		var contexto = new Contexto();
		var vm = contexto.ViewModel;

		vm.CancelarCommand.CanExecute("Nueva").ShouldBeFalse();
		vm.HabilitarRegistrarMateria = true;
		vm.CancelarCommand.CanExecute("Nueva").ShouldBeTrue();

		vm.CancelarCommand.CanExecute("Editar").ShouldBeFalse();
		vm.HabilitarEditarMateria = true;
		vm.CancelarCommand.CanExecute("Editar").ShouldBeTrue();
	}
	#endregion
}
