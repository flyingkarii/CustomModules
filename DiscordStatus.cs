// VERSION 1.3
// MADE BY @SENTENNIAL
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BattleBitAPI.Common;
using BBRAPIModules;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace BattleBitAPI.Features
{
    [RequireModule(typeof(PlaceholderLib))]
    [Module("Connects each server to a Discord Bot, and updates the Discord Bot's status with the server's player-count and map information.", "1.3")]
    public class DiscordStatus : BattleBitModule
    {
        public DiscordConfiguration Configuration { get; set; }
        private List<string> MessageQueue = new();
        private DiscordSocketClient discordClient;
        private ITextChannel chatMessageChannel;
        private ITextChannel reportsChannel;
        private bool discordReady = false;

        public override Task OnConnected()
        {
            if (string.IsNullOrEmpty(Configuration.DiscordBotToken))
            {
                Unload();
                throw new Exception("API Key is not set. Please set it in the configuration file.");
            }
            Task.Run(() => connectDiscord()).ContinueWith(t => Console.WriteLine($"Error during Discord connection {t.Exception}"), TaskContinuationOptions.OnlyOnFaulted);
            Task.Run(UpdateTimer).ContinueWith(t => Console.WriteLine($"Error during Discord Status update {t.Exception}"), TaskContinuationOptions.OnlyOnFaulted);
            Task.Run(SendChatMessages).ContinueWith(t => Console.WriteLine($"Error sending chat messages {t.Exception}"), TaskContinuationOptions.OnlyOnFaulted);
            return Task.CompletedTask;
        }
        private async void UpdateTimer()
        {
            while (this.IsLoaded && this.Server.IsConnected)
            {
                if (discordReady)
                    await updateDiscordStatus(getStatus());
                await Task.Delay(10000);
            }
        }

        private async void SendChatMessages()
        {
            while (this.IsLoaded && this.Server.IsConnected)
            {
                if (!discordReady)
                {
                    await Task.Delay(2000);
                    continue;
                }

                if (chatMessageChannel == null)
                {
                    Logger.Warn("!! CHAT MESSAGE CHANNEL ID IS INVALID !!");
                    break;
                }

                if (MessageQueue.Count == 0)
                {
                    continue;
                }

                List<string> messages = MessageQueue.Take(10).ToList();
                MessageQueue.RemoveRange(0, messages.Count);
                await chatMessageChannel.SendMessageAsync(string.Join(Environment.NewLine, messages));

                await Task.Delay(10000);
            }
        }

        public override void OnModuleUnloading()
        {
            Task.Run(() => disconnectDiscord());
        }

        public override async Task<bool> OnPlayerTypedMessage(RunnerPlayer player, ChatChannel channel, string msg)
        {
            string chatMessage = new PlaceholderLib(":speech_balloon: ``{name}`` ({channel}): ``{message}``")
                .AddParam("name", player.Name)
                .AddParam("channel", ToStringChatChannel(channel))
                .AddParam("message", msg)
                .Run();

            MessageQueue.Add(chatMessage);
            return true;
        }

        private async Task connectDiscord()
        {
            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged
            };
            discordClient = new DiscordSocketClient(config);
            discordClient.Ready += ReadyAsync;
            await discordClient.LoginAsync(TokenType.Bot, Configuration.DiscordBotToken);
            await discordClient.StartAsync();

            chatMessageChannel = await discordClient.GetChannelAsync(Configuration.ChatChannelId) as ITextChannel;
            ulong reportsId = Configuration.ReportChannelId != 0 ? Configuration.ReportChannelId : Configuration.ChatChannelId;
            reportsChannel = await discordClient.GetChannelAsync(reportsId) as ITextChannel;
        }

        private string getStatus()
        {
            return "" + Server.CurrentPlayerCount + "/" + Server.MaxPlayerCount +
                "(" + Server.InQueuePlayerCount + ") on " + Server.Map + " " + Server.Gamemode;
        }

        private async Task disconnectDiscord()
        {
            discordReady = false;
            try
            {
                await discordClient.StopAsync();
            }
            catch (Exception)
            {

            }
        }

        private Task ReadyAsync()
        {
            discordReady = true;
            Task.Run(() => updateDiscordStatus(getStatus()));
            return Task.CompletedTask;
        }

        private async Task updateDiscordStatus(string status)
        {
            if (discordReady == false)
            {
                return;
            }
            try
            {
                await discordClient.SetGameAsync(status);
            }
            catch (Exception)
            {

            }
        }

        private string ToStringChatChannel(ChatChannel channel)
        {
            switch (channel)
            {
                case ChatChannel.AllChat:
                    return "All Chat";
                case ChatChannel.TeamChat:
                    return "Team Chat";
                case ChatChannel.SquadChat:
                    return "Squad Chat";
                default:
                    return "All Chat";
            }
        }
    }

    public class DiscordConfiguration : ModuleConfiguration
    {
        public string DiscordBotToken { get; set; } = string.Empty;
        public int MaxMessageCount { get; set; } = 10;
        public ulong ChatChannelId { get; set; } = 0;
        public ulong ReportChannelId { get; set; } = 0;
    }
}