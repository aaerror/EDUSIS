namespace Core.Shared.Documentos;

/// <summary>
/// Lugar, fecha y motivo de expedición.
/// </summary>
/// <remarks>
/// <paramref name="Finalidad"/> es opcional: se imprime sólo cuando el trámite la requiere. Un
/// documento que declara un motivo que nadie pidió es peor que uno que no lo declara.
/// </remarks>
public sealed record MetadatosEmision(string Localidad, DateTime Fecha, string? Finalidad);
