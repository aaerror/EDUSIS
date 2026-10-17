using Domain.Usuarios;
using System.Runtime.CompilerServices;

namespace Core.Shared.Seguridad;

internal static class SesionActualExtensions
{
	/// <summary>
	/// Guarda de autorización de un caso de uso: si la sesión en curso no tiene
	/// <paramref name="unPermiso"/>, lanza <see cref="PermisoDenegadoException"/> y el método no
	/// llega a tocar el agregado.
	/// </summary>
	/// <remarks>
	/// Existe para que la guarda sea <strong>una sola línea</strong> en cada caso de uso. Con el
	/// <c>if</c>/<c>throw</c> escrito a mano se repetiría más de cincuenta veces, y una guarda que
	/// cuesta cuatro líneas es una guarda que alguien va a omitir en la pantalla siguiente.
	/// <para>
	/// <paramref name="operacion"/> lo completa el compilador con
	/// <see cref="CallerMemberNameAttribute"/>: no hay que escribir el nombre del método, así que
	/// tampoco puede quedar desactualizado al renombrarlo.
	/// </para>
	/// </remarks>
	internal static void Exigir(
		this ISesionActual sesion,
		Permiso unPermiso,
		[CallerMemberName] string operacion = "")
	{
		if (!sesion.TienePermiso(unPermiso))
		{
			throw new PermisoDenegadoException($"{operacion} requiere «{unPermiso.Descripcion}»");
		}
	}
}
