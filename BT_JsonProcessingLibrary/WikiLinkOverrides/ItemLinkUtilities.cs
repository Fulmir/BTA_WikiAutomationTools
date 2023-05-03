using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UtilityClassLibrary.WikiLinkOverrides
{
    internal static class ItemLinkUtilities
    {

        public static bool RunStringCheckOnName(string checkString, string name)
        {
            if (checkString == name)
                return true;
            else if (name.Contains(checkString))
                return true;

            return false;
        }

        public static bool RunRegexCheckOnName(string regexString, string name)
        {
            Regex nameCheckRegex = new Regex(regexString);

            return nameCheckRegex.IsMatch(name);
        }
    }
}
