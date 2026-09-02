namespace Domain.Shared.Exceptions;

internal class FechasInconsistentesException : Exception
{
    private const string ERROR = "Existen incoherencias entre las fechas.";


    public FechasInconsistentesException() : base() { }
    public FechasInconsistentesException(string parametro)
        : base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
    public FechasInconsistentesException(string parametro, Exception exception)
        : base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}