using Domain.Cursos.Exceptions;
using Domain.Shared;

namespace Domain.Cursos.Materias;

public class Horario : ValueObject
{
    private TimeOnly _limiteHorarioInicio;
    private TimeOnly _limiteHorarioFin;

    public Turno Turno { get; private set; }
    public Dia DiaSemana { get; private set; }
    public TimeOnly HoraInicio { get; private set; }
    public TimeOnly HoraFin { get; private set; }


    private Horario() { }
    private Horario(Turno turno, Dia diaSemana, TimeOnly horaInicio, int duracionHoraCatedra)
    {
        if (diaSemana.Equals(Dia.Sabado) || diaSemana.Equals(Dia.Domingo))
        {
            throw new FinDeSemanaException(nameof(diaSemana));
        }

        DiaSemana = diaSemana;
        Turno = turno;
        EstablecerLimitesHorario();
        if (horaInicio < _limiteHorarioInicio || horaInicio > _limiteHorarioFin)
        {
            throw new ArgumentException($"El horario de inicio corresponde a otro turno ({ Turno }).", nameof(horaInicio));
        }

        if (duracionHoraCatedra % 5 != 0)
        {
            throw new DuracionHoraCatedraException(nameof(duracionHoraCatedra));
        }

        if (duracionHoraCatedra < 30)
        {
            throw new ArgumentException($"La duración de la hora cátedra no puede ser menor a 30 minutos.", nameof(duracionHoraCatedra));
        }

        HoraInicio = horaInicio;
        HoraFin = HoraInicio.AddMinutes(40);
    }

    public static Horario Crear(Turno turno, Dia diaSemana, TimeOnly horaInicio, int duracionHoraCatedra)
    {
        return new(turno, diaSemana, horaInicio, duracionHoraCatedra);
    }

    public int DuracionHoraCatedra() => (HoraInicio - HoraFin).Minutes;

    private void EstablecerLimitesHorario()
    {
        switch (Turno)
        {
            case Turno.Mañana:
                _limiteHorarioInicio = new TimeOnly(07, 00);
                _limiteHorarioFin = new TimeOnly(13, 00);
                break;
            case Turno.Tarde:
                _limiteHorarioInicio = new TimeOnly(13, 00);
                _limiteHorarioFin = new TimeOnly(18, 00);
                break;
            case Turno.Noche:
                _limiteHorarioInicio = new TimeOnly(18, 00);
                _limiteHorarioFin = new TimeOnly(00, 00);                break;
            default:
                throw new ArgumentException("Error al establecer los límites del horario para el turno seleccionado.", nameof(Turno));
        }
    }

    public override IEnumerable<object> GetEqualityCommponents()
    {
        yield return Turno;
        yield return DiaSemana;
        yield return HoraInicio;
        yield return HoraFin;
    }
}
