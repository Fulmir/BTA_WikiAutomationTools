using BT_JsonProcessingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BTA_WikiTableGen
{
    internal static class MechFileSearch
    {
        static Regex BlacklistDirectories = new Regex(@"(BT Advanced Battle Armor|CustomUnits)");
        static Regex ClanMechDirectories = new Regex(@"BT Advanced Clan Mechs");
        static Regex QuadMechDirectories = new Regex(@"BT Advanced Quad Mechs");
        static Regex SanctuaryMechDirectories = new Regex(@"(BT Advanced Sanctuary Worlds Mechs|Heavy Metal Sanctuary Worlds Units)");
        static Regex HeroMechDirectories = new Regex(@"BT Advanced Unique Mechs");
        static Regex CommunityContentDirectories = new Regex(@"Community Content");

        static Dictionary<string, Dictionary<string, MechStats>> InnerSphereMechs = new Dictionary<string, Dictionary<string, MechStats>>();
        static Dictionary<string, Dictionary<string, MechStats>> ClanMechs = new Dictionary<string, Dictionary<string, MechStats>>();
        static Dictionary<string, Dictionary<string, MechStats>> SanctuaryMechs = new Dictionary<string, Dictionary<string, MechStats>>();
        static Dictionary<string, Dictionary<string, MechStats>> QuadMechs = new Dictionary<string, Dictionary<string, MechStats>>();
        static Dictionary<string, Dictionary<string, MechStats>> HeroMechs = new Dictionary<string, Dictionary<string, MechStats>>();
        static Dictionary<string, Dictionary<string, MechStats>> CommunityContentMechs = new Dictionary<string, Dictionary<string, MechStats>>();

        static Dictionary<string, MechStats> allMechs = new Dictionary<string, MechStats>();

        public static void GetAllMechsFromDefs(string modsFolder)
        {
            List<BasicFileData> chassisDefs = ModJsonHandler.SearchFiles(modsFolder, "chassisdef*.json");

            foreach(BasicFileData chassisDef in chassisDefs)
            {
                if (!BlacklistDirectories.IsMatch(chassisDef.Path))
                {
                    BasicFileData mechDef = GetMechDef(chassisDef);
                    if (!File.Exists(mechDef.Path))
                        continue;

                    var tempChassisDoc = JsonDocument.Parse(new StreamReader(chassisDef.Path).ReadToEnd());

                    string variantName = tempChassisDoc.RootElement.GetProperty("VariantName").ToString();
                    string chassisName = tempChassisDoc.RootElement.GetProperty("Description").GetProperty("Name").ToString();

                    allMechs[variantName] = new MechStats(modsFolder, variantName, chassisDef, mechDef);

                    if (ClanMechDirectories.IsMatch(chassisDef.Path))
                        AddToNestedDictionary(chassisName, variantName, ref ClanMechs);

                    else if (SanctuaryMechDirectories.IsMatch(chassisDef.Path))
                        AddToNestedDictionary(chassisName, variantName, ref SanctuaryMechs);

                    else if (HeroMechDirectories.IsMatch(chassisDef.Path))
                        AddToNestedDictionary(chassisName, variantName, ref HeroMechs);

                    else if (QuadMechDirectories.IsMatch(chassisDef.Path))
                        AddToNestedDictionary(chassisName, variantName, ref QuadMechs);

                    else if (CommunityContentDirectories.IsMatch(chassisDef.Path))
                        AddToNestedDictionary(chassisName, variantName, ref CommunityContentMechs);

                    else
                        AddToNestedDictionary(chassisName, variantName, ref InnerSphereMechs);
                }
            }
        }

        private static BasicFileData GetMechDef(BasicFileData chassisDef)
        {
            string baseSubDirectory = chassisDef.Path.Remove(chassisDef.Path.Length - chassisDef.FileName.Length - 7 - 1);
            string mechFileName = chassisDef.FileName.Replace("chassisdef_", "mechdef_");

            return new BasicFileData() { Path = baseSubDirectory + "mech\\" + mechFileName, FileName = mechFileName };
        }

        private static void AddToNestedDictionary(string chassisName, string variantName, ref Dictionary<string, Dictionary<string, MechStats>> target)
        {
            if (!target.ContainsKey(chassisName))
                target[chassisName] = new Dictionary<string, MechStats>();

            target[chassisName][variantName] = allMechs[variantName];
        }
    }
}
