using Domain.Curriculas.Materias.CargosDocentes;

namespace EDUSIS.TestSupport.Builders;

/// <summary>
/// Builder de <see cref="SituacionRevista"/> (el cargo docente sobre una materia). Estado por
/// defecto válido: cargo <see cref="Cargo.Titular"/> por tiempo indeterminado, sin funciones de
/// aula. <c>Cargo</c> es un <c>enum</c> del dominio y no necesita builder propio.
/// </summary>
/// <remarks>
/// Ojo con las invariantes del constructor: un cargo <see cref="Cargo.Interino"/> o
/// <see cref="Cargo.Suplente"/> exige fecha de fin; <see cref="EnFunciones"/> exige período
/// indeterminado y vigente.
/// </remarks>
public sealed class SituacionRevistaBuilder
{
	#region ESTADO POR DEFECTO
	private Guid _materiaID = Guid.NewGuid();
	private Guid _docenteID = Guid.NewGuid();
	private Cargo _cargo = Cargo.Titular;
	private DateTime _fechaInicio = DateTime.Today;
	private DateTime? _fechaFin = null;
	private bool _enFunciones = false;
	#endregion

	#region CONFIGURACIÓN
	public SituacionRevistaBuilder ConMateria(Guid materiaID)
	{
		_materiaID = materiaID;
		return this;
	}

	public SituacionRevistaBuilder ConDocente(Guid docenteID)
	{
		_docenteID = docenteID;
		return this;
	}

	public SituacionRevistaBuilder ConCargo(Cargo cargo)
	{
		_cargo = cargo;
		return this;
	}

	public SituacionRevistaBuilder ConFechaInicio(DateTime fechaInicio)
	{
		_fechaInicio = fechaInicio;
		return this;
	}

	public SituacionRevistaBuilder ConFechaFin(DateTime? fechaFin)
	{
		_fechaFin = fechaFin;
		return this;
	}

	public SituacionRevistaBuilder EnFunciones(bool enFunciones = true)
	{
		_enFunciones = enFunciones;
		return this;
	}
	#endregion

	#region CONSTRUCCIÓN
	public SituacionRevista Build() =>
		new(_materiaID, _docenteID, _cargo, _fechaInicio, _fechaFin, _enFunciones);
	#endregion
}
