namespace Core.ServicioCursos.DTOs.Responses;

public record HorarioResponse
{
    public string Turno { get; set; }
    public string DiaSemana { get; set; }
    public string HoraInicio { get; set; }
    public string HoraFin { get; set; }


    public HorarioResponse(string turno, string diaSemana, string horaInicio, string horaFin)
    {
        Turno = turno;
        DiaSemana = diaSemana;
        HoraInicio = horaInicio;
        HoraFin = horaFin;
    }
}
