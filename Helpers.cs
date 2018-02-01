﻿using System.Net;
using System.Linq;
using System.Collections.Generic;

using Discord;
using Discord.WebSocket;

using Newtonsoft.Json.Linq;
using System;
using Octokit;
using System.Diagnostics;
using System.IO;

namespace ChancyBot
{
    public class Helpers
    {
        public static readonly string[] sendchannels = { "announc", "general" };

        public static int GuildHasChannel(IReadOnlyCollection<SocketTextChannel> channels, string channelName)
        {
            bool found = false;
            int index = 0;
            
            while (!found && index < channels.Count)
            {
                if (channels.ElementAt(index).Name.Contains(channelName))
                {
                    found = true;
                }
                else
                {
                    index++;
                }
            }

            return (found)?index:-1;
        }

        // Using the static channels array, it finds the first matching channel to send the message to.
        // The order of channels in sendchannels array matters.
		public static SocketTextChannel FindSendChannel(SocketGuild guild)
		{
            int i = 0;
            int index = -1;
            bool found = false;
            while (!found && i < sendchannels.Length)
            {
                index = GuildHasChannel(guild.TextChannels, sendchannels[i]);
                if (index != -1)
                {
                    found = true;
                }
                else
                {
                    i++;
                }
            }

            return (found && index != -1) ? guild.TextChannels.ElementAt(index) : null;
        }
        
        // Sends mass message to all discord guilds the bot is connected to.
        public async static void SendMessageAllToGenerals(string input)
		{
            await Program.Instance.Log(new LogMessage(LogSeverity.Info, "SendMessageAll", "Text: " + input));

            foreach (SocketGuild guild in Program.Instance.client.Guilds) // loop through each discord guild
			{
                var usr = guild.CurrentUser;
                if (usr.GuildPermissions.Has(GuildPermission.SendMessages))
                {
                    SocketTextChannel channel = Helpers.FindSendChannel(guild); // find #general

                    if (channel != null) // #general exists
                    {
                        await Program.Instance.Log(new LogMessage(LogSeverity.Info, "SendMsg", "Sending msg to: " + channel.Name));

                        try
                        {
                            var emb = new EmbedBuilder();
                            emb.WithDescription(input);
                            await channel.SendMessageAsync("", false, emb);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Could not send to {0}: {1}", channel.Name, e.Message);
                        }
                    }
                }
			}
		}

        // Sends a message to a targeted discord guild
        public static async void SendMessageAllToTarget(string target, string input, EmbedBuilder emb = null)
        {
            await Program.Instance.Log(new LogMessage(LogSeverity.Info, "SendMessageTarget", "MSG To "+ target + ": " + input));

            foreach (SocketGuild guild in Program.Instance.client.Guilds) // loop through each discord guild
            {
                if (guild.Name.ToLower().Contains(target.ToLower())) // find target 
                {
                    var usr = guild.CurrentUser;
                    if (usr.GuildPermissions.Has(GuildPermission.SendMessages))
                    {
                        SocketTextChannel channel = Helpers.FindSendChannel(guild); // find desired channel

                        if (channel != null) // target exists
                        {
                            try
                            {
                                if (emb != null)
                                {

                                    await channel.SendMessageAsync(input, false, emb);
                                }
                                else
                                {
                                    await channel.SendMessageAsync(input);
                                }
                            }
                            catch(Exception e)
                            {
                                Console.WriteLine("Could not send to {0}: {1}", channel.Name, e.Message);
                            }
                        }
                    }
                }
            }
        }

        public async static void Update()
        {
            int pid = Process.GetCurrentProcess().Id;

            var client = new GitHubClient(new ProductHeaderValue("Steam-Discord-Bot"));
            var releases = await client.Repository.Release.GetAll("Headline", "Steam-Discord-Bot");

            string url = "https://github.com/Headline/Steam-Discord-Bot/releases/download/<name>/steam-discord-bot.zip";
            url = url.Replace("<name>", releases[0].Name);

            string command = string.Format("/k cd {0} & python updater.py {1} {2}",
                                                            Directory.GetCurrentDirectory(),
                                                            pid,
                                                            url);

            Process.Start("CMD.exe", command);
            Environment.Exit(0);
        }

        public static string GetLatestVersion(IReadOnlyList<Release> releases)
        {
            var detectionString = "-v";
            return releases[0].Name.Substring(releases[0].Name.IndexOf(detectionString) + detectionString.Length);
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
