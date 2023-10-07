namespace Domain.Cursos.Exceptions;

public class FinDeSemanaException : Exception
{
    private const string ERROR = "El día es fin de semana.";


    public FinDeSemanaException() : base() { }
    public FinDeSemanaException(string parametro)
        : base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
    public FinDeSemanaException(string parametro, Exception exception)
        : base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
