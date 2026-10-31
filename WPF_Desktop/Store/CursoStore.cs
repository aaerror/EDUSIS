using System;
namespace WPF_Desktop.Store;

/// <summary>
/// Curso elegido en la pantalla de cursos: el identificador más las dos etiquetas que imprimen las
/// pantallas destino en su encabezado y en sus textos de confirmación.
/// </summary>
/// <remarks>
/// <para>
/// No guarda un <c>CursoViewModel</c>. Un ViewModel como estado compartido ata estas pantallas
/// entre sí por su tipo de presentación y arrastra comandos y validación hasta un Singleton: es la
/// violación 14 de <c>docs/todos.md</c>.
/// </para>
/// <para>
/// Tampoco guarda el <c>CursoResponse</c> completo, aunque el diff sería menor. En un Singleton que
/// nadie limpia, los campos que ninguna pantalla destino lee quedan obsoletos sin que se note; este
/// record lleva exactamente los tres que se leen y nada más.
/// </para>
/// </remarks>
internal sealed record CursoSeleccionado(Guid CursoID, string Grado, string NivelEducativo);

internal class CursoStore
{
	public CursoSeleccionado Curso { get; set; }
}
