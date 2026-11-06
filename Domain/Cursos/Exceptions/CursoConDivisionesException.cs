namespace Domain.Cursos.Exceptions;

public class CursoConDivisionesException : Exception
{
	private const string ERROR = "El curso tiene divisiones y no se puede eliminar. Eliminar primero sus divisiones.";


	public CursoConDivisionesException()
		: base(ERROR) { }
	public CursoConDivisionesException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public CursoConDivisionesException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
