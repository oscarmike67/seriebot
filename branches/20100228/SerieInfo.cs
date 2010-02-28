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

namespace ReleaseBot
{
    public class SerieInfo : IComparable
    {
        private static EmptySerieInfo _empty = new EmptySerieInfo();
        public static EmptySerieInfo Empty
        {
            get { return _empty; }
        }

        public string Name
        {
            get;
            set;
        }

        public string ServiceAddress
        {
            get;
            set;
        }

        public string Status
        {
            get;
            set;
        }

        public string URL
        {
            get;
            set;
        }
        public string Genres
        {
            get;
            set;
        }

        public EpisodeInfo NextEpisode
        {
            get;
            set;
        }

        public EpisodeInfo LatestEpisode
        {
            get;
            set;
        }

        public override bool Equals(object obj)
        {
            return CompareTo(obj) == 0;
        }

        #region IComparable Members

        public int CompareTo(object obj)
        {
            SerieInfo otherInfo = obj as SerieInfo;
            if (otherInfo != null)
            {
                return string.Compare(Name, otherInfo.Name);
            }
            throw new NotImplementedException();
        }

        #endregion

        public override string ToString()
        {
            return Name;
        }
    }
}
