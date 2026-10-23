using WPF_Desktop.ViewModels.Docentes;

namespace WPF_Desktop.ViewModels.Shared.Messages;

/// <summary>
/// Resultado del buscador de docentes: el modal avisa a quien lo abrió cuál se eligió.
/// </summary>
/// <remarks>
/// El modal no conoce a sus llamadores. <c>ModalNavigationService</c> construye el ViewModel con una
/// fábrica sin parámetros, así que no hay por dónde pasarle una devolución de llamada: el resultado
/// viaja por el mensajero, que es el mismo camino que ya usa <c>UsuarioLogeadoMessage</c>.
///
/// Lleva el ViewModel entero y no sólo el identificador porque las dos pantallas que lo reciben
/// necesitan además el nombre para confirmar la operación, y <c>LegajoDocenteViewModel</c> sólo se
/// construye desde un <c>LegajoDocenteResponse</c>: rearmarlo desde primitivas pediría un
/// constructor nuevo para nada.
/// </remarks>
internal sealed record DocenteElegidoMessage(LegajoDocenteViewModel Docente);
