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
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
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
	#region Paneles del índice
	/// <remarks>
	/// El índice muestra una de las dos listas a la vez y el conmutador de arriba enlaza su
	/// <c>Tag</c> contra <see cref="PanelAbierto"/> con el converter <c>esIgualA</c>, igual que
	/// <c>GestionCursantes</c> contra <c>VistaDelAlumno</c>. Son constantes y no un <c>enum</c>
	/// porque viajan como <c>ConverterParameter</c>, que es texto.
	/// </remarks>
	public const string PANEL_CUENTAS = "Cuentas";
	public const string PANEL_SIN_CUENTA = "SinCuenta";
	#endregion

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

	/// <summary>
	/// Vistas filtradas de las dos listas. El filtro se aplica en memoria sobre lo ya cargado, así
	/// que responde a cada tecla sin ir a la base.
	/// </summary>
	/// <remarks>
	/// Son <c>ICollectionView</c> y no colecciones aparte a propósito: una vista filtra la colección
	/// original sin copiarla, mientras que rellenar una segunda colección con cada tecla destruiría y
	/// recrearía las filas, y la elegida se perdería en el camino — el mismo defecto que
	/// <see cref="ExecuteCargarCommandAsync"/> documenta para la recarga.
	/// </remarks>
	public ICollectionView UsuariosFiltrados { get; }
	public ICollectionView DocentesFiltrados { get; }
	#endregion

	#region Selección
	/// <summary>Cuál de las dos listas está abierta en el índice: una de las constantes PANEL_.</summary>
	[ObservableProperty]
	private string _panelAbierto = PANEL_CUENTAS;

	/// <summary>
	/// Texto del campo de filtro. Es uno solo para las dos listas porque sólo una se ve a la vez, y se
	/// limpia al cambiar de pestaña: un filtro heredado de la lista anterior esconde filas de la
	/// nueva sin que se vea por qué.
	/// </summary>
	[ObservableProperty]
	private string _filtro = string.Empty;

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

	/// <summary>Abre una de las dos listas del índice. El parámetro es una de las constantes PANEL_.</summary>
	public IRelayCommand<string> VerCommand { get; }
	#endregion


	public GestionUsuariosViewModel(IServicioUsuario servicioUsuario,
									IServicioDocente servicioDocente,
									IDialogService dialogService)
	{
		_servicioUsuario = servicioUsuario;
		_servicioDocente = servicioDocente;
		_dialogService = dialogService;

		UsuariosFiltrados = CollectionViewSource.GetDefaultView(Usuarios);
		UsuariosFiltrados.Filter = CoincideLaCuenta;

		DocentesFiltrados = CollectionViewSource.GetDefaultView(DocentesSinCuenta);
		DocentesFiltrados.Filter = CoincideElDocente;

		CargarCommand = new AsyncRelayCommand(ExecuteCargarCommandAsync);
		CrearUsuarioCommand = new AsyncRelayCommand(ExecuteCrearUsuarioCommandAsync, CanExecuteCrearUsuarioCommand);
		AsignarRolCommand = new AsyncRelayCommand(ExecuteAsignarRolCommandAsync, CanExecuteAsignarRolCommand);
		QuitarRolCommand = new AsyncRelayCommand(ExecuteQuitarRolCommandAsync, CanExecuteQuitarRolCommand);
		VerCommand = new RelayCommand<string>(ExecuteVer);
	}

	#region Textos de la pantalla
	/// <summary>Resumen de la lista abierta: "12 cuentas", "3 de 12", "Sin docentes pendientes".</summary>
	public string Resumen
	{
		get
		{
			bool cuentas = PanelAbierto == PANEL_CUENTAS;
			int total = cuentas ? Usuarios.Count : DocentesSinCuenta.Count;

			if (total is 0)
			{
				return Cargando
					? "Cargando…"
					: cuentas ? "Todavía no hay cuentas" : "Todos los docentes tienen cuenta";
			}

			int visibles = Contar(cuentas ? UsuariosFiltrados : DocentesFiltrados);

			if (visibles != total)
			{
				return $"{ visibles } de { total }";
			}

			return cuentas
				? $"{ total } { (total is 1 ? "cuenta" : "cuentas") }"
				: $"{ total } { (total is 1 ? "docente sin cuenta" : "docentes sin cuenta") }";
		}
	}

	/// <summary>
	/// Qué concede el rol elegido, leído de <c>Rol.Permisos</c>.
	/// </summary>
	/// <remarks>
	/// La pantalla reparte permisos y hasta ahora no decía cuáles: la atribución rol → permisos sólo
	/// estaba escrita en <c>Domain/Usuarios/Rol.cs</c> y en los diagramas de <c>docs/uml/</c>, así que
	/// el administrador elegía un rol por su nombre y averiguaba qué había concedido entrando con esa
	/// cuenta. Es presentación de un dato del dominio: se lee, no se reescribe acá.
	/// </remarks>
	public string PermisosDelRol
	{
		get
		{
			if (RolSeleccionado is null)
			{
				return string.Empty;
			}

			var permisos = RolSeleccionado.Permisos;

			if (permisos.Count is 0)
			{
				return "No concede ningún permiso.";
			}

			return $"{ permisos.Count } { (permisos.Count is 1 ? "permiso" : "permisos") }: "
				 + string.Join(" · ", permisos.Select(x => x.Descripcion));
		}
	}

	/// <summary>
	/// Por qué uno de los dos botones de rol está gris.
	/// </summary>
	/// <remarks>
	/// <c>CanExecute</c> apaga el botón que el dominio rechazaría —<c>AgregarRol</c> lanza si ya lo
	/// tiene, <c>QuitarRol</c> si no lo tiene—, pero un botón gris no se distingue de un botón gris
	/// por falta de selección. Esta línea dice cuál de los dos casos es.
	/// </remarks>
	public string EstadoDeLosRoles
	{
		get
		{
			if (UsuarioSeleccionado is null)
			{
				return string.Empty;
			}

			if (RolSeleccionado is null)
			{
				return "Elegí un rol para asignarlo o quitarlo.";
			}

			return UsuarioSeleccionado.RolesIDs.Contains(RolSeleccionado.Id)
				? $"La cuenta ya tiene { RolSeleccionado.Descripcion }: sólo se le puede quitar."
				: $"La cuenta no tiene { RolSeleccionado.Descripcion }: sólo se le puede asignar.";
		}
	}

	/// <summary>
	/// La cuenta elegida no tiene ni un rol.
	/// </summary>
	/// <remarks>
	/// Es el estado del que esta pantalla vino a rescatar al sistema —una cuenta que entra con el menú
	/// entero apagado— y la ficha lo dice con palabras: sin esto, la sección de roles de una cuenta
	/// así se vería igual que la de una cuenta todavía no cargada, o sea vacía y sin explicación.
	/// </remarks>
	public bool CuentaSinRoles =>
		UsuarioSeleccionado is not null && UsuarioSeleccionado.RolesIDs.Count is 0;

	/// <summary>Con qué rol nace la cuenta que está por crearse.</summary>
	public string EstadoDelAlta =>
		RolSeleccionado is null
			? "Elegí el rol con el que nace la cuenta: sin rol, entra al sistema con el menú entero apagado."
			: $"Nace con el rol { RolSeleccionado.Descripcion }.";

	/// <remarks>
	/// <c>ICollectionView</c> no expone un contador: lo único que garantiza es que se puede recorrer.
	/// </remarks>
	private static int Contar(ICollectionView vista)
	{
		int cuenta = 0;

		foreach (var _ in vista)
		{
			cuenta++;
		}

		return cuenta;
	}
	#endregion

	#region Filtro y conmutador
	/// <summary>Coincidencia por nombre del docente o por nombre de usuario.</summary>
	private bool CoincideLaCuenta(object item)
	{
		if (string.IsNullOrWhiteSpace(Filtro))
		{
			return true;
		}

		if (item is not UsuarioResponse cuenta)
		{
			return false;
		}

		string filtro = Filtro.Trim();

		return cuenta.NombreCompleto.Contains(filtro, StringComparison.OrdinalIgnoreCase)
			|| cuenta.Usuario.Contains(filtro, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>Coincidencia por nombre, legajo o documento, igual que el índice de Docentes.</summary>
	private bool CoincideElDocente(object item)
	{
		if (string.IsNullOrWhiteSpace(Filtro))
		{
			return true;
		}

		if (item is not LegajoDocenteResponse docente)
		{
			return false;
		}

		string filtro = Filtro.Trim();

		return docente.NombreCompleto.Contains(filtro, StringComparison.OrdinalIgnoreCase)
			|| docente.Legajo.Contains(filtro, StringComparison.OrdinalIgnoreCase)
			|| docente.DNI.Contains(filtro, StringComparison.OrdinalIgnoreCase);
	}

	private void ExecuteVer(string? panel)
	{
		if (panel is not (PANEL_CUENTAS or PANEL_SIN_CUENTA))
		{
			return;
		}

		PanelAbierto = panel;
	}

	partial void OnFiltroChanged(string value)
	{
		UsuariosFiltrados.Refresh();
		DocentesFiltrados.Refresh();
		OnPropertyChanged(nameof(Resumen));
	}

	partial void OnPanelAbiertoChanged(string value)
	{
		Filtro = string.Empty;
		OnPropertyChanged(nameof(Resumen));
	}

	partial void OnCargandoChanged(bool value) =>
		OnPropertyChanged(nameof(Resumen));

	partial void OnUsuarioSeleccionadoChanged(UsuarioResponse? value)
	{
		OnPropertyChanged(nameof(EstadoDeLosRoles));
		OnPropertyChanged(nameof(CuentaSinRoles));
	}

	partial void OnRolSeleccionadoChanged(Rol? value)
	{
		OnPropertyChanged(nameof(PermisosDelRol));
		OnPropertyChanged(nameof(EstadoDeLosRoles));
		OnPropertyChanged(nameof(EstadoDelAlta));
	}
	#endregion

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

			// Las dos listas cambiaron de tamaño: el conteo del índice y el de las pestañas salen de
			// Usuarios.Count y DocentesSinCuenta.Count, que la ObservableCollection ya notifica sola,
			// pero Resumen es una propiedad calculada y nadie la notifica por él.
			OnPropertyChanged(nameof(Resumen));
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

		Guid docenteID = DocenteSeleccionado.DocenteID;
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

			// El docente acaba de salir de la lista de pendientes y entrar a la de cuentas: la pantalla
			// se mueve con él y lo deja elegido. Quedarse en la pestaña de pendientes mostraría la
			// ficha vacía de una selección que ya no existe, y el alta recién hecha —con su insignia de
			// rol— quedaría en la otra pestaña, sin que nada indique que hay que ir a mirarla.
			PanelAbierto = PANEL_CUENTAS;
			UsuarioSeleccionado = Usuarios.FirstOrDefault(x => x.DocenteID == docenteID);

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
