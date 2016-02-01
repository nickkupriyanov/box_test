using Box.V2;
using Box.V2.Auth;
using Box.V2.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;

namespace serv.Controllers
{

    //public class CallBackParams
    //{
    //    public string url { get; set; }
    //    public string name { get; set; }
    //}

    public class CallBackParams
    {
        public string code { get; set; }
        public string state { get; set; }
    }

    public class CallBackParams2
    {
        public string ids { get; set; }
        public int length { get; set; }
    }


    public class BoxDriveController : ApiController
    {
        private const string ClientId = "s24y8ah30kx50uzzopct95s8belqi72e";
        private const string ClientSecret = "lojRF0VJimVNpOjJZALcqV0oGNoCnbi9";
        private const string AntiforgeryToken = "security_token=KnhMJatFipTAnM0nHlZA";

        //string stateValue = @"security_token%3DKnhMJatFipTAnM0nHlZA";

        //string[] ids;
        private static string code;

        [Route("BoxDrive/SaveFiles")]
        [HttpPost]
        [HttpGet]
        //[EnableCors(origins: "*", headers: "*", methods: "*")]
        public async void SaveFiles([FromUri] CallBackParams param )//string url, string name)//ids)
        {
            code = param.code;
            //this.ids = ids;

            //var url = string.Format(@"https://app.box.com/api/oauth2/authorize?response_type=code&client_id={0}&redirect_uri={1}&state={2}"
            //                       , ClientId
            //                       , "http%3A%2F%2Flocalhost%3A13473%2FBoxDrive%2FSaveFiles"
            //                       , AntiforgeryToken
            //                       );

            //var urlEncode = WebUtility.UrlEncode(url);
        }

        [Route("BoxDrive/CallbackSaveFiles")]
        [HttpPost]
        [HttpGet]
        async public Task<IHttpActionResult> CallbackSaveFiles([FromBody] CallBackParams2 param)
        {
            var boxClient = new BoxClient(new BoxConfig(ClientId, ClientSecret, new Uri("http://localhost:1176/Auth/Callback")));
            OAuthSession authSession = await boxClient.Auth.AuthenticateAsync(code);

            try
            {
                //foreach (var id in param.ids)
                {
                    var filestream = await boxClient.FilesManager.DownloadStreamAsync(param.ids);

                    var path = Path.GetTempFileName();
                    var bytes = new byte[param.length];
                    filestream.Read(bytes, 0, bytes.Length);

                    File.OpenWrite(path).Write(bytes, 0, bytes.Length);
                }
            }
            catch (Exception ex)
            {

                throw;
            }

            return Ok();
        }
    }
}
