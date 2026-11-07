using Shouldly;
using System.Collections;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows;
using Xunit;

namespace WPF_Desktop.UnitTests;

/// <summary>
/// Humo del sistema visual: carga los diccionarios del tema y construye todas las vistas.
/// </summary>
/// <remarks>
/// <para>
/// Esto cubre un agujero real de WPF: un <c>StaticResource</c> con una clave que no existe
/// NO es error de compilación —el compilador de XAML genera el BAML sin resolver claves— y
/// revienta recién cuando se construye el árbol visual de esa pantalla. Hasta que alguien
/// abre esa pantalla exacta, el error no aparece.
/// </para>
/// <para>
/// Es exactamente lo que pasó con <c>LoginUsuarioView</c>, que pedía un estilo llamado
/// <c>TextBlock</c> inexistente: compilaba perfecto y tiraba abajo la aplicación al abrir el
/// acceso.
/// </para>
/// <para>
/// Todo corre en UN solo hilo STA y con UNA sola <see cref="Application"/>: WPF admite una
/// instancia por dominio y sus objetos tienen afinidad de hilo.
/// </para>
/// </remarks>
[Collection(ColeccionDeWPF.NOMBRE)]
public class TemaTests
{
	/// <summary>
	/// Diccionarios del tema EN EL MISMO ORDEN que <c>App.xaml</c>. El orden es parte del
	/// contrato: un <c>StaticResource</c> sólo ve claves ya cargadas.
	/// </summary>
	private static readonly string[] Diccionarios =
	{
		"Theme/EDUSIS_Converters.xaml",
		"Theme/EDUSIS_Brushes.xaml",
		"Theme/EDUSIS_Typography.xaml",
		"Theme/EDUSIS_Surfaces.xaml",
		"Theme/EDUSIS_Feedback.xaml",
		"Theme/EDUSIS_ButtonStyle.xaml",
		"Theme/EDUSIS_Labels.xaml",
		"Theme/EDUSIS_TextBlock.xaml",
		"Theme/EDUSIS_TextBox.xaml",
		"Theme/EDUSIS_ComboBox.xaml",
		"Theme/EDUSIS_DatePicker.xaml",
		"Theme/EDUSIS_DataGrid.xaml",
		"Theme/EDUSIS_ListView.xaml",
		"Theme/EDUSIS_Icons.xaml",
		"Theme/EDUSIS_Forms.xaml",
		"Shared/DataTemplate.xaml",
	};

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_tema_carga_y_todas_las_vistas_se_construyen()
	{
		EjecutarEnSTA(() =>
		{
			ArmarAplicacion();

			var fallas = new StringBuilder();

			foreach (var tipo in VistasYVentanas())
			{
				try
				{
					Activator.CreateInstance(tipo, nonPublic: true);
				}
				catch (Exception ex)
				{
					// La excepción real viaja adentro de la TargetInvocationException.
					var causa = ex.GetBaseException();
					fallas.AppendLine($"{ tipo.FullName }: { causa.Message }");
				}
			}

			fallas.ToString().ShouldBeEmpty();
		});
	}

	/// <summary>
	/// Ningún diccionario de <c>Theme/</c> puede quedar afuera de la lista de arriba.
	/// </summary>
	/// <remarks>
	/// Sin esta comprobación, agregar un diccionario nuevo a <c>App.xaml</c> y olvidarlo acá
	/// dejaría la prueba anterior verde contra un tema incompleto, que es la peor clase de
	/// prueba: la que informa que todo anda porque no miró.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void La_prueba_del_tema_cubre_todos_los_diccionarios_del_proyecto()
	{
		var enDisco = RecursosDelEnsamblado()
			.Where(nombre => nombre.StartsWith("theme/", StringComparison.Ordinal)
						  && nombre.EndsWith(".baml", StringComparison.Ordinal))
			.Select(nombre => nombre.Replace(".baml", ".xaml", StringComparison.Ordinal))
			.OrderBy(nombre => nombre)
			.ToList();

		var cubiertos = Diccionarios
			.Select(ruta => ruta.ToLowerInvariant())
			.ToHashSet();

		enDisco.ShouldNotBeEmpty();
		enDisco.Where(nombre => !cubiertos.Contains(nombre)).ShouldBeEmpty();
	}

