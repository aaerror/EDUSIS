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

internal class HeaderEventHandler : AbstractPdfDocumentEventHandler
{
	//TODO: https://www.youtube.com/watch?v=hC9qkuV7_0M&ab_channel=YourProgrammingCoachVideo
	private readonly string _subtitle;


	public HeaderEventHandler(string subtitle)
	{
		_subtitle = subtitle;
	}

	protected override void OnAcceptedEvent(AbstractPdfDocumentEvent @event)
	{
		PdfDocumentEvent documentEvent = (PdfDocumentEvent) @event;
		PdfDocument pdfDocument = documentEvent.GetDocument();
		PdfPage page = documentEvent.GetPage();

		Rectangle pageSize = page.GetPageSize();
		PdfCanvas pdfCanvas = new PdfCanvas(page.NewContentStreamBefore(), page.GetResources(), pdfDocument);

		var headerArea = new Rectangle(60f, pageSize.GetTop() - 80f, pageSize.GetRight() - 120f, 40f);
		var canvas = new Canvas(pdfCanvas, headerArea);
		var fontProvider = new FontProvider();
		fontProvider.AddStandardPdfFonts();
		canvas.SetFontProvider(fontProvider);
		canvas.SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA));

		var headerContent = CreateHeaderTable();
		canvas.Add(headerContent);
		canvas.Close();
/*
		pdfCanvas.BeginText()
			.SetFontAndSize(Negrita, 10)
			.MoveText(pageSize.GetWidth() * 0.2, pageSize.GetTop() - 20)
			.ShowText("EDUSIS")
			.EndText();

		pdfCanvas.BeginText()
			.SetFontAndSize(Italica, 10)
			.MoveText(pageSize.GetWidth() * 0.8, pageSize.GetTop() - 20)
			.ShowText($"{ DateTime.Today.ToString("D") }")
			.EndText();

		pdfCanvas.BeginText()
			.SetFontAndSize(Regular, 10)
			.MoveText(pageSize.GetWidth() * 0.2, pageSize.GetTop() - 30)
			.ShowText(_subtitle)
			.EndText();

		pdfCanvas.SetStrokeColor(ColorConstants.GRAY)
			.SetLineWidth(1)
			.MoveTo(pageSize.GetWidth(), pageSize.GetTop() - 40)
			.LineTo(pageSize.GetWidth(), pageSize.GetHeight() - 40)
			.Stroke();

		pdfCanvas.Release();*/
	}

	private Table CreateHeaderTable()
	{
		var columnWidth = UnitValue.CreatePercentArray(new float[] { 30f, 70f });
		var table = new Table(columnWidth)
			.SetBorderBottom(new SolidBorder(ColorConstants.BLACK, 1f))
			.UseAllAvailableWidth();

		// Title
		var titleText = new Text("EDUSIS")
			.SetFontFamily(StandardFonts.HELVETICA_BOLD)
			.SetFontSize(12f)
			.SetHorizontalAlignment(HorizontalAlignment.LEFT)
			.SetTextAlignment(TextAlignment.LEFT);
		var title = new Paragraph(titleText);

		// Subtitle
		var subtitleText = new Text(_subtitle)
			.SetFontFamily(StandardFonts.HELVETICA)
			.SetFontSize(10f)
			.SetHorizontalAlignment(HorizontalAlignment.LEFT)
			.SetTextAlignment(TextAlignment.LEFT);
		var subtitle = new Paragraph(subtitleText);


		var leftCellsStyle = new Style()
			.SetBorder(Border.NO_BORDER)
			.SetHorizontalAlignment(HorizontalAlignment.CENTER)
			.SetVerticalAlignment(VerticalAlignment.MIDDLE)
			.SetTextAlignment(TextAlignment.LEFT);

		// Left Column
		var leftColumn = new Cell()
			.Add(title)
			.Add(subtitle)
			.AddStyle(leftCellsStyle);
		table.AddCell(leftColumn);


		// Date
		var dateText = new Text($"{DateTime.Today.Date.ToLongDateString()}")
			.SetFontFamily(StandardFonts.HELVETICA_OBLIQUE)
			.SetFontSize(10f)
			.SetHorizontalAlignment(HorizontalAlignment.RIGHT)
			.SetTextAlignment(TextAlignment.RIGHT);
		var date = new Paragraph(dateText);

		var rightCellsStyle = new Style()
			.SetBorder(Border.NO_BORDER)
			.SetHorizontalAlignment(HorizontalAlignment.CENTER)
			.SetVerticalAlignment(VerticalAlignment.MIDDLE)
			.SetTextAlignment(TextAlignment.RIGHT);

		var dateCell = new Cell()
			.Add(date)
			.AddStyle(rightCellsStyle);
		table.AddCell(dateCell);

		return table;
	}
}