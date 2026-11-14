namespace Domain.Personas.Exceptions;

public class FormatoTelefonoInvalidoException : Exception
{
    private const string ERROR = "El formato de teléfono es inválido .";


    public FormatoTelefonoInvalidoException()
        : base(ERROR) {}
    public FormatoTelefonoInvalidoException(string parametro)
        : base(string.Format($"{ERROR}. Parámetro: {parametro}")) {}
    public FormatoTelefonoInvalidoException(string parametro, Exception exception)
        : base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) {}
}
