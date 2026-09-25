using Domain.Shared;

namespace Domain.Usuarios;

public interface IUsuarioRepository : IRepository<Usuario>
{
	Task<Usuario?> BuscarPorUsernameAsync(string username);
	Task<Usuario?> BuscarPorDocenteAsync(Guid docenteID);
	Task<bool> ExisteUsuarioDelDocenteAsync(Guid docenteID);
	Task<bool> EsUsernameInvalidoAsync(string username);
}