using Core.ServicioCatedras;
using Core.ServicioCatedras.DTOs.Requests;
using Core.ServicioCatedras.DTOs.Responses;
using NSubstitute;
using Shouldly;
using WPF_Desktop.Store;
using WPF_Desktop.UnitTests.Dobles;
using WPF_Desktop.ViewModels.Cursos.Curriculas.Materias;
using WPF_Desktop.ViewModels.Cursos.Curriculas.Materias.Catedras;
using Xunit;

namespace WPF_Desktop.UnitTests.ViewModels;

/// <summary>
/// Cubre la pantalla nueva <see cref="GestionHorariosViewModel"/>: la carga por la catedra del
/// <see cref="CatedraStore"/>, el alta con la hora parseada del formulario, la baja con confirmacion, el recorrido
/// de "modificar" (quitar + agregar) y que no se exponga ningun DTO de Core.
/// </summary>
/// <remarks>
/// El test de los sesenta minutos es el que fija desde la UI el arreglo de AS-2: la duracion sale de
/// <c>TotalMinutes</c> y no de <c>Minutes</c>, que para una hora catedra de sesenta minutos devuelve cero y deja un
/// <c>QuitarHorarioRequest</c> que no identifica ningun horario.
/// </remarks>
public class GestionHorariosViewModelTests
{
	private sealed class Contexto
	{
		public IServicioCatedra ServicioCatedra { get; } = Substitute.For<IServicioCatedra>();
		public NavegacionFalsa NavegarACatedras { get; } = new();
		public MateriaStore MateriaStore { get; } = new();
		public CatedraStore CatedraStore { get; } = new();
		public DialogServiceFalso Dialogos { get; } = new();
		public Guid CursoID { get; } = Guid.NewGuid();
		public Guid MateriaID { get; } = Guid.NewGuid();
		public Guid CatedraID { get; } = Guid.NewGuid();
		public GestionHorariosViewModel ViewModel { get; }

		public Contexto(bool conCatedra = true, params HorarioResponse[] horarios)
		{
			MateriaStore.Materia = new MateriaSeleccionada(CursoID, MateriaID, "Matematica");

			if (conCatedra)
			{
				CatedraStore.Catedra = CatedraID;
			}

			Devolver(horarios);

			ViewModel = new GestionHorariosViewModel(NavegarACatedras,
													 ServicioCatedra,
													 MateriaStore,
													 CatedraStore,
													 Dialogos);
		}

		public void Devolver(params HorarioResponse[] horarios)
		{
			ServicioCatedra.ListarHorariosSegunCatedraAsync(Arg.Any<ListarHorariosRequest>())
						   .Returns(Task.FromResult<IReadOnlyCollection<HorarioResponse>>(horarios.ToList()));
		}

		/// <summary>
		/// Declara la carga horaria que el listado por materia va a devolver para esta catedra.
		/// </summary>
		/// <remarks>
		/// La pantalla lee el tope por ahi porque <c>IServicioCatedra</c> no expone la lectura de UNA
		/// catedra. Sin esta siembra NSubstitute devuelve una coleccion vacia y la carga queda en cero,
		/// que es el caso de "no se pudo leer el tope".
		/// </remarks>
		public void DevolverCargaHoraria(int? cargaHoraria)
		{
			ServicioCatedra.ListarCatedrasSegunMateriaAsync(Arg.Any<ListarCatedrasSegunMateriaRequest>())
						   .Returns(Task.FromResult<IReadOnlyCollection<CatedraResponse>>(new[]
						   {
							   new CatedraResponse(CatedraID, MateriaID, Guid.NewGuid(), cargaHoraria, 0, 0, null)
						   }));
		}
	}

	private static HorarioResponse CrearHorario(string turno, string dia, TimeOnly inicio, TimeOnly fin)
	{
		return new HorarioResponse(turno, dia, inicio, fin);
	}

