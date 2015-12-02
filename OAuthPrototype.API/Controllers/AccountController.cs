using System;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin.Security;
using Newtonsoft.Json.Linq;
using OAuthPrototype.API.Models;
using OAuthPrototype.API.Results;

namespace OAuthPrototype.API.Controllers {
	[RoutePrefix("api/Account")]
	public class AccountController : ApiController {
		private readonly AuthRepository _repo;

		public AccountController() {
			_repo = new AuthRepository();
		}

		// POST api/Account/Register
		[AllowAnonymous]
		[Route("Register")]
		public async Task<IHttpActionResult> Register(UserModel userModel) {
			if (!ModelState.IsValid) {
				return BadRequest(ModelState);
			}

			IdentityResult result = await _repo.RegisterUser(userModel);

			IHttpActionResult errorResult = GetErrorResult(result);

			if (errorResult != null) {
				return errorResult;
			}

			return Ok();
		}

		private IAuthenticationManager Authentication => Request.GetOwinContext().Authentication;

		// GET api/Account/ExternalLogin
		[OverrideAuthentication]
		[HostAuthentication(DefaultAuthenticationTypes.ExternalCookie)]
		[AllowAnonymous]
		[Route("ExternalLogin", Name = "ExternalLogin")]
		public async Task<IHttpActionResult> GetExternalLogin(string provider, string error = null) {
			string redirectUri = string.Empty;

			if (error != null) {
				return BadRequest(Uri.EscapeDataString(error));
			}

			if (!User.Identity.IsAuthenticated) {
				return new ChallengeResult(provider, this);
			}

			var redirectUriValidationResult = ValidateClientAndRedirectUri(Request, ref redirectUri);
			if (!string.IsNullOrWhiteSpace(redirectUriValidationResult)) {
				return BadRequest(redirectUriValidationResult);
			}

			ExternalLoginData externalLogin = ExternalLoginData.FromIdentity(User.Identity as ClaimsIdentity);
			if (externalLogin == null) {
				return InternalServerError();
			}

			if (externalLogin.LoginProvider != provider) {
				Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);
				return new ChallengeResult(provider, this);
			}

			IdentityUser user = await _repo.FindAsync(new UserLoginInfo(externalLogin.LoginProvider, externalLogin.ProviderKey));

			bool hasRegistered = user != null;

			redirectUri = string.Format("{0}#external_access_token={1}&provider={2}&haslocalaccount={3}&external_user_name={4}",
											redirectUri,
											externalLogin.ExternalAccessToken,
											externalLogin.LoginProvider,
											hasRegistered,
											externalLogin.UserName);

			return Redirect(redirectUri);

		}

		private string ValidateClientAndRedirectUri(HttpRequestMessage request, ref string redirectUriOutput) {
			var redirectUriString = GetQueryString(request, "redirect_uri");
			if (string.IsNullOrWhiteSpace(redirectUriString)) {
				return "redirect_uri is required";
			}

			Uri redirectUri;
			bool validUri = Uri.TryCreate(redirectUriString, UriKind.Absolute, out redirectUri);
			if (!validUri) {
				return "redirect_uri is invalid";
			}

			var clientId = GetQueryString(request, "client_id");
			if (string.IsNullOrWhiteSpace(clientId)) {
				return "client_Id is required";
			}

			var client = _repo.FindClient(clientId);
			if (client == null) {
				return $"Client_id '{clientId}' is not registered in the system.";
			}

			if (!string.Equals(client.AllowedOrigin, redirectUri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase)) {
				return $"The given URL is not allowed by Client_id '{clientId}' configuration.";
			}

			redirectUriOutput = redirectUri.AbsoluteUri;

			return string.Empty;

		}

		#region Helpers

		private async Task<ParsedExternalAccessToken> VerifyExternalAccessToken(string provider, string accessToken) {
			ParsedExternalAccessToken parsedToken = null;

			string verifyTokenEndPoint;

			if (provider == "Facebook") {
				//You can get it from here: https://developers.facebook.com/tools/accesstoken/
				//More about debug_token here: http://stackoverflow.com/questions/16641083/how-does-one-get-the-app-access-token-for-debug-token-inspection-on-facebook

				var appToken = "xxxxx";
				verifyTokenEndPoint = $"https://graph.facebook.com/debug_token?input_token={accessToken}&access_token={appToken}";
			} else if (provider == "Google") {
				verifyTokenEndPoint = $"https://www.googleapis.com/oauth2/v1/tokeninfo?access_token={accessToken}";
			} else {
				return null;
			}

			var client = new HttpClient();
			var uri = new Uri(verifyTokenEndPoint);
			var response = await client.GetAsync(uri);

			if (response.IsSuccessStatusCode) {
				var content = await response.Content.ReadAsStringAsync();

				dynamic jObj = (JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(content);

				parsedToken = new ParsedExternalAccessToken();

				if (provider == "Facebook") {
					parsedToken.user_id = jObj["data"]["user_id"];
					parsedToken.app_id = jObj["data"]["app_id"];

					if (!string.Equals(Startup.facebookAuthOptions.AppId, parsedToken.app_id, StringComparison.OrdinalIgnoreCase)) {
						return null;
					}
				} else if (provider == "Google") {
					parsedToken.user_id = jObj["user_id"];
					parsedToken.app_id = jObj["audience"];

					if (!string.Equals(Startup.googleAuthOptions.ClientId, parsedToken.app_id, StringComparison.OrdinalIgnoreCase)) {
						return null;
					}
				}
			}

			return parsedToken;
		}

		private static string GetQueryString(HttpRequestMessage request, string key) {
			var queryStrings = request.GetQueryNameValuePairs();

			if (queryStrings == null) return null;

			var match = queryStrings.FirstOrDefault(keyValue => string.Compare(keyValue.Key, key, StringComparison.OrdinalIgnoreCase) == 0);

			if (string.IsNullOrEmpty(match.Value)) return null;

			return match.Value;
		}

		private IHttpActionResult GetErrorResult(IdentityResult result) {
			if (result == null) {
				return InternalServerError();
			}

			if (!result.Succeeded) {
				if (result.Errors != null) {
					foreach (string error in result.Errors) {
						ModelState.AddModelError("", error);
					}
				}

				if (ModelState.IsValid) {
					// No ModelState errors are available to send, so just return an empty BadRequest.
					return BadRequest();
				}

				return BadRequest(ModelState);
			}

			return null;
		}

		#endregion

		protected override void Dispose(bool disposing) {
			if (disposing) {
				_repo.Dispose();
			}

			base.Dispose(disposing);
		}

		private class ExternalLoginData {
			public string LoginProvider { get; set; }
			public string ProviderKey { get; set; }
			public string UserName { get; set; }
			public string ExternalAccessToken { get; set; }

			public static ExternalLoginData FromIdentity(ClaimsIdentity identity) {
				if (identity == null) {
					return null;
				}

				Claim providerKeyClaim = identity.FindFirst(ClaimTypes.NameIdentifier);
				if (string.IsNullOrEmpty(providerKeyClaim?.Issuer) || string.IsNullOrEmpty(providerKeyClaim.Value)) {
					return null;
				}
				if (providerKeyClaim.Issuer == ClaimsIdentity.DefaultIssuer) {
					return null;
				}

				return new ExternalLoginData {
					LoginProvider = providerKeyClaim.Issuer,
					ProviderKey = providerKeyClaim.Value,
					UserName = identity.FindFirstValue(ClaimTypes.Name),
					ExternalAccessToken = identity.FindFirstValue("ExternalAccessToken"),
				};
			}
		}
	}
}