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
        public static int MAX_NUMBER_OF_LINES_IN_MESSAGE = 15;
		public static bool DEBUG = false;
		public static bool USE_ACTIVE_MODE = false;
		public static int PORT_ACTIVE = 11010;
		public static int PORT_TLS = 11011;
        public static bool CONVERT_EXTERNAL_IP_TO_INTERNAL_IP = false;

		public static string DEBUG_LOG_FILEPATH;

		static Program()
		{
			DEBUG_LOG_FILEPATH = string.Format("msg-{0:yyyy-MM-dd hh.mm.ss.FFF}.log", DateTime.Now);
		}

        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlAppDomain)]
        static void Main(string[] args)
        {
            //RegExpLib.CreateRegExpAssembly();
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

			HubSetting settings = new HubSetting();

			WriteLine("*** NofArguments:" + args.Length);
            string file = null;
            if (args.Length > 0)
            {
                file = args[0];
				WriteLine(string.Format("*** Arguments[0 = {0}]", file));
            }else{
                WriteLine("*** No arguments");
                file = "localhost.xml";
            }

            if (!string.IsNullOrEmpty(file) && !string.IsNullOrEmpty(file.Trim()))
            {
                Settings xml = new Settings();
                xml.Read(file);
                if (xml.Hubs.Count > 0)
                {
                    settings = xml.Hubs[0];

					// Set custom settings
					DEBUG = xml.UseDebug;
					USE_ACTIVE_MODE = xml.UserActiveConnectionMode;
                    CONVERT_EXTERNAL_IP_TO_INTERNAL_IP = xml.ConvertExternalIpToInternalIp;

					int prt = xml.ActivePort;
					if (prt >= System.Net.IPEndPoint.MinPort && prt <= System.Net.IPEndPoint.MaxPort)
					{
						PORT_ACTIVE = prt;
					}
					int prtTls = xml.TlsPort;
					if (prtTls >= System.Net.IPEndPoint.MinPort && prtTls <= System.Net.IPEndPoint.MaxPort)
					{
						PORT_TLS = prtTls;
					}

					int tmp = xml.MaxNumberOfLinesInMessage;
					if (tmp > 0)
					{
						MAX_NUMBER_OF_LINES_IN_MESSAGE = tmp;
					}


                    WriteLine("*** Getting External IP");
                    //string strIp = FlowLib.Utils.WebOperations.GetPage("http://ip.flowertwig.org");
                    //System.Net.IPAddress ipAddress;
                    //if (System.Net.IPAddress.TryParse(strIp, out ipAddress))
                    //{
                    //    WriteLine("\tIP: " + strIp);
                    //    settings.Set("ExternalIP", strIp);
                    //}else
                    //{
                    //    WriteLine("\tUnable to get External IP");
                    //}

					DcBot bot = new DcBot(settings);
					bot.Connect();

					Cleaner.Start();
				}
                else
                {
                    WriteLine("*** No hubs found in settings file.");
					settings.Address = "127.0.0.1";
					settings.Port = 411;
					settings.DisplayName = "Serie";
					settings.Protocol = "Nmdc";
					settings.UserDescription = "https://code.google.com/p/seriebot/";

					// Add default values so user know that it is possible to customize.
					settings.Set(Settings.KEY_USE_DEBUG, DEBUG.ToString());
					settings.Set(Settings.KEY_MAX_NUMBER_OF_LINES_IN_MESSAGE, MAX_NUMBER_OF_LINES_IN_MESSAGE.ToString());
					settings.Set(Settings.KEY_USE_ACTIVE_CONNECTION_MODE, USE_ACTIVE_MODE.ToString());
					settings.Set(Settings.KEY_PORT_TCP_AND_UDP, PORT_ACTIVE.ToString());
					settings.Set(Settings.KEY_PORT_TLS, PORT_TLS.ToString());

					xml = new Settings();
					xml.Hubs.Add(settings);
					xml.Write("localhost.xml");
					WriteLine("*** Example setting file has been created.");
					WriteLine("*** Press enter/return key to quit.");
				}
            }
			Console.Read();
			WriteLine("*** Application terminated by user.");
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

		public static void WriteLine()
		{
			WriteLine(string.Empty);
		}
		public static void WriteLine(string str)
		{
			Write(str + "\r\n");
		}

		public static void Write(object obj)
		{
			if (obj == null)
				Write("null");
			else
				Write(obj.ToString());
		}

		public static void Write(string str)
		{
			System.Console.Write(str);
			// Logg every message to file
			if (Program.DEBUG)
			{
				lock (DEBUG_LOG_FILEPATH)
				{
					File.AppendAllText(DEBUG_LOG_FILEPATH, str);
				}
			}
		}
    }
}
