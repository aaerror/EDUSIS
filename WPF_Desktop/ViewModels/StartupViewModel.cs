using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using System;
using WPF_Desktop.Navigation;
using WPF_Desktop.Store.NavigationStore;

namespace WPF_Desktop.ViewModels;

internal class StartupViewModel : ObservableObject
{
	private readonly INavigationService _loginUsuarioNavigationService;
	private readonly INavigationService _registrarUsuarioNavigationService;
	private StartupWindowNavigationStore _navigationStore;

	public ObservableObject ViewModel => _navigationStore.ViewModel;

	private bool _isWindowVisible = true;
	private string _year = DateTime.Now.Year.ToString();
	private string _seccionActiva = "Login";

	/// <summary>
	/// Pestaña abierta: <c>Login</c> o <c>Registrar</c>. La usa la vista para marcar cuál de
	/// las dos está activa; sin esto las dos se ven igual y no hay forma de saber qué
	/// formulario se está completando.
	/// </summary>
	public string SeccionActiva
	{
		get => _seccionActiva;
		private set => SetProperty(ref _seccionActiva, value);
	}

	public IRelayCommand NavigationCommand { get; }

	public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;


	public StartupViewModel(StartupWindowNavigationStore navigationStore, INavigationService loginUsuarioNavigationService, INavigationService registrarUsuarioNavigationService)
	{
		_loginUsuarioNavigationService = loginUsuarioNavigationService;
		_registrarUsuarioNavigationService = registrarUsuarioNavigationService;
		_navigationStore = navigationStore;

		NavigationCommand = new RelayCommand<string>(ExecuteNavigationCommand, CanExecuteNavigationCommand);

		_navigationStore.ViewModelChanged += OnViewModelChanged;

		// La ventana abría con el área del formulario vacía: había que hacer clic en "Login"
		// para que apareciera siquiera el campo de usuario. El acceso es lo que viene a hacer
		// el 99% de las veces; se navega solo.
		ExecuteNavigationCommand("Login");
	}

	public bool IsWindowVisible
	{
		get
		{
			return _isWindowVisible;
		}

		set
		{
			_isWindowVisible = value;
			SetProperty(ref _isWindowVisible, true);
		}
	}

	public string Year
	{
		get { return _year; }
	}

	#region NavigationCommand
	private bool CanExecuteNavigationCommand(object obj) =>
		true;

	private void ExecuteNavigationCommand(object obj)
	{
		switch (obj)
		{
			case "Login":
				_loginUsuarioNavigationService.Navigate();
				SeccionActiva = "Login";
				break;
			case "Registrar":
				_registrarUsuarioNavigationService.Navigate();
				SeccionActiva = "Registrar";
				break;
		}
	}
	#endregion

	private void OnViewModelChanged() =>
		OnPropertyChanged(nameof(ViewModel));
}
