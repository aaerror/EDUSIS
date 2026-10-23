using Infrastructure.Documentos.Content;

namespace Infrastructure.Documentos;

/// <summary>Renderiza un <see cref="Documento"/> a PDF.</summary>
internal interface IPortableDocumentFormat
{
	/// <summary>
	/// Devuelve el PDF en memoria. No escribe en disco: la ruta la elige la capa de presentación.
	/// </summary>
	byte[] Generar(Documento documento);
}
