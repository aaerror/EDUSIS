using Core.ServicioAsistencias;
using Core.ServicioAsistencias.DTOs.Requests;
using Core.ServicioAsistencias.DTOs.Responses;
using Core.ServicioCursantes;
using Core.ServicioCursantes.DTOs.Requests;
using Core.ServicioCursantes.DTOs.Responses;
using Core.ServicioDivisiones;
using Core.ServicioDivisiones.DTOs.Requests;
using Core.ServicioDivisiones.DTOs.Responses;
using Domain.Asistencias;
using Domain.Cursos;
using Domain.Shared;
using NSubstitute;
using Shouldly;
using WPF_Desktop.Store;
using WPF_Desktop.UnitTests.Dobles;
using WPF_Desktop.ViewModels.Cursos.Divisiones;
using Xunit;

namespace WPF_Desktop.UnitTests.ViewModels;

/// <summary>
/// Cubre la pantalla nueva <see cref="GestionAsistenciasViewModel"/>: la planilla del dia que sale de
/// <c>ConsultarPlanillaAsync</c> con los nombres que sale de <c>ListarCursantesAsync</c>, el estado
/// "sin planilla" que el ViewModel traduce del centinela de Core, los cuatro marcados y que no se
/// exponga ningun DTO de Core.
/// </summary>
public class GestionAsistenciasViewModelTests
{
	private const string CicloDelStore = "1999";

	private sealed class Contexto
	{
		public IServicioAsistencia ServicioAsistencia { get; } = Substitute.For<IServicioAsistencia>();
		public IServicioCursante ServicioCursante { get; } = Substitute.For<IServicioCursante>();
		public IServicioDivision ServicioDivision { get; } = Substitute.For<IServicioDivision>();
		public NavegacionFalsa NavegarADivisiones { get; } = new();
		public CursoStore CursoStore { get; } = new();
		public DivisionStore DivisionStore { get; } = new();
		public CicloLectivoStore CicloLectivoStore { get; } = new();
		public DialogServiceFalso Dialogos { get; } = new();
		public Guid CursoID { get; } = Guid.NewGuid();
		public Guid DivisionID { get; } = Guid.NewGuid();
		public GestionAsistenciasViewModel ViewModel { get; }

		public Contexto(bool conDivision = true)
		{
			CursoStore.Curso = new CursoSeleccionado(CursoID, Grado.Primero.ToString(), NivelEducativo.Secundaria.ToString());
			CicloLectivoStore.CicloLectivo = CicloDelStore;

			if (conDivision)
			{
				DivisionStore.Division = new DivisionSeleccionada(DivisionID);
			}

			DevolverCursantes();
			SinPlanilla();
			DevolverDivisionConPreceptor();

			ViewModel = new GestionAsistenciasViewModel(NavegarADivisiones,
														ServicioAsistencia,
														ServicioCursante,
														ServicioDivision,
														CursoStore,
														DivisionStore,
														CicloLectivoStore,
														Dialogos);
		}

		/// <summary>La división del drill-down con preceptor asignado: el caso normal.</summary>
		public void DevolverDivisionConPreceptor(string letra = "A", string preceptor = "Perez, Ana")
		{
			Devolver(new DivisionResponse(DivisionID, letra, Guid.NewGuid(), preceptor, 0));
		}

		/// <summary>
		/// La división sin preceptor: el cargo vacante deja <c>PreceptorID</c> en nulo y es lo que
		/// deshabilita "Abrir planilla".
		/// </summary>
		public void DevolverDivisionSinPreceptor(string letra = "A")
		{
			Devolver(new DivisionResponse(DivisionID, letra, null, null, 0));
		}

		/// <summary>No hay consulta por identificador: se lista el curso y la pantalla busca la suya.</summary>
		public void Devolver(params DivisionResponse[] divisiones)
		{
			ServicioDivision.ListarDivisionesAsync(Arg.Any<ListarDivisionesRequest>())
							.Returns(Task.FromResult<IReadOnlyCollection<DivisionResponse>>(divisiones.ToList()));
		}

