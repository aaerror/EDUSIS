using System.Security;
using Core.ServicioSecurity;
using Core.UnitTests.Infraestructura;
using EDUSIS.TestSupport;
using Shouldly;
using Xunit;

namespace Core.UnitTests.ServicioSeguridad;

/// <summary>
/// <see cref="Core.ServicioSecurity.ServicioSeguridad"/>: derivación PBKDF2 con salt aleatoria y
/// verificación en tiempo constante. La interfaz es <c>internal</c>; se resuelve por la vía
/// directa de <see cref="HostDeServicios.Seguridad"/> (InternalsVisibleTo).
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class ServicioSeguridadTests
{
	private readonly IServicioSeguridad _servicio;

	public ServicioSeguridadTests()
	{
		_servicio = new HostDeServicios().Seguridad;
	}

	private static SecureString ComoSecureString(string valor)
	{
		var secure = new SecureString();
		foreach (var caracter in valor)
		{
			secure.AppendChar(caracter);
		}

		secure.MakeReadOnly();
		return secure;
	}

	#region HashPassword
	[Fact]
	public void HashPassword_devuelve_salt_y_hash_en_base64_no_vacios()
	{
		var resultado = _servicio.HashPassword("clave-Segúra-123");

		resultado.ShouldContainKey("salt");
		resultado.ShouldContainKey("hash");
		resultado["salt"].ShouldNotBeNullOrWhiteSpace();
		resultado["hash"].ShouldNotBeNullOrWhiteSpace();
		Should.NotThrow(() => Convert.FromBase64String(resultado["salt"]));
		Should.NotThrow(() => Convert.FromBase64String(resultado["hash"]));
	}

	[Fact]
	public void HashPassword_usa_una_salt_aleatoria_distinta_en_cada_invocacion()
	{
		var primero = _servicio.HashPassword("misma-clave");
		var segundo = _servicio.HashPassword("misma-clave");

		segundo["salt"].ShouldNotBe(primero["salt"]);
		segundo["hash"].ShouldNotBe(primero["hash"]);
	}
	#endregion

	#region ValidatePassword
	[Fact]
	public void ValidatePassword_acepta_la_misma_clave_con_su_salt_y_hash()
	{
		const string clave = "R0das-de-Auxilio";
		var credenciales = _servicio.HashPassword(clave);

		var esValida = _servicio.ValidatePassword(ComoSecureString(clave), credenciales["salt"], credenciales["hash"]);

		esValida.ShouldBeTrue();
	}

	[Fact]
	public void ValidatePassword_rechaza_una_clave_incorrecta()
	{
		var credenciales = _servicio.HashPassword("clave-correcta");

		var esValida = _servicio.ValidatePassword(ComoSecureString("clave-incorrecta"), credenciales["salt"], credenciales["hash"]);

		esValida.ShouldBeFalse();
	}

	[Fact]
	public void ValidatePassword_rechaza_cuando_el_hash_pertenece_a_otra_clave()
	{
		var credencialesA = _servicio.HashPassword("clave-A");
		var credencialesB = _servicio.HashPassword("clave-B");

		var esValida = _servicio.ValidatePassword(ComoSecureString("clave-A"), credencialesA["salt"], credencialesB["hash"]);

		esValida.ShouldBeFalse();
	}
	#endregion
}
