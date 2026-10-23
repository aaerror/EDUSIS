namespace Infrastructure.Documentos.Content;

/// <summary>Orientación de la hoja.</summary>
/// <remarks>
/// Los reportes tabulares salen apaisados: diez columnas en A4 vertical dejan celdas de dos
/// centímetros en las que ningún apellido entra.
/// </remarks>
public enum Orientacion
{
	Vertical,
	Apaisada
}

/// <summary>
/// Documento listo para renderizar: su título, su orientación y la secuencia de bloques del
/// cuerpo.
/// </summary>
/// <remarks>
/// No conoce iText más allá de los bloques que contiene, y no sabe en qué se va a imprimir:
/// quién lo recorre es <see cref="PortableDocumentFormat"/>.
/// </remarks>
public sealed class Documento
{
	private readonly List<IContenido> _contenidos = new();

	/// <summary>Título del documento; va en el encabezado de cada página y en los metadatos.</summary>
	public string Titulo { get; }

	/// <summary>Línea de contexto bajo el título. Opcional.</summary>
	public string? Subtitulo { get; }

	public Orientacion Orientacion { get; }

	public IReadOnlyCollection<IContenido> Contenidos => _contenidos.AsReadOnly();


	private Documento(string titulo, string? subtitulo, Orientacion orientacion)
	{
		if (string.IsNullOrWhiteSpace(titulo))
		{
			throw new ArgumentException("Debe especificar el título del documento.", nameof(titulo));
		}

		Titulo = titulo;
		Subtitulo = subtitulo;
		Orientacion = orientacion;
	}

	public static Documento Crear(string titulo, string? subtitulo = null, Orientacion orientacion = Orientacion.Vertical) =>
		new(titulo, subtitulo, orientacion);

	public Documento Agregar(IContenido contenido)
	{
		ArgumentNullException.ThrowIfNull(contenido);

		_contenidos.Add(contenido);

		return this;
	}

	public Documento Agregar(IEnumerable<IContenido> contenidos)
	{
		foreach (var contenido in contenidos)
		{
			Agregar(contenido);
		}

		return this;
	}
}
