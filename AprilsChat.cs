using BattleBitAPI.Common;
using BBRAPIModules;
using System.Threading.Tasks;

namespace BBRModules
{
    [Module("A module that manages chat in a simpler way than ChatOverwrite.", "1.0.0")]
    public class AprilsChat : BattleBitModule
    {
        public AprilsChatConfiguration Configuration { get; set; } = null!;

        public override async Task<bool> OnPlayerTypedMessage(RunnerPlayer player, ChatChannel channel, string msg)
        {
            return true;
        }
    }

    public class AprilsChatConfiguration : ModuleConfiguration
    {
        public string MessageFormat { get; set; } = "{prefix} {gameBasedColor}{playerName}{/}";
        public string t = "";
    }
}
