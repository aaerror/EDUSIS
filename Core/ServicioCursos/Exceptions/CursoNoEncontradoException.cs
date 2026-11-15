namespace Core.ServicioCursos.Exceptions;

public class CursoNoEncontradoException : Exception
{
	private const string ERROR = "No se encontró el curso.";


	public CursoNoEncontradoException()
		: base(ERROR) {}
	public CursoNoEncontradoException(string parametro)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }")) {}
	public CursoNoEncontradoException(string parametro, Exception exception)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }"), exception) {}
}
