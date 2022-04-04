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

        public static readonly Regex regex = new Regex(@"\<(\w+)\>", RegexOptions.Compiled);
        

        public static string AddGlobals(this string s)
        {
            return regex.Replace(s, match => { return Globals.ContainsKey(match.Groups[1].Value) ? Globals[match.Groups[1].Value] : match.Value; });
        }

        public static string AddLocals(this string s, Dictionary<string, string> locals)
        {
            return regex.Replace(s, match => { return locals.ContainsKey(match.Groups[1].Value) ? locals[match.Groups[1].Value] : match.Value; }).AddGlobals();
        }
    }
}
