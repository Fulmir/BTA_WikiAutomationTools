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
        static Regex BlacklistDirectories = new Regex(@"(BT Advanced Battle Armor|CustomUnits)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static Regex ClanMechDirectories = new Regex(@"BT Advanced Clan Mechs", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static Regex QuadMechDirectories = new Regex(@"BT Advanced Quad Mechs", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static Regex SanctuaryMechDirectories = new Regex(@"(BT Advanced Sanctuary Worlds Mechs|Heavy Metal Sanctuary Worlds Units)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static Regex HeroMechDirectories = new Regex(@"BT Advanced Unique Mechs", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static Regex CommunityContentDirectories = new Regex(@"Community Content", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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

                    allMechs[variantName] = new MechStats(variantName, chassisDef, mechDef);
                    if (allMechs[variantName].Tags.Contains("BLACKLISTED") || allMechs[variantName].Tags.Contains("NOSALVAGE"))
                        continue;

                    if(IsHeroMech(variantName))
                        AddToNestedDictionary(chassisName, variantName, ref HeroMechs);

                    else if (IsClanMech(variantName, chassisDef.Path))
                        AddToNestedDictionary(chassisName, variantName, ref ClanMechs);

                    else if (SanctuaryMechDirectories.IsMatch(chassisDef.Path))
                        AddToNestedDictionary(chassisName, variantName, ref SanctuaryMechs);

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

        public static void OutputMechsToWikiTables()
        {
            StreamWriter mechTablePageWriter = new StreamWriter("MechPageTables.txt", false);

            mechTablePageWriter.WriteLine("==Inner Sphere Mechs==");
            mechTablePageWriter.Write(OutputDictionaryToStringByTonnage("Inner Sphere {0} Mechs", ref InnerSphereMechs));
            mechTablePageWriter.Close();

            mechTablePageWriter = new StreamWriter("MechPageTables.txt", true);
            mechTablePageWriter.WriteLine("==Clan Mechs==");
            mechTablePageWriter.Write(OutputDictionaryToStringByTonnage("Clan {0} Mechs", ref ClanMechs));
            mechTablePageWriter.Close();

            mechTablePageWriter = new StreamWriter("MechPageTables.txt", true);
            mechTablePageWriter.WriteLine("==Sanctuary Worlds Mechs==");
            mechTablePageWriter.Write(OutputDictionaryToStringByTonnage("Sanctuary Worlds {0} Mechs", ref SanctuaryMechs));
            mechTablePageWriter.Close();

            mechTablePageWriter = new StreamWriter("MechPageTables.txt", true);
            mechTablePageWriter.WriteLine("==Hero Mechs==");
            mechTablePageWriter.Write(OutputDictionaryToStringByTonnage("{0} Hero Mechs", ref HeroMechs));
            mechTablePageWriter.Close();

            mechTablePageWriter = new StreamWriter("MechPageTables.txt", true);
            mechTablePageWriter.WriteLine("==Other Mechs==");
            mechTablePageWriter.Write(OutputDictionaryToString("Quad Mechs", ref QuadMechs));
            mechTablePageWriter.Close();

            mechTablePageWriter = new StreamWriter("MechPageTables.txt", true);
            mechTablePageWriter.Write(OutputDictionaryToString("Community Content Mechs", ref CommunityContentMechs));
            mechTablePageWriter.Close();
        }

        private static string OutputDictionaryToStringByTonnage(string pluggableTitleString, ref Dictionary<string, Dictionary<string, MechStats>> targetDictionary)
        {
            List<string> sortedMechNames = targetDictionary.Keys.ToList();
            sortedMechNames.Sort();

            StringBuilder LightMechOutput = new StringBuilder();
            StringWriter LightMechWriter = new StringWriter(LightMechOutput);
            StartNewTableSection(string.Format(pluggableTitleString, "Light"), LightMechWriter);

            StringBuilder MediumMechOutput = new StringBuilder();
            StringWriter MediumMechWriter = new StringWriter(MediumMechOutput);
            StartNewTableSection(string.Format(pluggableTitleString, "Medium"), MediumMechWriter);

            StringBuilder HeavyMechOutput = new StringBuilder();
            StringWriter HeavyMechWriter = new StringWriter(HeavyMechOutput);
            StartNewTableSection(string.Format(pluggableTitleString, "Heavy"), HeavyMechWriter);

            StringBuilder AssaultMechOutput = new StringBuilder();
            StringWriter AssaultMechWriter = new StringWriter(AssaultMechOutput);
            StartNewTableSection(string.Format(pluggableTitleString, "Assault"), AssaultMechWriter);

            foreach (string mechName in sortedMechNames)
            {
                Dictionary<int, List<string>> tempTonnageTable = new Dictionary<int, List<string>>();

                List<string> sortedVariantModels = targetDictionary[mechName].Keys.ToList();
                sortedVariantModels.Sort(new MechModelNameComparer());

                foreach (string mechVariant in sortedVariantModels)
                {
                    int variantTonnage = targetDictionary[mechName][mechVariant].MechTonnage;
                    if (!tempTonnageTable.ContainsKey(variantTonnage))
                        tempTonnageTable[variantTonnage] = new List<string>();

                    tempTonnageTable[variantTonnage].Add(mechVariant);
                }

                List<int> tempSortedTonnages = tempTonnageTable.Keys.ToList();
                tempSortedTonnages.Sort();

                foreach(int tonnage in tempSortedTonnages)
                {
                    StringWriter tonnageWriter;
                    if (tonnage >= 80)
                        tonnageWriter = AssaultMechWriter;
                    else if(tonnage >= 60)
                        tonnageWriter = HeavyMechWriter;
                    else if(tonnage >= 40)
                        tonnageWriter = MediumMechWriter;
                    else
                        tonnageWriter = LightMechWriter;

                    StartMechTitleSection(tonnageWriter, mechName, tempTonnageTable[tonnage].Count);

                    List<MechStats> tempMechs = new List<MechStats>();
                    foreach(string mechVariant in tempTonnageTable[tonnage])
                    {
                        tempMechs.Add(targetDictionary[mechName][mechVariant]);
                    }

                    List<QuirkDef> tempQuirkList = QuirkHandler.CompileQuirksForVariants(tempMechs);
                    foreach(QuirkDef quirk in tempQuirkList)
                        QuirkHandler.OutputQuirkToString(quirk, true, tonnageWriter);

                    List<AffinityDef> tempAffinityList = AffinityHandler.CompileAffinitiesForVariants(tempMechs);
                    foreach(AffinityDef affinity in tempAffinityList)
                        AffinityHandler.OutputAffinityToString(affinity, tonnageWriter);

                    foreach (MechStats mechVariant in tempMechs)
                        mechVariant.OutputStatsToString(tonnageWriter);
                }
            }

            CloseTableSection(LightMechWriter);
            LightMechWriter.Close();

            CloseTableSection(MediumMechWriter);
            MediumMechWriter.Close();

            CloseTableSection(HeavyMechWriter);
            HeavyMechWriter.Close();

            CloseTableSection(AssaultMechWriter);
            AssaultMechWriter.Close();

            return LightMechOutput.ToString() + MediumMechOutput.ToString() + HeavyMechOutput.ToString() + AssaultMechOutput.ToString();
        }
        private static string OutputDictionaryToString(string pluggableTitleString, ref Dictionary<string, Dictionary<string, MechStats>> targetDictionary)
        {
            List<string> sortedMechNames = targetDictionary.Keys.ToList();
            sortedMechNames.Sort();

            StringBuilder mechOutput = new StringBuilder();
            StringWriter mechWriter = new StringWriter(mechOutput);
            StartNewTableSection(pluggableTitleString, mechWriter);

            foreach (string mechName in sortedMechNames)
            {
                List<string> sortedVariantModels = targetDictionary[mechName].Keys.ToList();
                sortedVariantModels.Sort(new MechModelNameComparer());

                StartMechTitleSection(mechWriter, mechName, targetDictionary[mechName].Count);

                List<QuirkDef> tempQuirkList = QuirkHandler.CompileQuirksForVariants(targetDictionary[mechName].Values.ToList());
                foreach (QuirkDef quirk in tempQuirkList)
                    QuirkHandler.OutputQuirkToString(quirk, true, mechWriter);

                List<AffinityDef> tempAffinityList = AffinityHandler.CompileAffinitiesForVariants(targetDictionary[mechName].Values.ToList());
                foreach (AffinityDef affinity in tempAffinityList)
                    AffinityHandler.OutputAffinityToString(affinity, mechWriter);

                foreach (string mechVariant in sortedVariantModels)
                {
                    targetDictionary[mechName][mechVariant].OutputStatsToString(mechWriter);
                }
            }
            CloseTableSection(mechWriter);
            mechWriter.Close();

            return mechOutput.ToString();
        }

        private static void StartNewTableSection(string section, StringWriter tableWriter)
        {
            tableWriter.WriteLine("<div class=\"mw-collapsible\">");
            tableWriter.WriteLine($"==={section}===");
            WriteTableStart(tableWriter);
        }

        private static void CloseTableSection(StringWriter tableWriter)
        {
            tableWriter.WriteLine("|}");
            tableWriter.WriteLine();
            tableWriter.WriteLine("</div>");
            tableWriter.WriteLine();
            tableWriter.WriteLine();
        }

        private static void WriteTableStart(StringWriter tableWriter)
        {
            tableWriter.WriteLine("{| class=\"wikitable sortable\" style=\"text-align: center;\"");
            tableWriter.WriteLine("! colspan=\"4\" |Mech");
            tableWriter.WriteLine("! colspan=\"5\" |Hardpoints");
            tableWriter.WriteLine("! colspan=\"3\" |Engine");
            tableWriter.WriteLine("! colspan=\"3\" |Components");
            tableWriter.WriteLine("! colspan=\"2\" |Free Tonnage");
            tableWriter.WriteLine("! colspan=\"3\" |Speed");
            tableWriter.WriteLine("|-");
            tableWriter.WriteLine("!<small>Chassis</small>");
            tableWriter.WriteLine("!<small>Model</small>");
            tableWriter.WriteLine("!<small>Mass</small>");
            tableWriter.WriteLine("!<small>Role</small>");
            tableWriter.WriteLine("!<small>B</small>");
            tableWriter.WriteLine("!<small>E</small>");
            tableWriter.WriteLine("!<small>M</small>");
            tableWriter.WriteLine("!<small>S</small>");
            tableWriter.WriteLine("!<small>O</small>");
            tableWriter.WriteLine("!<small>Type</small>");
            tableWriter.WriteLine("!<small>Core</small>");
            tableWriter.WriteLine("!<small>Heat Sinks</small>");
            tableWriter.WriteLine("!<small>Structure</small>");
            tableWriter.WriteLine("!<small>Armor</small>");
            tableWriter.WriteLine("!<small>Melee Weapon </small>");
            tableWriter.WriteLine("!<small>Core Gear</small>");
            tableWriter.WriteLine("!<small>Bare</small>");
            tableWriter.WriteLine("!<small>Walk</small>");
            tableWriter.WriteLine("!<small>Sprint</small>");
            tableWriter.WriteLine("!<small>Jump</small>");
            tableWriter.WriteLine("|-");
        }

        private static void StartMechTitleSection(StringWriter writer, string mechName, int variantCount)
        {
            writer.WriteLine($"|rowspan=\"{variantCount}\"|");
            writer.WriteLine($"[[File:{mechName}.png|125px|border|center]]");
            writer.WriteLine();
            writer.WriteLine($"'''[[{mechName.Replace(' ', '_')}|{mechName.ToUpper()}]]'''");
            writer.WriteLine();
        }

        private static bool IsHeroMech(string variantName)
        {
            if (allMechs[variantName].Tags.Contains("HeroMech"))
                return true;
            return false;
        }

        private static bool IsClanMech(string variantName, string chassisDefPath)
        {
            if (allMechs[variantName].Tags.Contains("ClanMech"))
                return true;
            if (ClanMechDirectories.IsMatch(chassisDefPath))
                return true;
            return false;
        }
    }
}
