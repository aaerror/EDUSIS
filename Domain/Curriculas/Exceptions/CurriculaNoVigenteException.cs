namespace Domain.Curriculas.Exceptions;

internal class CurriculaNoVigenteException : Exception
{
	private const string ERROR = "La currícula no se encuentra vigente.";


	public CurriculaNoVigenteException()
		: base(ERROR) {}
	public CurriculaNoVigenteException(string parametro)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }")) {}
	public CurriculaNoVigenteException(string parametro, Exception exception)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }"), exception) {}
}