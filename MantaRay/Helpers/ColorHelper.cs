using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MantaRay.Helpers
{
    public static class ColorHelper
    {
        static readonly Random rnd = new Random();

        public static Color GetRandomColor(int minBrightness = 100, int maxBrightness = 255, int alpha = 255)
        {
            return Color.FromArgb(alpha, rnd.Next(minBrightness, maxBrightness), rnd.Next(minBrightness, maxBrightness), rnd.Next(minBrightness, maxBrightness));
        }
    }
}
