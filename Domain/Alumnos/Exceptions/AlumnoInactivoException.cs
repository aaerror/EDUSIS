namespace Domain.Alumnos.Exceptions;

internal class AlumnoInactivoException : Exception
{
    private const string ERROR = "El alumno se encuentra inactivo.";


    public AlumnoInactivoException() : base() { }
    public AlumnoInactivoException(string parametro)
        : base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
    public AlumnoInactivoException(string parametro, Exception exception)
        : base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}