using BattleBitAPI.Common;
using BattleBitAPI.Common.Threading;
using BattleBitAPI.Features;
using BBRAPIModules;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BBRModules
{
    [Module("A module with a currency system, including saving, loading, and more. Uses SQLite.", "1.0.0")]
    [RequireModule(typeof(PlaceholderLib))]
    public class CurrencySystem : BattleBitModule
    {
        public static CurrencyConfiguration Configuration { get; set; } = null!;
        public CurrencyDatabase CurrencyDatabase { get; set; }
        public List<CurrencyPlayer> CurrencyPlayers { get; private set; }
        public List<CurrencyPlayer> TeamACurrencyPlayers { get => CurrencyPlayers.Where((p) => p.Player.Team == Team.TeamA).ToList(); }
        public List<CurrencyPlayer> TeamBCurrencyPlayers { get => CurrencyPlayers.Where((p) => p.Player.Team == Team.TeamB).ToList(); }

        public override async Task OnConnected()
        {
            CurrencyPlayers = new();

            CurrencyDatabase = new();
            await CurrencyDatabase.Open();
            await CurrencyDatabase.CreateCurrencyTable();

            lock (Server.AllPlayers)
            {
                foreach (RunnerPlayer player in Server.AllPlayers)
                {
                    CurrencyPlayer currencyPlayer = new(this, player);
                    CurrencyPlayers.Add(currencyPlayer);
                }
            }
        }

        public override Task OnPlayerConnected(RunnerPlayer player)
        {
            CurrencyPlayer currencyPlayer = new(this, player);
            CurrencyPlayers.Add(currencyPlayer);
            return Task.CompletedTask;
        }

        public override async Task OnPlayerDisconnected(RunnerPlayer player)
        {
            CurrencyPlayer currencyPlayer = GetCurrencyPlayer(player);
            await currencyPlayer.SaveAsync();
            CurrencyPlayers.Remove(currencyPlayer);
        }

        public CurrencyPlayer GetCurrencyPlayer(RunnerPlayer player) => CurrencyPlayers.Where(p => p.SteamID == player.SteamID.ToString()).Single();

        public CurrencyPlayer GetOfflineCurrencyPlayer(string steamId)
        {
            return CurrencyPlayers.Where(p => p.SteamID == steamId).Single() ?? new CurrencyPlayer(this, steamId);
        }
    }

    public class CurrencyPlayer
    {
        public string SteamID;
        public CurrencySystem System;
        public int CurrencyAmount;

        public CurrencyPlayer(CurrencySystem system, string steamId)
        {
            SteamID = steamId;
            System = system;

            SqliteConnection connection = System.CurrencyDatabase.GetConnection();
            SqliteCommand create = new("INSERT OR IGNORE INTO currencyStore (steamId, currency) VALUES ($steamId, $default)", connection);
            create.Parameters.AddWithValue("$steamId", SteamID.ToString());
            create.Parameters.AddWithValue("$default", CurrencySystem.Configuration.StartingAmount);
            create.ExecuteNonQuery();

            SqliteCommand get = new("SELECT currency FROM currencyStore WHERE steamId=$steamId", connection);
            get.Parameters.AddWithValue("$steamId", SteamID.ToString());
            object? value = get.ExecuteScalar();

            if (value != null)
                CurrencyAmount = int.Parse(value.ToString());

            System.CurrencyDatabase.OnChangedEvent += OnChanged;
        }

        public CurrencyPlayer Set(int newAmount)
        {
            CurrencyAmount = newAmount;

            CurrencyChangedArgs args = new();
            args.NewValue = CurrencyAmount;

            System.CurrencyDatabase.OnChanged(args);

            return this;
        }

        public int GetCurrency() => CurrencyAmount;
        public string GetCurrencyString() => new PlaceholderLib(CurrencySystem.Configuration.CurrencyFormat)
            .AddParam("amount", CurrencyAmount)
            .AddParam("currency", CurrencySystem.Configuration.Currency)
            .Run();

        public CurrencyPlayer Increment(int amount)
        {
            CurrencyAmount += amount;

            CurrencyChangedArgs args = new();
            args.NewValue = CurrencyAmount;

            System.CurrencyDatabase.OnChanged(args);

            return this;
        }

        public CurrencyPlayer Decrement(int amount)
        {
            CurrencyAmount -= amount;

            CurrencyChangedArgs args = new();
            args.NewValue = CurrencyAmount;

            System.CurrencyDatabase.OnChanged(args);

            return this;
        }

        public CurrencyPlayer Multiply(int multiplicand)
        {
            CurrencyAmount *= multiplicand;

            CurrencyChangedArgs args = new();
            args.NewValue = CurrencyAmount;

            System.CurrencyDatabase.OnChanged(args);

            return this;
        }

        public CurrencyPlayer Divide(int divisor)
        {
            CurrencyAmount /= divisor;

            CurrencyChangedArgs args = new();
            args.NewValue = CurrencyAmount;

            System.CurrencyDatabase.OnChanged(args);

            return this;
        }

        public void OnChanged(object? e, CurrencyChangedArgs args)
        {
            CurrencyAmount = args.NewValue;
        }

        public async Task SaveAsync()
        {
            SqliteConnection connection = System.CurrencyDatabase.GetConnection();
            SqliteCommand command = new("UPDATE currencyStore SET currency = $amount WHERE steamId=$steamId", connection);
            command.Parameters.AddWithValue("$steamId", SteamID);
            command.Parameters.AddWithValue("$amount", CurrencyAmount);
            await command.ExecuteNonQueryAsync();
        }

        public void Destroy()
        {
            System.CurrencyDatabase.OnChangedEvent -= OnChanged;
        }
    }

    public class CurrencyConfiguration : ModuleConfiguration
    {
        public string CurrencyFormat { get; set; } = "{amount} {currency}";
        public string Currency { get; set; } = "points";
        public int StartingAmount { get; set; } = 0;
    }

    public class CurrencyDatabase
    {
        public static SqliteConnection Connection { get; private set; } = null!;

        public CurrencyDatabase()
        {
            string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "data");
            string path = Path.Combine(directoryPath, "CurrencySystem.db");

            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            if (!File.Exists(path))
                File.Create(path);
        }

        public async Task CreateCurrencyTable()
        {
            var cmd = Connection.CreateCommand();
            cmd.CommandText =
                @"CREATE TABLE IF NOT EXISTS currencyStore (steamId INT PRIMARY KEY, currency INT)";
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task Open()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Data", "CurrencySystem.db");

            string connectionString = new SqliteConnectionStringBuilder()
            {
                Mode = SqliteOpenMode.ReadWriteCreate,
                DataSource = path,
                Cache = SqliteCacheMode.Shared
            }.ToString();

            Connection = new SqliteConnection(connectionString);
            await Connection.OpenAsync();
        }

        public async Task Close()
        {
            await Connection.CloseAsync();
        }

        public SqliteConnection GetConnection()
        {
            return Connection;
        }

        public Dictionary<CurrencyPlayer, int> GetTop(CurrencySystem system, int numToReturn)
        {
            Dictionary<CurrencyPlayer, int> wealthiest = new();
            SqliteCommand top = new SqliteCommand("SELECT TOP $num * FROM currencyStore ORDER BY currency ASC");
            top.Parameters.AddWithValue("$num", numToReturn);

            using (SqliteDataReader reader = top.ExecuteReader())
            {
                while (reader.Read())
                {
                    string steamId = reader.GetString(0);
                    int currency = reader.GetInt32(1);
                    CurrencyPlayer player = system.GetCurrencyPlayer(steamId);

                    wealthiest.Add()
                }
            }
        }

        public virtual void OnChanged(CurrencyChangedArgs e)
        {
            EventHandler<CurrencyChangedArgs> handler = OnChangedEvent;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        public event EventHandler<CurrencyChangedArgs> OnChangedEvent;
    }

    public class CurrencyChangedArgs
    {
        public int NewValue { get; set; }
    }
}
