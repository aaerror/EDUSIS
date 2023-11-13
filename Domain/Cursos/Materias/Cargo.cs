using System.ComponentModel;

namespace Domain.Cursos.Materias;

public enum Cargo
{
    [Description("Titular")]
    Titular,
    [Description("Suplente")]
    Suplente,
    [Description("Interino")]
    Interino
}
