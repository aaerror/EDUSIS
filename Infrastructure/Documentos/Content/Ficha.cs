using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace Infrastructure.Documentos.Content;

/// <summary>
/// Bloque de pares etiqueta/valor, para los datos que se consultan de un vistazo —documento,
/// legajo, curso— en lugar de leerse dentro de un párrafo.
/// </summary>
/// <remarks>
/// Se renderiza como tabla sin bordes y no como texto corrido porque quien recibe el documento
/// busca un dato puntual: alinear las etiquetas en una columna lo hace localizable.
/// </remarks>
public sealed class Ficha : IContenido
{
	private const float TAMANO = 10f;

	private readonly List<(string Etiqueta, string Valor)> _campos = new();


	private Ficha() { }

	public static Ficha Crear() =>
		new();

	/// <summary>Agrega el campo. Los valores vacíos se descartan: una etiqueta sin dato no informa.</summary>
	public Ficha Con(string etiqueta, string? valor)
	{
		if (!string.IsNullOrWhiteSpace(valor))
		{
			_campos.Add((etiqueta, valor.Trim()));
		}

		return this;
	}

	public IBlockElement CrearContenido(Fuentes fuentes)
	{
		var tabla = new Table(UnitValue.CreatePercentArray(new[] { 24f, 76f }))
			.UseAllAvailableWidth()
			.SetMarginTop(4f)
			.SetMarginBottom(14f);

		foreach (var (etiqueta, valor) in _campos)
		{
			tabla.AddCell(Celda(etiqueta.ToUpperInvariant(), fuentes.Negrita));
			tabla.AddCell(Celda(valor, fuentes.Regular));
		}

		return tabla;
	}

	private static Cell Celda(string texto, iText.Kernel.Font.PdfFont fuente) =>
		new Cell()
			.Add(new Paragraph(texto)
				.SetFont(fuente)
				.SetFontSize(TAMANO)
				.SetMargin(0)
				.SetMultipliedLeading(1.2f))
			.SetBorder(Border.NO_BORDER)
			.SetPaddingBottom(4f)
			.SetPaddingTop(0)
			.SetVerticalAlignment(VerticalAlignment.TOP);
}
