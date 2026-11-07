namespace WPF_Desktop.Store;

/// <summary>
/// PARA QUÉ se abrió el buscador de docentes. El modal es uno solo y lo comparten dos recorridos
/// —asignar el preceptor de una división y designar a alguien en una cátedra—, pero la decisión que
/// se toma adentro no es la misma y hasta acá el buscador no lo decía: se titulaba "Buscar docente"
/// y nunca nombraba el destino.
/// </summary>
/// <remarks>
/// El destino es el único dato de la decisión que NO está en la grilla de resultados, así que es el
/// que va arriba de todo. Un buscador que no dice a qué división le está buscando preceptor obliga
/// a elegir de memoria.
///
/// Mismo mecanismo que <see cref="DivisionSeleccionada"/>: es un parámetro de navegación y se
/// escribe ANTES de <c>Navigate()</c>, porque <c>ModalNavigationService</c> construye un
/// <c>BuscarViewModel</c> nuevo en cada apertura y el contexto se lee en su constructor.
///
/// <paramref name="Verbo"/> es la etiqueta del botón primario. No es decoración: es lo que
/// convierte al modal en el final del recorrido o en un paso intermedio. "Asignar preceptor" cierra
/// la transacción; "Elegir docente" la empieza.
/// </remarks>
internal sealed record BuscarContexto(string Titulo, string Detalle, string Verbo)
{
	/// <summary>
	/// El contexto de quien no declara ninguno. Habla de la herramienta porque no sabe del destino.
	/// </summary>
	public static BuscarContexto Generico { get; } =
		new(Titulo: "Buscar docente",
			Detalle: "Por apellido y nombre. Sin texto, lista a todos los docentes en funciones.",
			Verbo: "Elegir docente");
}

internal class BuscarContextoStore : IEstadoDeSesion
{
	private BuscarContexto _contexto = BuscarContexto.Generico;

	public void Declarar(BuscarContexto contexto) =>
		_contexto = contexto ?? BuscarContexto.Generico;

	/// <summary>
	/// Devuelve el contexto declarado y repone el genérico.
	/// </summary>
	/// <remarks>
	/// SE VACÍA AL LEERSE, a propósito. El store es Singleton y el contexto vale para UNA apertura:
	/// si quedara pegado, el recorrido que olvidara declarar el suyo abriría el modal con el título
	/// del recorrido anterior —"Preceptor de primer año A" encima de una designación de cátedra—, y
	/// un encabezado que miente sobre el destino es peor que uno que no lo nombra.
	/// </remarks>
	public BuscarContexto Tomar()
	{
		var contexto = _contexto;
		_contexto = BuscarContexto.Generico;

		return contexto;
	}

	/// <summary>
	/// Defensa en profundidad, no corrección: <see cref="Tomar"/> ya repone el genérico.
	/// </summary>
	/// <remarks>
	/// Lo que queda sin limpiar acá es un contexto DECLARADO y nunca tomado —alguien llamó a
	/// <see cref="Declarar"/> y la apertura del modal no llegó a ocurrir—, y ese contexto nombra una
	/// división o una cátedra concreta. Es la única manera de que el encabezado del buscador le
	/// mienta al usuario que entra después.
	/// </remarks>
	public void Limpiar() =>
		_contexto = BuscarContexto.Generico;
}
