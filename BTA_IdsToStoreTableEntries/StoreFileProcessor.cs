using BT_JsonProcessingLibrary;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UtilityClassLibrary;
using UtilityClassLibrary.WikiLinkOverrides;

namespace BTA_IdsToStoreTableEntries
{
    internal static class StoreFileProcessor
    {
        private static string FactoryStoreDirectory = "DynamicShops\\factories\\";
        private static string CommunityContentDirectory = "Community Content\\";

        //private static List<string> Headings = new List<string>();
        private static ConcurrentDictionary<string, Dictionary<StoreHeadingsGroup, List<StoreEntry>>> StoreDictionary = new ConcurrentDictionary<string, Dictionary<StoreHeadingsGroup, List<StoreEntry>>>();
        
        private static Dictionary<string, List<StoreTagAssociation>> allStoreListsByTag = new Dictionary<string, List<StoreTagAssociation>>();
        private static Dictionary<string, List<StoreTagAssociation>> factoryStoreListsByTag = new Dictionary<string, List<StoreTagAssociation>>();
        private static Dictionary<string, List<StoreTagAssociation>> nonFactoryStoreListsByTag = new Dictionary<string, List<StoreTagAssociation>>();

        private static Dictionary<string, List<StoreTagAssociation>> factionStoreLists = new Dictionary<string, List<StoreTagAssociation>>();

        public static void LoadStoreFileData(string modsFolder)
        {
            //Headings = TextFileListProcessor.GetStringListFromFile(".\\StoreTableHeadings.txt");

            PlanetDataHandler.PopulatePlanetFileData(modsFolder);

            FactionDataHandler.PopulateFactionDefData(modsFolder);

            GetAllItemLists(modsFolder);
            //GetFactoryItemLists(modsFolder);

            LoadFactoryTagData(modsFolder);
            GetFactionStores(modsFolder);
        }

        public static void OutputFactoryStoresToString()
        {
            StreamWriter individualTablesWriter = new StreamWriter(".\\FactoryStoreTables.txt", false);
            StreamWriter singleTableWriter = new StreamWriter(".\\AllFactoryStoresTable.txt", false);

            singleTableWriter.Write(CreateStoreTableHeader(new List<string> { "Planet", "Faction", "Rep Required" }, "sortable"));

            List<string> sortedKeys = factoryStoreListsByTag.Keys.ToList();
            sortedKeys.Sort();

            foreach (string planetTag in sortedKeys)
            {
                PlanetDataHandler.TryGetPlanetsWithTag(planetTag, out List<PlanetData> data);

                PlanetData planet = data.First();
                if (data.Count() > 1)
                    Console.WriteLine($"MULTIPLE PLANET ENTRIES FOUND FOR PLANET TAG?!?! {planetTag}");

                StoreTagAssociation storeTagData = factoryStoreListsByTag[planetTag].First();

                if (factoryStoreListsByTag[planetTag].Count() > 1)
                    Console.WriteLine($"MULTIPLE STORE LISTS FOUND FOR PLANET TAG!?! {planetTag}");

                individualTablesWriter.WriteLine($"=== {planet.Name} ===");
                individualTablesWriter.WriteLine("");
                individualTablesWriter.WriteLine("");
                singleTableWriter.Write(CreateStoreTableHeader(new List<string> { "Planet", "Faction", "Rep Required" }));
                string storeTableString = OutputStoreToString(
                    new List<string> { 
                        planet.Name,
                        storeTagData.owner == null ? "Any" : FactionDataHandler.GetUseableFactionNameFromShortName(storeTagData.owner),
                        storeTagData.rep == null ? "None" : storeTagData.rep },
                    StoreDictionary[storeTagData.itemsListId]);
                individualTablesWriter.Write(storeTableString);
                individualTablesWriter.WriteLine("|}");
                individualTablesWriter.WriteLine("");
                individualTablesWriter.WriteLine("");
                individualTablesWriter.WriteLine("");
                individualTablesWriter.WriteLine("");

                singleTableWriter.Write(storeTableString);
            }
            singleTableWriter.WriteLine("|}");


            individualTablesWriter.Close();
            singleTableWriter.Close();
        }

