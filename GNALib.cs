using BattleBitAPI.Common;
using BBRAPIModules;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

namespace BBRModules
{
    [Module("GNA\'s core library.", "1.0.0")]
    public class GNALib : BattleBitModule
    {
        public short GetPlayerCountInRoleOnTeam(RunnerServer server, Team team, GameRole role)
        {
            int index = 0;
            short count = 0;
            IEnumerable<RunnerPlayer> teamPlayers = team == Team.TeamA ? server.AllTeamAPlayers : server.AllTeamBPlayers;

            do
            {
                RunnerPlayer player = teamPlayers.ElementAt(index);

                if (player.Role == role)
                    count++;
                index++;
            } while (index < teamPlayers.Count());

            return count;
        }

        public List<RunnerPlayer> GetPlayersInTeam(RunnerServer server, Team team)
        {
            return team == Team.TeamA ? server.AllTeamAPlayers.ToList() : server.AllTeamBPlayers.ToList();
        }

        public string GetTimestringAsUnitResult(string timestring)
        {
            Dictionary<string, string> conversions = new Dictionary<string, string> {
                { "y", "year" },
                { "mo", "month" },
                { "w", "week" },
                { "d", "day" },
                { "h", "hour" },
                { "m", "minute" }
            };

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

                if (!conversions.ContainsKey(time.ToLower()))
                    valid = false;

                if (!valid)
                    return "";

                bool result = conversions.TryGetValue(time, out string unit);

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

        public int GetTimestringAsSeconds(string timestring)
        {
            bool valid = false;
            int total = 0;

            Dictionary<string, int> conversions = new Dictionary<string, int> {
                { "y", 31556952 },
                { "mo", 2629746 },
                { "w", 604800 },
                { "d", 86400 },
                { "h", 3600 },
                { "m", 60 }
            };

            bool isNumber = int.TryParse(timestring, out int secondsTime);

            if (isNumber)
                return secondsTime;

            foreach (Match match in Regex.Matches(timestring, "(\\d+)([A-Za-z]+)"))
            {
                valid = int.TryParse(match.Groups[1].Value, out int num);
                string time = match.Groups[2].Value;

                if (!conversions.ContainsKey(time.ToLower()))
                    valid = false;

                if (!valid)
                    return -1;

                conversions.TryGetValue(time, out int seconds);
                total += seconds * num;
            }

            return total;
        }

        public string GetSecondsAsTimeUnit(int seconds)
        {
            string timeunit = "Never";

            Dictionary<string, int> conversions = new Dictionary<string, int> {
                { "y", 31556952 },
                { "mo", 2629746 },
                { "w", 604800 },
                { "d", 86400 },
                { "h", 3600 },
                { "m", 60 }
            };

            if (seconds != -1)
            {
                timeunit = "";

                int remaining = seconds;
                int quantity = 0;

                for (int i = 0; i < conversions.Count; i++)
                {
                    KeyValuePair<string, int> pair = conversions.ElementAt(i);

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

        public List<string> GetAfter(string[] split, int index)
        {
            List<string> newSplit = new(split);
            newSplit.RemoveRange(0, index - 1);

            return newSplit;
        }

        public string Join(string[] strings, string separator)
        {
            string result = "";

            for (int i = 0; i < strings.Length; i++)
            {
                if (i == strings.Length - 1)
                    separator = "";

                string value = strings[i];
                result += value + separator;
            }

            return result;
        }

        public string Join(string[] strings, string separator, int fromIndex)
        {
            string result = "";

            for (int i = 0; i < strings.Length; i++)
            {

                if (i >= fromIndex)
                {
                    if (i == strings.Length - 1)
                        separator = "";

                    string value = strings[i];
                    result += value + separator;
                }
            }

            return result;
        }
    }
}
