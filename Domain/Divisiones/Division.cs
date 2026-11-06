using Domain.Divisiones.Exceptions;
using Domain.Shared;
using System.Text.RegularExpressions;

namespace Domain.Divisiones;

public sealed class Division : Entity
{
	private const int MAX_ALUMNOS = 35;

	public Guid CursoID { get; private set; } = Guid.Empty;
	public string Descripcion { get; private set; } = string.Empty;
	public Guid? Preceptor { get; private set; } = null;


	#region CONSTRUCTOR
	private Division()
		: base() { }

	private Division(Guid divisionID)
		: base(divisionID) { }

	public Division(Guid unCurso, string descripcion)
		: this(Guid.NewGuid())
	{
		if (Guid.Empty.Equals(unCurso))
		{
			throw new SinDatosCursoException();
		}

		if (string.IsNullOrWhiteSpace(descripcion))
		{
			throw new ArgumentNullException(nameof(descripcion), "Datos incompletos del nombre de la división.");
		}

		if (!Regex.IsMatch(descripcion.Trim(), @"^([a-zA-Z]){1}$", RegexOptions.None))
		{
			throw new ArgumentException("La división del curso debe ser una letra.", nameof(descripcion));
		}

		CursoID = unCurso;
		Descripcion = descripcion.ToUpper().Trim();
	}

	/// <summary>
	/// Propone la división que sigue a la última del curso: "A" si no hay ninguna, y la letra
	/// siguiente a la mayor existente en los demás casos. El tope es "Z": pasada esa letra no hay
	/// descripción válida posible y lanza <c>SinLetrasDisponiblesException</c>.
	/// </summary>
	public static Division Siguiente(Guid cursoID, IEnumerable<string> descripcionesExistentes)
	{
		var ultima = descripcionesExistentes.OrderDescending().FirstOrDefault();
		if (ultima is null)
		{
			return new Division(cursoID, "A");
		}

		// El tope cierra el TODO "sin tope tras 'Z'": char.ConvertFromUtf32('Z' + 1) da "[", que el
		// constructor rechazaba con un ArgumentException genérico ("debe ser una letra") en vez de
		// nombrar la regla de negocio que se está violando.
		var ultimaLetra = char.ToUpperInvariant(char.Parse(ultima.Trim()));
		if (ultimaLetra >= 'Z')
		{
			throw new SinLetrasDisponiblesException();
		}

		return new Division(cursoID, char.ConvertFromUtf32(ultimaLetra + 1));
	}
	#endregion

	#region Preceptor
	public bool EstaCargoPreceptorVacante() => Preceptor is null;

	public void AsignarPreceptor(Guid unPreceptor)
	{
		if (!EstaCargoPreceptorVacante())
		{
			throw new CargoPreceptorNoDisponibleException();
		}

		if (Guid.Empty.Equals(unPreceptor))
		{
			throw new SinDatosPreceptorException();
		}

		Preceptor = unPreceptor;
	}

	/// <summary>
	/// Corrige quién es el preceptor de una división que ya tiene el cargo ocupado. No es
	/// <see cref="QuitarPreceptor"/> + <see cref="AsignarPreceptor"/>: el cargo nunca queda
	/// vacante, así que no hay un estado intermedio en el que la división pierda su preceptor.
	/// </summary>
	public void CambiarPreceptor(Guid unPreceptor)
	{
		if (EstaCargoPreceptorVacante())
		{
			throw new CargoPreceptorDisponibleException();
		}

		if (Guid.Empty.Equals(unPreceptor))
		{
			throw new SinDatosPreceptorException();
		}

		if (Preceptor.Equals(unPreceptor))
		{
			throw new PreceptorSinCambiosException();
		}

		Preceptor = unPreceptor;
	}

	public void QuitarPreceptor()
	{
		if (EstaCargoPreceptorVacante())
		{
			throw new CargoPreceptorDisponibleException();
		}

		Preceptor = null;
	}
	#endregion

	#region Validaciones
	public void ValidarCupoDisponible(int cantidadActualDeCursantes)
	{
		if (cantidadActualDeCursantes >= MAX_ALUMNOS)
			throw new CupoMaximoAlcanzadoException();
	}

	// Acá vivía ValidarSePuedeEliminar(NivelEducativo, int): una división de Secundaria con
	// matrícula no se eliminaba nunca y una de Primaria sólo por debajo de MINIMO_MATRICULA_PRIMARIA
	// (15). La regla se quitó el 2026-11-06 por decisión de negocio, no por limpieza: eliminar una
	// división pasó a ser borrado físico que cascadea su matrícula —cátedras, planillas, cursantes y
	// sus calificaciones—, informada por el diálogo de confirmación de
	// ServicioDivision.ConsultarImpactoEliminacionDivisionAsync.
	//
	// El requerimiento que la sostenía (RF-02 de docs/uml/requerimientos-funcionales.md, y la regla
	// de CU-S11 en casos-uso-secretaria.md) se reescribió en la misma pasada. Vale la pena saberlo
	// antes de "restaurar" la guarda: ese RF ya había sido corregido el 2026-11-03 EN LA DIRECCIÓN
	// CONTRARIA, para alinearlo con esta invariante, así que el texto que hoy está en docs/ es el
	// segundo que describe esta operación. DivisionNoEliminableException y
	// DivisionConMatriculaSuficienteException se borraron con ella.
	#endregion
}