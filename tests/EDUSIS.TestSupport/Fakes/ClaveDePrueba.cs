using System.Security;

namespace EDUSIS.TestSupport.Fakes;

/// <summary>
/// Arma la <see cref="SecureString"/> de solo lectura que entrega el control de contraseña de la
/// pantalla de acceso, para construir un <c>LoginRequest</c> en las pruebas.
/// </summary>
public static class ClaveDePrueba
{
	public static SecureString De(string clave)
	{
		var segura = new SecureString();
		foreach (var caracter in clave)
		{
			segura.AppendChar(caracter);
		}

		segura.MakeReadOnly();
		return segura;
	}
}
