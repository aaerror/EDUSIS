namespace Core.ServicioDocentes.Exceptions;

public class DocumentoDocenteDuplicadoException : Exception
{
	private const string ERROR = "El D.N.I. del docente ya se encuentra registrado.";


	public DocumentoDocenteDuplicadoException() : base(ERROR) { }
}
