using BT_JsonProcessingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UtilityClassLibrary;

namespace BTA_WikiGeneration
{
    internal class FactionDataProcessor
    {
        string spamFolder = "SoldiersPiratesAssassinsMercs\\";

        private readonly JsonDocument spamConfig;

        public FactionDataProcessor(string modsFolder)
        {
            spamConfig = JsonDocument.Parse(File.ReadAllText(modsFolder + spamFolder + "mod.json"), UtilityStatics.GeneralJsonDocOptions);
            var MercPlanets = spamConfig.RootElement.GetProperty("Settings").GetProperty("PlanetFactionConfigs").EnumerateObject();
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
                SpamFactionData tempMercFaction = new SpamFactionData();

                if (FactionDataHandler.TryGetFactionById(mercConfig.Name, out FactionData mercFactionDef))
                {
                    tempMercFaction.FactionID = mercConfig.Name;
                    tempMercFaction.Name = FactionDataHandler.GetUseableFactionNameFromId(tempMercFaction.FactionID);

                    tempMercFaction.Description = mercFactionDef.Description;

                    tempMercFaction.UnitRating = mercConfig.Value.GetProperty("UnitRating").GetInt32();

                    tempMercFaction.RestrictionIsWhitelist = mercConfig.Value.GetProperty("RestrictionIsWhitelist").GetBoolean();
                    tempMercFaction.EmployerList = new List<string>();
                    var listOfEmployers = mercConfig.Value.GetProperty("EmployerRestrictions").EnumerateArray();
                    foreach (var val in listOfEmployers)
                    {
                        tempMercFaction.EmployerList.Add(FactionDataHandler.GetUseableFactionNameFromId(val.ToString()));
                        if (tempMercFaction.PrimaryEmployer == null && (val.ToString() != "Locals" && val.ToString() != "Mercenaries"))
                            tempMercFaction.PrimaryEmployer = FactionDataHandler.GetUseableFactionNameFromId(val.ToString());
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
                if(PlanetDataHandler.TryGetPlanetDataForId(planetDef.Name, out PlanetData planetData))
                {
                    string planetName = planetData.Name;
                    var mercCompanies = planetDef.Value.GetProperty("AlternateOpforWeights").EnumerateArray();

                    foreach (JsonElement merc in mercCompanies)
                    {
                        string tempMercId = merc.GetProperty("FactionName").ToString();
                        if (!mercPlanetMap.ContainsKey(tempMercId))
                            mercPlanetMap[tempMercId] = new List<string>();
                        mercPlanetMap[tempMercId].Add(planetName);
                    }
                }
            }

            return mercPlanetMap;
        }

        Dictionary<string, string> subCommandRatings = new Dictionary<string, string>(){
            {"40thShadowDivision", "Elite" }, 
            {"DeathCommandos", "Elite" }, 
            {"1stMcCarronsArmoredCavalry", "Elite" }, 
            {"1stSwordOfLight", "Elite" }, 
            {"DavionAssaultGuards", "Elite" }, 
            {"6thLyranGuards", "Elite" }, 
            {"1stKnightsOfTheInnerSphere", "Elite" }, 
            {"2ndArmyVMu", "Elite" }, 
            {"1stTyr", "Elite" }, 
            {"TaurianGuard", "Elite" }, 
            {"RaventhirsIronHand", "Elite" }, 
            {"1stAllianceAirWing", "Elite" }, 
            {"ILegioMartiaVictrix", "Elite" }, 
            {"9thDivisionWoB", "Regular" }, 
            {"WarriorHouseImarra", "Regular" }, 
            {"3rdNightStalkers", "Regular" }, 
            {"2ndCrucisLancers", "Regular" }, 
            {"10thLyranGuards", "Regular" }, 
            {"5thDonegalGuards", "Regular" }, 
            {"11thAvalonHussars", "Regular" }, 
            {"2ndFreeWorldsGuards", "Regular" }, 
            {"11thArmyVEta", "Regular" }, 
            {"2ndFreemen", "Regular" }, 
            {"PleiadesHussars", "Regular" }, 
            {"2ndCanopianFusiliers", "Regular" }, 
            {"3rdAllianceAirWing", "Regular" }, 
            {"VLegioRipariensis", "Regular" }, 
            {"WoBProtectorateMilitia", "Garrison" }, 
            {"6thConfederationReserveCavalry", "Garrison" }, 
            {"2ndLegionOfVega", "Garrison" }, 
            {"1stKitteryBorderers", "Garrison" }, 
            {"15thLyranRegulars", "Garrison" }, 
            {"30thMarikMilitia", "Garrison" }, 
            {"7thArmyVIota", "Garrison" }, 
            {"4thKavalleri", "Garrison" }, 
            {"3rdTaurianLancers", "Garrison" }, 
            {"MagistracyCavaliers", "Garrison" }, 
            {"4thAllianceAirWing", "Garrison" }, 
            {"CohorsMorituri", "Garrison" },
            {"CWEpsilonGalaxy", "Garrison" },
            {"CNCOmicronGalaxy", "Garrison" },
            {"CJFIotaGalaxy", "Garrison" },
            {"CGBThetaGalaxy", "Garrison" },
            {"CSRDeltaGalaxy", "Garrison" }
        };


        public void OutputFactionSubCommands()
        {
            JsonElement factionJson = spamConfig.RootElement.GetProperty("Settings");
            var factionParentCommands = factionJson.GetProperty("AlternateFactionConfigs").EnumerateObject();

            Dictionary<string, List<SpamFactionData>> parentFactions = new Dictionary<string, List<SpamFactionData>>();

            foreach (var parentFaction in factionParentCommands)
            {
                string parentFactionProperName = FactionDataHandler.GetUseableFactionNameFromId(parentFaction.Name);
                if (!parentFactions.ContainsKey(parentFactionProperName))
                    parentFactions[parentFactionProperName] = new List<SpamFactionData>();

                var subCommands = parentFaction.Value.GetProperty("AlternateOpforWeights").EnumerateArray();
                foreach (var subCommand in subCommands)
                {
                    string subCommandId = subCommand.GetProperty("FactionName").ToString();

                    SpamFactionData tempSubCommandData = new SpamFactionData();

                    if (FactionDataHandler.TryGetFactionById(subCommandId, out FactionData subCommandFactionData))
                    {
                        tempSubCommandData.FactionID = subCommandId;
                        tempSubCommandData.Name = FactionDataHandler.GetUseableFactionNameFromId(tempSubCommandData.FactionID);

                        tempSubCommandData.Description = subCommandFactionData.Description;

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
