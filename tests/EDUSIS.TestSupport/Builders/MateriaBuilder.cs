using Domain.Curriculas.Materias;

namespace EDUSIS.TestSupport.Builders;

/// <summary>
/// Builder de <see cref="Materia"/> suelta (sin pasar por <see cref="Domain.Curriculas.Curricula"/>).
/// Estado por defecto válido: <c>Matemática</c> con 4 horas cátedra. La <c>Materia</c> resultante
/// llega con el evento <c>MateriaRegistradaEvent</c> encolado, como en producción.
/// </summary>
public sealed class MateriaBuilder
{
	#region ESTADO POR DEFECTO
	private Guid _curriculaID = Guid.NewGuid();
	private string _descripcion = "Matemática";
	private int _horasCatedra = 4;
	#endregion

	#region CONFIGURACIÓN
	public MateriaBuilder ConCurricula(Guid curriculaID)
	{
		_curriculaID = curriculaID;
		return this;
	}

	public MateriaBuilder ConDescripcion(string descripcion)
	{
		_descripcion = descripcion;
		return this;
	}

	public MateriaBuilder ConHorasCatedra(int horasCatedra)
	{
		_horasCatedra = horasCatedra;
		return this;
	}
	#endregion

	#region CONSTRUCCIÓN
	public Materia Build() =>
		new(_curriculaID, _descripcion, _horasCatedra);
	#endregion
}
