using BT_JsonProcessingLibrary;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BTA_WikiGeneration
{
    internal class GearProcessor
    {
        private static ConcurrentDictionary<string, Dictionary<string, List<WeaponTableData>>> GearDict = new ConcurrentDictionary<string, Dictionary<string, List<WeaponTableData>>>();

        // TODO: FINISH THIS CLASS
        public static void GetAllGear(string modsFolder)
        {
            foreach (BasicFileData gearFile in ModJsonHandler.SearchFiles(modsFolder, new List<string>{ "Gear_*.json", "emod_*.json"}))
            {
                StreamReader gearFileReader = new StreamReader(gearFile.Path);
                JsonDocument gearFileJson = JsonDocument.Parse(gearFileReader.ReadToEnd());

                string gearId = ModJsonHandler.GetIdFromJsonDoc(gearFileJson);

                if (!GearDict.ContainsKey(gearId))
                    GearDict.TryAdd(gearId, new Dictionary<string, List<WeaponTableData>>());

                List<string> upgradeDefs = new List<string>();

                foreach (JsonElement upgradeId in gearFileJson.RootElement.GetProperty("Custom").GetProperty("AddonReference").GetProperty("WeaponAddonIds").EnumerateArray())
                {
                    string upgradeIdString = upgradeId.ToString();
                    BasicFileData upgradeDefFile = ModJsonHandler.SearchFiles(modsFolder, upgradeIdString + ".json")[0];

                    if (!GearDict[gearId].ContainsKey(upgradeIdString))
                        GearDict[gearId].Add(upgradeIdString, new List<WeaponTableData>());

                    StreamReader upgradeReader = new StreamReader(upgradeDefFile.Path);
                    JsonDocument upgradeFileJson = JsonDocument.Parse(upgradeReader.ReadToEnd());

                    foreach (JsonElement upgradeTag in upgradeFileJson.RootElement.GetProperty("targetComponentTags").EnumerateArray())
                    {
                        foreach (JsonElement upgradeMode in upgradeFileJson.RootElement.GetProperty("modes").EnumerateArray())
                            GearDict[gearId][upgradeIdString].Add(new WeaponTableData(upgradeTag.ToString(), upgradeMode));
                    }
                }
            }
        }

        public static void PrintGearEntriesToFile()
        {
            StreamWriter weaponWriter = new StreamWriter(".\\Output\\AttachmentsPageData.txt", false);

            List<string> sortedKeys = GearDict.Keys.ToList();
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

                weaponWriter.WriteLine(WeaponTableGenerator.OutputWeaponEntriesToTable(GearDict[attachmentGearId]));
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
