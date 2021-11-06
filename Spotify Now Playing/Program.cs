using Microsoft.Extensions.Configuration;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;

namespace Spotify_Now_Playing
{
    class Program
    {
        private static AppConfig _appConfig;
        private static HttpClient _httpClient;
        private static SpotifyWebAPI _api;
        private static Token _token;
        private static AuthorizationCodeAuth _auth;
        private static Timer _refreshTimer;
        private static string _prevTrackId = "";

        static async Task Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            _appConfig = config.GetSection("AppConfig").Get<AppConfig>();
            _httpClient = new HttpClient();

            _auth = new AuthorizationCodeAuth(_appConfig.ClientId, _appConfig.ClientSecret, "http://localhost:4002", "http://localhost:4002", Scope.UserReadCurrentlyPlaying);

            if (_appConfig.RefreshToken == "")
            {
                _auth.AuthReceived += AuthOnAuthReceived;
                _auth.Start();
                _auth.OpenBrowser();
            }
            else
            {
                _token = await _auth.RefreshToken(_appConfig.RefreshToken);
                SetupSpotify();
            }
            
            Console.ReadLine();
            _auth.Stop();
        }

        private static void SetupSpotify()
        {
            _api = new SpotifyWebAPI
            {
                AccessToken = _token.AccessToken,
                TokenType = _token.TokenType
            };

            var saveTimer = new Timer(3000);
            saveTimer.Elapsed += SaveNowPlaying;
            saveTimer.AutoReset = true;
            saveTimer.Enabled = true;

            _refreshTimer = new Timer(_token.ExpiresIn * 1000);
            _refreshTimer.Elapsed += TokenRefresh;
            _refreshTimer.AutoReset = true;
            _refreshTimer.Enabled = true;
        }

        private static async void AuthOnAuthReceived(object sender, AuthorizationCode payload)
        {
            var auth = (AuthorizationCodeAuth)sender;
            auth.Stop();

            _token = await auth.ExchangeCode(payload.Code);
            _appConfig.RefreshToken = _token.RefreshToken;

            var filePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            var json = File.ReadAllText(filePath);
            dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(json);

            jsonObj.AppConfig.RefreshToken = _token.RefreshToken;

            string output = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(filePath, output);

            SetupSpotify();
        }

        private static void SaveNowPlaying(object source, ElapsedEventArgs e)
        {
            var nowPlaying = _api.GetPlayingTrack();

            if (!(nowPlaying.Item is null) && _prevTrackId != nowPlaying.Item.Id)
            {
                Console.WriteLine("*************");
                _prevTrackId = nowPlaying.Item.Id;

                var artistPath = Path.Combine(_appConfig.SaveDirectory, "now_playing.txt");
                var albumArtPath = Path.Combine(_appConfig.SaveDirectory, "art.jpg");

                using (var writer = File.CreateText(artistPath))
                {
                    Console.WriteLine("Updating file with:");

                    Console.WriteLine("\t" + string.Join(',', nowPlaying.Item.Artists.Select(x => x.Name)));
                    writer.WriteLine(string.Join(',', nowPlaying.Item.Artists.Select(x => x.Name)));

                    Console.WriteLine("\t" + nowPlaying.Item.Album.Name);
                    writer.WriteLine(nowPlaying.Item.Album.Name);

                    Console.WriteLine("\t" + nowPlaying.Item.Name);
                    writer.WriteLine(nowPlaying.Item.Name);
                }

                var albumLink = nowPlaying.Item.Album.Images.OrderBy(x => x.Height).FirstOrDefault().Url;

                byte[] buffer = _httpClient.GetByteArrayAsync(albumLink).Result;

                using (var writer = File.Create(albumArtPath))
                {
                    Console.WriteLine("Saving album art");
                    writer.Write(buffer);
                }

                Console.WriteLine("*************");
            }
        }
        private static void TokenRefresh(object source, ElapsedEventArgs e)
        {
            Console.WriteLine("Refreshing Token");
            Token newToken = _auth.RefreshToken(_appConfig.RefreshToken).Result;
            _api.AccessToken = newToken.AccessToken;
            _api.TokenType = newToken.TokenType;
            _refreshTimer.Interval = newToken.ExpiresIn * 1000;
        }
    }    
}
