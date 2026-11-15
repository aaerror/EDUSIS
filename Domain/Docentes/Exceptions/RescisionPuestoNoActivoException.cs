namespace Domain.Docentes.Exceptions;

public class RescisionPuestoNoActivoException : Exception
{
	private const string ERROR = "Sólo se puede rescindir un puesto docente activo; un puesto pendiente se elimina.";


	public RescisionPuestoNoActivoException() : base(ERROR) { }
	public RescisionPuestoNoActivoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public RescisionPuestoNoActivoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
