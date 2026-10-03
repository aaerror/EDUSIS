// El SDK de WPF excluye System.IO de los implicit usings, a diferencia del SDK normal: hay que importarlo a mano.
using System.IO;

namespace WPF_Desktop.UnitTests.Dobles;

/// <summary>
/// Localiza archivos fuente de <c>WPF_Desktop</c> desde el directorio de salida de la prueba, subiendo hasta la carpeta
/// que contiene <c>EDUSIS.sln</c>. Lo usan las guardas estaticas (lectura de XAML y de C#) que no se pueden expresar
/// como prueba de comportamiento.
/// </summary>
internal static class RaizDelRepositorio
{
	public static string Resolver(string rutaRelativa)
	{
		var directorio = new DirectoryInfo(AppContext.BaseDirectory);

		while (directorio is not null && !File.Exists(Path.Combine(directorio.FullName, "EDUSIS.sln")))
		{
			directorio = directorio.Parent;
		}

		if (directorio is null)
		{
			throw new FileNotFoundException("No se encontro EDUSIS.sln subiendo desde " + AppContext.BaseDirectory);
		}

		return Path.Combine(directorio.FullName, rutaRelativa.Replace('/', Path.DirectorySeparatorChar));
	}

	public static string Leer(string rutaRelativa)
	{
		return File.ReadAllText(Resolver(rutaRelativa));
	}
}
