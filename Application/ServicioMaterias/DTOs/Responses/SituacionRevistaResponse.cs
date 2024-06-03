﻿namespace Core.ServicioMaterias.DTOs.Responses;

public record SituacionRevistaResponse
{
    public Guid DocenteID { get; init; }
    public string Docente { get; init; }
    public int Cargo { get; init; }
    public string CargoDescripcion {  get; init; }
    public DateTime FechaAlta { get; init; }
    public DateTime? FechaBaja { get; init; }
    public bool EnFunciones { get; init; }


    public SituacionRevistaResponse(Guid docenteID, string docente, int cargo, string cargoDescripcion, DateTime fechaAlta, DateTime? fechaBaja, bool enFunciones)
    {
        DocenteID = docenteID;
        Docente = docente;
        Cargo = cargo;
        CargoDescripcion = cargoDescripcion;
        FechaAlta = fechaAlta;
        FechaBaja = fechaBaja;
        EnFunciones = enFunciones;
    }
}
