using BBRAPIModules;
using Commands;
using Permissions;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using System.IO;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using BattleBitAPI.Features;

namespace BBRModules
{
    [Module("A module with punishments such as ban and mute.", "1.0.0")]
    [RequireModule(typeof(GranularPermissions))]
    [RequireModule(typeof(CommandHandler))]
    [RequireModule(typeof(SQLite))]
    public class Punishments : BattleBitModule
    {
        #region Data files
        public static PunishmentsConfiguration Configuration { get; set; }
        public static SQLite Database { get; set; }
        public static SqliteConnection Connection { get; set; }
        #endregion

        #region Modules
        [ModuleReference]
        public GranularPermissions GranularPermissions { get; set; } = null!;

        [ModuleReference]
        public CommandHandler CommandHandler { get; set; } = null!;
        #endregion

        public override void OnModulesLoaded()
        {
            CommandHandler.Register(this);
        }

        public override async Task OnConnected()
        {
            Database = new("Punishments");
            await Database.Open();
            await Database.CreateTable("punishments", new List<string>() { 
                "steamId TEXT PRIMARY KEY", "punishment INT", "punishedAt INT", "punishedFor INT", "punishedBy TEXT", "reason TEXT" 
            });

            Connection = Database.Connection;
        }

        public override async Task OnDisconnected()
        {
            await Database.Close();
        }

        #region Commands

        [CommandCallback("ban", Description = "Ban a player permanently.", Permissions = new[] { "Punishments.Ban" })]
        public void BanCommand(Context context, RunnerPlayer target, string? reason = "No reason provided.")
        {
            if (!(context.Source is ChatSource))
                return;

            ChatSource source = (ChatSource)context.Source;

            var punishment = new Punishment(target.SteamID.ToString(), PunishmentType.Ban, -1, source.Invoker.SteamID.ToString(), reason);
            punishment.Punish();

            string banMessage = new PlaceholderLib(Configuration.BanMessage)
                .AddParam("ServerName", Configuration.ServerName)
                .AddParam("ExpireTime", "Never")
                .AddParam("Reason", reason)
                .AddParam("AppealMessage", Configuration.AppealMessage)
                .Build();

            target.Kick(banMessage);
        }

        [CommandCallback("tempban", Description = "Ban a player temporarily.", Permissions = new[] { "Punishments.TempBan" })]
        public void TempBanCommand(Context context, RunnerPlayer target, string timestring, string? reason = "No reason provided.")
        {
            if (!(context.Source is ChatSource))
                return;

            ChatSource source = (ChatSource)context.Source;
            var seconds = PunishmentsUtils.GetTimestringAsSeconds(timestring);
            var punishment = new Punishment(target.SteamID.ToString(), PunishmentType.Ban, seconds, source.Invoker.SteamID.ToString(), reason);
            punishment.Punish();

            var expireTime = PunishmentsUtils.GetSecondsAsTimeUnit(seconds);
            string banMessage = new PlaceholderLib(Configuration.BanMessage)
                .AddParam("ServerName", Configuration.ServerName)
                .AddParam("ExpireTime", expireTime)
                .AddParam("Reason", reason)
                .AddParam("AppealMessage", Configuration.AppealMessage)
                .Build();

            target.Kick(banMessage);
        }

        [CommandCallback("banid", Description = "Ban a SteamID permanently.", ConsoleCommand = true, Permissions = new[] { "Punishments.BanId" })]
        public void BanIdCommand(Context context, string targetId, string? reason = "No reason provided.")
        {
            string admin = context.Source is ChatSource ? (context.Source as ChatSource)!.Invoker.SteamID.ToString() : "Server";

            if (!targetId.StartsWith("756511"))
                return;

            var punishment = new Punishment(targetId.ToString(), PunishmentType.Ban, -1, admin, reason);
            punishment.Punish();

            string banMessage = new PlaceholderLib(Configuration.BanMessage)
                .AddParam("ServerName", Configuration.ServerName)
                .AddParam("ExpireTime", "Never")
                .AddParam("Reason", reason)
                .AddParam("AppealMessage", Configuration.AppealMessage)
                .Build();
        }

