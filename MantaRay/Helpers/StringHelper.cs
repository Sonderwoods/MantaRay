using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MantaRay
{
    public static class StringHelper
    {

        public static bool HasLetters(this string line)
        {
            
            for (int i = 0; i < line.Length; i++)
            {

                if (line[i] != 'e' && line[i] != 'E' && (line[i] >= 'a' && line[i] <= 'z' || line[i] >= 'A' && line[i] <= 'Z'))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// https://stackoverflow.com/questions/842057/how-do-i-convert-a-timespan-to-a-formatted-string
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static string ToReadableAgeString(this TimeSpan span)
        {
            return string.Format("{0:0}", span.Days / 365.25);
        }

        public static string Cleaned(this string s)
        {

            StringBuilder sb = new StringBuilder();
            foreach (char c in s)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_')
                {
                    sb.Append(c);
                }
                else
                    sb.Append('_');
            }
            return sb.ToString();

        }

        [Pure]
        /// <summary>
        /// https://stackoverflow.com/questions/842057/how-do-i-convert-a-timespan-to-a-formatted-string
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static string ToReadableString(this TimeSpan span)
        {
            string formatted = string.Format("{0}{1}{2}{3}{4}",
                span.Duration().Days > 0 ? string.Format("{0:0} day{1}, ", span.Days, span.Days == 1 ? string.Empty : "s", CultureInfo.InvariantCulture) : string.Empty,
                span.Duration().Hours > 0 ? string.Format("{0:0} hour{1}, ", span.Hours, span.Hours == 1 ? string.Empty : "s", CultureInfo.InvariantCulture) : string.Empty,
                span.Duration().Minutes > 0 ? string.Format("{0:0} minute{1}, ", span.Minutes, span.Minutes == 1 ? string.Empty : "s", CultureInfo.InvariantCulture) : string.Empty,
                span.Duration().Seconds > 0 ? string.Format("{0:0} second{1}, ", span.Seconds, span.Seconds == 1 ? string.Empty : "s", CultureInfo.InvariantCulture) : string.Empty,
                span.Duration().Milliseconds > 0 ? string.Format("{0:0} millisecond{1}", span.Milliseconds, span.Milliseconds == 1 ? string.Empty : "s", CultureInfo.InvariantCulture) : string.Empty);

            if (formatted.EndsWith(", ")) formatted = formatted.Substring(0, formatted.Length - 2);

            if (string.IsNullOrEmpty(formatted)) formatted = "0 seconds";

            return formatted;
        }

        public static string ToShortString(this TimeSpan span)
        {
            double runTime = span.TotalMilliseconds;

            if (runTime < 1000)
                return $"{span.Milliseconds}ms";
            if (runTime < 60000)
                return string.Format("{0:0.0}s", runTime / 1000.0, CultureInfo.InvariantCulture);
            if (runTime < 3600000)
                return string.Format("{0:0.0}m", runTime / 60000.0, CultureInfo.InvariantCulture);
            if (runTime < 86400000)
                return string.Format("{0:0.0}h", runTime / 3600000.0, CultureInfo.InvariantCulture);
            return string.Format("{0:0.0}d", runTime / 86400000.0, CultureInfo.InvariantCulture);
        }
    }
}
