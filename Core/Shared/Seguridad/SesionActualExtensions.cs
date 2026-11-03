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

	/// <summary>
	/// Guarda de autorización para una operación auxiliar que sirve a varios casos de uso: alcanza
	/// con <strong>uno</strong> de <paramref name="losPermisos"/>.
	/// </summary>
	/// <remarks>
	/// Existe por los dos <c>ListarCiclosLectivosAsync</c>, que no son un caso de uso sino el
	/// combo que llena la pantalla antes de emitir. Exigir ahí el permiso del documento académico
	/// dejaría a Preceptoría sin poder elegir período para el certificado de asistencia, que sí
	/// puede emitir. No es un "o" general para los casos de uso: una operación con un actor
	/// definido usa <see cref="Exigir"/> con su permiso y nada más.
	/// </remarks>
	internal static void ExigirAlguno(
		this ISesionActual sesion,
		Permiso[] losPermisos,
		[CallerMemberName] string operacion = "")
	{
		if (losPermisos.Any(sesion.TienePermiso))
		{
			return;
		}

		var descripciones = string.Join(" o ", losPermisos.Select(x => $"«{x.Descripcion}»"));

		throw new PermisoDenegadoException($"{operacion} requiere {descripciones}");
	}
}
