using BT_JsonProcessingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UtilityClassLibrary;

namespace BTA_WikiTableGen
{
    internal static class MechFileSearch
    {
        static Regex BlacklistDirectories = new Regex(@"(BT Advanced Battle Armor|CustomUnits)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static Regex ClanMechDirectories = new Regex(@"BT Advanced Clan Mechs", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static Regex QuadMechDirectories = new Regex(@"BT Advanced Quad Mechs", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static Regex SanctuaryMechDirectories = new Regex(@"(BT Advanced Sanctuary Worlds Mechs|Heavy Metal Sanctuary Worlds Units)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static Regex CommunityContentDirectories = new Regex(@"Community Content", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static Regex NumbersInGroupKey = new Regex(@"(?'key'[A-Z]+)([- ]?)", RegexOptions.Compiled);

        static List<string> CustomMechVariants = new List<string>();
        internal static List<string> WhitelistMechVariants = new List<string>();
        static List<string> SeparateMechEntries = new List<string>();

        static Dictionary<string, Dictionary<string, MechStats>> InnerSphereMechs = new Dictionary<string, Dictionary<string, MechStats>>();
        static Dictionary<string, Dictionary<string, MechStats>> ClanMechs = new Dictionary<string, Dictionary<string, MechStats>>();
        static Dictionary<string, Dictionary<string, MechStats>> SanctuaryMechs = new Dictionary<string, Dictionary<string, MechStats>>();
        static Dictionary<string, Dictionary<string, MechStats>> QuadMechs = new Dictionary<string, Dictionary<string, MechStats>>();
        static Dictionary<string, Dictionary<string, MechStats>> HeroMechs = new Dictionary<string, Dictionary<string, MechStats>>();
        static Dictionary<string, Dictionary<string, MechStats>> CommunityContentMechs = new Dictionary<string, Dictionary<string, MechStats>>();
        static Dictionary<string, Dictionary<string, MechStats>> CustomMechs = new Dictionary<string, Dictionary<string, MechStats>>();

        static Dictionary<string, MechStats> allMechs = new Dictionary<string, MechStats>();

        static Dictionary<string, List<MechNameCounter>> GroupKeyToNameTracker = new Dictionary<string, List<MechNameCounter>>();

        // TODO: See how this works parallelized...
        public static void GetAllMechsFromDefs(string modsFolder)
        {
            CustomMechVariants = TextFileListProcessor.GetStringListFromFile(".\\CustomMechsList.txt");
            WhitelistMechVariants = TextFileListProcessor.GetStringListFromFile(".\\MechVariantsWhitelist.txt");
            SeparateMechEntries = TextFileListProcessor.GetStringListFromFile(".\\SeparateMechEntries.txt");

            List<BasicFileData> chassisDefs = ModJsonHandler.SearchFiles(modsFolder, "chassisdef*.json");

            foreach (BasicFileData chassisDef in chassisDefs)
            {
                if (!BlacklistDirectories.IsMatch(chassisDef.Path))
                {
                    BasicFileData mechDef = GetMechDef(chassisDef);
                    if (!File.Exists(mechDef.Path))
                        continue;

                    var tempChassisDoc = JsonDocument.Parse(new StreamReader(chassisDef.Path).ReadToEnd());

                    string variantName = tempChassisDoc.RootElement.GetProperty("VariantName").ToString().Trim();
                    string chassisName = tempChassisDoc.RootElement.GetProperty("Description").GetProperty("Name").ToString().Trim();

                    allMechs[variantName] = new MechStats(chassisName, variantName, chassisDef, mechDef);
                    if (allMechs[variantName].Blacklisted)
                        continue;

                    AddToGroupKeyToNameTracker(chassisName, variantName);

                    if (CustomMechVariants.Contains(variantName))
                        AddToNestedDictionary(variantName, ref CustomMechs);

                    else if (CommunityContentDirectories.IsMatch(chassisDef.Path))
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
            string mechGroupKey = VariantNameToGroupKey(variantName) + "_" + allMechs[variantName].MechTonnage;
            if (!target.ContainsKey(mechGroupKey))
                target[mechGroupKey] = new Dictionary<string, MechStats>();

            target[mechGroupKey][variantName] = allMechs[variantName];
        }

        private static string CleanNameForKey(string chassisName)
        {
            return chassisName.Trim().Replace("Royal", "", StringComparison.OrdinalIgnoreCase).Replace("Primitive", "", StringComparison.OrdinalIgnoreCase).Trim();
        }

        private static void AddToGroupKeyToNameTracker(string chassisName, string variantName)
        {
            chassisName = CleanNameForKey(chassisName);

            string mechGroupKey = VariantNameToGroupKey(variantName) + "_" + allMechs[variantName].MechTonnage;
            if (!GroupKeyToNameTracker.ContainsKey(mechGroupKey))
                GroupKeyToNameTracker[mechGroupKey] = new List<MechNameCounter>();

            int refCounter = GroupKeyToNameTracker[mechGroupKey].FindIndex((counter) => counter.MechName == chassisName);
            if (refCounter > -1 && GroupKeyToNameTracker[mechGroupKey][refCounter].MechName == chassisName)
            {
                GroupKeyToNameTracker[mechGroupKey][refCounter] = new MechNameCounter()
                {
                    MechName = chassisName,
                    UseCount = GroupKeyToNameTracker[mechGroupKey][refCounter].UseCount + 1
                };
                return;
            }
            GroupKeyToNameTracker[mechGroupKey].Add(new MechNameCounter()
            {
                MechName = chassisName,
                UseCount = 1
            });
        }
        private static string VariantNameToGroupKey(string variantName)
        {
            Match nameMatch = NumbersInGroupKey.Match(variantName.Trim());
            if (nameMatch.Success)
            {
                return nameMatch.Groups["key"].Value;
            }
            else
            {
                return variantName.Trim().Split(new char[] { '-', ' ' })[0];
            }
        }

        private static string TryGetNameForGroupKey(string mechGroupKey, ref Dictionary<string, Dictionary<string, MechStats>> targetDictionary)
        {
            Dictionary<string, MechNameCounter> mechNameCounters = new Dictionary<string, MechNameCounter>();
            int highestUseCount = 0;
            string highestUseName = "ERROR";

            foreach (MechStats mech in targetDictionary[mechGroupKey].Values)
            {
                string cleanedName = CleanNameForKey(mech.MechName);
                if (mechNameCounters.TryGetValue(cleanedName, out MechNameCounter nameCount))
                {
                    nameCount.UseCount++;
                    mechNameCounters[cleanedName] = nameCount;
                }
                else
                    mechNameCounters[cleanedName] = new MechNameCounter()
                    {
                        MechName = cleanedName,
                        UseCount = 1
                    };
                if (mechNameCounters[cleanedName].UseCount > highestUseCount)
                {
                    highestUseCount = mechNameCounters[cleanedName].UseCount;
                    highestUseName = cleanedName;
                }
            }
            return highestUseName;
        }
        private static string TryGetNameForGroupKey(string mechGroupKey)
        {
            MechNameCounter winningName = new MechNameCounter()
            {
                MechName = "ERROR",
                UseCount = -1
            };

            foreach (MechNameCounter counter in GroupKeyToNameTracker[mechGroupKey])
            {
                if (winningName.UseCount < counter.UseCount)
                    winningName = counter;
                else if (winningName.UseCount == counter.UseCount)
                    if (winningName.MechName.Length > counter.MechName.Length)
                        winningName.MechName = counter.MechName;
            }

            return winningName.MechName;
        }

        public static void OutputMechsToWikiTables()
        {
            StreamWriter mechTablePageWriter = new StreamWriter("MechPageTables.txt", false);

            mechTablePageWriter.WriteLine(QuirkHandler.WriteOutCommonQuirkEffects());

            mechTablePageWriter.WriteLine("==Inner Sphere Mechs==");
            mechTablePageWriter.Write(OutputDictionaryToStringByTonnage("Inner Sphere {0} Mechs", ref InnerSphereMechs, true, false));
            mechTablePageWriter.Close();

            mechTablePageWriter = new StreamWriter("MechPageTables.txt", true);
            mechTablePageWriter.WriteLine("==Clan Mechs==");
            mechTablePageWriter.Write(OutputDictionaryToStringByTonnage("Clan {0} Mechs", ref ClanMechs, true, false));
            mechTablePageWriter.Close();

            mechTablePageWriter = new StreamWriter("MechPageTables.txt", true);
            mechTablePageWriter.WriteLine("==Sanctuary Worlds Mechs==");
            mechTablePageWriter.Write(OutputDictionaryToStringByTonnage("Sanctuary Worlds {0} Mechs", ref SanctuaryMechs, true, false));
            mechTablePageWriter.Close();

            mechTablePageWriter = new StreamWriter("MechPageTables.txt", true);
            mechTablePageWriter.WriteLine("==Hero Mechs==");
            mechTablePageWriter.Write(OutputDictionaryToStringByTonnage("{0} Hero Mechs", ref HeroMechs, true, true));
            mechTablePageWriter.Close();

            mechTablePageWriter = new StreamWriter("MechPageTables.txt", true);
            mechTablePageWriter.WriteLine("==Other Mechs==");
            mechTablePageWriter.Write(OutputDictionaryToStringByTonnage("Quad Mechs", ref QuadMechs, false, false));
            mechTablePageWriter.Close();

            mechTablePageWriter = new StreamWriter("MechPageTables.txt", true);
            mechTablePageWriter.Write(OutputDictionaryToStringByTonnage("Custom Mechs", ref CustomMechs, false, false));
            mechTablePageWriter.Close();

            mechTablePageWriter = new StreamWriter("MechPageTables.txt", true);
            mechTablePageWriter.Write(OutputDictionaryToStringByTonnage("Community Content Mechs", ref CommunityContentMechs, false, true));
            mechTablePageWriter.Close();
        }

        private static string OutputDictionaryToStringByTonnage(string pluggableTitleString, ref Dictionary<string, Dictionary<string, MechStats>> targetDictionary, bool breakUpListByTonnage, bool useGlobalNamesList)
        {
            Dictionary<string, List<string>> mechNamesToMechGroupKeys = new Dictionary<string, List<string>>();

            foreach (string mechGroupKey in targetDictionary.Keys.ToList())
            {
                string primaryMechName;
                if (useGlobalNamesList)
                    primaryMechName = TryGetNameForGroupKey(mechGroupKey);
                else
                    primaryMechName = TryGetNameForGroupKey(mechGroupKey, ref targetDictionary);

                if (mechNamesToMechGroupKeys.ContainsKey(primaryMechName))
                    Console.WriteLine($"Already have value for name {primaryMechName} and mechGroupKey {mechGroupKey}. Other mechGroupKey is {mechNamesToMechGroupKeys[primaryMechName].Last()}. Adding mechGroupKey to list.");
                else
                    mechNamesToMechGroupKeys[primaryMechName] = new List<string>();

                mechNamesToMechGroupKeys[primaryMechName].Add(mechGroupKey);
            }

            List<string> sortedMechNames = mechNamesToMechGroupKeys.Keys.ToList();
            sortedMechNames.Sort();

            StringWriter LightMechWriter = null;
            StringWriter MediumMechWriter = null;
            StringWriter HeavyMechWriter = null;
            StringWriter AssaultMechWriter = null;
            StringWriter AllMechsWriter = null;

            StringBuilder LightMechOutput = null;
            StringBuilder MediumMechOutput = null;
            StringBuilder HeavyMechOutput = null;
            StringBuilder AssaultMechOutput = null;
            StringBuilder AllMechsOutput = null;


            if (breakUpListByTonnage)
            {
                LightMechOutput = new StringBuilder();
                LightMechWriter = new StringWriter(LightMechOutput);
                StartNewTableSection(string.Format(pluggableTitleString, "Light"), LightMechWriter);

                MediumMechOutput = new StringBuilder();
                MediumMechWriter = new StringWriter(MediumMechOutput);
                StartNewTableSection(string.Format(pluggableTitleString, "Medium"), MediumMechWriter);

                HeavyMechOutput = new StringBuilder();
                HeavyMechWriter = new StringWriter(HeavyMechOutput);
                StartNewTableSection(string.Format(pluggableTitleString, "Heavy"), HeavyMechWriter);

                AssaultMechOutput = new StringBuilder();
                AssaultMechWriter = new StringWriter(AssaultMechOutput);
                StartNewTableSection(string.Format(pluggableTitleString, "Assault"), AssaultMechWriter);
            }
            else
            {
                AllMechsOutput = new StringBuilder();
                AllMechsWriter = new StringWriter(AllMechsOutput);
                StartNewTableSection(string.Format(pluggableTitleString), AllMechsWriter);
            }

            foreach (string mechName in sortedMechNames)
            {
                foreach (string mechGroupKey in mechNamesToMechGroupKeys[mechName])
                {
                    List<string> sortedVariantModels = targetDictionary[mechGroupKey].Keys.ToList();
                    sortedVariantModels.Sort(new MechModelNameComparer());

                    int excludedVariantsCount = 0;
                    List<string> excludedVariants = new List<string>();
                    List<string> otherVariants = new List<string>();
                    string variantMechName = "ERROR";
                    foreach (MechStats variant in targetDictionary[mechGroupKey].Values)
                    {
                        if ((variant.VariantAssemblyRules != null &&
                            (variant.VariantAssemblyRules.Value.Exclude || !variant.VariantAssemblyRules.Value.Include))
                            || SeparateMechEntries.Contains(variant.MechModel))
                        {
                            excludedVariantsCount++;
                            excludedVariants.Add(variant.MechModel);
                        }
                        else if (variant.MechName.Contains("Primitive"))
                        {
                            excludedVariantsCount++;
                            otherVariants.Add(variant.MechModel);
                            variantMechName = variant.MechName;
                        }
                    }

                    int tonnage = targetDictionary[mechGroupKey].First().Value.MechTonnage;

                    StringWriter variantWriter;
                    if (breakUpListByTonnage)
                    {
                        if (tonnage >= 80)
                            variantWriter = AssaultMechWriter;
                        else if (tonnage >= 60)
                            variantWriter = HeavyMechWriter;
                        else if (tonnage >= 40)
                            variantWriter = MediumMechWriter;
                        else
                            variantWriter = LightMechWriter;
                    }
                    else
                        variantWriter = AllMechsWriter;

                    List<MechStats> tempMechs = new List<MechStats>();
                    List<MechStats> otherTempMechs = new List<MechStats>();
                    foreach (string variant in sortedVariantModels)
                    {
                        if (!excludedVariants.Contains(variant) && !otherVariants.Contains(variant))
                            tempMechs.Add(targetDictionary[mechGroupKey][variant]);
                        if (otherVariants.Contains(variant))
                            otherTempMechs.Add(targetDictionary[mechGroupKey][variant]);
                    }

                    foreach (string variant in excludedVariants)
                    {
                        MechStats excludedVariant = allMechs[variant];
                        StartMechTitleSection(variantWriter, excludedVariant.MechName, excludedVariant.MechModel, 1);

                        foreach (QuirkDef quirk in excludedVariant.MechQuirks.Values)
                            QuirkHandler.OutputQuirkToString(quirk, variantWriter);

                        if (excludedVariant.MechAffinity.HasValue)
                            AffinityHandler.OutputAffinityToString(excludedVariant.MechAffinity.Value, variantWriter);

                        excludedVariant.OutputStatsToString(variantWriter);
                    }
                    if (otherVariants.Count() > 0)
                    {
                        StartMechTitleSection(variantWriter, variantMechName, otherTempMechs.First().MechModel, otherVariants.Count());

                        List<QuirkDef> tempQuirkList = QuirkHandler.CompileQuirksForVariants(otherTempMechs);
                        foreach (QuirkDef quirk in tempQuirkList)
                            QuirkHandler.OutputQuirkToString(quirk, variantWriter);

                        List<AffinityDef> tempAffinityList = AffinityHandler.CompileAffinitiesForVariants(otherTempMechs);
                        foreach (AffinityDef affinity in tempAffinityList)
                            AffinityHandler.OutputAffinityToString(affinity, variantWriter);

                        foreach (MechStats mechVariant in otherTempMechs)
                            mechVariant.OutputStatsToString(variantWriter);
                    }
                    if (sortedVariantModels.Count - excludedVariantsCount >= 1)
                    {
                        StartMechTitleSection(variantWriter, mechName, tempMechs.First().MechModel, sortedVariantModels.Count - excludedVariantsCount);

                        List<QuirkDef> tempQuirkList = QuirkHandler.CompileQuirksForVariants(tempMechs);
                        foreach (QuirkDef quirk in tempQuirkList)
                            QuirkHandler.OutputQuirkToString(quirk, variantWriter);

                        List<AffinityDef> tempAffinityList = AffinityHandler.CompileAffinitiesForVariants(tempMechs);
                        foreach (AffinityDef affinity in tempAffinityList)
                            AffinityHandler.OutputAffinityToString(affinity, variantWriter);

                        foreach (MechStats mechVariant in tempMechs)
                            mechVariant.OutputStatsToString(variantWriter);
                    }
                }
            }

            if (breakUpListByTonnage)
            {
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
            else
            {
                CloseTableSection(AllMechsWriter);
                AllMechsWriter.Close();
                return AllMechsOutput.ToString();
            }


        }

        private static void StartNewTableSection(string section, StringWriter tableWriter)
        {
            tableWriter.WriteLine($"==={section}===");
            tableWriter.WriteLine("<div class=\"mw-collapsible\">");
            tableWriter.WriteLine();
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
            tableWriter.WriteLine("! colspan=\"8\" |Hardpoints");
            tableWriter.WriteLine("! colspan=\"3\" |Engine");
            tableWriter.WriteLine("! colspan=\"3\" |Components");
            tableWriter.WriteLine("! colspan=\"2\" |Free Tonnage");
            tableWriter.WriteLine("! colspan=\"3\" |Speed");
            tableWriter.WriteLine("|-");
            tableWriter.WriteLine("!<small>Chassis</small>");
            tableWriter.WriteLine("!<small>Model</small>");
            tableWriter.WriteLine("!<small>Mass</small>");
            tableWriter.WriteLine("!<small>Role</small>");
            tableWriter.WriteLine("!<small>Ba</small>");
            tableWriter.WriteLine("!<small>En</small>");
            tableWriter.WriteLine("!<small>Mi</small>");
            tableWriter.WriteLine("!<small>Ar</small>");
            tableWriter.WriteLine("!<small>Su</small>");
            tableWriter.WriteLine("!<small>O</small>");
            tableWriter.WriteLine("!<small>Bo</small>");
            tableWriter.WriteLine("!<small>Me</small>");
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

        private static void StartMechTitleSection(StringWriter writer, string mechName, string firstVariantName, int variantCount)
        {
            string cleanMechName = mechName.Replace("Prototype", "").Trim().Replace(' ', '_');
            string imageName = mechName.Replace("Royal", "", StringComparison.OrdinalIgnoreCase).Replace("Primitive", "", StringComparison.OrdinalIgnoreCase).Trim().Replace(' ', '_').Replace("'", "");

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
