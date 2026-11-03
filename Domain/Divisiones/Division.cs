using Domain.Divisiones.Exceptions;
using Domain.Shared;
using System.Text.RegularExpressions;

namespace Domain.Divisiones;

public sealed class Division : Entity
{
	private const int MAX_ALUMNOS = 35;
	private const int MINIMO_MATRICULA_PRIMARIA = 15;

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

	/// <summary>
	/// Una división vacía se elimina siempre, de cualquier nivel. Con cursantes, el nivel decide:
	/// en Secundaria no se elimina nunca y en Primaria se exige que la matrícula haya caído por
	/// debajo del mínimo que justifica mantener la división abierta.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <paramref name="cantidadCursantes"/> es la <em>matrícula vigente</em> del ciclo lectivo, no
	/// el total histórico de inscripciones: una división sin alumnos cursando llega acá con cero
	/// aunque arrastre inscripciones cerradas de ciclos anteriores. Esas las frena después una
	/// guarda de integridad de <c>ServicioDivision.EliminarDivisionAsync</c>, porque
	/// <c>FK_CURSANTE_DIVISION</c> está declarada con <c>OnDelete(DeleteBehavior.Restrict)</c> y el
	/// borrado reventaría en SQL Server con una violación de clave ajena en lugar de una excepción
	/// de dominio.
	/// </para>
	/// <para>
	/// <strong>El orden de esa guarda respecto de esta validación importa</strong>, y está elegido
	/// para que estas dos ramas sigan vivas: "toda inscripción" contiene a "matrícula vigente", así
	/// que si la guarda de integridad corriera primero, <paramref name="cantidadCursantes"/>
	/// llegaría siempre en cero y ni <c>DivisionNoEliminableException</c> ni
	/// <c>DivisionConMatriculaSuficienteException</c> se dispararían nunca en producción. La
	/// decisión está documentada en <c>docs/plan/historico/casos-uso-pendientes.md</c>, "La decisión
	/// sobre la clave ajena de división", y el motivo del orden, en el propio caso de uso.
	/// </para>
	/// </remarks>
	public void ValidarSePuedeEliminar(NivelEducativo nivel, int cantidadCursantes)
	{
		if (cantidadCursantes == 0)
		{
			return;
		}

		if (nivel == NivelEducativo.Secundaria)
		{
			throw new DivisionNoEliminableException();
		}

		if (nivel == NivelEducativo.Primaria && cantidadCursantes >= MINIMO_MATRICULA_PRIMARIA)
		{
			throw new DivisionConMatriculaSuficienteException();
		}
	}
	#endregion
}