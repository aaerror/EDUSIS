using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioDocentes;
using Core.ServicioUsuarios.DTOs.Requests;
using Core.ServicioUsuarios;
using Microsoft.IdentityModel.Tokens;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System;
using WPF_Desktop.Shared;
using WPF_Desktop.ViewModels.Docentes;

namespace WPF_Desktop.ViewModels.Usuarios;

[ObservableRecipient]
internal partial class RegistrarUsuarioViewModel : ObservableValidator
{
	private readonly IServicioUsuario _servicioUsuario;
	private readonly IServicioDocente _servicioDocente;
	private readonly IDialogService _dialogService;

	[ObservableProperty]
	[NotifyDataErrorInfo]
	[Required(AllowEmptyStrings = false, ErrorMessage = "Debe ingresar el número de legajo.")]
	//[Range(minimum:6, maximum: 6, ErrorMessage="Se debe ingresar un número de seis dígitos.")]
	[RegularExpression(@"^(\d{6})$", ErrorMessage = "El legajo posee un formato inválido.", MatchTimeoutInMilliseconds = 2500)]
	private string _legajo = string.Empty;

	[ObservableProperty]
	private LegajoDocenteViewModel _docente;

	[ObservableProperty]
	private ObservableCollection<LegajoDocenteViewModel> _docentes;

	[ObservableProperty]
	private bool _existenDocentesActivos = false;

	#region Commands
	public IAsyncRelayCommand RegistrarAsyncCommand { get; }
	public IAsyncRelayCommand UpdateListaDocentesAsyncCommand { get; }
	#endregion


	public RegistrarUsuarioViewModel(IServicioUsuario servicioUsuario,
									 IServicioDocente servicioDocente,
									 IDialogService dialogService)
	{
		_servicioUsuario = servicioUsuario;
		_servicioDocente = servicioDocente;
		_dialogService = dialogService;

		RegistrarAsyncCommand = new AsyncRelayCommand(ExecuteRegistrarCommand, CanExecuteRegistrarCommand);
		UpdateListaDocentesAsyncCommand = new AsyncRelayCommand(UpdateListaDocentesAsync);
	}

	/// <remarks>
	/// Sin cartel cuando no hay docentes: la pantalla ya muestra el aviso en línea que lo
	/// explica y deshabilita los campos. Un MessageBox encima del mismo mensaje, apenas se abre
	/// la ventana de acceso, era un clic de más para leer algo que ya estaba a la vista.
	/// </remarks>
	public async Task UpdateListaDocentesAsync()
	{
		try
		{
			var response = await _servicioDocente.ListarDocentesActivosAsync();
			if (!response.IsNullOrEmpty())
			{
				Docentes = new ObservableCollection<LegajoDocenteViewModel>(response.Select(x => new LegajoDocenteViewModel(x)));
				ExistenDocentesActivos = true;
			}
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "No se pudo cargar el listado de docentes");
		}
	}

	/*
	public bool ExistenDocentesActivos
	{
		get
		{
			return _existenDocentesActivos;
		}

		set
		{
			_existenDocentesActivos = value;
			SetProperty(ref _existenDocentesActivos, value);
		}
	}

	public string Legajo
	{
		get
		{
			return _legajo;
		}

		set
		{
			_errorsByProperty.Remove(nameof(Legajo));
			_legajo = value;
			OnPropertyChanged(nameof(Legajo));

			if (string.IsNullOrWhiteSpace(Legajo))
			{
				_errorsByProperty.Add(nameof(Legajo), new List<string>
				{
					"Debe ingresar el número de legajo."
				});

				ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Legajo)));
			}
			else
			{
				if (!Regex.IsMatch(Legajo, @"^(\d{6})$", RegexOptions.None, TimeSpan.FromMilliseconds(2500)))
				{
					_errorsByProperty.Add(nameof(Legajo), new List<string>
				{
					"El legajo posee un formato inválido. Se debe ingresar un número de seis dígitos."
				});

					ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Legajo)));
				}
			}
		}
	}

	public LegajoDocenteViewModel Docente
	{
		get
		{
			return _docente;
		}

		set
		{
			_docente = value;
			SetProperty(ref _docente, value);
		}
	}
	
	public ObservableCollection<LegajoDocenteViewModel> Docentes
	{
		get
		{
			return _docentes;
		}

		set
		{
			_docentes = value;
			SetProperty(ref _docentes, value);
		}
	}
	*/

	#region RegistrarAsyncCommand
	private bool CanExecuteRegistrarCommand() =>
		!HasErrors && Docente is not null;

	private async Task ExecuteRegistrarCommand()
	{
		try
		{
			if (string.IsNullOrWhiteSpace(Legajo))
			{
				_dialogService.MostrarAdvertencia("Escribí el número de legajo del docente.", "Falta el legajo");

				return;
			}

			var request = new SolicitarAccesoRequest(Docente.DocenteID, Legajo);
			await _servicioUsuario.SolicitarAccesoAsync(request);

			// Las credenciales iniciales no son un aviso al pie que se va solo: es lo único que
			// el docente necesita para entrar la primera vez, así que el diálogo exige acuse.
			_dialogService.ConfirmarAccion(SolicitudDeDialogo.Aviso(
				titulo: $"Usuario creado para { Docente.NombreCompleto }",
				mensaje: $"Legajo { Docente.Legajo }. Para entrar la primera vez se usa el correo como usuario "
					   + "y el número de documento como contraseña."));
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "No se pudo crear el usuario");
		}
	}
	#endregion
}