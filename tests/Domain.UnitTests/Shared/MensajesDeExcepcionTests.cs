using Domain.Shared;
using EDUSIS.TestSupport;
using Shouldly;
using System.Reflection;
using Xunit;

namespace Domain.UnitTests.Shared;

/// <summary>
/// Guarda de convención sobre los mensajes de las excepciones de <c>Domain</c>: el constructor sin
/// parámetros de cada una tiene que producir su propia constante <c>ERROR</c>, no el texto por defecto
/// de .NET.
/// </summary>
/// <remarks>
/// <para>
/// Nace de un defecto medido el 2026-11-14: <strong>17 de las 75 excepciones de <c>Domain</c> declaraban
/// su constructor sin parámetros como <c>: base()</c> en vez de <c>: base(ERROR)</c></strong>, y 22 sitios
/// de <c>throw</c> vivos las lanzaban así. Como <c>CLAUDE.md</c> fija que las excepciones suben hasta el
/// ViewModel sin ser tragadas y la UI muestra <c>ex.Message</c>, lo que leía el usuario en esos 22 caminos
/// era el texto por defecto de .NET <strong>en inglés</strong> —"Exception of type '…' was thrown."— en una
/// aplicación cuyo código y cuyos mensajes están enteramente en español.
/// </para>
/// <para>
/// <strong>Por qué hace falta una guarda y no alcanzaron las pruebas que ya había.</strong> Las 1613 pruebas
/// del proyecto estaban en verde con el defecto presente y siguieron en verde después de corregirlo:
/// ninguna afirma el mensaje de una excepción, sólo su tipo. Un <c>ERROR</c> que nadie pasa a <c>base</c>
/// es una constante muerta que el compilador no objeta —está usada por los otros dos constructores— y que
/// ninguna aserción de tipo puede notar. Entre los 22 sitios estaba <c>FechasInconsistentesException</c>,
/// que <c>RangoFechas</c> lanza cuando el inicio de un período es posterior a su fin: una de las
/// validaciones que más fácil dispara un usuario cargando fechas.
/// </para>
/// <para>
/// La prueba recorre el assembly por reflexión en lugar de enumerar las 75, para que una excepción nueva
/// quede cubierta sin que nadie se acuerde de agregarla acá.
/// </para>
/// </remarks>
public class MensajesDeExcepcionTests
{
	/// <summary>
	/// Las excepciones de <c>Domain</c> que se pueden construir sin argumentos. Se excluyen las que no
	/// declaran constructor sin parámetros, porque su mensaje no puede salir mal por esta vía.
	/// </summary>
	private static IEnumerable<Type> ExcepcionesConstruiblesSinArgumentos()
	{
		return typeof(Entity).Assembly
							 .GetTypes()
							 .Where(t => typeof(Exception).IsAssignableFrom(t))
							 .Where(t => !t.IsAbstract)
							 .Where(t => t.GetConstructor(Type.EmptyTypes) is not null);
	}

	[Fact]
	[Trait("Categoria", Categorias.Unidad)]
	public void Toda_excepcion_de_dominio_trae_su_propio_mensaje_sin_argumentos()
	{
		var excepciones = ExcepcionesConstruiblesSinArgumentos().ToList();

		// Si esto da cero, la reflexión dejó de encontrar los tipos y la prueba pasaría vacía.
		excepciones.Count.ShouldBeGreaterThan(50);

		var mudas = excepciones
			.Select(t => (Tipo: t, Mensaje: ((Exception)Activator.CreateInstance(t)!).Message))
			.Where(x => x.Mensaje.Contains("Exception of type") || x.Mensaje.Contains("was thrown"))
			.Select(x => x.Tipo.Name)
			.OrderBy(nombre => nombre)
			.ToList();

		mudas.ShouldBeEmpty("el constructor sin parámetros de estas excepciones no pasa su constante ERROR "
							+ "a base(), así que el usuario lee el texto por defecto de .NET en inglés en vez "
							+ "del mensaje en español de la regla que se violó: " + string.Join(", ", mudas));
	}

	/// <summary>
	/// La contracara: el mensaje además tiene que estar en español. Una excepción que pase a <c>base</c> una
	/// cadena vacía satisfaría la prueba de arriba y seguiría sin decirle nada al usuario.
	/// </summary>
	[Fact]
	[Trait("Categoria", Categorias.Unidad)]
	public void Ninguna_excepcion_de_dominio_tiene_un_mensaje_vacio()
	{
		var vacias = ExcepcionesConstruiblesSinArgumentos()
			.Select(t => (Tipo: t, Mensaje: ((Exception)Activator.CreateInstance(t)!).Message))
			.Where(x => string.IsNullOrWhiteSpace(x.Mensaje))
			.Select(x => x.Tipo.Name)
			.OrderBy(nombre => nombre)
			.ToList();

		vacias.ShouldBeEmpty("estas excepciones construyen un mensaje vacío: " + string.Join(", ", vacias));
	}
}
