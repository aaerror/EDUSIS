namespace Domain.Curriculas.Exceptions;

public class LimiteHorasSemanalesException : Exception
{
	private const string ERROR = "Se alcanzó el límite de horas cátedra semanales de la currícula.";


	public LimiteHorasSemanalesException()
		: base(ERROR) {}
	public LimiteHorasSemanalesException(string parametro)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }")) {}
	public LimiteHorasSemanalesException(string parametro, Exception exception)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }"), exception) {}
}
