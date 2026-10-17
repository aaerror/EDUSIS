using Domain.Shared;
using Domain.Usuarios;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WPF_Desktop.Security;

/// <summary>
/// Sesión en curso de la aplicación. Se registra como <c>Singleton</c>: hay un único usuario
/// logueado por proceso.
/// </summary>
/// <remarks>
/// Reemplaza a <c>Thread.CurrentPrincipal</c> como almacenamiento de la sesión. El motivo es
/// concreto, no estético: en .NET (Core) el security context NO viaja con el
/// <c>ExecutionContext</c> —a diferencia de .NET Framework—, así que el principal no se propaga
/// a las continuaciones <c>async</c> que retomen fuera del hilo de UI. Hoy sobrevive de casualidad
/// porque los <c>await</c> de los ViewModels no usan <c>ConfigureAwait(false)</c> y el
/// <c>SynchronizationContext</c> del Dispatcher los devuelve al hilo de UI. Una decisión de
/// autorización no puede depender de eso: un singleton no tiene afinidad de hilo.
/// </remarks>
internal sealed class SesionUsuario : ISesionUsuario
{
	private Guid? _usuarioID;
	private IReadOnlySet<Permiso> _permisos = new HashSet<Permiso>();

	// Anotado como nullable a propósito, a diferencia de los Store de Store/: el evento es null
	// hasta que alguien se suscribe, y con Nullable habilitado la forma no anulable emite CS8618.
	public event Action? Cambio;

	public Guid? UsuarioID => _usuarioID;

	public bool EstaAutenticado => _usuarioID is not null;

	public bool TienePermiso(Permiso unPermiso) =>
		_permisos.Contains(unPermiso);

	public void Iniciar(Guid usuarioID, IEnumerable<int> rolesIDs)
	{
		if (Guid.Empty.Equals(usuarioID))
		{
			throw new ArgumentNullException(nameof(usuarioID), "Se debe asignar un usuario a la sesión.");
		}

		// Los permisos se asignan ANTES del identificador: así EstaAutenticado nunca es true
		// con el conjunto de permisos todavía vacío.
		_permisos = ResolverPermisos(rolesIDs ?? Enumerable.Empty<int>());
		_usuarioID = usuarioID;

		Cambio?.Invoke();
	}

	public void Cerrar()
	{
		_usuarioID = null;
		_permisos = new HashSet<Permiso>();

		Cambio?.Invoke();
	}

	/// <summary>
	/// Aplana los roles del usuario en su conjunto de permisos. Los roles llegan como Id
	/// (<c>UsuarioResponse.RolesIDs</c>) y se resuelven contra los que el código conoce; un Id
	/// desconocido se **ignora** en vez de lanzar, porque un rol que la base tenga y el código no,
	/// no debe impedir iniciar sesión ni habilitar nada.
	/// </summary>
	private static IReadOnlySet<Permiso> ResolverPermisos(IEnumerable<int> rolesIDs)
	{
		var conocidos = Enumeration.GetAll<Rol>()
								   .ToDictionary(rol => rol.Id);

		return rolesIDs.Where(conocidos.ContainsKey)
					   .SelectMany(rolID => conocidos[rolID].Permisos)
					   .ToHashSet();
	}
}
