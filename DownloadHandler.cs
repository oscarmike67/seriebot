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
                    switch (func)
                    {
                        case "new":
                            FuncListShare(connection, share, usrId, FunctionTypes.ListNewEpisodes); break;
                        case "list":
                            FuncListShare(connection, share, usrId, FunctionTypes.ListAllEpisodes); break;
                        case "debug":
                            FuncListShare(connection, share, usrId, FunctionTypes.ListDebugInfoOnEpisodes); break;
                        case "countdown":
                        case "cd":
                            FuncCountDownShare(connection, share, usrId); break;
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
            var listWithDuplicates = new SortedList<string, EpisodeInfo>();

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

                    EpisodeInfo ep = new EpisodeInfo();
                    ep.Version = version;
                    ep.RawFileName = filename;

                    if (!listWithDuplicates.ContainsKey(name))
                    {
                        listWithDuplicates.Add(name, ep);
                    }
                    else
                    {
                        EpisodeInfo tmpEp = listWithDuplicates[name];
                        if (tmpEp.Version < version)
                        {
                            listWithDuplicates.Remove(name);
                            listWithDuplicates.Add(name, ep);
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

        protected static void GetSeriesFromShare(Share share, out SortedList<SerieInfo, EpisodeInfo> list, out Dictionary<string, KeyValuePair<string, int>> listIgnore)
        {
            list = new SortedList<SerieInfo, EpisodeInfo>();
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

            listIgnore = t1Func.IgnoreList.Union(t2Func.IgnoreList).ToDictionary(f => f.Key, System.Collections.Generic.EqualityComparer<string>.Default);

            var listWithDuplicates = t1Func.DuplicatesList.Where(
                f => !t2Func.DuplicatesList.ContainsKey(f.Key)
                    || f.Value.Version >= t2Func.DuplicatesList[f.Key].Version).Union(t2Func.DuplicatesList.Where(
                f2 => !t1Func.DuplicatesList.ContainsKey(f2.Key)
                    || f2.Value.Version > t1Func.DuplicatesList[f2.Key].Version))
                    .ToDictionary(f3 => f3.Key, System.Collections.Generic.EqualityComparer<string>.Default);

            LogMsg("/Find Ignore and Series");
            #endregion

            #region Get info from series and remove duplicates (happens because of different folder names)
            //SortedList<SerieInfo, EpisodeInfo> list = new SortedList<SerieInfo, EpisodeInfo>();

            LogMsg("Get Series");
            foreach (var seriePair in listWithDuplicates)
            {
                SerieInfo info = Service.GetSerie(seriePair.Key);
                if (info != null)
                {
                    bool addValue = true;
                    if (list.ContainsKey(info))
                    {
                        if (list[info].Version >= seriePair.Value.Value.Version)
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
        }

        public static void FuncCountDownShare(DcBot connection, Share share, string usrId)
        {
            int lines = 0;
            bool anyInfo = false;
            DateTime todaysDate = DateTime.Now.Date;
            List<string> servicesUsed = new List<string>();
            SortedList<SerieInfo, EpisodeInfo> list;
            Dictionary<string, KeyValuePair<string, int>> listIgnore;

            StringBuilder sb = new StringBuilder("Countdown of your Series:\r\n");
            lines++;

            GetSeriesFromShare(share, out list, out listIgnore);

            // Get series and make sure we order it on total days left and serie name..
            SortedList<string , SerieInfo> listOrderedByDate = new SortedList<string, SerieInfo>();
            foreach (var seriePair in list)
            {
                SerieInfo info = seriePair.Key;
                if (info != null && !listIgnore.ContainsKey(Ignore.CreateName(info.Name)))
                {
                    EpisodeInfo epNext = info.NextEpisode;
                    if (epNext != null)
                    {
                        var difference = epNext.Date.Subtract(todaysDate);
                        double totalDays = difference.TotalDays;
                        if (totalDays >= 0)
                        {
                            string key = string.Format("{0:000}-{1}", totalDays, info.Name);
                            listOrderedByDate.Add(key, info);

                            servicesUsed.Add(info.ServiceAddress);

                            anyInfo = true;
                        }
                    }
                }
            }

            List<string> outputList = new List<string>();
            DateTime lastDate = DateTime.MinValue;
            DateTime today = DateTime.Today;
            DateTime tomorrow = DateTime.Today.AddDays(1);
            int nOfDaysLeftInWeek = 0;
            switch (today.DayOfWeek)
            {
                case DayOfWeek.Monday:
                    nOfDaysLeftInWeek = 6;
                    break;
                case DayOfWeek.Tuesday:
                    nOfDaysLeftInWeek = 5;
                    break;
                case DayOfWeek.Wednesday:
                    nOfDaysLeftInWeek = 4;
                    break;
                case DayOfWeek.Thursday:
                    nOfDaysLeftInWeek = 3;
                    break;
                case DayOfWeek.Friday:
                    nOfDaysLeftInWeek = 2;
                    break;
                case DayOfWeek.Saturday:
                    nOfDaysLeftInWeek = 1;
                    break;
                case DayOfWeek.Sunday:
                default:
                    nOfDaysLeftInWeek = 0;
                    break;
            }

            bool nextWeekHasHit = false;
            bool moreThenAMonthHasHit = false;

            foreach (var orderedPair in listOrderedByDate)
            {
                bool showDateAfterName = false;
                bool stuffAdded = false;

                if (DateTime.Compare(lastDate, orderedPair.Value.NextEpisode.Date.Date) < 0)
                {
                    lastDate = orderedPair.Value.NextEpisode.Date.Date;

                    if (DateTime.Equals(today, lastDate.Date))
                    {
                        sb.Append("\tToday");
                        sb.AppendFormat(" ({0:yyyy-MM-dd}):", lastDate);
                        stuffAdded = true;
                    }
                    else if (DateTime.Equals(tomorrow, lastDate.Date))
                    {
                        sb.Append("\tTomorrow");
                        sb.AppendFormat(" ({0:yyyy-MM-dd}):", lastDate);
                        stuffAdded = true;
                    }
                    else
                    {
                        // How many days have to pass until this date?
                        var timeLeft = lastDate.Subtract(today);
                        // Does this date occure this week?
                        if (timeLeft.TotalDays <= nOfDaysLeftInWeek)
                        {
                            switch (lastDate.DayOfWeek)
                            {
                                case DayOfWeek.Monday:
                                    sb.Append("\tMonday");
                                    break;
                                case DayOfWeek.Tuesday:
                                    sb.Append("\tTuesday");
                                    break;
                                case DayOfWeek.Wednesday:
                                    sb.Append("\tWednesday");
                                    break;
                                case DayOfWeek.Thursday:
                                    sb.Append("\tThursday");
                                    break;
                                case DayOfWeek.Friday:
                                    sb.Append("\tFriday");
                                    break;
                                case DayOfWeek.Saturday:
                                    sb.Append("\tSaturday");
                                    break;
                                case DayOfWeek.Sunday:
                                    sb.Append("\tSunday");
                                    break;
                            }
                            sb.AppendFormat(" ({0:yyyy-MM-dd}):", lastDate);
                            stuffAdded = true;
                        }
                        else if (timeLeft.TotalDays <= 7 + nOfDaysLeftInWeek)  // Does this date occure next week?
                        {
                            if (!nextWeekHasHit)
                            {
                                sb.Append("\tNext week");
                                var dateBeginingOfNextWeek = today.AddDays(1 + nOfDaysLeftInWeek);
                                var dateEndingOfNextWeek = today.AddDays(7 + nOfDaysLeftInWeek);
                                sb.AppendFormat(" ({0:yyyy-MM-dd} -> {1:yyyy-MM-dd}):", dateBeginingOfNextWeek,
                                                dateEndingOfNextWeek);

                                nextWeekHasHit = true;
                                stuffAdded = true;
                            }
                            showDateAfterName = true;
                        }else
                        {
                            if (!moreThenAMonthHasHit)
                            {
                                sb.Append("\tMore than 2 weeks:");
                                moreThenAMonthHasHit = true;
                                stuffAdded = true;
                            }
                            showDateAfterName = true;
                        }
                    }

                    if (stuffAdded)
                    {
                        sb.AppendLine();
                        lines++;
                    }
                }

                sb.Append("\t\t");
                sb.Append(orderedPair.Value.Name);

                if (showDateAfterName)
                {
                    sb.AppendFormat(" ({0:yyyy-MM-dd})", lastDate);
                }

                // Make sure we are not exceeding max number of lines in hub.
                if (Program.MAX_NUMBER_OF_LINES_IN_MESSAGE <= lines)
                {
                    connection.SendMessage(Actions.PrivateMessage, usrId, sb.ToString());
                    sb = new StringBuilder();
                    lines = 0;
                }
                
                sb.AppendLine();
                lines++;
            }

            LogMsg("/Display Series");

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
        }

        public static void FuncListShare(DcBot connection, Share share, string usrId, FunctionTypes funcType)
        {
            int lines = 0;
            bool anyInfo = false;
            DateTime todaysDate = DateTime.Now.Date;
            List<string> servicesUsed = new List<string>();
            SortedList<SerieInfo, EpisodeInfo> list;
            Dictionary<string, KeyValuePair<string, int>> listIgnore;


            StringBuilder sb = new StringBuilder("Your current serie information:\r\n");
            lines++;

            GetSeriesFromShare(share, out list, out listIgnore);

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

            foreach (var seriePair in list)
            {
                SerieInfo info = seriePair.Key;
                if (info != null && !listIgnore.ContainsKey(Ignore.CreateName(info.Name)))
                {
                    EpisodeInfo epLast = info.LatestEpisode;
                    EpisodeInfo epNext = info.NextEpisode;

                    if (epLast != null)
                    {
                        int currentSeason = epLast.Version / 100;
                        int currentEpisode = epLast.Version % 100;

                        int usrSeasonVersion = seriePair.Value.Version / 100;
                        int usrEpisodeVersion = seriePair.Value.Version % 100;
                        EpisodeInfo usrEpisode = seriePair.Value;

                        bool addedInfo = false;


                        switch (funcType)
                        {
                            case FunctionTypes.ListAllEpisodes:
                            case FunctionTypes.ListNewEpisodes:
                                if (currentSeason > usrSeasonVersion)
                                {
                                    if (currentSeason == (usrSeasonVersion + 1))
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
                                else if (currentSeason == usrSeasonVersion)
                                {
                                    if (currentEpisode > usrEpisodeVersion)
                                    {
                                        int difEpisode = currentEpisode - usrEpisodeVersion;
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
                                    else if (funcType == FunctionTypes.ListAllEpisodes)
                                    {
                                        sb.AppendFormat("\t{0}: You have the latest episode.", info.Name);
                                        addedInfo = true;
                                    }
                                }

                                if (addedInfo)
                                {
                                    anyInfo = true;
                                    sb.AppendFormat("\t\t(Your last episode is: S{0:00}E{1:00})\r\n", usrSeasonVersion,
                                                    usrEpisodeVersion);
                                    servicesUsed.Add(info.ServiceAddress);
                                    lines++;
                                }
                                break;
                            case FunctionTypes.ListDebugInfoOnEpisodes:
                                anyInfo = true;
                                sb.AppendFormat("\t{0}\t\t(Episode: S{1:00}E{2:00})\r\n\t\t{3}\r\n", info.Name, usrSeasonVersion,
                                                usrEpisodeVersion, usrEpisode.RawFileName);
                                break;
                            case FunctionTypes.ListCountDownEpisodes:
                                if (epNext != null)
                                {
                                    var difference = epNext.Date.Subtract(todaysDate);
                                    if (difference.TotalDays >= 0)
                                    {
                                        sb.AppendFormat("\t{0}\t\tDays left: {1} ({2:yyyy-MM-dd})\r\n", info.Name,
                                                        difference.TotalDays, epNext.Date);
                                        anyInfo = true;
                                    }
                                }
                                break;
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

            switch (funcType)
            {
                case FunctionTypes.ListNewEpisodes:
                    if (!anyInfo)
                    {
                        sb.AppendLine("You seem to have latest episode of every serie you are sharing!");
                    }
                    break;
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
