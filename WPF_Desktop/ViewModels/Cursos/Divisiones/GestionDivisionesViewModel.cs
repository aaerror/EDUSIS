using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core.ServicioDivisiones.DTOs.Requests;
using Core.ServicioDivisiones.DTOs.Responses;
using Core.ServicioDivisiones;
using Core.Shared.DTOs.Personas.Requests;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System;
using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;
using WPF_Desktop.ViewModels.Docentes;
using WPF_Desktop.ViewModels.Shared.Messages;

namespace WPF_Desktop.ViewModels.Cursos.Divisiones;

internal partial class GestionDivisionesViewModel : ObservableValidator, IUbicacion
{
	#region Service
	private readonly IServicioDivision _servicioDivision;
	private readonly INavigationService _gestionCursosNavigationService;
	private readonly INavigationService _gestionCursantesNavigationService;
	private readonly INavigationService _gestionAsistenciasNavigationService;
	private readonly INavigationService _buscarDocenteModalNavigationService;
	#endregion

	#region Mensajeria
	private readonly IMessenger _messenger;
	#endregion

	#region Stores
	private readonly CursoStore _cursoStore;
	private readonly DivisionStore _divisionStore;
	private readonly CicloLectivoStore _cicloLectivoStore;
	private readonly BuscarContextoStore _buscarContextoStore;
	#endregion

	#region Dialogos
	private readonly IDialogService _dialogService;
	#endregion

	#region Request
	private EliminarDivisionRequest request;
	#endregion

	private ObservableCollection<DivisionViewModel> _divisiones;

	#region Formulario
	/// <summary>
	/// La letra de la división nueva. El sistema la propone al cargar la pantalla
	/// (<c>ProponerDescripcionDivisionAsync</c>) y el usuario la puede cambiar: es la decisión de
	/// DIV-1. La validación de acá es de forma —una sola letra— y no reemplaza a la del dominio:
	/// que esa letra no esté ya registrada en el curso lo decide <c>AgregarDivisionAsync</c> con
	/// <c>DivisionDuplicadaException</c>, porque es una regla que depende del estado de la base.
	/// </summary>
	[NotifyCanExecuteChangedFor(nameof(RegistrarCommandAsync))]
	[NotifyDataErrorInfo]
	[Required(ErrorMessage = "Indique la letra de la división.")]
	[RegularExpression(@"^[A-Za-z]$", ErrorMessage = "La división se identifica con una sola letra.")]
	[ObservableProperty]
	private string _descripcion = string.Empty;
	#endregion

	[ObservableProperty]
	private int _totalDivisiones;
	[ObservableProperty]
	private string _curso;
	[ObservableProperty]
	private string _nivelEducativo;

	#region Flags
	[ObservableProperty]
	private bool _habilitarDivisiones;
	#endregion

	#region Notificaciones
	[ObservableProperty]
	private string _message = string.Empty;
	[ObservableProperty]
	private bool _habilitarNotificacion;
	#endregion

