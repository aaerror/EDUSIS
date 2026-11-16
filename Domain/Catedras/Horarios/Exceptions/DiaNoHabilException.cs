namespace Domain.Catedras.Horarios.Exceptions;

public class DiaNoHabilException : Exception
{
	private const string ERROR = "El horario debe asignarse a un día hábil (lunes a viernes).";


	public DiaNoHabilException() : base(ERROR) { }
	public DiaNoHabilException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public DiaNoHabilException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