        [CommandCallback("tempbanid", Description = "Ban a SteamID temporarily.", ConsoleCommand = true, Permissions = new[] { "Punishments.TempBanId" })]
        public void TempBanIdCommand(Context context, string targetId, string timestring, string? reason = "No reason provided.")
        {
            string admin = context.Source is ChatSource ? (context.Source as ChatSource)!.Invoker.SteamID.ToString() : "Server";

            var seconds = PunishmentsUtils.GetTimestringAsSeconds(timestring);
            var punishment = new Punishment(targetId.ToString(), PunishmentType.Ban, seconds, admin, reason);
            punishment.Punish();

            var expireTime = PunishmentsUtils.GetSecondsAsTimeUnit(seconds);
            string banMessage = new PlaceholderLib(Configuration.BanMessage)
                .AddParam("ServerName", Configuration.ServerName)
                .AddParam("ExpireTime", expireTime)
                .AddParam("Reason", reason)
                .AddParam("AppealMessage", Configuration.AppealMessage)
                .Build();
        }

        #endregion
    }

    public class PunishmentsConfiguration : ModuleConfiguration
    {
        public string ServerName { get; set; } = "YourServerName";
        public string AppealMessage { get; set; } = "You may appeal at some.link.com!";
        public string BanMessage { get; set; } = "You were banned from {#ffaaaa ServerName /}!\n\nExpires in: {#ffaaaa ExpireTime /}\nReason: {#ffaaaa Reason /}\n\n{#ffaaaa AppealMessage /}";
        public string MuteMessage { get; set; } = "You were muted!\nExpires: {#ffaaaa ExpireTime /}\nReason: {#ffaaaa Reason /}\n{ffaaaa AppealMessage /}";
    }

    public class Punishment
    {
        public string SteamId { get; set; }
        public PunishmentType PunishmentType { get; set; }
        public long PunishedDate { get; set; }
        public long PunishedFor { get; set; }
        public string AdminId { get; set; }
        public string Reason { get; set; }

        public Punishment(string steamId)
        {
            SteamId = steamId;
        }

        public Punishment(string steamId, PunishmentType punishmentType, long punishedFor, string adminId, string reason)
        {
            SteamId = steamId;
            PunishmentType = punishmentType;
            PunishedDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            PunishedFor = punishedFor;
            AdminId = adminId;
            Reason = reason;
        }

        public async Task PunishAsync()
        {
            var insert = Punishments.Connection.CreateCommand();
            insert.CommandText = @"INSERT OR IGNORE INTO punishments (steamId, punishment, punishedAt, punishedFor, punishedBy, reason) VALUES ($steamId, $punishment, $punishedAt, $punishedFor, $punishedBy, $reason)";
            insert.Parameters.AddWithValue("$steamId", SteamId);
            insert.Parameters.AddWithValue("$punishment", PunishmentType);
            insert.Parameters.AddWithValue("$punishedAt", PunishedDate);
            insert.Parameters.AddWithValue("$punishedFor", PunishedFor);
            insert.Parameters.AddWithValue("$punishedBy", AdminId);
            insert.Parameters.AddWithValue("$reason", Reason);
            await insert.ExecuteNonQueryAsync();
        }

        public void Punish()
        {
            var insert = Punishments.Connection.CreateCommand();
            insert.CommandText = @"INSERT OR IGNORE INTO punishments (steamId, punishment, punishedAt, punishedFor, punishedBy, reason) VALUES ($steamId, $punishment, $punishedAt, $punishedFor, $punishedBy, $reason)";
            insert.Parameters.AddWithValue("$steamId", SteamId);
            insert.Parameters.AddWithValue("$punishment", PunishmentType);
            insert.Parameters.AddWithValue("$punishedAt", PunishedDate);
            insert.Parameters.AddWithValue("$punishedFor", PunishedFor);
            insert.Parameters.AddWithValue("$punishedBy", AdminId);
            insert.Parameters.AddWithValue("$reason", Reason);
            insert.ExecuteNonQuery();
        }

