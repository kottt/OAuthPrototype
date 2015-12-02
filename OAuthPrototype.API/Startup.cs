using System;
using System.Web.Http;
using Microsoft.AspNet.Identity;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Security.Facebook;
using Microsoft.Owin.Security.Google;
using Microsoft.Owin.Security.OAuth;
using OAuthPrototype.API.Providers;
using Owin;

[assembly: OwinStartup(typeof(OAuthPrototype.API.Startup))]
namespace OAuthPrototype.API {
	public class Startup {

		public static OAuthBearerAuthenticationOptions OAuthBearerOptions { get; private set; }
		public static GoogleOAuth2AuthenticationOptions googleAuthOptions { get; private set; }
		public static FacebookAuthenticationOptions facebookAuthOptions { get; private set; }

		public void Configuration(IAppBuilder app) {
			HttpConfiguration config = new HttpConfiguration();

			ConfigureOAuth(app);

			WebApiConfig.Register(config);
			app.UseCors(CorsOptions.AllowAll);
			app.UseWebApi(config);
		}

		public void ConfigureOAuth(IAppBuilder app) {
			app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);
			OAuthBearerOptions = new OAuthBearerAuthenticationOptions();

			OAuthAuthorizationServerOptions oAuthServerOptions = new OAuthAuthorizationServerOptions() {
				AllowInsecureHttp = true,
				TokenEndpointPath = new PathString("/token"),
				AccessTokenExpireTimeSpan = TimeSpan.FromMinutes(30),
				Provider = new SimpleAuthorizationServerProvider(),
				RefreshTokenProvider = new SimpleRefreshTokenProvider()
			};

			// Token Generation
			app.UseOAuthAuthorizationServer(oAuthServerOptions);
			app.UseOAuthBearerAuthentication(OAuthBearerOptions);


			//Configure Google External Login
			googleAuthOptions = new GoogleOAuth2AuthenticationOptions() {
				ClientId = "xxx",
				ClientSecret = "xxx",
				Provider = new GoogleAuthProvider()
			};
			app.UseGoogleAuthentication(googleAuthOptions);

			//Configure Facebook External Login
			facebookAuthOptions = new FacebookAuthenticationOptions() {
				AppId = "xxx",
				AppSecret = "xxx",
				Provider = new FacebookAuthProvider()
			};
			app.UseFacebookAuthentication(facebookAuthOptions);
		}
	}
}