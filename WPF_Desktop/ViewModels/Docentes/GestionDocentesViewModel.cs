using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core.ServicioDocentes.DTOs.Requests;
using Core.ServicioDocentes.DTOs.Responses;
using Core.ServicioDocentes;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Data;
using System;
using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;
using WPF_Desktop.ViewModels.Shared.Messages;

namespace WPF_Desktop.ViewModels.Docentes;

internal partial class GestionDocentesViewModel : ObservableValidator
{
	#region Servicios
	private readonly IServicioDocente _servicioDocentes;
	private readonly IDialogService _dialogService;
	#endregion

	#region NavigationService
	private readonly INavigationService _registrarDocenteNavigationService;
	private readonly INavigationService _perfilDocenteNavigationService;
	private readonly INavigationService _gestionPuestosNavigationService;
	private readonly INavigationService _gestionLicenciasNavigationService;
	#endregion

	private LegajoStore _perfilBuscadoStore;

	/// <summary>
	/// Texto del filtro. No lleva <c>[Required]</c>: la pantalla arranca con el listado
	/// completo y el filtro es opcional.
	/// </summary>
	/// <remarks>
	/// El atributo anterior tenía un efecto que no se veía venir: con el campo vacío
	/// —es decir, al abrir la pantalla— <c>HasErrors</c> quedaba en <c>true</c>, y como
	/// <c>CanExecuteBuscarCommand</c> era <c>!HasErrors</c>, el botón de buscar nacía
	/// deshabilitado. Había que escribir algo para poder hacer cualquier cosa.
	/// </remarks>
	[ObservableProperty]
	private string _searchedText = string.Empty;

