using Domain.Shared;

namespace EDUSIS.TestSupport;

/// <summary>
/// Reloj con una fecha fija para pruebas deterministas de reglas de vigencia.
/// Uso: <c>using var _ = RelojFijo.En(new DateTime(2026, 3, 1));</c>
/// </summary>
public sealed class RelojFijo : IReloj
{
	public DateTime Hoy { get; }

	public RelojFijo(DateTime hoy) => Hoy = hoy.Date;

	public static IDisposable En(DateTime hoy) =>
		Reloj.Fijar(new RelojFijo(hoy));
}