        public bool IsPunished(PunishmentType? punishmentType = null)
        {
            var get = Punishments.Connection.CreateCommand();
            get.CommandText = punishmentType != null ? "SELECT * FROM punishments WHERE steamId = $steamId AND punishment = $punishment LIMIT 1" : "SELECT * FROM punishments WHERE steamId = $steamId LIMIT 1";
            get.Parameters.AddWithValue("$steamId", SteamId);
            get.Parameters.AddWithValue("$punishment", punishmentType);
            
            using (SqliteDataReader reader = get.ExecuteReader())
            {
                if (!reader.HasRows)
                    return false;

                long current = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                long punishedAt = reader.GetInt64(2);
                long punishedFor = reader.GetInt64(3);

                return punishedFor == -1 || current - (punishedAt + punishedFor) < 0;
            }
        }

        public async Task<bool> IsPunishedAsync(PunishmentType? punishmentType = null)
        {
            var get = Punishments.Connection.CreateCommand();
            get.CommandText = punishmentType != null ? "SELECT * FROM punishments WHERE steamId = $steamId AND punishment = $punishment LIMIT 1" : "SELECT * FROM punishments WHERE steamId = $steamId LIMIT 1";
            get.Parameters.AddWithValue("$steamId", SteamId);
            get.Parameters.AddWithValue("$punishment", punishmentType);

            using (SqliteDataReader reader = await get.ExecuteReaderAsync())
            {
                if (!reader.HasRows)
                    return false;

                long current = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                long punishedAt = reader.GetInt64(2);
                long punishedFor = reader.GetInt64(3);

                return punishedFor == -1 || current - (punishedAt + punishedFor) < 0;
            }
        }

        public void UnpunishIfReady(PunishmentType? punishmentType = null)
        {
            var get = Punishments.Connection.CreateCommand();
            get.CommandText = punishmentType != null ? "SELECT * FROM punishments WHERE steamId = $steamId AND punishment = $punishment LIMIT 1" : "SELECT * FROM punishments WHERE steamId = $steamId LIMIT 1";
            get.Parameters.AddWithValue("$steamId", SteamId);
            get.Parameters.AddWithValue("$punishment", punishmentType);

            using (SqliteDataReader reader = get.ExecuteReader())
            {
                if (!reader.HasRows)
                    return;

                long current = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                long punishedAt = reader.GetInt64(2);
                long punishedFor = reader.GetInt64(3);

                if (punishedFor != -1 && current - (punishedAt + punishedFor) < 0)
                {
                    var remove = Punishments.Connection.CreateCommand();
                    remove.CommandText = "DELETE * FROM punishments WHERE steamId=$steamId";
                    remove.Parameters.AddWithValue("$steamId", SteamId);
                    remove.ExecuteNonQuery();
                }
            }
        }

        public async Task UnpunishIfReadyAsync(PunishmentType? punishmentType = null)
        {
            var get = Punishments.Connection.CreateCommand();
            get.CommandText = punishmentType != null ? "SELECT * FROM punishments WHERE steamId = $steamId AND punishment = $punishment LIMIT 1" : "SELECT * FROM punishments WHERE steamId = $steamId LIMIT 1";
            get.Parameters.AddWithValue("$steamId", SteamId);
            get.Parameters.AddWithValue("$punishment", punishmentType);

            using (SqliteDataReader reader = await get.ExecuteReaderAsync())
            {
                if (!reader.HasRows)
                    return;

                long current = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                long punishedAt = reader.GetInt64(2);
                long punishedFor = reader.GetInt64(3);

                if (punishedFor != -1 && current - (punishedAt + punishedFor) < 0)
                {
                    var remove = Punishments.Connection.CreateCommand();
                    remove.CommandText = "DELETE * FROM punishments WHERE steamId=$steamId";
                    remove.Parameters.AddWithValue("$steamId", SteamId);
                    await remove.ExecuteNonQueryAsync();
                }
            }
        }

