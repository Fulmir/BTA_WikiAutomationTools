using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UtilityClassLibrary
{
    public static class MediaWikiTextEncoder
    {
        public static string ConvertToMediaWikiSafeText(string text)
        {
            string output = "";
            foreach(char c in text)
            {
                output += CheckAndConvertCharacter(c);
            }
            return output;
        }

        private static string CheckAndConvertCharacter(char character)
        {
            switch(character)
            {
                case '!':
                    return "%21";
                case '\"':
                    return "%22";
                case '#':
                    return "%23";
                case '$':
                    return "%24";
                case '%':
                    return "%25";
                case '&':
                    return "%26";
                case '\'':
                    return "%27";
                case '(':
                    return "%28";
                case ')':
                    return "%29";
                case '*':
                    return "%2A";
                case '+':
                    return "%2B";
                case ',':
                    return "%2C";
                case '/':
                    return "%2F";
                case ':':
                    return "%3A";
                case ';':
                    return "%3B";
                case '=':
                    return "%3D";
                case '?':
                    return "%3F";
                case '@':
                    return "%40";
                case '[':
                    return "%5B";
                case ']':
                    return "%5D";
            }
            return character.ToString();
        }
    }
}
