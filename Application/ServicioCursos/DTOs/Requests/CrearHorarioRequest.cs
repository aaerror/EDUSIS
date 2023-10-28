namespace Core.ServicioCursos.DTOs.Requests;

public record CrearHorarioRequest
{
    public Guid Curso {  get; set; }
    public Guid Materia { get; set; }
    public int Turno { get; set; }
    public int DiaSemana { get; set; }
    public string HoraInicio { get; set; }
    public int Duracion { get; set; }


    public CrearHorarioRequest(Guid curso, Guid materia, int turno, int diaSemana, string horaInicio, int duracion)
    {
        Curso = curso;
        Materia = materia;
        Turno = turno;
        DiaSemana = diaSemana;
        HoraInicio = horaInicio;
        Duracion = duracion;
    }
}
