using Box.V2;
using Box.V2.Auth;
using Box.V2.Config;
using Box.V2.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Mvc;

namespace test.Controllers
{
    public class CallBackParams
    {
        public string state { get; set; }
        public string code { get; set; }
    }

    public class AuthController : Controller
    {
        private const string ClientId = "s24y8ah30kx50uzzopct95s8belqi72e";
        private const string ClientSecret = "lojRF0VJimVNpOjJZALcqV0oGNoCnbi9";
        private const string AntiforgeryToken = "security_token=KnhMJatFipTAnM0nHlZA";

        //[Route("auth/Click")]
        //[HttpPost]
        //public HttpResponseMessage Click()
        //{
        //    var url = string.Format( @"https://app.box.com/api/oauth2/authorize?response_type=code&client_id={0}&state={1}"
        //                           , @"s24y8ah30kx50uzzopct95s8belqi72e"
        //                           , AuthController.AntiforgeryToken
        //                           );

        //    var urlEncode = WebUtility.UrlEncode(url);

        //    return new HttpResponseMessage()
        //    {
        //        Content = new StringContent(urlEncode)
        //    };
        //}

            // GET /Index
        public async Task<ActionResult> Callback()
        {
            if (IsError()) return Error(Request.QueryString["error"], Request.QueryString["error_description"]);
            if (IsCode()) return await Token(Request.QueryString["code"], Request.QueryString["state"]);

            ClearSession();
            return View("Index", "Home");
        }

        /// <summary>
        /// The first step of the OAuth2 flow. Redirect the user to Box for credentialing and authorization of this application.
        /// </summary>
        // GET /Authorize
        public ActionResult Authorize(AuthModel authModel)
        {
            // Generate and stash an antiforgery token
            var antiforgeryToken = Guid.NewGuid().ToString();
            Session[AntiforgeryToken] = antiforgeryToken;

            // Stash the Client ID/Secret for easy access when the user is redirected back to this page.
            Session[ClientId] = authModel.ClientId;
            Session[ClientSecret] = authModel.ClientSecret;

            // Redirect the user to Box's OAuth2 authorization page
            string authUrl = string.Format("https://app.box.com/api/oauth2/authorize?response_type=code&client_id={0}&state={1}", authModel.ClientId, antiforgeryToken);
            return new RedirectResult(authUrl);
        }

        /// <summary>
        /// The second and final step of the OAuth2 flow. The user has authorized this application at Box's site and redirected them back to this site. Validate the redirect and exchange the authorization code for a access/refresh token pair.
        /// </summary>
        private async Task<ActionResult> Token(string code, string state)
        {
            try
            {
                // Validate that the 'code' has not already been exchanged for an access token. This prevents replay attacks.
                if (!ValidateAntiforgeryToken(state))
                {
                    Response.StatusCode = 400;
                    return View("Error", new ErrorModel { Message = "forged_request", Description = "This code has already been used to fetch an authorization token." });
                }

                // Fetch the stashed Client ID/Secret from the Session
                var clientId = ClientId ;// Session[ClientId] as string;
                var clientSecret = ClientSecret;// Session[ClientSecret] as string;

                // Exchange the 'code' for an authorization/refresh token pair
                // var authSession = await ExchangeCodeForTokenPair(code, clientId, clientSecret);

                var boxClient = new BoxClient(new BoxConfig(clientId, clientSecret, new Uri("http://localhost:1176/Auth/Callback")));
                OAuthSession authSession = await boxClient.Auth.AuthenticateAsync(code);

                

                // TODO:


                // Clear out the session variables for security
                ClearSession();

                var authInfo = new AuthModel { ClientId = clientId, ClientSecret = clientSecret, AuthToken = authSession.AccessToken, RefreshToken = authSession.RefreshToken };
                return View("Index", authInfo);
            }
            catch (BoxException e)
            {
                // Response.StatusCode = (int)e.StatusCode;
                return Error(". . .", e.Message);
            }
            catch (Exception e)
            {
                Response.StatusCode = 500;
                return Error(e.Message, e.StackTrace);
            }
        }

        /// <summary>
        /// Exchange the Box authorization code for an access/refresh token pair
        /// </summary>
        /// <param name="code">The Box authorization code provided when the user is redirected back to this site from Box</param>
        /// <param name="clientId">The Box application's client ID</param>
        /// <param name="clientSecret">The Box application's client secret</param>
        private static async Task<OAuthSession> ExchangeCodeForTokenPair(string code, string clientId, string clientSecret)
        {
            var boxClient = new BoxClient(new BoxConfig(clientId, clientSecret, new Uri("http://localhost:1176/Auth/Callback")));
            OAuthSession authSession = await boxClient.Auth.AuthenticateAsync(code);
            return authSession;
        }

        /// <summary>
        /// Validate that the Box authorization code has not been previously used.
        /// See also: http://www.twobotechnologies.com/blog/2014/02/importance-of-state-in-oauth2.html
        /// </summary>
        /// <param name="state">The state returned by Box when the user is redirected back to this site.</param>
        private bool ValidateAntiforgeryToken(string state)
        {
            //var existingState = Session[AntiforgeryToken] as string;
            return AntiforgeryToken == state;// !string.IsNullOrWhiteSpace(existingState) && existingState.Equals(state);
        }

        private void ClearSession()
        {
            Session[ClientId] = null;
            Session[ClientSecret] = null;
            Session[AntiforgeryToken] = null;
        }

        private bool IsCode()
        {
            return Request.QueryString["code"] != null;
        }

        private bool IsError()
        {
            return Request.QueryString["error"] != null;
        }

        private ViewResult Error(string error, string description)
        {
            ClearSession();
            return View("Error", new ErrorModel { Message = error, Description = description ?? "(none)" });
        }
    }
}