        public static void OutputFactionStoresToString()
        {
            Dictionary<string, string> factionNames = new Dictionary<string, string>();

            foreach(string factionId in factionStoreLists.Keys)
            {
                string factionName = FactionDataHandler.GetUseableFactionNameFromId(factionId);
                factionNames.Add(factionName, factionId);
            }

            List<string> sortedFactionNames = factionNames.Keys.ToList();
            sortedFactionNames.Sort();

            StreamWriter singleTableWriter = new StreamWriter(".\\FactionStoresTable.txt", false);
            StreamWriter individualTablesWriter = new StreamWriter(".\\IndividualFactionStoreTAbles.txt", false);

            singleTableWriter.Write(CreateStoreTableHeader(new List<string> { "Faction" }, "sortable"));

            List<string> sortedKeys = factoryStoreListsByTag.Keys.ToList();
            sortedKeys.Sort();

            foreach (string factionName in sortedFactionNames) 
            {
                string factionId = factionNames[factionName];

                string factionLink = $"[[File:{factionId}_logo.jpg|link={factionName}|75px]] \r\n{FactionDataHandler.GetLinkFromFactionId(factionId)}";

                Dictionary<StoreHeadingsGroup, List<StoreEntry>> factionStoreList = ConsolidateStoreLists(factionStoreLists[factionId].Select(itemList => { return itemList.itemsListId; }).ToList());

                individualTablesWriter.WriteLine($"=== {factionName} ===");
                individualTablesWriter.WriteLine("");
                individualTablesWriter.WriteLine("");
                singleTableWriter.Write(CreateStoreTableHeader(new List<string> { "Faction" }));
                string storeTableString = OutputStoreToString(
                    new List<string> { factionLink },
                    factionStoreList);
                individualTablesWriter.Write(storeTableString);
                individualTablesWriter.WriteLine("|}");
                individualTablesWriter.WriteLine("");
                individualTablesWriter.WriteLine("");
                individualTablesWriter.WriteLine("");
                individualTablesWriter.WriteLine("");

                singleTableWriter.Write(storeTableString);
            }
            singleTableWriter.WriteLine("|}");


            individualTablesWriter.Close();
            singleTableWriter.Close();
        }

        private static void LoadFactoryTagData(string modsFolder)
        {
            string shopDataFolder = modsFolder + "DynamicShops\\sshops\\";

            foreach (BasicFileData fileData in ModJsonHandler.SearchFiles(shopDataFolder, "*.json"))
            {
                StreamReader storeDataReader = new StreamReader(fileData.Path);

                JsonDocument storeData = JsonDocument.Parse(storeDataReader.ReadToEnd());

                foreach(var storeConfigEntry in storeData.RootElement.EnumerateArray())
                {
                    StoreTagAssociation tempAssociation = new StoreTagAssociation();
                    JsonElement conditions = storeConfigEntry.GetProperty("conditions");
                    if (conditions.TryGetProperty("tag", out JsonElement tagJson))
                        tempAssociation.tag = tagJson.ToString();
                    if (conditions.TryGetProperty("owner", out JsonElement ownerJson))
                        tempAssociation.owner = ownerJson.ToString();
                    if(conditions.TryGetProperty("rep", out JsonElement repJson))
                    {
                        string tempString = repJson.ToString();
                        if (tempString.Contains('+'))
                            tempString = tempString.Replace("+", "").ToUpper() + "+";

                        tempAssociation.rep = tempString;
                    }

                    tempAssociation.itemsListId = storeConfigEntry.GetProperty("items").ToString();

                    if(tempAssociation.tag != null)
                    {
                        if (!allStoreListsByTag.ContainsKey(tempAssociation.tag))
                            allStoreListsByTag.Add(tempAssociation.tag, new List<StoreTagAssociation>());
                        allStoreListsByTag[tempAssociation.tag].Add(tempAssociation);

                        if (fileData.FileName.StartsWith("factories - "))
                        {
                            if (!factoryStoreListsByTag.ContainsKey(tempAssociation.tag))
                                factoryStoreListsByTag.Add(tempAssociation.tag, new List<StoreTagAssociation>());
                            factoryStoreListsByTag[tempAssociation.tag].Add(tempAssociation);
                        }
                        else
                        {
                            if (!nonFactoryStoreListsByTag.ContainsKey(tempAssociation.tag))
                                nonFactoryStoreListsByTag.Add(tempAssociation.tag, new List<StoreTagAssociation>());
                            nonFactoryStoreListsByTag[tempAssociation.tag].Add(tempAssociation);
                        }
                    }
                }
            }
        }

