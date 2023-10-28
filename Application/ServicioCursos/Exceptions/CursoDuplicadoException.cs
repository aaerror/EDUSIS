namespace Core.ServicioCursos.Exceptions;

public class CursoDuplicadoException : Exception
{
    private const string ERROR = "El curso ya se encuentra registrado.";


    public CursoDuplicadoException() : base(ERROR) { }
}
