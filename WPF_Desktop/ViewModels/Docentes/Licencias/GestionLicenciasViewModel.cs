using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioDocentes.DTOs.Requests;
using Core.ServicioLicencias.DTOs.Requests;
using Core.ServicioLicencias;
using Domain.Licencias;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System;
using Core.Shared.Seguridad;
using Domain.Usuarios;
using WPF_Desktop.Navigation;
using WPF_Desktop.Security;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;
using Microsoft.IdentityModel.Tokens;
using WPF_Desktop.ViewModels.Docentes;

namespace WPF_Desktop.ViewModels.Docentes.Licencias;

internal partial class GestionLicenciasViewModel : ObservableValidator, IUbicacion
{
	#region Servicios
	private readonly IServicioLicencia _servicioLicencia;
	private readonly IDialogService _dialogService;
	private readonly ISesionActual _sesion;
	#endregion

	/// <summary>
	/// Docente cuyas licencias se gestionan, fijado una sola vez al construir.
	/// </summary>
	/// <remarks>
	/// <c>LegajoStore.PersonaID</c> es <c>Guid?</c>, y la guarda va acá una vez en
	/// lugar de repetir un <c>.Value</c> en los seis sitios que arman requests. Llegar a esta
	/// pantalla sin docente elegido es un defecto de navegación y no un estado de la pantalla:
	/// <c>GestionDocentesViewModel</c> escribe el store antes de navegar.
	/// </remarks>
	private readonly Guid _docenteID;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(CancelarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(EditarCommand))]
	[NotifyCanExecuteChangedFor(nameof(GuardarCommandAsync))]
	private bool _habilitarEdicion;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(CancelarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(EditarCommand))]
	[NotifyCanExecuteChangedFor(nameof(GuardarCommandAsync))]
	private bool _habilitarDetalleLicencias;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(CancelarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(GuardarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(RegistrarCommand))]
	private bool _habilitarUpsert;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(CancelarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(GuardarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(RegistrarCommand))]
	private bool _habilitarRegistro;

	[ObservableProperty]
	private bool _habilitarMessage;

	[ObservableProperty]
	private string _message = string.Empty;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(CancelarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(EditarCommand))]
	[NotifyCanExecuteChangedFor(nameof(GuardarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(RegistrarCommand))]
	private LicenciaViewModel _licenciaSeleccionada;

	[ObservableProperty]
	private ObservableCollection<LicenciaViewModel> _licencias = new();

	/// <summary>Días que suma la prórroga de la licencia aprobada seleccionada.</summary>
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(GuardarCommandAsync))]
	private int _diasDeProrroga = 1;

	/// <summary>Último día de licencia que se le fija a una aprobada indefinida.</summary>
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(GuardarCommandAsync))]
	private DateTime? _fechaDeFin;

	#region Commands
	public IAsyncRelayCommand CargarLicenciasCommandAsync { get; }
	public IAsyncRelayCommand CancelarCommandAsync { get; }
	public IAsyncRelayCommand GuardarCommandAsync { get; }
	public IRelayCommand EditarCommand { get; }
	public IRelayCommand RegistrarCommand { get; }
	public IRelayCommand VolverCommand { get; }
	#endregion

	public IReadOnlyList<Migaja> Ruta { get; }


	public GestionLicenciasViewModel(IServicioLicencia servicioLicencia,
									 IDialogService dialogService,
									 INavigationService<GestionDocentesViewModel> gestionDocentesNavigationService,
									 LegajoStore perfilBuscadoStore,
									 ISesionActual sesion)
	{
		_servicioLicencia = servicioLicencia;
		_dialogService = dialogService;
		_sesion = sesion;
		_docenteID = perfilBuscadoStore.PersonaID
			?? throw new ArgumentNullException(nameof(perfilBuscadoStore), "Error al gestionar las licencias. Primero debe buscar un docente antes de continuar.");

		VolverCommand = new RelayCommand(gestionDocentesNavigationService.Navigate);

		Ruta = new[]
		{
			Migaja.Hacia("DOCENTES", VolverCommand),
			Migaja.Actual("LICENCIAS")
		};

		CargarLicenciasCommandAsync = new AsyncRelayCommand(ExecuteCargarLicenciasCommand);
		CancelarCommandAsync = new AsyncRelayCommand<string>(ExecuteCancelarCommand, CanExecuteCancelarCommand);
		GuardarCommandAsync = new AsyncRelayCommand<string>(ExecuteGuardarCommandAsync, CanExecuteGuardarCommand);
		// Cargar, editar y borrar la solicitud es GestionarDocentes; aprobarla o rechazarla, el
		// permiso de resolución que corresponde al artículo (ver PuedeResolver). Secretaría
		// Académica llega a esta pantalla sólo para resolver: sin esto vería habilitado un
		// "Registrar licencia" que Core le niega.
		EditarCommand = new RelayCommand<string>(ExecuteEditarCommand, _sesion.Permite<string>(Permiso.GestionarDocentes, CanExecuteEditarCommand));
		RegistrarCommand = new RelayCommand<string>(ExecuteRegistrarCommand, _sesion.Permite<string>(Permiso.GestionarDocentes, CanExecuteRegistrarCommand));

		HabilitarUpsert = false;
		HabilitarRegistro = false;
		HabilitarEdicion = false;
	}

	public async Task ExecuteCargarLicenciasCommand()
	{
		try
		{
			var request = new DocenteIDRequest(_docenteID);
			var licencias = await _servicioLicencia.BuscarLicenciasSegunDocenteAsync(request);

			if (licencias.IsNullOrEmpty())
			{
				Message = "Este docente no tiene licencias registradas. Cargá la primera con «Registrar licencia».";
				HabilitarMessage = true;
				HabilitarDetalleLicencias = false;
			}
			else
			{
				Licencias = new ObservableCollection<LicenciaViewModel>(licencias.Select(x => new LicenciaViewModel(x)));

				Message = string.Empty;
				HabilitarMessage = false;
				HabilitarDetalleLicencias = true;
			}
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "No se pudieron cargar las licencias");
		}
	}

	#region CancelarCommandAsync
	private bool CanExecuteCancelarCommand(object? obj)
	{
		switch (obj)
		{
			case "Editar":
				return HabilitarUpsert && HabilitarEdicion && LicenciaSeleccionada is not null;

			case "Nueva":
				return HabilitarUpsert && HabilitarRegistro;

			default:
				return false;
		}
	}

	private async Task ExecuteCancelarCommand(object? obj)
	{
		await ExecuteCargarLicenciasCommand();

		switch (obj)
		{
			case "Editar":
				HabilitarUpsert = false;
				HabilitarEdicion = false;

				break;

			case "Nueva":
				HabilitarUpsert = false;
				HabilitarRegistro = false;

				break;

			default:
				return;
		}
	}
	#endregion

	#region Licencia aprobada
	/// <summary>
	/// Hay una licencia aprobada y sin finalizar seleccionada en el listado, y la sesión puede
	/// operar sobre ella. Gobierna el panel de reintegro y prórroga.
	/// </summary>
	public bool MostrarLicenciaEnCurso =>
		PuedeResolver() && HabilitarDetalleLicencias && !HabilitarEdicion && LicenciaSeleccionada.EstaAprobada;

	// Cada acción del panel se ofrece sólo si el dominio la admite para la licencia elegida: la
	// prórroga suma días a un fin que existe, la fecha de fin se le pone a una indefinida y sólo
	// un artículo de plazo abierto puede quedar indefinido.
	public bool PuedeProrrogar =>
		MostrarLicenciaEnCurso && !LicenciaSeleccionada.EsIndefinida;

	public bool PuedeFijarFechaDeFin =>
		MostrarLicenciaEnCurso && LicenciaSeleccionada.EsIndefinida;

	public bool PuedeDeclararIndefinida =>
		MostrarLicenciaEnCurso && !LicenciaSeleccionada.EsIndefinida && LicenciaSeleccionada.AdmiteIndefinida;

	private void NotificarLicenciaEnCurso()
	{
		OnPropertyChanged(nameof(MostrarLicenciaEnCurso));
		OnPropertyChanged(nameof(PuedeProrrogar));
		OnPropertyChanged(nameof(PuedeFijarFechaDeFin));
		OnPropertyChanged(nameof(PuedeDeclararIndefinida));
	}

	partial void OnLicenciaSeleccionadaChanged(LicenciaViewModel value)
	{
		DiasDeProrroga = 1;
		FechaDeFin = null;
		NotificarLicenciaEnCurso();
	}

	partial void OnHabilitarDetalleLicenciasChanged(bool value) =>
		NotificarLicenciaEnCurso();

	partial void OnHabilitarEdicionChanged(bool value) =>
		NotificarLicenciaEnCurso();
	#endregion

	#region GuardarCommandAsync
	/// <summary>
	/// Si la sesión puede resolver la licencia seleccionada: aprobarla o rechazarla, y una vez
	/// aprobada, finalizarla o cambiar su duración. Es el mismo permiso que exige
	/// <c>ServicioLicencia</c> según el artículo; la seguridad sigue en <c>Core</c>.
	/// </summary>
	private bool PuedeResolver() =>
		LicenciaSeleccionada is not null
		&& _sesion.TienePermiso(LicenciaSeleccionada.ResolucionAcademica ? Permiso.ResolverLicenciasAcademicas : Permiso.ResolverLicencias);

	// Un mismo comando con nueve parámetros y dos permisos distintos: la guarda va por caso y no
	// envolviendo el CanExecute entero con Permite.
	private bool CanExecuteGuardarCommand(object? obj)
	{
		switch (obj)
		{
			case "Aprobar":
				return PuedeResolver() && HabilitarDetalleLicencias && !HabilitarEdicion && LicenciaSeleccionada.Estado.Equals(Enum.GetName(Estado.Pendiente));
			case "Rechazar":
				return PuedeResolver() && HabilitarDetalleLicencias && !HabilitarEdicion && LicenciaSeleccionada.Estado.Equals(Enum.GetName(Estado.Pendiente));
			case "Finalizar":
				return MostrarLicenciaEnCurso;
			case "Prorrogar":
				return PuedeProrrogar && DiasDeProrroga > 0;
			case "FijarFin":
				return PuedeFijarFechaDeFin && FechaDeFin.HasValue && FechaDeFin.Value.Date > DateTime.Today;
			case "Indefinida":
				return PuedeDeclararIndefinida;
			case "Editar":
				return _sesion.TienePermiso(Permiso.GestionarDocentes) && HabilitarUpsert && HabilitarEdicion && LicenciaSeleccionada is not null && !LicenciaSeleccionada.HasErrors;
			case "Nueva":
				return _sesion.TienePermiso(Permiso.GestionarDocentes) && HabilitarUpsert && HabilitarRegistro && LicenciaSeleccionada is not null && !LicenciaSeleccionada.HasErrors;
			case "Eliminar":
				return _sesion.TienePermiso(Permiso.GestionarDocentes) && HabilitarDetalleLicencias && !HabilitarEdicion && LicenciaSeleccionada is not null && LicenciaSeleccionada.Estado.Equals(Enum.GetName(Estado.Pendiente));
			default:
				return false;
		}
	}

	/// <summary>
	/// Las escrituras de la pantalla: aprobar, rechazar, editar, registrar y eliminar la solicitud,
	/// y sobre una aprobada, registrar el reintegro, prorrogarla, fijarle fin o declararla indefinida.
	/// </summary>
	/// <remarks>
	/// <para>
	/// QUÉ PREGUNTA Y QUÉ NO. Antes las cinco abrían el mismo cartel —"¿Está seguro que
	/// desea…?", con ícono de información— así que la única irreversible no se distinguía de un
	/// alta. Ahora:
	/// </para>
	/// <list type="bullet">
	/// <item><b>Aprobar</b> y <b>rechazar</b> preguntan, sin rojo: sacan la licencia de
	/// Pendiente y después los dos botones se apagan, así que es una decisión de una sola vez.
	/// El diálogo dice qué pasa con los días.</item>
	/// <item><b>Editar</b> y <b>registrar</b> no preguntan: el usuario completó el formulario y
	/// apretó "Guardar"; la licencia queda pendiente y se puede volver a editar.</item>
	/// <item><b>Eliminar</b> pregunta en rojo y con la consecuencia: no se puede deshacer.</item>
	/// <item><b>Registrar el reintegro</b> pregunta, sin rojo: cierra la licencia y devuelve al
	/// docente a sus cátedras. Prorrogar, fijar el fin y declarar indefinida no preguntan: el dato
	/// se escribió en el panel y se puede volver a cambiar mientras la licencia no se finalice.</item>
	/// </list>
	/// </remarks>
	private async Task ExecuteGuardarCommandAsync(object? obj)
	{
		switch (obj)
		{
			case "Aprobar":
				if (!_dialogService.ConfirmarAccion(SolicitudDeDialogo.Revisar(
						titulo: $"Aprobar la licencia por { LicenciaSeleccionada.Denominacion.ToLowerInvariant() }",
						mensaje: $"Quedan concedidos { LicenciaSeleccionada.Dias } días desde el { LicenciaSeleccionada.FechaInicio:dd/MM/yyyy}. "
							   + "La licencia deja de estar pendiente y ya no se puede editar ni rechazar.",
						verbo: "Aprobar licencia")))
				{
					return;
				}

				try
				{
					var request = new ActualizarEstadoLicenciaRequest(LicenciaID: LicenciaSeleccionada.LicenciaID,
																	  DocenteID: _docenteID,
																	  Observacion: LicenciaSeleccionada.Observacion);
					await _servicioLicencia.AprobarLicencia(request);

					_dialogService.MostrarInformacion($"{ LicenciaSeleccionada.Denominacion }, { LicenciaSeleccionada.Dias } días desde el { LicenciaSeleccionada.FechaInicio:dd/MM/yyyy}.", "Licencia aprobada");
					await ExecuteCargarLicenciasCommand();
				}
				catch (Exception ex)
				{
					_dialogService.MostrarError(ex.Message, "No se pudo aprobar la licencia");
				}
				break;

			case "Finalizar":
				var mensajeDeReintegro = LicenciaSeleccionada.Vencida
					? $"La licencia terminó el { LicenciaSeleccionada.FechaFin:dd/MM/yyyy} y queda finalizada con esa fecha. "
					: "Hoy queda como su último día de licencia. ";
				if (!_dialogService.ConfirmarAccion(SolicitudDeDialogo.Revisar(
						titulo: $"Registrar el reintegro de la licencia por { LicenciaSeleccionada.Denominacion.ToLowerInvariant() }",
						mensaje: mensajeDeReintegro
							   + "El docente vuelve a estar al frente de las cátedras que lo esperaban: las que quedaron sin nadie en funciones y las que cubría su suplente.",
						verbo: "Registrar reintegro")))
				{
					return;
				}

				await OperarSobreAprobadaAsync(
					() => _servicioLicencia.FinalizarLicencia(new FinalizarLicenciaRequest(LicenciaSeleccionada.LicenciaID, _docenteID)),
					"Reintegro registrado", "No se pudo registrar el reintegro");
				break;

			case "Prorrogar":
				var dias = DiasDeProrroga;
				await OperarSobreAprobadaAsync(
					() => _servicioLicencia.ExtenderLicencia(new ExtenderLicenciaRequest(LicenciaSeleccionada.LicenciaID, _docenteID, dias)),
					"Licencia prorrogada", "No se pudo prorrogar la licencia");
				break;

			case "FijarFin":
				var fechaDeFin = FechaDeFin!.Value;
				await OperarSobreAprobadaAsync(
					() => _servicioLicencia.FijarFechaFinLicencia(new FijarFechaFinLicenciaRequest(LicenciaSeleccionada.LicenciaID, _docenteID, fechaDeFin)),
					"Fecha de fin registrada", "No se pudo fijar la fecha de fin");
				break;

			case "Indefinida":
				await OperarSobreAprobadaAsync(
					() => _servicioLicencia.DeclararLicenciaIndefinida(new DeclararLicenciaIndefinidaRequest(LicenciaSeleccionada.LicenciaID, _docenteID)),
					"Licencia indefinida", "No se pudo declarar indefinida la licencia");
				break;

			case "Rechazar":
				if (!_dialogService.ConfirmarAccion(SolicitudDeDialogo.Revisar(
						titulo: $"Rechazar la licencia por { LicenciaSeleccionada.Denominacion.ToLowerInvariant() }",
						mensaje: "La solicitud queda rechazada y deja de estar pendiente: no se puede editar ni aprobar después. "
							   + "La observación que escribas queda como motivo.",
						verbo: "Rechazar licencia")))
				{
					return;
				}

				try
				{
					var request = new ActualizarEstadoLicenciaRequest(LicenciaID: LicenciaSeleccionada.LicenciaID,
																	  DocenteID: _docenteID,
																	  Observacion: LicenciaSeleccionada.Observacion);
					await _servicioLicencia.RechazarLicencia(request);

					_dialogService.MostrarInformacion($"{ LicenciaSeleccionada.Denominacion }.", "Licencia rechazada");
					await ExecuteCargarLicenciasCommand();
				}
				catch (Exception ex)
				{
					_dialogService.MostrarError(ex.Message, "No se pudo rechazar la licencia");
				}
				break;

			case "Editar":
				try
				{
					var request = new ModificarLicenciaRequest(DocenteID: _docenteID,
															   LicenciaID: LicenciaSeleccionada.LicenciaID,
															   Articulo: LicenciaSeleccionada.Articulo.ToString(),
															   Dias: LicenciaSeleccionada.Dias,
															   FechaInicio: LicenciaSeleccionada.FechaInicio,
															   Observacion: LicenciaSeleccionada.Observacion);
					await _servicioLicencia.ModificarLicencia(request);

					_dialogService.MostrarInformacion($"{ LicenciaSeleccionada.Denominacion }, { LicenciaSeleccionada.Dias } días.", "Licencia actualizada");
					await ExecuteCargarLicenciasCommand();
				}
				catch (Exception ex)
				{
					_dialogService.MostrarError(ex.Message, "No se pudo actualizar la licencia");
				}

				HabilitarUpsert = false;
				HabilitarEdicion = false;

				break;

			case "Nueva":
				try
				{
					var request = new NuevaSolicitudLicenciaRequest(DocenteID: _docenteID,
																	Articulo: LicenciaSeleccionada.Articulo.ToString(),
																	FechaInicio: LicenciaSeleccionada.FechaInicio,
																	Dias: LicenciaSeleccionada.Dias,
																	Observacion: LicenciaSeleccionada.Observacion);
					await _servicioLicencia.SolicitarLicencia(request);

					_dialogService.MostrarInformacion($"{ LicenciaSeleccionada.Denominacion }, { LicenciaSeleccionada.Dias } días desde el { LicenciaSeleccionada.FechaInicio:dd/MM/yyyy}. Queda pendiente de aprobación.", "Licencia registrada");
					await ExecuteCargarLicenciasCommand();
				}
				catch (Exception ex)
				{
					_dialogService.MostrarError(ex.Message, "No se pudo registrar la licencia");
				}

				HabilitarUpsert = false;
				HabilitarRegistro = false;

				break;

			case "Eliminar":
				if (!_dialogService.ConfirmarAccion(SolicitudDeDialogo.Destructiva(
						titulo: $"Eliminar la licencia por { LicenciaSeleccionada.Denominacion.ToLowerInvariant() }",
						consecuencia: $"Se borran los { LicenciaSeleccionada.Dias } días solicitados desde el { LicenciaSeleccionada.FechaInicio:dd/MM/yyyy} "
									+ "y la licencia desaparece del legajo. Esta acción no se puede deshacer.",
						verbo: "Eliminar licencia")))
				{
					return;
				}

				try
				{
					var request = new EliminarLicenciaRequest(LicenciaID: LicenciaSeleccionada.LicenciaID,
															  DocenteID: _docenteID);
					await _servicioLicencia.EliminarLicencia(request);

					_dialogService.MostrarInformacion($"{ LicenciaSeleccionada.Denominacion }.", "Licencia eliminada");
					await ExecuteCargarLicenciasCommand();
				}
				catch (Exception ex)
				{
					_dialogService.MostrarError(ex.Message, "No se pudo eliminar la licencia");
				}

				break;
		}
	}

	/// <summary>
	/// Las cuatro operaciones sobre una licencia aprobada terminan igual: aviso, recarga del listado
	/// para que se vea el período nuevo, o el motivo del dominio si no se pudo.
	/// </summary>
	/// <remarks>
	/// El aviso nombra el artículo leído ANTES de recargar: la recarga reemplaza la colección y la
	/// selección del listado deja de apuntar a la licencia operada.
	/// </remarks>
	private async Task OperarSobreAprobadaAsync(Func<Task> operacion, string tituloExito, string tituloError)
	{
		var denominacion = LicenciaSeleccionada.Denominacion;

		try
		{
			await operacion();

			_dialogService.MostrarInformacion($"{ denominacion }.", tituloExito);
			await ExecuteCargarLicenciasCommand();
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, tituloError);
		}
	}
	#endregion

	#region EditarCommand
	private bool CanExecuteEditarCommand(object? obj)
	{
		switch (obj)
		{
			case "Editar":
				return HabilitarDetalleLicencias && LicenciaSeleccionada is not null && LicenciaSeleccionada.Estado.Equals(Enum.GetName(Estado.Pendiente));
			default:
				return false;
		}
	}

	private void ExecuteEditarCommand(object? obj)
	{
		switch (obj)
		{
			case "Editar":
				HabilitarDetalleLicencias = false;
				HabilitarUpsert = true;
				HabilitarEdicion = true;

				break;
		}
	}
	#endregion

	#region RegistrarCommand
	private bool CanExecuteRegistrarCommand(object? obj)
	{
		switch (obj)
		{
			case "Nueva":
				return !HabilitarRegistro;
			default:
				return false;
		}
	}

	private void ExecuteRegistrarCommand(object? obj)
	{
		switch (obj)
		{
			case "Nueva":
				HabilitarMessage = false;
				HabilitarDetalleLicencias = false;

				HabilitarUpsert = true;
				HabilitarRegistro = true;

				LicenciaSeleccionada = new LicenciaViewModel();

				break;
		}
	}
	#endregion
}