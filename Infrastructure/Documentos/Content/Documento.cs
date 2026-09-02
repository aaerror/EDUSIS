using iText.Layout.Element;

namespace Infrastructure.Documentos.Content;

public class Documento
{
	#region Content
	private List<IBlockElement> _elementos;
	#endregion

	#region Document
	private readonly string _tituloDocumento = string.Empty;
	private string _subtituloDocumento = string.Empty;
	#endregion

	public IReadOnlyCollection<IBlockElement> Elementos => _elementos.AsReadOnly();


	private Documento(string tituloDocumento)
	{
		if (string.IsNullOrWhiteSpace(tituloDocumento))
		{
			throw new ArgumentException("Debe agregar el título del documento.", nameof(tituloDocumento));
		}
		_tituloDocumento = tituloDocumento;

		_elementos = new List<IBlockElement>();
	}

	private Documento(string tituloDocumento, string? subtituloDocumento)
		: this(tituloDocumento)
	{
		_subtituloDocumento = subtituloDocumento;
	}

	public static Documento Create(string tituloDocumento) =>
		new(tituloDocumento);

	public static Documento Create(string tituloDocumento, string? subtituloDocumento) =>
		new(tituloDocumento, subtituloDocumento);

	public void AgregarContenido(IContenido contenido)
	{
		var elemento = contenido.CrearContenido();
		_elementos.Add(elemento);
	}

	internal void AgregarContenido(IBlockElement parrafo)
	{
		throw new NotImplementedException();
	}
	/*
   public IContenidoBuilder AgregarParrafo(string texto)
   {
	   var text = new Text(texto)
		   .SetFontFamily(StandardFontFamilies.HELVETICA)
		   .SetFontSize(12);
	   var parrafo = new Paragraph(text)
		   .SetTextAlignment(TextAlignment.JUSTIFIED);

	   _elementos.Add(parrafo);

	   return this;
   }

   public IContenidoBuilder AgregarTabla(int cantidadColumnas, string[] encabezadoTabla, List<List<string>> data)
   {
	   _tabla = new Table(cantidadColumnas, false)
		   .UseAllAvailableWidth()
		   .SetHorizontalAlignment(HorizontalAlignment.CENTER)
		   .SetTextAlignment(TextAlignment.CENTER)
		   .SetVerticalAlignment(VerticalAlignment.MIDDLE);

	   if (!encabezadoTabla.IsNullOrEmpty())
	   {
		   if (cantidadColumnas != encabezadoTabla.Count())
		   {
			   throw new ArgumentException("La cantidad de columnas y la cantidad de nombres de encabezados no coinciden.");
		   }

		   foreach (var encabezado in encabezadoTabla)
		   {
			   var celda = new Cell()
				   .SetHorizontalAlignment(HorizontalAlignment.CENTER)
				   .SetTextAlignment(TextAlignment.CENTER)
				   .SetVerticalAlignment(VerticalAlignment.MIDDLE);

			   var textoEncabezado = new Text(encabezado)
				   .SetFontFamily(StandardFonts.HELVETICA_BOLD)
				   .SetFontSize(12);

			   celda.Add(new Paragraph(textoEncabezado));

			   _tabla.AddHeaderCell(celda);
		   }
	   }

	   foreach (var fila in data)
	   {
		   foreach (var campo in fila)
		   {
			   var celda = new Cell()
				   .SetHorizontalAlignment(HorizontalAlignment.CENTER)
				   .SetTextAlignment(TextAlignment.CENTER)
				   .SetVerticalAlignment(VerticalAlignment.MIDDLE);

			   var texto = new Text(campo)
				   .SetFontFamily(StandardFonts.HELVETICA_BOLD)
				   .SetFontSize(12);

			   celda.Add(new Paragraph(texto));
		   }
	   }

	   _elementos.Add(_tabla);

	   return this;
   }
*/
}
