using WPF_Desktop.Security;
using Domain.Usuarios;
using Core.Shared.Seguridad;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioDocentes.DTOs.Requests;
using Core.ServicioDocentes.DTOs.Responses;
using Core.ServicioDocentes;
using Domain.Docentes.Puestos;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System;
using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;
using WPF_Desktop.ViewModels.Docentes;

namespace WPF_Desktop.ViewModels.Docentes.Puestos;

internal partial class GestionPuestosViewModel : ObservableObject, IUbicacion
{
	private readonly ISesionActual _sesion;

	#region Servicios
	private readonly IServicioDocente _servicioDocentes;
	private readonly IDialogService _dialogService;
	#endregion

	/// <summary>
	/// Docente cuyos puestos se gestionan, fijado una sola vez al construir.
	/// </summary>
	/// <remarks>
	/// <c>LegajoStore.PersonaID</c> es <c>Guid?</c> y la guarda va acá. Llegar a esta
	/// pantalla sin docente elegido es un defecto de navegación y no un estado de la pantalla:
	/// <c>GestionDocentesViewModel</c> escribe el store antes de navegar.
	/// </remarks>
	private readonly Guid _docenteID;


	[ObservableProperty]
	private string _mensaje = string.Empty;

	[ObservableProperty]
	private bool _habilitarMensaje;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(CancelarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(GuardarCommandAsync))]	
	private bool _habilitarRegistro;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(EditarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(EliminarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(ActivarCommandAsync))]
	private bool _habilitarListadoPuestos;

	[ObservableProperty]
	private string _legajo = string.Empty;
	[ObservableProperty]
	private string _documento = string.Empty;
	[ObservableProperty]
	private string _nombreCompleto = string.Empty;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(EditarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(EliminarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(CancelarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(GuardarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(ActivarCommandAsync))]
	private PuestoDocenteViewModel _puestoDocente;

	#region Response
	private LegajoDocenteResponse _legajoDocenteResponse;
	#endregion

	[ObservableProperty]
	private ListCollectionView _puestosDocente;

	#region Commands
	public IAsyncRelayCommand ActivarCommandAsync { get; }
	public IAsyncRelayCommand CargarPuestosCommandAsync { get; }
	public IAsyncRelayCommand CancelarCommandAsync { get; }
	public IRelayCommand EditarCommandAsync { get; }
	public IAsyncRelayCommand EliminarCommandAsync { get; }
	public IAsyncRelayCommand GuardarCommandAsync { get; }

	public IRelayCommand RegistrarCommand { get; }
	public IRelayCommand VolverCommand { get; }
	#endregion

	public IReadOnlyList<Migaja> Ruta { get; }


	public GestionPuestosViewModel(IServicioDocente servicioDocentes,
								   IDialogService dialogService,
								   INavigationService<GestionDocentesViewModel> gestionDocentesNavigationService,
								   LegajoStore perfilBuscadoStore, ISesionActual sesion)
	{
		_sesion = sesion;
		_servicioDocentes = servicioDocentes;
		_dialogService = dialogService;
		_docenteID = perfilBuscadoStore.PersonaID
			?? throw new ArgumentNullException(nameof(perfilBuscadoStore), "Error al gestionar los puestos. Primero debe buscar un docente antes de continuar.");

		VolverCommand = new RelayCommand(gestionDocentesNavigationService.Navigate);

		Ruta = new[]
		{
			Migaja.Hacia("DOCENTES", VolverCommand),
			Migaja.Actual("PUESTOS")
		};

		ActivarCommandAsync = new AsyncRelayCommand(ExecuteActivarCommandAsync, _sesion.Permite(Permiso.GestionarDocentes, CanExecuteActivarCommand));
		CargarPuestosCommandAsync = new AsyncRelayCommand(ExecuteCargarPuestosDocentesAsync);
		CancelarCommandAsync = new AsyncRelayCommand<string>(ExecuteCancelarCommandAsync, CanExecuteCancelarCommand);
		EditarCommandAsync = new RelayCommand(ExecuteEditarCommand, _sesion.Permite(Permiso.GestionarDocentes, CanExecuteEditarCommand));
		EliminarCommandAsync = new AsyncRelayCommand<string>(ExecuteEliminarCommandAsync, _sesion.Permite<string>(Permiso.GestionarDocentes, CanExecuteEliminarCommand));
		GuardarCommandAsync = new AsyncRelayCommand<string>(ExecuteGuardarCommandAsync, _sesion.Permite<string>(Permiso.GestionarDocentes, CanExecuteGuardarCommand));

		RegistrarCommand = new RelayCommand(ExecuteRegistrarCommand, _sesion.Permite(Permiso.GestionarDocentes, CanExecuteRegistrarCommand));

		HabilitarListadoPuestos = false;
		HabilitarRegistro = false;
		HabilitarMensaje = false;
	}

	/// <summary>
	/// Trae el legajo del docente y sus puestos.
	/// </summary>
	/// <remarks>
	/// El try/catch no estaba: una caída de la consulta subía al manejador global, que apaga la
	/// aplicación. Lo que falló acá es una lectura, y lo que corresponde es decirlo y dejar la
	/// pantalla en pie.
	/// </remarks>
	private async Task ExecuteCargarPuestosDocentesAsync()
	{
		try
		{
			var docenteID = new DocenteIDRequest(_docenteID);
			_legajoDocenteResponse = await _servicioDocentes.MostrarLegajoDocenteAsync(docenteID);
			NombreCompleto = _legajoDocenteResponse.NombreCompleto;
			Legajo = _legajoDocenteResponse.Legajo;
			Documento = _legajoDocenteResponse.DNI;


			var puestos = await _servicioDocentes.ListarPuestosDocentesAsync(docenteID);
			if (puestos.IsNullOrEmpty())
			{
				Mensaje = $"{ NombreCompleto } todavía no tiene puestos registrados. Agregá el primero con «Agregar puesto».";
				HabilitarMensaje = true;
				HabilitarListadoPuestos = false;
			}
			else
			{
				var puestosDocentes = new ObservableCollection<PuestoDocenteViewModel>(puestos.Select(x => new PuestoDocenteViewModel(x)));

				PuestosDocente = new ListCollectionView(puestosDocentes);
				PuestosDocente.GroupDescriptions.Add(new PropertyGroupDescription("Estado"));

				Mensaje = string.Empty;
				HabilitarMensaje = false;
				HabilitarListadoPuestos = true;
			}
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "No se pudieron cargar los puestos");
		}
	}

	#region ActivarCommandAsync
	private bool CanExecuteActivarCommand() =>
		HabilitarListadoPuestos && PuestoDocente is not null && PuestoDocente.Estado.Equals(Enum.GetName(EstadoPuesto.Pendiente));

	/// <summary>
	/// Toma de posesión: pasa el puesto pendiente a activo.
	/// </summary>
	/// <remarks>
	/// Se confirma porque no tiene vuelta atrás: un puesto activo ya no se edita ni se elimina,
	/// sólo se revoca. Que el período haya iniciado lo decide el dominio, no el botón.
	/// </remarks>
	private async Task ExecuteActivarCommandAsync()
	{
		if (!_dialogService.ConfirmarAccion(SolicitudDeDialogo.Revisar(
				titulo: $"Activar el puesto { PuestoDocente.Posicion } de { NombreCompleto }",
				mensaje: "El docente toma posesión del puesto. A partir de ahora el puesto ya no se puede editar ni eliminar; sólo revocar.",
				verbo: "Activar puesto")))
		{
			return;
		}

		try
		{
			var request = new ActivarPuestoDocenteRequest(DocenteID: _legajoDocenteResponse.DocenteID,
														  PuestoID: PuestoDocente.PuestoID);
			await _servicioDocentes.ActivarPuestoDocenteAsync(request);

			_dialogService.MostrarInformacion($"{ PuestoDocente.Posicion } de { NombreCompleto }.", "Puesto activado");
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "No se pudo activar el puesto");
		}

		await ExecuteCargarPuestosDocentesAsync();
	}
	#endregion

	#region CancelarCommandAsync
	private bool CanExecuteCancelarCommand(object? obj)
	{
		switch (obj)
		{
			case "Nuevo":
				return HabilitarRegistro;

			default:
				return false;
		}
	}

	private async Task ExecuteCancelarCommandAsync(object? obj)
	{
		await ExecuteCargarPuestosDocentesAsync();

		switch (obj)
		{
			case "Nuevo":
				HabilitarRegistro = false;

				break;
		}
	}
	#endregion

	#region EditarCommandAsync
	private bool CanExecuteEditarCommand() =>
		HabilitarListadoPuestos && PuestoDocente is not null && PuestoDocente.Estado.Equals(Enum.GetName(EstadoPuesto.Pendiente));

	/// <summary>
	/// Abre el puesto elegido en el formulario de abajo. No guarda nada por su cuenta.
	/// </summary>
	private void ExecuteEditarCommand()
	{
		HabilitarListadoPuestos = false;
		HabilitarRegistro = true;
	}
	#endregion

	#region EliminarCommandAsync
	private bool CanExecuteEliminarCommand(object? obj)
	{
		switch (obj)
		{
			case "Revocar": 
				return HabilitarListadoPuestos && PuestoDocente is not null && PuestoDocente.Estado.Equals(Enum.GetName(EstadoPuesto.Activo));

			case "Eliminar":
				return HabilitarListadoPuestos && PuestoDocente is not null && PuestoDocente.Estado.Equals(Enum.GetName(EstadoPuesto.Pendiente));

			default:
				return false;
		}
	}

	/// <summary>
	/// Revocar y eliminar: las dos acciones destructivas de la pantalla.
	/// </summary>
	/// <remarks>
	/// <para>
	/// No son sinónimos y el diálogo lo dice. <b>Revocar</b> cierra un puesto que está ACTIVO
	/// —queda en el legajo con su fecha de fin—, y <b>eliminar</b> borra un puesto PENDIENTE que
	/// nunca entró en vigencia. Antes las dos preguntaban "¿Está seguro que desea…?" con un
	/// ícono de información, y la de eliminar no nombraba ni el puesto.
	/// </para>
	/// </remarks>
	private async Task ExecuteEliminarCommandAsync(object? obj)
	{
		switch (obj)
		{
			case "Revocar":
				if (!_dialogService.ConfirmarAccion(SolicitudDeDialogo.Destructiva(
						titulo: $"Revocar el puesto { PuestoDocente.Posicion } de { NombreCompleto }",
						consecuencia: "El puesto se cierra con fecha de hoy y deja de contar como activo. "
									+ "Queda registrado en el legajo con su período, así que la antigüedad ya cumplida no se pierde.",
						verbo: "Revocar puesto")))
				{
					return;
				}

				try
				{
					var request = new RevocarPuestoDocenteRequest(DocenteID: _legajoDocenteResponse.DocenteID,
																  PuestoID: PuestoDocente.PuestoID,
																  FechaFin: null);
					await _servicioDocentes.RevocarPuestoDocenteAsync(request);

					_dialogService.MostrarInformacion($"{ PuestoDocente.Posicion } de { NombreCompleto }.", "Puesto revocado");
				}
				catch (Exception ex)
				{
					_dialogService.MostrarError(ex.Message, "No se pudo revocar el puesto");
				}

				await ExecuteCargarPuestosDocentesAsync();
				break;

			case "Eliminar":
				if (!_dialogService.ConfirmarAccion(SolicitudDeDialogo.Destructiva(
						titulo: $"Eliminar el puesto { PuestoDocente.Posicion } de { NombreCompleto }",
						consecuencia: "El puesto está pendiente, así que se borra del legajo sin dejar registro. "
									+ "Esta acción no se puede deshacer.",
						verbo: "Eliminar puesto")))
				{
					return;
				}

				try
				{
					var request = new EliminarPuestoDocenteRequest(DocenteID: _legajoDocenteResponse.DocenteID,
																   PuestoID: PuestoDocente.PuestoID);
					await _servicioDocentes.EliminarPuestoDocenteAsync(request);

					_dialogService.MostrarInformacion($"{ PuestoDocente.Posicion } de { NombreCompleto }.", "Puesto eliminado");
				}
				catch (Exception ex)
				{
					_dialogService.MostrarError(ex.Message, "No se pudo eliminar el puesto");
				}

				await ExecuteCargarPuestosDocentesAsync();
				break;
		}
	}
	#endregion

	#region GuardarCommandAsync
	private bool CanExecuteGuardarCommand(object? obj)
	{
		switch (obj)
		{
			case "Upsert":
				return HabilitarRegistro && PuestoDocente is not null && !PuestoDocente.HasErrors;

			default:
				return false;
		}
	}

	/// <summary>
	/// Guarda el puesto del formulario: lo crea si es nuevo, lo edita si ya existía.
	/// </summary>
	/// <remarks>
	/// Sin confirmación previa. El usuario completó el formulario y apretó "Guardar puesto";
	/// preguntarle "¿Está seguro?" encima no agrega información —no se pierde nada, el puesto
	/// se puede editar o eliminar después— y convertía un alta en tres clics.
	/// </remarks>
	private async Task ExecuteGuardarCommandAsync(object? obj)
	{
		switch (obj)
		{
			case "Upsert":
				var puestoID = PuestoDocente.PuestoID;
				bool esNuevo = Guid.Empty.Equals(puestoID);

				try
				{
					if (esNuevo)
					{
						var request = new RegistrarPuestoDocenteRequest(DocenteID: _legajoDocenteResponse.DocenteID,
																		Posicion: PuestoDocente.Posicion,
																		Estado: PuestoDocente.Estado,
																		FechaInicio: PuestoDocente.FechaInicio,
																		FechaFin: PuestoDocente.FechaFin);
						await _servicioDocentes.AgregarPuestoDocenteAsync(request);

						_dialogService.MostrarInformacion($"{ PuestoDocente.Posicion } para { NombreCompleto }, desde el { PuestoDocente.FechaInicio:dd/MM/yyyy}.", "Puesto agregado");
					}
					else
					{
						var request = new EditarPuestoDocenteRequest(DocenteID: _legajoDocenteResponse.DocenteID,
																	 PuestoID: puestoID,
																	 Posicion: PuestoDocente.Posicion,
																	 FechaInicio: PuestoDocente.FechaInicio,
																	 FechaFin: PuestoDocente.FechaFin);
						await _servicioDocentes.EditarPuestoDocenteAsync(request);

						_dialogService.MostrarInformacion($"{ PuestoDocente.Posicion } de { NombreCompleto }.", "Puesto actualizado");
					}
				}
				catch (Exception ex)
				{
					_dialogService.MostrarError(ex.Message, esNuevo ? "No se pudo agregar el puesto" : "No se pudo actualizar el puesto");
				}

				await ExecuteCargarPuestosDocentesAsync();
				HabilitarRegistro = false;

				break;
		}
	}
	#endregion

	#region RegistrarCommand
	private bool CanExecuteRegistrarCommand() =>
		!HabilitarRegistro;

	private void ExecuteRegistrarCommand()
	{
		PuestoDocente = new PuestoDocenteViewModel();

		HabilitarRegistro = true;
		HabilitarMensaje = false;
		HabilitarListadoPuestos = false;
	}
	#endregion
}