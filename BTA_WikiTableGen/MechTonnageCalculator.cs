using BT_JsonProcessingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BTA_WikiTableGen
{
    internal static class MechTonnageCalculator
    {
        static Regex EngineHeatsinksRegex = new Regex(@"(?<=""EngineHSFreeExt: )(\d)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static double GetCoreWeight(MechStats mechStats)
        {
            double baseChassisWeight = Math.Round((double)mechStats.MechTonnage / 10, 1);

            List<EquipmentData> allEquipment = new List<EquipmentData>();
            allEquipment.AddRange(mechStats.BaseGear);
            allEquipment.AddRange(mechStats.FixedGear);
            allEquipment.AddRange(mechStats.DefaultGear);

            EquipmentData? engineTypeData = null;
            EquipmentData? engineCoreData = null;
            EquipmentData? gyroData = null;
            EquipmentData? structureData = null;
            EquipmentData? engineHeatsinkData = null;

            List<string> coreGearIds = new List<string>();

            // Find engine type, engine size, gyro type, and structure data
            foreach (EquipmentData data in allEquipment)
            {
                if (data.Id.Equals(mechStats.EngineTypeId))
                {
                    engineTypeData = data;
                    coreGearIds.Add(data.Id);
                }
                else if (data.Id.Equals(mechStats.EngineCoreId))
                {
                    engineCoreData = data;
                    coreGearIds.Add(data.Id);
                }
                else if (data.Id.Equals(mechStats.GyroTypeId))
                {
                    gyroData = data;
                    coreGearIds.Add(data.Id);
                }
                else if (data.Id.Equals(mechStats.StructureTypeId))
                {
                    structureData = data;
                    coreGearIds.Add(data.Id);
                }
                else if (data.GearType.Contains(GearCategory.EngineHeatsinks))
                {
                    engineHeatsinkData = data;
                    coreGearIds.Add(data.Id);
                }
            }

            double realStructureWeight = baseChassisWeight;
            // Get real structure weight
            if (structureData != null && structureData.Value.StructureFactor.HasValue)
            {
                realStructureWeight = baseChassisWeight + CalculateStructureWeightAdjust(baseChassisWeight, (double)structureData.Value.StructureFactor);
                realStructureWeight = DoHalfTonRounding(realStructureWeight);
            }

            double gyroTonnage = 0;
            double engineTonnage = 0;
            int freeHeatsinkCount = 0;

            // Get Engine Base Weight
            // Calc and subtract Gyro weight
            if (engineCoreData != null)
            {
                gyroTonnage = Math.Round((double)mechStats.EngineSize / 100, 0, MidpointRounding.ToPositiveInfinity);
                engineTonnage = (double)engineCoreData?.Tonnage - gyroTonnage;

                // Get engine mod weight and adjust engine weight
                if (engineTypeData != null && TryGetEngineWeightFactor(engineTypeData.Value, out double engineFactor))
                {
                    engineTonnage *= engineFactor;
                    engineTonnage = DoHalfTonRounding(engineTonnage);
                }

                if (engineCoreData.Value.GearJsonDoc.RootElement.GetProperty("Custom").TryGetProperty("BonusDescriptions", out JsonElement engineBonuses))
                {
                    if (EngineHeatsinksRegex.IsMatch(engineBonuses.ToString()))
                        freeHeatsinkCount = Convert.ToInt32(EngineHeatsinksRegex.Match(engineBonuses.ToString()).Captures[0].Value);
                }
            }

            // Add any extra gyro weight and base gyro weight
            if (gyroData.HasValue && (gyroData.Value.Tonnage > 0 || gyroData.Value.StructureFactor.HasValue))
            {
                gyroTonnage += gyroData.Value.Tonnage;
                if (gyroData.Value.StructureFactor.HasValue)
                    gyroTonnage += DoHalfTonRounding(CalculateStructureWeightAdjust(baseChassisWeight, gyroData.Value.StructureFactor.Value));
            }

            double fixedGearWeight = 0;

            // Check if there is a fixed cockpit or Life Support so we don't double-count those...
            int cockpitCount = 0;
            int lifeSupportCount = 0;

            // Get and add weights for fixed equipment
            foreach (EquipmentData fixedGear in mechStats.FixedGear)
            {
                if (fixedGear.GearType.Contains(GearCategory.Cockpit))
                    cockpitCount++;
                if (fixedGear.GearType.Contains(GearCategory.LifeSupportA))
                    lifeSupportCount++;
                if (fixedGear.GearType.Contains(GearCategory.LifeSupportB))
                    lifeSupportCount++;

                if (!coreGearIds.Contains(fixedGear.Id) && (fixedGear.Tonnage != 0 || fixedGear.StructureFactor != null))
                {
                    if (fixedGear.Tonnage != 0)
                    {
                        if (fixedGear.GearType.Contains(GearCategory.Heatsink) && freeHeatsinkCount > 0)
                            freeHeatsinkCount--;
                        else
                            fixedGearWeight += fixedGear.Tonnage;
                    }
                    if (fixedGear.StructureFactor != null)
                        fixedGearWeight += DoHalfTonRounding(CalculateStructureWeightAdjust(baseChassisWeight, fixedGear.StructureFactor.Value));
                }
            }

            int eCoolingWeight = 0;

            if (engineHeatsinkData.HasValue)
                eCoolingWeight = (int)engineHeatsinkData.Value.Tonnage;

            return mechStats.MechTonnage - (realStructureWeight + gyroTonnage + engineTonnage + eCoolingWeight + fixedGearWeight + (1 - cockpitCount) + (2 - lifeSupportCount));
        }

        public static double GetBareWeight(MechStats mechStats)
        {
            double baseChassisWeight = Math.Round((double)mechStats.MechTonnage / 10, 1);

            EquipmentData? engineTypeData = null;
            EquipmentData? engineCoreData = null;
            EquipmentData? gyroData = null;
            EquipmentData? structureData = null;
            EquipmentData? engineHeatsinkData = null;

            List<string> coreGearIds = new List<string>();

            // Find engine type, engine size, gyro type, and structure data
            foreach (EquipmentData data in mechStats.FixedGear)
            {
                if (data.Id.Equals(mechStats.EngineTypeId))
                {
                    engineTypeData = data;
                    coreGearIds.Add(data.Id);
                }
                if (data.Id.Equals(mechStats.EngineCoreId))
                {
                    engineCoreData = data;
                    coreGearIds.Add(data.Id);
                }
                if (data.Id.Equals(mechStats.GyroTypeId))
                {
                    gyroData = data;
                    coreGearIds.Add(data.Id);
                }
                if (data.Id.Equals(mechStats.StructureTypeId))
                {
                    structureData = data;
                    coreGearIds.Add(data.Id);
                }
            }

            double realStructureWeight = baseChassisWeight;
            // Get real structure weight
            if (structureData != null && structureData.Value.StructureFactor.HasValue)
            {
                realStructureWeight = baseChassisWeight + CalculateStructureWeightAdjust(baseChassisWeight, (double)structureData.Value.StructureFactor);
                realStructureWeight = DoHalfTonRounding(realStructureWeight);
            }

            double gyroTonnage = 0;
            double engineTonnage = 0;
            int freeHeatsinkCount = 0;

            // Get Engine Base Weight
            // Calc and subtract Gyro weight
            if (engineCoreData != null)
            {
                gyroTonnage = Math.Round((double)mechStats.EngineSize / 100, 0, MidpointRounding.ToPositiveInfinity);
                engineTonnage = (double)engineCoreData?.Tonnage - gyroTonnage;

                // Get engine mod weight and adjust engine weight
                if (engineTypeData != null && TryGetEngineWeightFactor(engineTypeData.Value, out double engineFactor))
                {
                    engineTonnage *= engineFactor;
                    engineTonnage = DoHalfTonRounding(engineTonnage);
                }

                if (engineCoreData.Value.GearJsonDoc.RootElement.GetProperty("Custom").TryGetProperty("BonusDescriptions", out JsonElement engineBonuses))
                {
                    if (EngineHeatsinksRegex.IsMatch(engineBonuses.ToString()))
                        freeHeatsinkCount = Convert.ToInt32(EngineHeatsinksRegex.Match(engineBonuses.ToString()).Captures[0].Value);
                }
            }

            // Add any extra gyro weight and base gyro weight
            if (gyroData.HasValue && (gyroData.Value.Tonnage > 0 || gyroData.Value.StructureFactor.HasValue))
            {
                gyroTonnage += gyroData.Value.Tonnage;
                if (gyroData.Value.StructureFactor.HasValue)
                    gyroTonnage += DoHalfTonRounding(CalculateStructureWeightAdjust(baseChassisWeight, gyroData.Value.StructureFactor.Value));
            }

            double fixedGearWeight = 0;

            // Check if there is a fixed cockpit or Life Support so we don't double-count those...
            int cockpitCount = 0;
            int lifeSupportCount = 0;

            // Get and add weights for fixed equipment
            foreach (EquipmentData fixedGear in mechStats.FixedGear)
            {
                if (fixedGear.GearType.Contains(GearCategory.Cockpit))
                    cockpitCount++;
                if (fixedGear.GearType.Contains(GearCategory.LifeSupportA))
                    lifeSupportCount++;
                if (fixedGear.GearType.Contains(GearCategory.LifeSupportB))
                    lifeSupportCount++;

                if (!coreGearIds.Contains(fixedGear.Id) && (fixedGear.Tonnage > 0 || fixedGear.StructureFactor != null))
                {
                    if (fixedGear.Tonnage > 0)
                    {
                        if (fixedGear.GearType.Contains(GearCategory.Heatsink) && freeHeatsinkCount > 0)
                            freeHeatsinkCount--;
                        else
                            fixedGearWeight += fixedGear.Tonnage;
                    }
                    if (fixedGear.StructureFactor != null)
                        fixedGearWeight += DoHalfTonRounding(CalculateStructureWeightAdjust(baseChassisWeight, fixedGear.StructureFactor.Value));
                }
            }

            int eCoolingWeight = 0;

            if (engineHeatsinkData.HasValue)
                eCoolingWeight = (int)engineHeatsinkData.Value.Tonnage;

            return mechStats.MechTonnage - (realStructureWeight + gyroTonnage + engineTonnage + eCoolingWeight + fixedGearWeight + (1 - cockpitCount) + (2 - lifeSupportCount));
        }

        private static bool TryGetEngineWeightFactor(EquipmentData engineTypeData, out double engineFactor)
        {
            engineFactor = 1;

            if (engineTypeData.GearJsonDoc.RootElement.TryGetProperty("Custom", out JsonElement customVals))
            {
                if (customVals.TryGetProperty("Weights", out JsonElement weights))
                {
                    if (weights.TryGetProperty("EngineFactor", out JsonElement engineFactorJson))
                    {
                        engineFactor = engineFactorJson.GetDouble();
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool TryGetStructureWeightFactor(JsonDocument gearData, out double structureFactor)
        {
            structureFactor = 1;

            if (gearData.RootElement.TryGetProperty("Custom", out JsonElement customVals))
            {
                if (customVals.TryGetProperty("Weights", out JsonElement weights))
                {
                    if (weights.TryGetProperty("StructureFactor", out JsonElement engineFactorJson))
                    {
                        structureFactor = engineFactorJson.GetDouble();
                        return true;
                    }
                }
            }

            return false;
        }

        private static double CalculateStructureWeightAdjust(double baseStructureWeight, double structureFactor)
        {
            return baseStructureWeight * (structureFactor - 1);
        }

        private static double DoHalfTonRounding(double unroundedWeight)
        {
            return Math.Round(unroundedWeight * 2, 0, MidpointRounding.ToPositiveInfinity) / 2;
        }
    }
}