	/// <summary>
	/// El campo con error muestra el motivo DEBAJO del campo, y no sólo en el ToolTip.
	/// </summary>
	/// <remarks>
	/// <para>
	/// La plantilla <c>Campo</c> dibuja esa fila con un <c>DataTrigger</c> sobre
	/// <c>Content.(Validation.HasError)</c>, que es un camino de enlace encadenado hacia una
	/// propiedad adjunta: si estuviera mal escrito, WPF no falla ni avisa —se traga el enlace y
	/// no dibuja nada—. Construir la vista no alcanza para detectarlo, porque el error de
	/// enlace es silencioso; hace falta provocar un error de validación real y mirar el
	/// resultado.
	/// </para>
	/// <para>
	/// Importa porque es el único canal del mensaje para quien no usa mouse: un ToolTip exige
	/// apuntar, y sin esta fila lo único que queda es un borde rojo.
	/// </para>
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_campo_con_error_muestra_el_motivo_debajo_del_campo()
	{
		EjecutarEnSTA(() =>
		{
			var recursos = ArmarAplicacion().Resources;

			recursos["Txt.Error.Campo"].ShouldNotBeNull("el estilo del motivo tiene que estar en los recursos");
			recursos["Campo"].ShouldNotBeNull();

			// Un correo sin arroba: ContactoViewModel ya lo rechaza con [EmailAddress].
			// global:: porque el namespace de las pruebas también tiene un ViewModels.
			var contacto = new global::WPF_Desktop.ViewModels.Shared.ContactoViewModel(null)
			{
				Email = "sin-arroba"
			};

			var caja = new TextBox
			{
				Style = (Style) recursos["Txb.Campo"]
			};

			BindingOperations.SetBinding(caja, TextBox.TextProperty, new Binding("Email")
			{
				Source = contacto,
				Mode = BindingMode.TwoWay,
				UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
				ValidatesOnNotifyDataErrors = true
			});

			var campo = new HeaderedContentControl
			{
				Header = "Correo electrónico",
				Style = (Style) recursos["Campo"],
				Content = caja
			};

			campo.Measure(new Size(400, 400));
			campo.Arrange(new Rect(0, 0, 400, 400));

			Validation.GetHasError(caja)
					  .ShouldBeTrue("el enlace tendría que haber marcado el error del correo");

			var motivo = campo.Template.FindName("Error", campo) as TextBlock;

			motivo.ShouldNotBeNull("la plantilla Campo tiene que traer la fila del motivo");

			var diagnostico = $"texto='{ motivo!.Text }', visibilidad={ motivo.Visibility }";

			motivo.Text.ShouldNotBeNullOrWhiteSpace(diagnostico);
			motivo.Visibility.ShouldBe(Visibility.Visible, diagnostico);
		});
	}

	#region Infraestructura
	/// <summary>
	/// WPF admite UNA <see cref="Application"/> por dominio, y estas pruebas comparten proceso.
	/// </summary>
	private static Application ArmarAplicacion() =>
		Application.Current ?? new Application
		{
			Resources = ArmarRecursos()
		};

	private static ResourceDictionary ArmarRecursos()
	{
		var raiz = new ResourceDictionary();

		foreach (var ruta in Diccionarios)
		{
			raiz.MergedDictionaries.Add(new ResourceDictionary
			{
				Source = new Uri($"pack://application:,,,/WPF_Desktop;component/{ ruta }", UriKind.Absolute)
			});
		}

		return raiz;
	}

	private static IEnumerable<Type> VistasYVentanas() =>
		typeof(App).Assembly
				   .GetTypes()
				   .Where(tipo => !tipo.IsAbstract
							   && !tipo.IsGenericTypeDefinition
							   && (tipo.IsSubclassOf(typeof(UserControl)) || tipo.IsSubclassOf(typeof(Window)))
							   && tipo.GetConstructor(Type.EmptyTypes) is not null)
				   .OrderBy(tipo => tipo.FullName);

	private static IEnumerable<string> RecursosDelEnsamblado()
	{
		var ensamblado = typeof(App).Assembly;

		using var flujo = ensamblado.GetManifestResourceStream($"{ ensamblado.GetName().Name }.g.resources");
		if (flujo is null)
		{
			return Enumerable.Empty<string>();
		}

		using var lector = new ResourceReader(flujo);

		return lector.Cast<DictionaryEntry>()
					 .Select(entrada => (string) entrada.Key)
					 .ToList();
	}

	/// <summary>
	/// xUnit corre en hilos MTA; WPF exige STA. Las excepciones del hilo se vuelven a lanzar
	/// acá para que la prueba falle con la causa real y no por tiempo de espera.
	/// </summary>
	private static void EjecutarEnSTA(Action accion)
	{
		Exception? error = null;

		var hilo = new Thread(() =>
		{
			try
			{
				accion();
			}
			catch (Exception ex)
			{
				error = ex;
			}
		});

		hilo.SetApartmentState(ApartmentState.STA);
		hilo.Start();
		hilo.Join();

		if (error is not null)
		{
			throw new Xunit.Sdk.XunitException(error.ToString());
		}
	}
	#endregion
}
