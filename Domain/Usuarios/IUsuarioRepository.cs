using Domain.Shared;

namespace Domain.Usuarios;

public interface IUsuarioRepository : IRepository<Usuario>
{
	public Usuario BuscarPorEmail(string unUsuario);

	public bool ExisteUsuarioDelDocente(Guid docenteID);

	public bool EsEmailInvalido(string usuario);

	public void RecuperarDatosAcceso(string usuario, out string salt, out string hash);
}