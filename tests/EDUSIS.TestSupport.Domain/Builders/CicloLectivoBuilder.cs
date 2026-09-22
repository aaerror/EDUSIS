using Domain.Cursantes;

namespace EDUSIS.TestSupport.Builders;

/// <summary>
/// Builder del value object <see cref="CicloLectivo"/>. Estado por defecto válido: el año en
/// curso. <see cref="Build"/> usa la factoría pública <see cref="CicloLectivo.Crear"/> (exige
/// un período de exactamente 4 dígitos).
/// </summary>
public sealed class CicloLectivoBuilder
{
	#region ESTADO POR DEFECTO
	private string _periodo = DateTime.Today.Year.ToString();
	#endregion

	#region CONFIGURACIÓN
	public CicloLectivoBuilder ConPeriodo(string periodo)
	{
		_periodo = periodo;
		return this;
	}

	public CicloLectivoBuilder ConAño(int año)
	{
		_periodo = año.ToString();
		return this;
	}
	#endregion

	#region CONSTRUCCIÓN
	public CicloLectivo Build() =>
		CicloLectivo.Crear(_periodo);
	#endregion
}
