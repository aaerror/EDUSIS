using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace WPF_Desktop.Components;

public partial class PaginaComponent : UserControl
{
	public static readonly DependencyProperty RutaProperty = DependencyProperty.Register(nameof(Ruta), typeof(IEnumerable), typeof(PaginaComponent), new PropertyMetadata(null));
	public static readonly DependencyProperty TituloProperty = DependencyProperty.Register(nameof(Titulo), typeof(string), typeof(PaginaComponent), new PropertyMetadata(string.Empty));
	public static readonly DependencyProperty SubtituloProperty = DependencyProperty.Register(nameof(Subtitulo), typeof(string), typeof(PaginaComponent), new PropertyMetadata(string.Empty));
	public static readonly DependencyProperty AccionProperty = DependencyProperty.Register(nameof(Accion), typeof(object), typeof(PaginaComponent), new PropertyMetadata(null));
	public static readonly DependencyProperty SecundariasProperty = DependencyProperty.Register(nameof(Secundarias), typeof(object), typeof(PaginaComponent), new PropertyMetadata(null));


	public PaginaComponent()
	{
		InitializeComponent();
	}

	/// <summary>La ruta hasta esta pantalla. Se liga a <c>IUbicacion.Ruta</c> del ViewModel.</summary>
	public IEnumerable? Ruta
	{
		get
		{
			return (IEnumerable?) GetValue(RutaProperty);
		}

		set
		{
			SetValue(RutaProperty, value);
		}
	}

	/// <summary>
	/// El nombre de la pantalla, no el del dato que muestra.
	/// </summary>
	/// <remarks>
	/// Tres pantallas hermanas —Cátedras, Horarios y Situación de revista— ligaban el título a
	/// <c>Materia.Descripcion</c>, así que las tres se llamaban igual ("Matemática") y en
	/// ninguna aparecía la palabra que decía qué era esa pantalla. El dato va en la ruta o en la
	/// ficha; el título dice qué se hace acá.
	/// </remarks>
	public string Titulo
	{
		get
		{
			return (string) GetValue(TituloProperty);
		}

		set
		{
			SetValue(TituloProperty, value);
		}
	}

	/// <summary>Qué se puede hacer en esta pantalla. No reformula el título.</summary>
	public string Subtitulo
	{
		get
		{
			return (string) GetValue(SubtituloProperty);
		}

		set
		{
			SetValue(SubtituloProperty, value);
		}
	}

	/// <summary>La acción principal de la pantalla. Una sola, destacada.</summary>
	public object? Accion
	{
		get
		{
			return GetValue(AccionProperty);
		}

		set
		{
			SetValue(AccionProperty, value);
		}
	}

	/// <summary>Acciones opcionales, en estilo secundario o terciario.</summary>
	public object? Secundarias
	{
		get
		{
			return GetValue(SecundariasProperty);
		}

		set
		{
			SetValue(SecundariasProperty, value);
		}
	}
}
