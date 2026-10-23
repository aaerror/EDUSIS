using Core.Shared.Documentos;
using Infrastructure.Documentos.Content;
using Infrastructure.Documentos.Events;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Event;
using iText.Layout;

namespace Infrastructure.Documentos;

/// <summary>
/// Ciclo de vida del documento iText: abre el escritor, fija los márgenes y los metadatos,
/// engancha el encabezado y el pie de página, y escribe los bloques del
/// <see cref="Documento"/>.
/// </summary>
/// <remarks>
/// Los márgenes superior e inferior son generosos porque el encabezado y el pie se dibujan
/// FUERA del área de contenido, con un <c>Canvas</c> propio en el evento de cierre de página: si
/// el margen no les reserva lugar, el cuerpo se imprime encima.
/// </remarks>
internal sealed class PdfCreator : IDisposable
{
	private const float MARGEN_SUPERIOR = 96f;
	private const float MARGEN_INFERIOR = 72f;
	private const float MARGEN_LATERAL = 56f;

	private readonly Fuentes _fuentes;
	private readonly PdfDocument _pdf;
	private readonly Document _document;


	public PdfCreator(Documento documento, Stream salida, IDatosInstitucionales institucion)
	{
		var escritor = new PdfWriter(salida);

		// El flujo lo administra quien lo creó: cerrarlo acá impediría leer los bytes después.
		escritor.SetCloseStream(false);

		_pdf = new PdfDocument(escritor);
		_fuentes = Fuentes.Estandar();

		EstablecerMetadatos(documento, institucion);

		var tamano = documento.Orientacion == Orientacion.Apaisada
			? PageSize.A4.Rotate()
			: PageSize.A4;

		_document = new Document(_pdf, tamano);
		_document.SetMargins(MARGEN_SUPERIOR, MARGEN_LATERAL, MARGEN_INFERIOR, MARGEN_LATERAL);

		_pdf.AddEventHandler(PdfDocumentEvent.END_PAGE, new HeaderEventHandler(institucion.Nombre, documento.Titulo, _fuentes));
		_pdf.AddEventHandler(PdfDocumentEvent.END_PAGE, new FooterEventHandler($"{ institucion.Nombre } · CUE { institucion.CUE }", _fuentes));
	}

	/// <summary>Escribe los bloques del documento, en orden.</summary>
	public void Renderizar(Documento documento)
	{
		foreach (var contenido in documento.Contenidos)
		{
			_document.Add(contenido.CrearContenido(_fuentes));
		}
	}

	private void EstablecerMetadatos(Documento documento, IDatosInstitucionales institucion)
	{
		var info = _pdf.GetDocumentInfo();
		info.SetTitle(documento.Titulo);
		info.SetAuthor(institucion.Nombre);
		info.SetCreator("EDUSIS");

		if (!string.IsNullOrWhiteSpace(documento.Subtitulo))
		{
			info.SetSubject(documento.Subtitulo);
		}
	}

	public void Dispose()
	{
		// Close() del Document cierra también el PdfDocument y descarga el contenido al flujo.
		if (!_pdf.IsClosed())
		{
			_document.Close();
		}
	}
}
