using System.Collections.Generic;
using System.Security.Claims;
using System;

namespace WPF_Desktop.Security;

internal class BasicIdentity : ClaimsIdentity
{
	private BasicIdentity(Guid usuarioID, string docente, string email, string rol)
		: base(authenticationType: AuthType.Basic.ToString())
	{
		var claims = new List<Claim>() {
			new Claim(ClaimTypes.NameIdentifier, usuarioID.ToString()),
			new Claim(ClaimTypes.Name, docente),
			new Claim(ClaimTypes.Email, email),
			new Claim(ClaimTypes.Role, rol)
		};

		AddClaims(claims);
	}

	public static BasicIdentity Create(Guid usuarioID, string docente, string email, string rol) =>
		new BasicIdentity(usuarioID: usuarioID,
						 docente: docente,
						 email: email,
						 rol: rol);
}