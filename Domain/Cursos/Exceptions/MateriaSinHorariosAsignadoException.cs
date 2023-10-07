namespace Domain.Cursos.Exceptions;

public class MateriaSinHorariosAsignadoException : Exception
{
    private const string ERROR = "La materia no tiene horarios asignado.";


    public MateriaSinHorariosAsignadoException() : base() { }
    public MateriaSinHorariosAsignadoException(string? parametro)
        : base(string.Format($"{ ERROR }. ({ parametro })")) { }
    public MateriaSinHorariosAsignadoException(string? parametro, Exception exception)
        : base(string.Format($"{ ERROR }. ({ parametro })"), exception) { }
}
