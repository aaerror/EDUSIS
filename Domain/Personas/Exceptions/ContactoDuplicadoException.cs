namespace Domain.Personas.Exceptions;

public class ContactoDuplicadoException : Exception
{
    private const string ERROR = "El contacto ya se encuentra almacenado.";


    public ContactoDuplicadoException() : base() { }
    public ContactoDuplicadoException(string parametro)
        : base(string.Format($"{ ERROR }. Parámetro: { parametro }")) { }
    public ContactoDuplicadoException(string parametro, Exception exception)
        : base(string.Format($"{ ERROR }. Parámetro: { parametro }"), exception) { }
}
