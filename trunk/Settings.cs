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

using System.IO;
using System.Xml;
using FlowLib.Containers;
using FlowLib.Utils.Convert.Settings;

namespace ReleaseBot
{
	public static class SettingsExtensionMethods
	{
		public static HubSetting GetFirstOrDefault(this Settings set)
		{
			if (set != null && set.Hubs != null && set.Hubs.Count > 0)
			{
				return set.Hubs[0];
			}
			else
			{
				return null;
			}
		}
	}

	public class Settings : Xmpl
	{
		public const string KEY_USE_DEBUG = "UseDefault";
		public const string KEY_MAX_NUMBER_OF_LINES_IN_MESSAGE = "MaxNofLinesInMsg";

		public bool UseDebug
		{
			get
			{
				HubSetting fhub = this.GetFirstOrDefault();
				if (fhub == null)
					return false;

				string strValue;
				bool usDebg;
				if (fhub.TryGetValue(KEY_USE_DEBUG, out strValue) && bool.TryParse(strValue, out usDebg))
					return usDebg;
				else
					return false;
			}
			set
			{
				HubSetting fhub = this.GetFirstOrDefault();
				if (fhub == null)
					return;

				fhub.Set(KEY_USE_DEBUG, value.ToString());
			}
		}

		public int MaxNumberOfLinesInMessage
		{
			get
			{
				HubSetting fhub = this.GetFirstOrDefault();
				if (fhub == null)
					return -1;

				string strValue;
				int value;
				if (fhub.TryGetValue(KEY_MAX_NUMBER_OF_LINES_IN_MESSAGE, out strValue) && int.TryParse(strValue, out value))
					return value;
				else
					return -1;
			}
			set
			{
				HubSetting fhub = this.GetFirstOrDefault();
				if (fhub == null)
					return;

				fhub.Set(KEY_MAX_NUMBER_OF_LINES_IN_MESSAGE, value.ToString());
			}
		}

		public Settings()
			: base()
		{
			System.Collections.Generic.List<string> hubAttr = Nodes["Hub"];
			Nodes.Remove("Hub");
			hubAttr.Add(KEY_USE_DEBUG);
			hubAttr.Add(KEY_MAX_NUMBER_OF_LINES_IN_MESSAGE);
			Nodes.Add("Hub", hubAttr);

		}
    }
}
