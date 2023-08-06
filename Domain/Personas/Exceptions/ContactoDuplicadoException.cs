namespace Domain.Personas.Exceptions;

public class ContactoDuplicadoException : Exception
{
    private const string ERROR = "El contacto ya se encuentra almacenado.";


    public ContactoDuplicadoException() : base() { }
    public ContactoDuplicadoException(string mensaje) : base(string.Format($"{ERROR}\n{mensaje}")) { }
    public ContactoDuplicadoException(string mensaje, Exception exception) : base(string.Format($"{ERROR}\n{mensaje}"), exception) { }
}
