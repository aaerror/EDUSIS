using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WPF_Desktop.Shared;

/// <summary>
/// Devuelve la rueda del mouse al contenedor de arriba cuando el control que está debajo del
/// puntero no tiene nada más que scrollear.
/// </summary>
/// <remarks>
/// <para>
/// <c>ScrollViewer.OnMouseWheel</c> de WPF marca el evento como manejado SIEMPRE que exista un
/// <c>ScrollInfo</c>, haya o no desplazamiento para consumir. Como la plantilla de
/// <c>Grilla</c> trae su propio <see cref="ScrollViewer"/>, una grilla con dos filas —o una ya
/// scrolleada hasta el fondo— se come la rueda y la página de atrás no se mueve. Con tres
/// grillas apiladas, la mitad de la superficie de la pantalla queda muerta al gesto principal
/// de navegación.
/// </para>
/// <para>
/// El arreglo correcto sería que la grilla llene su celda y no scrollee sola, y así está
/// compuesta la pantalla de alumnos. Esto cubre lo que queda: las ventanas chicas, donde el
/// contenido sí desborda, y las pantallas que todavía apilan grillas dentro de un scroll de
/// página.
/// </para>
/// </remarks>
public static class RuedaDelMouse
{
	/// <summary>Enciende el reenvío en el control al que se lo cuelga.</summary>
	public static readonly DependencyProperty BurbujeaProperty =
		DependencyProperty.RegisterAttached(
			"Burbujea",
			typeof(bool),
			typeof(RuedaDelMouse),
			new PropertyMetadata(false, AlCambiarBurbujea));

	public static bool GetBurbujea(DependencyObject elemento) => (bool) elemento.GetValue(BurbujeaProperty);

	public static void SetBurbujea(DependencyObject elemento, bool valor) => elemento.SetValue(BurbujeaProperty, valor);

	private static void AlCambiarBurbujea(DependencyObject elemento, DependencyPropertyChangedEventArgs e)
	{
		if (elemento is not UIElement control)
		{
			return;
		}

		// PreviewMouseWheel y no MouseWheel: hay que llegar ANTES que el ScrollViewer interno,
		// que es quien marca el evento como manejado.
		if ((bool) e.NewValue)
		{
			control.PreviewMouseWheel += AlGirarLaRueda;
		}
		else
		{
			control.PreviewMouseWheel -= AlGirarLaRueda;
		}
	}

	private static void AlGirarLaRueda(object sender, MouseWheelEventArgs e)
	{
		if (e.Handled || sender is not DependencyObject origen)
		{
			return;
		}

		// El control todavía tiene desplazamiento propio en esa dirección: es suyo.
		if (BuscarScrollViewer(origen) is ScrollViewer scroll && PuedeConsumir(scroll, e.Delta))
		{
			return;
		}

		e.Handled = true;

		var reenvio = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
		{
			RoutedEvent = UIElement.MouseWheelEvent,
			Source = sender
		};

		(VisualTreeHelper.GetParent(origen) as UIElement)?.RaiseEvent(reenvio);
	}

	private static bool PuedeConsumir(ScrollViewer scroll, int delta)
	{
		if (scroll.ScrollableHeight <= 0)
		{
			return false;
		}

		// Delta negativo es rueda hacia abajo.
		return delta < 0
			? scroll.VerticalOffset < scroll.ScrollableHeight
			: scroll.VerticalOffset > 0;
	}

	/// <summary>El <see cref="ScrollViewer"/> que la plantilla del control puso adentro.</summary>
	private static ScrollViewer? BuscarScrollViewer(DependencyObject raiz)
	{
		if (raiz is ScrollViewer propio)
		{
			return propio;
		}

		for (var i = 0; i < VisualTreeHelper.GetChildrenCount(raiz); i++)
		{
			if (BuscarScrollViewer(VisualTreeHelper.GetChild(raiz, i)) is ScrollViewer encontrado)
			{
				return encontrado;
			}
		}

		return null;
	}
}
