using BBRAPIModules;
using Commands;
using Permissions;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BBRModules
{
    [Module("A module with punishments such as ban and mute.", "1.0.0")]
    [RequireModule(typeof(GranularPermissions))]
    [RequireModule(typeof(CommandHandler))]
    public class Punishments : BattleBitModule
    {
        #region Data files
        public static PunishmentsConfiguration Configuration { get; set; }
        public static PunishmentsData Data { get; set; }
        #endregion

        #region Modules
        [ModuleReference]
        public GranularPermissions GranularPermissions { get; set; } = null!;
        #endregion

        public override async Task OnConnected()
        {

        }

        #region Commands

        [CommandCallback("ban", Description = "Ban a player permanently.", ConsoleCommand = true, Permissions = new[] { "ban" })]
        public void BanCommand(RunnerPlayer source, string steamid, string? timestring, string reason)
        {
            Logger.Info($"{source} + {steamid} + {timestring} + {reason}");
        }

        #endregion
    }

    public class PunishmentsConfiguration : ModuleConfiguration
    {

    }

    public class PunishmentsData : ModuleConfiguration
    {
        public List<string> Bans = new();
        public List<string> Mutes = new();
    }
}
