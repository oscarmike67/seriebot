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
using FlowLib.Containers;
using FlowLib.Events;
using FlowLib.Utils.FileLists;
using System.IO;
using System.Threading;

namespace ReleaseBot
{
    public class DownloadHandler
    {
        public static bool TryHandleDownload(DcBot connection, DownloadItem item, Source source)
        {
            if (item != null && source != null)
            {
                string fileType = item.ContentInfo.Get(ContentInfo.FILELIST);
                string func = item.ContentInfo.Get("FUNC");
                string path = item.ContentInfo.Get(ContentInfo.STORAGEPATH);
                string usrId = item.ContentInfo.Get("USR");

                byte[] data = null;

                BaseFilelist filelist = null;

                int i = 0;
                while (i < 10)
                {
                    try
                    {
                        data = File.ReadAllBytes(path);
                        break;
                    }
                    catch (Exception)
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                    finally
                    {
                        i++;
                    }

                }

                if (data == null)
                    return false;

                switch (fileType)
                {
                    case BaseFilelist.XMLBZ:
                        filelist = new FilelistXmlBz2(data, true);
                        break;
                    case BaseFilelist.XML:
                        filelist = new FilelistXmlBz2(data, false);
                        break;
                    case BaseFilelist.BZ:
                        filelist = new FilelistMyList(data, true);
                        break;
                    default:
                        connection.SendMessage(Actions.PrivateMessage, usrId, "Unrecognized filelist.");
                        return false;
                }

                LogMsg("CreateShare");
                filelist.CreateShare();
                LogMsg("/CreateShare");
                Share share = filelist.Share as Share;

                File.Delete(path);

                if (share != null)
                {
                    if ("new".Equals(func))
                    {
                        FuncNew(connection, share, usrId);
                    }
                }
            }

            return false;
        }

        public static void LogMsg(string str)
        {
            Program.WriteLine(String.Format("*** {1}:\t\t\t{0}", DateTime.Now.Ticks, str));
        }

        private static Testing Test(IEnumerable<KeyValuePair<string, ContentInfo>> tmp)
        {
            var t = new Testing
            {
                Data = tmp,
                Progress = 0
            };
            var thrd = new Thread(new ParameterizedThreadStart(OnTest));
            thrd.IsBackground = true;
            thrd.Start(t);
            return t;
        }

        private static void OnTest(object obj)
        {
            Testing t = obj as Testing;
            IEnumerable<KeyValuePair<string, ContentInfo>> tmp = t.Data;

            var listIgnore = new SortedList<string, int>();
            var listWithDuplicates = new SortedList<string, int>();

            foreach (KeyValuePair<string, ContentInfo> ci in tmp)
            {
                string name;
                int seasonNr, episodeNr;
                string filename = ci.Value.Get(ContentInfo.VIRTUAL);
                if (Ignore.TryAddIgnore(filename, out name))
                {
                    if (!listIgnore.ContainsKey(name))
                    {
                        listIgnore.Add(name, 0);
                    }
                }
                else if (Service.TryGetSerie(filename, out name, out seasonNr, out episodeNr))
                {
                    int version = seasonNr = (seasonNr * 100) + episodeNr;
                    if (!listWithDuplicates.ContainsKey(name))
                    {
                        listWithDuplicates.Add(name, version);
                    }
                    else
                    {
                        int tmpVersion = listWithDuplicates[name];
                        if (tmpVersion < version)
                        {
                            listWithDuplicates.Remove(name);
                            listWithDuplicates.Add(name, version);
                        }
                    }
                }
            }
            //if (listIgnore.Count != 0)
            t.IgnoreList = listIgnore;
            //if (listWithDuplicates.Count != 0)
            t.DuplicatesList = listWithDuplicates;
            t.Progress = 1;
        }

