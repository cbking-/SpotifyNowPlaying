using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TokenSwapServer.Models;

namespace TokenSwapServer.Controllers
{
    [ApiController]
    public class SwapController : ControllerBase
    {
        SpotifyConfig _config;
        readonly string AUTH_HEADER;
        public SwapController(SpotifyConfig config)
        {
            _config = config;

            AUTH_HEADER = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(_config.ClientId + ":" + _config.ClientSecret));
        }

        [HttpGet]
        [Route("")]
        public ActionResult Get()
        {
            var str = Request.QueryString.ToString();
 
            return Redirect($"https://accounts.spotify.com/authorize/{str}&client_id={_config.ClientId}&redirect_uri=http://localhost:4002/auth");
        }

        // POST: api/Swap
        [HttpPost]
        [Route("swap")]
        public ActionResult Swap([FromBody] string value)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return BadRequest();
                }

                Dictionary<string, string> kv = value.Split(' ')
                    .Select(x => x.Split('='))
                    .Where(x => x.Length == 2)
                    .ToDictionary(x => x.First(), x => x.Last());

                var postData = "grant_type=authorization_code";
                postData += "&redirect_uri=" + _config.CallbackURI;
                postData += "&code=" + kv["code"];

                var data = Encoding.ASCII.GetBytes(postData);

                WebRequest webRequest = WebRequest.Create("https://accounts.spotify.com/api/token");
                webRequest.Method = "POST";
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.Headers.Set("Authorization", AUTH_HEADER);

                webRequest.ContentLength = data.Length;

                using (var resBodyStream = webRequest.GetRequestStream())
                {
                    resBodyStream.Write(data, 0, data.Length);
                }

                HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return BadRequest();
                }

                string resBody = new StreamReader(response.GetResponseStream()).ReadToEnd();
                return Content(resBody, "application/json");
            }
            catch (Exception ex)
            {
                return BadRequest();
            }

        }

        // POST: api/Refresh
        [HttpPost]
        [Route("refresh")]
        public ActionResult Refresh([FromBody] string value)
        {
            try
            {

                if (string.IsNullOrWhiteSpace(value))
                {
                    return BadRequest();
                }

                Dictionary<string, string> kv = value.Split(' ')
                    .Select(x => x.Split('='))
                    .Where(x => x.Length == 2)
                    .ToDictionary(x => x.First(), x => x.Last());

                var postData = "grant_type=refresh_token";
                postData += "&refresh_token=" + kv["refresh_token"];

                var data = Encoding.ASCII.GetBytes(postData);

                WebRequest webRequest = WebRequest.Create("https://accounts.spotify.com/api/token");
                webRequest.Method = "POST";
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.Headers.Set("Authorization", AUTH_HEADER);
                webRequest.ContentLength = data.Length;

                using (var resBodyStream = webRequest.GetRequestStream())
                {
                    resBodyStream.Write(data, 0, data.Length);
                }

                HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return BadRequest();
                }

                string resBody = new StreamReader(response.GetResponseStream()).ReadToEnd();
                return Content(resBody, "application/json");
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }
    }
}
