using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UtilityClassLibrary;

namespace BT_JsonProcessingLibrary
{
    public static class FactionDataHandler
    {
        private static string factionsFolder = "BT Advanced Factions\\";
        private static string spamFolder = "SoldiersPiratesAssassinsMercs\\";

        private static Dictionary<string, string> factionIdToWikiLink = new Dictionary<string, string>() {
            { "AuriganDirectorate", "Aurigan Directorate" },
            { "AuriganPirates", "[[Local Pirates|Pirates]]" },
            { "AuriganRestoration", "[[Aurigan Coalition|Aurigan Restoration (Arano)]]" },
            { "Chainelane", "[[Chainelane Isles]]" },
            { "Circinus", "[[Circinus Federation]]" },
            { "ClanDiamondShark", "[[Clan Diamond Shark]]" },
            { "ClanGhostBear", "[[Clan Ghost Bear]]" },
            { "ClanJadeFalcon", "[[Clan Jade Falcon]]" },
            { "ClanNovaCat", "[[Clan Nova Cat]]" },
            { "ClanSnowRaven", "[[Clan Snow Raven]]" },
            { "ClanWolf", "[[Clan Wolf]]" },
            { "ComStar", "[[ComStar]]" },
            { "DaneSacellum", "[[Dane Sacellum]]" },
            { "Davion", "[[Federated Suns|Federated Suns (Davion)]]" },
            { "DarkCaste", "[[Dark Caste]]" },
            { "Delphi", "[[New Delphi Compact]]" },
            { "Hanse", "[[Hanseatic League]]" },
            { "Illyrian", "[[Illyrian Palatinate]]" },
            { "Ives", "[[St. Ives Compact]]" },
            { "JacobsonHaven", "[[Jacobson Haven]]" },
            { "JarnFolk", "[[JàrnFòlk]]" },
            { "Kurita", "[[Draconis Combine|Draconis Combine (Kurita)]]" },
            { "Liao", "[[Capellan Confederation|Capellan Confederation (Liao)]]" },
            { "Locals", "Local Government" },
            { "Lothian", "[[Lothian League]]" },
            { "MagistracyOfCanopus", "[[Magistracy of Canopus]]" },
            { "MallardRepublic", "[[Mallard Republic]]" },
            { "Marian", "[[Marian Hegemony]]" },
            { "Marik", "[[Free Worlds League|Free Worlds League (Marik)]]" },
            { "Mercenaries", "[[List_of_Mercenary_Factions_For_SPAM|Mercenaries]]" },
            { "Outworld", "[[Outworlds Alliance]]" },
            { "Rasalhague", "[[Free Rasalhague Republic]]" },
            { "Rim", "[[Rim Collection]]" },
            { "SanctuaryAlliance", "[[Sanctuary Alliance]]" },
            { "Steiner", "[[Lyran Commonwealth|Lyran Commonwealth (Steiner)]]" },
            { "TaurianConcordat", "[[Taurian Concordat]]" },
            { "Tortuga", "[[Tortuga Dominions]]" },
            { "WordOfBlake", "[[Word of Blake]]" }
        };

        private static ConcurrentDictionary<string, FactionData> factionDefsById = new ConcurrentDictionary<string, FactionData>();
        private static ConcurrentDictionary<string, FactionData> factionDefsByShortName = new ConcurrentDictionary<string, FactionData>();
        private static Dictionary<string, string> spamFactionMappingByIds = new Dictionary<string, string>();

