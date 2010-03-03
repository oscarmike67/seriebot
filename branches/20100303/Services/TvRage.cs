/*
 *
 * Copyright (C) 2010 Mattias Blomqvist, seriebot at flowertwig dot org
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace ReleaseBot.Services
{
    public class TvRage
    {
        const string SERVICE_ADDRESS = "www.TvRage.com";
        static string _service = "http://services.tvrage.com/tools/quickinfo.php?show=";
        public static bool IsValid
        {
            get;
            set;
        }

        static TvRage()
        {
            IsValid = true;
        }

        public static SerieInfo GetNewInfo(string name)
        {
            try
            {
                string content = FlowLib.Utils.WebOperations.GetPage(_service + HttpUtility.UrlEncode(name));
                return Parse(content);
            }
            catch (System.Exception ex)
            {
                return null;
            }
        }

        private static SerieInfo Parse(string raw)
        {
            SerieInfo info = new SerieInfo();
            info.ServiceAddress = SERVICE_ADDRESS;
            string[] lines = raw.Split('\n');
            foreach (var line in lines)
            {
                string[] pair = line.Split('@');
                if (pair.Length != 2)
                    continue;
                switch (pair[0])
                {
                    case "Show Name": info.Name = pair[1]; break;
                    case "Status": info.Status = pair[1]; break;
                    case "Genres": info.Genres = pair[1]; break;
                    case "Next Episode": info.NextEpisode = EpisodeInfo.Parse(pair[1]); break;
                    case "Latest Episode": info.LatestEpisode = EpisodeInfo.Parse(pair[1]); break;
                    case "Show URL": info.URL = pair[1]; break;
                }
            }

            if (!string.IsNullOrEmpty(info.Name))
            {
                IsValid = true;
                return info;
            }

            if (!string.IsNullOrEmpty(raw) && raw.StartsWith("No Show Results"))
            {
                return new EmptySerieInfo { ServiceAddress = SERVICE_ADDRESS };
            }
            IsValid = false;
            return null;
        }

    }
}
