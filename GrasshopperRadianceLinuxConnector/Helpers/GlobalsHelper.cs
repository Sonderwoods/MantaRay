using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GrasshopperRadianceLinuxConnector
{
    internal static class GlobalsHelper
    {
        public static readonly Dictionary<string, string> Globals = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public static readonly Regex regexAdvanced = new Regex(@"<([\w]+?)-??([\d.]*)*?>", RegexOptions.Compiled);


        private static String Replacers(Match matchResult, Dictionary<string, string> locals = null, List<string> missingKeys = null)
        {
            locals = locals ?? Globals;
            if (!locals.ContainsKey(matchResult.Groups[1].Value))
            {
                missingKeys?.Add(matchResult.Groups[1].Value);
                return "<" + matchResult.Groups[1].Value + ">";
            }


            if (matchResult.Groups[2].Success)
            {
                if (int.TryParse(matchResult.Groups[2].Value, out int delNumbers))
                {
                    return locals[matchResult.Groups[1].Value].Substring(0, Math.Max(0, locals[matchResult.Groups[1].Value].Length - 1 - delNumbers));
                }
                else if (String.Equals(matchResult.Groups[2].Value, ".", StringComparison.InvariantCulture))
                {
                    string[] parts = locals[matchResult.Groups[1].Value].Split('.');
                    return String.Join(".", parts.Take(parts.Length - 1));
                }
                else
                    throw new Exception("invalid syntax. Use <value>,  <value-22> or <value-.>");

            }
            else
                return locals[matchResult.Groups[1].Value];

        }


        public static string AddGlobals(this string s, Dictionary<string, string> locals = null, List<string> missingKeys = null)
        {

            if (locals != null)
            {
                Dictionary<string, string> _locals = new Dictionary<string, string>(locals);
                // Setup the dict only once and not in the Replacers method
                foreach (KeyValuePair<string, string> item in Globals) _locals[item.Key] = item.Value;
                
                return regexAdvanced.Replace(s, new MatchEvaluator((v) => Replacers(v, _locals, missingKeys)));
            }
            else
            {
                return regexAdvanced.Replace(s, new MatchEvaluator((v) => Replacers(v, null, missingKeys)));
            }


        }
    }
}
