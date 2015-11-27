using System.Web.Http;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(OAuthPrototype.API.Startup))]
namespace OAuthPrototype.API {
	public class Startup {
		public void Configuration(IAppBuilder app) {
			HttpConfiguration config = new HttpConfiguration();
			WebApiConfig.Register(config);
			app.UseWebApi(config);
		}
	}
}