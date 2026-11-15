using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioUsuarios.DTOs.Requests;
using Core.ServicioUsuarios;
using Core.Shared.Seguridad;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace WPF_Desktop.ViewModels.Usuarios;

internal partial class UsuarioViewModel : ObservableObject
{
	private readonly IServicioUsuario _servicioUsuario;
	private readonly ISesionActual _sesion;

	[ObservableProperty]
	private string _username;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Iniciales))]
	private string _nombreCompleto;

	/// <summary>
	/// Los roles de la sesión, por <c>Id</c>. La credencial los muestra con el converter
	/// <c>roles</c>, el mismo que usa la pantalla de Usuarios.
	/// </summary>
	/// <remarks>
	/// Es el dato que explica el menú: las secciones se habilitan por permiso, los permisos salen
	/// del rol, y hasta ahora el rol no se veía en ninguna pantalla de la aplicación — ni siquiera
	/// en la que dice "las secciones que tu rol no habilita aparecen apagadas". Viaja por Id y no
	/// por descripción por lo que explica el XML-doc de <c>UsuarioResponse</c>.
	/// </remarks>
	[ObservableProperty]
	private IReadOnlyList<int> _rolesIDs = Array.Empty<int>();

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Estado))]
	private bool _estaConectado = false;

	public string Estado => EstaConectado ? "Conectado" : "Sin conexión";

	/// <summary>
	/// Iniciales para la credencial del rail. Dos letras como máximo: con tres o más deja de
	/// leerse como una inicial y pasa a parecer una sigla.
	/// </summary>
	public string Iniciales
	{
		get
		{
			if (string.IsNullOrWhiteSpace(NombreCompleto))
			{
				return "–";
			}

			var palabras = NombreCompleto.Split(' ', StringSplitOptions.RemoveEmptyEntries);

			return palabras.Length is 1
				? palabras[0][..1].ToUpperInvariant()
				: string.Concat(palabras[0][..1], palabras[^1][..1]).ToUpperInvariant();
		}
	}

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
		RolesIDs = usuario.RolesIDs;
		EstaConectado = true;
	}
}
