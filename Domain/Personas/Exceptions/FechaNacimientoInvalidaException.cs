namespace Domain.Personas.Exceptions;

internal class FechaNacimientoInvalidaException : Exception
{
    private const string ERROR = "La fecha de nacimiento es inválida.";


    public FechaNacimientoInvalidaException() : base() { }
    public FechaNacimientoInvalidaException(string parametro)
        : base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
    public FechaNacimientoInvalidaException(string parametro, Exception exception)
        : base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