        private static void GetAllItemLists(string modsFolder)
        {
            FactionDataHandler.PopulateFactionDefData(modsFolder);

            Dictionary<string, List<BasicFileData>> fileCopiesDict = new Dictionary<string, List<BasicFileData>>();

            foreach (BasicFileData factionShopData in ModJsonHandler.SearchFiles(modsFolder.Replace("mods\\", ""), "*.csv"))
            {
                if (!factionShopData.Path.Contains(".modtek"))
                {
                    if (!fileCopiesDict.ContainsKey(factionShopData.FileName))
                        fileCopiesDict.Add(factionShopData.FileName, new List<BasicFileData>());
                    fileCopiesDict[factionShopData.FileName].Add(factionShopData);
                }
            }

            ParallelOptions parallelOptions = new ParallelOptions();
            parallelOptions.MaxDegreeOfParallelism = 8;

            Parallel.ForEach(fileCopiesDict.Keys, parallelOptions, fileName =>
            {
                foreach (BasicFileData storeFileDef in fileCopiesDict[fileName])
                {
                    UnpackStoreList(storeFileDef, modsFolder, true);
                }
            });
        }

        private static void GetFactionStores(string modsFolder)
        {
            FactionDataHandler.PopulateFactionDefData(modsFolder);

            string shopDataFolder = modsFolder + "DynamicShops\\fshops\\";

            foreach (BasicFileData factionShopData in ModJsonHandler.SearchFiles(shopDataFolder, "*.json"))
            {
                StreamReader factionStoreReader = new StreamReader(factionShopData.Path);
                JsonDocument factionStoreDef = JsonDocument.Parse(factionStoreReader.ReadToEnd());

                List<StoreTagAssociation> factionStoreCollection = new List<StoreTagAssociation>();

                string factionId = factionShopData.FileName.Replace(".json", "").Trim();

                foreach (JsonElement factionItems in factionStoreDef.RootElement.EnumerateArray())
                {
                    factionStoreCollection.Add(new StoreTagAssociation
                    {
                        itemsListId = factionItems.GetProperty("items").ToString(),
                        owner = factionId
                    });
                }

                factionStoreLists.Add(factionId, factionStoreCollection);
            }
        }

        //private static void GetFactoryItemLists(string modsFolder)
        //{
        //    var itemLists = ModJsonHandler.SearchFiles(modsFolder + FactoryStoreDirectory, "BTA_List_*");
        //    itemLists.AddRange(ModJsonHandler.SearchFiles(modsFolder + CommunityContentDirectory, "BTA_List_*"));

        //    ParallelOptions parallelOptions = new ParallelOptions();
        //    parallelOptions.MaxDegreeOfParallelism = 8;

        //    Parallel.ForEach(itemLists, parallelOptions, fileData =>
        //    {
        //        UnpackStoreList(fileData, modsFolder);
        //    });
        //}

