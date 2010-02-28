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
using System.Xml;
using System.Xml.Linq;
using ReleaseBot.Services.TheTvDbUtils;

namespace ReleaseBot.Services
{
    public class TheTvDb
    {
		const string SERVICE_ADDRESS = "www.TheTvDb.com";
		const string SERVICE_MIRRORS = "http://www.thetvdb.com/api/{0}/mirrors.xml";
		static string _service = "http://www.thetvdb.com/api/GetSeries.php?seriesname=";
		const string API_KEY = "0E122B1BB1298F0F";
		static Mirror _currentMirror;

		public static bool IsValid
		{
			get;
			set;
		}

		static TheTvDb()
		{
			string content = FlowLib.Utils.WebOperations.GetPage(string.Format(SERVICE_MIRRORS, API_KEY));
			if (string.IsNullOrEmpty(content))
				return;	// Something went wrong. Do not use this service.
			XDocument xmlDoc = XDocument.Load(string.Format(SERVICE_MIRRORS, API_KEY));

			var mirrors = from mirror in xmlDoc.Descendants("Mirror")
						  select new Mirror
						  {
							  Id = mirror.Element("id").Value,
							  Path = mirror.Element("mirrorpath").Value,
							  TypeMask = mirror.Element("typemask").Value.ToMirrorTypes()
						  };
			_currentMirror = mirrors.Where(f => ((MirrorTypes.Xml & f.TypeMask) == MirrorTypes.Xml)).TakeSingleAtRandom();
			if (_currentMirror != null)
			{
				IsValid = true;
			}
		}

        public static SerieInfo GetNewInfo(string name)
        {
            try
            {
				string url = _service;
				string content = FlowLib.Utils.WebOperations.GetPage(url + HttpUtility.UrlEncode(name));
                SerieInfo info = ParseGetSerie(content);

				if (info == null || info is EmptySerieInfo)
					return info;

				content = FlowLib.Utils.WebOperations.GetPage(info.URL);
				AppendSerieInfo(content, ref info);

				return info;
            }
            catch (System.Exception ex)
            {
                return null;
            }
        }

		private static void AppendSerieInfo(string raw, ref SerieInfo info)
		{
			try
			{
				XDocument xdoc = XDocument.Parse(raw);
				string geners = (from extendedInfo in xdoc.Descendants("Series")
							 select extendedInfo.Element("Genre").Value).FirstOrDefault();
				info.Genres = geners.Trim('|').Replace("|", " | ");

				var episodes = from ep in xdoc.Descendants("Episode")
							   select new EpisodeInfo
							   {
								    Name = ep.Element("EpisodeName").Value,
									 Date = ep.Element("FirstAired").Value.ToDateTime(),
									Version = (ep.Element("SeasonNumber").Value + "x" + ep.Element("EpisodeNumber").Value).ToEpisodeVersion()
							   };

				DateTime dateNow = DateTime.Now;
				// Get last sent episode
				info.LatestEpisode = episodes.Reverse().SkipWhile(f => f.Date > dateNow || f.Date == DateTime.MinValue).FirstOrDefault();

				// Get Next episode
				info.NextEpisode = episodes.Where(f => f.Date > dateNow).FirstOrDefault();
			}
			catch (Exception) { }
		}

		private static SerieInfo ParseGetSerie(string raw)
		{
			try
			{
				XDocument xdoc = XDocument.Parse(raw);
				var series = from s in xdoc.Descendants("Series")
							 select new SerieInfo
							 {
								 Name = s.Element("SeriesName").Value,
								 URL = string.Format("{0}/api/{1}/series/{2}/all/en.xml", _currentMirror.Path, API_KEY, s.Element("seriesid").Value),
								 ServiceAddress = SERVICE_ADDRESS
							 };
				if (series.Count() > 0)
				{
					IsValid = true;
					return series.FirstOrDefault();
				}

				return new EmptySerieInfo { ServiceAddress = SERVICE_ADDRESS };
			}
			catch (Exception ex) { }

			IsValid = false;
			return null;
		}
	}
}
