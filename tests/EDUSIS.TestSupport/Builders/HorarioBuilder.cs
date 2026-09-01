using Domain.Curriculas.Materias.Horarios;

namespace EDUSIS.TestSupport.Builders;

/// <summary>
/// Builder del value object <see cref="Horario"/>. Estado por defecto válido: turno
/// <see cref="Turno.Mañana"/>, día <see cref="Dia.Lunes"/>, inicio 08:00 y hora cátedra de 40
/// minutos. <see cref="Build"/> usa la factoría pública <see cref="Horario.Crear"/>.
/// </summary>
/// <remarks>
/// Invariantes del constructor: el día no puede ser sábado ni domingo, la hora de inicio debe
/// caer dentro de los límites del turno y la duración debe ser múltiplo de 5 y ≥ 30 minutos.
/// </remarks>
public sealed class HorarioBuilder
{
	#region ESTADO POR DEFECTO
	private Turno _turno = Turno.Mañana;
	private Dia _diaSemana = Dia.Lunes;
	private TimeOnly _horaInicio = new(8, 0);
	private int _duracionHoraCatedra = 40;
	#endregion

	#region CONFIGURACIÓN
	public HorarioBuilder ConTurno(Turno turno)
	{
		_turno = turno;
		return this;
	}

	public HorarioBuilder ConDia(Dia diaSemana)
	{
		_diaSemana = diaSemana;
		return this;
	}

	public HorarioBuilder ConHoraInicio(TimeOnly horaInicio)
	{
		_horaInicio = horaInicio;
		return this;
	}

	public HorarioBuilder ConDuracionHoraCatedra(int minutos)
	{
		_duracionHoraCatedra = minutos;
		return this;
	}
	#endregion

	#region CONSTRUCCIÓN
	public Horario Build() =>
		Horario.Crear(_turno, _diaSemana, _horaInicio, _duracionHoraCatedra);
	#endregion
}
