namespace Domain.Curriculas.Exceptions;

public class LimiteEspaciosCurricularesException : Exception
{
	private const string ERROR = "Se alcanzó el límite de espacios curriculares de la currícula.";


	public LimiteEspaciosCurricularesException()
		: base(ERROR) {}
	public LimiteEspaciosCurricularesException(string parametro)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }")) {}
	public LimiteEspaciosCurricularesException(string parametro, Exception exception)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }"), exception) {}
}
