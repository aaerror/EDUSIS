namespace Core.ServicioMaterias.DTOs.Responses;

public record HorarioResponse
{
    public string Turno { get; set; }
    public string DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }


    public HorarioResponse(string turno, string diaSemana, TimeOnly horaInicio, TimeOnly horaFin)
    {
        Turno = turno;
        DiaSemana = diaSemana;
        HoraInicio = horaInicio;
        HoraFin = horaFin;
    }
}
