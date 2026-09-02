using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core.ServicioAutenticaciones.DTOs.Requests;
using Core.ServicioAutenticaciones;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System;
using WPF_Desktop.Navigation;
using WPF_Desktop.Security;
using WPF_Desktop.ViewModels.Shared.Messages;

namespace WPF_Desktop.ViewModels.Usuarios;

internal partial class LoginUsuarioViewModel : ObservableValidator
{
	private readonly IServicioAutenticacion _servicioAutenticacion;
	private readonly INavigationService _navigationService;

	[ObservableProperty]
	[NotifyDataErrorInfo]
	[Required(AllowEmptyStrings=false, ErrorMessage="Se debe ingresar un correo electrónico.")]
	[RegularExpression(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$", ErrorMessage="Correo electrónico inválido.", MatchTimeoutInMilliseconds=2500)]
	private string _usuario = string.Empty;

	[ObservableProperty]
	[NotifyDataErrorInfo]
	[Required(AllowEmptyStrings=false, ErrorMessage="Se debe ingresar la clave de acceso para continuar.")]
	private SecureString _clave;

	[ObservableProperty]
	private string _mensajeError = string.Empty;

	#region Commands
	public IAsyncRelayCommand RegistrarCommandAsync { get; }
	#endregion


	public LoginUsuarioViewModel(INavigationService navigationService, IServicioAutenticacion servicioAutenticacion)
	{
		_servicioAutenticacion = servicioAutenticacion;
		_navigationService = navigationService;

		RegistrarCommandAsync = new AsyncRelayCommand<string>(ExecuteRegistrarCommand, CanExecuteRegistrarCommand);
	}

	/*
	public string Usuario
	{
		get
		{
			return _usuario;
		}

		set
		{
			_errorsByProperty.Remove(nameof(Usuario));
			_usuario = value;
			OnPropertyChanged(nameof(Usuario));

			if (string.IsNullOrWhiteSpace(value))
			{
				_errorsByProperty.Add(nameof(Usuario),
					new List<string>
					{
						"Se debe ingresar un correo electrónico."
					});
				ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Usuario)));
			}
			else
			{
				if (!Regex.IsMatch(Usuario, @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(2500)))
				{
					_errorsByProperty.Add(nameof(Usuario),
						new List<string>
						{
							"Correo electrónico inválido."
						});
					ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Usuario)));
				}
			}
		}
	}

	public SecureString Clave
	{
		get
		{
			return _clave;
		}

		set
		{
			_clave = value;
			OnPropertyChanged(nameof(Clave));
		}
	}
	*/

	/*
	public string MensajeError
	{
		get { return _mensajeError; }
		set
		{
			_mensajeError = value;
			SetProperty(ref _mensajeError, value);
		}
	}
	*/

	#region DataError
	/*
	public IEnumerable GetErrors(string? propertyName) =>
		_errorsByProperty.GetValueOrDefault(propertyName, new List<string>());

	public bool HasErrors =>
		_errorsByProperty.Any();
	*/
	#endregion

	#region RegistrarCommand
	private bool CanExecuteRegistrarCommand(object obj) =>
		!HasErrors;

	private async Task ExecuteRegistrarCommand(object obj)
	{
		string messageBoxText = string.Empty;
		string caption = string.Empty;
		MessageBoxResult result;

		switch (obj)
		{
			case "Login":
				if (string.IsNullOrWhiteSpace(Usuario) || Clave is null)
				{
					messageBoxText = "Se deben completar todos los campos necesarios para poder ingresar.";
					caption = "Error en la operación";
					MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error);

					Usuario = string.Empty;

					return;
				}

				try
				{
					Clave.MakeReadOnly();

					var credentials = new NetworkCredential();
					credentials.UserName = Usuario;
					credentials.SecurePassword = Clave;

					var request = new LoginRequest(credentials);
					var response = await _servicioAutenticacion.Login(request);


					var identities = new List<BasicIdentity>();
					foreach (var rol in response.Roles)
					{
						var identity = BasicIdentity.Create(usuarioID: response.UsuarioID,
															docente: response.NombreCompleto,
															email: response.Usuario,
															rol: rol);
						identities.Add(identity);
					}
					Thread.CurrentPrincipal = CustomPrincipal.Create(identities);

					WeakReferenceMessenger.Default.Send(new UsuarioLogeadoMessage(response.UsuarioID));
					Application.Current.MainWindow.Close();
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
				}

				break;

				/*
			case "RecuperarAcceso":
				if (string.IsNullOrWhiteSpace(Usuario))
				{
					messageBoxText = "Se debe ingresar el usuario para restablecer la contraseña del mismo.";
					caption = "Error en la operación";
					MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error);

					Usuario = string.Empty;

					return;
				}

				try
				{
					var request = new RecuperarAccesoRequest(Usuario);
					_servicioAutenticacion.RecuperarAcceso(request);

					messageBoxText = $"Se restableció correctamente el acceso del usuario { Usuario }. Ahora, debe utilizar su legajo como acceso al mismo.";
					caption = "Operación exitosa";
					MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
				}
				break;
				*/
		}
	}
	#endregion
}