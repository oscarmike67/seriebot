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

namespace ReleaseBot
{
    public class CommandHandler
    {
        public static bool TryHandleMsg(DcBot connection, string id, string command, int msgtype)
        {
            if (string.IsNullOrEmpty(command))
                return false;

            bool isCommand = false;
            switch (command[0])
            {
                case '+':
                case '-':
                case '!':
                case '.':
                    isCommand = true;
                    break;
            }

            if (!isCommand)
                return false;

            StringBuilder sb = null;
            command = command.Substring(1);
            string param = null;
            int pos;
            if ((pos = command.IndexOf(' ')) != -1)
            {
                param = command.Substring(pos).Trim();
                command = command.Substring(0, pos).ToLower();
            }
            else
            {
                command = command.ToLower();
            }

            switch (command)
            {
                case "next":
                    if (string.IsNullOrEmpty(param))
                    {
                        connection.SendMessage(msgtype, id, "Command works like this: +next <Serie Name>. For example +next smallville");
                    }
                    else
                    {
                        SerieInfo info = Service.GetSerie(param);
                        if (info != null)
                        {
                            if (info.NextEpisode != null)
                            {
                                connection.SendMessage(msgtype, id, string.Format("{0} - {1}", info.Name, info.NextEpisode));
                            }
                            else if (info.LatestEpisode != null)
                            {
                                connection.SendMessage(msgtype, id, string.Format("{0} - Last episode: {1}", info.Name, info.LatestEpisode));
                            }
                            else
                            {
                                connection.SendMessage(msgtype, id, string.Format("{0} - Status: {1}", info.Name, info.Status));
                            }
                        }
                        else
                        {
                            connection.SendMessage(Actions.PrivateMessage, id, "no info found.");
                        }
                    }
                    break;
                case "last":
                    if (string.IsNullOrEmpty(param))
                    {
                        connection.SendMessage(Actions.PrivateMessage, id, "Command works like this: +last <Serie Name>. For example +last smallville");
                    }
                    else
                    {
                        SerieInfo info = Service.GetSerie(param);
                        if (info != null)
                        {
                            if (info.LatestEpisode != null)
                            {
                                connection.SendMessage(msgtype, id, string.Format("{0} - {1}", info.Name, info.LatestEpisode));
                            }
                            else
                            {
                                connection.SendMessage(msgtype, id, string.Format("{0} - Last: {1}", info.Name, info.Status));
                            }
                        }
                        else
                        {
                            connection.SendMessage(Actions.PrivateMessage, id, "no info found.");
                        }
                    }
                    break;
                case "new":
                    User usr = connection.GetUser(id);
                    if (usr != null)
                    {
                        long share;
                        if (usr.Tag.Mode != FlowLib.Enums.ConnectionTypes.Direct)
                        {
                            connection.SendMessage(Actions.PrivateMessage, id, "You need to be active to use this command.");
                        }
                        else if (!long.TryParse(usr.UserInfo.Share, out share) || share <= 0)
                        {
                            connection.SendMessage(Actions.PrivateMessage, id, "You need to share stuff to use this command.");
                        }
                        else
                        {
                            connection.SendMessage(Actions.PrivateMessage, id, "Please note that this command may take several minutes to complete. (Writing the command more then once will reset your position in queue and place you last)");
                            connection.GetFileList(usr, "new");
                        }
                    }
                    break;
                case "ignore":
                    if (param == null)
                    {
                        sb = new StringBuilder();
                        sb.AppendLine("When, what and how to ignore.");

                        sb.AppendLine("When can ignore be used?");
                        sb.AppendLine("Ignore can be used with the following commands:");
                        sb.AppendLine("\t+new");
                        sb.AppendLine();

                        sb.AppendLine("What is ignore?");
                        sb.AppendLine("You can tell me to not display information about series.");
                        sb.AppendLine("If used in combination with +new command,");
                        sb.AppendLine("I will for example not display how many episodes you are behind for those series you have choosed me to ignore.");
                        sb.AppendLine();

                        sb.AppendLine("How do i ignore a serie?");
                        sb.AppendLine("You can tell me to ignore a serie by adding a file to your share for every serie you want me to ignore.");
                        sb.AppendLine("This file should have the following format:");
                        sb.AppendLine("\t<Serie name>.ignore");
                        sb.AppendLine("If you for example want to ignore 'Lost' you should add a file with this name:");
                        sb.AppendLine("\tLost.ignore");
                        sb.AppendLine();
                        sb.AppendLine("Type: +ignore <Serie name> to get the filename to use.");

                        connection.SendMessage(Actions.PrivateMessage, id, sb.ToString());
                    }
                    else
                    {
                        // convert serie name to ignore filename
                        connection.SendMessage(Actions.PrivateMessage, id, string.Format("{0} will give you this filename: {1}.ignore", param, Ignore.CreateName(param)));
                    }
                    break;
                case "cache":
                        sb = new StringBuilder();
                        if (param == null)
                        {
                            string[] files = System.IO.Directory.GetFiles(Service.Directory);
                            int lines = 0;
                            foreach (string f in files)
                            {
                                System.IO.FileInfo fi = new System.IO.FileInfo(f);
                                if (!fi.Name.EndsWith(".update"))
                                {
                                    sb.AppendLine(Service.GetCacheInfoFromKey(fi.Name));
                                }
                                // Make sure we are not exceeding max number of lines in hub.
                                if (Program.MAX_NUMBER_OF_LINES_IN_MESSAGE <= lines)
                                {
                                    connection.SendMessage(Actions.PrivateMessage, id, sb.ToString());
                                    sb = new StringBuilder();
                                    lines = 0;
                                }
                            }
                            // Have we any lines to send?
                            if (lines > 0)
                            {
                                connection.SendMessage(Actions.PrivateMessage, id, sb.ToString());
                            }
                        }
                        else
                        {
                            connection.SendMessage(Actions.PrivateMessage, id, "This is a list command. You can't send params with it :)");
                        }
                    break;
                default:
                    return false;
            }
            return true;
        }
    }
}
