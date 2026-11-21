namespace Domain.Licencias;

/// <summary>
/// Cómo limita la norma la duración de una licencia.
/// </summary>
public enum TipoDePlazo
{
	/// <summary>La norma fija los días: la licencia dura exactamente eso.</summary>
	Fijo,

	/// <summary>La norma fija un tope: la licencia dura hasta esos días.</summary>
	Maximo,

	/// <summary>La norma no fija duración: admite cualquier cantidad de días o ninguna fecha de fin.</summary>
	Abierto
}

/// <summary>
/// Plazo que la normativa asigna a cada <see cref="Articulo"/>, con su denominación y su norma de origen.
/// </summary>
/// <remarks>
/// <para>
/// Los plazos son por licencia, no anuales: los topes por año (60 días de corto tratamiento, 10 de
/// atención de familiar, 15 de exámenes) y los distintos para suplentes (Ley 9018) exigen mirar las
/// demás licencias del docente y su situación de revista, y no se controlan acá.
/// </para>
/// <para>
/// Los días se cuentan corridos (Ley 4356, art. 25): la fecha de fin es la de inicio más los días.
/// </para>
/// </remarks>
public sealed record PlazoLegal(TipoDePlazo Tipo, int Dias, string Denominacion, string Norma)
{
	/// <summary>Si la licencia puede quedar sin fecha de fin.</summary>
	public bool AdmiteIndefinida =>
		Tipo == TipoDePlazo.Abierto;

	// TODO: plazos tomados de la cartilla de Salud Laboral de UEPC; falta contrastarlos con el texto oficial de la Ley 4356, el Decreto 1405/95 y la Ley 9905.
	public static PlazoLegal De(Articulo articulo) => articulo switch
	{
		Articulo.EnfermedadCortoTratamiento => new(TipoDePlazo.Maximo, 60, "Enfermedad de corto tratamiento", "Ley 4356, art. 3"),
		Articulo.EnfermedadLargoTratamiento => new(TipoDePlazo.Maximo, 730, "Enfermedad de largo tratamiento", "Ley 4356, art. 3"),
		Articulo.AtencionDeFamiliar => new(TipoDePlazo.Maximo, 10, "Atención de familiar a cargo", "Ley 4356, art. 13"),
		Articulo.MatrimonioPropio => new(TipoDePlazo.Fijo, 15, "Matrimonio", "Ley 4356, art. 14 inc. a"),
		Articulo.MatrimonioDeHijo => new(TipoDePlazo.Fijo, 2, "Matrimonio de hijo", "Ley 4356, art. 14 inc. b"),
		Articulo.FallecimientoDeFamiliarDirecto => new(TipoDePlazo.Fijo, 12, "Fallecimiento de cónyuge, padre, madre o hijo", "Ley 4356, art. 14 inc. c"),
		Articulo.FallecimientoDeHermanoOSuegro => new(TipoDePlazo.Fijo, 6, "Fallecimiento de hermano o suegro", "Ley 4356, art. 14 inc. d"),
		// TODO: la norma cuenta los días de examen como hábiles; acá se cuentan corridos como el resto.
		Articulo.Examen => new(TipoDePlazo.Maximo, 15, "Examen", "Ley 4356, art. 15"),
		Articulo.EstudioEncomendado => new(TipoDePlazo.Abierto, 0, "Estudio encomendado por el Gobierno", "Ley 4356, art. 16"),
		Articulo.OtrasCausas => new(TipoDePlazo.Abierto, 0, "Otras causas", "Ley 4356, art. 19"),
		// 180 días en general; 280 por parto múltiple o hijo con discapacidad o enfermedad grave.
		Articulo.Maternidad => new(TipoDePlazo.Maximo, 280, "Maternidad", "Ley 9905"),
		Articulo.InterrupcionDelEmbarazo => new(TipoDePlazo.Fijo, 40, "Interrupción del embarazo", "Ley 9905"),
		// 8 días si convive con la madre, 2 si no; 14 por parto múltiple o hijo con discapacidad.
		Articulo.Paternidad => new(TipoDePlazo.Maximo, 14, "Paternidad", "Ley 9905"),
		// 100 días en general; 200 por adopción múltiple o de un niño con discapacidad.
		Articulo.Adopcion => new(TipoDePlazo.Maximo, 200, "Adopción", "Ley 9905"),
		Articulo.Deportiva => new(TipoDePlazo.Maximo, 60, "Deportiva", "Ley nacional 20596, art. 6"),
		Articulo.AccidenteDeTrabajo => new(TipoDePlazo.Abierto, 0, "Accidente de trabajo", "Ley nacional 24557"),
		_ => throw new ArgumentOutOfRangeException(nameof(articulo), articulo, "El artículo de licencia no tiene plazo legal asignado.")
	};
}
