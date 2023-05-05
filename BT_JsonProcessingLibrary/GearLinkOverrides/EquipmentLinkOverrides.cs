using BT_JsonProcessingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace UtilityClassLibrary.WikiLinkOverrides
{
    public static class EquipmentLinkOverrides
    {
        private static Dictionary<string, string> EquipmentPageOverridesList = new Dictionary<string, string>();

        public static void PopulateEquipmentOverrides()
        {
            EquipmentPageOverridesList = TextFileListProcessor.GetStringListFromFile(".\\GearLinkOverrides\\EquipmentPageOverridesList.txt").ToDictionary(
                (csvString) => csvString.Split(',')[0]
                , (csvString) => csvString.Split(',')[1]);
        }

        public static StoreEntry GetStoreDataForId(string id, string modsFolder)
        {
            BasicFileData equipmentFile = ModJsonHandler.SearchFiles(modsFolder, $"{id}.json")[0];
            JsonDocument equipmentJson = ModJsonHandler.GetJsonDocument(equipmentFile.Path);

            List<string> categoryIds = ModJsonHandler.GetAllCategoryIds(equipmentJson);

            if (categoryIds.Contains("Armor"))
                return GetStoreEntryDataWithNameSubTarget(equipmentJson, "Armor");

            if (categoryIds.Contains("ModularArmor"))
                return GetStoreEntryDataWithNameSubTarget(equipmentJson, "Modular_Armor");

            else if (categoryIds.Contains("Cockpit"))
                return GetStoreEntryDataWithUiNameSubTarget(equipmentJson, "Cockpit");

            else if (categoryIds.Contains("Heatsink") || categoryIds.Contains("Exchanger") || categoryIds.Contains("HeatBank") || categoryIds.Contains("EngineHeatBlock") || categoryIds.Contains("Cooling"))
                return GetStoreEntryDataWithUiNameSubTarget(equipmentJson, "Cooling");

            else if (categoryIds.Contains("EngineCore"))
                return GetStoreEntryDataWithSpecifiedSubTarget(equipmentJson, "Engines", "Engine Cores");

            else if (categoryIds.Contains("EngineShield"))
                return GetStoreEntryDataWithNameSubTarget(equipmentJson, "Engines");

            else if (categoryIds.Contains("Gyro"))
                return GetStoreEntryDataWithUiNameSubTarget(equipmentJson, "Mech_Gyros");

            else if (categoryIds.Contains("StandardJJ") || categoryIds.Contains("ImprovedJJ") || categoryIds.Contains("JumpTurbine") || categoryIds.Contains("QuadFrontLegJJ"))
                return GetStoreEntryDataWithUiNameSubTarget(equipmentJson, "Jump_Jets");

            else if (categoryIds.Contains("ECM"))
                return GetStoreEntryDataWithUiNameSubTarget(equipmentJson, "ECM");

            else if (categoryIds.Contains("Probe"))
                return GetStoreEntryDataWithUiNameSubTarget(equipmentJson, "Active_Probes");

            else if (categoryIds.Contains("ArmShoulder"))
                return GetStoreEntryDataWithUiNameSubTarget(equipmentJson, "Shoulder_Actuators");

            else if (categoryIds.Contains("ArmUpperActuator"))
                return GetStoreEntryDataWithUiNameSubTarget(equipmentJson, "Upper_Arm_Actuators");

            else if (categoryIds.Contains("ArmLowerActuator"))
                return GetStoreEntryDataWithUiNameSubTarget(equipmentJson, "Lower_Arm_Actuators");

            else if (categoryIds.Contains("ArmHandActuator"))
                return GetStoreEntryDataWithUiNameSubTarget(equipmentJson, "Hand_Actuators");

            else if (categoryIds.Contains("LegHip"))
                return GetStoreEntryDataWithUiNameSubTarget(equipmentJson, "Hip_Actuators");

            else if (categoryIds.Contains("LegUpperActuator"))
                return GetStoreEntryDataWithUiNameSubTarget(equipmentJson, "Upper_Leg_Actuators");

            else if (categoryIds.Contains("LegLowerActuator"))
                return GetStoreEntryDataWithUiNameSubTarget(equipmentJson, "Lower_Leg_Actuators");

            else if (categoryIds.Contains("LegFootActuator"))
                return GetStoreEntryDataWithUiNameSubTarget(equipmentJson, "Foot_Actuators");

            else if (categoryIds.Contains("WeaponAttachment"))
                return GetStoreEntryDataWithUiNameSubTarget(equipmentJson, "Weapon_Attachments");

            else if (categoryIds.Contains("AESArm") || categoryIds.Contains("AESLeg"))
                return GetStoreEntryDataWithUiNameSubTarget(equipmentJson, "Actuator_Enhancement_System");

            else if (categoryIds.Contains("CASE") || categoryIds.Contains("CASE2"))
                return GetStoreEntryDataWithUiNameSubTarget(equipmentJson, "CASE");

            else if (categoryIds.Contains("MASC"))
                return GetStoreEntryDataWithUiNameSubTarget(equipmentJson, "M.A.S.C.");

            else if (categoryIds.Contains("CombatShield"))
                return GetStoreEntryDataWithUiNameSubTarget(equipmentJson, "Shields");

            else if (categoryIds.Contains("HarJel"))
                return GetStoreEntryDataWithUiNameSubTarget(equipmentJson, "HarJel_Self-Repair_System");

            else if (categoryIds.Contains("C3Slave") || categoryIds.Contains("C3i") || categoryIds.Contains("C3Master"))
                return GetStoreEntryDataWithUiNameSubTarget(equipmentJson, "C3");

            else if (categoryIds.Contains("TSM") || categoryIds.Contains("ProtoTSM") || categoryIds.Contains("TSMOmni"))
                return GetStoreEntryDataWithUiNameSubTarget(equipmentJson, "T.S.M.");

            else if (categoryIds.Contains("Airdrop"))
                return GetStoreEntryDataWithUiNameSubTarget(equipmentJson, "Airdrop_Beacons");

            else
                return GetGenericEquipmentStoreData(equipmentJson);
        }

        private static StoreEntry GetStoreEntryDataWithNameSubTarget(JsonDocument equipJsonDoc, string pageTargetName)
        {
            string id = ModJsonHandler.GetIdFromJsonDoc(equipJsonDoc);
            string name = ModJsonHandler.GetNameFromJsonDoc(equipJsonDoc);
            string uiName = ModJsonHandler.GetUiNameFromJsonDoc(equipJsonDoc);

            return new StoreEntry
            {
                Id = id,
                Name = name,
                UiName = uiName,
                LinkPageTarget = pageTargetName,
                PageSubTarget = name,
                StoreHeading = StoreHeadingsGroup.Equipment
            };
        }

        private static StoreEntry GetStoreEntryDataWithUiNameSubTarget(JsonDocument equipJsonDoc, string pageTargetName)
        {
            string id = ModJsonHandler.GetIdFromJsonDoc(equipJsonDoc);
            string name = ModJsonHandler.GetNameFromJsonDoc(equipJsonDoc);
            string uiName = ModJsonHandler.GetUiNameFromJsonDoc(equipJsonDoc);

            return new StoreEntry
            {
                Id = id,
                Name = name,
                UiName = uiName,
                LinkPageTarget = pageTargetName,
                PageSubTarget = uiName,
                StoreHeading = StoreHeadingsGroup.Equipment
            };
        }

        private static StoreEntry GetStoreEntryDataWithSpecifiedSubTarget(JsonDocument equipJsonDoc, string pageTargetName, string pageSubTargetName)
        {
            string id = ModJsonHandler.GetIdFromJsonDoc(equipJsonDoc);
            string name = ModJsonHandler.GetNameFromJsonDoc(equipJsonDoc);
            string uiName = ModJsonHandler.GetUiNameFromJsonDoc(equipJsonDoc);

            return new StoreEntry
            {
                Id = id,
                Name = name,
                UiName = uiName,
                LinkPageTarget = pageTargetName,
                PageSubTarget = pageSubTargetName,
                StoreHeading = StoreHeadingsGroup.Equipment
            };
        }

        private static StoreEntry GetGenericEquipmentStoreData(JsonDocument equipJsonDoc)
        {
            string id = ModJsonHandler.GetIdFromJsonDoc(equipJsonDoc);
            string name = ModJsonHandler.GetNameFromJsonDoc(equipJsonDoc);
            string uiName = ModJsonHandler.GetUiNameFromJsonDoc(equipJsonDoc);

            return new StoreEntry
            {
                Id = id,
                Name = name,
                UiName = uiName,
                LinkPageTarget = uiName,
                StoreHeading = StoreHeadingsGroup.Equipment
            };
        }
    }
}
