using Core.Shared.Seguridad;
using Domain.Shared;
using Domain.Usuarios;

namespace EDUSIS.Seed;

/// <summary>
/// Sesión del proceso de siembra: concede los diez permisos. No hay un usuario humano detrás de
/// una corrida del seeder, y la herramienta corre con la base entera a su disposición de todos
/// modos.
/// </summary>
/// <remarks>
/// Vive en el seeder y <strong>no</strong> en <c>CoreDI</c>, que es lo que el plan pedía
/// (Fase 5): si la capa de aplicación registrara por omisión una sesión que concede todo, un
/// composition root que olvidara su propio registro correría sin autorización y sin un solo
/// error. Cada composition root se hace cargo de su sesión; este es la de esta herramienta.
/// <para>
/// Hoy el seeder construye los agregados por el dominio y guarda con <c>IUnitOfWork</c>, así que
/// ningún caso de uso con guarda se ejecuta y esta clase no llega a consultarse. Está igual, por
/// dos razones: <c>CoreDI.AddServices()</c> registra los catorce servicios y cualquiera que se
/// resuelva pide <see cref="ISesionActual"/> por constructor, y el día que el seeder sí llame a
/// un caso de uso no debería tener que descubrir esto a los golpes.
/// </para>
/// </remarks>
internal sealed class SesionDeSistema : ISesionActual
{
	public Guid? UsuarioID => null;

	public bool EstaAutenticado => true;

	public bool TienePermiso(Permiso unPermiso) =>
		Enumeration.GetAll<Permiso>().Contains(unPermiso);
}