        private static Dictionary<StoreHeadingsGroup, List<StoreEntry>> UnpackStoreList(BasicFileData storeFile, string modsFolder, bool addToStoreList = false)
        {
            StreamReader storeListReader = new StreamReader(storeFile.Path);
            storeListReader.ReadLine();

            string storeName = storeFile.FileName.Replace(".csv", "");

            if(!addToStoreList && StoreDictionary.TryGetValue(storeName, out Dictionary<StoreHeadingsGroup, List<StoreEntry>> storeDictionary))
                return storeDictionary;

            Dictionary<StoreHeadingsGroup, List<StoreEntry>> storeEntries = new Dictionary<StoreHeadingsGroup, List<StoreEntry>>();

            storeEntries[StoreHeadingsGroup.FullMechs] = new List<StoreEntry>();
            storeEntries[StoreHeadingsGroup.MechParts] = new List<StoreEntry>();
            storeEntries[StoreHeadingsGroup.Ammunition] = new List<StoreEntry>();
            storeEntries[StoreHeadingsGroup.BattleArmor] = new List<StoreEntry>();
            storeEntries[StoreHeadingsGroup.Equipment] = new List<StoreEntry>();
            storeEntries[StoreHeadingsGroup.Vehicles] = new List<StoreEntry>();
            storeEntries[StoreHeadingsGroup.Contracts] = new List<StoreEntry>();
            storeEntries[StoreHeadingsGroup.Weapons] = new List<StoreEntry>();

            while (!storeListReader.EndOfStream)
            {
                string[] storeEntry = storeListReader.ReadLine().Split(',');

                StoreEntry tempStoreEntry = new StoreEntry();

                switch (storeEntry[1])
                {
                    case "Weapon":
                        tempStoreEntry.StoreHeading = StoreHeadingsGroup.Weapons;
                        break;
                    case "JumpJet":
                        tempStoreEntry.StoreHeading = StoreHeadingsGroup.Equipment;
                        break;
                    case "AmmunitionBox":
                        tempStoreEntry.StoreHeading = StoreHeadingsGroup.Ammunition;
                        break;
                    case "HeatSink":
                        tempStoreEntry.StoreHeading = StoreHeadingsGroup.Equipment;
                        break;
                    case "Upgrade":
                        if (storeEntry[0].StartsWith("Gear_Contract"))
                            tempStoreEntry.StoreHeading = StoreHeadingsGroup.Contracts;
                        else if (storeEntry[0].StartsWith("Gear_"))
                            tempStoreEntry.StoreHeading = StoreHeadingsGroup.Equipment;
                        break;
                    case "Mech":
                        if (storeEntry[0].StartsWith("mechdef_ba_"))
                            tempStoreEntry.StoreHeading = StoreHeadingsGroup.BattleArmor;
                        else if (storeEntry[0].StartsWith("mechdef"))
                            tempStoreEntry.StoreHeading = StoreHeadingsGroup.FullMechs;
                        else if (storeEntry[0].StartsWith("vehicledef"))
                            tempStoreEntry.StoreHeading = StoreHeadingsGroup.Vehicles;
                        break;
                    case "MechPart":
                        tempStoreEntry.StoreHeading = StoreHeadingsGroup.MechParts;
                        break;
                    case "Reference":
                        tempStoreEntry.StoreHeading = StoreHeadingsGroup.Reference;
                        tempStoreEntry.Id = storeEntry[0];
                        //foreach(var pair in UnpackStoreList(ModJsonHandler.SearchFiles(modsFolder, storeEntry[0] + ".csv").First(), modsFolder))
                        //{
                        //    storeEntries[pair.Key].AddRange(pair.Value);
                        //}
                        continue;
                }

                BasicFileData itemDefFile = ModJsonHandler.SearchFiles(modsFolder, storeEntry[0] + ".json")[0];
                JsonDocument fileDoc = ModJsonHandler.GetJsonDocument(itemDefFile.Path);

                tempStoreEntry.Id = ModJsonHandler.GetIdFromJsonDoc(fileDoc);
                tempStoreEntry.Name = ModJsonHandler.GetNameFromJsonDoc(fileDoc);
                tempStoreEntry.UiName = ModJsonHandler.GetUiNameFromJsonDoc(fileDoc);

                if(tempStoreEntry.StoreHeading == StoreHeadingsGroup.FullMechs || tempStoreEntry.StoreHeading == StoreHeadingsGroup.MechParts)
                {
                    BasicFileData chassisDefFile = ModJsonHandler.GetChassisDef(itemDefFile);
                    StreamReader chassisReader = new StreamReader(chassisDefFile.Path);
                    tempStoreEntry.PageSubTarget = JsonDocument.Parse(chassisReader.ReadToEnd()).RootElement.GetProperty("VariantName").ToString().Trim();
                    if(MechLinkOverrides.TryGetLinkOverride(tempStoreEntry.PageSubTarget, out string linkOverride))
                    {
                        tempStoreEntry.LinkPageTarget = linkOverride;
                    }
                    else
                    {
                        tempStoreEntry.LinkPageTarget = tempStoreEntry.Name;
                    }
                }
                else if (tempStoreEntry.StoreHeading == StoreHeadingsGroup.Vehicles)
                {
                    BasicFileData chassisDefFile = ModJsonHandler.GetVehicleChassisDef(itemDefFile);
                    StreamReader chassisReader = new StreamReader(chassisDefFile.Path);
                    tempStoreEntry.PageSubTarget = tempStoreEntry.UiName;
                    JsonDocument tempChassisDoc = JsonDocument.Parse(chassisReader.ReadToEnd());
                    tempStoreEntry.LinkPageTarget = ModJsonHandler.GetNameFromJsonDoc(tempChassisDoc);
                }
                else if (tempStoreEntry.StoreHeading == StoreHeadingsGroup.Weapons)
                {
                    tempStoreEntry.LinkPageTarget = "Weapons";
                    if (WeaponLinkOverrides.TryGetLinkOverride(storeFile.Path, tempStoreEntry.Id, tempStoreEntry.UiName, out string linkOverride))
                        tempStoreEntry.PageSubTarget = linkOverride;
                    else
                        tempStoreEntry.PageSubTarget = tempStoreEntry.UiName.Replace("Ammo", "").Trim();
                }
                else if (tempStoreEntry.StoreHeading == StoreHeadingsGroup.Equipment)
                {
                    tempStoreEntry = EquipmentLinkOverrides.GetStoreDataForId(tempStoreEntry.Id, modsFolder);
                }
                else if (tempStoreEntry.StoreHeading == StoreHeadingsGroup.Ammunition)
                {
                    tempStoreEntry.LinkPageTarget = "Ammunition";
                    if (AmmoBoxLinkOverrides.TryGetLinkOverride(tempStoreEntry.UiName, out string linkOverride))
                        tempStoreEntry.PageSubTarget = linkOverride;
                }
                else if (tempStoreEntry.StoreHeading == StoreHeadingsGroup.Contracts)
                {
                    tempStoreEntry.LinkPageTarget = "Contracts";
                    tempStoreEntry.PageSubTarget = tempStoreEntry.UiName;
                }
                else
                {
                    tempStoreEntry.LinkPageTarget = tempStoreEntry.UiName;
                }

                storeEntries[tempStoreEntry.StoreHeading].Add(tempStoreEntry);
            }

            StoreDictionary.TryAdd(storeName, storeEntries);

            return storeEntries;
        }

