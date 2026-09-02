using System.Collections.Generic;
using System.Security.Claims;

namespace WPF_Desktop.Security;

internal class CustomPrincipal : ClaimsPrincipal
{
	private CustomPrincipal(List<BasicIdentity> identities)
		: base(identities: identities) { }

	public static CustomPrincipal Create(List<BasicIdentity> identities) =>
		new CustomPrincipal(identities: identities);
}