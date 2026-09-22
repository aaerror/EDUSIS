using System.Linq;
using Domain.Catedras.SituacionesRevista;

namespace EDUSIS.TestSupport.Builders;

/// <summary>
/// Builder de <see cref="SituacionRevista"/> (el cargo docente sobre una cátedra). El
/// constructor de <see cref="SituacionRevista"/> es <c>internal</c> — sólo
/// <see cref="Domain.Catedras.Catedra"/> instancia situaciones de revista —, así que este
/// builder arma una <see cref="Domain.Catedras.Catedra"/> auxiliar con <see cref="CatedraBuilder"/>
/// y designa sobre ella. Estado por defecto válido: cargo <see cref="Cargo.Titular"/> por tiempo
/// indeterminado.
/// </summary>
/// <remarks>
/// Ojo con las invariantes del constructor: un cargo <see cref="Cargo.Interino"/> o
/// <see cref="Cargo.Suplente"/> exige fecha de fin.
/// </remarks>
public sealed class SituacionRevistaBuilder
{
	#region ESTADO POR DEFECTO
	private Guid _docenteID = Guid.NewGuid();
	private Cargo _cargo = Cargo.Titular;
	private DateTime _fechaInicio = DateTime.Today;
	private DateTime? _fechaFin = null;
	#endregion

	#region CONFIGURACIÓN
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
	#endregion

	#region CONSTRUCCIÓN
	public SituacionRevista Build()
	{
		var catedra = new CatedraBuilder().Build();

		// Una suplencia siempre reemplaza a una designación vigente: la cátedra necesita un ocupante previo.
		Guid? reemplazaA = _cargo.Equals(Cargo.Suplente)
			? catedra.Designar(Guid.NewGuid(), Cargo.Titular, DateTime.Today.AddYears(-1), null)
			: null;

		var situacionRevistaID = catedra.Designar(_docenteID, _cargo, _fechaInicio, _fechaFin, reemplazaA);
		return catedra.SituacionesRevista.Single(situacion => situacion.Id.Equals(situacionRevistaID));
	}
	#endregion
}
