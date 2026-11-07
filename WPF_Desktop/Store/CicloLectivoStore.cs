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
internal class CicloLectivoStore : IEstadoDeSesion
{
	public string CicloLectivo { get; set; } = DateTime.Today.Year.ToString();

	/// <summary>
	/// Vuelve al año en curso, no al año que dejó el usuario anterior.
	/// </summary>
	/// <remarks>
	/// Es el único store de los seis que las pantallas escriben mientras están abiertas, así que es
	/// también el único donde conservar el valor entre sesiones sería defendible —"la máquina
	/// trabaja sobre el ciclo 2024"—. Se limpia igual: el año en curso es el valor que acierta el
	/// 99% de las veces y el que el usuario que entra puede verificar de un vistazo, mientras que un
	/// ciclo heredado del turno anterior se ve idéntico a uno elegido a propósito.
	/// </remarks>
	public void Limpiar() =>
		CicloLectivo = DateTime.Today.Year.ToString();
}
