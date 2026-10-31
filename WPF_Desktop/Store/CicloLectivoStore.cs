using System;

namespace WPF_Desktop.Store;

/// <summary>
/// Ciclo lectivo con el que trabajan las pantallas que filtran por año. Arranca en el año en curso.
/// </summary>
/// <remarks>
/// Es el único store que las pantallas además <b>escriben</b> mientras están abiertas
/// (<c>GestionDocumentacion</c>, <c>GestionReportes</c>, <c>GestionCursantes</c>), y aun así no
/// necesita notificar: cada pantalla expone su propia propiedad observable y el store es sólo el
/// valor compartido. Ver el hallazgo de los eventos muertos en <c>docs/todos.md</c>.
/// </remarks>
internal class CicloLectivoStore
{
	public string CicloLectivo { get; set; } = DateTime.Today.Year.ToString();
}
