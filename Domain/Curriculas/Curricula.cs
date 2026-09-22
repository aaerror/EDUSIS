using Domain.Curriculas.Exceptions;
using Domain.Shared;

namespace Domain.Curriculas;

public sealed class Curricula : Entity
{
	public Guid CursoID { get; private set; } = Guid.Empty;
	public RangoFechas Periodo { get; private set; }


	#region CONSTRUCTOR
	private Curricula() {}

	private Curricula(Guid curriculaID)
		: base(curriculaID) {}

	private Curricula(
		Guid curriculaID,
		Guid unCurso,
		DateTime fechaInicio,
		DateTime? fechaFin)
			: this(curriculaID)
	{
		CursoID = unCurso;
		Periodo = fechaFin.HasValue ? RangoFechas.Create(fechaInicio, fechaFin.Value) : RangoFechas.Create(fechaInicio);
	}

	public Curricula(Guid unCurso, DateTime fechaInicio, DateTime? fechaFin)
		: this(Guid.NewGuid(), unCurso, fechaInicio, fechaFin) { }
	#endregion

	public bool EstaVigente() =>
		Periodo.EstaVigente();

	public void Desafectar() =>
		EstablecerFechaFinalizacion(DateTime.Today);

	public void EstablecerFechaFinalizacion(DateTime fechaFin)
	{
		if (!Periodo.EstaVigente())
		{
			throw new CurriculaNoVigenteException();
		}

		Periodo = Periodo.ActualizarFechaFin(fechaFin);
	}
}
