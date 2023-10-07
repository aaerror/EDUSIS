namespace Domain.Personas.Exceptions;

public class FormatoInvalidoException : Exception
{
    private const string ERROR = "El formato es inválido o no reconocido.";


    public FormatoInvalidoException() : base() { }
    public FormatoInvalidoException(string mensaje) : base(string.Format($"{ ERROR }\n{mensaje}")) { }
    public FormatoInvalidoException(string mensaje, Exception exception) : base(string.Format($"{ ERROR }\n{ mensaje }"), exception) { }
}
