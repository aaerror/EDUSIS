namespace Domain.Cursantes.Exceptions;

public class CursanteNoEncontradoException : Exception
{
	private const string ERROR = "No se encontró la inscripción del cursante.";


	public CursanteNoEncontradoException()
		: base(ERROR) {}
	public CursanteNoEncontradoException(string parametro)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }")) {}
	public CursanteNoEncontradoException(string parametro, Exception exception)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }"), exception) {}
}
