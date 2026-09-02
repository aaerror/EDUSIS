namespace Domain.Personas.Exceptions;

internal class FormatoEmailInvalidoException : Exception
{
    private const string ERROR = "El formato de e-mail es inválido .";

    public FormatoEmailInvalidoException() : base() { }
    public FormatoEmailInvalidoException(string parametro)
        : base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
    public FormatoEmailInvalidoException(string parametro, Exception exception)
        : base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}