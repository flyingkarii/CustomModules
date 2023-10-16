using BattleBitAPI.Common;
using BattleBitAPI.Features;
using BBRAPIModules;
using Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BBRModules
{
    [Module("A module that manages chat in a simpler way than ChatOverwrite.", "1.0.0")]
    [RequireModule(typeof(GranularPermissions))]
    [RequireModule(typeof(PlaceholderLib))]
    public class AprilsChat : BattleBitModule
    {
        public AprilsChatConfiguration Configuration { get; set; } = null!;

        [ModuleReference]
        public GranularPermissions GranularPermissions { get; set; } = null!;

        public override async Task<bool> OnPlayerTypedMessage(RunnerPlayer player, ChatChannel channel, string msg)
        {
            return true;
        }
        
        public List<string> HasSpecialChat(RunnerPlayer player, ChatData chatData)
        {
            List<string> result = new();
            string[] playerGroups = GranularPermissions.ServerConfiguration.PlayerGroups.ContainsKey(player.SteamID) ? GranularPermissions.ServerConfiguration.PlayerGroups[player.SteamID].ToArray() : Array.Empty<string>();

            foreach (KeyValuePair<string, string> pairs in chatData.Required)
            {
                bool groupFound = playerGroups.Where(group => pairs.Key.ToLower().Equals($"group.{group.ToLower()}")).Count() > 0;

                if (groupFound)
                    result.Add(pairs.Value);
            }

            return result;
        }
    }

    public class AprilsChatConfiguration : ModuleConfiguration
    {
        public string DefaultNameColor = "{SquadOrTeamColor}";
        public string MessageFormat { get; set; } = "{Prefix}{Tag}{NameColor PlayerName /}{TeamAndSquad}{Suffix}: {!Message}";
        public ChatData Prefixes { get; set; } = new();
        public ChatData Suffixes { get; set; } = new();
        public ChatData ChatColors { get; set; } = new();
        public ChatData NameColors { get; set; } = new();
        public Dictionary<string, List<string>> SpecialTags { get; set; } = new()
        {
            {"{ff0000}[ATag]{/}", new List<string>() {"7656119xxxxxxx" } }
        };
    }

    public class ChatData
    {
        public Dictionary<string, string> Required { get; set; } = new()
        { 
            { "7656119xxxxxxx", "ValueToGiveForSteamID"},
            { "Group.GroupName", "ValueToGiveForGroup"},
            { "Permission", "ValueToGiveForPermission"},
        };
    }

    public enum SpecialChatType
    {
        Prefix,
        Suffix,
        Tag,
        NameColor,
        ChatColor,
        Suffixes
    }
}
