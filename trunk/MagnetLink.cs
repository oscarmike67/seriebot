using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using ReleaseBot.Clients;

namespace ReleaseBot
{
    public class MagnetLink
    {
        public string Link { get; set; }

        public static MagnetLink CreateMagnetLink(ClientInfo clientInfo, string name, int seasonNumber, int episodeNumber)
        {
            // If we have no client type, we cant know if client support this
            if (clientInfo == null)
                return null;

            ClientType clientType = clientInfo.Type;
            bool clientSupportsMagnet = false;

            string displayNameFormat = string.Empty;
            switch (clientType)
            {
                case ClientType.DCpp:
                    if (clientInfo.Version > 0.770d)
                    {
                        clientSupportsMagnet = true;
                    }
                    displayNameFormat = "{0} Season {1:00} Episode {2:00}";
                    break;
                case ClientType.Jucy:
                    if (clientInfo.Version > 0.85d)
                    {
                        clientSupportsMagnet = true;
                    }
                    displayNameFormat = "Search for next episode";
                    break;
            }

            if (clientSupportsMagnet)
            {
                StringBuilder sb = new StringBuilder(name);
                sb.Replace(":", string.Empty);
                sb.Replace(",", string.Empty);
                sb.Replace(".", string.Empty);
                sb.Replace("-", string.Empty);
                sb.Replace("_", string.Empty);
                sb.Replace(";", string.Empty);
                sb.Replace("'", string.Empty);
                sb.Replace("´", string.Empty);
                sb.Replace("<", string.Empty);
                sb.Replace(">", string.Empty);
                sb.Replace("|", string.Empty);
                sb.Replace("*", string.Empty);
                sb.Replace("(", string.Empty);
                sb.Replace(")", string.Empty);
                sb.Replace("[", string.Empty);
                sb.Replace("]", string.Empty);
                string convertedName = sb.ToString();

                //HttpServerUtility
                string displayName =
                    HttpUtility.UrlEncode(string.Format(displayNameFormat, name, seasonNumber, episodeNumber));
                string searchText =
                    HttpUtility.UrlEncode(string.Format("{0} s{1:00}e{2:00}", convertedName, seasonNumber, episodeNumber));

                return new MagnetLink {Link = string.Format("magnet:?kt={0}&dn={1}", searchText, displayName)};
            }else
            {
                return null;
            }
        }
    }
}
