using Domain.Personas.Domicilios;
using Domain.Shared;
using Domain.Shared.Exceptions;
using EDUSIS.TestSupport;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Shared;

/// <summary>
/// Excepciones transversales de <c>Domain/Shared/Exceptions/</c>:
/// <see cref="ExcesoCaracteresException"/> (pública) y <c>FechasInconsistentesException</c>
/// (<c>internal</c>: se verifica por el nombre del tipo, sin referenciarlo, para no exigir un
/// segundo <c>InternalsVisibleTo</c> de producción).
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class ExcepcionesTests
{
	#region ExcesoCaracteresException
	[Fact]
	public void ExcesoCaracteresException_se_lanza_cuando_la_observacion_supera_120_caracteres()
	{
		var observacionLarga = new string('á', 121);

		Should.Throw<ExcesoCaracteresException>(() =>
			Direccion.Crear("Pasaje Ñuñorco", "1234", nameof(Vivienda.Casa), observacionLarga));
	}

	[Fact]
	public void ExcesoCaracteresException_no_se_lanza_en_el_limite_de_120_caracteres()
	{
		var observacionAlLimite = new string('á', 120);

		Should.NotThrow(() =>
			Direccion.Crear("Pasaje Ñuñorco", "1234", nameof(Vivienda.Casa), observacionAlLimite));
	}

	[Fact]
	public void ExcesoCaracteresException_antepone_su_mensaje_de_error_constante()
	{
		var ex = new ExcesoCaracteresException("detalle puntual");

		ex.Message.ShouldContain("excede el límite de caracteres");
		ex.Message.ShouldContain("detalle puntual");
	}
	#endregion

	#region FechasInconsistentesException
	[Fact]
	public void FechasInconsistentesException_se_lanza_si_la_fecha_fin_es_anterior_al_inicio()
	{
		var inicio = new DateTime(2026, 3, 1);
		var fin = new DateTime(2026, 2, 1);

		var ex = Should.Throw<Exception>(() => RangoFechas.Create(inicio, fin));

		ex.GetType().Name.ShouldBe("FechasInconsistentesException");
	}

	[Fact]
	public void FechasInconsistentesException_no_se_lanza_con_un_rango_coherente()
	{
		var inicio = new DateTime(2026, 2, 1);
		var fin = new DateTime(2026, 3, 1);

		Should.NotThrow(() => RangoFechas.Create(inicio, fin));
	}

	[Fact]
	public void RangoFechas_admite_inicio_y_fin_en_el_mismo_dia()
	{
		var dia = new DateTime(2026, 2, 1);

		Should.NotThrow(() => RangoFechas.Create(dia, dia));
	}
	#endregion
}
