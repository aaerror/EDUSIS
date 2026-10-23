using Core.ServicioAutenticaciones;
using NSubstitute;
using Shouldly;
using WPF_Desktop.Navigation;
using WPF_Desktop.Security;
using WPF_Desktop.ViewModels.Usuarios;
using Xunit;

namespace WPF_Desktop.UnitTests.ViewModels;

/// <summary>
/// El aviso de error del acceso: el recuadro y su texto tienen que decir lo mismo.
/// </summary>
/// <remarks>
/// La vista enlaza la VISIBILIDAD del aviso contra <c>HasErrors</c> y su TEXTO contra
/// <c>MensajeError</c>. Mientras MensajeError fue una propiedad que nadie asignaba, el
/// formulario mostraba el recuadro vacío: dos fuentes distintas para un solo aviso. Nada de
/// eso se podía detectar compilando ni mirando el XAML.
/// </remarks>
public class LoginUsuarioViewModelTests
{
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Sin_errores_no_hay_aviso_ni_texto()
	{
		var viewModel = ArmarViewModel();

		viewModel.HasErrors.ShouldBeFalse();
		viewModel.MensajeError.ShouldBeEmpty();
	}

	[Theory]
	[Trait("Categoria", "Unidad")]
	[InlineData("   ", "Se debe ingresar un correo electrónico.")]
	[InlineData("no-es-un-correo", "Correo electrónico inválido.")]
	public void Un_usuario_invalido_deja_el_aviso_visible_CON_texto(string usuario, string esperado)
	{
		var viewModel = ArmarViewModel();

		viewModel.Usuario = usuario;

		viewModel.HasErrors.ShouldBeTrue();
		viewModel.MensajeError.ShouldContain(esperado);
	}

	/// <summary>
	/// Vaciar el campo después de haberlo completado SÍ dispara la validación.
	/// </summary>
	/// <remarks>
	/// Con el formulario recién abierto no alcanza con asignar <see cref="string.Empty"/>:
	/// <c>Usuario</c> ya arranca vacío y el setter que genera <c>[ObservableProperty]</c> corta
	/// cuando el valor no cambió, así que no hay validación que fallar. Ese caso —entrar y
	/// pulsar Ingresar sin escribir nada— lo atiende el comando, no el aviso.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Borrar_un_usuario_ya_escrito_pide_el_correo()
	{
		var viewModel = ArmarViewModel();
		viewModel.Usuario = "docente@edu.ar";

		viewModel.Usuario = string.Empty;

		viewModel.HasErrors.ShouldBeTrue();
		viewModel.MensajeError.ShouldContain("Se debe ingresar un correo electrónico.");
	}

	/// <summary>
	/// La regresión concreta: que el recuadro no vuelva a aparecer en blanco.
	/// </summary>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_aviso_nunca_queda_visible_sin_texto()
	{
		var viewModel = ArmarViewModel();

		viewModel.Usuario = "no-es-un-correo";

		// Son las dos propiedades que consume el aviso de LoginUsuarioView.
		if (viewModel.HasErrors)
		{
			viewModel.MensajeError.ShouldNotBeNullOrWhiteSpace();
		}
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Corregir_el_usuario_apaga_el_aviso_y_limpia_el_texto()
	{
		var viewModel = ArmarViewModel();
		viewModel.Usuario = "no-es-un-correo";

		viewModel.Usuario = "docente@edu.ar";

		viewModel.HasErrors.ShouldBeFalse();
		viewModel.MensajeError.ShouldBeEmpty();
	}

	/// <summary>
	/// MensajeError se calcula, así que tiene que avisar su cambio cuando cambian los errores
	/// o el enlace de la vista se queda con el texto viejo.
	/// </summary>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void MensajeError_notifica_su_cambio_al_cambiar_los_errores()
	{
		var viewModel = ArmarViewModel();
		var notificadas = new List<string?>();
		viewModel.PropertyChanged += (_, argumentos) => notificadas.Add(argumentos.PropertyName);

		viewModel.Usuario = "no-es-un-correo";

		notificadas.ShouldContain(nameof(viewModel.MensajeError));
	}

	private static LoginUsuarioViewModel ArmarViewModel() =>
		new(Substitute.For<INavigationService>(),
			Substitute.For<IServicioAutenticacion>(),
			Substitute.For<ISesionUsuario>());
}
