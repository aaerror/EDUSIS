using System;
namespace WPF_Desktop.Store;

/// <summary>
/// Materia elegida en la pantalla de currículas: el identificador propio, el del curso al que
/// pertenece —que la pantalla de cátedras necesita para listar divisiones— y la descripción, que es
/// el título de las dos pantallas destino.
/// </summary>
/// <remarks>
/// No guarda un <c>MateriaViewModel</c> (violación 14 de <c>docs/todos.md</c>). Era el peor caso de
/// los tres: <c>MateriaViewModel</c> deriva de <c>ObservableValidator</c> y lleva <c>[Required]</c>
/// sobre <c>Descripcion</c>, así que el contexto de navegación arrastraba validación de formulario
/// y podía viajar en estado inválido. Se dejan afuera <c>CurriculaID</c> y <c>HorasCatedra</c>:
/// ninguna pantalla destino los lee del store.
/// </remarks>
internal sealed record MateriaSeleccionada(Guid CursoID, Guid MateriaID, string Descripcion);

internal class MateriaStore
{
	public MateriaSeleccionada Materia { get; set; }
}
