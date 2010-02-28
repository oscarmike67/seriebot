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
using FlowLib.Utils.Convert.Settings;
using FlowLib.Containers;
using System.Security.Permissions;
using System.IO;

namespace ReleaseBot
{
    public class Program
    {
        public const int MAX_NUMBER_OF_LINES_IN_MESSAGE = 15;

        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlAppDomain)]
        static void Main(string[] args)
        {
            //RegExpLib.CreateRegExpAssembly();
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

			HubSetting settings = new HubSetting();

            Console.WriteLine("NofArguments:" + args.Length);
            string file = null;
            if (args.Length > 0)
            {
                file = args[0];
                Console.WriteLine(string.Format("Arguments[0 = {0}]", file));
            }else{
                Console.WriteLine("No arguments");
                file = "localhost.xml";
            }

            if (!string.IsNullOrEmpty(file) && !string.IsNullOrEmpty(file.Trim()))
            {
                Xmpl xml = new Xmpl();
                xml.Read(file);
                if (xml.Hubs.Count > 0)
                {
                    settings = xml.Hubs[0];
                }
                else
                {
                    Console.WriteLine("No hubs found in settings file.");
					settings.Address = "127.0.0.1";
					settings.Port = 441;
					settings.DisplayName = "Serie";
					settings.Protocol = "Nmdc";
					settings.UserDescription = "https://code.google.com/p/seriebot/";

					xml = new Xmpl();
					xml.Hubs.Add(settings);
					xml.Write("localhost.xml");
					Console.WriteLine("Example setting file has been created.");
				}
            }

            DcBot bot = new DcBot(settings);
            bot.Connect();

            Cleaner.Start();

            Console.Read();
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            if (ex != null)
            {
                string path = string.Format("error-{0:yyyy-MM-dd hh.mm.ss.FFF}.log", DateTime.Now);
                File.WriteAllText(path, ex.ToString());
            }
        }
    }
}
