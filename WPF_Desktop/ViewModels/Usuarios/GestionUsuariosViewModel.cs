using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioAutenticaciones.DTOs.Responses;
using Core.ServicioDocentes.DTOs.Responses;
using Core.ServicioDocentes;
using Core.ServicioUsuarios.DTOs.Requests;
using Core.ServicioUsuarios;
using Domain.Shared;
using Domain.Usuarios;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System;
using WPF_Desktop.Shared;

namespace WPF_Desktop.ViewModels.Usuarios;

/// <summary>
/// Administración de cuentas: crear el usuario de un docente y asignarle o quitarle roles. Cierra
/// UI-46, que era el resto del hallazgo UI-19.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Esta pantalla es el camino por el que alguien puede ampliarse los permisos.</strong>
/// <c>AdministrarUsuarios</c> y <c>AsignarRoles</c> son los dos permisos que separan a
/// <c>Rol.Admin</c> de <c>Rol.Docente</c>, que tiene los otros trece: quien llegue acá puede darse
/// el rol que quiera. Es intrínseco a tener una pantalla de roles, y lo que mantiene gobernable la
/// tabla es que esos dos permisos los tenga un solo rol. Lo que no puede pasar es que la
/// administración se pierda, y de eso se encarga <c>UltimoAdministradorException</c> en el borrado
/// de docente.
/// </para>
/// <para>
/// No usa ningún <c>Store</c>: no es destino de ningún drill-down y no recibe parámetros de
/// navegación. Cuelga directo de su raíz de menú, gobernada por <c>AdministrarUsuarios</c>.
/// </para>
/// <para>
/// Los roles salen de <c>Enumeration.GetAll&lt;Rol&gt;()</c> —del dominio, no de un <c>x:Array</c>
/// en el XAML, que es el defecto que UI-27 registró para las horas cátedra— y viajan por <c>Id</c>,
/// nunca por descripción: decisión #6 de <c>plan/rbac-y-seed.md</c>. El combo muestra
/// <c>Descripcion</c> y manda <c>Id</c>.
/// </para>
/// </remarks>
internal partial class GestionUsuariosViewModel : ObservableObject
{
	#region Service
	private readonly IServicioUsuario _servicioUsuario;
	private readonly IServicioDocente _servicioDocente;
	private readonly IDialogService _dialogService;
	#endregion

	#region Listados
	public ObservableCollection<UsuarioResponse> Usuarios { get; } = new();

	/// <summary>
	/// Docentes activos que <strong>todavía no tienen cuenta</strong>. Se cruza acá y no en
	/// <c>Core</c> porque la pantalla ya tiene las dos listas: pedirle al servicio una tercera
	/// consulta para cruzarlas sería una ida a la base por un dato que está en memoria.
	/// </summary>
	public ObservableCollection<LegajoDocenteResponse> DocentesSinCuenta { get; } = new();

	/// <summary>Los seis roles del catálogo, tomados del dominio.</summary>
	public IReadOnlyCollection<Rol> Roles { get; } = Enumeration.GetAll<Rol>().ToList();
	#endregion

