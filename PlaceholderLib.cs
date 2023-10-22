using BBRAPIModules;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace BattleBitAPI.Features
{
    [Module("A library for placeholders. Supports multiple elements and formats rich text.", "1.3.0")]
    public class PlaceholderLib : BattleBitModule
    {
        private readonly Regex re = new Regex(@"\{([^\}]+)\}", RegexOptions.Compiled);

        public string Text { get; set; }
        public Dictionary<string, object> Parameters;

        public PlaceholderLib Create() => new PlaceholderLib();
        public PlaceholderLib Create(string text) => new PlaceholderLib(text);
        public PlaceholderLib Create(string text, Dictionary<string, object> parameters) => new PlaceholderLib(text, parameters);

        public PlaceholderLib()
        {
            Text = "";
            Parameters = new();
        }

        public PlaceholderLib(string text)
        {
            Text = text;
            Parameters = new Dictionary<string, object>();
        }

        public PlaceholderLib(string text, Dictionary<string, object> parameters)
        {
            Text = text;
            Parameters = parameters;
        }

        public PlaceholderLib AddParam(string key, object value, bool translateHex = true)
        {
            if (key == null || value == null)
            {
                return this;
            }

            if (translateHex)
                value = GetHexTranslate(value.ToString()!);

            Parameters.Add(key, value);
            return this;
        }

        public PlaceholderLib AddParam(string key, object value)
        {
            if (key == null || value == null)
            {
                return this;
            }

            value = GetHexTranslate(value.ToString()!);

            Parameters.Add(key, value);
            return this;
        }

        public string GetHexTranslate(string str)
        {
            return re.Replace(str, delegate (Match match)
            {
                if (match.Groups[1].Value.StartsWith("#"))
                    return $"<color={match.Groups[1].Value}>";
                else if (match.Groups[1].Value.Equals("/"))
                    return "</color>";

                return "{" + match.Groups[1].Value + "}";
            });
        }

        public string GetSurroundedValue(string str)
        {
            List<string> split = str.Split(" ").ToList();
            string newString = "";

            bool limited = str.StartsWith("!");

            if (limited)
            {
                str = str.Substring(1);

                if (Parameters.ContainsKey(str))
                    return Parameters[str].ToString()!;
                else
                    return "{!" + str + "}";
            }

            foreach (string value in split)
            {
                string newValue = GetValueOf(value);
                newString += newValue;
            }

            return newString;
        }

        public string GetValueOf(string str)
        {
            string[] equalsSplit = str.Split("=");

            if (str.StartsWith("#"))
                return $"<color={str}>";
            else if (str.Equals("/"))
                return "</color>";
            else if (str.StartsWith("/"))
                return "<" + str + ">";
            else if (Parameters.ContainsKey(str))
                return GetHexTranslate(Parameters[str].ToString()!);
            else if (equalsSplit.Length > 1)
            {
                return "<" + str + ">";
            }

            switch (str)
            {
                case "b":
                case "i":
                case "lowercase":
                case "uppercase":
                case "smallcaps":
                case "noparse":
                case "nobr":
                case "sup":
                case "sub":
                    return "<" + str + ">";
                default:
                    return "{" + str + "}";
            }
        }

        public string Build()
        {
            return re.Replace(Text, delegate (Match match)
            {
                return GetSurroundedValue(match.Groups[1].Value);
            });
        }

        [Obsolete]
        public string Run() => Build();
    }
}
