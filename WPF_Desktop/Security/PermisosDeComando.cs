using Core.Shared.Seguridad;
using Domain.Usuarios;
using System;

namespace WPF_Desktop.Security;

/// <summary>
/// <c>CanExecute</c> de un comando cuyo caso de uso tiene guarda en <c>Core</c>: exige el MISMO
/// permiso que <c>_sesion.Exigir(...)</c> y, además, la condición propia del comando.
/// </summary>
/// <remarks>
/// <para>
/// Antes sólo el menú (<c>MainViewModel</c>) consultaba permisos: dentro de una pantalla el usuario
/// descubría lo que no podía hacer por el diálogo de <c>PermisoDenegadoException</c>, después de
/// llenar el formulario. Con esto el botón nace gris. La seguridad sigue en <c>Core</c>; esto es
/// sólo para que la pantalla no ofrezca lo que <c>Core</c> va a rechazar.
/// </para>
/// <para>
/// La sesión no cambia mientras vive una pantalla —cerrar sesión cierra <c>MainWindow</c> y la
/// siguiente sesión construye ViewModels nuevos—, así que no hace falta reevaluar los comandos ante
/// <c>ISesionUsuario.Cambio</c>.
/// </para>
/// </remarks>
internal static class PermisosDeComando
{
	public static Func<bool> Permite(this ISesionActual sesion, Permiso permiso, Func<bool>? condicion = null) =>
		() => sesion.TienePermiso(permiso) && (condicion is null || condicion());

	public static Predicate<T?> Permite<T>(this ISesionActual sesion, Permiso permiso, Predicate<T?> condicion) =>
		parametro => sesion.TienePermiso(permiso) && condicion(parametro);

	public static Predicate<T?> Permite<T>(this ISesionActual sesion, Permiso permiso) =>
		_ => sesion.TienePermiso(permiso);
}
