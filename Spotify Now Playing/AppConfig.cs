using System;
using System.Collections.Generic;
using System.Text;

namespace Spotify_Now_Playing
{
    class AppConfig
    {
        public string ClientId {get; set;}
        public string ClientSecret { get; set; }
        public string SaveDirectory { get; set; }
        public string RefreshToken { get; set; }
    }
}
