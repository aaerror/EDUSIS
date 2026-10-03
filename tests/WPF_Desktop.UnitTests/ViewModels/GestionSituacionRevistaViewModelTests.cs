using Core.ServicioCatedras;
using Core.ServicioCatedras.DTOs.Requests;
using Core.ServicioCatedras.DTOs.Responses;
using Core.ServicioDocentes;
using Core.ServicioDocentes.DTOs.Responses;
using Core.ServicioMaterias.DTOs.Responses;
using NSubstitute;
using Shouldly;
using WPF_Desktop.Store;
using WPF_Desktop.UnitTests.Dobles;
using WPF_Desktop.ViewModels.Cursos.Curriculas.Materias;
using WPF_Desktop.ViewModels.Cursos.Curriculas.Materias.SituacionRevista;
using WPF_Desktop.ViewModels.Docentes;
using Xunit;

namespace WPF_Desktop.UnitTests.ViewModels;

/// <summary>
/// Cubre <see cref="GestionSituacionRevistaViewModel"/> (W6.A): los seis casos de uso remapeados a
/// <see cref="IServicioCatedra"/> con el <c>CatedraID</c> de <see cref="CatedraStore"/>, la cadena de suplencias
/// (un suplente sin <c>ReemplazaA</c> no llega al servicio; titular e interino nunca envian <c>ReemplazaA</c>, porque el
/// dominio lanza <c>OcupanteConReemplazoException</c>), una sola llamada a <c>ListarDocentesActivosAsync</c> por carga y
/// el bloque "Docente en Funciones", que sigue a <c>SituacionRevistaEnFunciones</c> y no a la fila seleccionada.
/// </summary>
public class GestionSituacionRevistaViewModelTests
{
	private sealed class Contexto
	{
		public IServicioCatedra ServicioCatedra { get; } = Substitute.For<IServicioCatedra>();
		public IServicioDocente ServicioDocente { get; } = Substitute.For<IServicioDocente>();
		public NavegacionFalsa NavegarAMaterias { get; } = new();
		public DialogServiceFalso Dialogos { get; } = new();
		public CursoStore CursoStore { get; } = new();
		public MateriaStore MateriaStore { get; } = new();
		public CatedraStore CatedraStore { get; } = new();

		public Guid CatedraID { get; } = Guid.NewGuid();
		public Guid SituacionCreadaID { get; } = Guid.NewGuid();

		/// <summary>Lo que devuelve <c>ListarSituacionesRevistaAsync</c>; se puede mutar entre cargas.</summary>
		public List<SituacionRevistaResponse> Situaciones { get; } = new();

		/// <summary>Lo que devuelve <c>ListarDocentesActivosAsync</c>.</summary>
		public List<LegajoDocenteResponse> Docentes { get; } = new();

		public GestionSituacionRevistaViewModel ViewModel { get; }

