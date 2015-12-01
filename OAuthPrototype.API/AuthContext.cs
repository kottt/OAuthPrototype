using System.Data.Entity;
using Microsoft.AspNet.Identity.EntityFramework;
using OAuthPrototype.API.Entities;

namespace OAuthPrototype.API {
	public class AuthContext : IdentityDbContext<IdentityUser> {
		public AuthContext()
			: base("AuthContext") {
		}

		public DbSet<Client> Clients { get; set; }
		public DbSet<RefreshToken> RefreshTokens { get; set; }
	}
}