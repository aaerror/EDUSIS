namespace Domain.Cursantes.Exceptions;

internal class AsistenciaRegistradaException : Exception
{
    private const string ERROR = "Ya se encuentra registrada la asistencia.";


    public AsistenciaRegistradaException() : base() { }
    public AsistenciaRegistradaException(string parametro)
        : base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
    public AsistenciaRegistradaException(string parametro, Exception exception)
        : base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }

}