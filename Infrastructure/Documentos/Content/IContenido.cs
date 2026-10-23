using iText.Layout.Element;

namespace Infrastructure.Documentos.Content;

/// <summary>Un bloque del cuerpo del documento: un párrafo, una tabla, el pie de firma.</summary>
public interface IContenido
{
	IBlockElement CrearContenido(Fuentes fuentes);
}