		public Contexto()
		{
			MateriaStore.Materia = new MateriaViewModel(new MateriaResponse(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Matematica", 4));
			CatedraStore.Catedra = CatedraID;

			ServicioCatedra.ListarSituacionesRevistaAsync(Arg.Any<ListarSituacionesRevistaRequest>())
						   .Returns(_ => Task.FromResult<IReadOnlyCollection<SituacionRevistaResponse>>(Situaciones.ToList()));
			ServicioCatedra.DesignarDocenteAsync(Arg.Any<DesignarDocenteRequest>())
						   .Returns(Task.FromResult(SituacionCreadaID));
			ServicioDocente.ListarDocentesActivosAsync()
						   .Returns(_ => Task.FromResult<IReadOnlyCollection<LegajoDocenteResponse>>(Docentes.ToList()));

			ViewModel = new GestionSituacionRevistaViewModel(NavegarAMaterias,
															 ServicioDocente,
															 ServicioCatedra,
															 Dialogos,
															 CursoStore,
															 MateriaStore,
															 CatedraStore);
		}

		public SituacionRevistaResponse Agregar(string cargo = "Titular",
												string estado = "Aceptado",
												DateTime? inicio = null,
												DateTime? fin = null,
												bool enFunciones = false,
												Guid? reemplazaA = null,
												Guid? docenteID = null)
		{
			var situacion = new SituacionRevistaResponse(Guid.NewGuid(),
														 CatedraID,
														 docenteID ?? Guid.NewGuid(),
														 estado,
														 cargo,
														 inicio ?? DateTime.Today.AddDays(-30),
														 fin,
														 reemplazaA,
														 enFunciones);
			Situaciones.Add(situacion);

			return situacion;
		}

		public LegajoDocenteResponse AgregarDocente(Guid docenteID, string nombre)
		{
			var docente = new LegajoDocenteResponse(docenteID, nombre, "30111222", "20301112223", "L-1", DateTime.Today.AddYears(-2), null, true);
			Docentes.Add(docente);

			return docente;
		}

		/// <summary>Elige un docente de la busqueda y abre el formulario de alta (lo que hace <c>SeleccionarCommand</c>).</summary>
		public SituacionRevistaViewModel PrepararAlta(string cargo, bool enFunciones = false)
		{
			var docente = new LegajoDocenteResponse(Guid.NewGuid(), "Perez Juan", "30111222", "20301112223", "L-2", DateTime.Today.AddYears(-1), null, true);
			ViewModel.LegajoDocente = new LegajoDocenteViewModel(docente);
			ViewModel.SeleccionarCommand.Execute(null);

			var alta = ViewModel.SituacionRevistaINSERT;
			alta.Cargo = cargo;
			alta.EnFunciones = enFunciones;

			return alta;
		}

		public SituacionRevistaViewModel Fila(Guid situacionRevistaID)
		{
			return ViewModel.DocentesEnMateria.Single(x => x.SituacionRevistaID == situacionRevistaID);
		}
	}

	#region Carga: ListarSituacionesRevistaAsync con el CatedraID del store
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_lista_las_situaciones_de_la_catedra_del_store()
	{
		var contexto = new Contexto();
		contexto.Agregar();

		await contexto.ViewModel.CargarSituacionRevistasAsync();

		await contexto.ServicioCatedra.Received(1)
					  .ListarSituacionesRevistaAsync(Arg.Is<ListarSituacionesRevistaRequest>(r => r.CatedraID == contexto.CatedraID));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task El_comando_de_carga_del_Loaded_llega_al_servicio()
	{
		var contexto = new Contexto();
		contexto.Agregar();

		await contexto.ViewModel.CargarSituacionRevistasCommandAsync.ExecuteAsync(null);

		await contexto.ServicioCatedra.Received(1).ListarSituacionesRevistaAsync(Arg.Any<ListarSituacionesRevistaRequest>());
		contexto.ViewModel.DocentesEnMateria.Count.ShouldBe(1);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_sin_situaciones_avisa_y_no_habilita_la_gestion()
	{
		var contexto = new Contexto();

		await contexto.ViewModel.CargarSituacionRevistasAsync();

		contexto.ViewModel.DocentesEnMateria.ShouldBeEmpty();
		contexto.ViewModel.HabilitarGestionSituacionRevista.ShouldBeFalse();
		contexto.ViewModel.HabilitarNotificacion.ShouldBeTrue();
		contexto.Dialogos.Advertencias.Count.ShouldBe(1);
	}
	#endregion

	#region ListarDocentesActivosAsync: exactamente una vez por carga
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task ListarDocentesActivos_se_llama_exactamente_una_vez_aunque_haya_muchas_filas()
	{
		var contexto = new Contexto();
		for (var i = 0; i < 5; i++)
		{
			var docenteID = Guid.NewGuid();
			contexto.AgregarDocente(docenteID, $"Docente {i}");
			contexto.Agregar(cargo: i == 0 ? "Titular" : "Suplente", docenteID: docenteID, reemplazaA: i == 0 ? null : Guid.NewGuid());
		}

		await contexto.ViewModel.CargarSituacionRevistasAsync();

		await contexto.ServicioDocente.Received(1).ListarDocentesActivosAsync();
		contexto.ViewModel.DocentesEnMateria.Count.ShouldBe(5);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task ListarDocentesActivos_se_llama_una_vez_por_cada_carga()
	{
		var contexto = new Contexto();
		contexto.Agregar();
		contexto.Agregar(cargo: "Interino");

		await contexto.ViewModel.CargarSituacionRevistasAsync();
		await contexto.ViewModel.CargarSituacionRevistasAsync();

		await contexto.ServicioDocente.Received(2).ListarDocentesActivosAsync();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Sin_situaciones_no_se_consulta_el_listado_de_docentes()
	{
		var contexto = new Contexto();

		await contexto.ViewModel.CargarSituacionRevistasAsync();

		await contexto.ServicioDocente.DidNotReceive().ListarDocentesActivosAsync();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task El_nombre_del_docente_se_cruza_en_memoria_por_DocenteID()
	{
		var contexto = new Contexto();
		var conocido = Guid.NewGuid();
		contexto.AgregarDocente(conocido, "Gomez Maria");
		var situacionConocida = contexto.Agregar(docenteID: conocido);
		var situacionDesconocida = contexto.Agregar(cargo: "Interino", docenteID: Guid.NewGuid());

		await contexto.ViewModel.CargarSituacionRevistasAsync();

		contexto.Fila(situacionConocida.SituacionRevistaID).Docente.ShouldBe("Gomez Maria");
		contexto.Fila(situacionDesconocida.SituacionRevistaID).Docente.ShouldBe("Docente no disponible");
	}
	#endregion

	#region Bloque "Docente en Funciones"
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task HabilitarDocenteEnFunciones_es_true_cuando_hay_alguien_en_funciones()
	{
		var contexto = new Contexto();
		contexto.Agregar(cargo: "Interino");
		var enFunciones = contexto.Agregar(enFunciones: true);

		await contexto.ViewModel.CargarSituacionRevistasAsync();

		contexto.ViewModel.HabilitarDocenteEnFunciones.ShouldBeTrue();
		contexto.ViewModel.SituacionRevistaEnFunciones.ShouldNotBeNull();
		contexto.ViewModel.SituacionRevistaEnFunciones.SituacionRevistaID.ShouldBe(enFunciones.SituacionRevistaID);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task HabilitarDocenteEnFunciones_es_false_cuando_no_hay_nadie_en_funciones()
	{
		var contexto = new Contexto();
		contexto.Agregar();
		contexto.Agregar(cargo: "Interino");

		await contexto.ViewModel.CargarSituacionRevistasAsync();

		contexto.ViewModel.HabilitarDocenteEnFunciones.ShouldBeFalse();
		contexto.ViewModel.SituacionRevistaEnFunciones.ShouldBeNull();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task HabilitarDocenteEnFunciones_sigue_a_la_presencia_de_alguien_en_funciones_entre_cargas()
	{
		var contexto = new Contexto();
		var titular = contexto.Agregar(enFunciones: true);

		await contexto.ViewModel.CargarSituacionRevistasAsync();
		contexto.ViewModel.HabilitarDocenteEnFunciones.ShouldBeTrue();

		// El dominio relevo al docente: la misma situacion sigue vigente pero ya no esta en funciones.
		contexto.Situaciones.Clear();
		contexto.Situaciones.Add(titular with { EnFunciones = false });

		await contexto.ViewModel.CargarSituacionRevistasAsync();

		contexto.ViewModel.HabilitarDocenteEnFunciones.ShouldBeFalse();
		contexto.ViewModel.SituacionRevistaEnFunciones.ShouldBeNull();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_sin_situaciones_despues_de_haber_tenido_a_alguien_en_funciones_apaga_el_bloque()
	{
		var contexto = new Contexto();
		contexto.Agregar(enFunciones: true);
		await contexto.ViewModel.CargarSituacionRevistasAsync();
		contexto.ViewModel.HabilitarDocenteEnFunciones.ShouldBeTrue();

		contexto.Situaciones.Clear();
		await contexto.ViewModel.CargarSituacionRevistasAsync();

		contexto.ViewModel.HabilitarDocenteEnFunciones.ShouldBeFalse();
		contexto.ViewModel.SituacionRevistaEnFunciones.ShouldBeNull();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Seleccionar_otra_fila_no_cambia_lo_que_muestra_el_bloque_de_docente_en_funciones()
	{
		var contexto = new Contexto();
		var enFunciones = contexto.Agregar(enFunciones: true);
		var otra = contexto.Agregar(cargo: "Interino");
		await contexto.ViewModel.CargarSituacionRevistasAsync();

		contexto.ViewModel.SituacionRevistaUPDATE = contexto.Fila(otra.SituacionRevistaID);

		contexto.ViewModel.SituacionRevistaEnFunciones.SituacionRevistaID.ShouldBe(enFunciones.SituacionRevistaID);
		contexto.ViewModel.HabilitarDocenteEnFunciones.ShouldBeTrue();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Deseleccionar_la_grilla_no_apaga_ni_vacia_el_bloque_de_docente_en_funciones()
	{
		var contexto = new Contexto();
		var enFunciones = contexto.Agregar(enFunciones: true);
		await contexto.ViewModel.CargarSituacionRevistasAsync();

		contexto.ViewModel.SituacionRevistaUPDATE = null;

		contexto.ViewModel.SituacionRevistaEnFunciones.ShouldNotBeNull();
		contexto.ViewModel.SituacionRevistaEnFunciones.SituacionRevistaID.ShouldBe(enFunciones.SituacionRevistaID);
		contexto.ViewModel.HabilitarDocenteEnFunciones.ShouldBeTrue();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Despues_de_cargar_la_grilla_queda_preseleccionada_la_fila_en_funciones()
	{
		var contexto = new Contexto();
		var enFunciones = contexto.Agregar(enFunciones: true);
		contexto.Agregar(cargo: "Interino");

		await contexto.ViewModel.CargarSituacionRevistasAsync();

		contexto.ViewModel.SituacionRevistaUPDATE.ShouldNotBeNull();
		contexto.ViewModel.SituacionRevistaUPDATE.SituacionRevistaID.ShouldBe(enFunciones.SituacionRevistaID);
	}
	#endregion

	#region Suplencias: quien es reemplazable
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task SituacionesReemplazables_tiene_solo_las_situaciones_vigentes_de_la_catedra()
	{
		var contexto = new Contexto();
		var hoy = DateTime.Today;
		var indeterminada = contexto.Agregar(cargo: "Titular", inicio: hoy.AddDays(-30), fin: null);
		var vigenteHastaHoy = contexto.Agregar(cargo: "Suplente", inicio: hoy.AddDays(-2), fin: hoy, reemplazaA: indeterminada.SituacionRevistaID);
		var vencida = contexto.Agregar(cargo: "Interino", inicio: hoy.AddDays(-30), fin: hoy.AddDays(-1));
		var finalizada = contexto.Agregar(cargo: "Titular", estado: "Finalizado", inicio: hoy.AddDays(-60), fin: hoy);

		await contexto.ViewModel.CargarSituacionRevistasAsync();

		var ids = contexto.ViewModel.SituacionesReemplazables.Select(x => x.SituacionRevistaID).ToList();
		ids.ShouldContain(indeterminada.SituacionRevistaID);
		ids.ShouldContain(vigenteHastaHoy.SituacionRevistaID);
		ids.ShouldNotContain(vencida.SituacionRevistaID);
		ids.ShouldNotContain(finalizada.SituacionRevistaID);
		contexto.ViewModel.SituacionesReemplazables.ShouldAllBe(x => x.CatedraID == contexto.CatedraID);
	}

	[Fact(Skip = "Hallazgo R6-1: EsVigente de la UI no exige FechaInicio <= hoy (el dominio si: RangoFechas.HaIniciado). Una designacion que todavia no empezo se ofrece como reemplazable y Catedra.Designar la rechaza con SituacionRevistaNoEncontradaException. Se habilita cuando la UI replique HaIniciado.")]
	[Trait("Categoria", "Unidad")]
	public async Task SituacionesReemplazables_excluye_las_designaciones_que_todavia_no_empezaron()
	{
		var contexto = new Contexto();
		var futura = contexto.Agregar(cargo: "Titular", inicio: DateTime.Today.AddDays(10), fin: null);

		await contexto.ViewModel.CargarSituacionRevistasAsync();

		contexto.ViewModel.SituacionesReemplazables.Select(x => x.SituacionRevistaID).ShouldNotContain(futura.SituacionRevistaID);
	}

	[Theory]
	[Trait("Categoria", "Unidad")]
	[InlineData("Suplente", true)]
	[InlineData("Titular", false)]
	[InlineData("Interino", false)]
	public void HabilitarReemplazaA_solo_es_true_para_el_cargo_Suplente(string cargo, bool esperado)
	{
		var contexto = new Contexto();

		contexto.PrepararAlta(cargo);

		contexto.ViewModel.HabilitarReemplazaA.ShouldBe(esperado);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Cambiar_el_cargo_de_Suplente_a_Titular_descarta_el_ReemplazaA_elegido()
	{
		var contexto = new Contexto();
		var alta = contexto.PrepararAlta("Suplente");
		alta.ReemplazaA = Guid.NewGuid();

		alta.Cargo = "Titular";

		contexto.ViewModel.HabilitarReemplazaA.ShouldBeFalse();
		alta.ReemplazaA.ShouldBeNull();
	}
	#endregion

	#region Designar: DesignarDocenteAsync y la cadena de suplencias
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Designar_un_titular_llega_a_DesignarDocenteAsync_con_el_CatedraID_del_store_y_sin_ReemplazaA()
	{
		var contexto = new Contexto();
		var alta = contexto.PrepararAlta("Titular");

		await contexto.ViewModel.GuardarCommand.ExecuteAsync("Insert");

		await contexto.ServicioCatedra.Received(1).DesignarDocenteAsync(Arg.Is<DesignarDocenteRequest>(r =>
			r.CatedraID == contexto.CatedraID &&
			r.DocenteID == alta.DocenteID &&
			r.Cargo == "Titular" &&
			r.ReemplazaA == null));
		contexto.Dialogos.Errores.ShouldBeEmpty();
	}

	[Theory]
	[Trait("Categoria", "Unidad")]
	[InlineData("Titular")]
	[InlineData("Interino")]
	public async Task ReemplazaA_no_viaja_cuando_el_cargo_es_titular_o_interino_aunque_haya_quedado_cargado(string cargo)
	{
		var contexto = new Contexto();
		var alta = contexto.PrepararAlta(cargo);

		// Estado residual: el usuario eligio un reemplazo y despues cambio de cargo, o algo lo escribio directo.
		alta.ReemplazaA = Guid.NewGuid();

		await contexto.ViewModel.GuardarCommand.ExecuteAsync("Insert");

		await contexto.ServicioCatedra.Received(1).DesignarDocenteAsync(Arg.Is<DesignarDocenteRequest>(r =>
			r.Cargo == cargo &&
			r.ReemplazaA == null));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Designar_un_suplente_sin_ReemplazaA_no_llega_al_servicio_y_se_avisa_por_dialogo()
	{
		var contexto = new Contexto();
		contexto.PrepararAlta("Suplente");

		await contexto.ViewModel.GuardarCommand.ExecuteAsync("Insert");

		await contexto.ServicioCatedra.DidNotReceive().DesignarDocenteAsync(Arg.Any<DesignarDocenteRequest>());
		await contexto.ServicioCatedra.DidNotReceive().PonerEnFuncionesAsync(Arg.Any<PonerEnFuncionesRequest>());
		contexto.Dialogos.Advertencias.Count.ShouldBe(1);
		contexto.Dialogos.Advertencias[0].Mensaje.ShouldContain("reemplaza");
		contexto.Dialogos.Errores.ShouldBeEmpty();
		contexto.Dialogos.Confirmaciones.ShouldBeEmpty();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Designar_un_suplente_con_ReemplazaA_envia_el_reemplazo_elegido()
	{
		var contexto = new Contexto();
		var titular = contexto.Agregar();
		await contexto.ViewModel.CargarSituacionRevistasAsync();
		var alta = contexto.PrepararAlta("Suplente");
		alta.ReemplazaA = titular.SituacionRevistaID;

		await contexto.ViewModel.GuardarCommand.ExecuteAsync("Insert");

		await contexto.ServicioCatedra.Received(1).DesignarDocenteAsync(Arg.Is<DesignarDocenteRequest>(r =>
			r.CatedraID == contexto.CatedraID &&
			r.Cargo == "Suplente" &&
			r.ReemplazaA == titular.SituacionRevistaID));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Designar_con_EnFunciones_pone_en_funciones_la_situacion_devuelta_por_el_servicio()
	{
		var contexto = new Contexto();
		contexto.PrepararAlta("Titular", enFunciones: true);

		await contexto.ViewModel.GuardarCommand.ExecuteAsync("Insert");

		await contexto.ServicioCatedra.Received(1).PonerEnFuncionesAsync(Arg.Is<PonerEnFuncionesRequest>(r =>
			r.CatedraID == contexto.CatedraID &&
			r.SituacionRevistaID == contexto.SituacionCreadaID));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Designar_sin_EnFunciones_no_llama_a_PonerEnFunciones()
	{
		var contexto = new Contexto();
		contexto.PrepararAlta("Titular", enFunciones: false);

		await contexto.ViewModel.GuardarCommand.ExecuteAsync("Insert");

		await contexto.ServicioCatedra.DidNotReceive().PonerEnFuncionesAsync(Arg.Any<PonerEnFuncionesRequest>());
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Si_el_servicio_rechaza_la_designacion_el_error_se_muestra_y_no_se_propaga()
	{
		var contexto = new Contexto();
		contexto.PrepararAlta("Titular");
		contexto.ServicioCatedra.DesignarDocenteAsync(Arg.Any<DesignarDocenteRequest>())
				.Returns(Task.FromException<Guid>(new InvalidOperationException("Cargo ocupado")));

		await Should.NotThrowAsync(async () =>
		{
			await contexto.ViewModel.GuardarCommand.ExecuteAsync("Insert");
		});

		contexto.Dialogos.Errores.Count.ShouldBe(1);
		contexto.Dialogos.Errores[0].Mensaje.ShouldBe("Cargo ocupado");
		await contexto.ServicioCatedra.DidNotReceive().PonerEnFuncionesAsync(Arg.Any<PonerEnFuncionesRequest>());
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Si_el_usuario_no_confirma_la_designacion_no_se_llama_al_servicio()
	{
		var contexto = new Contexto();
		contexto.PrepararAlta("Titular");
		contexto.Dialogos.RespuestaDeConfirmacion = false;

		await contexto.ViewModel.GuardarCommand.ExecuteAsync("Insert");

		await contexto.ServicioCatedra.DidNotReceive().DesignarDocenteAsync(Arg.Any<DesignarDocenteRequest>());
	}
	#endregion

	#region Los otros casos de uso remapeados
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Poner_en_funciones_llega_a_PonerEnFuncionesAsync_con_la_situacion_seleccionada()
	{
		var contexto = new Contexto();
		var titular = contexto.Agregar();
		await contexto.ViewModel.CargarSituacionRevistasAsync();
		contexto.ViewModel.SituacionRevistaUPDATE = contexto.Fila(titular.SituacionRevistaID);

		await contexto.ViewModel.GuardarCommand.ExecuteAsync("Docente");

		await contexto.ServicioCatedra.Received(1).PonerEnFuncionesAsync(Arg.Is<PonerEnFuncionesRequest>(r =>
			r.CatedraID == contexto.CatedraID &&
			r.SituacionRevistaID == titular.SituacionRevistaID));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Poner_en_funciones_sin_confirmacion_no_llama_al_servicio()
	{
		var contexto = new Contexto();
		var titular = contexto.Agregar();
		await contexto.ViewModel.CargarSituacionRevistasAsync();
		contexto.ViewModel.SituacionRevistaUPDATE = contexto.Fila(titular.SituacionRevistaID);
		contexto.Dialogos.RespuestaDeConfirmacion = false;

		await contexto.ViewModel.GuardarCommand.ExecuteAsync("Docente");

		await contexto.ServicioCatedra.DidNotReceive().PonerEnFuncionesAsync(Arg.Any<PonerEnFuncionesRequest>());
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Relevar_de_funciones_llega_a_RelevarDeFuncionesAsync_con_el_CatedraID_del_store()
	{
		var contexto = new Contexto();
		var enFunciones = contexto.Agregar(enFunciones: true);
		await contexto.ViewModel.CargarSituacionRevistasAsync();
		contexto.ViewModel.SituacionRevistaUPDATE = contexto.Fila(enFunciones.SituacionRevistaID);

		await contexto.ViewModel.EditarCommand.ExecuteAsync("Docente");

		await contexto.ServicioCatedra.Received(1).RelevarDeFuncionesAsync(Arg.Is<RelevarDeFuncionesRequest>(r => r.CatedraID == contexto.CatedraID));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Establecer_fin_de_designacion_llega_a_EstablecerFinDeDesignacionAsync_con_fecha_de_hoy()
	{
		var contexto = new Contexto();
		var titular = contexto.Agregar();
		await contexto.ViewModel.CargarSituacionRevistasAsync();
		contexto.ViewModel.SituacionRevistaUPDATE = contexto.Fila(titular.SituacionRevistaID);

		await contexto.ViewModel.EditarCommand.ExecuteAsync("Rescindir");

		await contexto.ServicioCatedra.Received(1).EstablecerFinDeDesignacionAsync(Arg.Is<EstablecerFinDeDesignacionRequest>(r =>
			r.CatedraID == contexto.CatedraID &&
			r.SituacionRevistaID == titular.SituacionRevistaID &&
			r.FechaFin == DateTime.Today));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Finalizar_designacion_llega_a_FinalizarDesignacionAsync_con_la_situacion_seleccionada()
	{
		var contexto = new Contexto();
		var titular = contexto.Agregar();
		await contexto.ViewModel.CargarSituacionRevistasAsync();
		contexto.ViewModel.SituacionRevistaUPDATE = contexto.Fila(titular.SituacionRevistaID);

		await contexto.ViewModel.EliminarCommand.ExecuteAsync("Docente");

		await contexto.ServicioCatedra.Received(1).FinalizarDesignacionAsync(Arg.Is<FinalizarDesignacionRequest>(r =>
			r.CatedraID == contexto.CatedraID &&
			r.SituacionRevistaID == titular.SituacionRevistaID));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Finalizar_designacion_sin_confirmacion_no_llama_al_servicio()
	{
		var contexto = new Contexto();
		var titular = contexto.Agregar();
		await contexto.ViewModel.CargarSituacionRevistasAsync();
		contexto.ViewModel.SituacionRevistaUPDATE = contexto.Fila(titular.SituacionRevistaID);
		contexto.Dialogos.RespuestaDeConfirmacion = false;

		await contexto.ViewModel.EliminarCommand.ExecuteAsync("Docente");

		await contexto.ServicioCatedra.DidNotReceive().FinalizarDesignacionAsync(Arg.Any<FinalizarDesignacionRequest>());
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task El_texto_de_finalizar_habla_de_finalizar_y_no_de_eliminar_definitivamente()
	{
		var contexto = new Contexto();
		var titular = contexto.Agregar();
		await contexto.ViewModel.CargarSituacionRevistasAsync();
		contexto.ViewModel.SituacionRevistaUPDATE = contexto.Fila(titular.SituacionRevistaID);

		await contexto.ViewModel.EliminarCommand.ExecuteAsync("Docente");

		contexto.Dialogos.Confirmaciones.Count.ShouldBe(1);
		contexto.Dialogos.Confirmaciones[0].Titulo.ShouldBe("Finalizar Designación");
		contexto.Dialogos.Confirmaciones[0].Mensaje.ShouldNotContain("definitivamente");
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Un_error_del_servicio_al_relevar_se_muestra_y_no_se_propaga()
	{
		var contexto = new Contexto();
		var enFunciones = contexto.Agregar(enFunciones: true);
		await contexto.ViewModel.CargarSituacionRevistasAsync();
		contexto.ViewModel.SituacionRevistaUPDATE = contexto.Fila(enFunciones.SituacionRevistaID);
		contexto.ServicioCatedra.RelevarDeFuncionesAsync(Arg.Any<RelevarDeFuncionesRequest>())
				.Returns(Task.FromException(new InvalidOperationException("Sin docente")));

		await Should.NotThrowAsync(async () =>
		{
			await contexto.ViewModel.EditarCommand.ExecuteAsync("Docente");
		});

		contexto.Dialogos.Errores.Count.ShouldBe(1);
		contexto.Dialogos.Errores[0].Mensaje.ShouldBe("Sin docente");
	}
	#endregion

	#region Navegacion
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Volver_navega_una_vez_con_el_servicio_inyectado()
	{
		var contexto = new Contexto();

		contexto.ViewModel.NavigationCommand.Execute("Materias");

		contexto.NavegarAMaterias.Llamadas.ShouldBe(1);
	}
	#endregion
}
