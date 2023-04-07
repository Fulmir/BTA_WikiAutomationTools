using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTA_SpamFactionBuilder
{
    internal class SpamFactionData
    {
        internal string? FactionID { get; set; }
        internal string? Name { get; set; }
        internal string? Description { get; set; }

        internal List<string> EmployerList { get; set; } = new List<string>();
        internal string? PrimaryEmployer { get; set; }
        internal bool RestrictionIsWhitelist { get; set; } = true;
        internal List<string> Personality { get; set; } = new List<string>();
        internal int UnitRating { get; set; } = 0;
        internal List<string> PlanetNames { get; set; } = new List<string>();
        internal bool IncludeUnitsSectionInOutput { get; set; } = false;
        internal string SubcommandRating { get; set; } = string.Empty;

        internal string OutputDefToHTML()
        {
            string output = "<div style=\"display: block; clear: both; max-width: 900px; margin-left: 20px;\">\r\n";
            output += $"<span style=\"font-size: 1em;font-weight: bold;\">{Name}</span>\n";
            //output += "<div style=\"\">\r\n";
            output += "{| class=\"wikitable\" style=\"clear: left; float: left; padding: 10px; background: transparent; min-width:250px;\"\r\n";
            output += $"|[[Image:{FactionID}_logo.png|130x130px|center|{Name}]]\r\n";
            output += $"<u>'''{Name}'''</u>\r\n";
            output += "\r\n";
            if (SubcommandRating != string.Empty)
            {
                output += $"'''Unit Rating''': {SubcommandRating}\r\n";
            }
            if (Personality.Count > 0)
            {
                output += $"'''Personality''': {OutputListWithCommas(Personality)}\r\n";
                output += "\r\n";
            }
            if(UnitRating > 0)
                output += $"'''MRB Rating''': {UnitRating}\r\n";
            if (PlanetNames.Count > 0)
            {
                output += ";<u>Planets</u>\r\n";
                foreach (string item in PlanetNames)
                    output += $":{item}\r\n";
            }
            if (EmployerList.Count > 0)
            {
                output += ";<u>Factions</u>\r\n";
                foreach (string item in EmployerList)
                    output += $":{item}\r\n";
            }
            output += "|}\r\n";
            //output += "</div>";
            output += "\r\n";
            output += $"<p style=\"padding: 15px 0 0 10px; float: left; max-width: 630px\">{Description}</p>\r\n";
            if(IncludeUnitsSectionInOutput)
            {
                output += "<div class=\"toccolours mw-collapsible mw-collapsed\" style=\"float: left; clear: left; width: 75%;\">\r\n";
                output += $"<div style=\"font-weight:bold;line-height:1.6;\">Mechs fielded by the {Name}</div>\r\n";
                output += $"<div class=\"mw-collapsible-content\">{{{{FactionMechs|{FactionID}}}}}</div></div>\r\n";
                output += "\r\n";
            }
            output += "</div>";
            output += "\r\n";

            return output;
        }

        private string OutputListWithCommas(List<string> list)
        {
            string output = "";
            bool first = true;
            foreach (var item in list)
            {
                if (!first)
                {
                    first = false;
                    output += ", ";
                }

                output += item;
            }
            return output;
        }
    }
}
