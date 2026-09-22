namespace Domain.Catedras.Exceptions;

internal class SinDatosMateriaException : Exception
{
	private const string ERROR = "Materia sin especificar.";


	public SinDatosMateriaException()
		: base(ERROR) { }
	public SinDatosMateriaException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public SinDatosMateriaException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
