namespace Core.ServicioAutenticaciones.DTOs.Responses;

/// <summary>
/// Los roles viajan como **Id** y no como descripción. El Id es canónico; la descripción es
/// presentación: vive en una columna <c>varchar(15)</c> no unicode (<c>RolesConfiguration</c>),
/// así que en un servidor cuya colación no cubra 'ó' el texto "Dirección" se degrada y dejaría de
/// coincidir con el del código. Como esa comparación alimenta la autorización, el usuario perdería
/// todos sus permisos en silencio. Un int no depende de la colación. Para mostrar el nombre:
/// <c>Enumeration.FromValue&lt;Rol&gt;(id).Descripcion</c>.
/// </summary>
public record UsuarioResponse(Guid UsuarioID, Guid DocenteID, string Usuario, string NombreCompleto, string Puesto, List<int> RolesIDs);