using Shouldly;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WPF_Desktop.Views.Cursos.Divisiones;
using Xunit;

namespace WPF_Desktop.UnitTests;

/// <summary>
/// La planilla del día deja ver la tabla de alumnos, y el casillero marcado se ve marcado.
/// </summary>
/// <remarks>
/// <para>
/// ESTA PANTALLA SE QUEDÓ SIN TABLA DE ALUMNOS Y NINGUNA PRUEBA SE ENTERÓ. Fecha, contadores y el
/// pie de la planilla son filas <c>Auto</c> y la grilla es la única fila <c>*</c>: sumadas, las
/// <c>Auto</c> se comían el presupuesto entero de la tarjeta y la fila <c>*</c> resolvía a
/// <b>0px</b> en la ventana mínima y a <b>31px</b> —sólo los encabezados de columna, cero
/// alumnos— en la que abre la aplicación. La pantalla sólo servía maximizada.
/// </para>
/// <para>
/// Es el mismo defecto que <c>GuardasEstaticasW6Tests.Ninguna_vista_pone_una_fila_estrella_adentro_de_un_GroupBox</c>
/// fija para las secciones, pero por el otro lado: ahí la fila <c>*</c> crecía sin límite, acá se
/// aplastaba a cero. Las dos formas son invisibles leyendo el XAML, y ninguna la ve una prueba de
/// ViewModel: hay que <b>medir el layout</b>. Por eso esta prueba mide en lugar de inspeccionar el
/// código, y es la única del proyecto que lo hace.
/// </para>
/// <para>
/// El <c>DataContext</c> es un POCO y no el ViewModel real —que es <c>internal</c> y pide servicio,
/// diálogo y stores por constructor—: los enlaces de WPF son por nombre, así que un objeto con las
/// propiedades que la vista lee alcanza para que el layout sea el de verdad. Lo que se mide es el
/// alto que reparte el <c>Grid</c>, y de eso el ViewModel no participa.
/// </para>
/// </remarks>
[Collection(ColeccionDeWPF.NOMBRE)]
public class AltoDeLaPlanillaTests
{
	/// <summary>Alto mínimo de la ventana (<c>MainWindow.xaml</c>, <c>MinHeight="620"</c>).</summary>
	private const double VENTANA_MINIMA = 620;

	/// <summary>Alto con el que abre la aplicación (<c>MainWindow.xaml</c>, <c>Height="800"</c>).</summary>
	private const double VENTANA_POR_OMISION = 800;

	/// <summary>La franja de título de <c>WindowChrome</c> más el ancho del rail del menú.</summary>
	private const double FRANJA_DE_TITULO = 38;
	private const double ANCHO_DEL_RAIL = 248;
	private const double VENTANA_POR_OMISION_ANCHO = 1320;

	/// <summary>Encabezado de columnas (32px) más tres filas de alumno: lo mínimo para tomar lista.</summary>
	private const double ALTO_UTIL_MINIMO = 120;

	/// <summary>
	/// A cualquier alto de ventana que la aplicación permita, la grilla de alumnos se ve.
	/// </summary>
	[Theory]
	[Trait("Categoria", "Unidad")]
	[InlineData(VENTANA_MINIMA)]
	[InlineData(VENTANA_POR_OMISION)]
	public void La_grilla_de_alumnos_tiene_alto_util_en_toda_ventana_permitida(double altoDeVentana)
	{
		EjecutarEnSTA(() =>
		{
			var vista = Dibujar(altoDeVentana, out _);

			var grilla = (DataGrid) vista.FindName("DataGrid_Registros");

			grilla.ActualHeight.ShouldBeGreaterThanOrEqualTo(ALTO_UTIL_MINIMO,
				$"a {altoDeVentana}px de ventana la grilla mide {grilla.ActualHeight:0.#}px: "
			  + "una fila Auto nueva le comió la fila *");
		});
	}

	/// <summary>
	/// Lo que no entra se alcanza scrolleando, y lo que entra no deja una barra inútil.
	/// </summary>
	/// <remarks>
	/// Las dos mitades importan. Sin la primera, el pie de la planilla —los minutos de la tardanza
	/// y la observación— queda recortado abajo y sin forma de llegar. Sin la segunda, el piso de
	/// <c>MinHeight</c> estaría puesto de más y la pantalla scrollearía siempre, que es el defecto
	/// que el comentario de la vista evitaba prohibiendo el ScrollViewer de página.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void La_planilla_scrollea_cuando_no_entra_y_no_scrollea_cuando_entra()
	{
		EjecutarEnSTA(() =>
		{
			Dibujar(VENTANA_MINIMA, out var apretada);
			Dibujar(VENTANA_POR_OMISION, out var holgada);

			apretada.ScrollableHeight.ShouldBeGreaterThan(0,
				"en la ventana mínima el contenido no entra y tiene que poder scrollearse");

			holgada.ScrollableHeight.ShouldBe(0,
				$"en la ventana por omisión sobra alto y no tiene que haber scroll: sobran {holgada.ScrollableHeight:0.#}px");
		});
	}

