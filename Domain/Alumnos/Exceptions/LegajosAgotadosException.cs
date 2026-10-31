namespace Domain.Alumnos.Exceptions;

public class LegajosAgotadosException : Exception
{
	private const string ERROR = "Se agotaron los legajos disponibles para los alumnos.";


	public LegajosAgotadosException()
		: base(ERROR) { }
	public LegajosAgotadosException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public LegajosAgotadosException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
