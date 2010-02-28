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

namespace ReleaseBot.Services.TheTvDbUtils
{
	public static class ExtensionMethods
	{
		public static MirrorTypes ToMirrorTypes(this string strTypes)
		{
			try
			{
				return (MirrorTypes)Enum.Parse(typeof(MirrorTypes), strTypes);
			}
			catch (Exception ex)
			{
				return MirrorTypes.None;
			}
		}

		public static DateTime ToDateTime(this string str)
		{
			DateTime dt;
			DateTime.TryParse(str, out dt);
			return dt;
		}

		public static int ToEpisodeVersion(this string str)
		{
			int seasonNr = 0;
			int episodeNr = 0;
			string[] sep = str.Split('x');
			if (sep.Length == 2)
			{
				int.TryParse(sep[0], out seasonNr);
				int.TryParse(sep[1], out episodeNr);
				return (seasonNr * 100) + episodeNr;
			}
			return 0;
		}

		private static Random _random = new Random();

		public static Mirror TakeSingleAtRandom(this IEnumerable<Mirror> collection)
		{
			return collection.ElementAt(_random.Next(0, collection.Count() -1));
		}
	}
}