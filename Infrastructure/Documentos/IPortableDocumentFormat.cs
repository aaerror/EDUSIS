using Infrastructure.Documentos.Content;

namespace Infrastructure.Documentos;

internal interface IPortableDocumentFormat
{
	Task<FileStream> Create(string filename, string title, Documento contenido);
}