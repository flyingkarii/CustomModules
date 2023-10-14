using BBRAPIModules;
using System;
using System.Threading.Tasks;

namespace BBRModules
{
    [RequireModule(typeof(CurrencySystem))]
    [Module("test", "1.0.0")]
    public class CurrencyExample : BattleBitModule
    {
        [ModuleReference]
        public CurrencySystem CurrencySystem { get; set; } = null!;

        public override async Task OnPlayerConnected(RunnerPlayer player)
        {
            CurrencyPlayer currencyPlayer = CurrencySystem.GetCurrencyPlayer(player);
            currencyPlayer.Increment(2);
            Console.WriteLine(currencyPlayer.GetCurrency());
        }
    }
}
