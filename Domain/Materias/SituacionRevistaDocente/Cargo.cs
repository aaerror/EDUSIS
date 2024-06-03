using System.ComponentModel;

namespace Domain.Materias.SituacionRevistaDocente;

public enum Cargo
{
    [Description("Titular")]
    Titular,
    [Description("Suplente")]
    Suplente,
    [Description("Interino")]
    Interino
}
