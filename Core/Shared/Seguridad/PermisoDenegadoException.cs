namespace Core.Shared.Seguridad;

internal class PermisoDenegadoException : Exception
{
	private const string ERROR = "El usuario no tiene permiso para realizar esta operación.";


	public PermisoDenegadoException()
		: base(ERROR) { }
	public PermisoDenegadoException(string parametro)
		: base($"{ERROR} Parámetro: {parametro}") { }
	public PermisoDenegadoException(string parametro, Exception exception)
		: base($"{ERROR} Parámetro: {parametro}", exception) { }
}
