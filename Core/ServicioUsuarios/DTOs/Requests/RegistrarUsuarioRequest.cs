namespace Core.ServicioUsuarios.DTOs.Requests;

/// <summary>
/// Alta de la cuenta de un docente. El rol viaja como <c>Id</c> y <strong>el caso de uso lo usa</strong>.
/// </summary>
/// <remarks>
/// <para>
/// <c>RegistrarUsuarioAsync</c> tiene que <strong>leer</strong> el rol: si construyera
/// <c>new Usuario(docenteID, email, salt, hash)</c>, el constructor sin roles, todo usuario nacería
/// sin un solo rol y por lo tanto sin un solo permiso: entraría al sistema y se encontraría el menú
/// entero apagado, sin forma de arreglarlo si no hubiera pantalla que asigne roles.
/// </para>
/// <para>
/// Los roles viajan por <c>Id</c> (<c>int</c>), que es canónico, y no por descripción, que es
/// presentación y vive en una columna no unicode.
/// </para>
/// </remarks>
public record RegistrarUsuarioRequest(Guid DocenteID, int RolID);
