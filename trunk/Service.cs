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
using ReleaseBot.Services;

namespace ReleaseBot
{
    public class Service
    {
        static string _directory = null;
        //static Regex _regExp = new Regex(@".*\\([a-zA-Z\.]+)\.[S|s]([0-9]{1,2})[E|e]([0-9]{0,2}).*\\.*", RegexOptions.Compiled);
		//static Regex _regExp = new Regex(@".*\\([a-zA-Z\.]+)\.[S|s]{0,1}([0-9]{1,2})[E|e|X|x]([0-9]{0,2}).*[\\|.].*", RegexOptions.Compiled);
		//static Regex _regExp = new Regex(@".*\\([a-zA-Z0-9\.]+)\.[S|s]{0,1}([0-9]{1,2})[E|e|X|x]([0-9]{0,2}).*[\\|.].*", RegexOptions.Compiled);
		//static Regex _regExp = new Regex(@".*\\(?<Name>[a-zA-Z0-9\.\ ]+)\.[S|s]{0,1}(?<Season>[0-9]{1,2})[E|e|X|x](?<Episode>[0-9]{0,2}).*[\\|.].*", RegexOptions.Compiled);
		static Regex _regExp = new Regex(@".*\\(?<Name>[a-zA-Z0-9\.\ \'\-]+)[\.|\ ][S|s]{0,1}(?<Season>[0-9]{1,2})[E|e|X|x](?<Episode>[0-9]{0,2}).*[\\|.].*", RegexOptions.Compiled);
		private static bool _NeedSleep;

        public static string Directory
        {
            get
            {
                return _directory;
            }
        }

        static Service()
        {
            _directory = AppDomain.CurrentDomain.BaseDirectory + System.IO.Path.DirectorySeparatorChar + "DATA" + System.IO.Path.DirectorySeparatorChar;
            if (!System.IO.Directory.Exists(_directory))
            {
                System.IO.Directory.CreateDirectory(_directory);
            }
        }

        public static string MakeKey(string name)
        {
            return FlowLib.Utils.Convert.Base32.Encode(System.Text.Encoding.UTF8.GetBytes(name.ToLower().Trim()));
        }

        public static bool TryGetSerie(string filename, out string name, out int seasonNr, out int episodeNr)
        {
            //DownloadHandler.LogMsg("TryGetSerie");
            Match m = _regExp.Match(filename);
            if (m.Success)
            {
                if (m.Groups.Count == 4)
                {
					StringBuilder sb = new StringBuilder(m.Groups["Name"].Value.ToLower());
					sb.Replace('.', ' ');
					sb.Replace('\'',' ');
					sb.Replace('-',' ');
					name = sb.ToString();
                    //name = m.Groups["Name"].Value.ToLower().Replace('.', ' ');
                    int.TryParse(m.Groups["Season"].Value, out seasonNr);
                    int.TryParse(m.Groups["Episode"].Value, out episodeNr);
                    //name = m.Groups[1].Value;
                    //version = m.Groups[2].Value;
                    //version = m.Groups[3].Value;
                    //DownloadHandler.LogMsg("/TryGetSerie[true]");
                    return true;
                }
            }
            name = null;
            seasonNr = -1;
            episodeNr = -1;
            //DownloadHandler.LogMsg("/TryGetSerie[false]");
            return false;
        }

        public static bool IsSerie(string name)
        {
            return _regExp.IsMatch(name);
        }

        public static DateTime GetNextUpdateForKey(string key)
        {
            DateTime updateDate = DateTime.MinValue;
            if (System.IO.File.Exists(_directory + key + ".update"))
            {
                if (DateTime.TryParse(System.IO.File.ReadAllText(_directory + key + ".update"), out updateDate))
                {
                    // If we have an invalid date, try to update every week.
                    if (updateDate == DateTime.MinValue)
                    {
                        updateDate = System.IO.File.GetLastWriteTime(_directory + key + ".update");
                        updateDate = updateDate.AddDays(7);
                    }
                }
            }
            else
            {
                // If we have no information about when to update, update every 2 week.
                updateDate = System.IO.File.GetLastWriteTime(_directory + key);
                updateDate = updateDate.AddDays(14);
            }
            return updateDate;
        }

        public static SerieInfo GetSerie(string name)
        {
            string url = name;
            string key = MakeKey(name);
            SerieInfo info = null;

            //string content = null;
            bool getNewContent = false;
            if (System.IO.File.Exists(_directory + key + ".xml"))
            {
                DateTime updateDate = GetNextUpdateForKey(key);
                if (updateDate < DateTime.Now)
                    getNewContent = true;
            }
            else
            {
                getNewContent = true;
            }

            if (getNewContent)
            {
                if (_NeedSleep)
                {
                    // Sleep X miliseconds
                    System.Threading.Thread.Sleep(250);
                }
                _NeedSleep = true;
                info = GetNewInfo(name);
                // Do we have a valid result?
                if (info != null)
                {
                    if (info is EmptySerieInfo)
                    {
                        System.IO.File.WriteAllText(_directory + key + ".bad", info.ServiceAddress);
                        info = null;
                    }
                    else
                    {
                        FlowLib.Utils.FileOperations<SerieInfo>.SaveObject(_directory + key + ".xml", info);
                    }
                }
            }
            else
            {
                _NeedSleep = false;
                info = FlowLib.Utils.FileOperations<SerieInfo>.LoadObject(_directory + key + ".xml");
            }

            if (info != null && info.NextEpisode != null && info.NextEpisode.Date > DateTime.MinValue)
            {
                System.IO.File.WriteAllText(_directory + key + ".update", info.NextEpisode.Date.ToString("yyyy-MM-dd"));
            }

            return info;
        }

        private static SerieInfo GetNewInfo(string name)
        {
            SerieInfo info = null;
			if (TvRage.IsValid)
			{
				info = TvRage.GetNewInfo(name);
			}

			// If we have no result. Test with some other TV service.
            if (info == null)
            {
				if (TheTvDb.IsValid)
				{
					info = TheTvDb.GetNewInfo(name);
				}
            }
            return info;
        }

        public static string GetCacheInfoFromKey(string key)
        {
            if (System.IO.File.Exists(_directory + key + ".xml"))
            {
                SerieInfo info = FlowLib.Utils.FileOperations<SerieInfo>.LoadObject(_directory + key + ".xml");
                //SerieInfo info = Parse(content);
                if (info == null)
                {
                    System.IO.File.Delete(_directory + key + ".xml");
                }
                else
                {
                    return string.Format("{0} [{1}] - Will be updated after: {2:yyyy-MM-dd}", info.Name, key, GetNextUpdateForKey(key));
                }
            }

            return string.Format("No cache found with Key: {0}.", key);
        }

        public static string GetCacheInfo(string name)
        {
            string key = MakeKey(name);
            return GetCacheInfoFromKey(key);
        }

        public static void CleanMemory()
        {
            // Reset service status
            TvRage.IsValid = true;
        }
    }
}
