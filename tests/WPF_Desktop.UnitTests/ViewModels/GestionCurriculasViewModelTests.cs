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
			CursoStore.Curso = new CursoSeleccionado(CursoID, Grado.Primero.ToString(), NivelEducativo.Secundaria.ToString());

			DevolverCurriculas(new CurriculaResponse(CursoID, CurriculaID, new DateTime(2024, 3, 1), null, Vigente: true));
			DevolverMaterias();

			// Una currícula holgada por defecto: elegir una currícula consulta su ocupación, y sin
			// este doble todos los casos recibirían un cupo nulo y un error que no están probando.
			DevolverOcupacion(espaciosOcupados: 3, horasOcupadas: 9);

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

		/// <summary>Los topes son los reales del dominio: 14 espacios y 40 horas cátedra semanales.</summary>
		public void DevolverOcupacion(int espaciosOcupados, int horasOcupadas)
		{
			ServicioCurricula.ConsultarOcupacionAsync(Arg.Any<CurriculaRequest>())
							 .Returns(Task.FromResult(new OcupacionCurricularResponse(EspaciosOcupados: espaciosOcupados,
																					  EspaciosMaximos: 14,
																					  HorasOcupadas: horasOcupadas,
																					  HorasMaximas: 40)));
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
		/// <remarks>
		/// Termina llamando a <c>CargarMateriasCommandAsync</c> porque es lo que hace la pantalla:
		/// elegir una fila dispara ese comando desde el <c>SelectionChanged</c> de la grilla, y es
		/// esa misma cadena la que después pide la ocupación de la currícula. El setter de
		/// <c>Curricula</c> no consulta nada por su cuenta —dos consultas en vuelo chocan contra el
		/// mismo <c>DbContext</c>—, así que un helper que sólo asignara la propiedad dejaría el cupo
		/// sin cargar y no se parecería al recorrido real.
		/// </remarks>
		public async Task SeleccionarCurriculaAsync()
		{
			await ViewModel.CargarCurriculasCommandAsync.ExecuteAsync(null);
			ViewModel.Curricula = ViewModel.Curriculas[0];
			await ViewModel.CargarMateriasCommandAsync.ExecuteAsync(null);
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

	/// <summary>
	/// Un alta rechazada muestra el error y CONSERVA lo que el usuario había escrito.
	/// </summary>
	/// <remarks>
	/// Antes el <c>finally</c> vaciaba descripción y carga horaria pasara lo que pasara, y el
	/// <c>catch</c> cerraba el formulario. Los dos motivos de rechazo son el nombre repetido y el tope
	/// de la currícula, y en los dos lo que corresponde es corregir el dato que está a la vista: tirar
	/// el formulario obligaba a escribir la materia de nuevo para cambiarle un número.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Un_alta_rechazada_conserva_lo_que_el_usuario_escribio()
	{
		var contexto = new Contexto();
		await contexto.SeleccionarCurriculaAsync();
		contexto.ServicioMateria.RegistrarMateriaAsync(Arg.Any<RegistrarMateriaRequest>())
								.Returns(Task.FromException<Guid>(new InvalidOperationException("duplicada")));

		// Se abre el alta como lo hace el botón: es el recorrido que deja el formulario a la vista.
		await contexto.ViewModel.RegistrarCommand.ExecuteAsync("Materia");
		contexto.ViewModel.Descripcion = "Quimica";
		contexto.ViewModel.CargaHoraria = 5;

		await contexto.ViewModel.GuardarCommandAsync.ExecuteAsync("Materia");

		contexto.Dialogos.Errores.Single().Mensaje.ShouldBe("duplicada");
		contexto.ViewModel.Descripcion.ShouldBe("Quimica");
		contexto.ViewModel.CargaHoraria.ShouldBe(5);
		contexto.ViewModel.HabilitarRegistrarMateria.ShouldBeTrue("el formulario queda abierto para corregir");
	}

	/// <summary>
	/// Un alta exitosa vacía el formulario y devuelve a la vista la tarjeta de currículas.
	/// </summary>
	/// <remarks>
	/// Es la guarda de la pantalla que se quedaba en blanco: abrir el alta colapsa la tarjeta —su
	/// <c>Visibility</c> cuelga de <c>HabilitarCurriculas</c>— y nadie la volvía a encender, así que
	/// después de agregar una materia quedaba el encabezado y nada debajo.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Un_alta_exitosa_cierra_el_formulario_y_devuelve_la_tarjeta()
	{
		var contexto = new Contexto();
		await contexto.SeleccionarCurriculaAsync();
		await contexto.ViewModel.RegistrarCommand.ExecuteAsync("Materia");
		contexto.ViewModel.HabilitarCurriculas.ShouldBeFalse("el alta oculta la tarjeta mientras dura");
		contexto.ViewModel.Descripcion = "Quimica";

		await contexto.ViewModel.GuardarCommandAsync.ExecuteAsync("Materia");

		contexto.ViewModel.HabilitarRegistrarMateria.ShouldBeFalse();
		contexto.ViewModel.HabilitarCurriculas.ShouldBeTrue();
		contexto.ViewModel.Descripcion.ShouldBeEmpty();
	}

	/// <summary>Cancelar el alta también devuelve la tarjeta.</summary>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cancelar_el_alta_devuelve_la_tarjeta()
	{
		var contexto = new Contexto();
		await contexto.SeleccionarCurriculaAsync();
		await contexto.ViewModel.RegistrarCommand.ExecuteAsync("Materia");

		await contexto.ViewModel.CancelarCommand.ExecuteAsync("Nueva");

		contexto.ViewModel.HabilitarRegistrarMateria.ShouldBeFalse();
		contexto.ViewModel.HabilitarCurriculas.ShouldBeTrue();
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

	/// <summary>
	/// Un curso sin currículas deja la tarjeta a la vista con la grilla vacía.
	/// </summary>
	/// <remarks>
	/// Antes colapsaba la tarjeta entera y la reemplazaba por un aviso, con lo cual el estado vacío de
	/// la grilla —que ya trae su frase y dice qué hacer— era código inalcanzable. El contrato de
	/// <c>docs/ui-ux.md</c> es que toda grilla declara su vacío y que ese vacío se vea.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Sin_curriculas_la_grilla_queda_vacia_y_a_la_vista()
	{
		var contexto = new Contexto();
		contexto.DevolverCurriculas();

		await contexto.ViewModel.CargarCurriculasCommandAsync.ExecuteAsync(null);

		contexto.ViewModel.Curriculas.ShouldBeEmpty();
		contexto.ViewModel.HabilitarCurriculas.ShouldBeTrue();
		contexto.ViewModel.HabilitarNotificacion.ShouldBeFalse();
		_ = contexto.ServicioMateria.DidNotReceiveWithAnyArgs().ListarMateriasSegunCurriculaAsync(default!);
	}

	/// <summary>
	/// La ocupación de la currícula elegida se consulta sola y apaga el alta cuando no entra nada más.
	/// </summary>
	/// <remarks>
	/// Es el arreglo del tope invisible: <c>ServicioMateria</c> calculaba estos dos números justo
	/// antes de rechazar un alta y los descartaba, así que el usuario descubría el límite chocándose.
	/// </remarks>
	[Theory]
	[Trait("Categoria", "Unidad")]
	[InlineData(3, 9, true)]
	[InlineData(14, 9, false)]
	[InlineData(3, 40, false)]
	public async Task El_cupo_de_la_curricula_decide_si_se_puede_agregar_otra_materia(int espacios, int horas, bool admite)
	{
		var contexto = new Contexto();
		contexto.DevolverOcupacion(espaciosOcupados: espacios, horasOcupadas: horas);

		await contexto.SeleccionarCurriculaAsync();

		contexto.ViewModel.EspaciosOcupados.ShouldBe(espacios);
		contexto.ViewModel.EspaciosMaximos.ShouldBe(14);
		contexto.ViewModel.HorasOcupadas.ShouldBe(horas);
		contexto.ViewModel.HorasMaximas.ShouldBe(40);
		contexto.ViewModel.AdmiteOtraMateria.ShouldBe(admite);
		contexto.ViewModel.RegistrarCommand.CanExecute("Materia").ShouldBe(admite);
	}

	/// <summary>
	/// El combo del alta ofrece sólo las horas que la currícula todavía admite.
	/// </summary>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task El_combo_de_horas_se_recorta_a_las_que_entran()
	{
		var contexto = new Contexto();
		contexto.DevolverOcupacion(espaciosOcupados: 3, horasOcupadas: 38);

		await contexto.SeleccionarCurriculaAsync();

		// Quedan 2 horas libres: el tope de 5 por materia deja de mandar.
		contexto.ViewModel.OpcionesCargaHoraria.ShouldBe(new[] { 1, 2 });
	}

	/// <summary>
	/// Sobre una currícula cerrada no se puede agregar, editar ni quitar materias.
	/// </summary>
	/// <remarks>
	/// <c>ServicioMateria</c> exige currícula vigente para las tres operaciones. La grilla lista
	/// también las cerradas —son el registro de los años anteriores— y sobre ellas los botones
	/// quedaban encendidos hasta que Core contestaba <c>CurriculaNoVigenteException</c>.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Una_curricula_cerrada_no_acepta_cambios_en_sus_materias()
	{
		var contexto = new Contexto();
		var cerradaID = Guid.NewGuid();
		contexto.DevolverCurriculas(new CurriculaResponse(contexto.CursoID, cerradaID, new DateTime(2020, 3, 1), new DateTime(2023, 12, 31), Vigente: false));
		contexto.DevolverMaterias(contexto.CrearMateria("Historia"));

		await contexto.ViewModel.CargarCurriculasCommandAsync.ExecuteAsync(null);
		contexto.ViewModel.Curricula = contexto.ViewModel.Curriculas.Single();
		await contexto.ViewModel.CargarMateriasCommandAsync.ExecuteAsync(null);
		contexto.ViewModel.Materia = contexto.ViewModel.Materias.Single();

		contexto.ViewModel.CurriculaVigente.ShouldBeFalse();
		contexto.ViewModel.RegistrarCommand.CanExecute("Materia").ShouldBeFalse();
		contexto.ViewModel.EditarCommand.CanExecute("Materia").ShouldBeFalse();
		contexto.ViewModel.EliminarCommandAsync.CanExecute("Materia").ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_materias_pide_las_de_la_curricula_seleccionada_a_IServicioMateria()
	{
		var contexto = new Contexto();
		contexto.DevolverMaterias(contexto.CrearMateria("A", 1), contexto.CrearMateria("B", 2));

		// Elegir la currícula ES lo que pide sus materias: el helper hace el mismo recorrido que la
		// grilla, que dispara CargarMateriasCommandAsync desde su SelectionChanged.
		await contexto.SeleccionarCurriculaAsync();

		_ = contexto.ServicioMateria.Received(1).ListarMateriasSegunCurriculaAsync(new ListarMateriasSegunCurriculaRequest(contexto.CursoID, contexto.CurriculaID));
		contexto.ViewModel.Materias.Select(m => m.Descripcion).ShouldBe(new[] { "A", "B" });
		contexto.ViewModel.Materias.Select(m => m.HorasCatedra).ShouldBe(new int?[] { 1, 2 });
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
		var curricula = new CurriculaViewModel(new CurriculaResponse(contexto.CursoID, contexto.CurriculaID, DateTime.Today, null, Vigente: true));

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
