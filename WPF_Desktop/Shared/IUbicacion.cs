using System.Collections.Generic;

namespace WPF_Desktop.Shared;

/// <summary>
/// La implementa el ViewModel de una pantalla que sabe decir dónde está parada.
/// </summary>
/// <remarks>
/// <para>
/// Es el ViewModel y no el shell el que arma la ruta, por una razón concreta: el camino de
/// vuelta necesita contexto que sólo él tiene. Volver de "Cursantes" a "Divisiones" no es
/// navegar a una pantalla, es navegar a las divisiones DE ESE CURSO, y el curso vive en el
/// store que este ViewModel ya leyó.
/// </para>
/// <para>
/// El shell no la exige: una pantalla que no la implementa muestra sólo su título, como antes.
/// </para>
/// </remarks>
internal interface IUbicacion
{
	/// <summary>
	/// La ruta completa, del nivel más alto al actual. El último tramo es esta pantalla y no
	/// lleva comando.
	/// </summary>
	IReadOnlyList<Migaja> Ruta { get; }
}
