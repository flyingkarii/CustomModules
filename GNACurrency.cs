using BattleBitAPI.Common;
using BBRAPIModules;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BBRModules
{
    [Module("A module used to make CurrencyLib additions for GNA.", "1.0.0")]
    [RequireModule(typeof(CurrencySystem))]
    public class GNACurrency : BattleBitModule
    {
        public Dictionary<string, int> Killstreaks = new();

        public override Task OnPlayerConnected(RunnerPlayer player)
        {
            Killstreaks.Add(player.SteamID.ToString(), 0);
            return Task.CompletedTask;
        }

        public override Task OnPlayerDisconnected(RunnerPlayer player)
        {
            Killstreaks.Remove(player.SteamID.ToString());
            return Task.CompletedTask;
        }

        [ModuleReference]
        public CurrencySystem CurrencySystem { get; set; } = null!;

        public override async Task OnAPlayerDownedAnotherPlayer(OnPlayerKillArguments<RunnerPlayer> args)
        {
            double multi = GetKillstreakMultiplier(args.Killer);
            CurrencyPlayer currencyPlayer = CurrencySystem.GetCurrencyPlayer(args.Killer);
            currencyPlayer.Increment(Convert.ToInt32(multi));
        }

        public double GetKillstreakMultiplier(RunnerPlayer player)
        {
            int value = Killstreaks[player.SteamID.ToString()];

            switch (value)
            {
                case < 4:
                    return value * 1.25;
                case >= 4 and < 8:
                    return value * 1.65;
                case >= 8 and < 12:
                    return value * 1.5;
                case >= 12 and < 20:
                    return value * 2.5;
                case >= 20:
                    return value * 3;
            }
        }
    }
}
