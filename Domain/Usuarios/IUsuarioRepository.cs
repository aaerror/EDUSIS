using Domain.Shared;

namespace Domain.Usuarios;

public interface IUsuarioRepository : IRepository<Usuario>
{
	Task<Usuario?> BuscarPorUsernameAsync(string username);
	Task<Usuario?> BuscarPorDocenteAsync(Guid docenteID);
	Task<bool> ExisteUsuarioDelDocenteAsync(Guid docenteID);
	Task<bool> EsUsernameInvalidoAsync(string username);

	/// <summary>
	/// Cuántas cuentas tienen asignado ese rol, por <c>Id</c> y no por descripción.
	/// </summary>
	/// <remarks>
	/// La consume la guarda del último administrador del borrado de docente. Por <c>Id</c> siguiendo
	/// la decisión #6 de <c>docs/plan/rbac-y-seed.md</c>: la descripción vive en una columna
	/// <c>varchar</c> no unicode y tres de los seis roles llevan acento, así que compararla es
	/// frágil de una forma que no se ve hasta que falla.
	/// </remarks>
	Task<int> ContarUsuariosConRolAsync(int rolID);
}