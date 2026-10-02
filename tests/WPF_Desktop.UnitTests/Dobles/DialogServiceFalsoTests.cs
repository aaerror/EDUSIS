using Shouldly;
using WPF_Desktop.Shared;
using Xunit;

namespace WPF_Desktop.UnitTests.Dobles;

public class DialogServiceFalsoTests
{
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_contrato_cubre_los_cuatro_usos_y_un_doble_los_registra_sin_abrir_ventanas()
	{
		var falso = new DialogServiceFalso { RespuestaDeConfirmacion = false };
		IDialogService dialogos = falso;

		dialogos.MostrarInformacion("info", "t1");
		dialogos.MostrarAdvertencia("aviso", "t2");
		dialogos.MostrarError("error", "t3");
		var aceptado = dialogos.Confirmar("seguro?", "t4");

		falso.Informaciones.ShouldBe(new[] { ("info", "t1") });
		falso.Advertencias.ShouldBe(new[] { ("aviso", "t2") });
		falso.Errores.ShouldBe(new[] { ("error", "t3") });
		falso.Confirmaciones.ShouldBe(new[] { ("seguro?", "t4") });
		aceptado.ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void DialogService_implementa_el_contrato()
	{
		typeof(IDialogService).IsAssignableFrom(typeof(DialogService)).ShouldBeTrue();
	}
}
