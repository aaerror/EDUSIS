namespace Domain.Docentes.Exceptions;

public class DocenteEnEdadJubilatoriaException : Exception
{
	private const string ERROR = "El docente se encuentra en edad jubilatoria.";


	public DocenteEnEdadJubilatoriaException() : base() { }
	public DocenteEnEdadJubilatoriaException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public DocenteEnEdadJubilatoriaException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}