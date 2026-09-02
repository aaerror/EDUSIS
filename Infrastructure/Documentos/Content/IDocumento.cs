using iText.Layout.Element;

namespace Infrastructure.Documentos.Content;

public interface IDocumento
{
	IBlockElement CrearContenido();
	//IReadOnlyCollection<IBlockElement> RecuperarContenido();
}
