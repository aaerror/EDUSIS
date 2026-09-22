namespace Domain.Catedras.Exceptions;

internal class HorarioNoAsignadoException : Exception
{
	private const string ERROR = "El horario no se encuentra asignado a la cátedra.";


	public HorarioNoAsignadoException()
		: base(ERROR) { }
	public HorarioNoAsignadoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public HorarioNoAsignadoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
