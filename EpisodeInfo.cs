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
using System.Text.RegularExpressions;

namespace ReleaseBot
{
    public class EpisodeInfo
    {
        private static Regex regExp = new Regex(@"^([0-9]{1,2})x([0-9]{1,2})\^(.*)\^(.*)$", RegexOptions.IgnoreCase);

        public string Name
        {
            get;
            set;
        }
        public DateTime Date
        {
            get;
            set;
        }
        public int Version
        {
            get;
            set;
        }

        public string RawFileName { get; set; }

        public static EpisodeInfo Parse(string str)
        {
            //  1  2             3                   4
            // 05x17^All the Time in the World^May/22/2006
            if (str == null)
                return null;
            Match m = regExp.Match(str);
            if (m.Success)
            {
                EpisodeInfo ep = new EpisodeInfo();
                int seasonNr, episodeNr;
                int.TryParse(m.Groups[1].Value, out seasonNr);
                int.TryParse(m.Groups[2].Value, out episodeNr);
                ep.Version = (seasonNr * 100) + episodeNr;
                ep.Name = m.Groups[3].Value;

                DateTime dat = DateTime.MinValue;
                DateTime.TryParse(m.Groups[4].Value, out dat);

                ep.Date = dat;
                return ep;
            }
            return null;
        }

        public override string ToString()
        {
            return string.Format("[S{2:00}E{3:00}] {0} - {1:yyyy-MM-dd}", this.Name, this.Date, this.Version / 100, this.Version % 100);
        }
    }
}
