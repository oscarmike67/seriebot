using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ReleaseBot.Clients;

namespace ReleaseBot
{
    public class ClientParser
    {
        public static ClientInfo Parse(string rawInfo)
        {
            string[] sections = rawInfo.Split(' ');
            if (sections.Length == 2)
            {
                string key = sections[0];
                string value = sections[1];
                double version;
                if (double.TryParse(value,NumberStyles.Float, CultureInfo.GetCultureInfo("en-GB").NumberFormat, out version))
                {
                    var clientInfo = new ClientInfo {Tag = key, Version = version};
                    switch (clientInfo.Tag)
                    {
                        case "++":
                            clientInfo.Type = ClientType.DCpp;
                            break;
                        case "UC":
                            clientInfo.Type = ClientType.Jucy;
                            break;
                        default:
                            clientInfo.Type = ClientType.Unknown;
                            break;
                    }
                    return clientInfo;
                }
            }
            return null;
        }
    }
}
