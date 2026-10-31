namespace Domain.Materias.Exceptions;

public class MateriaNoEncontradaException : Exception
{
	private const string ERROR = "No se encontró la materia de la currícula.";


	public MateriaNoEncontradaException()
		: base(ERROR) {}
	public MateriaNoEncontradaException(string parametro)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }")) {}
	public MateriaNoEncontradaException(string parametro, Exception exception)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }"), exception) {}
}