		public void DevolverCursantes(params CursanteResponse[] cursantes)
		{
			ServicioCursante.ListarCursantesAsync(Arg.Any<BuscarListadoRequest>())
							.Returns(Task.FromResult<IReadOnlyCollection<CursanteResponse>>(cursantes.ToList()));
		}

		public void DevolverPlanilla(PlanillaAsistenciaResponse planilla)
		{
			ServicioAsistencia.ConsultarPlanillaAsync(Arg.Any<ConsultarPlanillaRequest>())
							  .Returns(Task.FromResult(planilla));
		}

		/// <summary>
		/// El centinela de Core: <c>BuscarPlanillaAsync</c> lanza <see cref="NullReferenceException"/>
		/// cuando no hay planilla para esa fecha, y eso NO es un error para la pantalla.
		/// </summary>
		public void SinPlanilla()
		{
			ServicioAsistencia.ConsultarPlanillaAsync(Arg.Any<ConsultarPlanillaRequest>())
							  .Returns(Task.FromException<PlanillaAsistenciaResponse>(new NullReferenceException("Planilla de asistencia no encontrada.")));
		}
	}

	private static CursanteResponse CrearCursante(string nombreCompleto)
	{
		return new CursanteResponse(Guid.NewGuid(), Guid.NewGuid(), nombreCompleto, "30111222", 15, false);
	}

	private static RegistroAsistenciaResponse CrearRegistro(Guid cursanteID,
															TipoAsistencia tipo = TipoAsistencia.Presente,
															TimeSpan? minutos = null,
															string? observacion = null)
	{
		return new RegistroAsistenciaResponse(cursanteID, tipo, minutos, observacion);
	}

	private static PlanillaAsistenciaResponse CrearPlanilla(Guid divisionID,
															DateTime fecha,
															IReadOnlyCollection<RegistroAsistenciaResponse> registros,
															bool cerrada = false,
															int presentes = 0,
															int ausencias = 0,
															int inasistencias = 0,
															int tardanzas = 0)
	{
		return new PlanillaAsistenciaResponse(Guid.NewGuid(),
											  divisionID,
											  fecha,
											  Guid.NewGuid(),
											  cerrada,
											  registros,
											  presentes,
											  ausencias,
											  inasistencias,
											  tardanzas);
	}

