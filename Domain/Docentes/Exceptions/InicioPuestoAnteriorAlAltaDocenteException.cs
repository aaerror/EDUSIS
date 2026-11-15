namespace Domain.Docentes.Exceptions;

public class InicioPuestoAnteriorAlAltaDocenteException : Exception
{
	private const string ERROR = "La fecha de inicio en el cargo docente debe ser la misma a la fecha de alta del docente en la institución o posterior.";


	public InicioPuestoAnteriorAlAltaDocenteException() : base(ERROR) { }
	public InicioPuestoAnteriorAlAltaDocenteException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public InicioPuestoAnteriorAlAltaDocenteException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
