﻿using System;
using System.Net;
using System.Threading.Tasks;

using SteamKit2;
using Discord;
using Newtonsoft.Json.Linq;

namespace SteamDiscordBot.Jobs
{
	public class UpdateJob : Job
	{
		private uint version;
		private uint appid;

		public UpdateJob(uint appid)
		{
            this.version = 0;
			this.appid = appid;
		}

		public async override void OnRun()
		{
			using (dynamic steamApps = WebAPI.GetInterface("ISteamApps"))
			{
				steamApps.Timeout = (int)TimeSpan.FromSeconds(30).TotalMilliseconds;

				KeyValue results = null;

				try
				{
					results = steamApps.UpToDateCheck(appid: appid, version: 0);
				}
				catch (WebException ex)
				{
					if (ex.Status != WebExceptionStatus.Timeout)
					{
                        await Program.Instance.Log(new LogMessage(LogSeverity.Warning, "UpdateCheck", string.Format("Unable to make UpToDateCheck request: {0}", ex.Message)));
                    }

                    return;
				}

				if (!results["success"].AsBoolean())
					return; // no useful result from the api, or app isn't configured

				uint requiredVersion = (uint)results["required_version"].AsInteger(-1);

				if ((int)requiredVersion == -1)
					return; // some apps are incorrectly configured and don't report a required version

                if (this.version != requiredVersion && this.version != 0)
                {
                    await Task.Run(() => Program.Instance.Log(new LogMessage(LogSeverity.Info, "UpdateCheck", string.Format("{0} (version: {1}) is no longer up to date. New version: {2}", UpdateJob.GetAppName(appid), this.version, requiredVersion))));
                    await Task.Run(() => MessageTools.SendMessageAllToGenerals(string.Format("{0} (version: {1}) is no longer up to date. New version: {2} \nLearn more: {3}", UpdateJob.GetAppName(appid), this.version, requiredVersion, ("https://steamdb.info/patchnotes/?appid=" + appid))));
                }

                this.version = requiredVersion;
            }
        }

        public static string GetAppName(uint appid)
        {
            var json = new WebClient().DownloadString("http://store.steampowered.com/api/appdetails?appids=" + appid);
            JObject o = JObject.Parse(json);
            string name = (string)o["" + appid]["data"]["name"];
            return name;
        }
    }
}
