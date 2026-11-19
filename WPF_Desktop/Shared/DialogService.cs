using System.Linq;
using System;
using System.Windows;
using System.Windows.Threading;
using WPF_Desktop.Windows;

namespace WPF_Desktop.Shared;

/// <summary>
/// Implementación real: avisos al pie para lo que sólo informa, diálogo propio para lo que
/// interrumpe.
/// </summary>
/// <remarks>
/// <para>
/// Antes esta clase era un envoltorio de <c>MessageBox.Show</c>, cuatro líneas. El cambio no
/// toca a ningún ViewModel: los once que ya usaban <see cref="IDialogService"/> pasaron a tener
/// avisos no bloqueantes y diálogos tematizados sin que se les editara una línea.
/// </para>
/// <para>
/// Lo que se ganó al dejar el MessageBox nativo: el diálogo se puede escribir en el idioma de
/// la aplicación —"Eliminar división" en lugar de "Sí"—, tiene lugar para la consecuencia, y
/// los éxitos dejaron de exigir un clic para seguir trabajando. Para quien marca treinta
/// asistencias seguidas, eran treinta clics de más.
/// </para>
/// </remarks>
internal class DialogService : IDialogService
{
	private readonly AvisoStore _avisos;

	public DialogService(AvisoStore avisos)
	{
		_avisos = avisos;
	}

	/// <summary>
	/// Un éxito no interrumpe: aparece al pie y se va solo.
	/// </summary>
	/// <remarks>
	/// El título de la operación es lo que el usuario necesita leer ("Cambios guardados"), y el
	/// mensaje largo el detalle. Los call sites heredados pasan el mensaje primero y el título
	/// después —firma original—, así que acá el título va arriba y el mensaje abajo.
	/// </remarks>
	public void MostrarInformacion(string mensaje, string titulo)
	{
		_avisos.Informar(titulo, mensaje);
	}

	public void MostrarAdvertencia(string mensaje, string titulo)
	{
		_avisos.Advertir(titulo, mensaje);
	}

	/// <summary>
	/// Un error sí interrumpe. Un aviso que se va solo a los cinco segundos es exactamente lo
	/// que no hay que hacer con la única pista de por qué algo no se guardó.
	/// </summary>
	/// <remarks>
	/// Si el diálogo no se puede abrir ahora —ver <see cref="Preguntar"/>— se lo posterga y se lo
	/// muestra apenas el dispatcher lo permita: un error no se pierde ni se convierte en otro.
	/// </remarks>
	public void MostrarError(string mensaje, string titulo)
	{
		var solicitud = SolicitudDeDialogo.Error(titulo, mensaje);

		if (Preguntar(solicitud) is null)
		{
			Application.Current?.Dispatcher.BeginInvoke(new Action(() => Preguntar(solicitud)), DispatcherPriority.Background);
		}
	}

	public bool Confirmar(string mensaje, string titulo)
	{
		return Preguntar(SolicitudDeDialogo.Revisar(titulo, mensaje, "Confirmar")) is true;
	}

	/// <remarks>
	/// Una confirmación no se puede postergar: quien pregunta espera la respuesta para seguir. Si el
	/// diálogo no se puede abrir, la respuesta es "no", que es la única que no ejecuta nada que el
	/// usuario no aceptó.
	/// </remarks>
	public bool ConfirmarAccion(SolicitudDeDialogo solicitud)
	{
		return Preguntar(solicitud) is true;
	}

	/// <summary>
	/// Abre el diálogo modal y devuelve lo que contestó el usuario, o <see langword="null"/> si en
	/// este momento WPF no permite abrir un diálogo modal.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <c>ShowDialog</c> necesita un bucle de mensajes anidado, y WPF lo prohíbe mientras el
	/// dispatcher tiene el procesamiento suspendido —durante una pasada de layout, por ejemplo—:
	/// lanza <see cref="InvalidOperationException"/> ("No se puede realizar esta operación mientras el
	/// procesamiento del distribuidor está suspendido"). Pasaba cuando un ViewModel capturaba un error
	/// en un comando disparado por un evento de la vista durante el layout: el diálogo de error
	/// lanzaba a su vez, esa excepción escapaba del <c>catch</c> del ViewModel y el manejador global
	/// cerraba la aplicación por un error que ya estaba atendido.
	/// </para>
	/// <para>
	/// La ventana alcanza a mostrarse antes de que falle el bucle modal, así que se la cierra. El
	/// cierre se posterga también, porque cerrar una ventana durante el layout puede fallar por el
	/// mismo motivo.
	/// </para>
	/// </remarks>
	private static bool? Preguntar(SolicitudDeDialogo solicitud)
	{
		var dialogo = new ConfirmacionWindow(solicitud)
		{
			Owner = VentanaActiva()
		};

		try
		{
			return dialogo.ShowDialog() is true;
		}
		catch (InvalidOperationException)
		{
			dialogo.Dispatcher.BeginInvoke(new Action(() =>
			{
				if (dialogo.IsLoaded)
				{
					dialogo.Close();
				}
			}), DispatcherPriority.Background);

			return null;
		}
	}

	/// <summary>
	/// Dueño del diálogo: la ventana activa, o la principal si no hay ninguna activa.
	/// </summary>
	/// <remarks>
	/// Sin <c>Owner</c> el diálogo puede aparecer detrás de la ventana que lo pidió y
	/// centrarse en la pantalla en lugar de sobre ella. Con <c>WindowStartupLocation</c> en
	/// <c>CenterOwner</c> y sin dueño, WPF lo centra en el escritorio.
	/// </remarks>
	private static Window? VentanaActiva()
	{
		if (Application.Current is null)
		{
			return null;
		}

		return Application.Current.Windows
							.OfType<Window>()
							.FirstOrDefault(ventana => ventana.IsActive)
			?? Application.Current.MainWindow;
	}
}
