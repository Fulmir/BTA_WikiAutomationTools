using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BTA_SpamFactionBuilder
{
    internal class FactionDataProcessor
    {
        string factionsFolder = "BT Advanced Factions\\";
        string spamFolder = "SoldiersPiratesAssassinsMercs\\";
        string worldDefsFolder = "InnerSphereMap\\StarSystems\\";
        string modsFolder = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\BATTLETECH\\Mods\\";

        private readonly Dictionary<string, JsonDocument> factionDefs;
        private readonly Dictionary<string, JsonDocument> planetDefs;
        private readonly JsonDocument spamConfig;

        public FactionDataProcessor()
        {
            Console.WriteLine("File path to mods folder? eg: C:\\Games\\...");
            Console.WriteLine($"If blank defaults to: {modsFolder}");
            Console.Write(":");

            string filePath = Console.ReadLine() ?? "";
            modsFolder = string.IsNullOrEmpty(filePath) ? modsFolder : filePath;

            Console.WriteLine("");


            spamConfig = JsonDocument.Parse(File.ReadAllText(modsFolder + spamFolder + "mod.json"), new JsonDocumentOptions() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });

            List<string> factionDefFiles = SearchFiles(modsFolder + factionsFolder, @"faction_*.json");

            factionDefs = new Dictionary<string, JsonDocument>();

            foreach (string factionDefFile in factionDefFiles)
            {
                var temp = JsonDocument.Parse(File.ReadAllText(factionDefFile), new JsonDocumentOptions() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
                if (temp.RootElement.TryGetProperty("factionID", out var factionId))
                {
                    factionDefs[factionId.ToString()] = temp;
                }
            }

            var MercPlanets = spamConfig.RootElement.GetProperty("Settings").GetProperty("PlanetFactionConfigs").EnumerateObject();
            planetDefs = new Dictionary<string, JsonDocument>();

            foreach (JsonProperty element in MercPlanets)
            {
                planetDefs.Add(element.Name, JsonDocument.Parse(File.ReadAllText($"{modsFolder + worldDefsFolder + element.Name}.json"), new JsonDocumentOptions() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip }));
            }
        }
        public List<string> SearchFiles(string startingPath, string filePattern)
        {
            string? dirName = Path.GetDirectoryName(startingPath);
            string? fileName = Path.GetFileName(startingPath);

            var files = from file in Directory.EnumerateFiles(
                            string.IsNullOrWhiteSpace(dirName) ? "." : dirName,
                            filePattern,
                            SearchOption.AllDirectories)
                        select file;

            return files.ToList();
        }

        public void OutputIdsToNamesFile()
        {
            using StreamWriter outputFile = new("IdsToNamesForLua.txt", append: false);

            bool firstLine = true;

            foreach (string key in factionDefs.Keys)
            {

                JsonDocument currentDoc = factionDefs[key];
                if (currentDoc != null)
                {
                    string name;
                    if (!CheckForSpecialTranslation(key, out name))
                    {
                        name = currentDoc.RootElement.GetProperty("Name").ToString();
                        if (name.StartsWith("the "))
                            name = name.Substring(4);
                    }
                    if (!BlacklistedNames(name))
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

        private bool CheckForSpecialTranslation(string factionId, out string factionName)
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

        private bool BlacklistedNames(string name)
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

        public void OutputSpamFactionsToParentsTranslation()
        {
            JsonElement factionJson = spamConfig.RootElement.GetProperty("Settings");
            using StreamWriter outputFile = new("SpamParentIdsDictionary.txt", append: false);

            var factionConfigs = factionJson.GetProperty("AlternateFactionConfigs").EnumerateObject();

            bool firstLine = true;

            foreach (var factionConfig in factionConfigs)
            {
                if (!firstLine)
                    outputFile.WriteLine(",");

                string baseFactionName = factionConfig.Name;
                outputFile.Write($"  [\"{baseFactionName}\"] = \"{baseFactionName}\"");
                if (firstLine)
                    firstLine = false;

                var subFactions = factionConfig.Value.GetProperty("AlternateOpforWeights").EnumerateArray();
                foreach (var subFaction in subFactions)
                {
                    if (!firstLine)
                        outputFile.WriteLine(",");
                    string subFactionName = subFaction.GetProperty("FactionName").ToString();
                    outputFile.Write($"  [\"{subFactionName}\"] = \"{baseFactionName}\"");
                }
            }

            var mercConfigs = factionJson.GetProperty("MercFactionConfigs").EnumerateObject();

            if (!firstLine)
                outputFile.WriteLine(",");

            outputFile.Write($"  [\"{"Mercenaries"}\"] = 'Mercenaries'");

            foreach (var mercConfig in mercConfigs)
            {
                if (!firstLine)
                    outputFile.WriteLine(",");

                string mercCompanyId = mercConfig.Name;
                outputFile.Write($"  [\"{mercCompanyId}\"] = 'Mercenaries'");
                if (firstLine)
                    firstLine = false;
            }
        }

        public void OutputMercFactionInfo()
        {
            JsonElement factionJson = spamConfig.RootElement.GetProperty("Settings");
            var mercConfigs = factionJson.GetProperty("MercFactionConfigs").EnumerateObject();

            Dictionary<string, List<string>> mercPlanetMap = MapPlanetsToMercsByIds();

            Dictionary<string, List<SpamFactionData>> mercFactions = new Dictionary<string, List<SpamFactionData>>();

            foreach (var mercConfig in mercConfigs)
            {
                JsonDocument mercFactionDef = factionDefs[mercConfig.Name];
                SpamFactionData tempMercFaction = new SpamFactionData();

                if (mercFactionDef != null)
                {
                    tempMercFaction.FactionID = mercConfig.Name;
                    tempMercFaction.Name = GetUseableFactionName(mercFactionDef, tempMercFaction.FactionID);

                    tempMercFaction.Description = mercFactionDef.RootElement.GetProperty("Description").ToString();

                    tempMercFaction.UnitRating = mercConfig.Value.GetProperty("UnitRating").GetInt32();

                    tempMercFaction.RestrictionIsWhitelist = mercConfig.Value.GetProperty("RestrictionIsWhitelist").GetBoolean();
                    tempMercFaction.EmployerList = new List<string>();
                    var listOfEmployers = mercConfig.Value.GetProperty("EmployerRestrictions").EnumerateArray();
                    foreach (var val in listOfEmployers)
                    {
                        tempMercFaction.EmployerList.Add(GetUseableFactionName(factionDefs[val.ToString()], val.ToString()));
                        if (tempMercFaction.PrimaryEmployer == null && (val.ToString() != "Locals" && val.ToString() != "Mercenaries"))
                            tempMercFaction.PrimaryEmployer = GetUseableFactionName(factionDefs[val.ToString()], val.ToString());
                    }

                    if (mercPlanetMap.ContainsKey(tempMercFaction.FactionID))
                    {
                        tempMercFaction.PlanetNames = mercPlanetMap[tempMercFaction.FactionID];
                        if (tempMercFaction.PrimaryEmployer == null)
                            tempMercFaction.PrimaryEmployer = "Other Merc Commands";
                    }

                    tempMercFaction.Personality = new List<string>();
                    var listOfPersonalityBits = mercConfig.Value.GetProperty("PersonalityAttributes").EnumerateArray();
                    foreach (var val in listOfPersonalityBits)
                    {
                        tempMercFaction.Personality.Add(val.ToString());
                    }

                    List<SpamFactionData> tempDataList;

                    if (tempMercFaction.PrimaryEmployer == null)
                    {
                        Console.WriteLine($"{tempMercFaction.Name} don't have a primary employer! Sticking them in the \"other\" group!\r\n ID: {tempMercFaction.FactionID}");
                        tempMercFaction.PrimaryEmployer = "Other Merc Commands";
                    }

                    if (mercFactions.TryGetValue(tempMercFaction.PrimaryEmployer, out tempDataList))
                    {
                        tempDataList.Add(tempMercFaction);
                        mercFactions[tempMercFaction.PrimaryEmployer] = tempDataList;
                    }
                    else
                    {
                        tempDataList = new List<SpamFactionData>();
                        tempDataList.Add(tempMercFaction);
                        mercFactions[tempMercFaction.PrimaryEmployer] = tempDataList;
                    }
                }
                else
                    Console.Write("ERROR: No JSON for " + mercConfig.Name);
            }

            using StreamWriter outputFile = new("MercenariesListPage.txt", append: false);

            foreach (string key in mercFactions.Keys)
            {
                outputFile.WriteLine("<div style=\"clear: both;\">");
                outputFile.WriteLine($"==={key}===");
                outputFile.WriteLine();
                foreach (SpamFactionData mercCompany in mercFactions[key])
                {
                    outputFile.WriteLine(mercCompany.OutputDefToHTML());
                }
                outputFile.WriteLine("</div>");
            }
        }

        private Dictionary<string, List<string>> MapPlanetsToMercsByIds()
        {
            Dictionary<string, List<string>> mercPlanetMap = new Dictionary<string, List<string>>();

            var planetFactionDefs = spamConfig.RootElement.GetProperty("Settings").GetProperty("PlanetFactionConfigs").EnumerateObject();

            foreach (var planetDef in planetFactionDefs)
            {
                string planetName = planetDefs[planetDef.Name].RootElement.GetProperty("Description").GetProperty("Name").ToString();
                var mercCompanies = planetDef.Value.GetProperty("AlternateOpforWeights").EnumerateArray();

                foreach (JsonElement merc in mercCompanies)
                {
                    string tempMercId = merc.GetProperty("FactionName").ToString();
                    if (!mercPlanetMap.ContainsKey(tempMercId))
                        mercPlanetMap[tempMercId] = new List<string>();
                    mercPlanetMap[tempMercId].Add(planetName);
                }
            }

            return mercPlanetMap;
        }

        private string GetUseableFactionName(JsonDocument factionDef, string factionId)
        {
            string name;
            if (!CheckForSpecialTranslation(factionId, out name))
            {
                name = factionDef.RootElement.GetProperty("Name").ToString();
                if (name.StartsWith("the "))
                    name = name.Substring(4);
            }
            return name;
        }

        Dictionary<string, string> subCommandRatings = new Dictionary<string, string>(){
            {"40thShadowDivision", "Elite"}, 
            {"DeathCommandos", "Elite"}, 
            {"1stMcCarronsArmoredCavalry", "Elite"}, 
            {"1stSwordOfLight", "Elite"}, 
            {"DavionAssaultGuards", "Elite"}, 
            {"6thLyranGuards", "Elite"}, 
            {"1stKnightsOfTheInnerSphere", "Elite"}, 
            {"2ndArmyVMu", "Elite"}, 
            {"1stTyr", "Elite"}, 
            {"TaurianGuard", "Elite"}, 
            {"RaventhirsIronHand", "Elite"}, 
            {"1stAllianceAirWing", "Elite"}, 
            {"ILegioMartiaVictrix", "Elite"}, 
            {"9thDivisionWoB", "Regular"}, 
            {"WarriorHouseImarra", "Regular"}, 
            {"3rdNightStalkers", "Regular"}, 
            {"2ndCrucisLancers", "Regular"}, 
            {"10thLyranGuards", "Regular"}, 
            {"5thDonegalGuards", "Regular"}, 
            {"11thAvalonHussars", "Regular"}, 
            {"2ndFreeWorldsGuards", "Regular"}, 
            {"11thArmyVEta", "Regular"}, 
            {"2ndFreemen", "Regular"}, 
            {"PleiadesHussars", "Regular"}, 
            {"2ndCanopianFusiliers", "Regular"}, 
            {"3rdAllianceAirWing", "Regular"}, 
            {"VLegioRipariensis", "Regular"}, 
            {"WoBProtectorateMilitia", "Garrison"}, 
            {"6thConfederationReserveCavalry", "Garrison"}, 
            {"2ndLegionOfVega", "Garrison"}, 
            {"1stKitteryBorderers", "Garrison"}, 
            {"15thLyranRegulars", "Garrison"}, 
            {"30thMarikMilitia", "Garrison"}, 
            {"7thArmyVIota", "Garrison"}, 
            {"4thKavalleri", "Garrison"}, 
            {"3rdTaurianLancers", "Garrison"}, 
            {"MagistracyCavaliers", "Garrison"}, 
            {"4thAllianceAirWing", "Garrison"}, 
            {"CohorsMorituri", "Garrison"},
            {"CWEpsilonGalaxy", "Garrison"},
            {"CNCOmicronGalaxy", "Garrison"},
            {"CJFIotaGalaxy", "Garrison"},
            {"CGBThetaGalaxy", "Garrison"}
        };


        public void OutputFactionSubCommands()
        {
            JsonElement factionJson = spamConfig.RootElement.GetProperty("Settings");
            var factionParentCommands = factionJson.GetProperty("AlternateFactionConfigs").EnumerateObject();

            Dictionary<string, List<SpamFactionData>> parentFactions = new Dictionary<string, List<SpamFactionData>>();

            foreach (var parentFaction in factionParentCommands)
            {
                string parentFactionProperName = GetUseableFactionName(factionDefs[parentFaction.Name], parentFaction.Name);
                if (!parentFactions.ContainsKey(parentFactionProperName))
                    parentFactions[parentFactionProperName] = new List<SpamFactionData>();

                var subCommands = parentFaction.Value.GetProperty("AlternateOpforWeights").EnumerateArray();
                foreach (var subCommand in subCommands)
                {
                    string subCommandId = subCommand.GetProperty("FactionName").ToString();

                    JsonDocument subCommandJsonData = factionDefs[subCommandId];
                    SpamFactionData tempSubCommandData = new SpamFactionData();

                    if (subCommandJsonData != null)
                    {
                        tempSubCommandData.FactionID = subCommandId;
                        tempSubCommandData.Name = GetUseableFactionName(subCommandJsonData, tempSubCommandData.FactionID);

                        tempSubCommandData.Description = subCommandJsonData.RootElement.GetProperty("Description").ToString();

                        tempSubCommandData.IncludeUnitsSectionInOutput = true;

                        if (subCommandRatings.ContainsKey(subCommandId))
                            tempSubCommandData.SubcommandRating = subCommandRatings[subCommandId];
                        else
                            Console.WriteLine("ERROR: No SUBCOMMAND RATING for " + subCommandId);

                        parentFactions[parentFactionProperName].Add(tempSubCommandData);
                    }
                    else
                        Console.WriteLine("ERROR: No JSON for " + subCommandId);
                }
            }

            using StreamWriter outputFile = new("SubcommandsOuput.txt", append: false);

            foreach (string key in parentFactions.Keys)
            {
                outputFile.WriteLine("<div style=\"clear: both;\">");
                outputFile.WriteLine($"==={key}===");
                outputFile.WriteLine();
                foreach (SpamFactionData subCommand in parentFactions[key])
                {
                    outputFile.WriteLine(subCommand.OutputDefToHTML());
                }
                outputFile.WriteLine("</div>");
            }
        }
    }
}
