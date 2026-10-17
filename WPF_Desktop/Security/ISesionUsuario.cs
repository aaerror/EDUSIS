using Core.Shared.Seguridad;
using System;
using System.Collections.Generic;

namespace WPF_Desktop.Security;

/// <summary>
/// La sesión vista desde la presentación: el puerto que consume <c>Core</c>
/// (<see cref="ISesionActual"/>) más los mutadores que sólo esta capa necesita. Se separa en dos
/// interfaces para que un caso de uso no pueda abrir ni cerrar sesión, sólo consultarla.
/// </summary>
internal interface ISesionUsuario : ISesionActual
{
	/// <summary>
	/// Se dispara al iniciar o cerrar sesión. Mismo patrón que los <c>*NavigationStore</c>: los
	/// consumidores se suscriben para reevaluar su estado — en particular, los comandos que
	/// dependen de un permiso necesitan un <c>NotifyCanExecuteChanged</c> acá.
	/// </summary>
	event Action? Cambio;

	void Iniciar(Guid usuarioID, IEnumerable<int> rolesIDs);

	void Cerrar();
}