	#region Carga
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_pide_los_horarios_de_la_catedra_del_store()
	{
		var contexto = new Contexto();

		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		_ = contexto.ServicioCatedra.Received(1).ListarHorariosSegunCatedraAsync(new ListarHorariosRequest(contexto.CatedraID));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_mapea_cada_HorarioResponse_a_un_HorarioViewModel()
	{
		var contexto = new Contexto(true,
									CrearHorario("Tarde", "Lunes", new TimeOnly(14, 0), new TimeOnly(14, 40)),
									CrearHorario("Tarde", "Martes", new TimeOnly(15, 0), new TimeOnly(15, 40)));

		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.ViewModel.Horarios.Count.ShouldBe(2);
		contexto.ViewModel.Horarios[0].ShouldBeOfType<HorarioViewModel>();
		contexto.ViewModel.Horarios[0].Dia.ShouldBe("Lunes");
		contexto.ViewModel.Horarios[1].Dia.ShouldBe("Martes");
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_dos_veces_no_duplica_los_horarios_y_limpia_la_seleccion()
	{
		var contexto = new Contexto(true, CrearHorario("Tarde", "Lunes", new TimeOnly(14, 0), new TimeOnly(14, 40)));

		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);
		contexto.ViewModel.Horario = contexto.ViewModel.Horarios[0];
		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.ViewModel.Horarios.Count.ShouldBe(1);
		contexto.ViewModel.Horario.ShouldBeNull();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Sin_catedra_en_el_store_la_carga_no_esta_habilitada()
	{
		var contexto = new Contexto(conCatedra: false);

		contexto.CatedraStore.Catedra.ShouldBe(Guid.Empty);
		contexto.ViewModel.CargarCommand.CanExecute(null).ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task La_duracion_de_la_fila_sale_de_TotalMinutes_y_no_de_Minutes()
	{
		// AS-2: una hora catedra de SESENTA minutos es un TimeSpan de 01:00:00, cuyo Minutes es 0.
		var contexto = new Contexto(true, CrearHorario("Tarde", "Lunes", new TimeOnly(14, 0), new TimeOnly(15, 0)));

		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.ViewModel.Horarios[0].DuracionMinutos.ShouldBe(60);
		contexto.ViewModel.Horarios[0].HoraInicioValor.ShouldBe(new TimeOnly(14, 0));
	}
	#endregion

	#region Opciones de los combos
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Los_dias_ofrecidos_son_cinco_y_no_incluyen_el_fin_de_semana()
	{
		// El dominio rechaza sabado y domingo: no se ofrecen. Es presentacion, no validacion duplicada.
		var contexto = new Contexto();

		contexto.ViewModel.Dias.Count.ShouldBe(5);
		contexto.ViewModel.Dias.ShouldContain("Lunes");
		contexto.ViewModel.Dias.ShouldContain("Viernes");
		contexto.ViewModel.Dias.ShouldNotContain("Sabado");
		contexto.ViewModel.Dias.ShouldNotContain("Domingo");
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Los_turnos_ofrecidos_son_los_tres_del_dominio()
	{
		var contexto = new Contexto();

		contexto.ViewModel.Turnos.Count.ShouldBe(3);
		contexto.ViewModel.Turnos.ShouldContain("Tarde");
		contexto.ViewModel.Turnos.ShouldContain("Noche");
	}
	#endregion

	#region Presupuesto de la catedra
	/// <summary>
	/// El tope de la catedra se LEE, no se descubre fallando.
	/// </summary>
	/// <remarks>
	/// <c>Catedra.AgregarHorario</c> rechaza con <c>HorasCatedraCompletasException</c> cuando
	/// <c>HorasAsignadas</c> —la cantidad de horarios cargados— llega a <c>CargaHoraria</c>. La pantalla
	/// no mostraba ninguno de los dos numeros, asi que se cargaban horarios hasta que uno reventaba.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_trae_la_carga_horaria_de_la_catedra_y_calcula_lo_que_falta()
	{
		var contexto = new Contexto(true,
									CrearHorario("Tarde", "Lunes", new TimeOnly(14, 0), new TimeOnly(14, 40)),
									CrearHorario("Tarde", "Martes", new TimeOnly(15, 0), new TimeOnly(15, 40)));
		contexto.DevolverCargaHoraria(5);

		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.ViewModel.CargaHoraria.ShouldBe(5);
		contexto.ViewModel.HorasConHorario.ShouldBe(2);
		contexto.ViewModel.HorasSinAsignar.ShouldBe(3);
		contexto.ViewModel.HorasCompletas.ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Con_la_semana_completa_el_alta_se_apaga_en_lugar_de_fallar()
	{
		var contexto = new Contexto(true,
									CrearHorario("Tarde", "Lunes", new TimeOnly(14, 0), new TimeOnly(14, 40)),
									CrearHorario("Tarde", "Martes", new TimeOnly(15, 0), new TimeOnly(15, 40)));
		contexto.DevolverCargaHoraria(2);

		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		// Formulario entero y valido: lo unico que apaga el boton es el tope.
		contexto.ViewModel.Turno = "Tarde";
		contexto.ViewModel.Dia = "Miercoles";
		contexto.ViewModel.HoraInicio = "16:00";
		contexto.ViewModel.DuracionMinutos = 40;

		contexto.ViewModel.HorasSinAsignar.ShouldBe(0);
		contexto.ViewModel.HorasCompletas.ShouldBeTrue();
		contexto.ViewModel.AgregarHorarioCommand.CanExecute(null).ShouldBeFalse();
	}

	/// <summary>
	/// Sin carga horaria conocida la pantalla FALLA ABIERTA: deja intentar y que conteste el dominio.
	/// </summary>
	/// <remarks>
	/// <c>CatedraResponse.CargaHoraria</c> es opcional. Apagar el alta sobre un dato que no se tiene
	/// seria mas estricto que el dominio, que es el defecto que docs/ui-ux.md ya corrigio en los cuatro
	/// botones de las designaciones.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Sin_carga_horaria_conocida_el_alta_sigue_disponible()
	{
		var contexto = new Contexto(true,
									CrearHorario("Tarde", "Lunes", new TimeOnly(14, 0), new TimeOnly(14, 40)));
		contexto.DevolverCargaHoraria(null);

		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.ViewModel.Turno = "Tarde";
		contexto.ViewModel.Dia = "Martes";
		contexto.ViewModel.HoraInicio = "15:00";
		contexto.ViewModel.DuracionMinutos = 40;

		contexto.ViewModel.CargaHoraria.ShouldBe(0);
		contexto.ViewModel.HorasCompletas.ShouldBeFalse();
		contexto.ViewModel.AgregarHorarioCommand.CanExecute(null).ShouldBeTrue();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task El_presupuesto_se_pide_en_la_misma_cadena_que_los_horarios_y_no_en_paralelo()
	{
		var contexto = new Contexto();
		contexto.DevolverCargaHoraria(4);

		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		// Una sola lectura de cada cosa por carga: el DbContext no admite dos consultas en vuelo.
		_ = contexto.ServicioCatedra.Received(1).ListarHorariosSegunCatedraAsync(Arg.Any<ListarHorariosRequest>());
		_ = contexto.ServicioCatedra.Received(1).ListarCatedrasSegunMateriaAsync(Arg.Any<ListarCatedrasSegunMateriaRequest>());
	}
	#endregion

	#region AgregarHorarioCommand
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Agregar_manda_la_catedra_del_store_la_hora_parseada_y_la_duracion_del_formulario()
	{
		var contexto = new Contexto();
		contexto.ViewModel.Turno = "Tarde";
		contexto.ViewModel.Dia = "Lunes";
		contexto.ViewModel.HoraInicio = "14:00";
		contexto.ViewModel.DuracionMinutos = 40;

		contexto.ViewModel.AgregarHorarioCommand.CanExecute(null).ShouldBeTrue();

		await contexto.ViewModel.AgregarHorarioCommand.ExecuteAsync(null);

		_ = contexto.ServicioCatedra.Received(1).AgregarHorarioAsync(new AgregarHorarioRequest(contexto.CatedraID,
																							   "Tarde",
																							   "Lunes",
																							   new TimeOnly(14, 0),
																							   40));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Agregar_avisa_y_recarga_la_grilla()
	{
		var contexto = new Contexto();
		contexto.ViewModel.Turno = "Tarde";
		contexto.ViewModel.Dia = "Lunes";
		contexto.ViewModel.HoraInicio = "14:00";

		await contexto.ViewModel.AgregarHorarioCommand.ExecuteAsync(null);

		contexto.Dialogos.Informaciones.Count.ShouldBe(1);
		_ = contexto.ServicioCatedra.Received(1).ListarHorariosSegunCatedraAsync(Arg.Any<ListarHorariosRequest>());
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Una_hora_con_formato_invalido_no_habilita_el_comando_ni_llega_a_Core()
	{
		var contexto = new Contexto();
		contexto.ViewModel.Turno = "Tarde";
		contexto.ViewModel.Dia = "Lunes";
		contexto.ViewModel.HoraInicio = "25:99";

		contexto.ViewModel.AgregarHorarioCommand.CanExecute(null).ShouldBeFalse();

		await contexto.ViewModel.AgregarHorarioCommand.ExecuteAsync(null);

		_ = contexto.ServicioCatedra.DidNotReceive().AgregarHorarioAsync(Arg.Any<AgregarHorarioRequest>());
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Sin_turno_ni_dia_elegidos_el_comando_de_agregar_no_esta_habilitado()
	{
		var contexto = new Contexto();
		contexto.ViewModel.HoraInicio = "14:00";

		contexto.ViewModel.AgregarHorarioCommand.CanExecute(null).ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Un_error_del_dominio_al_agregar_llega_al_dialogo_con_el_titulo_de_la_pantalla()
	{
		var contexto = new Contexto();
		contexto.ViewModel.Turno = "Tarde";
		contexto.ViewModel.Dia = "Lunes";
		contexto.ViewModel.HoraInicio = "14:00";
		contexto.ServicioCatedra.AgregarHorarioAsync(Arg.Any<AgregarHorarioRequest>())
								.Returns(Task.FromException(new InvalidOperationException("el horario se superpone")));

		await contexto.ViewModel.AgregarHorarioCommand.ExecuteAsync(null);

		contexto.Dialogos.Errores.Count.ShouldBe(1);
		contexto.Dialogos.Errores[0].Mensaje.ShouldBe("el horario se superpone");
		contexto.Dialogos.Errores[0].Titulo.ShouldBe("Horarios");
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Un_error_del_servicio_al_cargar_llega_al_dialogo_con_el_titulo_de_la_pantalla()
	{
		var contexto = new Contexto();
		contexto.ServicioCatedra.ListarHorariosSegunCatedraAsync(Arg.Any<ListarHorariosRequest>())
								.Returns(Task.FromException<IReadOnlyCollection<HorarioResponse>>(new InvalidOperationException("sin conexion")));

		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.Dialogos.Errores.Count.ShouldBe(1);
		contexto.Dialogos.Errores[0].Mensaje.ShouldBe("sin conexion");
		contexto.Dialogos.Errores[0].Titulo.ShouldBe("Horarios");
	}
	#endregion

	#region QuitarHorarioCommand
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Sin_fila_elegida_el_comando_de_quitar_no_esta_habilitado()
	{
		var contexto = new Contexto();

		contexto.ViewModel.QuitarHorarioCommand.CanExecute(null).ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Quitar_pide_confirmacion_y_si_el_usuario_no_acepta_no_llama_a_Core()
	{
		var contexto = new Contexto(true, CrearHorario("Tarde", "Lunes", new TimeOnly(14, 0), new TimeOnly(15, 0)));
		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);
		contexto.ViewModel.Horario = contexto.ViewModel.Horarios[0];
		contexto.Dialogos.RespuestaDeConfirmacion = false;

		await contexto.ViewModel.QuitarHorarioCommand.ExecuteAsync(null);

		contexto.Dialogos.Confirmaciones.Count.ShouldBe(1);
		_ = contexto.ServicioCatedra.DidNotReceive().QuitarHorarioAsync(Arg.Any<QuitarHorarioRequest>());
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Quitar_aceptado_manda_el_turno_el_dia_el_TimeOnly_de_inicio_y_la_duracion_de_la_fila_elegida()
	{
		var contexto = new Contexto(true, CrearHorario("Tarde", "Lunes", new TimeOnly(14, 0), new TimeOnly(15, 0)));
		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);
		contexto.ViewModel.Horario = contexto.ViewModel.Horarios[0];
		contexto.Dialogos.RespuestaDeConfirmacion = true;

		contexto.ViewModel.QuitarHorarioCommand.CanExecute(null).ShouldBeTrue();

		await contexto.ViewModel.QuitarHorarioCommand.ExecuteAsync(null);

		_ = contexto.ServicioCatedra.Received(1).QuitarHorarioAsync(new QuitarHorarioRequest(contexto.CatedraID,
																							 "Tarde",
																							 "Lunes",
																							 new TimeOnly(14, 0),
																							 60));
		contexto.Dialogos.Informaciones.Count.ShouldBe(1);
	}
	#endregion

	#region Modificar = quitar + agregar
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Elegir_una_fila_copia_sus_datos_al_formulario()
	{
		// Es todo el recorrido de "modificar": no hay operacion atomica en Core, el usuario corrige el formulario
		// y confirma los dos pasos, primero quitar y despues agregar.
		var contexto = new Contexto(true, CrearHorario("Noche", "Jueves", new TimeOnly(19, 30), new TimeOnly(20, 30)));
		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.ViewModel.Horario = contexto.ViewModel.Horarios[0];

		contexto.ViewModel.Turno.ShouldBe("Noche");
		contexto.ViewModel.Dia.ShouldBe("Jueves");
		contexto.ViewModel.HoraInicio.ShouldBe("19:30");
		contexto.ViewModel.DuracionMinutos.ShouldBe(60);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Limpiar_la_seleccion_no_vacia_el_formulario()
	{
		// CargarHorariosAsync deja la seleccion en nulo despues de recargar: si el hook vaciara el formulario,
		// borraria lo que el usuario acaba de escribir.
		var contexto = new Contexto(true, CrearHorario("Noche", "Jueves", new TimeOnly(19, 30), new TimeOnly(20, 30)));
		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);
		contexto.ViewModel.Horario = contexto.ViewModel.Horarios[0];

		contexto.ViewModel.Horario = null;

		contexto.ViewModel.Turno.ShouldBe("Noche");
		contexto.ViewModel.HoraInicio.ShouldBe("19:30");
	}
	#endregion

	#region Navegacion
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Volver_navega_una_sola_vez_y_no_toca_el_CatedraStore()
	{
		var contexto = new Contexto();

		contexto.ViewModel.VolverCommand.Execute(null);

		contexto.NavegarACatedras.Llamadas.ShouldBe(1);
		contexto.CatedraStore.Catedra.ShouldBe(contexto.CatedraID);
	}
	#endregion

	#region Contrato del ViewModel
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_ViewModel_no_expone_ningun_tipo_de_Core_como_propiedad_bindeable()
	{
		// Violacion #5 del plan: ObservableCollection<XxxResponse> / XxxResponse como propiedad publica.
		var propiedades = typeof(GestionHorariosViewModel).GetProperties();

		foreach (var propiedad in propiedades)
		{
			var tipos = new[] { propiedad.PropertyType }.Concat(propiedad.PropertyType.GetGenericArguments());

			foreach (var tipo in tipos)
			{
				(tipo.Namespace ?? string.Empty).StartsWith("Core.").ShouldBeFalse($"{propiedad.Name} expone el tipo de Core {tipo.FullName}");
			}
		}
	}
	#endregion
}