        private static Dictionary<StoreHeadingsGroup, List<StoreEntry>> ConsolidateStoreLists(List<string> storeIds)
        {
            Dictionary<StoreHeadingsGroup, List<StoreEntry>> resultingStoreList = new Dictionary<StoreHeadingsGroup, List<StoreEntry>>();

            List<Dictionary<StoreHeadingsGroup, List<StoreEntry>>> unpackedReferenceStores = new List<Dictionary<StoreHeadingsGroup, List<StoreEntry>>>();

            foreach (string storeId in storeIds)
            {
                if(StoreDictionary[storeId].ContainsKey(StoreHeadingsGroup.Reference))
                    unpackedReferenceStores.Add(ConsolidateStoreLists(StoreDictionary[storeId][StoreHeadingsGroup.Reference].Select(storeRef => { return storeRef.Id; }).ToList()));

                resultingStoreList = CombineTwoStoreLists(resultingStoreList, StoreDictionary[storeId]);
            }

            foreach(var storeReference in unpackedReferenceStores)
            {
                resultingStoreList = CombineTwoStoreLists(resultingStoreList, storeReference);
            }

            return resultingStoreList;
        }

        private static Dictionary<StoreHeadingsGroup, List<StoreEntry>> CombineTwoStoreLists(Dictionary<StoreHeadingsGroup, List<StoreEntry>> firstList, Dictionary<StoreHeadingsGroup, List<StoreEntry>> secondList)
        {
            Dictionary<StoreHeadingsGroup, List<StoreEntry>> combinedStoreList = new Dictionary<StoreHeadingsGroup, List<StoreEntry>>();

            if (firstList.ContainsKey(StoreHeadingsGroup.Ammunition) || secondList.ContainsKey(StoreHeadingsGroup.Ammunition))
            {
                combinedStoreList.Add(StoreHeadingsGroup.Ammunition, new List<StoreEntry>());
                if (firstList.ContainsKey(StoreHeadingsGroup.Ammunition))
                    combinedStoreList[StoreHeadingsGroup.Ammunition].AddRange(firstList[StoreHeadingsGroup.Ammunition]);
                if (secondList.ContainsKey(StoreHeadingsGroup.Ammunition))
                    combinedStoreList[StoreHeadingsGroup.Ammunition].AddRange(secondList[StoreHeadingsGroup.Ammunition]);
            }
            if (firstList.ContainsKey(StoreHeadingsGroup.BattleArmor) || secondList.ContainsKey(StoreHeadingsGroup.BattleArmor))
            {
                combinedStoreList.Add(StoreHeadingsGroup.BattleArmor, new List<StoreEntry>());
                if (firstList.ContainsKey(StoreHeadingsGroup.BattleArmor))
                    combinedStoreList[StoreHeadingsGroup.BattleArmor].AddRange(firstList[StoreHeadingsGroup.BattleArmor]);
                if (secondList.ContainsKey(StoreHeadingsGroup.BattleArmor))
                    combinedStoreList[StoreHeadingsGroup.BattleArmor].AddRange(secondList[StoreHeadingsGroup.BattleArmor]);
            }
            if (firstList.ContainsKey(StoreHeadingsGroup.Contracts) || secondList.ContainsKey(StoreHeadingsGroup.Contracts))
            {
                combinedStoreList.Add(StoreHeadingsGroup.Contracts, new List<StoreEntry>());
                if (firstList.ContainsKey(StoreHeadingsGroup.Contracts))
                    combinedStoreList[StoreHeadingsGroup.Contracts].AddRange(firstList[StoreHeadingsGroup.Contracts]);
                if (secondList.ContainsKey(StoreHeadingsGroup.Contracts))
                    combinedStoreList[StoreHeadingsGroup.Contracts].AddRange(secondList[StoreHeadingsGroup.Contracts]);
            }
            if (firstList.ContainsKey(StoreHeadingsGroup.Equipment) || secondList.ContainsKey(StoreHeadingsGroup.Equipment))
            {
                combinedStoreList.Add(StoreHeadingsGroup.Equipment, new List<StoreEntry>());
                if (firstList.ContainsKey(StoreHeadingsGroup.Equipment))
                    combinedStoreList[StoreHeadingsGroup.Equipment].AddRange(firstList[StoreHeadingsGroup.Equipment]);
                if (secondList.ContainsKey(StoreHeadingsGroup.Equipment))
                    combinedStoreList[StoreHeadingsGroup.Equipment].AddRange(secondList[StoreHeadingsGroup.Equipment]);
            }
            if (firstList.ContainsKey(StoreHeadingsGroup.FullMechs) || secondList.ContainsKey(StoreHeadingsGroup.FullMechs))
            {
                combinedStoreList.Add(StoreHeadingsGroup.FullMechs, new List<StoreEntry>());
                if (firstList.ContainsKey(StoreHeadingsGroup.FullMechs))
                    combinedStoreList[StoreHeadingsGroup.FullMechs].AddRange(firstList[StoreHeadingsGroup.FullMechs]);
                if (secondList.ContainsKey(StoreHeadingsGroup.FullMechs))
                    combinedStoreList[StoreHeadingsGroup.FullMechs].AddRange(secondList[StoreHeadingsGroup.FullMechs]);
            }
            if (firstList.ContainsKey(StoreHeadingsGroup.MechParts) || secondList.ContainsKey(StoreHeadingsGroup.MechParts))
            {
                combinedStoreList.Add(StoreHeadingsGroup.MechParts, new List<StoreEntry>());
                if (firstList.ContainsKey(StoreHeadingsGroup.MechParts))
                    combinedStoreList[StoreHeadingsGroup.MechParts].AddRange(firstList[StoreHeadingsGroup.MechParts]);
                if (secondList.ContainsKey(StoreHeadingsGroup.MechParts))
                    combinedStoreList[StoreHeadingsGroup.MechParts].AddRange(secondList[StoreHeadingsGroup.MechParts]);
            }
            if (firstList.ContainsKey(StoreHeadingsGroup.Vehicles) || secondList.ContainsKey(StoreHeadingsGroup.Vehicles))
            {
                combinedStoreList.Add(StoreHeadingsGroup.Vehicles, new List<StoreEntry>());
                if (firstList.ContainsKey(StoreHeadingsGroup.Vehicles))
                    combinedStoreList[StoreHeadingsGroup.Vehicles].AddRange(firstList[StoreHeadingsGroup.Vehicles]);
                if (secondList.ContainsKey(StoreHeadingsGroup.Vehicles))
                    combinedStoreList[StoreHeadingsGroup.Vehicles].AddRange(secondList[StoreHeadingsGroup.Vehicles]);
            }
            if (firstList.ContainsKey(StoreHeadingsGroup.Weapons) || secondList.ContainsKey(StoreHeadingsGroup.Weapons))
            {
                combinedStoreList.Add(StoreHeadingsGroup.Weapons, new List<StoreEntry>());
                if (firstList.ContainsKey(StoreHeadingsGroup.Weapons))
                    combinedStoreList[StoreHeadingsGroup.Weapons].AddRange(firstList[StoreHeadingsGroup.Weapons]);
                if (secondList.ContainsKey(StoreHeadingsGroup.Weapons))
                    combinedStoreList[StoreHeadingsGroup.Weapons].AddRange(secondList[StoreHeadingsGroup.Weapons]);
            }

            return combinedStoreList;
        }

