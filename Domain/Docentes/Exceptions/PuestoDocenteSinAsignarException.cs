namespace Domain.Docentes.Exceptions;

internal class PuestoDocenteSinAsignarException : Exception
{
	private const string ERROR = "El puesto docente no se encuentra asignado al docente.";


	public PuestoDocenteSinAsignarException() : base() { }
	public PuestoDocenteSinAsignarException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public PuestoDocenteSinAsignarException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}