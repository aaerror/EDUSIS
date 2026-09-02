namespace Domain.Curriculas.Exceptions;

internal class MateriaNoEncontradaException : Exception
{
	private const string ERROR = "No se encontró la materia en la currícula.";


	public MateriaNoEncontradaException()
		: base(ERROR) { }
	public MateriaNoEncontradaException(string? parametro)
		: base(string.Format($"{ERROR}. ({parametro})")) { }
	public MateriaNoEncontradaException(string? parametro, Exception exception)
		: base(string.Format($"{ERROR}. ({parametro})"), exception) { }
}