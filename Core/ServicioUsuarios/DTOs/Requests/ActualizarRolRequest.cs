namespace Core.ServicioUsuarios.DTOs.Requests;

/// <remarks>
/// El rol viaja por <c>Id</c> y no por descripción: tres de los seis roles llevan acento
/// ("Dirección", "Preceptoría", "Secretaría Académica"), así que un literal mal escrito fallaría en
/// ejecución y no en compilación.
/// </remarks>
public record ActualizarRolRequest(Guid UsuarioID, int RolID);
