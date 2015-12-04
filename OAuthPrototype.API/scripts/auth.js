(function(parameters) {
	var apiServiceBaseUri = "http://localhost:91/";

	var $scope = {};

	$scope.authExternalProvider = function (provider) {
		var redirectUri = location.protocol + "//" + location.host + "/authcomplete.html";

		var externalProviderUrl = apiServiceBaseUri + "api/Account/ExternalLogin?provider=" + provider
                                                                    + "&response_type=token"
                                                                    + "&redirect_uri=" + redirectUri;
		window.$windowScope = $scope;

		var oauthWindow = window.open(externalProviderUrl, "Authenticate Account", "location=0,status=0,width=600,height=750");
	};

	$scope.authCompletedCB = function (fragment) {
		var externalAuthData = {
			provider: fragment.provider,
			userName: fragment.external_user_name,
			externalAccessToken: fragment.external_access_token
		};
		console.log(externalAuthData);
		alert(JSON.stringify(externalAuthData));
	};

	document.getElementById("loginButton").onclick = function () {
		$scope.authExternalProvider("Facebook");
		return false;
	}
})();