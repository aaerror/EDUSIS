
using iText.Layout.Element;

namespace Infrastructure.Documentos.Content;

public class TablaBuilder : ITablaBuilder
{
	private int _cantidadColumnas;

	public TablaBuilder(int cantidadColumnas)
	{
		_cantidadColumnas = cantidadColumnas;
	}

	public void ConColumnas(List<string> columnas)
	{
		throw new NotImplementedException();
	}

	public void ConEncabezadoDeTabla(string encabezado)
	{
		throw new NotImplementedException();
	}

	public void ConFilas(List<Dictionary<int, string>> filas)
	{
		throw new NotImplementedException();
	}

	public Table ConstruirTabla()
	{
		throw new NotImplementedException();
	}
}
