using BT_JsonProcessingLibrary;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BTA_WikiTableGen
{
    internal static class MechGearHandler
    {
        private static string ModFolder;
        private static ConcurrentDictionary<string, EquipmentData> GearData = new ConcurrentDictionary<string, EquipmentData>();
        private static ConcurrentDictionary<string, List<string>> TagsToGearIds = new ConcurrentDictionary<string, List<string>>();

        public static Regex engineSizeRegex = new Regex(@"(?<=emod_engine_)(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static Regex structureRegex = new Regex(@"(\w*structureslots\w*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static Regex armorRegex = new Regex(@"(emod_armorslots\w*|Gear_armorslots\w*|Gear_Reflective_Coating)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static Regex DirectoryExcludeRegex = new Regex(@"(\\CustomAmmoCategories\\)", RegexOptions.Compiled);

        public static void InstantiateModsFolder(string modsFolder)
        {
            ModFolder = modsFolder;
            GetMechEngineerDefaults(modsFolder);
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
                    BasicFileData gearFile = gearFileData[0];
                    StreamReader reader = new StreamReader(gearFile.Path);

                    string fileText = reader.ReadToEnd();
                    JsonDocument gearJsonDoc = JsonDocument.Parse(fileText);

                    bool haveCategories = false;
                    JsonElement categoriesJson = new JsonElement();
                    if (gearJsonDoc.RootElement.TryGetProperty("Custom", out JsonElement custom))
                        haveCategories = custom.TryGetProperty("Category", out categoriesJson);

                    equipmentData = new EquipmentData()
                    {
                        Id = gearJsonDoc.RootElement.GetProperty("Description").GetProperty("Id").ToString(),
                        UIName = gearJsonDoc.RootElement.GetProperty("Description").GetProperty("UIName").ToString(),
                        Tonnage = gearJsonDoc.RootElement.GetProperty("Tonnage").GetDouble(),
                        GearType = haveCategories ? DetermineGearCategory(gearId, fileText, categoriesJson) : DetermineGearCategory(gearId, fileText),
                        GearJsonDoc = gearJsonDoc
                    };

                    if (MechTonnageCalculator.TryGetStructureWeightFactor(gearJsonDoc, out double tempStructureFactor))
                        equipmentData.StructureFactor = tempStructureFactor;

                    GearData[equipmentData.Id] = equipmentData;

                    return true;
                }
                else
                    Console.WriteLine($"NO GEAR FILE FOUND FOR {gearId} WOOPS!");
            }
            return false;
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

        public static List<string> GetDefaultGearIdsForTags(List<string> tags)
        {
            List<string> output = new List<string>();
            foreach (string tag in tags)
            {
                if (TagsToGearIds.ContainsKey(tag))
                    output.AddRange(TagsToGearIds[tag]);
            }
            return output;
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

                        if (!TagsToGearIds.ContainsKey(tagName.ToString()))
                            TagsToGearIds[tagName.ToString()] = new List<string>();
                        foreach(JsonElement tagDefault in unitType.GetProperty("Defaults").EnumerateArray())
                        {
                            TagsToGearIds[tagName.ToString()].Add(tagDefault.GetProperty("DefID").ToString());
                        }
                    }
                }
            }
        }
    }
}