	#region Selección
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(AsignarRolCommand))]
	[NotifyCanExecuteChangedFor(nameof(QuitarRolCommand))]
	private UsuarioResponse? _usuarioSeleccionado;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(CrearUsuarioCommand))]
	private LegajoDocenteResponse? _docenteSeleccionado;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(CrearUsuarioCommand))]
	[NotifyCanExecuteChangedFor(nameof(AsignarRolCommand))]
	[NotifyCanExecuteChangedFor(nameof(QuitarRolCommand))]
	private Rol? _rolSeleccionado;

	[ObservableProperty]
	private bool _cargando;
	#endregion

	#region Commands
	public IAsyncRelayCommand CargarCommand { get; }
	public IAsyncRelayCommand CrearUsuarioCommand { get; }
	public IAsyncRelayCommand AsignarRolCommand { get; }
	public IAsyncRelayCommand QuitarRolCommand { get; }
	#endregion


	public GestionUsuariosViewModel(IServicioUsuario servicioUsuario,
									IServicioDocente servicioDocente,
									IDialogService dialogService)
	{
		_servicioUsuario = servicioUsuario;
		_servicioDocente = servicioDocente;
		_dialogService = dialogService;

		CargarCommand = new AsyncRelayCommand(ExecuteCargarCommandAsync);
		CrearUsuarioCommand = new AsyncRelayCommand(ExecuteCrearUsuarioCommandAsync, CanExecuteCrearUsuarioCommand);
		AsignarRolCommand = new AsyncRelayCommand(ExecuteAsignarRolCommandAsync, CanExecuteAsignarRolCommand);
		QuitarRolCommand = new AsyncRelayCommand(ExecuteQuitarRolCommandAsync, CanExecuteQuitarRolCommand);
	}

	#region CargarCommand
	/// <remarks>
	/// La carga NO se dispara desde el constructor. El ViewModel se construye dentro de la factory
	/// del <c>INavigationService</c>, donde no hay nada que atrape una excepción: una consulta que
	/// falle ahí revienta la navegación en lugar de mostrar un cartel. Lo dispara la vista con
	/// <c>Loaded</c>, igual que el resto de las pantallas de listado.
	/// </remarks>
	private async Task ExecuteCargarCommandAsync()
	{
		try
		{
			Cargando = true;

			var usuarios = await _servicioUsuario.ListarUsuariosAsync();
			var docentes = await _servicioDocente.ListarDocentesActivosAsync();

			var seleccionadoID = UsuarioSeleccionado?.UsuarioID;

			Usuarios.Clear();
			foreach (var usuario in usuarios)
			{
				Usuarios.Add(usuario);
			}

			// Se recalcula en cada carga: un alta mueve un docente de esta lista a la de arriba.
			var conCuenta = usuarios.Select(x => x.DocenteID).ToHashSet();
			DocentesSinCuenta.Clear();
			foreach (var docente in docentes.Where(x => !conCuenta.Contains(x.DocenteID)))
			{
				DocentesSinCuenta.Add(docente);
			}

			// La selección se repone por Id y no por instancia: los Response son records nuevos en
			// cada carga, así que la referencia anterior ya no está en la colección y la grilla
			// quedaría sin fila elegida justo después de asignar un rol — con los dos botones de rol
			// deshabilitados y sin que se vea por qué.
			UsuarioSeleccionado = seleccionadoID is null
				? null
				: Usuarios.FirstOrDefault(x => x.UsuarioID == seleccionadoID);
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "No se pudieron cargar los usuarios");
		}
		finally
		{
			Cargando = false;
		}
	}
	#endregion

	#region CrearUsuarioCommand
	private bool CanExecuteCrearUsuarioCommand() =>
		DocenteSeleccionado is not null && RolSeleccionado is not null;

	/// <remarks>
	/// El rol es obligatorio y por eso entra en el <c>CanExecute</c>. Hasta el 2026-11-06
	/// <c>RegistrarUsuarioAsync</c> ignoraba el rol del request y creaba cuentas sin un solo permiso,
	/// que entraban al sistema con el menú entero apagado y sin forma de arreglarlo. Un alta sin rol
	/// es exactamente lo que esta pantalla no debe poder producir.
	/// </remarks>
	private async Task ExecuteCrearUsuarioCommandAsync()
	{
		if (DocenteSeleccionado is null || RolSeleccionado is null)
		{
			return;
		}

		string nombre = DocenteSeleccionado.NombreCompleto;
		string rol = RolSeleccionado.Descripcion;

		if (!_dialogService.ConfirmarAccion(SolicitudDeDialogo.Destructiva(
				titulo: $"Crear la cuenta de { nombre }",
				consecuencia: $"La cuenta nace con el rol { rol } y los permisos que ese rol tenga. "
							+ "El nombre de usuario es el correo del legajo y la clave inicial, su "
							+ "número de documento.",
				verbo: "Crear cuenta")))
		{
			return;
		}

		try
		{
			await _servicioUsuario.RegistrarUsuarioAsync(
				new RegistrarUsuarioRequest(DocenteSeleccionado.DocenteID, RolSeleccionado.Id));

			DocenteSeleccionado = null;
			await ExecuteCargarCommandAsync();

			_dialogService.MostrarInformacion($"{ nombre } ya puede ingresar con el rol { rol }.", "Cuenta creada");
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "No se pudo crear la cuenta");
		}
	}
	#endregion

	#region AsignarRolCommand
	/// <remarks>
	/// Exige que el usuario <strong>no tenga</strong> ese rol, porque <c>Usuario.AgregarRol</c> lanza
	/// si ya lo tiene. No duplica la invariante: anticipa su resultado para que el botón esté gris en
	/// vez de levantar un cartel de error. La regla sigue viviendo en el dominio.
	/// </remarks>
	private bool CanExecuteAsignarRolCommand() =>
		UsuarioSeleccionado is not null
		&& RolSeleccionado is not null
		&& !UsuarioSeleccionado.RolesIDs.Contains(RolSeleccionado.Id);

	private async Task ExecuteAsignarRolCommandAsync()
	{
		if (UsuarioSeleccionado is null || RolSeleccionado is null)
		{
			return;
		}

		try
		{
			await _servicioUsuario.AsignarRolAsync(
				new ActualizarRolRequest(UsuarioSeleccionado.UsuarioID, RolSeleccionado.Id));

			await ExecuteCargarCommandAsync();
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "No se pudo asignar el rol");
		}
	}
	#endregion

	#region QuitarRolCommand
	/// <remarks>El espejo de <see cref="CanExecuteAsignarRolCommand"/>: <c>Usuario.QuitarRol</c> lanza si no lo tiene.</remarks>
	private bool CanExecuteQuitarRolCommand() =>
		UsuarioSeleccionado is not null
		&& RolSeleccionado is not null
		&& UsuarioSeleccionado.RolesIDs.Contains(RolSeleccionado.Id);

	private async Task ExecuteQuitarRolCommandAsync()
	{
		if (UsuarioSeleccionado is null || RolSeleccionado is null)
		{
			return;
		}

		// Quitarle el último rol deja la cuenta sin un solo permiso, que es el estado del que esta
		// pantalla vino a rescatar al sistema. Se avisa y se deja seguir: puede ser intencional
		// —desactivar una cuenta sin borrarla— y la decisión es del administrador.
		if (UsuarioSeleccionado.RolesIDs.Count is 1
			&& !_dialogService.ConfirmarAccion(SolicitudDeDialogo.Destructiva(
				titulo: $"Quitarle el único rol a { UsuarioSeleccionado.NombreCompleto }",
				consecuencia: "La cuenta queda sin ningún rol, y por lo tanto sin ningún permiso: "
							+ "puede iniciar sesión pero se encuentra el menú entero deshabilitado. "
							+ "Se le puede asignar otro rol en cualquier momento.",
				verbo: "Quitar el rol")))
		{
			return;
		}

		try
		{
			await _servicioUsuario.QuitarRolAsync(
				new ActualizarRolRequest(UsuarioSeleccionado.UsuarioID, RolSeleccionado.Id));

			await ExecuteCargarCommandAsync();
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "No se pudo quitar el rol");
		}
	}
	#endregion
}
