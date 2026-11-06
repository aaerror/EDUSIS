namespace Core.ServicioUsuarios.DTOs.Requests;

/// <summary>
/// Alta de la cuenta de un docente. El rol viaja como <c>Id</c> y <strong>el caso de uso lo usa</strong>.
/// </summary>
/// <remarks>
/// <para>
/// Hasta el 2026-11-06 el tercer dato era <c>string Rol</c> y <c>RegistrarUsuarioAsync</c>
/// <strong>no lo leía</strong>: construía <c>new Usuario(docenteID, email, salt, hash)</c>, el
/// constructor sin roles. Todo usuario creado por el caso de uso nacía sin un solo rol y por lo tanto
/// sin un solo permiso: entraba al sistema y se encontraba el menú entero apagado, sin forma de
/// arreglarlo porque no había pantalla que asignara roles. Era el mismo patrón que CORE-14 (los
/// <c>{"Basic","Custom"}</c> hardcodeados) y que UI-40: el campo viaja, la capa de abajo lo ignora.
/// </para>
/// <para>
/// Pasó de <c>string</c> a <c>int</c> en el mismo cambio, siguiendo la decisión #6 de
/// <c>docs/plan/rbac-y-seed.md</c>: los roles viajan por <c>Id</c>, que es canónico, y no por
/// descripción, que es presentación y vive en una columna no unicode.
/// </para>
/// </remarks>
public record RegistrarUsuarioRequest(Guid DocenteID, int RolID);
