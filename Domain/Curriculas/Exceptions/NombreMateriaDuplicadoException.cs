namespace Domain.Curriculas.Exceptions;

internal class NombreMateriaDuplicadoException : Exception
{
	private const string ERROR = "Ya existe una materia registrada con ese nombre en la currícula.";


	public NombreMateriaDuplicadoException()
		: base(ERROR) { }
	public NombreMateriaDuplicadoException(string? parametro)
		: base(string.Format($"{ERROR}. ({parametro})")) { }
	public NombreMateriaDuplicadoException(string? parametro, Exception exception)
		: base(string.Format($"{ERROR}. ({parametro})"), exception) { }
}