namespace Core.Shared;

/// <summary>
/// El porcentaje de asistencia, redondeado a un decimal.
/// </summary>
/// <remarks>
/// <para>
/// Estaba escrito como método privado de <c>ServicioAsistencia</c>, y el resumen académico del
/// alumno necesita exactamente la misma cuenta. Copiarla habría dejado dos fórmulas que pueden
/// divergir —la que informa el preceptor al cerrar la planilla y la que informa el perfil del
/// alumno— sobre los mismos registros. Es el mismo motivo por el que
/// <c>Calificacion.NOTA_APROBACION</c> es público en lugar de estar repetido en el boletín.
/// </para>
/// <para>
/// Vive en <c>Core.Shared</c> y no en <c>Domain</c> porque no es una invariante de
/// <c>RegistroAsistencia</c>: ninguna entidad calcula porcentajes. Es una proyección de lectura, y
/// las dos que la usan son servicios de aplicación del mismo assembly.
/// </para>
/// </remarks>
internal static class PorcentajeDeAsistencia
{
	/// <summary>
	/// Qué porcentaje de <paramref name="sobre"/> jornadas fueron asistidas.
	/// </summary>
	/// <remarks>
	/// Sin marcas devuelve cero: no hay período del que opinar, y un 100% sobre cero días diría que
	/// no se faltó nunca.
	///
	/// La tardanza cuenta como asistida y por eso no la descuenta ningún llamador. El alumno llegó
	/// tarde pero estuvo en clase, y un porcentaje que la descontara diría que faltó un día entero
	/// por cinco minutos. Los minutos quedan en el registro del día, que es donde significan algo.
	/// </remarks>
	public static decimal Calcular(int asistidas, int sobre)
	{
		if (sobre <= 0)
		{
			return decimal.Zero;
		}

		return Math.Round(asistidas * 100m / sobre, 1, MidpointRounding.AwayFromZero);
	}
}