	[NotifyCanExecuteChangedFor(nameof(EliminarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(NavigationCommand))]
	[NotifyCanExecuteChangedFor(nameof(RegistrarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(AsignarPreceptorCommand))]
	[NotifyPropertyChangedFor(nameof(VerboPreceptor))]
	[ObservableProperty]
	private DivisionViewModel _division;
	[ObservableProperty]
	private ListCollectionView _listCollectionDocentes;

	/// <summary>
	/// La etiqueta del único botón de preceptor: dice lo que va a pasar con la división elegida.
	/// </summary>
	/// <remarks>
	/// Antes eran DOS botones excluyentes, "Asignar preceptor" y "Cambiar preceptor", uno al lado
	/// del otro y habilitados por condiciones inversas: siempre había uno muerto a la vista, y la
	/// regla —que no se puede asignar sobre un cargo ocupado— se aprendía mirando cuál se encendía.
	/// Un solo botón que se renombra dice la misma regla sin gastar un control en deshabilitarse.
	/// </remarks>
	public string VerboPreceptor =>
		Division?.PreceptorID is null ? "Asignar preceptor" : "Cambiar preceptor";

	#region Commands
	public IAsyncRelayCommand CargarDivisionesCommandAsync { get; }
	public IAsyncRelayCommand EliminarCommandAsync { get; }
	public IAsyncRelayCommand RegistrarCommandAsync { get; }
	public IRelayCommand AsignarPreceptorCommand { get; }
	public IAsyncRelayCommand PreceptorElegidoCommandAsync { get; }

	public IRelayCommand NavigationCommand { get; }
	public IRelayCommand VolverCommand { get; }
	#endregion

	public IReadOnlyList<Migaja> Ruta { get; }


	/// <remarks>
	/// ORDEN DE LOS CUATRO <see cref="INavigationService"/>: los cuatro son del mismo tipo y la factory
	/// lambda de <c>WPF_DesktopDI</c> los distingue SÓLO por posición. Invertirlos compila y rompe la
	/// navegación en silencio. Orden obligatorio: 1) <paramref name="gestionCursosNavigationService"/>
	/// (volver a cursos), 2) <paramref name="gestionCursantesNavigationService"/> (ir a los alumnos de
	/// la división), 3) <paramref name="gestionAsistenciasNavigationService"/> (ir a la asistencia de
	/// la división), 4) <paramref name="buscarDocenteModalNavigationService"/> (abrir el buscador de
	/// docentes en el modal). Los tres primeros son los destinos del drill-down y van agrupados; el del
	/// modal queda último porque no es una pantalla de este recorrido.
	/// </remarks>
	public GestionDivisionesViewModel(IServicioDivision servicioDivisiones,
									  INavigationService gestionCursosNavigationService,
									  INavigationService gestionCursantesNavigationService,
									  INavigationService gestionAsistenciasNavigationService,
									  INavigationService buscarDocenteModalNavigationService,
									  CursoStore cursoStore,
									  DivisionStore divisionStore,
									  CicloLectivoStore cicloLectivoStore,
									  BuscarContextoStore buscarContextoStore,
									  IDialogService dialogService,
									  IMessenger messenger)
	{
		_servicioDivision = servicioDivisiones;
		_gestionCursosNavigationService = gestionCursosNavigationService;
		_gestionCursantesNavigationService = gestionCursantesNavigationService;
		_gestionAsistenciasNavigationService = gestionAsistenciasNavigationService;
		_buscarDocenteModalNavigationService = buscarDocenteModalNavigationService;
		_cursoStore = cursoStore;
		_divisionStore = divisionStore;
		_cicloLectivoStore = cicloLectivoStore;
		_buscarContextoStore = buscarContextoStore;
		_dialogService = dialogService;
		_messenger = messenger;

		Curso = _cursoStore.Curso.Grado.ToString();
		NivelEducativo = _cursoStore.Curso.NivelEducativo.ToString();

		_divisiones = new ObservableCollection<DivisionViewModel>();

		CargarDivisionesCommandAsync = new AsyncRelayCommand(CargarDivisionesAsync);
		EliminarCommandAsync = new AsyncRelayCommand<string>(ExecuteEliminarCommandAsync, CanExecuteEliminarCommand);
		RegistrarCommandAsync = new AsyncRelayCommand<string>(ExecuteRegistrarCommandAsync, CanExecuteRegistrarCommand);

		AsignarPreceptorCommand = new RelayCommand(ExecuteAsignarPreceptorCommand, CanExecuteAsignarPreceptorCommand);
		PreceptorElegidoCommandAsync = new AsyncRelayCommand<LegajoDocenteViewModel>(ExecutePreceptorElegidoCommandAsync);

		NavigationCommand = new RelayCommand<string>(ExecuteNavigationCommand, CanExecuteNavigationCommand);
		VolverCommand = new RelayCommand(gestionCursosNavigationService.Navigate);

		Ruta = new[]
		{
			Migaja.Hacia("CURSOS", VolverCommand),
			Migaja.Actual($"{ Grados.Decir(Curso).ToUpper() } · { NivelEducativo.ToUpper() } · DIVISIONES")
		};

		// El buscador vive en el modal y no conoce a sus llamadores: el elegido vuelve por acá y la
		// transacción TERMINA en este comando. Antes el mensaje sólo dejaba el docente en una
		// propiedad y abría una tarjeta al pie de la página para que el usuario confirmara ahí.
		_messenger.Register<GestionDivisionesViewModel, DocenteElegidoMessage>(this, (receptor, mensaje) =>
			receptor.PreceptorElegidoCommandAsync.Execute(mensaje.Docente));
	}

	private async Task CargarDivisionesAsync()
	{
		_divisiones.Clear();
		try
		{
			var divisiones = await _servicioDivision.ListarDivisionesAsync(new ListarDivisionesRequest(_cursoStore.Curso.CursoID, _cicloLectivoStore.CicloLectivo));
			TotalDivisiones = divisiones.Count;

			// La propuesta va antes del corte por lista vacía: el curso sin divisiones es justamente
			// el caso donde más sirve tener la letra precargada ("A"), y después del return no se
			// ejecutaría nada.
			await ProponerDescripcionAsync();

			if (divisiones.Count is 0)
			{
				HabilitarDivisiones = false;
				HabilitarNotificacion = true;

				Message = "No existen divisiones agregadas en el curso hasta el momento. Debe registrar al menos una división en el curso para poder trabajar.";

				return;
			}

			_divisiones = new ObservableCollection<DivisionViewModel>(divisiones.Select(x => new DivisionViewModel(x)));
			ListCollectionDocentes = new ListCollectionView(_divisiones);

			HabilitarDivisiones = true;
			HabilitarNotificacion = false;
		}
		catch (Exception ex)
		{
			HabilitarDivisiones = false;
			HabilitarNotificacion = true;

			Message = $"Error al cargar las materias del curso.\nError: { ex.Message }";
		}
	}

	/// <summary>
	/// Precarga la letra que propone el dominio. Va en su propio <c>try</c> y no en el de
	/// <c>CargarDivisionesAsync</c> a propósito: quedarse sin letras —el curso llegó a la "Z",
	/// <c>SinLetrasDisponiblesException</c>— no tiene que impedir ver ni administrar las divisiones
	/// que ya existen, que es todo lo demás que hace esta pantalla. El usuario se enterará al
	/// intentar registrar, con el mensaje del dominio, que es donde la regla vive.
	/// </summary>
	private async Task ProponerDescripcionAsync()
	{
		try
		{
			Descripcion = await _servicioDivision.ProponerDescripcionDivisionAsync(new ProponerDescripcionRequest(_cursoStore.Curso.CursoID));
		}
		catch (Exception)
		{
			Descripcion = string.Empty;
		}
	}

	#region AsignarPreceptorCommand
	/// <summary>
	/// Declara a qué división le está buscando preceptor y abre el modal. Es todo lo que hace: el
	/// trabajo contra <c>Core</c> ocurre en <see cref="PreceptorElegidoCommandAsync"/>, cuando el
	/// buscador devuelve al elegido.
	/// </summary>
	/// <remarks>
	/// Un solo comando para asignar y para cambiar. Son dos casos de uso distintos en <c>Core</c>
	/// —el cargo vacante y el cargo ocupado— pero UNA sola intención del usuario: poner a alguien al
	/// frente de esta división. Cuál de los dos corre lo decide el estado de la división al recibir
	/// al docente, no un flag que esta pantalla tenga que acordarse de prender y apagar.
	/// </remarks>
	private bool CanExecuteAsignarPreceptorCommand() =>
		Division is not null;

	private void ExecuteAsignarPreceptorCommand()
	{
		_buscarContextoStore.Declarar(ContextoDelBuscador());
		_buscarDocenteModalNavigationService.Navigate();
	}

	/// <summary>
	/// El encabezado con el que se abre el buscador: la división de destino y el estado del cargo.
	/// </summary>
	/// <remarks>
	/// ES LA PRIMERA LÍNEA QUE SE LEE ANTES DE ELEGIR, y por eso dice el destino y no la
	/// herramienta. El detalle nombra la consecuencia de cada caso: con el cargo vacante, que sin
	/// preceptor no hay planilla de asistencia; con el cargo ocupado, a quién se está reemplazando,
	/// que es el dato que convierte "cambiar preceptor" en una decisión informada.
	///
	/// EL DETALLE NOMBRA EL CARGO Y EL TÍTULO NOMBRA LA DIVISIÓN, sin repetirse. "El cargo está
	/// vacante" es como se dice en la escuela, pero solo —sin decir QUÉ cargo— obliga a deducirlo
	/// del título; "la preceptoría" lo nombra con la palabra de la institución y en una línea que
	/// no tiene que volver a decir de qué división se trata, porque el renglón de arriba acaba de
	/// decirlo. No hay una forma oficial en la jerga: se usa tanto "el cargo está vacante" como
	/// "la preceptoría de tal división está vacante".
	///
	/// En el caso ocupado el preceptor saliente se nombra DOS VECES a propósito. "El docente que
	/// elijas lo reemplaza y pierde el acceso" deja sin sujeto claro quién pierde el acceso: el
	/// que entra o el que sale. Repetir el nombre cuesta unas palabras y no deja lugar a la duda
	/// en el único renglón donde la duda importa.
	/// </remarks>
	private BuscarContexto ContextoDelBuscador()
	{
		var division = $"{ Grados.Decir(Curso) } { Division.Descripcion }";

		return Division.PreceptorID is null
			? new BuscarContexto(Titulo: $"Preceptor de { division }",
								 Detalle: "La preceptoría está vacante. Sin preceptor no se puede abrir la planilla de asistencia de la división.",
								 Verbo: "Asignar preceptor")
			: new BuscarContexto(Titulo: $"Preceptor de { division }",
								 Detalle: $"La preceptoría está a cargo de { Division.Preceptor }. El docente que elijas toma el cargo y { Division.Preceptor } pierde el acceso a las planillas de la división.",
								 Verbo: "Cambiar preceptor");
	}
	#endregion

	#region EliminarCommandAsync
	private bool CanExecuteEliminarCommand(object obj) => obj switch
	{
		"Division" => Division is not null,
		"Preceptor" => Division is not null && Division.PreceptorID is not null,
		_ => false
	};

	private async Task ExecuteEliminarCommandAsync(object obj)
	{
		switch (obj)
		{
			case "Division":
				// El texto que había acá afirmaba que "Core rechaza la operación si todavía tiene
				// cursantes inscriptos: primero hay que darlos de baja o cambiarlos de división", y
				// desde el 2026-11-06 es FALSO: el borrado cascadea la matrícula entera. Es el mismo
				// defecto que tenía el diálogo de alumno antes de ee75a3c —un texto que describía un
				// comportamiento inexistente— así que ahora las cifras salen del caso de uso.
				var requestDivision = new EliminarDivisionRequest(_cursoStore.Curso.CursoID, Division.DivisionID);

				ImpactoEliminacionDivisionResponse impacto;
				try
				{
					// Antes del diálogo y en su propio try: si no se puede averiguar qué se pierde, no
					// hay nada que preguntar, y el usuario tiene que enterarse de eso y no de un
					// cartel genérico después de haber confirmado.
					impacto = await _servicioDivision.ConsultarImpactoEliminacionDivisionAsync(requestDivision);
				}
				catch (Exception ex)
				{
					_dialogService.MostrarError(ex.Message, "No se pudo averiguar qué se perdería");
					return;
				}

				if (!_dialogService.ConfirmarAccion(SolicitudDeDialogo.Destructiva(
						titulo: $"Eliminar la división { Division.Descripcion } de { Grados.Decir(Curso) }",
						consecuencia: DescribirConsecuenciaDeLaDivision(impacto),
						verbo: "Eliminar división")))
				{
					return;
				}

				try
				{
					await _servicioDivision.EliminarDivisionAsync(requestDivision);

					await CargarDivisionesAsync();
					_dialogService.MostrarInformacion($"División { Division.Descripcion } de { Grados.Decir(Curso) }.", "División eliminada");
				}
				catch (Exception ex)
				{
					_dialogService.MostrarError(ex.Message, "No se pudo eliminar la división");
				}

				break;

			case "Preceptor":
				if (!_dialogService.ConfirmarAccion(SolicitudDeDialogo.Destructiva(
						titulo: $"Quitar a { Division.Preceptor } de la división { Division.Descripcion }",
						consecuencia: "La división queda sin preceptor, y sin preceptor no se puede abrir la planilla "
									+ "de asistencia del día. Se le puede asignar otro en cualquier momento.",
						verbo: "Quitar preceptor")))
				{
					return;
				}

				try
				{
					var request = new EliminarPreceptorRequest(_cursoStore.Curso.CursoID, Division.DivisionID);
					await _servicioDivision.QuitarPreceptorAsync(request);

					await CargarDivisionesAsync();
					_dialogService.MostrarInformacion($"La división { Division.Descripcion } quedó sin preceptor.", "Preceptor dado de baja");
				}
				catch (Exception ex)
				{
					_dialogService.MostrarError(ex.Message, "No se pudo quitar el preceptor");
				}
				break;
		}
	}

	/// <summary>
	/// El texto de la consecuencia, con las cifras exactas de lo que el borrado destruye.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Es <c>static</c> y no toca estado del ViewModel a propósito, por el mismo motivo que su gemelo
	/// <c>GestionAlumnosViewModel.DescribirConsecuencia</c>: la redacción y la pluralización cambian
	/// según los números, y así se prueban sin construir el ViewModel con sus dependencias.
	/// </para>
	/// <para>
	/// Nombra <strong>calificaciones</strong> y no "boletines". El boletín es un PDF que se emite al
	/// vuelo (<c>IServicioDocumento.GenerarBoletinCalificacionesAsync</c>), no una entidad
	/// persistida: lo que el borrado destruye son filas de <c>calificacion</c>, y después de eso el
	/// boletín simplemente no se puede volver a emitir.
	/// </para>
	/// <para>
	/// Las designaciones se nombran aunque el caso de uso no las borre una por una —caen por
	/// <c>FK_SITUACION_REVISTA_CATEDRA</c>, que es <c>Cascade</c>—: que el borrado sea implícito no
	/// las hace menos destruidas.
	/// </para>
	/// </remarks>
	internal static string DescribirConsecuenciaDeLaDivision(ImpactoEliminacionDivisionResponse impacto)
	{
		if (!impacto.TieneHistorial)
		{
			return "La división no tiene inscripciones, planillas de asistencia ni cátedras registradas. "
				 + "Esta acción no se puede deshacer.";
		}

		var partes = new List<string>();

		if (impacto.Inscripciones > 0)
		{
			partes.Add(Contar(impacto.Inscripciones, "inscripción", "inscripciones"));
		}

		if (impacto.Calificaciones > 0)
		{
			partes.Add(Contar(impacto.Calificaciones, "calificación", "calificaciones"));
		}

		if (impacto.RegistrosAsistencia > 0)
		{
			partes.Add(Contar(impacto.RegistrosAsistencia, "registro de asistencia", "registros de asistencia"));
		}

		string ciclos = impacto.CiclosLectivos.Count switch
		{
			0 => string.Empty,
			1 => $" del ciclo { impacto.CiclosLectivos.First() }",
			_ => $" de los ciclos { Enumerar(impacto.CiclosLectivos) }"
		};

		string planillas = impacto.Planillas > 0
			? " Se eliminan " + Contar(impacto.Planillas, "planilla de asistencia", "planillas de asistencia") + "."
			: string.Empty;

		string catedras = impacto.Catedras > 0
			? " Se eliminan " + Contar(impacto.Catedras, "cátedra", "cátedras")
				+ (impacto.Designaciones > 0
					? " con " + Contar(impacto.Designaciones, "designación de docente", "designaciones de docentes")
					: string.Empty)
				+ "."
			: string.Empty;

		string borrado = partes.Count > 0
			? $"Se borran { Enumerar(partes) }{ ciclos }."
			: string.Empty;

		return $"{ borrado }{ planillas }{ catedras } Esta acción no se puede deshacer.".TrimStart();
	}

	// Gemelos de los de GestionAlumnosViewModel. Dos copias es tolerable y las deja probables por
	// separado; una tercera pide extraerlos a WPF_Desktop/Shared/.
	private static string Contar(int cantidad, string singular, string plural) =>
		cantidad is 1 ? $"1 { singular }" : $"{ cantidad } { plural }";

	/// <summary>Une con comas y una "y" antes del último, como se escribe en castellano.</summary>
	private static string Enumerar(IReadOnlyCollection<string> partes) =>
		partes.Count switch
		{
			0 => string.Empty,
			1 => partes.First(),
			_ => $"{ string.Join(", ", partes.Take(partes.Count - 1)) } y { partes.Last() }"
		};
	#endregion


	#region NavigationCommand
	/// <remarks>
	/// Los tres pedían además <c>!HabilitarRegistrarDocente</c>: con la tarjeta de asignación
	/// abierta al pie, navegar la habría dejado a medio camino. La tarjeta ya no existe —la
	/// transacción entera ocurre en el modal, que es modal— así que no hay estado a medias que
	/// proteger y los destinos sólo dependen de que haya una división elegida.
	/// </remarks>
	private bool CanExecuteNavigationCommand(object obj) => obj switch
	{
		"Curso" => true,
		"Cursantes" => Division is not null,
		"Asistencias" => Division is not null,
		_ => false
	};

	private void ExecuteNavigationCommand(object obj)
	{
		switch (obj)
		{
			case "Curso":
				_gestionCursosNavigationService.Navigate();
				break;

			case "Cursantes":
				_divisionStore.Division = new DivisionSeleccionada(Division.DivisionID);
				_gestionCursantesNavigationService.Navigate();
				break;

			case "Asistencias":
				// La división es el parámetro de navegación: se escribe ANTES de navegar porque el
				// ViewModel destino la lee en su constructor, y el destino nace recién en Navigate().
				_divisionStore.Division = new DivisionSeleccionada(Division.DivisionID);
				_gestionAsistenciasNavigationService.Navigate();
				break;
		}
	}
	#endregion

	#region RegistrarCommandAsync
	/// <remarks>
	/// El caso "Docente" se fue a <see cref="AsignarPreceptorCommand"/>. Acá no pintaba: registrar
	/// una división crea un registro y asignar un preceptor modifica uno que ya existe, y lo único
	/// que los juntaba era el nombre del comando.
	/// </remarks>
	private bool CanExecuteRegistrarCommand(object obj) => obj switch
	{
		"Division" => !string.IsNullOrWhiteSpace(Descripcion) && !HasErrors,
		_ => false
	};

	private async Task ExecuteRegistrarCommandAsync (object obj)
	{
		switch (obj)
		{
			case "Division":
				// Se confirma porque la letra que se propone no es la que se guarda: el campo
				// viene precargado con la sugerencia de Core y el usuario pudo escribir otra.
				if (!_dialogService.ConfirmarAccion(SolicitudDeDialogo.Revisar(
						titulo: $"Crear la división { Descripcion.ToUpper() }",
						mensaje: $"Queda en { Grados.Decir(_cursoStore.Curso.Grado) } de { _cursoStore.Curso.NivelEducativo.ToLower() }, sin preceptor y sin cursantes.",
						verbo: "Crear división")))
				{
					return;
				}

				try
				{
					await _servicioDivision.AgregarDivisionAsync(new AgregarDivisionRequest(_cursoStore.Curso.CursoID, Descripcion));

					await CargarDivisionesAsync();
					_dialogService.MostrarInformacion($"División { Descripcion.ToUpper() } de { Grados.Decir(Curso) }. Asignale un preceptor para poder tomar asistencia.", "División creada");
				}
				catch (Exception ex)
				{
					_dialogService.MostrarError(ex.Message, "No se pudo crear la división");
				}

				break;
		}
	}
	#endregion

	#region PreceptorElegidoCommandAsync
	/// <summary>
	/// Cierra la transacción con el docente que devolvió el buscador.
	/// </summary>
	/// <remarks>
	/// ACÁ TERMINA EL RECORRIDO, y antes no: el docente elegido en el modal aterrizaba en una
	/// propiedad y abría una tarjeta al pie de la página cuyo único contenido era el nombre que el
	/// usuario acababa de elegir, más un botón para confirmarlo. Esa tarjeta no agregaba
	/// información —el docente es el único dato de esta operación— así que era un paso de
	/// confirmación disfrazado de formulario. El patrón viene de Situación de Revista, donde SÍ se
	/// gana el lugar porque después del docente todavía hay cargo, período y a quién reemplaza.
	///
	/// CUÁL DE LOS DOS CASOS DE USO CORRE LO DECIDE LA DIVISIÓN, no un flag. La intención se
	/// deducía de <c>ModificandoPreceptor</c>, que esta pantalla prendía antes de navegar y
	/// apagaba al terminar; cancelar el modal no lo apagaba, porque el buscador no conoce a sus
	/// llamadores. Preguntarle a <c>Division.PreceptorID</c> no se puede desincronizar: la grilla
	/// no se recarga mientras el modal está arriba.
	///
	/// Reemplazar al preceptor no es quitar y volver a asignar —eso dejaría el cargo vacante en el
	/// medio— sino <c>ModificarPreceptorAsync</c>, una sola unidad de trabajo.
	/// </remarks>
	private async Task ExecutePreceptorElegidoCommandAsync(LegajoDocenteViewModel docente)
	{
		if (docente is null || Division is null)
		{
			return;
		}

		try
		{
			if (Division.PreceptorID is null)
			{
				await AsignarPreceptorAsync(docente);
			}
			else
			{
				await ModificarPreceptorAsync(docente);
			}
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "Error en la operación");
		}
	}

	/// <remarks>
	/// NO PIDE CONFIRMACIÓN. El cargo está vacante: no saca a nadie de su puesto, se deshace con
	/// "Quitar preceptor" y el modal viene de decir, en su primera línea, a qué división se le
	/// estaba buscando preceptor. Un diálogo acá repetiría el encabezado que el usuario acaba de
	/// leer para tomar justamente esta decisión. El caso que sí saca a alguien —cambiar— conserva
	/// el suyo, en rojo.
	/// </remarks>
	private async Task AsignarPreceptorAsync(LegajoDocenteViewModel docente)
	{
		try
		{
			var request = new RegistrarPreceptorRequest(CursoID: _cursoStore.Curso.CursoID,
														DivisionID: Division.DivisionID,
														DocenteID: docente.DocenteID);

			await _servicioDivision.AsignarPreceptorAsync(request);

			var division = Division.Descripcion;

			await CargarDivisionesAsync();
			_dialogService.MostrarInformacion($"{ docente.NombreCompleto } quedó a cargo de { Grados.Decir(Curso) } { division }. Ya puede abrir y firmar las planillas de asistencia.", "Preceptor asignado");
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "No se pudo asignar el preceptor");
		}
	}

