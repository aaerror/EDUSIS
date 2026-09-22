namespace Domain.Asistencias.Exceptions;

internal class CursanteYaIncluidoException : Exception
{
	private const string ERROR = "El cursante ya está incluido en la planilla.";


	public CursanteYaIncluidoException()
		: base(ERROR) { }
	public CursanteYaIncluidoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public CursanteYaIncluidoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