        public void Unpunish(PunishmentType? punishmentType = null)
        {
            var remove = Punishments.Connection.CreateCommand();
            remove.CommandText = punishmentType == null ? "DELETE * FROM punishments WHERE steamId=$steamId" : "DELETE * FROM punishments WHERE steamId=$steamId AND punishment=$punishment";
            remove.Parameters.AddWithValue("$steamId", SteamId);
            remove.Parameters.AddWithValue("$punishment", PunishmentType);
            remove.ExecuteNonQuery();
        }

        public async Task UnpunishAsync(PunishmentType? punishmentType = null)
        {
            var remove = Punishments.Connection.CreateCommand();
            remove.CommandText = punishmentType == null ? "DELETE * FROM punishments WHERE steamId=$steamId" : "DELETE * FROM punishments WHERE steamId=$steamId AND punishment=$punishment";
            remove.Parameters.AddWithValue("$steamId", SteamId);
            remove.Parameters.AddWithValue("$punishment", PunishmentType);
            await remove.ExecuteNonQueryAsync();
        }
    }

    public enum PunishmentType
    {
        Ban,
        Mute
    }

    public class PunishmentsUtils
    {
        static Dictionary<string, int> lettersToSeconds = new Dictionary<string, int> {
            { "y", 31556952 },
            { "mo", 2629746 },
            { "w", 604800 },
            { "d", 86400 },
            { "h", 3600 },
            { "m", 60 }
        };

        static Dictionary<string, string> lettersToUnit = new Dictionary<string, string> {
            { "y", "year" },
            { "mo", "month" },
            { "w", "week" },
            { "d", "day" },
            { "h", "hour" },
            { "m", "minute" }
        };

        public static string GetTimestringAsUnitResult(string timestring)
        {
            string unitResult = "";

            bool isNumber = int.TryParse(timestring, out int secondsTime);
            bool valid = true;

            if (isNumber)
                return secondsTime + " seconds";

            MatchCollection collection = Regex.Matches(timestring, "(\\d+)([A-Za-z]+)");
            int i = 0;

            foreach (Match match in collection)
            {
                valid = int.TryParse(match.Groups[1].Value, out int num);
                string time = match.Groups[2].Value;

                if (!lettersToUnit.ContainsKey(time.ToLower()))
                    valid = false;

                if (!valid)
                    return "";

                bool result = lettersToUnit.TryGetValue(time, out string unit);

                if (result)
                {
                    if (num != 1)
                        unit += "s";

                    string beginning = "";
                    string end = "";

                    if (collection.Count > i + 1)
                    {
                        end += " ";
                    }
                    else
                    {
                        beginning = "and ";
                    }

                    unitResult += beginning + num + " " + unit + end;
                }

                i++;
            }

            return unitResult;
        }

        public static int GetTimestringAsSeconds(string timestring)
        {
            bool valid = false;
            int total = 0;

            bool isNumber = int.TryParse(timestring, out int secondsTime);

            if (isNumber)
                return secondsTime;

            foreach (Match match in Regex.Matches(timestring, "(\\d+)([A-Za-z]+)"))
            {
                valid = int.TryParse(match.Groups[1].Value, out int num);
                string time = match.Groups[2].Value;

                if (!lettersToSeconds.ContainsKey(time.ToLower()))
                    valid = false;

                if (!valid)
                    return -1;

                lettersToSeconds.TryGetValue(time, out int seconds);
                total += seconds * num;
            }

            return total;
        }

        public static string GetSecondsAsTimeUnit(int seconds)
        {
            string timeunit = "Never";

            if (seconds != -1)
            {
                timeunit = "";

                int remaining = seconds;
                int quantity = 0;

                for (int i = 0; i < lettersToSeconds.Count; i++)
                {
                    KeyValuePair<string, int> pair = lettersToSeconds.ElementAt(i);

                    if (remaining == 0)
                        break;

                    if (pair.Value > remaining)
                        continue;

                    if (remaining / pair.Value >= 1)
                    {
                        quantity = (int)Math.Floor((decimal)remaining / pair.Value);
                        remaining -= quantity * pair.Value;
                    }

                    timeunit += $"{quantity}{pair.Key}";
                }
            }

            return GetTimestringAsUnitResult(timeunit);
        }
    }
}
