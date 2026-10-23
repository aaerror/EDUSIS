using iText.Layout.Element;
using iText.Layout.Properties;

namespace Infrastructure.Documentos.Content;

/// <summary>Un párrafo de texto del cuerpo del documento.</summary>
public sealed class Parrafo : IContenido
{
	private const float TAMANO_BASE = 11f;

	private readonly string _texto;

	private TextAlignment _alineacion = TextAlignment.JUSTIFIED;
	private float _tamano = TAMANO_BASE;
	private bool _negrita;
	private bool _italica;
	private bool _sangria;
	private float _espacioSuperior;
	private float _espacioInferior = 10f;


	private Parrafo(string texto)
	{
		_texto = texto ?? string.Empty;
	}

	public static Parrafo Crear(string texto) =>
		new(texto);

	public Parrafo Centrado()
	{
		_alineacion = TextAlignment.CENTER;

		return this;
	}

	public Parrafo ALaDerecha()
	{
		_alineacion = TextAlignment.RIGHT;

		return this;
	}

	public Parrafo ALaIzquierda()
	{
		_alineacion = TextAlignment.LEFT;

		return this;
	}

	public Parrafo ConTamano(float tamano)
	{
		_tamano = tamano;

		return this;
	}

	public Parrafo EnNegrita()
	{
		_negrita = true;

		return this;
	}

	public Parrafo EnItalica()
	{
		_italica = true;

		return this;
	}

	/// <summary>Sangría de primera línea, para los párrafos del cuerpo de un certificado.</summary>
	public Parrafo ConSangria()
	{
		_sangria = true;

		return this;
	}

	public Parrafo ConEspacio(float superior, float inferior)
	{
		_espacioSuperior = superior;
		_espacioInferior = inferior;

		return this;
	}

	public IBlockElement CrearContenido(Fuentes fuentes)
	{
		var fuente = _negrita ? fuentes.Negrita
			: _italica ? fuentes.Italica
			: fuentes.Regular;

		var parrafo = new Paragraph(_texto)
			.SetFont(fuente)
			.SetFontSize(_tamano)
			.SetTextAlignment(_alineacion)
			.SetMultipliedLeading(1.35f)
			.SetMarginTop(_espacioSuperior)
			.SetMarginBottom(_espacioInferior);

		if (_sangria)
		{
			parrafo.SetFirstLineIndent(28f);
		}

		return parrafo;
	}
}
