using Core.ServicioDocumentos;
using Core.Shared.Documentos;
using EDUSIS.TestSupport;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Core.UnitTests.ServicioDocumentos;

/// <summary>
/// <see cref="IServicioDocumento"/>: <c>GenerarCertificadoAlumnoRegular</c> delega en el puerto
/// <see cref="IGeneradorDocumentos"/> (aislado con NSubstitute) sin tocar el sistema de archivos
/// — en particular sin escribir en <c>C:\edusis\docs\</c>, la ruta de salida real cableada en
/// <c>Infrastructure</c>.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class ServicioDocumentoTests
{
	private readonly IGeneradorDocumentos _generador = Substitute.For<IGeneradorDocumentos>();
	private readonly IServicioDocumento _servicio;

	public ServicioDocumentoTests()
	{
		_servicio = new ServicioDocumento(_generador);
	}

	[Fact]
	public async Task GenerarCertificadoAlumnoRegular_invoca_al_generador_con_los_datos_del_certificado()
	{
		await _servicio.GenerarCertificadoAlumnoRegular();

		await _generador.Received(1).GenerarCertificadoAlumnoRegularAsync(
			Arg.Is<CertificadoAlumnoRegularRequest>(r =>
				r.NombreCompleto == "Juan Perez" &&
				r.DNI == "33998731" &&
				r.Grado == "1" &&
				r.NivelEducativo == "Secundaria"));
	}

	[Fact]
	public async Task GenerarCertificadoAlumnoRegular_no_escribe_en_la_ruta_de_salida_real()
	{
		await _servicio.GenerarCertificadoAlumnoRegular();

		// El puerto está doblado: nunca se toca el disco. La ruta hardcodeada de producción
		// (C:\edusis\docs\) no debe existir tras ejecutar el caso de uso en la suite.
		Directory.Exists(@"C:\edusis\docs\").ShouldBeFalse();
	}

	[Fact]
	public async Task GenerarCertificadoAlumnoRegular_propaga_la_excepcion_del_generador_sin_transformarla()
	{
		_generador
			.GenerarCertificadoAlumnoRegularAsync(Arg.Any<CertificadoAlumnoRegularRequest>())
			.Returns(Task.FromException(new InvalidOperationException("Falla del generador de PDF")));

		var ex = await Should.ThrowAsync<InvalidOperationException>(
			() => _servicio.GenerarCertificadoAlumnoRegular());

		ex.Message.ShouldBe("Falla del generador de PDF");
	}
}