	[ObservableProperty]
	private bool _cargando;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(DesafectarCommand))]
	[NotifyCanExecuteChangedFor(nameof(EliminarCommand))]
	[NotifyCanExecuteChangedFor(nameof(NavigationCommand))]
	private LegajoDocenteViewModel? _legajoDocenteSeleccionado;

	[ObservableProperty]
	private ObservableCollection<LegajoDocenteViewModel> _legajosDocentes = new();

	/// <summary>
	/// Vista filtrada del listado. El filtro se aplica en memoria sobre lo ya cargado, así
	/// que responde a cada tecla sin ir a la base.
	/// </summary>
	public ICollectionView DocentesFiltrados { get; }

	/// <summary>Resumen del índice: "24 docentes" o "3 de 24".</summary>
	public string Resumen
	{
		get
		{
			int total = LegajosDocentes.Count;
			if (total is 0)
			{
				return Cargando ? "Cargando…" : "Sin docentes activos";
			}

			int visibles = 0;
			foreach (var _ in DocentesFiltrados)
			{
				visibles++;
			}

			return visibles == total
				? $"{ total } { (total is 1 ? "docente" : "docentes") }"
				: $"{ visibles } de { total }";
		}
	}

	#region Commands
	public IRelayCommand RegistrarCommand { get; }
	public IAsyncRelayCommand CargarCommandAsync { get; }
	public IAsyncRelayCommand DesafectarCommand { get; }
	public IAsyncRelayCommand EliminarCommand { get; }
	public IRelayCommand NavigationCommand { get; }
	#endregion


	public GestionDocentesViewModel(IServicioDocente servicioDocentes,
									IDialogService dialogService,
									INavigationService registrarDocenteNavigationService,
									INavigationService perfilDocenteNavigationService,
									INavigationService gestionPuestoNavigationService,
									INavigationService gestionLicenciasNavigationService,
									LegajoStore perfilBuscadoStore)
	{
		_servicioDocentes = servicioDocentes;
		_dialogService = dialogService;
		_registrarDocenteNavigationService = registrarDocenteNavigationService;
		_perfilDocenteNavigationService = perfilDocenteNavigationService;
		_gestionPuestosNavigationService = gestionPuestoNavigationService;
		_gestionLicenciasNavigationService = gestionLicenciasNavigationService;
		_perfilBuscadoStore = perfilBuscadoStore;

		DocentesFiltrados = CollectionViewSource.GetDefaultView(LegajosDocentes);
		DocentesFiltrados.Filter = CoincideConFiltro;

		RegistrarCommand = new RelayCommand<string>(_ => _registrarDocenteNavigationService.Navigate());
		CargarCommandAsync = new AsyncRelayCommand(ExecuteCargarCommandAsync);
		DesafectarCommand = new AsyncRelayCommand(ExecuteDesafectarCommandAsync, CanExecuteDesafectarCommand);
		EliminarCommand = new AsyncRelayCommand(ExecuteEliminarCommandAsync, CanExecuteEliminarCommand);
		NavigationCommand = new RelayCommand<string>(ExecuteNavigationCommand, CanExecuteNavigationCommand);
	}

	#region Filtro
	/// <summary>
	/// Coincidencia por nombre o por legajo, sin distinguir mayúsculas ni acentos de más: el
	/// personal de secretaría busca tanto por apellido como por número de legajo.
	/// </summary>
	private bool CoincideConFiltro(object item)
	{
		if (string.IsNullOrWhiteSpace(SearchedText))
		{
			return true;
		}

		if (item is not LegajoDocenteViewModel docente)
		{
			return false;
		}

		string filtro = SearchedText.Trim();

		// Ojo con los nombres: en LegajoDocenteViewModel la región "Properties" escrita a mano
		// —con DNI, CUIL y compañía— está dentro de un /* */, así que las propiedades reales
		// son las que genera [ObservableProperty] a partir de los campos.
		return docente.NombreCompleto.Contains(filtro, StringComparison.OrdinalIgnoreCase)
			|| docente.Legajo.Contains(filtro, StringComparison.OrdinalIgnoreCase)
			|| docente.DocumentoNacionalIdentidad.Contains(filtro, StringComparison.OrdinalIgnoreCase);
	}

	partial void OnSearchedTextChanged(string value)
	{
		DocentesFiltrados.Refresh();
		OnPropertyChanged(nameof(Resumen));
	}

	partial void OnCargandoChanged(bool value) =>
		OnPropertyChanged(nameof(Resumen));
	#endregion

	#region CargarCommand
	/// <summary>
	/// Trae el listado completo de docentes activos al abrir la pantalla.
	/// </summary>
	/// <remarks>
	/// <c>IServicioDocente.ListarDocentesActivosAsync</c> ya existía y no la llamaba nadie: la
	/// pantalla obligaba a escribir un nombre y apretar "Buscar" antes de mostrar una sola
	/// fila. Eso convierte el índice en una adivinanza —hay que saber a quién se busca para
	/// poder ver quién hay— y es el motivo principal por el que la pantalla se sentía vacía.
	/// </remarks>
	private async Task ExecuteCargarCommandAsync()
	{
		Cargando = true;

		try
		{
			var response = await _servicioDocentes.ListarDocentesActivosAsync();

			LegajosDocentes.Clear();
			foreach (var legajoDocente in response)
			{
				LegajosDocentes.Add(new LegajoDocenteViewModel(legajoDocente));
			}

			DocentesFiltrados.Refresh();
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "No se pudo cargar el listado de docentes");
		}
		finally
		{
			Cargando = false;
			OnPropertyChanged(nameof(Resumen));
		}
	}
	#endregion

	#region DesafectarCommand
	/// <remarks>
	/// La baja blanda: cierra el período del legajo y lo marca inactivo, sin borrar nada. Hasta el
	/// 2026-11-06 era lo único que había, y se llamaba "Quitar": un soft delete con nombre de
	/// borrado, que es el defecto que CORE-24 relevó. Ahora son dos botones con dos verbos distintos.
	/// </remarks>
	private bool CanExecuteDesafectarCommand() =>
		LegajoDocenteSeleccionado is not null;

	private async Task ExecuteDesafectarCommandAsync()
	{
		if (LegajoDocenteSeleccionado is null)
		{
			return;
		}

		string nombre = LegajoDocenteSeleccionado.NombreCompleto;
		string legajo = LegajoDocenteSeleccionado.Legajo;

		if (!_dialogService.ConfirmarAccion(SolicitudDeDialogo.Destructiva(
				titulo: $"Desafectar a { nombre } del personal docente",
				consecuencia: $"El legajo { legajo } se cierra con la fecha de hoy y queda inactivo. "
							+ "No se borra nada: sus licencias, sus puestos y sus designaciones se "
							+ "conservan, y el legajo sigue consultable. Para borrarlo del sistema "
							+ "está la acción Eliminar.",
				verbo: "Desafectar")))
		{
			return;
		}

		try
		{
			await _servicioDocentes.DesafectarDocenteAsync(new DocenteIDRequest(LegajoDocenteSeleccionado.DocenteID));
			LegajoDocenteSeleccionado = null;

			await ExecuteCargarCommandAsync();

			_dialogService.MostrarInformacion($"{ nombre } quedó desafectado.", "Docente desafectado");
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "No se pudo desafectar el docente");
		}
	}
	#endregion

	#region EliminarCommand
	private bool CanExecuteEliminarCommand() =>
		LegajoDocenteSeleccionado is not null;

	/// <remarks>
	/// El borrado físico. El texto anterior de este diálogo afirmaba dos cosas falsas: que "las
	/// cátedras donde esté designado quedan sin docente en funciones" —no pasaba nada, el caso de uso
	/// sólo desafectaba— y que "sus licencias y puestos ya registrados se conservan", que era cierto
	/// justamente porque no borraba. Ahora borra, y las cifras salen del caso de uso.
	/// </remarks>
	private async Task ExecuteEliminarCommandAsync()
	{
		if (LegajoDocenteSeleccionado is null)
		{
			return;
		}

		string nombre = LegajoDocenteSeleccionado.NombreCompleto;
		string legajo = LegajoDocenteSeleccionado.Legajo;
		var request = new DocenteIDRequest(LegajoDocenteSeleccionado.DocenteID);

		ImpactoEliminacionDocenteResponse impacto;
		try
		{
			impacto = await _servicioDocentes.ConsultarImpactoEliminacionDocenteAsync(request);
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "No se pudo averiguar qué se perdería");
			return;
		}

		// Si Core va a rechazar, no se pregunta: se dice qué hay que desarmar antes. Pedirle al
		// usuario que confirme algo que no va a pasar es el defecto de forma que el curso también
		// evita con su guarda de divisiones.
		if (impacto.EstaBloqueado)
		{
			_dialogService.MostrarAdvertencia(DescribirBloqueo(impacto, nombre), "No se puede eliminar");
			return;
		}

		if (!_dialogService.ConfirmarAccion(SolicitudDeDialogo.Destructiva(
				titulo: $"Eliminar el legajo de { nombre }",
				consecuencia: DescribirConsecuenciaDelDocente(impacto, legajo),
				verbo: "Eliminar docente")))
		{
			return;
		}

		try
		{
			await _servicioDocentes.EliminarDocenteAsync(request);
			LegajoDocenteSeleccionado = null;

			// Recargar: dejar la fila borrada en pantalla es la forma más rápida de que
			// alguien vuelva a operar sobre un registro que ya no existe.
			await ExecuteCargarCommandAsync();

			_dialogService.MostrarInformacion($"{ nombre } ya no figura en el sistema.", "Docente eliminado");
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "No se pudo eliminar el docente");
		}
	}

	/// <summary>Por qué <c>Core</c> va a rechazar el borrado, y qué hacer antes.</summary>
	internal static string DescribirBloqueo(ImpactoEliminacionDocenteResponse impacto, string nombre)
	{
		if (impacto.EsElUltimoAdministrador)
		{
			return $"{ nombre } tiene la única cuenta con rol Administrador. Si se elimina, nadie "
				 + "puede crear usuarios ni asignar roles, y el sistema queda sin forma de "
				 + "recuperar su administración desde la aplicación. Hay que darle el rol "
				 + "Administrador a otra cuenta antes.";
		}

		string designaciones = impacto.DesignacionesVigentes is 1
			? "1 designación vigente"
			: $"{ impacto.DesignacionesVigentes } designaciones vigentes";

		return $"{ nombre } tiene { designaciones } en cátedras. Hay que finalizarlas o "
			 + "reemplazarlas antes de eliminar el legajo: la cátedra pertenece a la materia y no "
			 + "al docente, así que el borrado no puede dejarla sin ocupante por su cuenta. "
			 + "Si lo que buscás es darlo de baja conservando el legajo, usá Desafectar.";
	}

	/// <summary>
	/// El texto de la consecuencia, con las cifras exactas de lo que el borrado destruye.
	/// </summary>
	/// <remarks>
	/// Dice que las planillas de asistencia conservan la firma <strong>sin un número</strong>: es un
	/// hecho fijo de la operación y contarlo exigiría recorrer <c>planilla_asistencia</c> entera, que
	/// se quedó sin índice por <c>preceptor_id</c> al quitarse la clave ajena. Vale decirlo igual,
	/// porque es lo único del borrado que deja un identificador apuntando a alguien que ya no está.
	/// </remarks>
	internal static string DescribirConsecuenciaDelDocente(ImpactoEliminacionDocenteResponse impacto, string legajo)
	{
		string planillas = "Las planillas de asistencia que haya firmado se conservan, con su firma.";

		if (!impacto.TieneVinculos)
		{
			return $"El legajo { legajo } se borra del sistema, junto con sus puestos. "
				 + $"{ planillas } Esta acción no se puede deshacer.";
		}

		var partes = new List<string>();

		if (impacto.Licencias > 0)
		{
			partes.Add(impacto.Licencias is 1 ? "1 licencia" : $"{ impacto.Licencias } licencias");
		}

		if (impacto.DesignacionesCerradas > 0)
		{
			partes.Add(impacto.DesignacionesCerradas is 1
				? "1 designación ya finalizada"
				: $"{ impacto.DesignacionesCerradas } designaciones ya finalizadas");
		}

		if (impacto.TieneUsuario)
		{
			partes.Add("su usuario del sistema, con los roles que tenga");
		}

		string lista = partes.Count switch
		{
			0 => string.Empty,
			1 => partes[0],
			_ => $"{ string.Join(", ", partes.Take(partes.Count - 1)) } y { partes.Last() }"
		};

		string borrado = partes.Count > 0
			? $"Se borran { lista }, además del legajo { legajo } y sus puestos."
			: $"Se borra el legajo { legajo } y sus puestos.";

		string preceptor = impacto.DivisionesQueQuedanSinPreceptor > 0
			? " " + (impacto.DivisionesQueQuedanSinPreceptor is 1
					? "1 división queda sin preceptor"
					: $"{ impacto.DivisionesQueQuedanSinPreceptor } divisiones quedan sin preceptor")
				  + " y hay que asignarle otro para poder abrir su planilla del día."
			: string.Empty;

		return $"{ borrado }{ preceptor } { planillas } Esta acción no se puede deshacer.";
	}
	#endregion

	#region NavigationCommand
	private bool CanExecuteNavigationCommand(object obj) =>
		LegajoDocenteSeleccionado is not null;

	private void ExecuteNavigationCommand(object obj)
	{
		if (LegajoDocenteSeleccionado is null)
		{
			return;
		}

		_perfilBuscadoStore.PersonaID = LegajoDocenteSeleccionado.DocenteID;
		_perfilBuscadoStore.Documento = LegajoDocenteSeleccionado.DocumentoNacionalIdentidad;

		switch (obj)
		{
			case "Licencia":
				_gestionLicenciasNavigationService.Navigate();
				break;
			case "Perfil":
				// El ID sale del elegido y no del store: acá ya está garantizado no nulo por la
				// guarda de arriba, y el store lo expone como Guid? desde UI-34.
				WeakReferenceMessenger.Default.Send(new DocenteSeleccionadoMessage(LegajoDocenteSeleccionado.DocenteID));
				_perfilDocenteNavigationService.Navigate();
				break;
			case "Puesto":
				_gestionPuestosNavigationService.Navigate();
				break;
		}
	}
	#endregion
}
