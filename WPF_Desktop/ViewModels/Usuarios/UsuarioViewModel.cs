using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioUsuarios.DTOs.Requests;
using Core.ServicioUsuarios;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Threading;
using System;
using WPF_Desktop.Security;

namespace WPF_Desktop.ViewModels.Usuarios;

//https://dotnetcodr.com/security-and-cryptography/
//https://tamingdotnet.blogspot.com/2024/11/claimsprincipal-claimsidentity-claims.html
internal partial class UsuarioViewModel : ObservableObject
{
	private readonly IServicioUsuario _servicioUsuario;

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

	public UsuarioViewModel(IServicioUsuario servicioUsuario)
	{
		_servicioUsuario = servicioUsuario;

		CargarUsuarioCommandAsync = new AsyncRelayCommand(CargarUsuarioCommandExecute);
	}

	private async Task CargarUsuarioCommandExecute()
	{
		var principal = Thread.CurrentPrincipal as ClaimsPrincipal;
		if (!principal.Identity.IsAuthenticated)
		{
			EstaConectado = false;
		}
		else
		{
			var identity = principal.Identity as BasicIdentity;
			if (identity is not null)
			{
				var usuarioID = Guid.Parse(identity.FindFirst(ClaimTypes.NameIdentifier).Value);
				var usuario = await _servicioUsuario.BuscarUsuarioPorIDAsync(new UsuarioIDRequest(usuarioID));

				Username = usuario.Usuario;
				NombreCompleto = usuario.NombreCompleto;
				EstaConectado = principal.Identity.IsAuthenticated;

				return;
			}

			EstaConectado = false;
		}
	}
}