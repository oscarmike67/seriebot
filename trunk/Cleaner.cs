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
using System.Threading;

namespace ReleaseBot
{
    public class Cleaner
    {
        public static void Start()
        {
            Thread thread = new Thread(new ThreadStart(OnWorking));
            thread.IsBackground = true;
            thread.Start();
        }

        private static void OnWorking()
        {
            // set interval to 5 min
            int interval = 5 * 60 * 1000;

            try
            {
                do
                {
					Program.WriteLine("*** Cleaning memory: Start");
                    Service.CleanMemory();

                    GC.Collect();
					Program.WriteLine("*** Cleaning memory: End");
					Thread.Sleep(interval);
                } while (true);
            }
            catch (ThreadAbortException) { }
        }
    }
}
