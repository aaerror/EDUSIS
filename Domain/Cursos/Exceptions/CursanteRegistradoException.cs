namespace Domain.Cursos.Exceptions;

internal class CursanteRegistradoException : Exception
{
	private const string ERROR = "El cursante ya se encuentra registrado.";


	public CursanteRegistradoException()
		: base(ERROR) { }
	public CursanteRegistradoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public CursanteRegistradoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}