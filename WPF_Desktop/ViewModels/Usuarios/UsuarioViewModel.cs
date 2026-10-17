using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioUsuarios.DTOs.Requests;
using Core.ServicioUsuarios;
using Core.Shared.Seguridad;
using System.Threading.Tasks;

namespace WPF_Desktop.ViewModels.Usuarios;

internal partial class UsuarioViewModel : ObservableObject
{
	private readonly IServicioUsuario _servicioUsuario;
	private readonly ISesionActual _sesion;

	[ObservableProperty]
	private string _username;

	[ObservableProperty]
	private string _nombreCompleto;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Estado))]
	private bool _estaConectado = false;

	public string Estado => EstaConectado ? "Conectado" : "Sin conexión";

	#region Commands
	public IAsyncRelayCommand CargarUsuarioCommandAsync { get; }
	#endregion

	public UsuarioViewModel(IServicioUsuario servicioUsuario, ISesionActual sesion)
	{
		_servicioUsuario = servicioUsuario;
		_sesion = sesion;

		CargarUsuarioCommandAsync = new AsyncRelayCommand(CargarUsuarioCommandExecute);
	}

	/// <summary>
	/// Lee la sesión del singleton y no de <c>Thread.CurrentPrincipal</c>. Además de resolver la
	/// afinidad de hilo (ver <c>SesionUsuario</c>), saca de encima un <c>NullReferenceException</c>
	/// latente: la versión anterior hacía <c>principal.Identity</c> sobre el resultado de un
	/// <c>as ClaimsPrincipal</c> sin comprobar null.
	/// </summary>
	private async Task CargarUsuarioCommandExecute()
	{
		if (!_sesion.EstaAutenticado)
		{
			EstaConectado = false;
			return;
		}

		var usuario = await _servicioUsuario.BuscarUsuarioPorIDAsync(new UsuarioIDRequest(_sesion.UsuarioID.Value));

		Username = usuario.Usuario;
		NombreCompleto = usuario.NombreCompleto;
		EstaConectado = true;
	}
}
