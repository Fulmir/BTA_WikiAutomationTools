using BT_JsonProcessingLibrary;
using System.Collections.Concurrent;
using System.Text.Json;

namespace BTA_WikiGeneration
{
    public static class WeaponAttachmentProcessor
    {
        private static ConcurrentDictionary<string, Dictionary<string,List<WeaponTableData>>> WeaponAttachmentUpgradesDict = new ConcurrentDictionary<string, Dictionary<string, List<WeaponTableData>>>();

        public static void GetAllGearAttachments(string modsFolder)
        {
            foreach(BasicFileData gearFile in ModJsonHandler.SearchFiles(modsFolder, "Gear_Attachment_*.json"))
            {
                StreamReader gearFileReader = new StreamReader(gearFile.Path);
                JsonDocument gearFileJson = JsonDocument.Parse(gearFileReader.ReadToEnd());

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
            StreamWriter attachmentWriter = new StreamWriter(".\\Output\\AttachmentsPageData.txt", false);

            List<string> sortedKeys = WeaponAttachmentUpgradesDict.Keys.ToList();
            sortedKeys.Sort();

            foreach (string attachmentGearId in sortedKeys)
            {
                MechGearHandler.TryGetEquipmentData(attachmentGearId, out EquipmentData attachmentData);

                attachmentWriter.WriteLine($"=== {attachmentData.UIName} ===");
                attachmentWriter.WriteLine();
                attachmentWriter.WriteLine($"{ModJsonHandler.GetDescriptionDetailsFromJsonDoc(attachmentData.GearJsonDoc)}");
                attachmentWriter.WriteLine();

                JsonElement attachmentDescriptionJson = attachmentData.GearJsonDoc.RootElement.GetProperty("Description");

                attachmentWriter.WriteLine("{| class=\"wikitable\"");
                attachmentWriter.WriteLine("! Manufacturer: ");
                attachmentWriter.WriteLine($"| {attachmentDescriptionJson.GetProperty("Manufacturer").ToString()}");
                attachmentWriter.WriteLine("|-");

                attachmentWriter.WriteLine("! Tonnage: ");
                attachmentWriter.WriteLine($"| {attachmentData.GearJsonDoc.RootElement.GetProperty("Tonnage").ToString()}");
                attachmentWriter.WriteLine("|-");

                attachmentWriter.WriteLine("! Critical Slots: ");
                attachmentWriter.WriteLine($"| {attachmentData.GearJsonDoc.RootElement.GetProperty("InventorySize").ToString()}");
                attachmentWriter.WriteLine("|-");

                attachmentWriter.WriteLine("! Install Location:");
                attachmentWriter.WriteLine($"| Any (same as Weapon)");
                attachmentWriter.WriteLine("|-");

                attachmentWriter.WriteLine("! Value: ");
                attachmentWriter.WriteLine($"| {attachmentDescriptionJson.GetProperty("Cost").GetInt32().ToString("###,###,###")}");
                attachmentWriter.WriteLine("|-");

                attachmentWriter.WriteLine("! Gear ID: ");
                attachmentWriter.WriteLine($"| {attachmentData.Id}");
                attachmentWriter.WriteLine("|-");

                attachmentWriter.WriteLine("|}");

                attachmentWriter.WriteLine("<br/>");
                attachmentWriter.WriteLine("'''Equipped Effects By Weapon:'''");

                attachmentWriter.WriteLine(WeaponTableGenerator.OutputWeaponEntriesToTable(WeaponAttachmentUpgradesDict[attachmentGearId]));
                attachmentWriter.WriteLine("<br/>");

                attachmentWriter.WriteLine("'''Critical Effects:'''");
                attachmentWriter.WriteLine("* DESTROYED: Equipped effects disabled");
                attachmentWriter.WriteLine("<br/>");

                attachmentWriter.WriteLine("<div class=\"toccolours mw-collapsible mw-collapsed\" style=\"max-width:1000px;\">");
                attachmentWriter.WriteLine("<div style=\"font-weight:bold;line-height:1.6;\">'''Found On These 'Mechs: (Click Expand For List)'''</div>");
                attachmentWriter.WriteLine("<div class=\"mw-collapsible-content\">");
                attachmentWriter.WriteLine($"{{{{EquipmentMechs|{attachmentData.Id}}}}}");
                attachmentWriter.WriteLine("</div></div>");
                attachmentWriter.WriteLine();
                attachmentWriter.WriteLine();
                attachmentWriter.WriteLine();
            }

            attachmentWriter.Close();
            attachmentWriter.Dispose();
        }
    }
}
