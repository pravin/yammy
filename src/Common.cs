// Yammy - Yahoo Messenger Archives Decoder
// Copyright (C) 2005-2007, Pravin Paratey (pravinp at gmail dot com)
// http://yammy.sourceforge.net
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

using System;
using System.IO;
using System.Text;
using Microsoft.Win32;

using Yammy.Properties;

namespace Yammy
{
	public static class Common
	{
		/// <summary>
		/// Reads a file and returns its contents as string. 
		/// All files are assumed to be in the WebRoot directory
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static string ReadTextFile(string path)
		{
			string strContent = string.Empty;
			StreamReader sr = null;
			try
			{
				sr = new StreamReader(path);
				strContent = sr.ReadToEnd();
			}
			catch (Exception e)
			{
				Logger.Instance.LogException(e);
			}
			finally
			{
				if (sr != null)
					sr.Close();
			}

			return strContent;
		}

		/// <summary>
		/// Gets yahoo path from registry
		/// </summary>
		/// <returns></returns>
		public static string GetYahooPath()
		{
			string strProfilesPath = string.Empty;
			try
			{
				RegistryKey keyYahooPagerLocation = Registry.ClassesRoot.OpenSubKey(@"ymsgr\shell\open\command");
				if (keyYahooPagerLocation != null)
				{
					strProfilesPath = keyYahooPagerLocation.GetValue(String.Empty) as string; // Get default string
				}
				if (strProfilesPath != null && strProfilesPath.Length > 0)
				{
					strProfilesPath = strProfilesPath.Substring(strProfilesPath.IndexOf("\"") + 1, strProfilesPath.LastIndexOf('\\'));
				}
			}
			catch { }
			return strProfilesPath;
		}

		/// <summary>
		/// Constructs file path from vars
		/// </summary>
		/// <param name="localUser"></param>
		/// <param name="type"></param>
		/// <param name="remoteUser"></param>
		/// <param name="fileName"></param>
		/// <returns></returns>
		public static string ConstructPath(string localUser, string type, string remoteUser, string fileName)
		{
			string yahooPath = GetYahooPath();
			return yahooPath + "Profiles\\" + localUser + "\\Archive\\" +
				(type == "c" ? "Conferences\\" : (type == "i" ? "Messages\\" : "Mobile\\")) +
				remoteUser + "\\" + fileName;
		}

		// localuser=[]&type=[c|i|m]&remoteuser=[]&fname=[]&page=[]
		/// <summary>
		/// Gets the index page
		/// </summary>
		/// <returns></returns>
		public static string GetIndexPage()
		{
			LocalUserInfo []users = YahooInfo.GetLocalUsers();

			StringBuilder sb = new StringBuilder(Resources.SearchBoxHTMLSnippet + "<h1>Users</h1>");
			foreach (LocalUserInfo user in users)
			{
				sb.AppendFormat(
@"<div class=""cascade"">
	<div class=""avatar""><a href=""/show?localuser={0}""><img src=""{1}"" width=96 height=96 /></a></div>
	<div class=""desc"">
		<h2><a href=""/show?localuser={0}"">{0}</a></h2>
		<em>Total conversations: {2}</em><br />
		<em>Last conversation: {3}</em><br />
		<em>Archiving: {4} | <a href='/{5}archiving?localuser={0}'>{6}</a></em><br />
	</div>
</div>", user.LocalUser, user.IconPath, user.TotalConvos, user.LastConvoAt.ToShortDateString(),
	   user.ArchivingEnabled ? "<b style='color:#393'>Enabled</b>" : "<b style='color:#933'>Disabled</b>", 
	   user.ArchivingEnabled ? "disable" : "enable", user.ArchivingEnabled ? "Disable" : "Enable");
			}

			return sb.ToString();
		}

		/// <summary>
		/// Gets all the convos by the localUser
		/// </summary>
		/// <param name="localUser"></param>
		/// <returns></returns>
		public static string GetLocalUserFriends(string localUser)
		{
			string strPath = ConstructPath(localUser, "i", string.Empty, string.Empty);

			string strBreadCrumb = "<div class='crumb'><a href='/'>Users</a> &raquo; " +
				localUser + "</div>" + "<h1>Showing conversations for " + localUser + "</h1>";

			StringBuilder sb = new StringBuilder(strBreadCrumb);
			if (!Directory.Exists(strPath))
			{
				sb.Append("No conversations found");
				return sb.ToString();
			}
			string[] remoteUsers = Directory.GetDirectories(strPath);
			foreach (string remoteuser in remoteUsers)
			{
				int lastConvoDate = 0;
				int totalConversations = 0;
				foreach (string remoteUser in remoteUsers)
				{
					string[] files = Directory.GetFiles(remoteUser);
					foreach (string file in files)
					{
						try
						{
							int iDate = Int32.Parse(Path.GetFileNameWithoutExtension(file).Substring(0, 8));
							if (iDate > lastConvoDate)
								lastConvoDate = iDate;
						}
						catch { }
					}
					totalConversations += files.Length;
				}

				sb.AppendFormat(
@"<div class=""cascade"">
	<div class=""avatar""><a href=""/decode?localuser={0}&remoteuser={1}&type=i""><img src=""/images/generic.png"" width=96 height=96 /></a></div>
	<div class=""desc"">
		<h2><a href=""/decode?localuser={0}&remoteuser={1}&type=i"">{1}</a></h2>
		<em>Total conversations: {2}</em><br />
		<em>Last conversation: {3}</em><br />
	</div>
</div>", localUser, Path.GetFileNameWithoutExtension(remoteuser), totalConversations, 
	   GetDateTimeFromYYYYMMDD(lastConvoDate.ToString()).ToShortDateString());
			}
			return sb.ToString();
		}

		/// <summary>
		/// Gets a DateTime object from a filename string that looks like YYYYMMDD
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
		public static DateTime GetDateTimeFromYYYYMMDD(string filename)
		{
			int year = 1; int month = 1; int day = 1;

			try
			{
				year = Int32.Parse(filename.Substring(0, 4));
				month = Int32.Parse(filename.Substring(4, 2));
				day = Int32.Parse(filename.Substring(6));
			}
			catch { }
			return new DateTime(year, month, day);
		}
	}
}