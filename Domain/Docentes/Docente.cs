using Domain.Docentes.Licencias;
using Domain.Personas;
using Domain.Personas.Domicilios;
using System.Text.RegularExpressions;

namespace Domain.Docentes;

public class Docente : Persona
{
    private List<Licencia> _licencias = new List<Licencia>();
    private List<Puesto> _puestos = new List<Puesto>();
     
    public string Legajo { get; private set; }
    public string CUIL { get; private set; }
    public DateTime FechaAlta { get; private set; }
    public DateTime? FechaBaja { get; private set; } = null;
    public bool EstaActivo { get; private set; } = true;
    public IReadOnlyCollection<Licencia> Licencias => _licencias.AsReadOnly();
    public IReadOnlyCollection<Puesto> Puestos => _puestos.AsReadOnly();


    protected Docente()
        : base() { }

    public Docente(string legajo, string cuil, InformacionPersonal informacionPersonal, Domicilio domicilio, string email, string telefono)
        : base(informacionPersonal, domicilio, email, telefono)
    {
        if (informacionPersonal.Edad() < 18)
        {
            throw new ArgumentException($"El docente es menor de edad ({ informacionPersonal.Edad() } años).");
        }

        ValidarLegajo(legajo);
        ValidarCuil(cuil);

        Legajo = legajo;
        CUIL = cuil;
        FechaAlta = DateTime.Today;
    }

    private void ValidarLegajo(string legajoDocente)
    {
        if (!Regex.IsMatch(legajoDocente, @"^(\d{6})$"))
        {
            throw new FormatException($"Verificar el legajo del docente: { nameof(legajoDocente) }.");
        }
    }

    private void ValidarCuil(string cuil)
    {
        //@"^\d{2}-\d{8}-\d{1}$
        if (!Regex.IsMatch(cuil, @"\b(20|23|24|27|30|33|34)(\D)?[0-9]{8}(\D)?[0-9]"))
        {
            throw new FormatException($"Verificar el CUIL del docente: { nameof(cuil) }.");
        }
    }

    #region Puesto Docente
    public Puesto AgregarPuesto(Posicion nuevaPosicion, DateTime fechaInicio)
    {
        var puesto = _puestos.Find(x => x.Posicion == nuevaPosicion && x.FechaFin is null);
        if (puesto is not null)
        {
            throw new ArgumentException($"El docente ya se encuentra en el puesto de { puesto.Posicion }, desde el { puesto.FechaInicio.ToString("D") }.");
        }

        var nuevoPuesto = Puesto.Create(nuevaPosicion, fechaInicio);
        _puestos.Add(nuevoPuesto);

        return nuevoPuesto;
    }
    
    public Puesto CambiarPuesto(Posicion nuevaPosicion, DateTime fechaInicio)
    {
        var puestoViejo = _puestos.Where(x => x.FechaFin is not null)
                                         .FirstOrDefault();
        if (puestoViejo is null)
        {
            throw new ArgumentException("El docente no tiene ningún puesto en la institución.");
        }

        QuitarPuesto((int) puestoViejo.Posicion, puestoViejo.FechaInicio);
        return AgregarPuesto(nuevaPosicion, fechaInicio);
    }

    public Puesto QuitarPuesto(int posicion, DateTime fechaInicio)
    {
        var puesto = _puestos.Find(x => x.Posicion.Equals((Posicion) posicion) && x.FechaInicio.Date == fechaInicio.Date && x.FechaFin is null);
        if (puesto is null)
        {
            throw new ArgumentNullException("Puesto docente no encontrado.");
        }

        var puestoEliminado = puesto.QuitarPuesto();

        var index = _puestos.IndexOf(puesto);
        _puestos[index] = puestoEliminado;

        return puestoEliminado;
    }
    #endregion

    #region Licencia
    public Licencia RegistrarLicencia(int articulo, int dias, DateTime fechaInicio, string observacion)
    {
        var existeLicencia = _licencias.Any(x => x.Estado.Equals(Estado.Activa) || x.Estado.Equals(Estado.Pendiente));
        if (existeLicencia)
        {
            throw new ArgumentException($"El docente ya posee una licencia en curso.");
        }

        var nuevaLicencia = Licencia.Crear((Articulo) articulo, dias, fechaInicio, observacion);
        _licencias.Add(nuevaLicencia);

        return nuevaLicencia;
    }

    public void AprobarLicencia(int articulo, int dias, DateTime fechaInicio)
    {
        Articulo articuloDocente = Enum.Parse<Articulo>(articulo.ToString());
        var licencia = _licencias.Find(x => x.Articulo.Equals(articuloDocente) && x.Dias == dias && x.FechaInicio.Equals(fechaInicio) && x.Estado.Equals(Estado.Pendiente));
        if (licencia is null)
        {
            throw new ArgumentException($"Licencia del docente no encontrada.");
        }

        var index = _licencias.IndexOf(licencia);
        _licencias[index] = licencia.Activar();
    }

    public void CancelarLicencia(int articulo, int dias, DateTime fechaInicio, string observacion)
    {
        Articulo articuloDocente = Enum.Parse<Articulo>(articulo.ToString());
        var licencia = _licencias.Find(x => x.Articulo.Equals(articuloDocente) && x.Dias == dias && x.FechaInicio.Equals(fechaInicio) && x.Estado.Equals(Estado.Pendiente));
        if (licencia is null)
        {
            throw new ArgumentException($"Licencia del docente no encontrada.");
        }

        var index = _licencias.IndexOf(licencia);
        _licencias[index] = licencia.Cancelar(observacion);
    }

    public Licencia ActualizarLicencia(Licencia unaLicencia, int articulo, int dias, DateTime fechaInicio, string observacion)
    {
        var licencia = _licencias.Find(x => x.Equals(unaLicencia));
        if (licencia is null)
        {
            throw new ArgumentException($"Licencia del docente no encontrada.");
        }

        var nuevaLicencia = licencia.ActualizarLicencia((Articulo) articulo, dias, fechaInicio, observacion);
        _licencias.Remove(licencia);
        _licencias.Add(nuevaLicencia);

        return nuevaLicencia;
    }
    #endregion
}