using BBRAPIModules;
using Bluscream;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BBRModules
{
    [RequireModule(typeof(Bluscream.BluscreamLib))]
    [Module("A module for conditional rotation and server/player settings.", "1.0.0")]
    public class AdvancedSettings : BattleBitModule
    {
        public SettingsConfiguration Configuration;
        public ServerSettingsConfiguration ServerConfiguration;

        [ModuleReference]
        public BluscreamLib BluscreamLib { get; set; } = null!;

        public override async Task OnConnected()
        {
            
        }

        public void PopulateMaps()
        {
            if (Configuration.GameModeRotation.Count == BluscreamLib.Maps.Count)
                return;

            Configuration.MapRotation.Clear();

            foreach (MapInfo mapInfo in BluscreamLib.Maps)
            {
                MapEntry entry = new MapEntry(mapInfo);
                Configuration.MapRotation.Add(entry);
            }

            Configuration.Save();
        }

        public void PopulateModes()
        {
            if (Configuration.GameModeRotation.Count == BluscreamLib.Maps.Count)
                return;

            Configuration.GameModeRotation.Clear();

            foreach (GameModeInfo modeInfo in BluscreamLib.GameModes)
            {
                GameModeEntry entry = new GameModeEntry(modeInfo);
                Configuration.GameModeRotation.Add(entry);
            }

            Configuration.Save();
        }
    }

    public class MapEntry
    {
        public bool Enabled = true;
        public string Name = "";
        public string DisplayName = "";

        public MapEntry(MapInfo mapInfo)
        {
            Name = mapInfo.Name;
            DisplayName = mapInfo.DisplayName;
        }
    }

    public class GameModeEntry
    {
        public bool Enabled = true;
        public string Name = "";
        public string DisplayName = "";

        public GameModeEntry(GameModeInfo modeInfo)
        {
            Name = modeInfo.Name;
            DisplayName = modeInfo.DisplayName;
        }
    }

    public class SettingsConfiguration : ModuleConfiguration
    {
        public bool PreventSameMap = false;
        public int NumberOfRoundsToPreventMap = 2;

        public bool PreventSameGameMode = false;
        public int NumberOfRoundsToPreventMode = 2;

        public List<MapEntry> MapRotation = new();
        public List<GameModeEntry> GameModeRotation = new();
        public ServerSettings ServerSettings = new();
        public PlayerModifications PlayerModifications = new();

        public List<ConditionalConfiguration> ConditionalConfigurations = null;
    }

    public class ServerSettingsConfiguration : SettingsConfiguration
    {
        public new List<MapEntry> MapRotation = null;
        public new List<GameModeEntry> GameModeRotation = null;

        public new List<ConditionalConfiguration> ConditionalConfigurations = new List<ConditionalConfiguration> { new ConditionalConfiguration() };
    }

    public class ConditionalConfiguration
    {
        public string ActivateOn = "&minPlayers = -1;&maxPlayers = 32;permissions = *,funny;group = admin;steamid = 0;SteamID=1";

        public new List<MapEntry> NewMapRotation = new();
        public new List<GameModeEntry> NewGameModeRotation = new();

        public Dictionary<string, object> NewServerSettings = new()
        {
            {"CanVoteNight", false}
        };
        public Dictionary<string, object> NewPlayerModifications = new()
        {
            {"RunningSpeedMultiplier", 1.0f}
        };
    }

    public class ServerSettings
    {
        public float DamageMultipler { get; set; } = 1.0f;
        public bool OnlyWinnerTeamCanVote { get; set; } = false;
        public bool PlayerCollision { get; set; } = false;
        public bool HideMapVotes { get; set; } = false;
        public bool CanVoteDay { get; set; } = true;
        public bool CanVoteNight { get; set; } = true;
        public byte MedicLimitPerSquad { get; set; } = 8;
        public byte EngineerLimitPerSquad { get; set; } = 8;
        public byte SupportLimitPerSquad { get; set; } = 8;
        public byte ReconLimitPerSquad { get; set; } = 8;
        public float TankSpawnDelayMultiplier { get; set; } = 1.0f;
        public float TransportSpawnDelayMultiplier { get; set; } = 1.0f;
        public float SeaVehicleSpawnDelayMultiplier { get; set; } = 1.0f;
        public float APCSpawnDelayMultiplier { get; set; } = 1.0f;
        public float HelicopterSpawnDelayMultiplier { get; set; } = 1.0f;
        public bool SquadRequiredToChangeRole { get; set; } = true;
    }

    public class PlayerModifications
    {
        public float RunningSpeedMultiplier = 1f;
        public float ReceiveDamageMultiplier = 1f;
        public float GiveDamageMultiplier = 1f;
        public float JumpHeightMultiplier = 1f;
        public float FallDamageMultiplier = 1f;
        public float ReloadSpeedMultiplier = 1f;
        public bool CanUseNightVision = true;
        public float DownTimeGiveUpTime = 60f;
        public bool AirStrafe = true;
        public bool CanDeploy = true;
        public bool CanSpectate = true;
        public float RespawnTime = 10f;
        public bool CanSuicide = true;
        public float MinDamageToStartBleeding = 10f;
        public float MinHpToStartBleeding = 40f;
        public float HPperBandage = 40f;
        public bool StaminaEnabled = false;
        public bool HitMarkersEnabled = true;
        public bool FriendlyHUDEnabled = true;
        public float CaptureFlagSpeedMultiplier = 1f;
        public bool PointLogHudEnabled = true;
        public bool KillFeed = false;
        public bool IsExposedOnMap = false;
        public bool Freeze = false;
        public float ReviveHP = 35f;
        public bool HideOnMap = false;
        public bool CanBleed = true;
    }
}
