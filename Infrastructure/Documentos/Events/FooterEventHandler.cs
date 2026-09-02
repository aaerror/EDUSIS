using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Event;
using iText.Kernel.Pdf;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Font;
using iText.Layout.Properties;
using iText.Layout;

namespace Infrastructure.Documentos.Events;

internal class FooterEventHandler : AbstractPdfDocumentEventHandler
{
	protected override void OnAcceptedEvent(AbstractPdfDocumentEvent @event)
	{
		PdfDocumentEvent documentEvent = (PdfDocumentEvent) @event;
		PdfDocument pdfDocument = documentEvent.GetDocument();
		PdfPage page = documentEvent.GetPage();

		Rectangle pageSize = page.GetPageSize();
		PdfCanvas pdfCanvas = new PdfCanvas(page.NewContentStreamBefore(), page.GetResources(), pdfDocument);

		var footerArea = new Rectangle(60f, pageSize.GetBottom() + 40f, pageSize.GetWidth() - 120f, 40f);
		var canvas = new Canvas(pdfCanvas, footerArea);
		
		var fontProvider = new FontProvider();
		fontProvider.AddStandardPdfFonts();

		canvas.SetFontProvider(fontProvider);
		canvas.SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA));

		var footerContent = CreateFooterTable(pdfDocument.GetPageNumber(page));
		canvas.Add(footerContent);

/*
		pdfCanvas.SetStrokeColor(ColorConstants.GRAY)
			.SetLineWidth(1)
			.MoveTo(pageSize.GetWidth(), pageSize.GetBottom() + 30)
			.LineTo(pageSize.GetWidth(), pageSize.GetHeight() + 30)
			.Stroke();

		pdfCanvas.BeginText()
			.SetFontAndSize(Regular, 10)
			.MoveText(pageSize.GetWidth() / 2 - 10, pageSize.GetBottom() + 20)
			.ShowText($"Página {pageNumber}")
			.EndText();

		pdfCanvas.Release();*/
	}

	private Table CreateFooterTable(int pageNumber)
	{
		var columnWidth = UnitValue.CreatePercentArray(new float[] { 100f });
		var table = new Table(columnWidth)
			.SetBorderTop(new SolidBorder(ColorConstants.BLACK, 1f))
			.UseAllAvailableWidth();

		// Número Página
		var numberText = new Text(pageNumber.ToString())
			.SetFontFamily(StandardFonts.HELVETICA)
			.SetFontSize(10f);
		var content = new Paragraph(numberText);

		var cellStyle = new Style()
			.SetBorder(Border.NO_BORDER)
			.SetHorizontalAlignment(HorizontalAlignment.RIGHT)
			.SetVerticalAlignment(VerticalAlignment.MIDDLE)
			.SetTextAlignment(TextAlignment.RIGHT);


		// Column
		var column = new Cell()
			.Add(content)
			.AddStyle(cellStyle);
		table.AddCell(column);

		return table;
	}
}
