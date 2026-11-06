namespace Core.ServicioUsuarios.DTOs.Requests;

/// <remarks>
/// El rol viaja por <c>Id</c> desde el 2026-11-06. Antes era <c>string Rol</c> y se resolvía con
/// <c>Enumeration.FromDescripcion&lt;Rol&gt;</c>: tres de los seis roles llevan acento
/// ("Dirección", "Preceptoría", "Secretaría Académica"), así que un literal mal escrito fallaba en
/// ejecución y no en compilación. Decisión #6 de <c>docs/plan/rbac-y-seed.md</c>.
/// </remarks>
public record ActualizarRolRequest(Guid UsuarioID, int RolID);
