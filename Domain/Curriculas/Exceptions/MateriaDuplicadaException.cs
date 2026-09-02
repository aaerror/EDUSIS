namespace Domain.Curriculas.Exceptions;

internal class MateriaDuplicadaException : Exception
{
	private const string ERROR = "Materia ya registrada en la currícula.";


	public MateriaDuplicadaException()
		: base(ERROR) { }
	public MateriaDuplicadaException(string? parametro)
		: base(string.Format($"{ERROR}. ({parametro})")) { }
	public MateriaDuplicadaException(string? parametro, Exception exception)
		: base(string.Format($"{ERROR}. ({parametro})"), exception) { }
}