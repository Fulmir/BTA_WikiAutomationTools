using BT_JsonProcessingLibrary;
using System.Collections.Concurrent;
using System.Text.Json;

namespace BTA_WikiGeneration
{
    public static class WeaponsProcessor
    {
        private static ConcurrentDictionary<string, Dictionary<string,List<WeaponTableData>>> WeaponAttachmentUpgradesDict = new ConcurrentDictionary<string, Dictionary<string, List<WeaponTableData>>>();

        // TODO: FINISH THIS CLASS
        public static void GetAllWeapons(string modsFolder)
        {
            foreach(BasicFileData weaponFile in ModJsonHandler.SearchFiles(modsFolder, "Weapon_*.json"))
            {
                StreamReader weaponFileReader = new StreamReader(weaponFile.Path);
                JsonDocument gearFileJson = JsonDocument.Parse(weaponFileReader.ReadToEnd());

                string gearId = ModJsonHandler.GetIdFromJsonDoc(gearFileJson);

                if(!WeaponAttachmentUpgradesDict.ContainsKey(gearId))
                    WeaponAttachmentUpgradesDict.TryAdd(gearId, new Dictionary<string, List<WeaponTableData>>());

                List<string> upgradeDefs = new List<string>();

                foreach(JsonElement upgradeId in gearFileJson.RootElement.GetProperty("Custom").GetProperty("AddonReference").GetProperty("WeaponAddonIds").EnumerateArray())
                {
                    string upgradeIdString = upgradeId.ToString();
                    BasicFileData upgradeDefFile = ModJsonHandler.SearchFiles(modsFolder, upgradeIdString + ".json")[0];

                    if (!WeaponAttachmentUpgradesDict[gearId].ContainsKey(upgradeIdString))
                        WeaponAttachmentUpgradesDict[gearId].Add(upgradeIdString, new List<WeaponTableData>());

                    StreamReader upgradeReader = new StreamReader(upgradeDefFile.Path);
                    JsonDocument upgradeFileJson = JsonDocument.Parse(upgradeReader.ReadToEnd());

                    foreach(JsonElement upgradeTag in upgradeFileJson.RootElement.GetProperty("targetComponentTags").EnumerateArray())
                    {
                        foreach (JsonElement upgradeMode in upgradeFileJson.RootElement.GetProperty("modes").EnumerateArray())
                            WeaponAttachmentUpgradesDict[gearId][upgradeIdString].Add(new WeaponTableData(upgradeTag.ToString(), upgradeMode));
                    }
                }
            }
        }

        public static void PrintGearEntriesToFile()
        {
            StreamWriter weaponWriter = new StreamWriter(".\\Output\\AttachmentsPageData.txt", false);

            List<string> sortedKeys = WeaponAttachmentUpgradesDict.Keys.ToList();
            sortedKeys.Sort();

            foreach (string attachmentGearId in sortedKeys)
            {
                MechGearHandler.TryGetEquipmentData(attachmentGearId, out EquipmentData attachmentData);

                weaponWriter.WriteLine($"=== {attachmentData.UIName} ===");
                weaponWriter.WriteLine();
                weaponWriter.WriteLine($"{ModJsonHandler.GetDescriptionDetailsFromJsonDoc(attachmentData.GearJsonDoc)}");
                weaponWriter.WriteLine();

                JsonElement attachmentDescriptionJson = attachmentData.GearJsonDoc.RootElement.GetProperty("Description");

                weaponWriter.WriteLine("{| class=\"wikitable\"");
                weaponWriter.WriteLine("! Manufacturer: ");
                weaponWriter.WriteLine($"| {attachmentDescriptionJson.GetProperty("Manufacturer").ToString()}");
                weaponWriter.WriteLine("|-");

                weaponWriter.WriteLine("! Tonnage: ");
                weaponWriter.WriteLine($"| {attachmentData.GearJsonDoc.RootElement.GetProperty("Tonnage").ToString()}");
                weaponWriter.WriteLine("|-");

                weaponWriter.WriteLine("! Critical Slots: ");
                weaponWriter.WriteLine($"| {attachmentData.GearJsonDoc.RootElement.GetProperty("InventorySize").ToString()}");
                weaponWriter.WriteLine("|-");

                weaponWriter.WriteLine("! Install Location:");
                weaponWriter.WriteLine($"| Any (same as Weapon)");
                weaponWriter.WriteLine("|-");

                weaponWriter.WriteLine("! Value: ");
                weaponWriter.WriteLine($"| {attachmentDescriptionJson.GetProperty("Cost").GetInt32().ToString("###,###,###")}");
                weaponWriter.WriteLine("|-");

                weaponWriter.WriteLine("! Gear ID: ");
                weaponWriter.WriteLine($"| {attachmentData.Id}");
                weaponWriter.WriteLine("|-");

                weaponWriter.WriteLine("|}");

                weaponWriter.WriteLine("<br/>");
                weaponWriter.WriteLine("'''Equipped Effects By Weapon:'''");

                weaponWriter.WriteLine(WeaponTableGenerator.OutputWeaponEntriesToTable(WeaponAttachmentUpgradesDict[attachmentGearId]));
                weaponWriter.WriteLine("<br/>");

                weaponWriter.WriteLine("'''Critical Effects:'''");
                weaponWriter.WriteLine("* DESTROYED: Equipped effects disabled");
                weaponWriter.WriteLine("<br/>");

                weaponWriter.WriteLine("<div class=\"toccolours mw-collapsible mw-collapsed\" style=\"max-width:1000px;\">");
                weaponWriter.WriteLine("<div style=\"font-weight:bold;line-height:1.6;\">'''Found On These 'Mechs: (Click Expand For List)'''</div>");
                weaponWriter.WriteLine("<div class=\"mw-collapsible-content\">");
                weaponWriter.WriteLine($"{{{{EquipmentMechs|{attachmentData.Id}}}}}");
                weaponWriter.WriteLine("</div></div>");
                weaponWriter.WriteLine();
                weaponWriter.WriteLine();
                weaponWriter.WriteLine();
            }

            weaponWriter.Close();
            weaponWriter.Dispose();
        }
    }
}
