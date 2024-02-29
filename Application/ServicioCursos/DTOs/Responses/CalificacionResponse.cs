namespace Core.ServicioCursos.DTOs.Responses;

public record CalificacionResponse
{
    public Guid MateriaID { get; init; }
    public string Materia { get; init; }
    public bool Asistencia { get; init; }
    public DateTime? Fecha { get; init; }
    public int Instancia { get; init; }
    public double? Nota { get; init; }


    public CalificacionResponse(Guid materiaID, string materia, bool asistencia, DateTime? fecha, int instancia, double? nota)
    {
        MateriaID = materiaID;
        Materia = materia;
        Asistencia = asistencia;
        Fecha = fecha;
        Instancia = instancia;
        Nota = nota;
    }
}
