using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UtilityClassLibrary.WikiLinkOverrides;

namespace BT_JsonProcessingLibrary
{
    public static class WeaponTableGenerator
    {
        public static string OutputWeaponEntriesToTable(Dictionary<string, List<WeaponTableData>> upgradeTagToUpgradeData)
        {
            StringBuilder weaponUpgradeTableBuilder = new StringBuilder();

            foreach (string upgradeTag in upgradeTagToUpgradeData.Keys)
            {
                WeaponTableData headerData = new WeaponTableData();

                Dictionary<EquipmentData, List<WeaponTableData>> gearToUpgradeModeDict = new Dictionary<EquipmentData, List<WeaponTableData>>();

                WeaponTableData? baseTableData = null;

                foreach(WeaponTableData upgradeTableData in upgradeTagToUpgradeData[upgradeTag])
                {
                    List<string> gearTargetIds = MechGearHandler.GetGearIdsWithGearTag(upgradeTableData.Id);
                    headerData = headerData + upgradeTableData;

                    if(baseTableData == null)
                    {
                        baseTableData = upgradeTableData;
                        // Need to print gear names but there will be multiple gear names per entry... probably a dictionary? OOF.
                        //weaponUpgradeTableBuilder.Append(GetTableRow(upgradeTableData, false, ))
                        foreach(string gearTargetId in gearTargetIds)
                        {
                            MechGearHandler.TryGetEquipmentData(gearTargetId, out EquipmentData gearData);
                            if (!gearToUpgradeModeDict.ContainsKey(gearData))
                                gearToUpgradeModeDict.Add(gearData, new List<WeaponTableData>());
                            gearToUpgradeModeDict[gearData].Add(upgradeTableData);
                        }
                    }
                    else
                    {
                        if(upgradeTableData != baseTableData)
                        {
                            foreach (string gearTargetId in gearTargetIds)
                            {
                                MechGearHandler.TryGetEquipmentData(gearTargetId, out EquipmentData gearData);
                                gearToUpgradeModeDict[gearData].Add(upgradeTableData);
                            }
                        }
                    }
                }

                weaponUpgradeTableBuilder.Append(GetTableHeader(false, "sortable", "text-align: center;", headerData));

                List<EquipmentData> sortedGearData = gearToUpgradeModeDict.Keys.ToList();
                sortedGearData.Sort();

                foreach (EquipmentData gearData in sortedGearData)
                {
                    foreach (WeaponTableData weaponModData in gearToUpgradeModeDict[gearData])
                    {
                        weaponUpgradeTableBuilder.Append(GetTableRow(weaponModData, false, gearData.UIName));
                    }
                }
                weaponUpgradeTableBuilder.AppendLine("|}");
                weaponUpgradeTableBuilder.AppendLine("</br>");
            }

            return weaponUpgradeTableBuilder.ToString();
        }

        private static string GetTableHeader(bool weaponEntryHeader, string additionalTableClasses = "", string additionalTableStyles = "", WeaponTableData? weaponTableData = null)
        {
            StringBuilder headingLineOne = new StringBuilder();
            StringBuilder headingLineTwo = new StringBuilder();

            headingLineOne.AppendLine($"{{| class=\"wikitable {additionalTableClasses}\"{(String.IsNullOrEmpty(additionalTableStyles) ? "" : (" style=\"" + additionalTableStyles + "\""))}");
            headingLineOne.AppendLine("! ");
            headingLineTwo.AppendLine("! <small>Name</small>");

            if (weaponEntryHeader || (weaponTableData.HasValue && weaponTableData.Value.AmmoTypeString != null))
            {
                headingLineOne.AppendLine("! ");
                headingLineTwo.AppendLine("! <small>Ammo</small>");
            }
            if (weaponEntryHeader || (weaponTableData.HasValue && weaponTableData.Value.HardpointType != null))
            {
                headingLineOne.AppendLine("! ");
                headingLineTwo.AppendLine("! <small>Hardpoint</small>");
            }

