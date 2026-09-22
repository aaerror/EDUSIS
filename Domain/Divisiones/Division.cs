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

	public static Division Siguiente(Guid cursoID, IEnumerable<string> descripcionesExistentes)
	{
		var ultima = descripcionesExistentes.OrderDescending().FirstOrDefault();
		var siguiente = ultima is null ? "A" : char.ConvertFromUtf32(char.Parse(ultima.Trim()) + 1);
		// TODO: sin tope tras 'Z'
		return new Division(cursoID, siguiente);
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

	public void ValidarSePuedeEliminar(NivelEducativo nivel, int cantidadCursantes)
	{
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