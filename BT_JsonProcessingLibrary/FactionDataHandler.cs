using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BT_JsonProcessingLibrary
{
    public static class FactionDataHandler
    {
        private static string factionsFolder = "BT Advanced Factions\\";

        private static Dictionary<string, string> factionIdToWikiLink = new Dictionary<string, string>() {
            { "AuriganDirectorate", "Aurigan Directorate"},
            { "Mercenaries", "[[List_of_Mercenary_Factions_For_SPAM|Mercenaries]]"},
            { "AuriganPirates", "[[Local Pirates|Pirates]]"},
            { "AuriganRestoration", "[[Aurigan Coalition|Aurigan Restoration (Arano)]]"},
            { "Chainelane", "[[Chainelane Isles]]"},
            { "Circinus", "[[Circinus Federation]]"},
            { "ClanDiamondShark", "[[Clan Diamond Shark]]"},
            { "ClanGhostBear", "[[Clan Ghost Bear]]"},
            { "ClanJadeFalcon", "[[Clan Jade Falcon]]"},
            { "ClanNovaCat", "[[Clan Nova Cat]]"},
            { "ClanSnowRaven", "[[Clan Snow Raven]]"},
            { "ClanWolf", "[[Clan Wolf]]"},
            { "ComStar", "[[ComStar]]"},
            { "DaneSacellum", "[[Dane Sacellum]]"},
            { "Davion", "[[Federated Suns|Federated Suns (Davion)]]"},
            { "DarkCaste", "[[Dark Caste]]"},
            { "Delphi", "[[New Delphi Compact]]"},
            { "Hanse", "[[Hanseatic League]]"},
            { "Ives", "[[St. Ives Compact]]"},
            { "JacobsonHaven", "[[Jacobson Haven]]"},
            { "JarnFolk", "[[JàrnFòlk]]"},
            { "Kurita", "[[Draconis Combine|Draconis Combine (Kurita)]]"},
            { "Liao", "[[Capellan Confederation|Capellan Confederation (Liao)]]"},
            { "Locals", "Local Government"},
            { "MagistracyOfCanopus", "[[Magistracy of Canopus]]"},
            { "MallardRepublic", "[[Mallard Republic]]"},
            { "Marian", "[[Marian Hegemony]]"},
            { "Marik", "[[Free Worlds League|Free Worlds League (Marik)]]"},
            { "Outworld", "[[Outworlds Alliance]]"},
            { "Rasalhague", "[[Free Rasalhague Republic]]"},
            { "Rim", "[[Rim Collection]]"},
            { "SanctuaryAlliance", "[[Sanctuary Alliance]]"},
            { "Steiner", "[[Lyran Commonwealth|Lyran Commonwealth (Steiner)]]"},
            { "TaurianConcordat", "[[Taurian Concordat]]"},
            { "Tortuga", "[[Tortuga Dominions]]"},
            { "WordOfBlake", "[[Word of Blake]]" }
        };

        private static ConcurrentDictionary<string, JsonDocument> factionDefsById = new ConcurrentDictionary<string, JsonDocument>();
        private static ConcurrentDictionary<string, JsonDocument> factionDefsByShortName = new ConcurrentDictionary<string, JsonDocument>();

        //private static bool DataPopulated = false;

        public static void PopulateFactionDefData(string modsFolder)
        {
            List<BasicFileData> factionDefFiles = ModJsonHandler.SearchFiles(modsFolder + factionsFolder, @"faction_*.json");

            ParallelOptions parallelOptions = new ParallelOptions();
            parallelOptions.MaxDegreeOfParallelism = 8;

            Parallel.ForEach(factionDefFiles, parallelOptions, factionDefFile =>
            {
                var temp = JsonDocument.Parse(File.ReadAllText(factionDefFile.Path), new JsonDocumentOptions() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
                if (temp.RootElement.TryGetProperty("factionID", out var factionId))
                {
                    factionDefsById[factionId.ToString()] = temp;
                }
                if (temp.RootElement.TryGetProperty("ShortName", out var factionShortName))
                {
                    factionDefsByShortName[factionShortName.ToString()] = temp;
                }
            });

            //DataPopulated = true;
        }

        public static string GetUseableFactionNameFromId(string factionId)
        {
            string factionName = factionDefsById[factionId].RootElement.GetProperty("Name").ToString();
            if (!CheckForSpecialTranslation(factionName, out factionName))
            {
                factionName = factionDefsById[factionId].RootElement.GetProperty("Name").ToString();
                if (factionName.StartsWith("the "))
                    factionName = factionName.Substring(4);
            }
            return factionName;
        }

        public static string GetUseableFactionNameFromShortName(string factionShortName)
        {
            string factionName = factionDefsByShortName[factionShortName].RootElement.GetProperty("Name").ToString();
            if (!CheckForSpecialTranslation(factionName, out factionName))
            {
                factionName = factionDefsByShortName[factionShortName].RootElement.GetProperty("Name").ToString();
                if (factionName.StartsWith("the "))
                    factionName = factionName.Substring(4);
            }
            return factionName;
        }

        public static string GetFactionIdFromShortName(string factionShortName)
        {
            return factionDefsByShortName[factionShortName].RootElement.GetProperty("factionID").ToString(); ;
        }

        public static string GetLinkFromFactionId(string factionId)
        {
            if (factionIdToWikiLink.ContainsKey(factionId))
                return factionIdToWikiLink[factionId];
            else
                return GetUseableFactionNameFromId(factionId);
        }

        public static JsonDocument GetFactionDataById(string factionId)
        {
            if (factionDefsById.TryGetValue(factionId, out JsonDocument factionData))
                return factionData;
            return null;
        }

        private static bool CheckForSpecialTranslation(string factionId, out string factionName)
        {
            switch (factionId)
            {
                case "AuriganPirates":
                    factionName = "Pirates";
                    return true;
                case "AuriganRestoration":
                    factionName = "Aurigan Restoration (Arano)";
                    return true;
                case "Davion":
                    factionName = "Federated Suns (Davion)";
                    return true;
                case "Kurita":
                    factionName = "Draconis Combine (Kurita)";
                    return true;
                case "Liao":
                    factionName = "Capellan Confederation (Liao)";
                    return true;
                case "Steiner":
                    factionName = "Lyran Commonwealth (Steiner)";
                    return true;
                case "Marik":
                    factionName = "Free Worlds League (Marik)";
                    return true;
            }
            factionName = "";
            return false;
        }

        public static void OutputIdsToNamesFile()
        {
            using StreamWriter outputFile = new("IdsToNamesForLua.txt", append: false);

            bool firstLine = true;

            List<string> sortedFactionIds = factionDefsById.Keys.ToList();
            sortedFactionIds.Sort();

            foreach (string key in sortedFactionIds)
            {

                JsonDocument currentDoc = factionDefsById[key];
                if (currentDoc != null)
                {
                    string name;
                    if (!CheckForSpecialTranslation(key, out name))
                    {
                        name = currentDoc.RootElement.GetProperty("Name").ToString();
                        if (name.StartsWith("the "))
                            name = name.Substring(4);
                    }
                    if (!BlacklistedFactionNames(name))
                    {
                        if (!firstLine)
                            outputFile.WriteLine(",");
                        outputFile.Write($"  [\"{key}\"] = \"{name}\"");
                    }
                }
                else
                    Console.Write("ERROR: No JSON for " + key);
                if (firstLine)
                    firstLine = false;
            }
        }

        private static bool BlacklistedFactionNames(string name)
        {
            switch (name)
            {
                case "Darius":
                    return true;
                case "Mercenary Review Board":
                    return true;
                case "Security Solutions, Inc.":
                    return true;
            }
            return false;
        }
    }
}
