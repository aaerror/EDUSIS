using Core.Shared.Seguridad;
using Domain.Usuarios;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WPF_Desktop.UnitTests.Dobles;

/// <summary>
/// Sesión para construir ViewModels en las pruebas. Sin argumentos concede todos los permisos —las
/// pruebas de comportamiento no quieren comandos apagados por permiso—; con una lista concede sólo
/// esos, para afirmar que un comando nace gris.
/// </summary>
internal sealed class SesionFalsa : ISesionActual
{
	private readonly HashSet<Permiso>? _permisos;

	public SesionFalsa() { }

	public SesionFalsa(params Permiso[] permisos) =>
		_permisos = permisos.ToHashSet();

	public Guid? UsuarioID { get; } = Guid.NewGuid();

	public bool EstaAutenticado => true;

	public bool TienePermiso(Permiso unPermiso) =>
		_permisos is null || _permisos.Contains(unPermiso);
}
