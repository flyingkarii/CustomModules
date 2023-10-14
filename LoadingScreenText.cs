using BattleBitAPI.Common;
using BattleBitAPI.Features;
using BBRAPIModules;
using System.Linq;
using System.Threading.Tasks;

namespace BattleBitBaseModules;

[Module("Configure the loading screen text of your server", "1.0.0")]
[RequireModule(typeof(PlaceholderLib))]
public class LoadingScreenText : BattleBitModule
{
    [ModuleReference]
    public PlaceholderLib PlaceholderLib { get; set; } = null!;

    public LoadingScreenTextConfiguration Configuration { get; set; }

    public override Task OnConnected()
    {
        string LoadingText = new PlaceholderLib(Configuration.LoadingScreenText)
            .AddParam("serverName", Server.ServerName)
            .AddParam("playerCount", Server.AllPlayers.Count())
            .AddParam("maxPlayers", Server.MaxPlayerCount)
            .AddParam("gamemode", Server.Gamemode)
            .AddParam("map", Server.Map)
            .Run();
        Server.SetLoadingScreenText(LoadingText);

        return Task.CompletedTask;
    }

    public override Task OnPlayerConnected(RunnerPlayer player)
    {
        int playerCount = Server.AllPlayers.Count();
        string LoadingText = new PlaceholderLib(Configuration.LoadingScreenText)
            .AddParam("serverName", Server.ServerName)
            .AddParam("playerCount", Server.CurrentPlayerCount + " (+" + Server.InQueuePlayerCount + ")")
            .AddParam("maxPlayers", Server.MaxPlayerCount)
            .AddParam("gamemode", Server.Gamemode)
            .AddParam("map", Server.Map)
            .Run();
        Server.SetLoadingScreenText(LoadingText);

        string WelcomeText = new PlaceholderLib(Configuration.WelcomeText)
            .AddParam("serverName", Server.ServerName)
            .AddParam("playerCount", Server.CurrentPlayerCount + " (+" + Server.InQueuePlayerCount + ")")
            .AddParam("maxPlayers", Server.MaxPlayerCount)
            .AddParam("gamemode", Server.Gamemode)
            .AddParam("map", Server.Map)
            .Run();
        player.SayToChat(WelcomeText);

        return Task.CompletedTask;
    }

    public override Task OnPlayerDisconnected(RunnerPlayer player)
    {
        string LoadingText = new PlaceholderLib(Configuration.LoadingScreenText)
            .AddParam("serverName", Server.ServerName)
            .AddParam("playerCount", Server.CurrentPlayerCount + " (+" + Server.InQueuePlayerCount + ")")
            .AddParam("maxPlayers", Server.MaxPlayerCount)
            .AddParam("gamemode", Server.Gamemode)
            .AddParam("map", Server.Map)
            .Run();
        Server.SetLoadingScreenText(LoadingText);
        return Task.CompletedTask;
    }

    public override Task OnGameStateChanged(GameState oldState, GameState newState)
    {
        string LoadingText = new PlaceholderLib(Configuration.LoadingScreenText)
            .AddParam("serverName", Server.ServerName)
            .AddParam("playerCount", Server.CurrentPlayerCount + " (+" + Server.InQueuePlayerCount + ")")
            .AddParam("maxPlayers", Server.MaxPlayerCount)
            .AddParam("gamemode", Server.Gamemode)
            .AddParam("map", Server.Map)
            .Run();
        Server.SetLoadingScreenText(LoadingText);
        return Task.CompletedTask;
    }
}

public class LoadingScreenTextConfiguration : ModuleConfiguration
{
    public string LoadingScreenText { get; set; } = "{#ffaaaa}Welcome to {/}{serverName}{#ffaaaa}!\n" +
        "We are currently playing {/}{gamemode}{#ffaaaa} on {/}{map}{#ffaaaa} with {/}{playerCount}{#ffaaaa}/{/}{maxPlayers}{#ffaaaa}!" +
        "\nEnjoy your stay!";
    public string WelcomeText { get; set; } = "{#ffaaaa}Welcome to {/}{serverName}{#ffaaaa}!\n" +
        "We are currently playing {/}{gamemode}{#ffaaaa} on {/}{map}{#ffaaaa} with {/}{playerCount}{#ffaaaa}/{/}{maxPlayers}{#ffaaaa}!" +
        "\nEnjoy your stay!";
}