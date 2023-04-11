using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BTA_WikiTableGen
{
    internal class MechModelNameComparer : IComparer<string>
    {
        private readonly Regex NumberRegex = new Regex("([a-zA-Z-_ ]*)(\\d+)([a-zA-Z-_ ]*)", RegexOptions.Compiled);
        public int Compare(string? x, string? y)
        {
            if(x == null && y == null)
                return 0;

            if(x == null)
                return -1;
            if(y == null)
                return 1;

            if(x.Equals(y))
                return 0;

            x = x.ToUpper();
            y = y.ToUpper();

            if (!NumberRegex.IsMatch(x) && !NumberRegex.IsMatch(y))
            {
                return x.CompareTo(y);
            }
            if (!NumberRegex.IsMatch(x))
                return 1;
            if (!NumberRegex.IsMatch(y))
                return -1;
            else
            {
                GroupCollection firstMatches = NumberRegex.Matches(x)[0].Groups;
                GroupCollection secondMatches = NumberRegex.Matches(y)[0].Groups;

                if (firstMatches[1].Length != secondMatches[1].Length)
                    return firstMatches[0].Value.CompareTo(secondMatches[0].Value);

                for(int i = 0; i < firstMatches[1].Length; i++)
                {
                    if (firstMatches[1].Value[i] != secondMatches[1].Value[i])
                    {
                        return firstMatches[1].Value[i].CompareTo(secondMatches[1].Value[i]);
                    }
                }

                if (Int32.TryParse(firstMatches[2].Value, out int firstInt) && Int32.TryParse(secondMatches[2].Value, out int secondInt))
                {
                    if(firstInt != secondInt)
                        return firstInt - secondInt;
                }

                if (firstMatches.Count > 3 && secondMatches.Count > 3)
                {
                    return firstMatches[3].Value.CompareTo(secondMatches[3].Value);
                }
                else if (firstMatches.Count > 3)
                {
                    return 1;
                }
                else if(secondMatches.Count > 3)
                {
                    return -1;
                }
            }
            return x.CompareTo(y);
        }
    }
}
