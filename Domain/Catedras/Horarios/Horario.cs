using Domain.Catedras.Horarios.Exceptions;
using Domain.Shared;

namespace Domain.Catedras.Horarios;

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
			throw new DiaNoHabilException();
		}

		DiaSemana = diaSemana;
		Turno = turno;
		EstablecerLimitesHorario();
		if (horaInicio < _limiteHorarioInicio || horaInicio > _limiteHorarioFin)
		{
			throw new HoraInicioFueraDeTurnoException();
		}

		if (duracionHoraCatedra % 5 != 0)
		{
			throw new DuracionHoraCatedraException(nameof(duracionHoraCatedra));
		}

		if (duracionHoraCatedra < 30)
		{
			throw new DuracionHoraCatedraMinimaException();
		}

		HoraInicio = horaInicio;
		HoraFin = HoraInicio.AddMinutes(duracionHoraCatedra);
	}

	public static Horario Crear(Turno turno, Dia diaSemana, TimeOnly horaInicio, int duracionHoraCatedra)
	{
		return new(turno, diaSemana, horaInicio, duracionHoraCatedra);
	}

	/// <summary>
	/// Entrada desde la capa de aplicación: turno y día llegan como texto y su validez es regla
	/// del dominio.
	/// </summary>
	public static Horario Crear(string turno, string diaSemana, TimeOnly horaInicio, int duracionHoraCatedra) =>
		Crear(Enumerados.Convertir<Turno>(turno, valor => new TurnoInexistenteException(valor)),
			  Enumerados.Convertir<Dia>(diaSemana, valor => new DiaInexistenteException(valor)),
			  horaInicio,
			  duracionHoraCatedra);

	// TotalMinutes, no Minutes: Minutes es el componente de minutos del TimeSpan, así que una hora
	// cátedra de 60 minutos devolvía 0 y una de 80 devolvía 20. No se notaba porque la duración
	// máxima del enumerado DuracionHoraCatedra es 50, donde los dos valores coinciden.
	public int DuracionHoraCatedra() => (int)(HoraFin - HoraInicio).TotalMinutes;

	public bool SeSuperponeCon(Horario otro) =>
		DiaSemana.Equals(otro.DiaSemana) && HoraInicio < otro.HoraFin && otro.HoraInicio < HoraFin;

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
				_limiteHorarioFin = new TimeOnly(23, 00);
				break;
			default:
				throw new TurnoInexistenteException();
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
