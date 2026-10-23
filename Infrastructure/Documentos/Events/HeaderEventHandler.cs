using Infrastructure.Documentos.Content;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Event;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace Infrastructure.Documentos.Events;

/// <summary>
/// Encabezado de cada página: establecimiento a la izquierda, documento y fecha a la derecha.
/// </summary>
/// <remarks>
/// Es la línea de identificación que acompaña a cada hoja, no el bloque institucional completo:
/// ese va una sola vez, al principio del cuerpo. Repetir nombre, domicilio, CUE y teléfono en
/// cada página consumiría un cuarto de la hoja sin informar nada nuevo.
/// </remarks>
internal class HeaderEventHandler : AbstractPdfDocumentEventHandler
{
	private const float TAMANO = 8.5f;
	private const float ALTO = 34f;

	private static readonly Color LINEA = new DeviceRgb(150, 150, 150);

	private readonly string _institucion;
	private readonly string _titulo;
	private readonly Fuentes _fuentes;


	public HeaderEventHandler(string institucion, string titulo, Fuentes fuentes)
	{
		_institucion = institucion;
		_titulo = titulo;
		_fuentes = fuentes;
	}

	protected override void OnAcceptedEvent(AbstractPdfDocumentEvent @event)
	{
		var documentEvent = (PdfDocumentEvent) @event;
		var pdfDocument = documentEvent.GetDocument();
		var page = documentEvent.GetPage();

		var pageSize = page.GetPageSize();
		var pdfCanvas = new PdfCanvas(page.NewContentStreamBefore(), page.GetResources(), pdfDocument);

		// El área arranca en el borde superior menos el alto del encabezado: así la línea
		// divisoria queda por encima del margen de contenido y no sobre el texto del cuerpo.
		var area = new Rectangle(pageSize.GetLeft() + 56f,
								 pageSize.GetTop() - 52f - ALTO,
								 pageSize.GetWidth() - 112f,
								 ALTO);

		using var canvas = new Canvas(pdfCanvas, area);
		canvas.Add(Encabezado());

		pdfCanvas.Release();
	}

	private Table Encabezado()
	{
		var tabla = new Table(UnitValue.CreatePercentArray(new[] { 55f, 45f }))
			.UseAllAvailableWidth()
			.SetBorderBottom(new SolidBorder(LINEA, 0.75f));

		tabla.AddCell(Celda(_institucion, _fuentes.Negrita, TextAlignment.LEFT));
		tabla.AddCell(Celda($"{ _titulo } · {DateTime.Today:dd/MM/yyyy}", _fuentes.Italica, TextAlignment.RIGHT));

		return tabla;
	}

	private static Cell Celda(string texto, iText.Kernel.Font.PdfFont fuente, TextAlignment alineacion) =>
		new Cell()
			.Add(new Paragraph(texto)
				.SetFont(fuente)
				.SetFontSize(TAMANO)
				.SetMargin(0)
				.SetTextAlignment(alineacion))
			.SetBorder(Border.NO_BORDER)
			.SetPaddingBottom(5f)
			.SetVerticalAlignment(VerticalAlignment.BOTTOM);
}
