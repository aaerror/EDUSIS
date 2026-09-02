namespace Domain.Alumnos.Exceptions;

internal class AlumnoMayorDeEdadException : Exception
{
    private const string ERROR = "El alumno cumplió la mayoría de edad.";


    public AlumnoMayorDeEdadException()
        : base() { }
    public AlumnoMayorDeEdadException(string parametro)
        : base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
    public AlumnoMayorDeEdadException(string parametro, Exception exception)
        : base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}