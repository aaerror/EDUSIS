using Xunit;

namespace WPF_Desktop.UnitTests;

/// <summary>
/// Las clases de prueba que levantan una <c>Application</c> de WPF corren de a una.
/// </summary>
/// <remarks>
/// <para>
/// WPF ADMITE UNA SOLA <c>Application</c> POR AppDomain, y xUnit corre las clases de prueba en
/// PARALELO. Mientras <see cref="TemaTests"/> era la única que la construía no se notaba: sus
/// <c>[Fact]</c> comparten clase, así que ya corrían serializados y el
/// <c>Application.Current ?? new Application(…)</c> de su <c>ArmarAplicacion</c> no competía con
/// nadie. La segunda clase que hizo lo mismo —<see cref="AltoDeLaPlanillaTests"/>— destapó la
/// carrera.
/// </para>
/// <para>
/// Falla de dos maneras y las dos se ven una vez cada tantas corridas, que es lo peor que puede
/// pasarle a una suite: <c>InvalidOperationException</c> "no se puede crear más de una instancia
/// System.Windows.Application" si las dos llegan al <c>new</c>, o un
/// <c>XamlParseException</c> "no se puede encontrar el recurso con el nombre 'Campo.Izquierda'" si
/// una empieza a construir vistas contra el diccionario que la otra todavía está mezclando. El
/// síntoma cae en cualquiera de las dos clases según quién llegue primero, así que la primera
/// corrida acusó a <c>TemaTests</c>, que no había cambiado.
/// </para>
/// <para>
/// Una colección con nombre serializa a sus miembros. Toda clase nueva que construya vistas o
/// toque el tema tiene que entrar acá.
/// </para>
/// </remarks>
[CollectionDefinition(NOMBRE)]
public class ColeccionDeWPF
{
	public const string NOMBRE = "Aplicación de WPF";
}