	private async Task ModificarPreceptorAsync(LegajoDocenteViewModel docente)
	{
		// Cambiar el preceptor saca a alguien de su cargo, así que se avisa en rojo: no es lo
		// mismo que asignar uno a una división que no tenía.
		if (!_dialogService.ConfirmarAccion(SolicitudDeDialogo.Destructiva(
				titulo: $"Pasar la división { Division.Descripcion } a { docente.NombreCompleto }",
				consecuencia: $"{ Division.Preceptor } deja el cargo de preceptor de { Grados.Decir(Curso) } { Division.Descripcion } "
							+ "y pierde el acceso a las planillas de asistencia de la división.",
				verbo: "Cambiar preceptor")))
		{
			return;
		}

		try
		{
			var request = new ModificarPreceptorRequest(CursoID: _cursoStore.Curso.CursoID,
														DivisionID: Division.DivisionID,
														DocenteID: docente.DocenteID);

			await _servicioDivision.ModificarPreceptorAsync(request);

			var division = Division.Descripcion;

			await CargarDivisionesAsync();
			_dialogService.MostrarInformacion($"{ Grados.Decir(Curso) } { division } quedó a cargo de { docente.NombreCompleto }.", "Preceptor cambiado");
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "No se pudo cambiar el preceptor");
		}
	}
	#endregion
}