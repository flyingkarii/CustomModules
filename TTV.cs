using BBRAPIModules;
using System.Threading.Tasks;

namespace BBRModules
{
    [Module("A module to mess with TTVs.", "1.0.0")]
    public class TTV : BattleBitModule
    {
        public static TTVConfig Configuration { get; set; } = null!;

        public override async Task OnPlayerConnected(RunnerPlayer player)
        {
            if (!player.Name.ToLower().Contains("ttv"))
                return;

            switch (Configuration.ActionType)
            {
                case "Kick":
                    player.Kick(Configuration.Message);
                    break;
                case "Message":
                    player.SayToChat(Configuration.Message);
                    break;
                case "TimedMessage":
                    player.Message(Configuration.Message, Configuration.TimedMessageLength);
                    break;
                default:
                    break;
            }
        }
    }

    public class TTVConfig : ModuleConfiguration
    {
        // Possible: Kick | Message | TimedMessage
        public string ActionType = "Kick";
        public string Message = "We don\'t like you.";
        public float TimedMessageLength = 5.0f;
    }
}
