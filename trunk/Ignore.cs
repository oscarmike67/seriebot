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
using System.Reflection;

namespace ReleaseBot
{
    public class Ignore
    {
        static string _strRegExp = @".*\\([a-zA-Z\. ]+)\.ignore$";
        static Regex _regExp = new Regex(_strRegExp, RegexOptions.Compiled); //, RegexOptions.IgnoreCase);

        //SortedList<string, int> listIgnore = new SortedList<string, int>();

        public static bool TryAddIgnore(string filename, out string name)
        {
            //DownloadHandler.LogMsg("TryAddIgnore");
            //if (_regExp.IsMatch(filename))
            //{
            Match m = _regExp.Match(filename);
            if (m.Success)
            {
                if (m.Groups.Count == 2)
                {
                    name = CreateName(m.Groups[1].Value);
                    //listIgnore.Add(CreateName(m.Groups[1].Value), -1);
                    //DownloadHandler.LogMsg("/TryAddIgnore[true]");
                    return true;
                }
            }
            //}
            name = null;
            //DownloadHandler.LogMsg("/TryAddIgnore[false]");
            return false;
        }

        public static string CreateName(string name)
        {
            StringBuilder sb = new StringBuilder();
            name = name.ToLower();
            for (int i = 0; i < name.Length; i++)
            {
                char ch = name[i];
                switch (ch)
                {
                    case 'a':
                    case 'b':
                    case 'c':
                    case 'd':
                    case 'e':
                    case 'f':
                    case 'g':
                    case 'h':
                    case 'i':
                    case 'j':
                    case 'k':
                    case 'l':
                    case 'm':
                    case 'n':
                    case 'o':
                    case 'p':
                    case 'q':
                    case 'r':
                    case 's':
                    case 't':
                    case 'u':
                    case 'v':
                    case 'w':
                    case 'x':
                    case 'y':
                    case 'z':
                    case 'å':
                    case 'ä':
                    case 'ö':
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                    case '.':
                    case ' ':
                        sb.Append(ch);
                        break;
                    default:
                        break;
                }
            }
            return sb.ToString();
        }

        //public bool IsMatch(string name)
        //{
        //    return listIgnore.ContainsKey(CreateName(name));
        //}

        //public bool IsEmpty
        //{
        //    get { return listIgnore.Count == 0; }
        //}

        //public int Count
        //{
        //    get { return listIgnore.Count; }
        //}
    }
}
