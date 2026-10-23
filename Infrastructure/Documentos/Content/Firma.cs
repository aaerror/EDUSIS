using iText.Kernel.Colors;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace Infrastructure.Documentos.Content;

/// <summary>
/// Pie de validación institucional: espacio en blanco para la firma de la autoridad competente y
/// para el sello del establecimiento.
/// </summary>
/// <remarks>
/// Va en blanco a propósito. El documento se firma y se sella sobre el impreso: imprimir un
/// nombre bajo la línea de firma afirmaría que esa persona lo firmó, que es justamente lo que la
/// firma tiene que acreditar.
/// </remarks>
public sealed class Firma : IContenido
{
	private const float TAMANO_LEYENDA = 9f;
	private const float ALTO_ESPACIO = 54f;

	private static readonly Color LINEA = new DeviceRgb(90, 90, 90);


	private Firma() { }

	public static Firma Crear() =>
		new();

	public IBlockElement CrearContenido(Fuentes fuentes)
	{
		var tabla = new Table(UnitValue.CreatePercentArray(new[] { 50f, 50f }))
			.UseAllAvailableWidth()
			.SetMarginTop(44f)
			// El pie de firma no se parte entre dos páginas: una línea de firma sin su leyenda, o
			// al revés, invalida el documento.
			.SetKeepTogether(true);

		tabla.AddCell(Espacio());
		tabla.AddCell(Espacio());

		tabla.AddCell(Leyenda("Firma de la autoridad competente", fuentes, conLineaSuperior: true));
		tabla.AddCell(Leyenda("Sello institucional", fuentes, conLineaSuperior: true));

		return tabla;
	}

	private static Cell Espacio() =>
		new Cell()
			.SetBorder(Border.NO_BORDER)
			.SetHeight(ALTO_ESPACIO);

	private static Cell Leyenda(string texto, Fuentes fuentes, bool conLineaSuperior)
	{
		var celda = new Cell()
			.Add(new Paragraph(texto)
				.SetFont(fuentes.Regular)
				.SetFontSize(TAMANO_LEYENDA)
				.SetMarginTop(4f)
				.SetMarginBottom(0)
				.SetTextAlignment(TextAlignment.CENTER))
			.SetBorder(Border.NO_BORDER)
			.SetPaddingLeft(18f)
			.SetPaddingRight(18f);

		if (conLineaSuperior)
		{
			celda.SetBorderTop(new SolidBorder(LINEA, 0.75f));
		}

		return celda;
	}
}
