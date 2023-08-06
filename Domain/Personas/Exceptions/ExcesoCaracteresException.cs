namespace Domain.Personas.Exceptions;

public class ExcesoCaracteresException : Exception
{
    private const string ERROR = "El texto excede el límite de caracteres permitidos.";


    public ExcesoCaracteresException() : base() { }
    public ExcesoCaracteresException(string mensaje) : base(string.Format($"{ERROR}\n{mensaje}")) { }
    public ExcesoCaracteresException(string mensaje, Exception exception) : base(string.Format($"{ERROR}\n{mensaje}"), exception) { }
}