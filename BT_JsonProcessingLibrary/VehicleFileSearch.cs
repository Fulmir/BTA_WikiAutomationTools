using BT_JsonProcessingLibrary;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UtilityClassLibrary;
using UtilityClassLibrary.WikiLinkOverrides;

namespace BT_JsonProcessingLibrary
{
    public static class VehicleFileSearch
    {
        private static Regex BlacklistDirectories = new Regex(@"(CustomUnits|MonsterMashup)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static Regex ClanVehicleDirectories = new Regex(@"BT Advanced Clan Tanks", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static Regex SanctuaryVehicleDirectories = new Regex(@"BT Advanced Sanctuary Worlds Tanks and Turrets", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static Regex CommunityContentDirectories = new Regex(@"Community Content", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static ConcurrentDictionary<string, Dictionary<string, VehicleStats>> InnerSphereVehicles = new ConcurrentDictionary<string, Dictionary<string, VehicleStats>>();
        private static ConcurrentDictionary<string, Dictionary<string, VehicleStats>> ClanVehicles = new ConcurrentDictionary<string, Dictionary<string, VehicleStats>>();
        private static ConcurrentDictionary<string, Dictionary<string, VehicleStats>> SanctuaryVehicles = new ConcurrentDictionary<string, Dictionary<string, VehicleStats>>();
        private static ConcurrentDictionary<string, Dictionary<string, VehicleStats>> AirSupportFighters = new ConcurrentDictionary<string, Dictionary<string, VehicleStats>>();
        private static ConcurrentDictionary<string, Dictionary<string, VehicleStats>> VtolVehicles = new ConcurrentDictionary<string, Dictionary<string, VehicleStats>>();
        private static ConcurrentDictionary<string, Dictionary<string, VehicleStats>> CommunityContentVehicles = new ConcurrentDictionary<string, Dictionary<string, VehicleStats>>();

        private static ConcurrentDictionary<string, Dictionary<string, VehicleStats>> PlayerControllableVehicles = new ConcurrentDictionary<string, Dictionary<string, VehicleStats>>();

        private static Dictionary<string, BasicFileData> chassisDefIndex = new Dictionary<string, BasicFileData>();

        private static ConcurrentDictionary<string, VehicleStats> allVehicles = new ConcurrentDictionary<string, VehicleStats>();

        private static List<string> blacklistedChassisIds = new List<string>();

        public static void GetAllVehiclesFromDefs(string modsFolder)
        {
            StreamReader blacklistReader = new StreamReader(".\\VehicleClassificationFiles\\VehicleBlacklistById.txt");

            while(!blacklistReader.EndOfStream)
            {
                blacklistedChassisIds.Add(blacklistReader.ReadLine());
            }

            ParallelOptions parallelOptions = new ParallelOptions();
            parallelOptions.MaxDegreeOfParallelism = 8;

            List<BasicFileData> vehicleChassisDefs = ModJsonHandler.SearchFiles(modsFolder, "vehiclechassisdef_*.json");

            foreach (BasicFileData vehicleChassisDefFile in vehicleChassisDefs)
            {
                string chassisId = vehicleChassisDefFile.FileName.Replace(".json", "");
                if (!chassisDefIndex.ContainsKey(chassisId))
                    chassisDefIndex.Add(chassisId, vehicleChassisDefFile);
                else
                    Logging.AddLogToQueue($"Duplicate VehicleChassisDef found for {chassisId}", LogLevel.Error, LogCategories.VehicleDefs);
            }

            List<BasicFileData> vehicleDefs = ModJsonHandler.SearchFiles(modsFolder, "vehicledef_*.json");

            Parallel.ForEach(vehicleDefs, parallelOptions, vehicleDef =>
            {
                if (!BlacklistDirectories.IsMatch(vehicleDef.Path))
                {
                    JsonDocument vehicleDefJson = JsonDocument.Parse(new StreamReader(vehicleDef.Path).ReadToEnd());
                    BasicFileData vehicleChassisDef = chassisDefIndex[ModJsonHandler.GetChassisDefId(vehicleDefJson, vehicleDef)];
                    if (!File.Exists(vehicleDef.Path))
                        return;

                    JsonDocument vehicleChassisJson = JsonDocument.Parse(new StreamReader(vehicleChassisDef.Path).ReadToEnd());

                    string vehicleId = vehicleDefJson.RootElement.GetProperty("Description").GetProperty("Id").ToString().Trim();
                    string chassisName = ModJsonHandler.GetNameFromJsonDoc(vehicleChassisJson);
                    string vehicleName = ModJsonHandler.GetUiNameFromJsonDoc(vehicleDefJson);

                    allVehicles[vehicleId] = new VehicleStats(vehicleChassisDef, vehicleDef);
                    if (allVehicles[vehicleId].Blacklisted || blacklistedChassisIds.Contains(vehicleId))
                        return;

                    if (allVehicles[vehicleId].VehicleMoveType == VehicleMovementTypes.Jet)
                    {
                        allVehicles[vehicleId].WikiTags.Add("Wiki_AerospaceFighter");
                        AddToNestedDictionary(chassisName, vehicleId, ref AirSupportFighters);
                    }

                    else if (CommunityContentDirectories.IsMatch(vehicleChassisDef.Path))
                    {
                        allVehicles[vehicleId].WikiTags.Add("Wiki_CommunityContent");
                        AddToNestedDictionary(chassisName, vehicleId, ref CommunityContentVehicles);
                    }

                    else if (allVehicles[vehicleId].VehicleMoveType == VehicleMovementTypes.VTOL)
                    {
                        allVehicles[vehicleId].WikiTags.Add("Wiki_VTOL");
                        AddToNestedDictionary(chassisName, vehicleId, ref VtolVehicles);
                    }

                    else if (ClanVehicleDirectories.IsMatch(vehicleChassisDef.Path))
                    {
                        allVehicles[vehicleId].WikiTags.Add("Wiki_ClanVehicle");
                        AddToNestedDictionary(chassisName, vehicleId, ref ClanVehicles);
                    }

                    else if (SanctuaryVehicleDirectories.IsMatch(vehicleChassisDef.Path))
                    {
                        allVehicles[vehicleId].WikiTags.Add("Wiki_SanctuaryAlliance");
                        AddToNestedDictionary(chassisName, vehicleId, ref SanctuaryVehicles);
                    }

                    else
                    {
                        allVehicles[vehicleId].WikiTags.Add("Wiki_InnerSphere");
                        AddToNestedDictionary(chassisName, vehicleId, ref InnerSphereVehicles);
                    }

                    if (allVehicles[vehicleId].PlayerControllable)
                    {
                        allVehicles[vehicleId].WikiTags.Add("Wiki_PlayerControl");
                        AddToNestedDictionary(chassisName, vehicleId, ref PlayerControllableVehicles);
                    }
                }
            });
        }

        private static void AddToNestedDictionary(string chassisName, string vehicleId, ref ConcurrentDictionary<string, Dictionary<string, VehicleStats>> target)
        {
            if (!target.ContainsKey(chassisName))
                target[chassisName] = new Dictionary<string, VehicleStats>();

            target[chassisName][vehicleId] = allVehicles[vehicleId];
        }

        public static void OutputVehiclesToWikiTables()
        {
            StreamWriter vehicleTablePageWriter = new StreamWriter("VehiclePageTables.txt", false);

            vehicleTablePageWriter.WriteLine("==Inner Sphere Vehicles==");
            vehicleTablePageWriter.Write(OutputDictionaryToStringByTonnage("===Inner Sphere {0} Vehicles===", ref InnerSphereVehicles, true, true));
            vehicleTablePageWriter.Close();

            vehicleTablePageWriter = new StreamWriter("VehiclePageTables.txt", true);
            vehicleTablePageWriter.WriteLine("==Clan Vehicles==");
            vehicleTablePageWriter.Write(OutputDictionaryToStringByTonnage("===Clan {0} Vehicles===", ref ClanVehicles, true, true));
            vehicleTablePageWriter.Close();

            vehicleTablePageWriter = new StreamWriter("VehiclePageTables.txt", true);
            vehicleTablePageWriter.WriteLine("==Sanctuary Worlds Vehicles==");
            vehicleTablePageWriter.Write(OutputDictionaryToStringByTonnage("===Sanctuary Worlds {0} Vehicles===", ref SanctuaryVehicles, true, true));
            vehicleTablePageWriter.Close();

            vehicleTablePageWriter = new StreamWriter("VehiclePageTables.txt", true);
            vehicleTablePageWriter.Write(OutputDictionaryToStringByTonnage("==VTOL Vehicles==", ref VtolVehicles, false, true));
            vehicleTablePageWriter.Close();

            vehicleTablePageWriter = new StreamWriter("VehiclePageTables.txt", true);
            vehicleTablePageWriter.Write(OutputDictionaryToStringByTonnage("==Aerospace Fighters==", ref AirSupportFighters, false, false));
            vehicleTablePageWriter.Close();

            vehicleTablePageWriter = new StreamWriter("VehiclePageTables.txt", true);
            vehicleTablePageWriter.Write(OutputDictionaryToStringByTonnage("==Community Content Vehicles==", ref CommunityContentVehicles, false, true));
            vehicleTablePageWriter.Close();
        }

        private static string OutputDictionaryToStringByTonnage(string pluggableTitleString, ref ConcurrentDictionary<string, Dictionary<string, VehicleStats>> targetDictionary, bool breakUpListByTonnage, bool hasNormalEngineData)
        {
            List<string> sortedVehicleChassisNames = targetDictionary.Keys.ToList();
            sortedVehicleChassisNames.Sort();

            StringWriter LightVehicleWriter = null;
            StringWriter MediumVehicleWriter = null;
            StringWriter HeavyVehicleWriter = null;
            StringWriter AssaultVehicleWriter = null;
            StringWriter AllVehiclesWriter = null;

            StringBuilder LightVehicleOutput = null;
            StringBuilder MediumVehicleOutput = null;
            StringBuilder HeavyVehicleOutput = null;
            StringBuilder AssaultVehicleOutput = null;
            StringBuilder AllVehiclesOutput = null;


            if (breakUpListByTonnage)
            {
                LightVehicleOutput = new StringBuilder();
                LightVehicleWriter = new StringWriter(LightVehicleOutput);
                StartNewTableSection(string.Format(pluggableTitleString, "Light"), LightVehicleWriter);

                MediumVehicleOutput = new StringBuilder();
                MediumVehicleWriter = new StringWriter(MediumVehicleOutput);
                StartNewTableSection(string.Format(pluggableTitleString, "Medium"), MediumVehicleWriter);

                HeavyVehicleOutput = new StringBuilder();
                HeavyVehicleWriter = new StringWriter(HeavyVehicleOutput);
                StartNewTableSection(string.Format(pluggableTitleString, "Heavy"), HeavyVehicleWriter);

                AssaultVehicleOutput = new StringBuilder();
                AssaultVehicleWriter = new StringWriter(AssaultVehicleOutput);
                StartNewTableSection(string.Format(pluggableTitleString, "Assault"), AssaultVehicleWriter);
            }
            else
            {
                AllVehiclesOutput = new StringBuilder();
                AllVehiclesWriter = new StringWriter(AllVehiclesOutput);
                StartNewTableSection(string.Format(pluggableTitleString), AllVehiclesWriter);
            }

            foreach (string chassisName in sortedVehicleChassisNames)
            {
                int tonnage = targetDictionary[chassisName].First().Value.VehicleWeight;

                StringWriter variantWriter;
                if (breakUpListByTonnage)
                {
                    if (tonnage >= 80)
                        variantWriter = AssaultVehicleWriter;
                    else if (tonnage >= 60)
                        variantWriter = HeavyVehicleWriter;
                    else if (tonnage >= 40)
                        variantWriter = MediumVehicleWriter;
                    else
                        variantWriter = LightVehicleWriter;
                }
                else
                    variantWriter = AllVehiclesWriter;

                List<string> sortedVehicleIds = targetDictionary[chassisName].Keys.ToList();
                sortedVehicleIds.Sort();

                string firstPlayerControllableVehicle = "";
                foreach (string vehicleId in sortedVehicleIds)
                {
                    if (targetDictionary[chassisName][vehicleId].PlayerControllable)
                        firstPlayerControllableVehicle = targetDictionary[chassisName][vehicleId].VehicleUiName;
                }

                StartVehicleTitleSection(variantWriter, chassisName, firstPlayerControllableVehicle, targetDictionary[chassisName].Count);

                foreach (string vehicleId in sortedVehicleIds)
                {
                    targetDictionary[chassisName][vehicleId].OutputStatsToString(variantWriter);
                }
            }

            if (breakUpListByTonnage)
            {
                CloseTableSection(LightVehicleWriter);
                LightVehicleWriter.Close();

                CloseTableSection(MediumVehicleWriter);
                MediumVehicleWriter.Close();

                CloseTableSection(HeavyVehicleWriter);
                HeavyVehicleWriter.Close();

                CloseTableSection(AssaultVehicleWriter);
                AssaultVehicleWriter.Close();
                return LightVehicleOutput.ToString() + MediumVehicleOutput.ToString() + HeavyVehicleOutput.ToString() + AssaultVehicleOutput.ToString();
            }
            else
            {
                CloseTableSection(AllVehiclesWriter);
                AllVehiclesWriter.Close();
                return AllVehiclesOutput.ToString();
            }


        }

        public static void PrintVehiclePagesToFiles()
        {
            string wikiPagesFolder = ".\\VehicleWikiPages\\";

            Directory.Delete(wikiPagesFolder, true);
            Directory.CreateDirectory(wikiPagesFolder);

            foreach(string vehicleChassisName in PlayerControllableVehicles.Keys)
            {
                List<string> vehicleNameCheckList = new List<string>();

                string linkName = vehicleChassisName;
                if (VehicleLinkOverrides.TryGetLinkOverride(vehicleChassisName, out string linkNameOverride))
                {
                    linkName = linkNameOverride;
                }

                StreamWriter vehiclePageWriter = new StreamWriter(wikiPagesFolder + linkName + ".wiki", false);

                vehiclePageWriter.WriteLine("<tabs>");

                List<string> sortedVehicleIds = PlayerControllableVehicles[vehicleChassisName].Keys.ToList();
                sortedVehicleIds.Sort(new ReferentialStringComparer<VehicleStats>(PlayerControllableVehicles[vehicleChassisName], "VehicleUiName", new List<string>()));

                List<string> aggregateWikiTags = new List<string>();

                foreach(string vehicleId in sortedVehicleIds)
                {
                    PlayerControllableVehicles[vehicleChassisName][vehicleId].OutputVehicleToPageTab(vehiclePageWriter);
                    aggregateWikiTags.AddRange(PlayerControllableVehicles[vehicleChassisName][vehicleId].WikiTags);

                    if (vehicleNameCheckList.Contains(PlayerControllableVehicles[vehicleChassisName][vehicleId].VehicleUiName))
                        Logging.AddLogToQueue($"Vehicle name duplicated for id: {vehicleId}", LogLevel.Warning, LogCategories.VehicleDefs);
                    vehicleNameCheckList.Add(PlayerControllableVehicles[vehicleChassisName][vehicleId].VehicleUiName);
                }

                vehiclePageWriter.WriteLine("</tabs>");
                vehiclePageWriter.WriteLine();
                vehiclePageWriter.WriteLine();

                GetWikiCategoriesForTags(aggregateWikiTags, vehiclePageWriter);

                vehiclePageWriter.Close();
            }
        }

        public static void GetWikiCategoriesForTags(List<string> tags, TextWriter writer)
        {
            if (tags.Contains("Wiki_AerospaceFighter"))
                writer.WriteLine($"[[Category:Aerospace Fighters]]");

            if (tags.Contains("Wiki_CommunityContent"))
                writer.WriteLine($"[[Category:Community Content]]");

            if (tags.Contains("Wiki_VTOL"))
                writer.WriteLine($"[[Category:VTOLs]]");

            if (tags.Contains("Wiki_ClanVehicle"))
                writer.WriteLine($"[[Category:Clan Vehicles]]");

            if (tags.Contains("Wiki_SanctuaryAlliance"))
                writer.WriteLine($"[[Category:Sanctuary Alliance Vehicles]]");

            if (tags.Contains("Wiki_InnerSphere"))
                writer.WriteLine($"[[Category:Inner Sphere Vehicles]]");

            if (tags.Contains("Wiki_PlayerControl"))
                writer.WriteLine($"[[Category:Controllable Vehicles]]");

        }

        private static void StartNewTableSection(string section, StringWriter tableWriter, bool hasNormalEngineData = true)
        {
            tableWriter.WriteLine($"{section}");
            tableWriter.WriteLine("<div class=\"mw-collapsible\">");
            tableWriter.WriteLine();
            tableWriter.WriteLine();
            WriteTableStart(tableWriter, hasNormalEngineData);
        }

        private static void CloseTableSection(StringWriter tableWriter)
        {
            tableWriter.WriteLine("|}");
            tableWriter.WriteLine();
            tableWriter.WriteLine("</div>");
            tableWriter.WriteLine();
            tableWriter.WriteLine();
        }

        private static void WriteTableStart(StringWriter tableWriter, bool hasNormalEngineData)
        {
            tableWriter.WriteLine("{| class=\"wikitable sortable\" style=\"text-align: center;\"");
            tableWriter.WriteLine("! colspan=\"4\" |Vehicle");
            tableWriter.WriteLine($"! colspan=\"{(hasNormalEngineData ? 4 : 1)}\" |Movement");
            tableWriter.WriteLine("! colspan=\"2\" |Defense");
            tableWriter.WriteLine("! colspan=\"5\" |Armor/Structure by Location");
            tableWriter.WriteLine("! colspan=\"3\" |Components");
            tableWriter.WriteLine("|-");
            tableWriter.WriteLine("!<small>Chassis</small>");
            tableWriter.WriteLine("!<small>Variant</small>");
            tableWriter.WriteLine("!<small>Mass</small>");
            tableWriter.WriteLine("!<small>Player Controllable</small>");
            tableWriter.WriteLine("!<small>Propulsion</small>");
            if (hasNormalEngineData)
            {
                tableWriter.WriteLine("!<small>Speed</small>");
                tableWriter.WriteLine("!<small>Engine Core</small>");
                tableWriter.WriteLine("!<small>Engine Type</small>");
            }
            tableWriter.WriteLine("!<small>Armor Total</small>");
            tableWriter.WriteLine("!<small>Structure Total</small>");
            tableWriter.WriteLine("!<small>Front</small>");
            tableWriter.WriteLine("!<small>Left</small>");
            tableWriter.WriteLine("!<small>Right</small>");
            tableWriter.WriteLine("!<small>Rear</small>");
            tableWriter.WriteLine("!<small>Turret</small>");
            tableWriter.WriteLine("!<small>Gear</small>");
            tableWriter.WriteLine("!<small>Weapons</small>");
            tableWriter.WriteLine("!<small>Ammo</small>");
            tableWriter.WriteLine("|-");
        }

        private static void StartVehicleTitleSection(StringWriter writer, string chassisName, string firstVehicleName, int vehicleTypeCount)
        {
            string imageName = chassisName.Replace(" ", "").Replace("(C)", "").Trim();

            string linkName = chassisName;
            if(VehicleLinkOverrides.TryGetLinkOverride(chassisName, out string linkNameOverride))
            {
                linkName = linkNameOverride;
            }

            writer.WriteLine($"|rowspan=\"{vehicleTypeCount}\"|");
            writer.WriteLine($"[[File:Vehicle_{imageName}.png|125px|border|center]]");
            writer.WriteLine();
            if(firstVehicleName == "")
                writer.WriteLine($"'''{chassisName.ToUpper()}'''");
            else
                writer.WriteLine($"'''[[{linkName}#{firstVehicleName}|{chassisName.ToUpper()}]]'''");
            writer.WriteLine();
        }
    }
}
