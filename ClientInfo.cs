using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReleaseBot.Clients;

namespace ReleaseBot
{
    public class ClientInfo
    {
        public string Tag { get; set; }
        public ClientType Type { get; set; }
        public double Version { get; set; }
    }
}
