using iText.Layout.Element;

namespace Infrastructure.Documentos.Content;

public interface ITablaBuilder
{
	void ConEncabezadoDeTabla(string encabezado);
	void ConColumnas(List<string> columnas);
	void ConFilas(List<Dictionary<int, string>> filas);
	Table ConstruirTabla();
}