	#region Carga
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_pide_la_planilla_con_la_division_del_store_y_la_fecha_de_la_pantalla()
	{
		var contexto = new Contexto();
		var fecha = new DateTime(1999, 3, 10);
		contexto.ViewModel.Fecha = fecha;

		// Cambiar la fecha ya consulta; el Cargar explícito es el de la carga inicial de la pantalla.
		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		_ = contexto.ServicioAsistencia.Received(2).ConsultarPlanillaAsync(new ConsultarPlanillaRequest(contexto.DivisionID, fecha));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_pide_el_listado_de_alumnos_con_el_curso_la_division_y_el_ciclo_lectivo_del_store()
	{
		var contexto = new Contexto();

		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		_ = contexto.ServicioCursante.Received(1).ListarCursantesAsync(new BuscarListadoRequest(contexto.CursoID, contexto.DivisionID, CicloDelStore));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_con_planilla_mapea_cada_registro_con_el_nombre_que_sale_del_listado_de_alumnos()
	{
		// PlanillaAsistenciaResponse no trae nombres: el ViewModel los une por CursanteID contra
		// ListarCursantesAsync, y el que no aparece en el listado queda sin nombre en lugar de romper.
		var contexto = new Contexto();
		var presente = CrearCursante("Perez, Ana");
		var tarde = CrearCursante("Luna, Juan");
		var desconocido = Guid.NewGuid();
		contexto.DevolverCursantes(presente, tarde);
		contexto.DevolverPlanilla(CrearPlanilla(contexto.DivisionID,
												contexto.ViewModel.Fecha,
												new List<RegistroAsistenciaResponse>
												{
													CrearRegistro(presente.CursanteID),
													CrearRegistro(tarde.CursanteID, TipoAsistencia.Tardanza, TimeSpan.FromMinutes(15)),
													CrearRegistro(desconocido, TipoAsistencia.Ausencia)
												}));

		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.ViewModel.HayPlanilla.ShouldBeTrue();
		contexto.ViewModel.Registros.Count.ShouldBe(3);
		contexto.ViewModel.Registros.Single(r => r.CursanteID == presente.CursanteID).NombreCompleto.ShouldBe("Perez, Ana");
		contexto.ViewModel.Registros.Single(r => r.CursanteID == tarde.CursanteID).NombreCompleto.ShouldBe("Luna, Juan");
		contexto.ViewModel.Registros.Single(r => r.CursanteID == tarde.CursanteID).Minutos.ShouldBe("15 min");
		contexto.ViewModel.Registros.Single(r => r.CursanteID == desconocido).NombreCompleto.ShouldBe(string.Empty);
		contexto.ViewModel.Registros.Single(r => r.CursanteID == desconocido).EsFalta.ShouldBeTrue();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_con_planilla_llena_los_cuatro_contadores_que_vienen_del_Response()
	{
		// Los contadores no se cuentan en la UI: los calcula el dominio y viajan en el Response.
		var contexto = new Contexto();
		contexto.DevolverPlanilla(CrearPlanilla(contexto.DivisionID,
												contexto.ViewModel.Fecha,
												new List<RegistroAsistenciaResponse>(),
												presentes: 18,
												ausencias: 2,
												inasistencias: 3,
												tardanzas: 4));

		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.ViewModel.Presentes.ShouldBe(18);
		contexto.ViewModel.Ausencias.ShouldBe(2);
		contexto.ViewModel.Inasistencias.ShouldBe(3);
		contexto.ViewModel.Tardanzas.ShouldBe(4);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_sin_planilla_deja_la_pantalla_en_estado_sin_planilla_y_no_muestra_ningun_error()
	{
		// Decision de la pantalla: "todavia no hay planilla para esta fecha" es un estado normal a
		// primera hora del dia, no un error. Core no lo distingue de un fallo real —lo informa con
		// NullReferenceException desde BuscarPlanillaAsync— y el ViewModel lo traduce a HayPlanilla.
		var contexto = new Contexto();
		contexto.DevolverCursantes(CrearCursante("Perez, Ana"));
		contexto.SinPlanilla();

		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.ViewModel.HayPlanilla.ShouldBeFalse();
		contexto.ViewModel.Cerrada.ShouldBeFalse();
		contexto.ViewModel.Registros.ShouldBeEmpty();
		contexto.ViewModel.CursantesSinRegistro.ShouldBeEmpty();
		contexto.ViewModel.Presentes.ShouldBe(0);
		contexto.ViewModel.Ausencias.ShouldBe(0);
		contexto.ViewModel.Inasistencias.ShouldBe(0);
		contexto.ViewModel.Tardanzas.ShouldBe(0);
		contexto.Dialogos.Errores.ShouldBeEmpty();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Un_error_de_otro_tipo_al_cargar_si_se_muestra_por_el_dialogo_y_no_se_propaga()
	{
		// La contracara del test anterior: el catch de NullReferenceException es un centinela puntual,
		// no un "no mostrar errores de la carga".
		var contexto = new Contexto();
		contexto.ServicioAsistencia.ConsultarPlanillaAsync(Arg.Any<ConsultarPlanillaRequest>())
								   .Returns(Task.FromException<PlanillaAsistenciaResponse>(new InvalidOperationException("sin conexion")));

		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.Dialogos.Errores.Count.ShouldBe(1);
		contexto.Dialogos.Errores[0].Mensaje.ShouldBe("sin conexion");
		contexto.ViewModel.HayPlanilla.ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_ofrece_para_incorporar_solo_a_los_alumnos_que_no_tienen_registro_en_la_planilla()
	{
		var contexto = new Contexto();
		var conRegistro = CrearCursante("Perez, Ana");
		var sinRegistro = CrearCursante("Luna, Juan");
		contexto.DevolverCursantes(conRegistro, sinRegistro);
		contexto.DevolverPlanilla(CrearPlanilla(contexto.DivisionID,
												contexto.ViewModel.Fecha,
												new List<RegistroAsistenciaResponse> { CrearRegistro(conRegistro.CursanteID) }));

		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.ViewModel.CursantesSinRegistro.Count.ShouldBe(1);
		contexto.ViewModel.CursantesSinRegistro[0].CursanteID.ShouldBe(sinRegistro.CursanteID);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_dos_veces_no_duplica_los_registros_ni_los_incorporables()
	{
		var contexto = new Contexto();
		var conRegistro = CrearCursante("Perez, Ana");
		var sinRegistro = CrearCursante("Luna, Juan");
		contexto.DevolverCursantes(conRegistro, sinRegistro);
		contexto.DevolverPlanilla(CrearPlanilla(contexto.DivisionID,
												contexto.ViewModel.Fecha,
												new List<RegistroAsistenciaResponse> { CrearRegistro(conRegistro.CursanteID) }));

		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);
		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.ViewModel.Registros.Count.ShouldBe(1);
		contexto.ViewModel.CursantesSinRegistro.Count.ShouldBe(1);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Sin_division_en_el_store_la_carga_no_esta_habilitada()
	{
		var contexto = new Contexto(conDivision: false);

		contexto.ViewModel.CargarCommand.CanExecute(null).ShouldBeFalse();
	}
	#endregion

	#region Marcado
	private static async Task<Contexto> ConPlanillaAbiertaYFilaElegida(bool cerrada = false)
	{
		var contexto = new Contexto();
		var cursante = CrearCursante("Perez, Ana");
		contexto.DevolverCursantes(cursante);
		contexto.DevolverPlanilla(CrearPlanilla(contexto.DivisionID,
												contexto.ViewModel.Fecha,
												new List<RegistroAsistenciaResponse> { CrearRegistro(cursante.CursanteID) },
												cerrada: cerrada));

		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		// La carga limpia la seleccion: la fila la elige la grilla, asi que se simula acá.
		contexto.ViewModel.Registro = contexto.ViewModel.Registros[0];

		return contexto;
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Los_cuatro_marcados_exigen_la_fila_sobre_la_que_marcan()
	{
		var contexto = await ConPlanillaAbiertaYFilaElegida();
		var fila = contexto.ViewModel.Registros[0];

		// Sin fila no hay a quién marcar. Los cuatro comandos la reciben por parámetro: los botones
		// de la grilla pasan la suya y los del pie pasan la seleccionada.
		contexto.ViewModel.MarcarPresenteCommand.CanExecute(null).ShouldBeFalse();
		contexto.ViewModel.MarcarAusenciaCommand.CanExecute(null).ShouldBeFalse();
		contexto.ViewModel.MarcarInasistenciaCommand.CanExecute(null).ShouldBeFalse();
		contexto.ViewModel.MarcarTardanzaCommand.CanExecute(null).ShouldBeFalse();

		contexto.ViewModel.MarcarPresenteCommand.CanExecute(fila).ShouldBeTrue();
		contexto.ViewModel.MarcarAusenciaCommand.CanExecute(fila).ShouldBeTrue();
		contexto.ViewModel.MarcarInasistenciaCommand.CanExecute(fila).ShouldBeTrue();
		contexto.ViewModel.MarcarTardanzaCommand.CanExecute(fila).ShouldBeTrue();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Los_cuatro_marcados_exigen_la_planilla_abierta()
	{
		var contexto = await ConPlanillaAbiertaYFilaElegida(cerrada: true);
		var fila = contexto.ViewModel.Registros[0];

		// Se pasa la fila a propósito: con null los cuatro darían false por falta de parámetro y el
		// caso no probaría nada sobre la planilla cerrada.
		contexto.ViewModel.Cerrada.ShouldBeTrue();
		contexto.ViewModel.MarcarPresenteCommand.CanExecute(fila).ShouldBeFalse();
		contexto.ViewModel.MarcarAusenciaCommand.CanExecute(fila).ShouldBeFalse();
		contexto.ViewModel.MarcarInasistenciaCommand.CanExecute(fila).ShouldBeFalse();
		contexto.ViewModel.MarcarTardanzaCommand.CanExecute(fila).ShouldBeFalse();
		contexto.ViewModel.IncorporarCursanteCommand.CanExecute(null).ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Sin_planilla_no_se_puede_marcar_pero_si_abrirla()
	{
		var contexto = new Contexto();

		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.ViewModel.MarcarPresenteCommand.CanExecute(null).ShouldBeFalse();
		contexto.ViewModel.CerrarPlanillaCommand.CanExecute(null).ShouldBeFalse();
		contexto.ViewModel.ReabrirPlanillaCommand.CanExecute(null).ShouldBeFalse();
		contexto.ViewModel.EliminarPlanillaCommand.CanExecute(null).ShouldBeFalse();
		contexto.ViewModel.AbrirPlanillaCommand.CanExecute(null).ShouldBeTrue();
	}

	[Theory]
	[Trait("Categoria", "Unidad")]
	[InlineData(0, false)]
	[InlineData(-5, false)]
	[InlineData(1, true)]
	public async Task Marcar_tardanza_exige_minutos_mayores_a_cero(int minutos, bool habilitado)
	{
		var contexto = await ConPlanillaAbiertaYFilaElegida();

		var fila = contexto.ViewModel.Registros[0];
		contexto.ViewModel.MinutosTardanza = minutos;

		contexto.ViewModel.MarcarTardanzaCommand.CanExecute(fila).ShouldBe(habilitado);
		contexto.ViewModel.MarcarPresenteCommand.CanExecute(fila).ShouldBeTrue();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Marcar_tardanza_manda_los_minutos_del_formulario_como_TimeSpan()
	{
		var contexto = await ConPlanillaAbiertaYFilaElegida();
		var fila = contexto.ViewModel.Registro!;
		var cursanteID = fila.CursanteID;
		contexto.ViewModel.MinutosTardanza = 20;
		contexto.ViewModel.Observacion = "llego con el transporte demorado";

		await contexto.ViewModel.MarcarTardanzaCommand.ExecuteAsync(fila);

		_ = contexto.ServicioAsistencia.Received(1).MarcarTardanzaAsync(new MarcarTardanzaRequest(contexto.DivisionID,
																								  contexto.ViewModel.Fecha,
																								  cursanteID,
																								  TimeSpan.FromMinutes(20),
																								  "llego con el transporte demorado"));
		contexto.ViewModel.Observacion.ShouldBeEmpty();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Una_observacion_en_blanco_viaja_como_null()
	{
		var contexto = await ConPlanillaAbiertaYFilaElegida();
		var fila = contexto.ViewModel.Registro!;
		var cursanteID = fila.CursanteID;
		contexto.ViewModel.Observacion = "   ";

		await contexto.ViewModel.MarcarAusenciaCommand.ExecuteAsync(fila);

		_ = contexto.ServicioAsistencia.Received(1).MarcarAusenciaAsync(new MarcarAusenciaRequest(contexto.DivisionID,
																								  contexto.ViewModel.Fecha,
																								  cursanteID,
																								  null));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Marcar_presente_recarga_la_planilla_para_refrescar_los_contadores()
	{
		var contexto = await ConPlanillaAbiertaYFilaElegida();

		await contexto.ViewModel.MarcarPresenteCommand.ExecuteAsync(contexto.ViewModel.Registro!);

		_ = contexto.ServicioAsistencia.Received(2).ConsultarPlanillaAsync(Arg.Any<ConsultarPlanillaRequest>());
	}

	/// <summary>
	/// Marca la fila QUE RECIBE y no la que está seleccionada, que son dos alumnos distintos.
	/// </summary>
	/// <remarks>
	/// Es la guarda del defecto que <c>docs/ui-ux.md</c> dejó anotado: un <c>Button</c> dentro de una
	/// celda marca el clic como manejado, así que apretar el botón de la fila 3 NO la selecciona, y un
	/// comando que leyera el <c>SelectedItem</c> terminaría marcándole la inasistencia a la fila 1.
	/// Acá se eligen explícitamente dos filas diferentes para que el caso falle si alguien vuelve a
	/// colgar el marcado de la selección.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Marcar_inasistencia_usa_la_fila_que_recibe_y_no_la_seleccionada()
	{
		var contexto = new Contexto();
		var elegido = CrearCursante("Perez, Ana");
		var otro = CrearCursante("Luna, Juan");
		contexto.DevolverCursantes(elegido, otro);
		contexto.DevolverPlanilla(CrearPlanilla(contexto.DivisionID,
												contexto.ViewModel.Fecha,
												new List<RegistroAsistenciaResponse>
												{
													CrearRegistro(elegido.CursanteID),
													CrearRegistro(otro.CursanteID)
												}));

		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		var filaSeleccionada = contexto.ViewModel.Registros[0];
		var filaDelBoton = contexto.ViewModel.Registros[1];
		contexto.ViewModel.Registro = filaSeleccionada;

		await contexto.ViewModel.MarcarInasistenciaCommand.ExecuteAsync(filaDelBoton);

		_ = contexto.ServicioAsistencia.Received(1).MarcarInasistenciaAsync(Arg.Is<MarcarInasistenciaRequest>(r => r.CursanteID == otro.CursanteID));
		_ = contexto.ServicioAsistencia.DidNotReceive().MarcarInasistenciaAsync(Arg.Is<MarcarInasistenciaRequest>(r => r.CursanteID == elegido.CursanteID));
	}

	/// <summary>
	/// Cambiar de alumno vacía la observación: la nota describe a UN registro y no se hereda.
	/// </summary>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Elegir_otra_fila_limpia_la_observacion_del_formulario()
	{
		var contexto = new Contexto();
		var uno = CrearCursante("Perez, Ana");
		var otro = CrearCursante("Luna, Juan");
		contexto.DevolverCursantes(uno, otro);
		contexto.DevolverPlanilla(CrearPlanilla(contexto.DivisionID,
												contexto.ViewModel.Fecha,
												new List<RegistroAsistenciaResponse>
												{
													CrearRegistro(uno.CursanteID),
													CrearRegistro(otro.CursanteID)
												}));

		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.ViewModel.Registro = contexto.ViewModel.Registros[0];
		contexto.ViewModel.Observacion = "se retira a las 11";

		contexto.ViewModel.Registro = contexto.ViewModel.Registros[1];

		contexto.ViewModel.Observacion.ShouldBeEmpty();
	}
	#endregion

	#region Incorporacion
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Incorporar_exige_un_alumno_elegido_y_manda_su_identificador()
	{
		var contexto = new Contexto();
		var conRegistro = CrearCursante("Perez, Ana");
		var sinRegistro = CrearCursante("Luna, Juan");
		contexto.DevolverCursantes(conRegistro, sinRegistro);
		contexto.DevolverPlanilla(CrearPlanilla(contexto.DivisionID,
												contexto.ViewModel.Fecha,
												new List<RegistroAsistenciaResponse> { CrearRegistro(conRegistro.CursanteID) }));
		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.ViewModel.IncorporarCursanteCommand.CanExecute(null).ShouldBeFalse();

		contexto.ViewModel.CursanteAIncorporar = contexto.ViewModel.CursantesSinRegistro[0];

		contexto.ViewModel.IncorporarCursanteCommand.CanExecute(null).ShouldBeTrue();

		await contexto.ViewModel.IncorporarCursanteCommand.ExecuteAsync(null);

		_ = contexto.ServicioAsistencia.Received(1).IncorporarCursanteAsync(new IncorporarCursanteRequest(contexto.DivisionID,
																										  contexto.ViewModel.Fecha,
																										  sinRegistro.CursanteID));
	}
	#endregion

	#region Ciclo de vida de la planilla
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Abrir_la_planilla_la_crea_para_la_fecha_elegida_y_vuelve_a_consultarla()
	{
		var contexto = new Contexto();
		var fecha = new DateTime(1999, 4, 5);
		contexto.ViewModel.Fecha = fecha;

		await contexto.ViewModel.AbrirPlanillaCommand.ExecuteAsync(null);

		_ = contexto.ServicioAsistencia.Received(1).AbrirPlanillaAsync(new AbrirPlanillaRequest(contexto.DivisionID, fecha));

		// Dos consultas y no una: la primera la dispara el cambio de fecha —elegir una fecha ES pedir
		// su planilla— y la segunda es la recarga con la que termina la apertura.
		_ = contexto.ServicioAsistencia.Received(2).ConsultarPlanillaAsync(new ConsultarPlanillaRequest(contexto.DivisionID, fecha));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cerrar_la_planilla_pide_confirmacion_y_sin_confirmar_no_llega_al_servicio()
	{
		var contexto = await ConPlanillaAbiertaYFilaElegida();
		contexto.Dialogos.RespuestaDeConfirmacion = false;

		await contexto.ViewModel.CerrarPlanillaCommand.ExecuteAsync(null);

		contexto.Dialogos.Confirmaciones.Count.ShouldBe(1);
		_ = contexto.ServicioAsistencia.DidNotReceive().CerrarPlanillaAsync(Arg.Any<CerrarPlanillaRequest>());
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Reabrir_solo_esta_habilitado_con_la_planilla_cerrada()
	{
		var abierta = await ConPlanillaAbiertaYFilaElegida();
		abierta.ViewModel.ReabrirPlanillaCommand.CanExecute(null).ShouldBeFalse();
		abierta.ViewModel.CerrarPlanillaCommand.CanExecute(null).ShouldBeTrue();

		var cerrada = await ConPlanillaAbiertaYFilaElegida(cerrada: true);
		cerrada.ViewModel.ReabrirPlanillaCommand.CanExecute(null).ShouldBeTrue();
		cerrada.ViewModel.CerrarPlanillaCommand.CanExecute(null).ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Eliminar_la_planilla_pide_confirmacion_y_sin_confirmar_no_llega_al_servicio()
	{
		var contexto = await ConPlanillaAbiertaYFilaElegida();
		contexto.Dialogos.RespuestaDeConfirmacion = false;

		await contexto.ViewModel.EliminarPlanillaCommand.ExecuteAsync(null);

		contexto.Dialogos.Confirmaciones.Count.ShouldBe(1);
		_ = contexto.ServicioAsistencia.DidNotReceive().EliminarPlanillaAsync(Arg.Any<EliminarPlanillaRequest>());
		contexto.Dialogos.Informaciones.ShouldBeEmpty();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Eliminar_la_planilla_confirmada_avisa_y_deja_la_pantalla_sin_planilla()
	{
		var contexto = await ConPlanillaAbiertaYFilaElegida();
		contexto.SinPlanilla();

		await contexto.ViewModel.EliminarPlanillaCommand.ExecuteAsync(null);

		_ = contexto.ServicioAsistencia.Received(1).EliminarPlanillaAsync(new EliminarPlanillaRequest(contexto.DivisionID, contexto.ViewModel.Fecha));
		contexto.Dialogos.Informaciones.Count.ShouldBe(1);
		contexto.ViewModel.HayPlanilla.ShouldBeFalse();
		contexto.ViewModel.Registros.ShouldBeEmpty();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Eliminar_una_planilla_cerrada_sigue_habilitado_y_el_mensaje_lo_da_el_dominio()
	{
		// La regla "una planilla cerrada no se elimina" es del dominio
		// (PlanillaCerradaNoEliminableException) y no se duplica en la UI: el boton queda habilitado y
		// el usuario recibe el mensaje del dominio por el dialogo de error.
		var contexto = await ConPlanillaAbiertaYFilaElegida(cerrada: true);
		contexto.ServicioAsistencia.EliminarPlanillaAsync(Arg.Any<EliminarPlanillaRequest>())
								   .Returns(Task.FromException(new InvalidOperationException("La planilla cerrada no se puede eliminar.")));

		contexto.ViewModel.EliminarPlanillaCommand.CanExecute(null).ShouldBeTrue();

		await contexto.ViewModel.EliminarPlanillaCommand.ExecuteAsync(null);

		contexto.Dialogos.Errores.Count.ShouldBe(1);
		contexto.Dialogos.Errores[0].Mensaje.ShouldBe("La planilla cerrada no se puede eliminar.");
	}
	#endregion

	#region Navegacion
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Volver_navega_una_sola_vez_a_las_divisiones()
	{
		var contexto = new Contexto();

		contexto.ViewModel.VolverCommand.CanExecute(null).ShouldBeTrue();
		contexto.ViewModel.VolverCommand.Execute(null);

		contexto.NavegarADivisiones.Llamadas.ShouldBe(1);
	}
	#endregion

	#region Contrato del ViewModel
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_ViewModel_no_expone_ningun_tipo_de_Core_como_propiedad_bindeable()
	{
		// Los Response de Core no se bindean: la planilla vive en un campo privado y la grilla ve
		// RegistroAsistenciaViewModel.
		var propiedades = typeof(GestionAsistenciasViewModel).GetProperties();

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

	#region Preceptor de la division
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_pide_las_divisiones_con_el_curso_y_el_ciclo_lectivo_del_store()
	{
		var contexto = new Contexto();

		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		_ = contexto.ServicioDivision.Received(1).ListarDivisionesAsync(new ListarDivisionesRequest(contexto.CursoID, CicloDelStore));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_toma_la_letra_y_el_preceptor_de_la_division_del_store_y_no_de_otra()
	{
		var contexto = new Contexto();
		contexto.Devolver(new DivisionResponse(Guid.NewGuid(), "Z", Guid.NewGuid(), "Otro, Preceptor", 0),
						  new DivisionResponse(contexto.DivisionID, "B", Guid.NewGuid(), "Perez, Ana", 0));

		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.ViewModel.Division.ShouldBe("B");
		contexto.ViewModel.Preceptor.ShouldBe("Perez, Ana");
		contexto.ViewModel.TienePreceptor.ShouldBeTrue();
	}

	/// <summary>
	/// La guarda de UI-DIV-1 trasladada a esta pantalla: sin preceptor, <c>AbrirPlanillaAsync</c>
	/// termina siempre en <c>SinDatosPreceptorException</c>, asi que el boton no se habilita en vez
	/// de dejar que el usuario descubra la regla chocandose con ella.
	/// </summary>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Sin_preceptor_asignado_no_se_puede_abrir_la_planilla()
	{
		var contexto = new Contexto();
		contexto.DevolverDivisionSinPreceptor();

		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.ViewModel.TienePreceptor.ShouldBeFalse();
		contexto.ViewModel.AbrirPlanillaCommand.CanExecute(null).ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Con_preceptor_asignado_y_sin_planilla_se_puede_abrir()
	{
		var contexto = new Contexto();

		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.ViewModel.AbrirPlanillaCommand.CanExecute(null).ShouldBeTrue();
	}

	/// <summary>
	/// Los dos avisos se excluyen: sin preceptor el de "abri la planilla" apuntaria a un boton
	/// deshabilitado, asi que la pantalla muestra el del preceptor y nada mas.
	/// </summary>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task El_aviso_de_sin_planilla_no_se_muestra_cuando_falta_el_preceptor()
	{
		var contexto = new Contexto();
		contexto.DevolverDivisionSinPreceptor();

		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.ViewModel.HayPlanilla.ShouldBeFalse();
		contexto.ViewModel.MostrarAvisoSinPlanilla.ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task El_aviso_de_sin_planilla_se_muestra_cuando_hay_preceptor_y_no_hay_planilla()
	{
		var contexto = new Contexto();

		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.ViewModel.MostrarAvisoSinPlanilla.ShouldBeTrue();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Con_planilla_abierta_no_se_muestra_ninguno_de_los_dos_avisos()
	{
		var contexto = new Contexto();
		var cursante = CrearCursante("Gomez, Juan");
		contexto.DevolverCursantes(cursante);
		contexto.DevolverPlanilla(CrearPlanilla(contexto.DivisionID,
												contexto.ViewModel.Fecha,
												new List<RegistroAsistenciaResponse> { CrearRegistro(cursante.CursanteID) }));

		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.ViewModel.MostrarAvisoSinPlanilla.ShouldBeFalse();
		contexto.ViewModel.TienePreceptor.ShouldBeTrue();
	}
	#endregion
}
