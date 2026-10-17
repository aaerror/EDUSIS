using Core.Shared.Seguridad;
using Domain.Shared;
using Domain.Usuarios;

namespace EDUSIS.TestSupport.Fakes;

/// <summary>
/// Doble de <see cref="ISesionActual"/> para las pruebas de los servicios de <c>Core</c>. Es la
/// pieza que destraba los fixtures al introducirse las guardas de permiso: los servicios ahora
/// reciben la sesión por constructor, y ni <c>HostDeServicios</c> ni
/// <c>ComposicionDePruebaE2E</c> tenían quién la provea.
/// </summary>
/// <remarks>
/// Deliberadamente <strong>sin registro por defecto en <c>CoreDI</c></strong>. Si la interfaz
/// quedara registrada en la capa de aplicación con permisos totales, un composition root que
/// olvidara su propio registro correría sin autorización y sin un solo error: el olvido se
/// resolvería contra el doble que concede todo. Que falte el registro y el contenedor falle al
/// resolver es la conducta buscada, no un inconveniente.
/// </remarks>
public sealed class SesionDePrueba : ISesionActual
{
	private readonly IReadOnlySet<Permiso> _permisos;


	#region CONSTRUCTOR
	private SesionDePrueba(Guid? usuarioID, IEnumerable<Permiso> permisos)
	{
		UsuarioID = usuarioID;
		_permisos = permisos.ToHashSet();
	}
	#endregion

	public Guid? UsuarioID { get; }

	public bool EstaAutenticado => UsuarioID is not null;

	public bool TienePermiso(Permiso unPermiso) =>
		_permisos.Contains(unPermiso);

	#region Fábricas
	/// <summary>
	/// Sesión abierta con los diez permisos. Es la que usan los fixtures para que las pruebas
	/// que ejercitan un caso de uso —y no la autorización— no tengan que saber qué permiso pide.
	/// </summary>
	public static SesionDePrueba ConTodosLosPermisos() =>
		new(Guid.NewGuid(), Enumeration.GetAll<Permiso>());

	/// <summary>Sesión abierta con exactamente los permisos indicados.</summary>
	public static SesionDePrueba Con(params Permiso[] permisos) =>
		new(Guid.NewGuid(), permisos);

	/// <summary>
	/// Sesión abierta sin ningún permiso: un usuario autenticado que no puede operar. Es el caso
	/// que distingue "no entró" de "entró y no le corresponde".
	/// </summary>
	public static SesionDePrueba SinPermisos() =>
		new(Guid.NewGuid(), Array.Empty<Permiso>());

	/// <summary>Sesión cerrada: nadie autenticado.</summary>
	public static SesionDePrueba Cerrada() =>
		new(null, Array.Empty<Permiso>());
	#endregion
}
