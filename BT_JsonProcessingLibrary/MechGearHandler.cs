using BT_JsonProcessingLibrary;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Formats.Tar;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UtilityClassLibrary;

namespace BT_JsonProcessingLibrary
{
    public static class MechGearHandler
    {
        private static string ModFolder;
        private static ConcurrentDictionary<string, EquipmentData> GearData = new ConcurrentDictionary<string, EquipmentData>();
        private static ConcurrentDictionary<string, List<string>> MechTagsToGearIds = new ConcurrentDictionary<string, List<string>>();
        private static ConcurrentDictionary<string, List<string>> GearTagsToGearIds = new ConcurrentDictionary<string, List<string>>();

        private static ConcurrentDictionary<string, List<string>> WeaponCategoriesToIds = new ConcurrentDictionary<string, List<string>>();
        private static ConcurrentDictionary<string, List<string>> AmmoBoxCategoriesToIds = new ConcurrentDictionary<string, List<string>>();

        private static Dictionary<string, string> AmmoBoxCategoriesToDisplayNames = new Dictionary<string, string>();
        private static Dictionary<string, string> WeaponCategoriesToDisplayNames = new Dictionary<string, string>();

        public static Regex engineSizeRegex = new Regex(@"(?<=emod_engine_)(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static Regex structureRegex = new Regex(@"(\w*structureslots\w*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static Regex armorRegex = new Regex(@"(emod_armorslots\w*|Gear_armorslots\w*|Gear_Reflective_Coating)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static Regex DirectoryExcludeRegex = new Regex(@"(\\CustomAmmoCategories\\)", RegexOptions.Compiled);

        public static void InstantiateModsFolder(string modsFolder)
        {
            ModFolder = modsFolder;
            GetMechEngineerDefaults(modsFolder);
            GetWeaponCategories(modsFolder);
            GetAmmoCategories(modsFolder);
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
                if(GearData[gearId].GearType.Contains(GearCategory.Weapon))
                {
                    foreach (string categoryId in GearData[gearId].GearCategories)
                    {
                        if(WeaponCategoriesToDisplayNames.ContainsKey(categoryId))
                            WeaponCategoriesToIds[categoryId].Add(gearId);
                    }
                }
                if (GearData[gearId].GearType.Contains(GearCategory.AmmoBox))
                {
                    foreach (string categoryId in GearData[gearId].GearCategories)
                    {
                        if (AmmoBoxCategoriesToDisplayNames.ContainsKey(categoryId))
                            AmmoBoxCategoriesToIds[categoryId].Add(gearId);
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
                    Logging.AddLogToQueue($"Too many files found for {gearId}, trying to filter, otherwise using first file.", LogLevel.Reporting, LogCategories.Gear);
                    gearFileData = gearFileData.FindAll((file) => !DirectoryExcludeRegex.IsMatch(file.Path));
                    if (gearFileData.Count > 1)
                        Logging.AddLogToQueue($"FAILED TO FILTER GEAR FILES {gearId}! WOOPS!", LogLevel.Warning, LogCategories.Immediate);
                }
                if (gearFileData.Count > 0)
                {
                    equipmentData = ParseEquipmentFile(gearFileData[0]);
                    
                    GearData[equipmentData.Id] = equipmentData;

                    return true;
                }
                else
                    Logging.AddLogToQueue($"NO GEAR FILE FOUND FOR: {gearId}", LogLevel.Error, LogCategories.Gear);
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

            return new EquipmentData(gearJsonDoc);
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

        private static void GetWeaponCategories(string modsFolder)
        {
            StreamReader weaponCategoriesReader = new StreamReader(modsFolder + "BT Advanced Core\\settings\\categories\\Categories_Weapons.json");
            JsonDocument mechEngDefaultsJson = JsonDocument.Parse(weaponCategoriesReader.ReadToEnd());

            foreach (JsonElement category in mechEngDefaultsJson.RootElement.GetProperty("Settings").EnumerateArray())
            {
                if (category.TryGetProperty("Name", out JsonElement categoryName))
                {
                    JsonElement displayName = category.GetProperty("DisplayName");

                    AmmoBoxCategoriesToDisplayNames[categoryName.ToString()] = displayName.ToString();
                    AmmoBoxCategoriesToIds[categoryName.ToString()] = new List<string>();
                }
            }
        }

        private static void GetAmmoCategories(string modsFolder)
        {
            StreamReader ammoCategoriesReader = new StreamReader(modsFolder + "BT Advanced Core\\settings\\categories\\Categories_Ammo.json");
            JsonDocument mechEngDefaultsJson = JsonDocument.Parse(ammoCategoriesReader.ReadToEnd());

            foreach (JsonElement category in mechEngDefaultsJson.RootElement.GetProperty("Settings").EnumerateArray())
            {
                if (category.TryGetProperty("Name", out JsonElement categoryName))
                {
                    JsonElement displayName = category.GetProperty("DisplayName");

                    WeaponCategoriesToDisplayNames[categoryName.ToString()] = displayName.ToString();
                    WeaponCategoriesToIds[categoryName.ToString()] = new List<string>();
                }
            }
        }
    }
}