        private static JsonDocument SpamConfigJson;

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
                    factionDefsById[factionId.ToString()] = new FactionData(temp);
                }
                if (temp.RootElement.TryGetProperty("ShortName", out var factionShortName))
                {
                    factionDefsByShortName[factionShortName.ToString()] = new FactionData(temp);
                }
            });

            SpamConfigJson = JsonDocument.Parse(File.ReadAllText(modsFolder + spamFolder + "mod.json"), new JsonDocumentOptions() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });

            OutputSpamFactionsToParentsTranslation();
        }

        public static string GetUseableFactionNameFromId(string factionId)
        {
            string factionName = factionDefsById[factionId].Name;
            if (!CheckForSpecialTranslation(factionName, out factionName))
            {
                factionName = factionDefsById[factionId].Name;
                if (factionName.StartsWith("the "))
                    factionName = factionName.Substring(4);
            }
            return factionName;
        }

        public static string GetUseableFactionNameFromShortName(string factionShortName)
        {
            string factionName = factionDefsByShortName[factionShortName].Name;
            if (!CheckForSpecialTranslation(factionName, out factionName))
            {
                factionName = factionDefsByShortName[factionShortName].Name;
                if (factionName.StartsWith("the "))
                    factionName = factionName.Substring(4);
            }
            return factionName;
        }

        public static string GetFactionIdFromShortName(string factionShortName)
        {
            return factionDefsByShortName[factionShortName].FactionId;
        }

        public static string GetLinkFromFactionId(string factionId)
        {
            if (factionIdToWikiLink.ContainsKey(factionId))
                return factionIdToWikiLink[factionId];
            else
                return GetUseableFactionNameFromId(factionId);
        }

        public static bool TryGetFactionById(string factionId, out FactionData factionDef)
        {
            return factionDefsById.TryGetValue(factionId, out factionDef);
        }

        public static bool TryGetParentFactionForId(string factionId, out string parentFactionId)
        {
            return spamFactionMappingByIds.TryGetValue(factionId, out parentFactionId);
        }

        public static void TagsListToFactionsSection(List<string> tagsList, TextWriter writer)
        {
            Dictionary<string, List<string>> FactionEntries = new Dictionary<string, List<string>>();

            foreach (string tag in tagsList)
            {
                if(TryGetFactionById(tag, out FactionData factionDef))
                {
                    if(TryGetParentFactionForId(tag, out string parentFactionId))
                    {
                        if (!FactionEntries.ContainsKey(parentFactionId))
                            FactionEntries[parentFactionId] = new List<string>();
                        FactionEntries[parentFactionId].Add(tag);
                    } else
                    {
                        if (!FactionEntries.ContainsKey(tag))
                            FactionEntries[tag] = new List<string>();
                        FactionEntries[tag].Add(tag);
                    }
                }
            }

            List<string> sortedParentTags = FactionEntries.Keys.ToList();
            sortedParentTags.Sort(new ReferentialStringComparer<FactionData>(factionDefsById, "Name", new List<string>()));

            writer.WriteLine("<div class=\"toccolours mw-collapsible\" style=\"width:400px\">");
            writer.WriteLine("<div style=\"font-weight:bold;line-height:1.6;>Factions</div>");
            writer.WriteLine("<div class=\"mw-collapsible-content\"><ul>");

            foreach(string parentTag in sortedParentTags)
            {
                if (FactionEntries[parentTag].Count() > 1)
                    writer.Write("<div class=\"mw-collapsible mw-collapsed\">");

                writer.Write("<li>");
                if (factionIdToWikiLink.TryGetValue(parentTag, out string wikiLink))
                    writer.WriteLine(wikiLink);
                writer.Write("</li>");

                if (FactionEntries[parentTag].Count() > 1)
                {
                    writer.Write("<div class=\"mw-collapsible-content\">");
                    writer.Write("<ul>");
                    foreach (string factionId in FactionEntries[parentTag])
                        writer.WriteLine($"<li>{factionDefsById[factionId].Name}</li>");
                    writer.Write("</ul>");
                    writer.Write("</div>");
                    writer.Write("</div>");
                }
            }

            writer.WriteLine("</ul></div></div>");
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
                if (TryGetFactionById(key, out FactionData currentFaction))
                {
                    string name;
                    if (!CheckForSpecialTranslation(key, out name))
                    {
                        name = currentFaction.Name;
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

        private static void OutputSpamFactionsToParentsTranslation()
        {
            JsonElement factionJson = SpamConfigJson.RootElement.GetProperty("Settings");

            var factionConfigs = factionJson.GetProperty("AlternateFactionConfigs").EnumerateObject();

            foreach (var factionConfig in factionConfigs)
            {
                string baseFactionName = factionConfig.Name;
                spamFactionMappingByIds[baseFactionName] = baseFactionName;

                var subFactions = factionConfig.Value.GetProperty("AlternateOpforWeights").EnumerateArray();
                foreach (var subFaction in subFactions)
                {
                    string subFactionName = subFaction.GetProperty("FactionName").ToString();
                    spamFactionMappingByIds[subFactionName] = baseFactionName;
                }
            }

            var mercConfigs = factionJson.GetProperty("MercFactionConfigs").EnumerateObject();

            spamFactionMappingByIds["Mercenaries"] = "Mercenaries";

            foreach (var mercConfig in mercConfigs)
            {
                string mercCompanyId = mercConfig.Name;
                spamFactionMappingByIds[mercCompanyId] = "Mercenaries";
            }
        }
    }
}
