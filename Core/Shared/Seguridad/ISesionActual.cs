using Domain.Usuarios;

namespace Core.Shared.Seguridad;

/// <summary>
/// Puerto de la sesión en curso. Lo define <c>Core</c> y lo implementa el composition root
/// (<c>WPF_Desktop</c>), igual que <see cref="Documentos.IGeneradorDocumentos"/>: así los casos
/// de uso pueden autorizar sin conocer claims, <c>ClaimsPrincipal</c> ni nada de la interfaz.
/// </summary>
/// <remarks>
/// Deliberadamente **sincrónico y sin consultas**: los roles se resuelven una vez, al iniciar
/// sesión, y quedan en la implementación. Si <c>TienePermiso</c> fuera a la base en cada
/// pregunta, el <c>CanExecute</c> de cada comando de la interfaz haría una consulta por
/// repintado. La contrapartida es que un cambio de roles recién aplica al volver a entrar.
/// </remarks>
public interface ISesionActual
{
	Guid? UsuarioID { get; }

	bool EstaAutenticado { get; }

	bool TienePermiso(Permiso unPermiso);
}
