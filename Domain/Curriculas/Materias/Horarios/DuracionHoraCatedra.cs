using System.ComponentModel;

namespace Domain.Curriculas.Materias.Horarios;

public enum DuracionHoraCatedra
{
    [Description("40 minutos")]
    Cuarenta = 40,
    [Description("45 minutos")]
    CuarentaCinco = 45,
    [Description("50 minutos")]
    Cincuenta = 50
}
