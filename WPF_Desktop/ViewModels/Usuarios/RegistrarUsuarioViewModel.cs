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
using System.Windows;
using System;
using WPF_Desktop.ViewModels.Docentes;

namespace WPF_Desktop.ViewModels.Usuarios;

[ObservableRecipient]
internal partial class RegistrarUsuarioViewModel : ObservableValidator
{
	private readonly IServicioUsuario _servicioUsuario;
	private readonly IServicioDocente _servicioDocente;

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


	public RegistrarUsuarioViewModel(IServicioUsuario servicioUsuario, IServicioDocente servicioDocente)
	{
		_servicioUsuario = servicioUsuario;
		_servicioDocente = servicioDocente;

		RegistrarAsyncCommand = new AsyncRelayCommand(ExecuteRegistrarCommand, CanExecuteRegistrarCommand);
		UpdateListaDocentesAsyncCommand = new AsyncRelayCommand(UpdateListaDocentesAsync);
	}

	public async Task UpdateListaDocentesAsync()
	{
		var response = await _servicioDocente.ListarDocentesActivosAsync();
		if (response.IsNullOrEmpty())
		{
			string messageBoxText = string.Empty;
			string caption = string.Empty;
			MessageBoxResult result;

			messageBoxText = "No hay docentes registrados en el sistema.";
			caption = "Alerta";
			MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
		}
		else
		{
			Docentes = new ObservableCollection<LegajoDocenteViewModel>(response.Select(x => new LegajoDocenteViewModel(x)));
			ExistenDocentesActivos = true;
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
		string messageBoxText = string.Empty;
		string caption = string.Empty;
		MessageBoxResult result;

		try
		{
			if (string.IsNullOrWhiteSpace(Legajo))
			{
				messageBoxText = "Debe ingresar el legajo del docente para continuar.";
				caption = "Error en la operación";
				MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error);
				Legajo = string.Empty;

				return;
			}

			var request = new SolicitarAccesoRequest(Docente.DocenteID, Legajo);
			await _servicioUsuario.SolicitarAccesoAsync(request);

			messageBoxText = $"Se ha generado el usuario para el docente:\n" +
							 $"\tDocente: {Docente.NombreCompleto}\n" +
							 $"\tLegajo: {Docente.Legajo}\n\n" +
							 $"Para ingresar al sistema debe utilizar su correo electrónico como usuario y su documento como clave de acceso.";
			caption = "Operación Exitosa";
			MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}
	#endregion
}