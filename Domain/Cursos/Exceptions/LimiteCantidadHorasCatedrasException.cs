namespace Domain.Cursos.Exceptions;

public class LimiteCantidadHorasCatedrasException : Exception
{
    private const string ERROR = "Límite de cantidad de horas cátedras alcanzado.";


    public LimiteCantidadHorasCatedrasException() : base() { }
    public LimiteCantidadHorasCatedrasException(string parametro)
        : base(string.Format($"{ ERROR }. Parámetro: { parametro }")) { }
    public LimiteCantidadHorasCatedrasException(string parametro, Exception exception)
        : base(string.Format($"{ ERROR }. Parámetro: { parametro }"), exception) { }

}
