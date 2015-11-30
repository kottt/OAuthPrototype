using System;
using System.Web.Http;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Security.OAuth;
using OAuthPrototype.API.Providers;
using Owin;

[assembly: OwinStartup(typeof(OAuthPrototype.API.Startup))]
namespace OAuthPrototype.API {
	public class Startup {
		public void Configuration(IAppBuilder app) {
			HttpConfiguration config = new HttpConfiguration();

			ConfigureOAuth(app);

			WebApiConfig.Register(config);
			app.UseCors(CorsOptions.AllowAll);
			app.UseWebApi(config);
		}

		public void ConfigureOAuth(IAppBuilder app) {
			OAuthAuthorizationServerOptions oAuthServerOptions = new OAuthAuthorizationServerOptions {
				AllowInsecureHttp = true,
				TokenEndpointPath = new PathString("/token"),
				AccessTokenExpireTimeSpan = TimeSpan.FromDays(1),
				Provider = new SimpleAuthorizationServerProvider()
			};

			// Token Generation
			app.UseOAuthAuthorizationServer(oAuthServerOptions);
			app.UseOAuthBearerAuthentication(new OAuthBearerAuthenticationOptions());
		}
	}
}