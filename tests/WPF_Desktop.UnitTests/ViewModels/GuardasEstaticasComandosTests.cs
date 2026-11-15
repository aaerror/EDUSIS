using Shouldly;
// El SDK de WPF excluye System.IO de los implicit usings, a diferencia del SDK normal: hay que importarlo a mano.
using System.IO;
using System.Text.RegularExpressions;
using WPF_Desktop.UnitTests.Dobles;
using Xunit;

namespace WPF_Desktop.UnitTests.ViewModels;

/// <summary>
/// Guarda estática sobre la notificación de los comandos: ningún <c>[NotifyPropertyChangedFor]</c> puede
/// apuntar a un comando, porque ese atributo no refresca el <c>CanExecute</c> y el defecto es invisible
/// para el compilador.
/// </summary>
/// <remarks>
/// <para>
/// Nace de un defecto hallado en <c>GestionCursosViewModel</c>: las dos propiedades
/// de las que dependía el borrado notificaban con <c>[NotifyPropertyChangedFor(nameof(EliminarCommandAsync))]</c>,
/// que levanta <c>PropertyChanged</c> para la <strong>propiedad</strong> del comando. El binding relee la
/// propiedad, obtiene la misma instancia de <c>ICommand</c>, la DP no cambia y <c>ButtonBase</c> no vuelve a
/// evaluar nada; los comandos del toolkit tampoco se cuelgan de <c>CommandManager.RequerySuggested</c>, así que
/// <c>CanExecuteChanged</c> no se levanta nunca. El botón <c>Eliminar</c> quedaba gris para siempre y eliminar
/// un curso era inalcanzable desde la UI.
/// </para>
/// <para>
/// <strong>Por qué hace falta una guarda estática y no alcanza una prueba por ViewModel.</strong> El atributo
/// equivocado <strong>compila sin un solo warning</strong>: el generador sólo exige que el objetivo sea una
/// propiedad existente, y la propiedad del comando existe. No hay error, no hay advertencia, y el síntoma no
/// aparece hasta que alguien abre la pantalla y no puede clickear el botón. Una prueba por ViewModel cubre el
/// ViewModel que alguien se acordó de cubrir; esto cubre los 47 y los que vengan. El atributo correcto es
/// <c>[NotifyCanExecuteChangedFor]</c>, que genera la llamada a <c>NotifyCanExecuteChanged()</c> en el setter,
/// y es lo que ya usaban los otros veinte ViewModels del proyecto: el archivo roto era el único outlier.
/// </para>
/// </remarks>
public class GuardasEstaticasComandosTests
{
	/// <summary>
	/// Un objetivo de <c>[NotifyPropertyChangedFor]</c> es un comando si su nombre contiene <c>Command</c>.
	/// Alcanza con eso porque la convención del repo nombra los tres: <c>CargarCursosCommandAsync</c>,
	/// <c>EliminarCommandAsync</c>, <c>NavigationCommand</c>.
	/// </summary>
	private static readonly Regex NotificaAUnComando =
		new(@"\[NotifyPropertyChangedFor\(nameof\((?<objetivo>[^)]*Command[^)]*)\)\)\]", RegexOptions.Compiled);

	private static IEnumerable<string> ViewModels()
	{
		var raiz = RaizDelRepositorio.Resolver("WPF_Desktop");
		var separador = Path.DirectorySeparatorChar;

		return Directory.EnumerateFiles(raiz, "*.cs", SearchOption.AllDirectories)
						.Where(f => !f.Contains($"{separador}obj{separador}") && !f.Contains($"{separador}bin{separador}"));
	}

	/// <summary>Devuelve el código sin los bloques <c>/* */</c> ni los comentarios de línea.</summary>
	private static string CodigoVivo(string rutaAbsoluta)
	{
		var fuente = File.ReadAllText(rutaAbsoluta);
		fuente = Regex.Replace(fuente, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);

		return string.Join("\n", fuente.Split('\n').Where(l => !l.TrimStart().StartsWith("//")));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Ningun_NotifyPropertyChangedFor_apunta_a_un_comando()
	{
		var raiz = RaizDelRepositorio.Resolver("WPF_Desktop");

		var infracciones = ViewModels()
			.SelectMany(archivo => NotificaAUnComando.Matches(CodigoVivo(archivo))
												     .Select(m => $"{Path.GetRelativePath(raiz, archivo)} → {m.Groups["objetivo"].Value}"))
			.ToList();

		infracciones.ShouldBeEmpty("NotifyPropertyChangedFor no refresca el CanExecute de un comando: "
								   + "levanta PropertyChanged para la propiedad del comando, que devuelve la misma "
								   + "instancia de ICommand, así que la vista nunca vuelve a consultar CanExecute y el "
								   + "control queda deshabilitado. El atributo correcto es NotifyCanExecuteChangedFor. "
								   + "Infracciones: " + string.Join(", ", infracciones));
	}

	/// <summary>
	/// La contracara de la guarda de arriba: confirma que el atributo correcto <strong>se usa</strong>, para que
	/// "ninguna infracción" no se pueda satisfacer borrando toda notificación de comandos del proyecto.
	/// </summary>
	/// <remarks>
	/// Sin esta prueba la guarda anterior pasaría en un proyecto que no notifique nada, que es exactamente el
	/// estado roto que se quiere prevenir. El umbral es deliberadamente flojo —se afirma que hay un uso amplio y
	/// no un número exacto— porque el número exacto convierte cualquier refactor en un fallo.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_proyecto_usa_NotifyCanExecuteChangedFor_para_refrescar_comandos()
	{
		var archivosQueNotifican = ViewModels()
			.Count(archivo => CodigoVivo(archivo).Contains("NotifyCanExecuteChangedFor"));

		archivosQueNotifican.ShouldBeGreaterThan(10);
	}
}
