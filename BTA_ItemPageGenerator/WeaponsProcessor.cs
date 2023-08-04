using BT_JsonProcessingLibrary;
using System.Collections.Concurrent;
using System.Text.Json;

namespace BTA_ItemPageGenerator
{
    public static class WeaponsProcessor
    {
        private static ConcurrentDictionary<string, Dictionary<string,List<WeaponTableData>>> WeaponAttachmentUpgradesDict = new ConcurrentDictionary<string, Dictionary<string, List<WeaponTableData>>>();

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
            StreamWriter attachmentStreamWriter = new StreamWriter(".\\Output\\AttachmentsPageData.txt", false);

            List<string> sortedKeys = WeaponAttachmentUpgradesDict.Keys.ToList();
            sortedKeys.Sort();

            foreach (string attachmentGearId in sortedKeys)
            {
                MechGearHandler.TryGetEquipmentData(attachmentGearId, out EquipmentData attachmentData);

                attachmentStreamWriter.WriteLine($"=== {attachmentData.UIName} ===");
                attachmentStreamWriter.WriteLine();
                attachmentStreamWriter.WriteLine($"{ModJsonHandler.GetDescriptionDetailsFromJsonDoc(attachmentData.GearJsonDoc)}");
                attachmentStreamWriter.WriteLine();

                JsonElement attachmentDescriptionJson = attachmentData.GearJsonDoc.RootElement.GetProperty("Description");

                attachmentStreamWriter.WriteLine("{| class=\"wikitable\"");
                attachmentStreamWriter.WriteLine("! Manufacturer: ");
                attachmentStreamWriter.WriteLine($"| {attachmentDescriptionJson.GetProperty("Manufacturer").ToString()}");
                attachmentStreamWriter.WriteLine("|-");

                attachmentStreamWriter.WriteLine("! Tonnage: ");
                attachmentStreamWriter.WriteLine($"| {attachmentData.GearJsonDoc.RootElement.GetProperty("Tonnage").ToString()}");
                attachmentStreamWriter.WriteLine("|-");

                attachmentStreamWriter.WriteLine("! Critical Slots: ");
                attachmentStreamWriter.WriteLine($"| {attachmentData.GearJsonDoc.RootElement.GetProperty("InventorySize").ToString()}");
                attachmentStreamWriter.WriteLine("|-");

                attachmentStreamWriter.WriteLine("! Install Location:");
                attachmentStreamWriter.WriteLine($"| Any (same as Weapon)");
                attachmentStreamWriter.WriteLine("|-");

                attachmentStreamWriter.WriteLine("! Value: ");
                attachmentStreamWriter.WriteLine($"| {attachmentDescriptionJson.GetProperty("Cost").GetInt32().ToString("###,###,###")}");
                attachmentStreamWriter.WriteLine("|-");

                attachmentStreamWriter.WriteLine("! Gear ID: ");
                attachmentStreamWriter.WriteLine($"| {attachmentData.Id}");
                attachmentStreamWriter.WriteLine("|-");

                attachmentStreamWriter.WriteLine("|}");

                attachmentStreamWriter.WriteLine("<br/>");
                attachmentStreamWriter.WriteLine("'''Equipped Effects By Weapon:'''");

                attachmentStreamWriter.WriteLine(WeaponTableGenerator.OutputWeaponEntriesToTable(WeaponAttachmentUpgradesDict[attachmentGearId]));
                attachmentStreamWriter.WriteLine("<br/>");

                attachmentStreamWriter.WriteLine("'''Critical Effects:'''");
                attachmentStreamWriter.WriteLine("* DESTROYED: Equipped effects disabled");
                attachmentStreamWriter.WriteLine("<br/>");

                attachmentStreamWriter.WriteLine("<div class=\"toccolours mw-collapsible mw-collapsed\" style=\"max-width:1000px;\">");
                attachmentStreamWriter.WriteLine("<div style=\"font-weight:bold;line-height:1.6;\">'''Found On These 'Mechs: (Click Expand For List)'''</div>");
                attachmentStreamWriter.WriteLine("<div class=\"mw-collapsible-content\">");
                attachmentStreamWriter.WriteLine($"{{{{EquipmentMechs|{attachmentData.Id}}}}}");
                attachmentStreamWriter.WriteLine("</div></div>");
                attachmentStreamWriter.WriteLine();
                attachmentStreamWriter.WriteLine();
                attachmentStreamWriter.WriteLine();
            }

            attachmentStreamWriter.Close();
            attachmentStreamWriter.Dispose();
        }
    }
}
