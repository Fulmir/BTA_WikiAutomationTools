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

        public static string GetLinkFromFactionId(string factionId)
        {
            return factionIdToWikiLink[factionId];
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
    }
}
