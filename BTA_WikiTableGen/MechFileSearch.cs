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

        static Dictionary<string, List<MechNameCounter>> PrefabToNameTracker = new Dictionary<string, List<MechNameCounter>>();

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

                    allMechs[variantName] = new MechStats(chassisName, variantName, chassisDef, mechDef);
                    if (allMechs[variantName].Blacklisted)
                        continue;

                    AddToPrefabToNameTracker(chassisName, variantName);

                    if (CommunityContentDirectories.IsMatch(chassisDef.Path))
                        AddToNestedDictionary(variantName, ref CommunityContentMechs);

                    else if (IsHeroMech(variantName))
                        AddToNestedDictionary(variantName, ref HeroMechs);

                    else if (IsClanMech(variantName, chassisDef.Path))
                        AddToNestedDictionary(variantName, ref ClanMechs);

                    else if (SanctuaryMechDirectories.IsMatch(chassisDef.Path))
                        AddToNestedDictionary(variantName, ref SanctuaryMechs);

                    else if (QuadMechDirectories.IsMatch(chassisDef.Path))
                        AddToNestedDictionary(variantName, ref QuadMechs);

                    else
                        AddToNestedDictionary(variantName, ref InnerSphereMechs);
                }
            }
        }

        private static BasicFileData GetMechDef(BasicFileData chassisDef)
        {
            string baseSubDirectory = chassisDef.Path.Remove(chassisDef.Path.Length - chassisDef.FileName.Length - 7 - 1);
            string mechFileName = chassisDef.FileName.Replace("chassisdef_", "mechdef_");

            return new BasicFileData() { Path = baseSubDirectory + "mech\\" + mechFileName, FileName = mechFileName };
        }

        private static void AddToNestedDictionary(string variantName, ref Dictionary<string, Dictionary<string, MechStats>> target)
        {
            string prefabKey = allMechs[variantName].PrefabId ?? allMechs[variantName].PrefabIdentifier;
            if (!target.ContainsKey(prefabKey))
                target[prefabKey] = new Dictionary<string, MechStats>();

            target[prefabKey][variantName] = allMechs[variantName];
        }
        
        private static void AddToPrefabToNameTracker(string chassisName, string variantName)
        {
            string prefabKey = allMechs[variantName].PrefabId ?? allMechs[variantName].PrefabIdentifier;
            if (!PrefabToNameTracker.ContainsKey(prefabKey))
                PrefabToNameTracker[prefabKey] = new List<MechNameCounter>();

            int refCounter = PrefabToNameTracker[prefabKey].FindIndex((counter) => counter.MechName == chassisName);
            if(refCounter > -1 && PrefabToNameTracker[prefabKey][refCounter].MechName == chassisName)
            {
                MechNameCounter tempCount = new MechNameCounter()
                {
                    MechName = chassisName,
                    UseCount = PrefabToNameTracker[prefabKey][refCounter].UseCount + 1
                };
                PrefabToNameTracker[prefabKey][refCounter] = tempCount;
                return;
            }
            PrefabToNameTracker[prefabKey].Add(new MechNameCounter()
            {
                MechName = chassisName,
                UseCount = 1
            });
        }

        private static bool TryGetNameForPrefabId(string prefabId, out string NameOutput)
        {
            MechNameCounter winningName = new MechNameCounter()
            {
                MechName = "ERROR",
                UseCount = -1
            };

            int ties = 1;

            foreach(MechNameCounter counter in PrefabToNameTracker[prefabId])
            {
                if (winningName.UseCount < counter.UseCount)
                {
                    winningName = counter;
                    ties = 1;
                }
                else if (winningName.UseCount == counter.UseCount)
                {
                    ties++;
                    if(winningName.MechName.Length > counter.MechName.Length)
                        winningName.MechName = counter.MechName;
                }
            }

            if (ties == 1)
            {
                NameOutput = winningName.MechName;
                return true;
            }
            NameOutput = winningName.MechName;
            return false;
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
            Dictionary<string, List<string>> primaryNamesToPrefabs = new Dictionary<string, List<string>>();

            foreach (string prefabId in targetDictionary.Keys.ToList())
            {
                TryGetNameForPrefabId(prefabId, out string primaryMechName);

                if (primaryNamesToPrefabs.ContainsKey(primaryMechName))
                    Console.WriteLine($"Already have value for name {primaryMechName} and prefab {prefabId}. Other prefab is {primaryNamesToPrefabs[primaryMechName].Last()}. Adding prefabId to list.");
                else
                    primaryNamesToPrefabs[primaryMechName] = new List<string>();

                primaryNamesToPrefabs[primaryMechName].Add(prefabId);
            }
            List<string> sortedMechNames = primaryNamesToPrefabs.Keys.ToList();
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
                foreach(string prefabIdFromName in primaryNamesToPrefabs[mechName])
                {
                    List<string> sortedVariantModels = targetDictionary[prefabIdFromName].Keys.ToList();
                    sortedVariantModels.Sort(new MechModelNameComparer());

                    int excludedVariantsCount = 0;
                    List<string> excludedVariants = new List<string>();
                    foreach (MechStats variant in targetDictionary[prefabIdFromName].Values)
                    {
                        if(variant.VariantAssemblyRules != null && 
                            (variant.VariantAssemblyRules.Value.Exclude || !variant.VariantAssemblyRules.Value.Include))
                        {
                            excludedVariantsCount++;
                            excludedVariants.Add(variant.MechModel);
                        }
                    }

                    int tonnage = targetDictionary[prefabIdFromName].First().Value.MechTonnage;

                    StringWriter variantWriter;
                    if (tonnage >= 80)
                        variantWriter = AssaultMechWriter;
                    else if (tonnage >= 60)
                        variantWriter = HeavyMechWriter;
                    else if (tonnage >= 40)
                        variantWriter = MediumMechWriter;
                    else
                        variantWriter = LightMechWriter;

                    List<MechStats> tempMechs = new List<MechStats>();
                    foreach (string variant in sortedVariantModels)
                    {
                        if (!excludedVariants.Contains(variant))
                            tempMechs.Add(targetDictionary[prefabIdFromName][variant]);
                    }

                    foreach(string variant in excludedVariants)
                    {
                        MechStats excludedVariant = allMechs[variant];
                        StartMechTitleSection(variantWriter, excludedVariant.MechName, 1);

                        foreach (QuirkDef quirk in excludedVariant.MechQuirks.Values)
                            QuirkHandler.OutputQuirkToString(quirk, true, variantWriter);

                        if(excludedVariant.MechAffinity.HasValue)
                            AffinityHandler.OutputAffinityToString(excludedVariant.MechAffinity.Value, variantWriter);

                        excludedVariant.OutputStatsToString(variantWriter);
                    }
                    if(sortedVariantModels.Count - excludedVariantsCount >= 1)
                    {
                        StartMechTitleSection(variantWriter, mechName, sortedVariantModels.Count - excludedVariantsCount);

                        List<QuirkDef> tempQuirkList = QuirkHandler.CompileQuirksForVariants(tempMechs);
                        foreach (QuirkDef quirk in tempQuirkList)
                            QuirkHandler.OutputQuirkToString(quirk, true, variantWriter);

                        List<AffinityDef> tempAffinityList = AffinityHandler.CompileAffinitiesForVariants(tempMechs);
                        foreach (AffinityDef affinity in tempAffinityList)
                            AffinityHandler.OutputAffinityToString(affinity, variantWriter);

                        foreach (MechStats mechVariant in tempMechs)
                            mechVariant.OutputStatsToString(variantWriter);
                    }
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
            string cleanMechName = mechName.Replace(' ', '_').Replace("'", "");
            string imageName = mechName.Replace("Royal", "",StringComparison.OrdinalIgnoreCase).Replace("Primitive", "", StringComparison.OrdinalIgnoreCase).Trim().Replace(' ', '_').Replace("'", "");

            string displayMechName = mechName;
            if (mechName.Contains("Primitive", StringComparison.OrdinalIgnoreCase))
                displayMechName = "Primitive " + mechName.Replace("Primitive", "", StringComparison.OrdinalIgnoreCase).Trim();

            writer.WriteLine($"|rowspan=\"{variantCount}\"|");
            writer.WriteLine($"[[File:{imageName}.png|125px|border|center]]");
            writer.WriteLine();
            writer.WriteLine($"'''[[{cleanMechName}|{displayMechName.ToUpper()}]]'''");
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
