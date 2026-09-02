using iText.IO.Font.Constants;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace Infrastructure.Documentos.Content;

public class Parrafo : IParrafo
{
	private string _titulo;
	private string _subtitulo;
	private string _texto;


	//private Parrafo() { }


	public IParrafo ConTitulo(string titulo)
	{
		_titulo = titulo;
		return this;
	}

	public IParrafo ConSubtitulo(string subtitulo)
	{
		_subtitulo = subtitulo;
		return this;
	}

	public IParrafo ConParrafo(string texto)
	{
		_texto = texto;
		return this;
	}

	public IBlockElement CrearContenido()
	{
		var text = new Text(_texto)
			.SetFontFamily(StandardFontFamilies.HELVETICA)
			.SetFontSize(12);

		var body = new Paragraph()
			.Add(new Tab())
			.Add(text)
			.SetTextAlignment(TextAlignment.JUSTIFIED);

		return body;
	}
}