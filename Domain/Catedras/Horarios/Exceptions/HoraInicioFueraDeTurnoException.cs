namespace Domain.Catedras.Horarios.Exceptions;

public class HoraInicioFueraDeTurnoException : Exception
{
	private const string ERROR = "La hora de inicio no corresponde a la franja del turno.";


	public HoraInicioFueraDeTurnoException() : base(ERROR) { }
	public HoraInicioFueraDeTurnoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public HoraInicioFueraDeTurnoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
