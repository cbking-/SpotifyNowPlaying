using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TokenSwapServer.Models
{
    public class SpotifyConfig
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }

        public string CallbackURI { get; set; }
    }
}
