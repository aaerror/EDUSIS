namespace Core.Shared.Documentos;

/// <summary>Período que abarca un documento, ambos extremos incluidos.</summary>
public sealed record RangoPeriodo(DateTime Desde, DateTime Hasta);
