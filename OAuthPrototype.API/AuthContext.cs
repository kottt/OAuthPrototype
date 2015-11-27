using Microsoft.AspNet.Identity.EntityFramework;

namespace OAuthPrototype.API {
	public class AuthContext : IdentityDbContext<IdentityUser> {
		public AuthContext()
			: base("AuthContext") {

		}
	}
}