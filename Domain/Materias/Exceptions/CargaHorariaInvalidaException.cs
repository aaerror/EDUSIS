namespace Domain.Materias.Exceptions;

public class CargaHorariaInvalidaException : Exception
{
	private const string ERROR = "La materia debe tener al menos una hora cátedra.";


	public CargaHorariaInvalidaException()
		: base(ERROR) {}
	public CargaHorariaInvalidaException(string parametro)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }")) {}
	public CargaHorariaInvalidaException(string parametro, Exception exception)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }"), exception) {}
}
