﻿using BT_JsonProcessingLibrary;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Formats.Tar;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BTA_WikiTableGen
{
    public static class MechGearHandler
    {
        private static string ModFolder;
        private static ConcurrentDictionary<string, EquipmentData> GearData = new ConcurrentDictionary<string, EquipmentData>();
        private static ConcurrentDictionary<string, List<string>> MechTagsToGearIds = new ConcurrentDictionary<string, List<string>>();
        private static ConcurrentDictionary<string, List<string>> GearTagsToGearIds = new ConcurrentDictionary<string, List<string>>();

        public static Regex engineSizeRegex = new Regex(@"(?<=emod_engine_)(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static Regex structureRegex = new Regex(@"(\w*structureslots\w*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static Regex armorRegex = new Regex(@"(emod_armorslots\w*|Gear_armorslots\w*|Gear_Reflective_Coating)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static Regex DirectoryExcludeRegex = new Regex(@"(\\CustomAmmoCategories\\)", RegexOptions.Compiled);

        public static void InstantiateModsFolder(string modsFolder)
        {
            ModFolder = modsFolder;
            GetMechEngineerDefaults(modsFolder);
        }

        public static void PopulateGearData()
        {
            List<BasicFileData> gearData = ModJsonHandler.SearchFiles(ModFolder, "Weapon_*.json");
            gearData.AddRange(ModJsonHandler.SearchFiles(ModFolder, "Gear_*.json"));
            gearData.AddRange(ModJsonHandler.SearchFiles(ModFolder, "emod_*.json"));

            foreach(BasicFileData gear in gearData)
            {
                string gearId = gear.FileName.Replace(".json", "");

                GearData[gearId] = ParseEquipmentFile(gear);

                if(GearData[gearId].GearJsonDoc.RootElement.TryGetProperty("ComponentTags", out JsonElement componentTagsJson))
                    if (componentTagsJson.TryGetProperty("items", out JsonElement tagsList))
                    {
                        foreach (var tag in tagsList.EnumerateArray())
                        {
                            if (!GearTagsToGearIds.ContainsKey(tag.ToString()))
                                GearTagsToGearIds.TryAdd(tag.ToString(), new List<string>());
                            GearTagsToGearIds[tag.ToString()].Add(gearId);
                        }
                    }
            }
        }

        public static List<string> GetGearIdsWithGearTag(string gearTag)
        {
            if(GearTagsToGearIds.ContainsKey(gearTag))
                return GearTagsToGearIds[gearTag];
            return new List<string>();
        }

        public static bool TryGetEquipmentData(string gearId, out EquipmentData equipmentData)
        {
            if (GearData.TryGetValue(gearId, out equipmentData))
            {
                return true;
            }
            else
            {
                List<BasicFileData> gearFileData = ModJsonHandler.SearchFiles(ModFolder, $"{gearId}.json");
                if (gearFileData.Count > 1)
                {
                    Console.WriteLine($"Too many files found for {gearId}, trying to filter, otherwise using first file.");
                    gearFileData = gearFileData.FindAll((file) => !DirectoryExcludeRegex.IsMatch(file.Path));
                    if (gearFileData.Count > 1)
                        Console.WriteLine("KINDA FAILED TO FILTER! WOOPS!");
                }
                if (gearFileData.Count > 0)
                {
                    equipmentData = ParseEquipmentFile(gearFileData[0]);
                    
                    GearData[equipmentData.Id] = equipmentData;

                    return true;
                }
                else
                    Console.WriteLine($"NO GEAR FILE FOUND FOR {gearId} WOOPS!");
            }
            return false;
        }

        public static List<string> GetDefaultGearIdsForMechTags(List<string> tags)
        {
            List<string> output = new List<string>();
            foreach (string tag in tags)
            {
                if (MechTagsToGearIds.ContainsKey(tag))
                    output.AddRange(MechTagsToGearIds[tag]);
            }
            return output;
        }

        private static EquipmentData ParseEquipmentFile(BasicFileData gearFile)
        {
            string gearId = gearFile.FileName.Replace(".json", "");

            StreamReader reader = new StreamReader(gearFile.Path);

            string fileText = reader.ReadToEnd();
            JsonDocument gearJsonDoc = JsonDocument.Parse(fileText);

            bool haveCategories = false;
            JsonElement categoriesJson = new JsonElement();
            if (gearJsonDoc.RootElement.TryGetProperty("Custom", out JsonElement custom))
                haveCategories = custom.TryGetProperty("Category", out categoriesJson);

            EquipmentData equipmentData = new EquipmentData()
            {
                Id = ModJsonHandler.GetIdFromJsonDoc(gearJsonDoc),
                UIName = ModJsonHandler.GetUiNameFromJsonDoc(gearJsonDoc),
                Tonnage = gearJsonDoc.RootElement.GetProperty("Tonnage").GetDouble(),
                GearType = haveCategories ? DetermineGearCategory(gearId, fileText, categoriesJson) : DetermineGearCategory(gearId, fileText),
                GearJsonDoc = gearJsonDoc
            };

            if (MechTonnageCalculator.TryGetStructureWeightFactor(gearJsonDoc, out double tempStructureFactor))
                equipmentData.StructureFactor = tempStructureFactor;

            return equipmentData;
        }

        private static List<GearCategory> DetermineGearCategory(string itemId, string fileText, JsonElement? categories = null)
        {
            List<GearCategory> categoryList = new List<GearCategory>();

            if (categories != null && categories.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement category in categories.Value.EnumerateArray())
                {
                    if (category.TryGetProperty("CategoryID", out JsonElement categoryId))
                    {
                        switch (categoryId.ToString())
                        {
                            case "Structure":
                                categoryList.Add(GearCategory.Structure);
                                break;
                            case "Quirk":
                                categoryList.Add(GearCategory.Quirk);
                                break;
                            case "Heatsink":
                                categoryList.Add(GearCategory.Heatsink);
                                break;
                            case "Gyro":
                                categoryList.Add(GearCategory.Gyro);
                                break;
                            case "Cockpit":
                                categoryList.Add(GearCategory.Cockpit);
                                break;
                            case "LifeSupportA":
                                categoryList.Add(GearCategory.LifeSupportA);
                                break;
                            case "LifeSupportB":
                                categoryList.Add(GearCategory.LifeSupportB);
                                break;
                            case "WeaponAttachment":
                                categoryList.Add(GearCategory.WeaponAttachment);
                                break;
                            case "Armor":
                                categoryList.Add(GearCategory.Armor);
                                break;
                            case "EngineHeatBlock":
                                categoryList.Add(GearCategory.EngineHeatsinks);
                                break;
                            case "Cooling":
                                categoryList.Add(GearCategory.HeatsinkKit);
                                break;
                            case "EngineShield":
                                categoryList.Add(GearCategory.EngineShield);
                                break;
                            case "EngineCore":
                                categoryList.Add(GearCategory.EngineCore);
                                break;
                            case "w/s/m/melee":
                                categoryList.Add(GearCategory.MeleeWeapon);
                                break;
                        }
                    }
                }
            }

            if (itemId.StartsWith("Weapon"))
                categoryList.Add(GearCategory.Weapon);
            if (structureRegex.IsMatch(itemId) && !categoryList.Contains(GearCategory.Structure))
                categoryList.Add(GearCategory.Structure);
            if (armorRegex.IsMatch(itemId) && !categoryList.Contains(GearCategory.Armor))
                categoryList.Add(GearCategory.Armor);
            if (categoryList.Count == 0)
                categoryList.Add(GearCategory.None);

            return categoryList;
        }

        private static void GetMechEngineerDefaults(string modsFolder)
        {
            StreamReader defaultsReader = new StreamReader(modsFolder + "BT Advanced Core\\settings\\defaults\\Defaults_MechEngineer.json");
            JsonDocument mechEngDefaultsJson = JsonDocument.Parse(defaultsReader.ReadToEnd());

            foreach (JsonElement setting in mechEngDefaultsJson.RootElement.GetProperty("Settings").EnumerateArray())
            {
                if (setting.TryGetProperty("UnitTypes", out JsonElement tagGearDefList))
                {
                    foreach(JsonElement unitType in tagGearDefList.EnumerateArray())
                    {
                        JsonElement tagName = unitType.GetProperty("UnitType");

                        if (!MechTagsToGearIds.ContainsKey(tagName.ToString()))
                            MechTagsToGearIds[tagName.ToString()] = new List<string>();
                        foreach(JsonElement tagDefault in unitType.GetProperty("Defaults").EnumerateArray())
                        {
                            MechTagsToGearIds[tagName.ToString()].Add(tagDefault.GetProperty("DefID").ToString());
                        }
                    }
                }
            }
        }
    }
}