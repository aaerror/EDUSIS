namespace Domain.Catedras.Horarios.Exceptions;

internal class HorarioDuplicadoException : Exception
{
	private const string ERROR = "El horario ya se encuentra asignado.";


	public HorarioDuplicadoException()
		: base(ERROR) { }
	public HorarioDuplicadoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public HorarioDuplicadoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
