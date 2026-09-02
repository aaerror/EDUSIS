using Infrastructure.Documentos.Events;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf.Event;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using iText.Layout.Font;
using iText.Layout.Properties;
using iText.Layout;

//https://github.com/nazuke/SEOMacroscope/tree/master
namespace Infrastructure.Documentos;

internal class PdfCreator
{
	private readonly PageSize _pageSize = PageSize.A4;
	private readonly MemoryStream _memoryStream;

	#region iText
	private readonly PdfHelper _pdfHelper;
	private readonly PdfDocument _pdf;
	private readonly Document _document;
	#endregion

	
	private PdfCreator(string title, MemoryStream memoryStream)
	{
		if (string.IsNullOrWhiteSpace(title))
		{
			throw new ArgumentException("Se debe especificar un título para el documento.");
		}

		_memoryStream = memoryStream;

		_pdfHelper = PdfHelper.Create(_memoryStream);
		_pdf = _pdfHelper.OpenWriter();
		_pdf.AddFont(PdfFontFactory.CreateFont(StandardFontFamilies.HELVETICA));

		_document = new Document(_pdf, _pageSize);
		_document.SetMargins(80, 60, 80, 60);
		AgregarFuentes();
	}

	public static PdfCreator Create(string title, MemoryStream memoryStream) =>
		new(title, memoryStream);

	private void AgregarFuentes()
	{
		var provider = new FontProvider();
		provider.AddStandardPdfFonts();

		_document.SetFontProvider(provider);
	}

	#region Páginas
	public PdfPage AgregarNuevaPagina() =>
		_pdf.AddNewPage();

	public PdfPage AgregarNuevaPagina(int numeroPagina) =>
		_pdf.AddNewPage(numeroPagina);

	public void BorrarPagina(int numeroPagina) =>
		_pdf.RemovePage(numeroPagina);
	#endregion

	#region Contenido
	public void AgregarContenido(IBlockElement element)
	{
		var text = new Text("content")
			.SetFontFamily(StandardFontFamilies.HELVETICA)
			.SetFontSize(12);
		var body = new Paragraph(text)
			.SetTextAlignment(TextAlignment.JUSTIFIED);

		_document.Add(element);
	}

	public void AgregarContenido(Paragraph parrafo)
	{
		_document.Add(parrafo);
	}
	#endregion

	#region Encabezado & Pie Página
	public void EstablecerEncabezadoYPiePagina(string subtitulo)
	{
		_pdf.AddEventHandler(PdfDocumentEvent.END_PAGE, new HeaderEventHandler(subtitulo));
		_pdf.AddEventHandler(PdfDocumentEvent.END_PAGE, new FooterEventHandler());
	}
	#endregion

	public void Cerrar()
	{
		if (!_pdf.IsClosed())
		{
			_pdf.Close();
		}
	}
}
