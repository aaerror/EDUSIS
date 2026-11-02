using System.Collections.ObjectModel;
using System.Windows.Threading;
using System;

namespace WPF_Desktop.Shared;

/// <summary>
/// La cola de avisos al pie. Única fuente de lo que se muestra abajo a la derecha del área de
/// contenido; la escribe <see cref="DialogService"/> y la dibuja <c>AvisosComponent</c>.
/// </summary>
/// <remarks>
/// <para>
/// Tope de cuatro avisos a la vista. Sin tope, una recarga que avisa por cada registro tapa la
/// pantalla con lo que vino a confirmar; al quinto se descarta el más viejo, que es el que ya
/// tuvo tiempo de leerse.
/// </para>
/// <para>
/// El descarte automático usa <see cref="DispatcherTimer"/> y no <c>Task.Delay</c>: el timer
/// corre en el hilo de la interfaz, así que la colección se modifica siempre desde el hilo que
/// la está dibujando. Modificar una <see cref="ObservableCollection{T}"/> ligada a un
/// <c>ItemsControl</c> desde otro hilo tira <c>NotSupportedException</c>.
/// </para>
/// </remarks>
internal class AvisoStore
{
	private const int MaximoALaVista = 4;

	private static readonly TimeSpan DuracionDelExito = TimeSpan.FromSeconds(5);
	private static readonly TimeSpan DuracionDeLaAdvertencia = TimeSpan.FromSeconds(8);

	public ObservableCollection<Aviso> Avisos { get; } = new();

	public void Informar(string titulo, string mensaje = "") =>
		Agregar(TipoDeAviso.Exito, titulo, mensaje);

	public void Advertir(string titulo, string mensaje = "") =>
		Agregar(TipoDeAviso.Advertencia, titulo, mensaje);

	public void Descartar(Aviso aviso) =>
		Avisos.Remove(aviso);

	private void Agregar(TipoDeAviso tipo, string titulo, string mensaje)
	{
		var aviso = new Aviso(tipo, titulo, mensaje, Descartar);

		Avisos.Add(aviso);

		while (Avisos.Count > MaximoALaVista)
		{
			Avisos.RemoveAt(0);
		}

		Programar(aviso, tipo is TipoDeAviso.Exito ? DuracionDelExito : DuracionDeLaAdvertencia);
	}

	private void Programar(Aviso aviso, TimeSpan duracion)
	{
		var reloj = new DispatcherTimer { Interval = duracion };

		reloj.Tick += (_, _) =>
		{
			reloj.Stop();
			Descartar(aviso);
		};

		reloj.Start();
	}
}
