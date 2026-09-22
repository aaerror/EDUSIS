using Domain.Divisiones;

namespace EDUSIS.TestSupport.Builders;

/// <summary>
/// Builder de <see cref="Division"/>. Estado por defecto válido: división "A" de un curso
/// arbitrario, sin preceptor asignado. <see cref="Build"/> usa el constructor público.
/// </summary>
public sealed class DivisionBuilder
{
	#region ESTADO POR DEFECTO
	private Guid _cursoID = Guid.NewGuid();
	private string _descripcion = "A";
	#endregion

	#region CONFIGURACIÓN
	public DivisionBuilder ConCurso(Guid cursoID)
	{
		_cursoID = cursoID;
		return this;
	}

	public DivisionBuilder ConDescripcion(string descripcion)
	{
		_descripcion = descripcion;
		return this;
	}
	#endregion

	#region CONSTRUCCIÓN
	public Division Build() =>
		new(_cursoID, _descripcion);
	#endregion
}
