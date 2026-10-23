using iText.Kernel.Colors;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace Infrastructure.Documentos.Content;

/// <summary>
/// Tabla de datos con encabezado: la forma de los reportes, del boletín y de las constancias de
/// materias.
/// </summary>
public sealed class Tabla : IContenido
{
	private const float TAMANO_ENCABEZADO = 9f;
	private const float TAMANO_CELDA = 9f;

	private static readonly Color FONDO_ENCABEZADO = new DeviceRgb(238, 238, 238);
	private static readonly Color LINEA = new DeviceRgb(170, 170, 170);

	private readonly IReadOnlyCollection<string> _columnas;
	private readonly IReadOnlyCollection<IReadOnlyCollection<string>> _filas;

	private string _mensajeSinDatos = "Sin datos para el alcance seleccionado.";


	private Tabla(IReadOnlyCollection<string> columnas, IReadOnlyCollection<IReadOnlyCollection<string>> filas)
	{
		if (columnas is null || !columnas.Any())
		{
			throw new ArgumentException("La tabla necesita al menos una columna.", nameof(columnas));
		}

		_columnas = columnas;
		_filas = filas ?? Array.Empty<IReadOnlyCollection<string>>();
	}

	public static Tabla Crear(IReadOnlyCollection<string> columnas, IReadOnlyCollection<IReadOnlyCollection<string>> filas) =>
		new(columnas, filas);

	public Tabla ConMensajeSinDatos(string mensaje)
	{
		_mensajeSinDatos = mensaje;

		return this;
	}

	public IBlockElement CrearContenido(Fuentes fuentes)
	{
		var tabla = new Table(UnitValue.CreatePercentArray(_columnas.Count))
			.UseAllAvailableWidth()
			.SetMarginTop(6f)
			.SetMarginBottom(14f);

		foreach (var columna in _columnas)
		{
			tabla.AddHeaderCell(Celda(columna, fuentes.Negrita, TAMANO_ENCABEZADO).SetBackgroundColor(FONDO_ENCABEZADO));
		}

		if (!_filas.Any())
		{
			// Una tabla vacía sin explicación se lee como un error del sistema. Con la leyenda, se
			// lee como lo que es: no hay datos que informar para este alcance. La leyenda ocupa la
			// fila entera (colspan) para que no queden celdas sueltas al costado.
			tabla.AddCell(Celda(_mensajeSinDatos, fuentes.Italica, TAMANO_CELDA, colspan: _columnas.Count)
				.SetTextAlignment(TextAlignment.CENTER)
				.SetMinHeight(28f));

			return tabla;
		}

		foreach (var fila in _filas)
		{
			foreach (var valor in fila)
			{
				tabla.AddCell(Celda(valor, fuentes.Regular, TAMANO_CELDA));
			}

			// Una fila con menos valores que columnas desalinearía todo lo que sigue.
			for (var faltante = fila.Count; faltante < _columnas.Count; faltante++)
			{
				tabla.AddCell(Celda(string.Empty, fuentes.Regular, TAMANO_CELDA));
			}
		}

		return tabla;
	}

	private static Cell Celda(string texto, iText.Kernel.Font.PdfFont fuente, float tamano, int colspan = 1) =>
		new Cell(1, colspan)
			.Add(new Paragraph(texto ?? string.Empty)
				.SetFont(fuente)
				.SetFontSize(tamano)
				.SetMargin(0)
				.SetMultipliedLeading(1.2f))
			.SetBorder(new SolidBorder(LINEA, 0.5f))
			.SetPadding(5f)
			.SetVerticalAlignment(VerticalAlignment.MIDDLE);
}
