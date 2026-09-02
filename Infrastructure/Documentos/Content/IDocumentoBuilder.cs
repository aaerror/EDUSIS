using iText.Layout.Element;

namespace Infrastructure.Documentos.Content;

public interface IDocumentoBuilder
{
	IDocumentoBuilder AgregarTituloDocumento(string titulo, string? subtitulo);
	IDocumentoBuilder AgregarContenido(IContenido contenido);
	Documento EnsamblarDocumento();
}