using iText.Kernel.Pdf;

namespace Infrastructure.Documentos;

public class PdfHelper
{
	private readonly PdfWriter _pdfWriter;


	private PdfHelper(MemoryStream memoryStream)
	{
		memoryStream.Flush();
		_pdfWriter = new PdfWriter(memoryStream);
	}

	public static PdfHelper Create(MemoryStream memoryStream) =>
		new PdfHelper(memoryStream);

	public PdfDocument OpenWriter() =>
		new PdfDocument(_pdfWriter);

	public void Position(long position) =>
		_pdfWriter.Position = position;

	public long CurrentPosition() =>
		_pdfWriter.GetCurrentPos();

	/*public void SetHeader(int page, string text)
	{
		// ADD BRAND
		var brand = new Paragraph(text);
		_documento.ShowTextAligned(brand, 559, 809, page, TextAlignment.RIGHT, VerticalAlignment.TOP, 0);

		// ADD DATE
		var header = new Paragraph(string.Format(DateTime.Today.Date.ToString("D")));
		_documento.ShowTextAligned(header, 559, 809, page, TextAlignment.RIGHT, VerticalAlignment.TOP, 0);
	}*/
}
