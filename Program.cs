using System;
using System.Collections.Generic;
using System.Drawing;

namespace BBRModules
{
    public class Program
    {
        public static void Main()
        {
            List<string> gradient = GetGradients("#ff0000", "#0000ff", 10);
            Console.WriteLine(string.Join(", ", gradient));
        }

        public static List<string> GetGradients(string startHex, string endHex, int steps)
        {
            Color start = ColorTranslator.FromHtml(startHex);
            Color end = ColorTranslator.FromHtml(endHex);
            List<string> hexCodes = new();

            int stepA = ((end.A - start.A) / (steps - 1));
            int stepR = ((end.R - start.R) / (steps - 1));
            int stepG = ((end.G - start.G) / (steps - 1));
            int stepB = ((end.B - start.B) / (steps - 1));

            for (int i = 0; i < steps; i++)
            {
                Color color = Color.FromArgb(start.A + (stepA * i),
                                            start.R + (stepR * i),
                                            start.G + (stepG * i),
                                            start.B + (stepB * i));
                hexCodes.Add(ColorTranslator.ToHtml(color));
            }

            return hexCodes;
        }
    }
}
