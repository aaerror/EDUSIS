namespace Core.ServicioMaterias.DTOs.Requests;

public record RegistrarHorarioEnMateria
{
    public Guid CursoID { get; set; }
    public Guid MateriaID { get; set; }
    public int Turno { get; set; }
    public int DiaSemana { get; set; }
    public string HoraInicio { get; set; }
    public int Duracion { get; set; }


    public RegistrarHorarioEnMateria(Guid cursoID, Guid materiaID, int turno, int diaSemana, string horaInicio, int duracion)
    {
        CursoID = cursoID;
        MateriaID = materiaID;
        Turno = turno;
        DiaSemana = diaSemana;
        HoraInicio = horaInicio;
        Duracion = duracion;
    }
}