	/// <summary>
	/// De los cuatro casilleros de una fila se enciende exactamente el del estado del alumno.
	/// </summary>
	/// <remarks>
	/// El encendido sale de comparar <c>Estado</c> contra la palabra de cada casillero con el
	/// converter <c>esIgualA</c>, y lo pinta un trigger de <c>Style</c> que tiene que ganarle a los
	/// tres estados del mouse de <c>Marca.Plantilla</c>. Las dos cosas se rompen en silencio: un
	/// <c>DataTrigger</c> mal ubicado deja los cuatro casilleros apagados —pasó, con el trigger
	/// adentro de <c>ControlTemplate.Triggers</c>, donde <c>RelativeSource TemplatedParent</c> no se
	/// evalúa— y la fila sigue dibujándose perfecta, sin decir cómo vino marcado el alumno. Que es
	/// el defecto que los cuatro Btn.Icono idénticos tenían de entrada.
	/// </remarks>
	[Theory]
	[Trait("Categoria", "Unidad")]
	[InlineData("Presente", "PRESENTE", "Primario")]
	[InlineData("Tardanza", "TARDANZA", "Primario")]
	[InlineData("Ausencia", "AUSENCIA", "Alerta")]
	[InlineData("Inasistencia", "INASIST.", "Alerta")]
	public void Se_enciende_el_casillero_del_estado_del_alumno(string estado, string rotuloEncendido, string pincelEncendido)
	{
		EjecutarEnSTA(() =>
		{
			var vista = Dibujar(VENTANA_POR_OMISION, out _, estado);

			var casilleros = Casilleros((DataGrid) vista.FindName("DataGrid_Registros"));

			casilleros.Count.ShouldBe(4, "la fila tiene que traer los cuatro casilleros");

			var encendidoEsperado = Color(pincelEncendido);
			var apagado = Color("Linea");

			foreach (var casillero in casilleros)
			{
				var rotulo = (string) casillero.Content;
				var encendido = rotulo == rotuloEncendido;
				var donde = $"[{rotulo}] con el alumno en {estado}";

				casillero.Tag.ShouldBe(encendido, $"el Tag de {donde}");

				// El apagado deja el fondo transparente; el encendido lo tiñe.
				((SolidColorBrush) casillero.Background).Color.A
					.ShouldBe(encendido ? (byte) 255 : (byte) 0, $"el alfa del fondo de {donde}");

				// La regla: hilo de Linea en los tres apagados, color pleno en el encendido, que es
				// lo que hace que el marcado se lea también sin distinguir colores.
				((SolidColorBrush) casillero.BorderBrush).Color
					.ShouldBe(encendido ? encendidoEsperado : apagado, $"la regla de {donde}");

				((SolidColorBrush) casillero.Foreground).Color
					.ShouldBe(encendido ? encendidoEsperado : Color("TintaTenue"), $"la letra de {donde}");
			}
		});
	}

	#region Infraestructura
	/// <summary>
	/// Monta la vista con una planilla abierta de treinta alumnos y la mide como la mide WPF.
	/// </summary>
	private static GestionAsistenciasView Dibujar(double altoDeVentana, out ScrollViewer marco, string estadoDelPrimero = "Presente")
	{
		_ = Application.Current ?? new Application { Resources = ArmarRecursos() };

		var contexto = new PlanillaFalsa();

		for (var i = 0; i < 30; i++)
		{
			contexto.Registros.Add(new FilaFalsa { Estado = i == 0 ? estadoDelPrimero : "Presente" });
		}

		var vista = new GestionAsistenciasView { DataContext = contexto };

		// El área de contenido de MainWindow: la ventana menos su franja de título y menos el rail.
		var disponible = new Size(VENTANA_POR_OMISION_ANCHO - ANCHO_DEL_RAIL, altoDeVentana - FRANJA_DE_TITULO);

		vista.Measure(disponible);
		vista.Arrange(new Rect(new Point(0, 0), disponible));
		vista.UpdateLayout();

		marco = (ScrollViewer) vista.FindName("MarcoPlanilla");

		return vista;
	}

	/// <summary>
	/// El color de un pincel del tema, por su clave. Así la prueba afirma contra el tema y no
	/// contra literales: cambiar el verde institucional no la vuelve roja.
	/// </summary>
	private static System.Windows.Media.Color Color(string clave) =>
		((SolidColorBrush) Application.Current.Resources[clave]).Color;

