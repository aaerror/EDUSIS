using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioDocentes.DTOs.Requests;
using Core.ServicioDocentes.DTOs.Responses;
using Core.ServicioDocentes;
using Domain.Docentes.Puestos;
using Microsoft.IdentityModel.Tokens;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using System;
using WPF_Desktop.Store;

namespace WPF_Desktop.ViewModels.Docentes.Puestos;

internal partial class GestionPuestosViewModel : ObservableObject
{
	#region Servicios
	private readonly IServicioDocente _servicioDocentes;
	#endregion

	private LegajoStore _perfilBuscadoStore;


	[ObservableProperty]
	private string _mensaje;

	[ObservableProperty]
	private bool _habilitarMensaje;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(CancelarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(GuardarCommandAsync))]	
	private bool _habilitarRegistro;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(EditarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(EliminarCommandAsync))]
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
	private PuestoDocenteViewModel _puestoDocente;

	#region Response
	private LegajoDocenteResponse _legajoDocenteResponse;
	private PuestoResponse _puestoDocenteResponse;
	#endregion

	[ObservableProperty]
	private ListCollectionView _puestosDocente;

	#region Commands
	public IAsyncRelayCommand CargarPuestosCommandAsync { get; }
	public IAsyncRelayCommand CancelarCommandAsync { get; }
	public IRelayCommand EditarCommandAsync { get; }
	public IAsyncRelayCommand EliminarCommandAsync { get; }
	public IAsyncRelayCommand GuardarCommandAsync { get; }

	public IRelayCommand RegistrarCommand { get; }
	#endregion


	public GestionPuestosViewModel(IServicioDocente servicioDocentes, LegajoStore perfilBuscadoStore)
	{
		_servicioDocentes = servicioDocentes;
		_perfilBuscadoStore = perfilBuscadoStore;


		CargarPuestosCommandAsync = new AsyncRelayCommand(ExecuteCargarPuestosDocentesAsync);
		CancelarCommandAsync = new AsyncRelayCommand<string>(ExecuteCancelarCommandAsync, CanExecuteCancelarCommand);
		EditarCommandAsync = new RelayCommand(ExecuteEditarCommand, CanExecuteEditarCommand);
		EliminarCommandAsync = new AsyncRelayCommand<string>(ExecuteEliminarCommandAsync, CanExecuteEliminarCommand);
		GuardarCommandAsync = new AsyncRelayCommand<string>(ExecuteGuardarCommandAsync, CanExecuteGuardarCommand);

		RegistrarCommand = new RelayCommand(ExecuteRegistrarCommand, CanExecuteRegistrarCommand);

		HabilitarListadoPuestos = false;
		HabilitarRegistro = false;
		HabilitarMensaje = false;
	}

	private async Task ExecuteCargarPuestosDocentesAsync()
	{
		var docenteID = new DocenteIDRequest(_perfilBuscadoStore.PersonaID);
		_legajoDocenteResponse = await _servicioDocentes.MostrarLegajoDocenteAsync(docenteID);
		NombreCompleto = _legajoDocenteResponse.NombreCompleto;
		Legajo = _legajoDocenteResponse.Legajo;
		Documento = _legajoDocenteResponse.DNI;


		var puestos = await _servicioDocentes.ListarPuestosDocentesAsync(docenteID);
		if (puestos.IsNullOrEmpty())
		{
			Mensaje = "No existen puestos docentes registrados para el docente.";
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

	#region CancelarCommandAsync
	private bool CanExecuteCancelarCommand(object obj)
	{
		switch (obj)
		{
			case "Nuevo":
				return HabilitarRegistro;

			default:
				return false;
		}
	}

	private async Task ExecuteCancelarCommandAsync(object obj)
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

	private void ExecuteEditarCommand()
	{
		HabilitarListadoPuestos = false;
		HabilitarRegistro = true;
		/*
		string messageBoxText = string.Empty;
		string caption = string.Empty;
		MessageBoxResult result;

		messageBoxText = $"¿Está seguro que desea cambiar el puesto del docente { NombreCompleto } a tiempo indeterminado?\n" +
						 $"\nCargo: { PuestoDocente.Posicion }" +
						 $"\nFecha Inicio: { PuestoDocente.FechaInicio.Date.ToString("D") }\n\n";
		caption = "Modificar Puesto Docente";

		result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);
		if (result is MessageBoxResult.Yes)
		{
			try
			{
				var request = new ModificarEventualidadPuestoDocenteRequest(DocenteID: _legajoDocenteResponse.DocenteID,
																			PuestoID: PuestoDocente.PuestoID);
				await _servicioDocentes.ActualizarPuestoDocenteAsync(request);

				messageBoxText = $"Cambios guardados exitósamente.";
				caption = "Operación Exitosa";
				MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		await ExecuteCargarPuestosDocentesAsync();
		*/

	}
	#endregion

	#region EliminarCommandAsync
	private bool CanExecuteEliminarCommand(object obj)
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

	private async Task ExecuteEliminarCommandAsync(object obj)
	{
		string messageBoxText = string.Empty;
		string caption = string.Empty;

		MessageBoxResult result;
		switch (obj)
		{
			case "Revocar":
				messageBoxText = $"¿Está seguro que desea dar de baja a { NombreCompleto } del puesto { PuestoDocente.Posicion }?";
				caption = "Revocar Puesto Docente";

				result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);
				if (result is MessageBoxResult.Yes)
				{
					try
					{
						var request = new RevocarPuestoDocenteRequest(DocenteID: _legajoDocenteResponse.DocenteID,
																	  PuestoID: PuestoDocente.PuestoID,
																	  FechaFin: null);
						await _servicioDocentes.RevocarPuestoDocenteAsync(request);

						messageBoxText = $"Cambios guardados exitósamente.";
						caption = "Operación Exitosa";
						MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}

				await ExecuteCargarPuestosDocentesAsync();
				break;

			case "Eliminar":
				messageBoxText = $"¿Está seguro que desea eliminar el puesto docente?";
				caption = "Quitar Puesto Docente";

				result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);
				if (result is MessageBoxResult.Yes)
				{
					try
					{
						var request = new EliminarPuestoDocenteRequest(DocenteID: _legajoDocenteResponse.DocenteID,
																	   PuestoID: PuestoDocente.PuestoID);
						await _servicioDocentes.EliminarPuestoDocenteAsync(request);

						messageBoxText = $"Cambios guardados exitósamente.";
						caption = "Operación Exitosa";
						MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}

				await ExecuteCargarPuestosDocentesAsync();
				break;
		}
	}
	#endregion

	#region GuardarCommandAsync
	private bool CanExecuteGuardarCommand(object obj)
	{
		switch (obj)
		{
			case "Upsert":
				return HabilitarRegistro && PuestoDocente is not null && !PuestoDocente.HasErrors;

			default:
				return false;
		}
	}

	private async Task ExecuteGuardarCommandAsync(object obj)
	{
		string messageBoxText = string.Empty;
		string caption = string.Empty;
		MessageBoxResult result;

		switch (obj)
		{
			case "Upsert":
				var puestoID = PuestoDocente.PuestoID;
				if (Guid.Empty.Equals(puestoID))
				{
					messageBoxText = $"¿Está seguro que desea agregar el docente { NombreCompleto } al puesto { PuestoDocente.Posicion }?";
					caption = "Asignar Puesto Docente";

					result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);
					if (result is MessageBoxResult.Yes)
					{
						try
						{
							var request = new RegistrarPuestoDocenteRequest(DocenteID: _legajoDocenteResponse.DocenteID,
																			Posicion: PuestoDocente.Posicion,
																			Estado: PuestoDocente.Estado,
																			FechaInicio: PuestoDocente.FechaInicio,
																			FechaFin: PuestoDocente.FechaFin);
							await _servicioDocentes.AgregarPuestoDocenteAsync(request);

							messageBoxText = $"Cambios guardados exitósamente.";
							caption = "Operación Exitosa";
							MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);
						}
						catch (Exception ex)
						{
							MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
						}
					}
				}
				else
				{
					messageBoxText = $"¿Está seguro que desea modificar los datos del puesto docente?";
					caption = "Modificar Puesto Docente";

					result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);
					if (result is MessageBoxResult.Yes)
					{
						try
						{
							var request = new EditarPuestoDocenteRequest(DocenteID: _legajoDocenteResponse.DocenteID,
																		 PuestoID: puestoID,
																		 Posicion: PuestoDocente.Posicion,
																		 FechaInicio: PuestoDocente.FechaInicio,
																		 FechaFin: PuestoDocente.FechaFin);
							await _servicioDocentes.EditarPuestoDocenteAsync(request);

							messageBoxText = $"Cambios guardados exitósamente.";
							caption = "Operación Exitosa";
							MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);
						}
						catch (Exception ex)
						{
							MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
						}
					}
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