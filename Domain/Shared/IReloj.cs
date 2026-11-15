namespace Domain.Shared;

/// <summary>
/// Fuente de la fecha actual. Las reglas del dominio la leen a través de <see cref="Reloj"/>.
/// </summary>
public interface IReloj
{
	/// <summary>
	/// Fecha actual, sin hora.
	/// </summary>
	DateTime Hoy { get; }
}