	/// <summary>Los cuatro casilleros de la primera fila realizada de la grilla.</summary>
	private static List<Button> Casilleros(DependencyObject raiz)
	{
		var encontrados = new List<Button>();

		void Recorrer(DependencyObject nodo)
		{
			for (var i = 0; i < VisualTreeHelper.GetChildrenCount(nodo) && encontrados.Count < 4; i++)
			{
				var hijo = VisualTreeHelper.GetChild(nodo, i);

				if (hijo is Button casillero && casillero.Content is string rotulo
				 && rotulo is "PRESENTE" or "AUSENCIA" or "INASIST." or "TARDANZA")
				{
					encontrados.Add(casillero);
					continue;
				}

				Recorrer(hijo);
			}
		}

		Recorrer(raiz);

		return encontrados;
	}

	/// <summary>Diccionarios del tema EN EL MISMO ORDEN que <c>App.xaml</c>, igual que en TemaTests.</summary>
	private static ResourceDictionary ArmarRecursos()
	{
		var rutas = new[]
		{
			"Theme/EDUSIS_Converters.xaml", "Theme/EDUSIS_Brushes.xaml", "Theme/EDUSIS_Typography.xaml",
			"Theme/EDUSIS_Surfaces.xaml", "Theme/EDUSIS_Feedback.xaml", "Theme/EDUSIS_ButtonStyle.xaml",
			"Theme/EDUSIS_Labels.xaml", "Theme/EDUSIS_TextBlock.xaml", "Theme/EDUSIS_TextBox.xaml",
			"Theme/EDUSIS_ComboBox.xaml", "Theme/EDUSIS_DatePicker.xaml", "Theme/EDUSIS_DataGrid.xaml",
			"Theme/EDUSIS_ListView.xaml", "Theme/EDUSIS_Icons.xaml", "Theme/EDUSIS_Forms.xaml",
			"Shared/DataTemplate.xaml",
		};

		var raiz = new ResourceDictionary();

		foreach (var ruta in rutas)
		{
			raiz.MergedDictionaries.Add(new ResourceDictionary
			{
				Source = new Uri($"pack://application:,,,/WPF_Desktop;component/{ ruta }", UriKind.Absolute)
			});
		}

		return raiz;
	}

	/// <summary>WPF admite UNA Application por dominio y sus objetos tienen afinidad de hilo.</summary>
	private static void EjecutarEnSTA(Action accion)
	{
		Exception? falla = null;

		var hilo = new Thread(() =>
		{
			try
			{
				accion();
			}
			catch (Exception ex)
			{
				falla = ex;
			}
		});

		hilo.SetApartmentState(ApartmentState.STA);
		hilo.Start();
		hilo.Join();

		if (falla is not null)
		{
			throw falla;
		}
	}
	#endregion

	#region Dobles del DataContext
	/// <summary>
	/// Lo que la vista lee del ViewModel, con una planilla abierta. Los comandos quedan en
	/// <c>null</c> a propósito: un comando sin enlazar deshabilita su botón y no cambia el alto.
	/// </summary>
	private class PlanillaFalsa
	{
		public string Vista { get; set; } = "Planilla";
		public bool HayPlanilla { get; set; } = true;
		public bool PlanillaAbierta { get; set; } = true;
		public bool Cerrada { get; set; }
		public bool TienePreceptor { get; set; } = true;
		public bool MostrarAvisoSinPlanilla { get; set; }
		public bool EstaCargando { get; set; }
		public string Curso { get; set; } = "Segundo";
		public string NivelEducativo { get; set; } = "SECUNDARIA";
		public string Division { get; set; } = "A";
		public string Preceptor { get; set; } = "Pérez, Marta";
		public DateTime Fecha { get; set; } = new DateTime(2026, 11, 7);
		public int Presentes { get; set; } = 28;
		public int Ausencias { get; set; } = 1;
		public int Inasistencias { get; set; }
		public int Tardanzas { get; set; } = 1;
		public int MinutosTardanza { get; set; } = 5;
		public string Observacion { get; set; } = string.Empty;
		public FilaFalsa? Registro { get; set; }
		public ObservableCollection<FilaFalsa> Registros { get; } = new();
		public ObservableCollection<FilaFalsa> CursantesSinRegistro { get; } = new();
		public ObservableCollection<object> Planillas { get; } = new();
		public ObservableCollection<object> ResumenCursantes { get; } = new();
		public IEnumerable<object> Ruta { get; set; } = Array.Empty<object>();
	}

	private class FilaFalsa
	{
		public string NombreCompleto { get; set; } = "Benítez, Lucía";
		public string Estado { get; set; } = "Presente";
		public string Minutos { get; set; } = string.Empty;
		public string Observacion { get; set; } = string.Empty;
		public bool EsFalta { get; set; }
	}
	#endregion
}
