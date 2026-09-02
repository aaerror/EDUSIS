namespace Domain.Personas.Exceptions;

internal class FormatoTelefonoInvalidoException : Exception
{
    private const string ERROR = "El formato de teléfono es inválido .";


    public FormatoTelefonoInvalidoException()
        : base() {}
    public FormatoTelefonoInvalidoException(string parametro)
        : base(string.Format($"{ERROR}. Parámetro: {parametro}")) {}
    public FormatoTelefonoInvalidoException(string parametro, Exception exception)
        : base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) {}
}