            if (weaponEntryHeader || (weaponTableData.HasValue && (weaponTableData.Value.WeaponTonnage.HasValue || weaponTableData.Value.WeaponSlots.HasValue)))
            {
                headingLineOne.AppendLine($"! colspan=\"{(weaponEntryHeader ? 2 : 
                    (0 + 
                    (weaponTableData.Value.WeaponTonnage.HasValue ? 1 : 0) + 
                    (weaponTableData.Value.WeaponSlots.HasValue ? 1 : 0)))}\" |Tonnage/Size");
                if (weaponEntryHeader || weaponTableData.Value.WeaponTonnage.HasValue)
                    headingLineTwo.AppendLine("! <small>Tonnage</small>");
                if(weaponEntryHeader || weaponTableData.Value.WeaponSlots.HasValue)
                    headingLineTwo.AppendLine("! <small>Slots</small>");
            }

            if (weaponEntryHeader || (weaponTableData.HasValue && (weaponTableData.Value.DamageNormal.HasValue || weaponTableData.Value.DamageHeat.HasValue || weaponTableData.Value.DamageStab.HasValue || weaponTableData.Value.DamageStructure.HasValue)))
            {
                headingLineOne.AppendLine($"! colspan=\"{(weaponEntryHeader ? 3 : 
                    (0 + 
                    (weaponTableData.Value.DamageNormal.HasValue ? 1 : 0) + 
                    (weaponTableData.Value.DamageHeat.HasValue ? 1 : 0) + 
                    (weaponTableData.Value.DamageStab.HasValue ? 1 : 0) + 
                    (weaponTableData.Value.DamageStructure.HasValue ? 1 : 0)))}\" |Damage");
                if (weaponEntryHeader || weaponTableData.Value.DamageNormal.HasValue)
                    headingLineTwo.AppendLine("! <small>Normal</small>");
                if (weaponEntryHeader || weaponTableData.Value.DamageHeat.HasValue)
                    headingLineTwo.AppendLine("! <small>Heat</small>");
                if (weaponEntryHeader || weaponTableData.Value.DamageStab.HasValue)
                    headingLineTwo.AppendLine("! <small>Stab</small>");
                if (!weaponEntryHeader && weaponTableData.Value.DamageStructure.HasValue)
                    headingLineTwo.AppendLine("! <small>Structure</small>");
            }

            if (weaponEntryHeader || (weaponTableData.HasValue && (weaponTableData.Value.Shots.HasValue || weaponTableData.Value.Projectiles.HasValue || weaponTableData.Value.Heat.HasValue || weaponTableData.Value.Recoil.HasValue)))
            {
                headingLineOne.AppendLine($"! colspan=\"{(weaponEntryHeader ? 4 : 
                    (0 + 
                    (weaponTableData.Value.Shots.HasValue ? 1 : 0) + 
                    (weaponTableData.Value.Projectiles.HasValue ? 1 : 0) + 
                    (weaponTableData.Value.Heat.HasValue ? 1 : 0) + 
                    (weaponTableData.Value.Recoil.HasValue ? 1 : 0)))}\" |Per salvo");
                if (weaponEntryHeader || weaponTableData.Value.Shots.HasValue)
                    headingLineTwo.AppendLine("! <small>Shots</small>");
                if (weaponEntryHeader || weaponTableData.Value.Projectiles.HasValue)
                    headingLineTwo.AppendLine("! <small>Projectiles</small>");
                if (weaponEntryHeader || weaponTableData.Value.Heat.HasValue)
                    headingLineTwo.AppendLine("! <small>Heat</small>");
                if (!weaponEntryHeader && weaponTableData.Value.Recoil.HasValue)
                    headingLineTwo.AppendLine("! <small>Recoil</small>");
            }

            if (weaponEntryHeader || (weaponTableData.HasValue && (weaponTableData.Value.AccuracyMod.HasValue || weaponTableData.Value.DirectFireAccuracy.HasValue || weaponTableData.Value.EvasionIgnored.HasValue || weaponTableData.Value.CritChanceBonus.HasValue || weaponTableData.Value.DirectFireAccuracy.HasValue)))
            {
                headingLineOne.AppendLine($"! colspan=\"{(weaponEntryHeader ? 3 : 
                    (0 + 
                    (weaponTableData.Value.AccuracyMod.HasValue ? 1 : 0) + 
                    (weaponTableData.Value.EvasionIgnored.HasValue ? 1 : 0) + 
                    (weaponTableData.Value.CritChanceBonus.HasValue ? 1 : 0) + 
                    (weaponTableData.Value.DirectFireAccuracy.HasValue ? 1 : 0) + 
                    (weaponTableData.Value.ClusteringMod.HasValue ? 1 : 0)))}\" |Modifiers");
                if (weaponEntryHeader || weaponTableData.Value.AccuracyMod.HasValue)
                    headingLineTwo.AppendLine("! <small>Accuracy</small>");
                if (weaponEntryHeader || weaponTableData.Value.DirectFireAccuracy.HasValue)
                    headingLineTwo.AppendLine("! <small>Direct Fire Acc</small>");
                if (!weaponEntryHeader && weaponTableData.Value.ClusteringMod.HasValue)
                    headingLineTwo.AppendLine("! <small>Clustering Mod</small>");
                if (weaponEntryHeader || weaponTableData.Value.EvasionIgnored.HasValue)
                    headingLineTwo.AppendLine("! <small>Evasion Ignored</small>");
                if (!weaponEntryHeader && weaponTableData.Value.CritChanceBonus.HasValue)
                    headingLineTwo.AppendLine("! <small>Bonus Crit Chance</small>");
            }

            if (weaponEntryHeader || (weaponTableData.HasValue && (weaponTableData.Value.MinRange.HasValue || weaponTableData.Value.ShortRange.HasValue || weaponTableData.Value.MediumRange.HasValue || weaponTableData.Value.LongRange.HasValue || weaponTableData.Value.MaxRange.HasValue)))
            {
                headingLineOne.AppendLine($"! colspan=\"{(weaponEntryHeader ? 5 : 
                    (0 + 
                    (weaponTableData.Value.MinRange.HasValue ? 1 : 0) + 
                    (weaponTableData.Value.ShortRange.HasValue ? 1 : 0) + 
                    (weaponTableData.Value.MediumRange.HasValue ? 1 : 0) + 
                    (weaponTableData.Value.LongRange.HasValue ? 1 : 0) + 
                    (weaponTableData.Value.MaxRange.HasValue ? 1 : 0)))}\" |Range");
                if (weaponEntryHeader || weaponTableData.Value.MinRange.HasValue)
                    headingLineTwo.AppendLine("! <small>Min</small>");
                if (weaponEntryHeader || weaponTableData.Value.ShortRange.HasValue)
                    headingLineTwo.AppendLine("! <small>Short</small>");
                if (weaponEntryHeader || weaponTableData.Value.MediumRange.HasValue)
                    headingLineTwo.AppendLine("! <small>Medium</small>");
                if (!weaponEntryHeader && weaponTableData.Value.LongRange.HasValue)
                    headingLineTwo.AppendLine("! <small>Long</small>");
                if (!weaponEntryHeader && weaponTableData.Value.MaxRange.HasValue)
                    headingLineTwo.AppendLine("! <small>Max</small>");
            }

            if (weaponEntryHeader || (weaponTableData.HasValue && weaponTableData.Value.FiresInMelee.HasValue))
            {
                headingLineOne.AppendLine("! ");
                headingLineTwo.AppendLine("! <small>Fires In Melee</small>");
            }

            if (weaponEntryHeader || (weaponTableData.HasValue && weaponTableData.Value.Bonuses != null && weaponTableData.Value.Bonuses.Count() > 0))
            {
                headingLineOne.AppendLine("! ");
                headingLineTwo.AppendLine("! <small>Additional Info</small>");
            }

            headingLineOne.AppendLine("|-");
            headingLineTwo.AppendLine("|-");

            return headingLineOne.ToString() + headingLineTwo.ToString();
        }

        private static string GetTableRow(WeaponTableData weaponTableData, bool weaponEntryLine = true, string alternateRowName = "")
        {
            StringBuilder weaponDataRowBuilder = new StringBuilder();

            weaponDataRowBuilder.AppendLine($"| {(weaponEntryLine ? weaponTableData.UiName : alternateRowName)}");

            if (weaponEntryLine || weaponTableData.AmmoTypeString != null)
            {
                AmmoBoxLinkOverrides.TryGetCategoryLink(weaponTableData.AmmoTypeString, out string categoryLink);
                weaponDataRowBuilder.AppendLine($"| {categoryLink}");
            }
            if (weaponEntryLine || weaponTableData.HardpointType != null)
                weaponDataRowBuilder.AppendLine($"| {weaponTableData.HardpointType}");

            if (weaponEntryLine || weaponTableData.WeaponTonnage.HasValue)
                weaponDataRowBuilder.AppendLine($"| {weaponTableData.WeaponTonnage}");
            if (weaponEntryLine || weaponTableData.WeaponSlots.HasValue)
                weaponDataRowBuilder.AppendLine($"| {weaponTableData.WeaponSlots}");

            if (weaponEntryLine || weaponTableData.DamageNormal.HasValue)
                weaponDataRowBuilder.AppendLine($"| {weaponTableData.DamageNormal?.ToString("+#;-#;0")}");
            if (weaponEntryLine || weaponTableData.DamageHeat.HasValue)
                weaponDataRowBuilder.AppendLine($"| {weaponTableData.DamageHeat?.ToString("+#;-#;0")}");
            if (weaponEntryLine || weaponTableData.DamageStab.HasValue)
                weaponDataRowBuilder.AppendLine($"| {weaponTableData.DamageStab?.ToString("+#;-#;0")}");
            if (!weaponEntryLine && weaponTableData.DamageStructure.HasValue)
                weaponDataRowBuilder.AppendLine($"| {weaponTableData.DamageStructure?.ToString("+#;-#;0")}");

            if (weaponEntryLine || weaponTableData.Shots.HasValue)
                weaponDataRowBuilder.AppendLine($"| {weaponTableData.Shots?.ToString("+#;-#;0")}");
            if (weaponEntryLine || weaponTableData.Projectiles.HasValue)
                weaponDataRowBuilder.AppendLine($"| {weaponTableData.Projectiles?.ToString("+#;-#;0")}");
            if (weaponEntryLine || weaponTableData.Heat.HasValue)
                weaponDataRowBuilder.AppendLine($"| {weaponTableData.Heat?.ToString("+#;-#;0")}");
            if (weaponEntryLine || weaponTableData.Recoil.HasValue)
                weaponDataRowBuilder.AppendLine($"| {weaponTableData.Recoil?.ToString("+#;-#;0")}");

            if (weaponEntryLine || weaponTableData.AccuracyMod.HasValue)
                weaponDataRowBuilder.AppendLine($"| {weaponTableData.AccuracyMod?.ToString("+#;-#;0")}");
            if (!weaponEntryLine && weaponTableData.DirectFireAccuracy.HasValue)
                weaponDataRowBuilder.AppendLine($"| {weaponTableData.DirectFireAccuracy?.ToString("+#;-#;0")}");
            if (!weaponEntryLine && weaponTableData.ClusteringMod.HasValue)
                weaponDataRowBuilder.AppendLine($"| {weaponTableData.ClusteringMod?.ToString("+#.##%;-#.##%;0")}");
            if (weaponEntryLine || weaponTableData.EvasionIgnored.HasValue)
                weaponDataRowBuilder.AppendLine($"| {weaponTableData.EvasionIgnored?.ToString("+#;-#;0")}");
            if (weaponEntryLine || weaponTableData.CritChanceBonus.HasValue)
                weaponDataRowBuilder.AppendLine($"| {weaponTableData.CritChanceBonus?.ToString("+#.##%;-#.##%;0")}");

            if (weaponEntryLine || weaponTableData.MinRange.HasValue)
                weaponDataRowBuilder.AppendLine($"| {weaponTableData.MinRange?.ToString("+#;-#;0")}");
            if (weaponEntryLine || weaponTableData.ShortRange.HasValue)
                weaponDataRowBuilder.AppendLine($"| {weaponTableData.ShortRange?.ToString("+#;-#;0")}");
            if (weaponEntryLine || weaponTableData.MediumRange.HasValue)
                weaponDataRowBuilder.AppendLine($"| {weaponTableData.MediumRange?.ToString("+#;-#;0")}");
            if (weaponEntryLine || weaponTableData.LongRange.HasValue)
                weaponDataRowBuilder.AppendLine($"| {weaponTableData.LongRange?.ToString("+#;-#;0")}");
            if (weaponEntryLine || weaponTableData.MaxRange.HasValue)
                weaponDataRowBuilder.AppendLine($"| {weaponTableData.MaxRange?.ToString("+#;-#;0")}");

            if (weaponEntryLine || weaponTableData.FiresInMelee.HasValue)
                weaponDataRowBuilder.AppendLine($"| {(weaponTableData.FiresInMelee.Value ? "Yes" : "No" )}");

            bool firstBonus = true;
            if (weaponEntryLine || weaponTableData.Bonuses.Count() > 0)
            {
                weaponDataRowBuilder.Append("| ");
                foreach (string bonusText in weaponTableData.Bonuses)
                {
                    if (!firstBonus)
                        weaponDataRowBuilder.Append("<br/>");
                    else firstBonus = false;
                    string[] bonusSplit = bonusText.Split(':');
                    BonusDef bonus = BonusTextHandler.GetBaseBonusDef(bonusSplit[0]);
                    bonus.PopulateBonusValues(bonusSplit);
                    weaponDataRowBuilder.Append($"{String.Format(bonus.LongDescription, bonus.BonusValues)}");
                }
                weaponDataRowBuilder.AppendLine("");
            }

            weaponDataRowBuilder.AppendLine("|-");

            return weaponDataRowBuilder.ToString();
        }
    }
}
