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

        public static Regex engineTypeRegex = new Regex(@"(emod_engine(?!_cooling|_\d+|.*size)([a-zA-Z3_]+))", RegexOptions.IgnoreCase  | RegexOptions.Compiled);
        public static Regex engineSizeRegex = new Regex(@"(?<=emod_engine_)(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static Regex heatsinkKitRegex = new Regex(@"(emod_kit\w*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static Regex heatsinkRegex = new Regex(@"(""CategoryID"": ""Heatsink"")", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static Regex structureRegex = new Regex(@"(\w*structureslots\w*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static Regex armorRegex = new Regex(@"(emod_armorslots\w*|Gear_armorslots\w*|Gear_Reflective_Coating)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static Regex gyroRegex = new Regex(@"(""CategoryID"": ""Gyro"")", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static Regex cockpitRegex = new Regex(@"(""CategoryID"": ""Cockpit"")", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static Regex lifeSupportRegex = new Regex(@"(""CategoryID"": ""LifeSupport)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static void InstantiateModsFolder(string modsFolder)
        {
            ModFolder = modsFolder;
        }

        public static bool TryGetEquipmentData(string gearId, out EquipmentData equipmentData)
        {
            if(GearData.TryGetValue(gearId, out equipmentData))
            {
                return true;
            } else
            {
                List<BasicFileData> gearFileData = ModJsonHandler.SearchFiles(ModFolder, $"{gearId}.json");
                if(gearFileData.Count > 1 )
                {
                    Console.WriteLine($"Too many files found for {gearId}, using first file.");
                } else if (gearFileData.Count > 0 )
                {
                    BasicFileData gearFile = gearFileData[0];
                    StreamReader reader = new StreamReader(gearFile.Path);

                    string fileText = reader.ReadToEnd();
                    JsonDocument gearJsonDoc = JsonDocument.Parse(fileText);

                    equipmentData = new EquipmentData()
                    {
                        Id = gearJsonDoc.RootElement.GetProperty("Description").GetProperty("Id").ToString(),
                        UIName = gearJsonDoc.RootElement.GetProperty("Description").GetProperty("UIName").ToString(),
                        Tonnage = gearJsonDoc.RootElement.GetProperty("Tonnage").GetDouble(),
                        GearType = DetermineGearCategory(gearId, fileText),
                        GearJsonDoc = gearJsonDoc
                    };

                    if(MechTonnageCalculator.TryGetStructureWeightFactor(gearJsonDoc, out double tempStructureFactor))
                        equipmentData.StructureFactor = tempStructureFactor;

                    GearData[equipmentData.Id] = equipmentData;

                    return true;
                }
            }
            return false;
        }

        private static List<GearCategory> DetermineGearCategory(string itemId, string fileText)
        {
            List<GearCategory> categoryList = new List<GearCategory>();

            if (itemId.StartsWith("Weapon"))
                categoryList.Add(GearCategory.Weapon);
            if (engineTypeRegex.IsMatch(itemId))
                categoryList.Add(GearCategory.Engine);
            if (engineSizeRegex.IsMatch(itemId))
                categoryList.Add(GearCategory.EngineCore);
            if (heatsinkKitRegex.IsMatch(itemId))
                categoryList.Add(GearCategory.HeatsinkKit);
            if (heatsinkRegex.IsMatch(fileText))
                categoryList.Add(GearCategory.Heatsink);
            if (structureRegex.IsMatch(itemId))
                categoryList.Add(GearCategory.Structure);
            if (cockpitRegex.IsMatch(fileText))
                categoryList.Add(GearCategory.Cockpit);
            if (lifeSupportRegex.IsMatch(fileText))
                categoryList.Add(GearCategory.LifeSupport);
            if (armorRegex.IsMatch(itemId))
                categoryList.Add(GearCategory.Armor);
            if (gyroRegex.IsMatch(fileText))
                categoryList.Add(GearCategory.Gyro);
            if (itemId.StartsWith("emod_engine_cooling_"))
                categoryList.Add(GearCategory.EngineHeatsinks);
            if (itemId.StartsWith("Gear"))
                categoryList.Add(GearCategory.Gear);
            if(categoryList.Count == 0)
                categoryList.Add(GearCategory.None);

            return categoryList;
        }
    }
}
