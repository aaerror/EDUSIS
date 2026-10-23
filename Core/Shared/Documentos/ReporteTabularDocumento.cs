namespace Core.Shared.Documentos;

/// <summary>
/// Reporte institucional en forma de tabla.
/// </summary>
/// <remarks>
/// Los cinco reportes comparten exactamente esta forma —título, alcance, encabezados, filas— y
/// se diferencian sólo en qué significan sus columnas, que es una decisión de los casos de uso.
/// Por eso hay un único tipo y un único renderizador: cinco records con los mismos campos
/// obligarían a cinco plantillas idénticas que habría que mantener en paralelo.
/// <para>
/// <paramref name="Filas"/> viaja como texto ya formateado: el renderizador no tiene que saber
/// si una celda es un porcentaje, una nota o un recuento.
/// </para>
/// </remarks>
public sealed record ReporteTabularDocumento(
	string Titulo,
	string Alcance,
	IReadOnlyCollection<string> Columnas,
	IReadOnlyCollection<IReadOnlyCollection<string>> Filas,
	MetadatosEmision Emision);
