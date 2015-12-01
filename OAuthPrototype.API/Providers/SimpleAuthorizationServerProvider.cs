using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OAuth;
using OAuthPrototype.API.Entities;

namespace OAuthPrototype.API.Providers {
	public class SimpleAuthorizationServerProvider : OAuthAuthorizationServerProvider {

		public override Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context) {

			string clientId;
			string clientSecret;
			Client client;

			if (!context.TryGetBasicCredentials(out clientId, out clientSecret)) {
				context.TryGetFormCredentials(out clientId, out clientSecret);
			}

			if (context.ClientId == null) {
				//Remove the comments from the below line context.SetError, and invalidate context 
				//if you want to force sending clientId/secrects once obtain access tokens. 
				context.Validated();
				//context.SetError("invalid_clientId", "ClientId should be sent.");
				return Task.FromResult<object>(null);
			}

			using (AuthRepository repo = new AuthRepository()) {
				client = repo.FindClient(context.ClientId);
			}

			if (client == null) {
				context.SetError("invalid_clientId", $"Client '{context.ClientId}' is not registered in the system.");
				return Task.FromResult<object>(null);
			}

			if (client.ApplicationType == Models.ApplicationTypes.NativeConfidential) {
				if (string.IsNullOrWhiteSpace(clientSecret)) {
					context.SetError("invalid_clientId", "Client secret should be sent.");
					return Task.FromResult<object>(null);
				}

				if (client.Secret != Helper.GetHash(clientSecret)) {
					context.SetError("invalid_clientId", "Client secret is invalid.");
					return Task.FromResult<object>(null);
				}
			}

			if (!client.Active) {
				context.SetError("invalid_clientId", "Client is inactive.");
				return Task.FromResult<object>(null);
			}

			context.OwinContext.Set("as:clientAllowedOrigin", client.AllowedOrigin);
			context.OwinContext.Set("as:clientRefreshTokenLifeTime", client.RefreshTokenLifeTime.ToString());

			context.Validated();
			return Task.FromResult<object>(null);
		}


		public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context) {

			string allowedOrigin = context.OwinContext.Get<string>("as:clientAllowedOrigin") ?? "*";

			context.OwinContext.Response.Headers.Add("Access-Control-Allow-Origin", new[] { allowedOrigin });

			using (AuthRepository repo = new AuthRepository()) {
				IdentityUser user = await repo.FindUser(context.UserName, context.Password);

				if (user == null) {
					context.SetError("invalid_grant", "The user name or password is incorrect.");
					return;
				}
			}

			var identity = new ClaimsIdentity(context.Options.AuthenticationType);
			identity.AddClaim(new Claim(ClaimTypes.Name, context.UserName));
			identity.AddClaim(new Claim("sub", context.UserName));
			identity.AddClaim(new Claim("role", "user"));

			var props = new AuthenticationProperties(new Dictionary<string, string> {
				{"as:client_id", context.ClientId ?? string.Empty},
				{"userName", context.UserName}
			});

			var ticket = new AuthenticationTicket(identity, props);
			context.Validated(ticket);

		}

		public override Task TokenEndpoint(OAuthTokenEndpointContext context) {
			foreach (var property in context.Properties.Dictionary) {
				context.AdditionalResponseParameters.Add(property.Key, property.Value);
			}

			return Task.FromResult<object>(null);
		}
	}
}