using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MantaRay
{
    internal static class GlobalsHelper
    {
        public static Dictionary<string, string> Globals { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public static Dictionary<string, string> GlobalsFromConnectComponent { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        //public static readonly Regex regexAdvanced = new Regex(@"<([\w]+?)-??([\d.]*)*?>", RegexOptions.Compiled);
        public static readonly Regex regexAdvanced = new Regex(@"<([a-zA-Z]+[\d]*)-??((?<=-)([\d]*||.))*?>", RegexOptions.Compiled);
        

        public static object @Lock = new object();
        /*
         * This is:
         * LETTER + optional number> for a key
         * LETTER + optional number +  "-" + number for a key and an int of how many letters to remove from the value (ie if <hdr> == "path.hdr", then <hdr-3> will == "path."
         * LETTER + optional number +  "-." will remove any file ending of the value.
         */

        public static string ApplyGlobals(this string s, Dictionary<string, string> locals = null, List<string> missingKeys = null, int maxDepth = 1)
        {
            for (int i = 0; i < maxDepth + 1; i++)
            {
                s = ApplyGlobalsOnce(s, locals, missingKeys);
            }
            return s;
        }

        private static string ApplyGlobalsOnce(string s, Dictionary<string, string> locals = null, List<string> missingKeys = null)
        {
            if (s == null)
                return s;

            lock (Lock)
            {
                if (locals != null)
                {
                    Dictionary<string, string> _locals = new Dictionary<string, string>(Globals);
                    // Setup the dict only once and not in the Replacers method
                    foreach (KeyValuePair<string, string> item in locals)
                    {
                        _locals[item.Key] = item.Value;
                    }


                    return regexAdvanced.Replace(s.Replace('−', '-'), new MatchEvaluator((v) => Replacers(v, _locals, missingKeys)));
                    //Replacing '-' unicode with the default ascii '-'. The unicode one was found in gensky documentation.
                    //Did you understand it? The two dashes are not the same!
                }
                else
                {
                    return regexAdvanced.Replace(s.Replace('−', '-'), new MatchEvaluator((v) => Replacers(v, null, missingKeys)));
                }

            }

        }


        private static String Replacers(Match matchResult, Dictionary<string, string> locals = null, List<string> missingKeys = null)
        {

            Dictionary<string, string> dict = new Dictionary<string, string>(GlobalsFromConnectComponent);
            foreach (var kvp in Globals)
            {
                dict[kvp.Key] = kvp.Value;
            }
            if (locals != null)
            {
                foreach (var kvp in locals)
                {
                    dict[kvp.Key] = kvp.Value;
                }
            }

            
            if (!dict.ContainsKey(matchResult.Groups[1].Value))
            {
                missingKeys?.Add(matchResult.Groups[1].Value);
                return "<" + matchResult.Groups[1].Value + ">";
            }


            if (matchResult.Groups[2].Success)
            {
                if (int.TryParse(matchResult.Groups[2].Value, out int delNumbers))
                {
                    if (int.TryParse(dict[matchResult.Groups[1].Value], out int inNumber))
                    {
                        return (inNumber - delNumbers).ToString();
                    }
                    else
                    {
                        return dict[matchResult.Groups[1].Value].Substring(0, Math.Max(0, dict[matchResult.Groups[1].Value].Length - delNumbers));

                    }
                }
                else if (String.Equals(matchResult.Groups[2].Value, ".", StringComparison.InvariantCulture))
                {
                    string[] parts = dict[matchResult.Groups[1].Value].Split('.');
                    return String.Join(".", parts.Take(parts.Length - 1));
                }
                else
                    throw new Exception("invalid syntax. Use <value>,  <value-22> or <value-.>");

            }
            else
                return dict[matchResult.Groups[1].Value];

        }

        
    }
}
