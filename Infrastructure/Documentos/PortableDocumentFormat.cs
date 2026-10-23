using Core.Shared.Documentos;
using Infrastructure.Documentos.Content;

namespace Infrastructure.Documentos;

/// <summary>Renderizado a PDF con iText.</summary>
/// <remarks>
/// Antes escribía en una ruta cableada —<c>C:\edusis\docs\</c>— que llegaba por el constructor.
/// Ahora devuelve los bytes: el destino es una decisión del usuario y se le pregunta en la capa
/// de presentación, de modo que esta clase ya no depende del sistema de archivos ni necesita que
/// exista ninguna carpeta.
/// </remarks>
internal sealed class PortableDocumentFormat : IPortableDocumentFormat
{
	private readonly IDatosInstitucionales _institucion;


	public PortableDocumentFormat(IDatosInstitucionales institucion)
	{
		_institucion = institucion;
	}

	public byte[] Generar(Documento documento)
	{
		ArgumentNullException.ThrowIfNull(documento);

		using var memoria = new MemoryStream();

		// El bloque cierra el PdfCreator —y con él el documento iText— ANTES de leer los bytes:
		// hasta que el documento no se cierra, el flujo no tiene el PDF completo.
		using (var creator = new PdfCreator(documento, memoria, _institucion))
		{
			creator.Renderizar(documento);
		}

		return memoria.ToArray();
	}
}
