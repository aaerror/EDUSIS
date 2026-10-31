namespace Domain.Curriculas.Exceptions;

public class CurriculaNoEncontradaException : Exception
{
	private const string ERROR = "No se encontró la currícula del curso.";


	public CurriculaNoEncontradaException()
		: base(ERROR) {}
	public CurriculaNoEncontradaException(string parametro)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }")) {}
	public CurriculaNoEncontradaException(string parametro, Exception exception)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }"), exception) {}
}
