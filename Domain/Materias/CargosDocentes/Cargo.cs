using System.ComponentModel;

namespace Domain.Materias.CargosDocentes;

public enum Cargo
{
    [Description("Titular")]
    Titular,
    [Description("Suplente")]
    Suplente,
    [Description("Interino")]
    Interino
}
