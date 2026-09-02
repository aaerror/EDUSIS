using Infrastructure.Documentos.Content;
using iText.Forms.Logs;

namespace Infrastructure.Documentos;

internal class PortableDocumentFormat : IPortableDocumentFormat
{
	private readonly string _path = string.Empty;
	private PdfCreator _pdfCreator;

	public PortableDocumentFormat(string path)
	{
		_path = path;
	}

	public async Task<FileStream> Create(string filename, string title, Documento documento)
	{
		try
		{
			using var memoryStream = new MemoryStream();

			_pdfCreator = PdfCreator.Create(title, memoryStream);
			var elementos = documento.Elementos;
			foreach (var content in elementos)
			{
				_pdfCreator.AgregarContenido(content);
			}
			/*
			 * _pdfCreator.AgregarContenido("" +
				"\r\n\tLorem ipsum dolor sit amet, consectetur adipiscing elit. Nullam a tempor mauris. Orci varius natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus. Phasellus est tortor, tristique quis faucibus nec, bibendum a tellus. Nullam malesuada volutpat odio, ut accumsan libero malesuada eget. Nam ac dolor in eros varius finibus. Praesent dignissim est vitae magna molestie porttitor. Ut eget nibh in elit consectetur ornare. Donec egestas risus sed nulla fermentum varius ut quis metus. Duis ultricies lorem orci, id auctor leo ullamcorper eu.\r\n\r\n\tMorbi lectus enim, convallis in pretium at, consequat eget tortor. Aliquam lobortis erat enim, a scelerisque eros blandit sit amet. Sed nec magna bibendum, accumsan nisi eget, sagittis nunc. Pellentesque vitae velit lectus. Morbi non lectus sodales lectus varius mattis non quis nisi. Morbi dapibus vehicula mollis. Curabitur vel laoreet ligula, nec molestie mi.\r\n\r\n\tMorbi tellus lacus, varius eu nunc a, varius varius enim. Proin quis arcu eu arcu viverra consequat. Aenean ut tellus eget leo aliquam tincidunt. Sed id tellus dui. Praesent vitae sapien semper, fringilla turpis at, fermentum felis. Pellentesque tellus ante, blandit at leo vel, placerat vulputate enim. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos. Duis id convallis felis. Sed hendrerit massa ut eros imperdiet, nec vestibulum mi scelerisque.\r\n\r\n\tCras lobortis vel purus sit amet egestas. Vivamus euismod sollicitudin tellus, eget dictum elit vestibulum ut. Phasellus sit amet dapibus turpis, et tincidunt risus. Interdum et malesuada fames ac ante ipsum primis in faucibus. Pellentesque dignissim lobortis dolor, eu finibus massa finibus in. Nam nec placerat lectus. Quisque sollicitudin nisl ac ligula ultricies, feugiat tincidunt mauris fermentum. Phasellus eu blandit tortor. Proin sodales elit sit amet auctor facilisis. Praesent blandit mauris ut ligula mollis luctus. Integer dolor sapien, tempus nec mauris sit amet, suscipit tempus ipsum. Curabitur ut vehicula tellus. Nunc ornare urna neque. Donec dignissim erat in congue aliquet. Etiam efficitur iaculis erat, id mattis erat dignissim porttitor.");
			*/
			_pdfCreator.EstablecerEncabezadoYPiePagina(title);
			_pdfCreator.Cerrar();

			Directory.CreateDirectory(_path);
			using var file = File.Create(_path + filename);
			//memoryStream.Position = 0;
			file.Write(memoryStream.ToArray(), 0, memoryStream.ToArray().Length);
			
			//return File(memoryStream.ToArray(), "application/json", filename);
			return file;
		}
		catch (Exception ex)
		{
			throw ex;
		}
	}

	/*private void AddHeader(Documento pdfDocument, iText.Layout.Document document)
	{
		var pages = pdfDocument.GetNumberOfPages();
		for (int i = 0; i <= pages; i++)
		{
			var currentPage = pdfDocument.GetPage(i);
			var canvas = new PdfCanvas(currentPage);

			var style = new Style();

			// ADD BRAND
			var brand = new Paragraph("EDUSIS");
			document.ShowTextAligned(brand, 559, 809, i, TextAlignment.RIGHT, VerticalAlignment.TOP, 0);

			// ADD DATE
			var header = new Paragraph(string.Format(DateTime.Today.Date.ToString("D")));
			document.ShowTextAligned(header, 559, 809, i, TextAlignment.RIGHT, VerticalAlignment.TOP, 0);

			// ENCABEZADO
			canvas.BeginText()
				  .SetFontAndSize(_font, 10)
				  .MoveText(PAGE_SIZE.GetWidth() * 0.75, PAGE_SIZE.GetHeight() - 10)
				  .ShowText($"{DateTime.Today.Date.ToString("D")}")
				  .EndText();

			// LÍNEA DIVISORIA
			canvas.SetStrokeColor(ColorConstants.BLACK)
				  .SetLineWidth(1)
				  .MoveTo(40, PAGE_SIZE.GetHeight() - 40)
				  .LineTo(PAGE_SIZE.GetWidth(), PAGE_SIZE.GetHeight() - 40)
				  .Stroke();
		}
	}

	private void AddFooter(Documento pdfDocument)
	{
		var pages = pdfDocument.GetNumberOfPages();
		for (int i = 0; i <= pages; ++i)
		{
			var currentPage = pdfDocument.GetPage(i);
			var canvas = new PdfCanvas(currentPage);

			// LÍNEA DIVISORIA
			canvas.SetStrokeColor(ColorConstants.BLACK)
				  .SetLineWidth(1)
				  .MoveTo(PAGE_SIZE.GetWidth() / 2 - 30, 20)
				  .LineTo(PAGE_SIZE.GetWidth() / 2 + 30, 20)
				  .Stroke();

			//Draw page number
			canvas.BeginText()
				  .SetFontAndSize(_font, 10)
				  .MoveText(PAGE_SIZE.GetWidth() / 2 - 7, 10)
				  .ShowText($"{ i } de { pages }")
				  .EndText();
		}
	}*/
}