        private static string CreateStoreTableHeader(List<string> additionalHeadings, string tableCssClass = "")
        {
            StringBuilder storeTableBuilder = new StringBuilder();

            storeTableBuilder.AppendLine($"{{| class=\"wikitable {tableCssClass}\" style=\"text-align: center\"");
            //storeTableBuilder.AppendLine($"! {storeNameHeading}");

            foreach(string heading in additionalHeadings)
            {
                storeTableBuilder.AppendLine($"! {heading}");
            }

            //storeTableBuilder.AppendLine("! Faction");
            //storeTableBuilder.AppendLine("! Rep Required");

            storeTableBuilder.AppendLine("! Weapons");
            storeTableBuilder.AppendLine("! Ammunition");
            storeTableBuilder.AppendLine("! Equipment");
            storeTableBuilder.AppendLine("! Full 'Mechs");
            storeTableBuilder.AppendLine("! 'Mech Parts");
            storeTableBuilder.AppendLine("! Vehicles");
            storeTableBuilder.AppendLine("! Battle Armor");
            storeTableBuilder.AppendLine("! Contracts");

            storeTableBuilder.AppendLine("|-");

            return storeTableBuilder.ToString();
        }

        private static string OutputStoreToString(List<string> otherColumnEntries, Dictionary<StoreHeadingsGroup, List<StoreEntry>> storeDict)
        {
            foreach(StoreHeadingsGroup heading in storeDict.Keys)
            {
                storeDict[heading].Sort();
            }

            StringBuilder storeTableBuilder = new StringBuilder();

            foreach(string columnData in otherColumnEntries)
            {
                storeTableBuilder.Append("! ");
                storeTableBuilder.AppendLine($"{columnData}");
            }

            storeTableBuilder.Append("| ");
            foreach (StoreEntry storeEntry in storeDict[StoreHeadingsGroup.Weapons])
                storeTableBuilder.Append($"[[{storeEntry.LinkPageTarget}#{storeEntry.PageSubTarget?? ""}|{storeEntry.UiName}]]</br>");
            if (!storeDict.ContainsKey(StoreHeadingsGroup.Weapons) || storeDict[StoreHeadingsGroup.Weapons].Count() == 0)
                storeTableBuilder.Append("None");
            storeTableBuilder.AppendLine("");

            storeTableBuilder.Append("| ");
            foreach (StoreEntry storeEntry in storeDict[StoreHeadingsGroup.Ammunition])
                storeTableBuilder.Append($"[[{storeEntry.LinkPageTarget}#{storeEntry.PageSubTarget ?? ""}|{storeEntry.UiName}]]</br>");
            if (!storeDict.ContainsKey(StoreHeadingsGroup.Ammunition) || storeDict[StoreHeadingsGroup.Ammunition].Count() == 0)
                storeTableBuilder.Append("None");
            storeTableBuilder.AppendLine("");

            storeTableBuilder.Append("| ");
            foreach (StoreEntry storeEntry in storeDict[StoreHeadingsGroup.Equipment])
                storeTableBuilder.Append($"[[{storeEntry.LinkPageTarget}#{storeEntry.PageSubTarget ?? ""}|{storeEntry.UiName}]]</br>");
            if (!storeDict.ContainsKey(StoreHeadingsGroup.Equipment) || storeDict[StoreHeadingsGroup.Equipment].Count() == 0)
                storeTableBuilder.Append("None");
            storeTableBuilder.AppendLine("");

            storeTableBuilder.Append("| ");
            foreach (StoreEntry storeEntry in storeDict[StoreHeadingsGroup.FullMechs])
                storeTableBuilder.Append($"[[{storeEntry.LinkPageTarget}#{storeEntry.PageSubTarget ?? ""}|{storeEntry.UiName}]]</br>");
            if (!storeDict.ContainsKey(StoreHeadingsGroup.FullMechs) || storeDict[StoreHeadingsGroup.FullMechs].Count() == 0)
                storeTableBuilder.Append("None");
            storeTableBuilder.AppendLine("");

            storeTableBuilder.Append("| ");
            foreach (StoreEntry storeEntry in storeDict[StoreHeadingsGroup.MechParts])
                storeTableBuilder.Append($"[[{storeEntry.LinkPageTarget}#{storeEntry.PageSubTarget ?? ""}|{storeEntry.UiName}]]</br>");
            if (!storeDict.ContainsKey(StoreHeadingsGroup.MechParts) || storeDict[StoreHeadingsGroup.MechParts].Count() == 0)
                storeTableBuilder.Append("None");
            storeTableBuilder.AppendLine("");

            storeTableBuilder.Append("| ");
            foreach (StoreEntry storeEntry in storeDict[StoreHeadingsGroup.Vehicles])
                storeTableBuilder.Append($"[[{storeEntry.LinkPageTarget}#{storeEntry.PageSubTarget ?? ""}|{storeEntry.UiName}]]</br>");
            if (!storeDict.ContainsKey(StoreHeadingsGroup.Vehicles) || storeDict[StoreHeadingsGroup.Vehicles].Count() == 0)
                storeTableBuilder.Append("None");
            storeTableBuilder.AppendLine("");

            storeTableBuilder.Append("| ");
            foreach (StoreEntry storeEntry in storeDict[StoreHeadingsGroup.BattleArmor])
                storeTableBuilder.Append($"[[{storeEntry.LinkPageTarget}#{storeEntry.PageSubTarget ?? ""}|{storeEntry.UiName}]]</br>");
            if (!storeDict.ContainsKey(StoreHeadingsGroup.BattleArmor) || storeDict[StoreHeadingsGroup.BattleArmor].Count() == 0)
                storeTableBuilder.Append("None");
            storeTableBuilder.AppendLine("");

            storeTableBuilder.Append("| ");
            foreach (StoreEntry storeEntry in storeDict[StoreHeadingsGroup.Contracts])
                storeTableBuilder.Append($"[[{storeEntry.LinkPageTarget}#{storeEntry.PageSubTarget ?? ""}|{storeEntry.UiName}]]</br>");
            if (!storeDict.ContainsKey(StoreHeadingsGroup.Contracts) || storeDict[StoreHeadingsGroup.Contracts].Count() == 0)
                storeTableBuilder.Append("None");
            storeTableBuilder.AppendLine("");
            storeTableBuilder.AppendLine("|-");

            return storeTableBuilder.ToString();
        }
    }
}
