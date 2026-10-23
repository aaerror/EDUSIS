namespace Core.Shared.Documentos;

/// <summary>
/// Documento ya renderizado, en memoria.
/// </summary>
/// <remarks>
/// Deliberadamente NO es una ruta ni un <c>FileStream</c>: quién decide dónde se guarda es la
/// capa de presentación, que es la única que puede preguntárselo al usuario. Devolver bytes deja
/// a los casos de uso sin dependencia del sistema de archivos —y por lo tanto comprobables sin
/// escribir en disco—. <paramref name="NombreSugerido"/> es sólo eso: una sugerencia para el
/// cuadro de guardado.
/// </remarks>
public sealed record DocumentoGenerado(string NombreSugerido, byte[] Contenido);