        public static void FuncNew(DcBot connection, Share share, string usrId)
        {
            int lines = 0;
            StringBuilder sb = new StringBuilder("Your current serie information:\r\n");
            lines++;
            //SortedList<string, int> listIgnore = new SortedList<string, int>();
            //SortedList<string, int> listWithDuplicates = new SortedList<string, int>();

            //IEnumerable<KeyValuePair<string, int>> listIgnore;
            //IEnumerable<KeyValuePair<string, int>> listWithDuplicates;


            #region Get latest version of all series
            LogMsg("Copy and split share");
            IEnumerable<KeyValuePair<string, ContentInfo>> tmp = share;

            int count = tmp.Count();

            int length = count / 2;
            var t1 = tmp.Take(length);
            var t2 = tmp.Skip(count - (length + 1));

            LogMsg("/Copy and split share");


            LogMsg("Find Ignore and Series");

            var t1Func = Test(t1);
            var t2Func = Test(t2);

            // Sleep while threads are working..
            while (t1Func.Progress == 0 || t2Func.Progress == 0)
            {
                Thread.Sleep(100);
            }

            System.Collections.Specialized.StringDictionary sd = new System.Collections.Specialized.StringDictionary();

            var listIgnore = t1Func.IgnoreList.Union(t2Func.IgnoreList).ToDictionary(f => f.Key, System.Collections.Generic.EqualityComparer<string>.Default);
            //var listWithDuplicates = t1Func.DuplicatesList.Union(t2Func.DuplicatesList).ToDictionary(f => f.Key, System.Collections.Generic.EqualityComparer<string>.Default);
            //var listWithDuplicates = t1Func.DuplicatesList.Where(f => !t2Func.DuplicatesList.ContainsKey(f.Key) || f.Value > t2Func.DuplicatesList[f.Key]).Union(t2Func.DuplicatesList).ToDictionary(f => f.Key, System.Collections.Generic.EqualityComparer<string>.Default);

            var listWithDuplicates = t1Func.DuplicatesList.Where(
                f => !t2Func.DuplicatesList.ContainsKey(f.Key)
                    || f.Value >= t2Func.DuplicatesList[f.Key]).Union(t2Func.DuplicatesList.Where(
                f2 => !t1Func.DuplicatesList.ContainsKey(f2.Key)
                    || f2.Value > t1Func.DuplicatesList[f2.Key]))
                    .ToDictionary(f3 => f3.Key, System.Collections.Generic.EqualityComparer<string>.Default);

            //var listWithDuplicates = t1Func.DuplicatesList.Where(
            //    f => !t2Func.DuplicatesList.ContainsKey(f.Key)
            //        || f.Value > t2Func.DuplicatesList[f.Key]).Union(t2Func.DuplicatesList.Where(
            //    f2 => !t1Func.DuplicatesList.ContainsKey(f2.Key)
            //        || f2.Value > t1Func.DuplicatesList[f2.Key]))
            //        .ToDictionary(f3 => f3.Key, System.Collections.Generic.EqualityComparer<string>.Default);


            LogMsg("/Find Ignore and Series");
            #endregion

            #region Get info from series and remove duplicates (happens because of different folder names)
            SortedList<SerieInfo, int> list = new SortedList<SerieInfo, int>();

            LogMsg("Get Series");
            foreach (var seriePair in listWithDuplicates)
            {
                SerieInfo info = Service.GetSerie(seriePair.Key);
                if (info != null)
                {
                    bool addValue = true;
                    if (list.ContainsKey(info))
                    {
                        if (list[info] >= seriePair.Value.Value)
                        {
                            addValue = false;
                        }
                        else
                        {
                            list.Remove(info);
                        }
                    }

                    if (addValue)
                        list.Add(info, seriePair.Value.Value);
                }
            }
            LogMsg("/Get Series");
            #endregion

            int ignoreCount = listIgnore.Count();
            sb.AppendFormat("I have found {0} different series in your share.\r\n", list.Count);
            lines++;
            sb.AppendFormat("You want me to ignore {0} of them.", ignoreCount);
            if (ignoreCount == 0)
            {
                sb.Append(" To learn more. Please write +ignore.");
            }
            sb.AppendLine();
            lines++;

            #region Get info about series
            LogMsg("Display Series");
            bool anyInfo = false;
            List<string> servicesUsed = new List<string>();

            foreach (var seriePair in list)
            {
                SerieInfo info = seriePair.Key;
                if (info != null && !listIgnore.ContainsKey(Ignore.CreateName(info.Name)))
                {
                    EpisodeInfo ep = info.LatestEpisode;
                    if (ep != null)
                    {
                        int currentSeason = ep.Version / 100;
                        int currentEpisode = ep.Version % 100;

                        int usrSeason = seriePair.Value / 100;
                        int usrEpisode = seriePair.Value % 100;

                        bool addedInfo = false;

                        if (currentSeason > usrSeason)
                        {
                            if (currentSeason == (usrSeason + 1))
                            {
                                sb.AppendFormat("\t{0}: A new season have started.", info.Name);
                                addedInfo = true;
                            }
                            else
                            {
                                sb.AppendFormat("\t{0}: You are behind more then one season.", info.Name);
                                addedInfo = true;
                            }
                        }
                        else if (currentSeason == usrSeason)
                        {
                            if (currentEpisode > usrEpisode)
                            {
                                int difEpisode = currentEpisode - usrEpisode;
                                if (difEpisode == 1)
                                {
                                    sb.AppendFormat("\t{0}: You are behind {1} episode.", info.Name, difEpisode);
                                    addedInfo = true;
                                }
                                else
                                {
                                    sb.AppendFormat("\t{0}: You are behind {1} episodes.", info.Name, difEpisode);
                                    addedInfo = true;
                                }
                            }
                        }

                        if (addedInfo)
                        {
                            anyInfo = true;
                            sb.AppendFormat("\t\t(Your last episode is: S{0:00}E{1:00})\r\n", usrSeason, usrEpisode);
                            servicesUsed.Add(info.ServiceAddress);
                            lines++;
                        }
                    }
                }

                // Make sure we are not exceeding max number of lines in hub.
                if (Program.MAX_NUMBER_OF_LINES_IN_MESSAGE <= lines)
                {
                    connection.SendMessage(Actions.PrivateMessage, usrId, sb.ToString());
                    sb = new StringBuilder();
                    lines = 0;
                }
            }
            LogMsg("/Display Series");

            if (!anyInfo)
            {
                sb.AppendLine("You seem to have latest episode of every serie you are sharing!");
            }

            sb.AppendLine();
            sb.AppendLine();


            sb.Append("This result was given to you by: http://code.google.com/p/seriebot/ ");
            string[] servicesUsedDistinct = servicesUsed.Distinct().ToArray();
            int serviceCount = servicesUsedDistinct.Length;
            if (serviceCount > 0)
            {
                sb.Append("with the help by: ");
                sb.AppendLine(string.Join(", ", servicesUsedDistinct));
            }
            else
            {
                sb.AppendLine();
            }

            //sb.AppendLine("This service is powered by: www.tvrage.com");

            // message will here be converted to right format and then be sent.
            connection.SendMessage(Actions.PrivateMessage, usrId, sb.ToString());
            #endregion
        }
    }
}
