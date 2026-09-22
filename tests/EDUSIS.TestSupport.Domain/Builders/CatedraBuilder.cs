using Domain.Catedras;

namespace EDUSIS.TestSupport.Builders;

/// <summary>
/// Builder de <see cref="Catedra"/> (dictado de una materia en una división). Estado por
/// defecto válido: materia y división arbitrarias con 4 horas cátedra, sin designaciones ni
/// horarios. <see cref="Build"/> usa el constructor público, que sólo toma identificadores y la
/// carga horaria como valor: <c>Catedra</c> no conoce el tipo <c>Materia</c>.
/// </summary>
public sealed class CatedraBuilder
{
	#region ESTADO POR DEFECTO
	private Guid _materiaID = Guid.NewGuid();
	private Guid _divisionID = Guid.NewGuid();
	private int _cargaHoraria = 4;
	#endregion

	#region CONFIGURACIÓN
	public CatedraBuilder ConMateria(Guid materiaID)
	{
		_materiaID = materiaID;
		return this;
	}

	public CatedraBuilder ConDivision(Guid divisionID)
	{
		_divisionID = divisionID;
		return this;
	}

	public CatedraBuilder ConCargaHoraria(int cargaHoraria)
	{
		_cargaHoraria = cargaHoraria;
		return this;
	}
	#endregion

	#region CONSTRUCCIÓN
	public Catedra Build() =>
		new(_materiaID, _divisionID, _cargaHoraria);
	#endregion
}
