using Domain.Shared;

namespace Domain.Usuarios;

public interface IUsuarioRepository : IRepository<Usuario>
{
    public Usuario BuscarPorUsuario(string unUsuario);

    public bool ExisteUsuarioDelDocente(Guid docenteID);

    public bool EsUsuarioInvalido(string usuario);

    public void RecuperarDatosAcceso(string usuario, out string salt, out string hash);
}
