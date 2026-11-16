namespace Domain.Catedras.Horarios.Exceptions;

public class DuracionHoraCatedraMinimaException : Exception
{
	private const string ERROR = "La duración de la hora cátedra no puede ser menor a 30 minutos.";


	public DuracionHoraCatedraMinimaException() : base(ERROR) { }
	public DuracionHoraCatedraMinimaException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public DuracionHoraCatedraMinimaException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
