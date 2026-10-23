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
/// Pie de cada página: establecimiento emisor a la izquierda, número de página a la derecha.
/// </summary>
/// <remarks>
/// Dice "Página N" y no "Página N de M" porque el total no se conoce al cerrar cada página:
/// saberlo exige una segunda pasada sobre el documento o un XObject de relleno diferido. Con el
/// número solo, una hoja suelta sigue siendo ubicable, que es para lo que está.
/// </remarks>
internal class FooterEventHandler : AbstractPdfDocumentEventHandler
{
	private const float TAMANO = 8.5f;
	private const float ALTO = 28f;

	private static readonly Color LINEA = new DeviceRgb(150, 150, 150);

	private readonly string _emisor;
	private readonly Fuentes _fuentes;


	public FooterEventHandler(string emisor, Fuentes fuentes)
	{
		_emisor = emisor;
		_fuentes = fuentes;
	}

	protected override void OnAcceptedEvent(AbstractPdfDocumentEvent @event)
	{
		var documentEvent = (PdfDocumentEvent) @event;
		var pdfDocument = documentEvent.GetDocument();
		var page = documentEvent.GetPage();

		var pageSize = page.GetPageSize();
		var pdfCanvas = new PdfCanvas(page.NewContentStreamBefore(), page.GetResources(), pdfDocument);

		var area = new Rectangle(pageSize.GetLeft() + 56f,
								 pageSize.GetBottom() + 30f,
								 pageSize.GetWidth() - 112f,
								 ALTO);

		using var canvas = new Canvas(pdfCanvas, area);
		canvas.Add(Pie(pdfDocument.GetPageNumber(page)));

		pdfCanvas.Release();
	}

	private Table Pie(int numeroPagina)
	{
		var tabla = new Table(UnitValue.CreatePercentArray(new[] { 70f, 30f }))
			.UseAllAvailableWidth()
			.SetBorderTop(new SolidBorder(LINEA, 0.75f));

		tabla.AddCell(Celda(_emisor, TextAlignment.LEFT));
		tabla.AddCell(Celda($"Página { numeroPagina }", TextAlignment.RIGHT));

		return tabla;
	}

	private Cell Celda(string texto, TextAlignment alineacion) =>
		new Cell()
			.Add(new Paragraph(texto)
				.SetFont(_fuentes.Regular)
				.SetFontSize(TAMANO)
				.SetMargin(0)
				.SetTextAlignment(alineacion))
			.SetBorder(Border.NO_BORDER)
			.SetPaddingTop(5f)
			.SetVerticalAlignment(VerticalAlignment.TOP);
}
