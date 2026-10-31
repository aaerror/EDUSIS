namespace Domain.Materias.Exceptions;

public class SinDatosCurriculaException : Exception
{
	private const string ERROR = "Se debe especificar la currícula de la materia.";


	public SinDatosCurriculaException()
		: base(ERROR) {}
	public SinDatosCurriculaException(string parametro)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }")) {}
	public SinDatosCurriculaException(string parametro, Exception exception)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }"), exception) {}
}
