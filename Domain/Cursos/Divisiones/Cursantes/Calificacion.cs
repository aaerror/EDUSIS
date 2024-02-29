using Domain.Shared;

namespace Domain.Cursos.Divisiones.Cursantes;

public class Calificacion : ValueObject
{
    private bool _asistencia = false;

    public Guid MateriaID { get; private set; }
    public DateTime? Fecha { get; private set; }
    public Instancia Instancia { get; private set; }
    public double? Nota { get; private set; }


    private Calificacion() {}

    private Calificacion(Guid materiaID, bool asistencia, DateTime? fecha, Instancia instancia, double? nota)
    {
        if (asistencia)
        {
            if (nota is null)
            {
                throw new ArgumentNullException(nameof(nota), "Debe especificar la nota del alumno para registrar una calificación.");
            }

            if (nota < 1 || nota > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(nota), "Nota inválida. La nota más pequeña es un uno (1) y la nota maxima es un diez (10)");
            }

            Nota = Math.Round((double)nota, 2);
        }

        if (fecha is not null)
        {
            if (DateTime.Today.Date < fecha.Value.Date)
            {
                throw new ArgumentException($"La fecha debe ser anterior al día de hoy ({ DateTime.Today })");
            }
        }
        

        MateriaID = materiaID;
        _asistencia = asistencia;
        Fecha = fecha.Value.Date;
        Instancia = instancia;
    }

    public static Calificacion Crear(Guid materiaID, bool asistencia, DateTime? fecha, Instancia instancia, double? nota)
    {
        return new(materiaID, asistencia, fecha, instancia, nota);
    }

    public Calificacion ModificarCalificacion(bool asistencia, DateTime? fecha, Instancia instancia, double? nota)
    {
        return new(MateriaID, asistencia, fecha, instancia, nota);
    }

    public bool EstuvoPresente() => _asistencia;

    public bool EstaAprobado() => _asistencia ? Nota > 5 : false;

    public override IEnumerable<object> GetEqualityCommponents()
    {
        yield return MateriaID;
        yield return _asistencia;
        yield return Fecha;
        yield return Instancia;
        yield return Nota;
    }
}