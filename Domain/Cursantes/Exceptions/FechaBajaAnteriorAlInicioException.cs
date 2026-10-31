namespace Domain.Cursantes.Exceptions;

public class FechaBajaAnteriorAlInicioException : Exception
{
	private const string ERROR = "La fecha de baja no puede ser anterior a la fecha de inicio de la cursada.";


	public FechaBajaAnteriorAlInicioException()
		: base(ERROR) { }
	public FechaBajaAnteriorAlInicioException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public FechaBajaAnteriorAlInicioException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
