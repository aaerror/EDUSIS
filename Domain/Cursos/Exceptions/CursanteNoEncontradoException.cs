namespace Domain.Cursos.Exceptions;

internal class CursanteNoEncontradoException : Exception
{
	private const string ERROR = "El cursante no esta registrado en esta división.";


	public CursanteNoEncontradoException()
		: base(ERROR) { }
	public CursanteNoEncontradoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public CursanteNoEncontradoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}