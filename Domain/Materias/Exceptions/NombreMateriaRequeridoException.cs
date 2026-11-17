namespace Domain.Materias.Exceptions;

public class NombreMateriaRequeridoException : Exception
{
	private const string ERROR = "Se debe especificar el nombre de la materia.";


	public NombreMateriaRequeridoException()
		: base(ERROR) { }

	public NombreMateriaRequeridoException(string parametro)
		: base(string.Format($"{ERROR} Parámetro: {parametro}")) { }

	public NombreMateriaRequeridoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR} Parámetro: {parametro}"), exception) { }
}
