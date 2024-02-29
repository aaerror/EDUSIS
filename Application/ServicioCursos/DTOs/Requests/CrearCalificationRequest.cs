namespace Core.ServicioCursos.DTOs.Requests;

public record CrearCalificationRequest
{
    public Guid Curso { get; init; }
    public Guid Division { get; init; }
    public Guid Materia { get; init; }
    public Guid Alumno { get; init; }
    public int Instancia { get; init; }
    public double Nota { get; init; }


    public CrearCalificationRequest(Guid curso, Guid division, Guid materia, Guid alumno, int instancia, double nota)
    {
        Curso = curso;
        Division = division;
        Materia = materia;
        Alumno = alumno;
        Instancia = instancia;
        Nota = nota;
    }
}
