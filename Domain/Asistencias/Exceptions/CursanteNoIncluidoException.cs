namespace Domain.Asistencias.Exceptions;

internal class CursanteNoIncluidoException : Exception
{
	private const string ERROR = "El cursante no está incluido en la planilla.";


	public CursanteNoIncluidoException()
		: base(ERROR) { }
	public CursanteNoIncluidoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public CursanteNoIncluidoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
