namespace Domain.Licencias;

/// <summary>
/// Tipo de licencia docente según la normativa de la provincia de Córdoba: Ley 4356 (régimen de
/// licencias, reglamentada por el Decreto 1405/95), Ley 9905 (maternidad, paternidad y adopción) y
/// las leyes nacionales de licencia deportiva (20596) y de riesgos del trabajo (24557).
/// </summary>
/// <remarks>
/// El plazo, la norma y la denominación de cada valor están en <see cref="PlazoLegal"/>. Los nombres
/// se persisten como texto (columna <c>articulo</c>): renombrar un valor exige una migración que
/// reescriba las filas existentes.
/// </remarks>
public enum Articulo
{
	EnfermedadCortoTratamiento,
	EnfermedadLargoTratamiento,
	AtencionDeFamiliar,
	MatrimonioPropio,
	MatrimonioDeHijo,
	FallecimientoDeFamiliarDirecto,
	FallecimientoDeHermanoOSuegro,
	Examen,
	EstudioEncomendado,
	OtrasCausas,
	Maternidad,
	InterrupcionDelEmbarazo,
	Paternidad,
	Adopcion,
	Deportiva,
	AccidenteDeTrabajo
}
