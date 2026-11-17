namespace Core.Shared;

/// <summary>
/// Identifica el assembly de <c>Core</c> en los escaneos por reflexión (MediatR registra con él
/// los <c>INotificationHandler</c> de <c>Core/Servicio*/Events/</c>).
/// </summary>
/// <remarks>
/// Existe sólo para eso y no se mueve de este proyecto. Antes se usaba <c>IGeneradorDocumentos</c>
/// como testigo: si ese puerto se movía o se borraba, los handlers de <c>Core</c> dejaban de
/// registrarse sin un solo error de compilación.
/// </remarks>
public static class MarcadorDeCore
{
}
