using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BT_JsonProcessingLibrary
{
    public struct BasicFileData
    {
        public string FileName { get; set; }
        public string Path { get; set; }
    }

    public struct Hardpoint
    {
        public string HardpointType { get; set; }
        public bool Omni { get; set; }
    }

    public struct ItemInfo
    {
        public string Id { get; set; }
        public string UIName { get; set; }
        public string Name { get; set; }
        public string Model { get; set; }
    }

    public struct EquipmentData : IComparable<EquipmentData>
    {
        public string Id { get; set; }
        public string UIName { get; set; }
        public double Tonnage { get; set; }
        public List<GearCategory> GearType { get; set; }
        public double? StructureFactor { get; set; }
        public JsonDocument GearJsonDoc { get; set; }

        public int CompareTo(EquipmentData other)
        {
            int nameCompare = CompareStringsWithNumbersByNumericalOrder.CompareStrings(UIName, other.UIName);
            if (nameCompare != 0)
                return nameCompare;
            else
                return CompareStringsWithNumbersByNumericalOrder.CompareStrings(Id, other.Id);
        }
    }

    public struct MovementItem
    {
        public string GearId { get; set; }
        public string EffectId { get; set; }
        public string UIName { get; set; }
        public MovementType MoveType { get; set; }
        public Operation Operation { get; set; }
        public double Value { get; set; }

    }

    public struct AffinityDef
    {
        public string Id { get; set; }
        public string UIName { get; set; }
        public string Description { get; set; }
    }

    public struct AssemblyVariant
    {
        public string PrefabId { get; set; }
        public bool Include { get; set; }
        public bool Exclude { get; set; }
    }

    public struct QuirkDef
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string UiName { get; set; }
        public int InstanceCount { get; set; }
        public List<BonusDef> QuirkBonuses { get; set; }
    }

    public struct BonusDef
    {
        public string BonusId { get; set; }
        public string LongDescription { get; set; }
        public string FullDescription { get; set; }
        public List<string> BonusValues { get; set; }
        public int StackingLimit { get; set; }
        public void PopulateBonusValues(string[] bonusStringSplit)
        {
            if (bonusStringSplit.Length > 1)
                this.BonusValues = bonusStringSplit[1].Split(",").Select((val) => val.Trim()).ToList();
            else
                this.BonusValues = new List<string> { "" };
        }
    }

    public struct MechNameCounter
    {
        public string MechName { get; set; }
        public int UseCount { get; set; }
    }

    public struct StoreTagAssociation
    {
        public string? tag { get; set; }
        public string? owner { get; set; }
        public string? rep { get; set; }
        public string itemsListId { get; set; }
    }

    public struct StoreEntry: IComparable<StoreEntry>
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string UiName { get; set; }
        public string LinkPageTarget { get; set; }
        public string? PageSubTarget { get; set; }
        public StoreHeadingsGroup StoreHeading { get; set; }

        int IComparable<StoreEntry>.CompareTo(StoreEntry other)
        {
            int compareVal = 0;
            if(this.UiName != null || other.UiName != null)
                compareVal = CompareStringsWithNumbersByNumericalOrder.CompareStrings(this.UiName, other.UiName);
            if (compareVal != 0)
                return compareVal;
            else
                compareVal = CompareStringsWithNumbersByNumericalOrder.CompareStrings(this.Id, other.Id);
            return compareVal;
        }
    }

    public struct WeaponTableData
    {
        public WeaponTableData(JsonDocument weaponJsonDoc)
        {
            JsonElement weaponRoot = weaponJsonDoc.RootElement;

            Id = weaponRoot.GetProperty("Description").GetProperty("Id").ToString();
            UiName = weaponRoot.GetProperty("Description").GetProperty("UIName").ToString();
            Description = weaponRoot.GetProperty("Description").GetProperty("Details").ToString();

            AmmoTypeString = weaponRoot.GetProperty("AmmoCategory").ToString();
            if (AmmoTypeString == "NotSet")
                AmmoTypeString = "N/A";
            if (weaponRoot.TryGetProperty("Category", out var category))
                HardpointType = category.ToString();
            else
                HardpointType = weaponRoot.GetProperty("weaponCategoryID").ToString();

            WeaponTonnage = weaponRoot.GetProperty("Tonnage").GetInt32();
            WeaponSlots = weaponRoot.GetProperty("InventorySize").GetInt32();

            DamageNormal = weaponRoot.GetProperty("Damage").GetDouble();
            if (weaponRoot.TryGetProperty("DamageVariance", out var damageVariance))
                DamageVarianceNormal = damageVariance.GetDouble();
            DamageHeat = weaponRoot.GetProperty("HeatDamage").GetDouble();
            DamageStab = weaponRoot.GetProperty("Instability").GetDouble();
            if(weaponRoot.TryGetProperty("StructureDamage", out var structureDamage))
                DamageStructure = structureDamage.GetDouble();

            Shots = weaponRoot.GetProperty("ShotsWhenFired").GetInt32();
            Projectiles = weaponRoot.GetProperty("ProjectilesPerShot").GetInt32();
            Heat = weaponRoot.GetProperty("HeatGenerated").GetInt32();
            Recoil = weaponRoot.GetProperty("RefireModifier").GetInt32();

            AccuracyMod = weaponRoot.GetProperty("AccuracyModifier").GetInt32();
            EvasionIgnored = weaponRoot.GetProperty("EvasivePipsIgnored").GetInt32();
            CritChanceBonus = weaponRoot.GetProperty("CriticalChanceMultiplier").GetDouble();

            MinRange = weaponRoot.GetProperty("MinRange").GetInt32();
            ShortRange = weaponRoot.GetProperty("RangeSplit")[0].GetInt32();
            MediumRange = weaponRoot.GetProperty("RangeSplit")[1].GetInt32();
            LongRange = weaponRoot.GetProperty("RangeSplit")[2].GetInt32();
            MaxRange = weaponRoot.GetProperty("MaxRange").GetInt32();

            FiresInMelee = true;
            if (weaponRoot.TryGetProperty("Custom", out var custom))
            {
                if (weaponRoot.TryGetProperty("Category", out var categories))
                    foreach (JsonElement customCategory in categories.EnumerateArray())
                    {
                        if (customCategory.ToString() == "NeverMelee")
                            FiresInMelee = false;
                    }
                if(weaponRoot.TryGetProperty("BonusDescriptions", out JsonElement bonusDescElement))
                    foreach (JsonElement bonusDesc in bonusDescElement.EnumerateArray())
                    {
                        string bonusName = bonusDesc.GetString().Split(':')[0];
                        if (WeaponTableData.BonusesNotToPrint.Contains(bonusName))
                            Bonuses.Add(bonusDesc.ToString());
                    }
            }

            if (weaponRoot.TryGetProperty("ClusteringModifier", out var clusterMod))
                ClusteringMod = clusterMod.GetDouble();
        }

        public WeaponTableData(string upgradeTagName, JsonElement modifierEffects)
        {
            Id = upgradeTagName;

            if(modifierEffects.TryGetProperty("AmmoCategory", out var ammoElement))
                AmmoTypeString = ammoElement.GetString();

            if (modifierEffects.TryGetProperty("DamagePerShot", out var damageElement))
                DamageNormal = damageElement.GetDouble();
            if (modifierEffects.TryGetProperty("HeatDamagePerShot", out var heatDamageElement))
                DamageHeat = heatDamageElement.GetDouble();
            if (modifierEffects.TryGetProperty("Instability", out var stabDamageElement))
                DamageStab = stabDamageElement.GetDouble();

            if (modifierEffects.TryGetProperty("ShotsWhenFired", out var shotsFiredElement))
                Shots = shotsFiredElement.GetInt32();
            if (modifierEffects.TryGetProperty("ProjectilesPerShot", out var projectilesPerShotElement))
                Projectiles = projectilesPerShotElement.GetInt32();

            if (modifierEffects.TryGetProperty("HeatGenerated", out var heatGenElement))
                Heat = heatGenElement.GetInt32();
            if (modifierEffects.TryGetProperty("RefireModifier", out var recoilElement))
                Recoil = recoilElement.GetInt32();

            if (modifierEffects.TryGetProperty("AccuracyModifier", out var accuracyModElement))
                AccuracyMod = ((int)accuracyModElement.GetDouble());
            if (modifierEffects.TryGetProperty("EvasivePipsIgnored", out var evasivePipsElement))
                EvasionIgnored = evasivePipsElement.GetInt32();
            if (modifierEffects.TryGetProperty("CriticalChanceMultiplier", out var critChanceMultiElement))
                CritChanceBonus = critChanceMultiElement.GetDouble();

            if (modifierEffects.TryGetProperty("MinRange", out var minRangeElement))
                MinRange = minRangeElement.GetInt32();
            if (modifierEffects.TryGetProperty("ShortRange", out var shortRangeElement))
                ShortRange = shortRangeElement.GetInt32();
            if (modifierEffects.TryGetProperty("MiddleRange", out var midRangeElement))
                MediumRange = midRangeElement.GetInt32();
            if (modifierEffects.TryGetProperty("LongRange", out var longRangeElement))
                LongRange = longRangeElement.GetInt32();
            if (modifierEffects.TryGetProperty("MaxRange", out var maxRangeElement))
                MaxRange = maxRangeElement.GetInt32();

            if (modifierEffects.TryGetProperty("ClusteringModifier", out var clusterModElement))
                ClusteringMod = clusterModElement.GetDouble();
            if (modifierEffects.TryGetProperty("APDamage", out var structureDamageElement))
                DamageStructure = structureDamageElement.GetDouble();
            if (modifierEffects.TryGetProperty("DirectFireModifier", out var directFireModElement))
                DirectFireAccuracy = directFireModElement.GetDouble();
        }

        public static WeaponTableData operator +(WeaponTableData left, WeaponTableData right)
        {
            WeaponTableData result = new WeaponTableData();

            result.Id = left.Id + "," + right.Id;
            result.UiName = "Combined Table Data";

            if(left.AmmoTypeString != null || right.AmmoTypeString != null)
                result.AmmoTypeString = left.AmmoTypeString?? "" + right.AmmoTypeString?? "";

            if (left.DamageNormal != null || right.DamageNormal != null)
                result.DamageNormal = left.DamageNormal ?? 0 + right.DamageNormal ?? 0;
            if (left.DamageVarianceNormal != null || right.DamageVarianceNormal != null)
                result.DamageVarianceNormal = left.DamageVarianceNormal ?? 0 + right.DamageVarianceNormal ?? 0;
            if (left.DamageHeat != null || right.DamageHeat != null)
                result.DamageHeat = left.DamageHeat ?? 0 + right.DamageHeat ?? 0;
            if (left.DamageStab != null || right.DamageStab != null)
                result.DamageStab = left.DamageStab ?? 0 + right.DamageStab ?? 0;

            if (left.Shots != null || right.Shots != null)
                result.Shots = left.Shots ?? 0 + right.Shots ?? 0;
            if (left.Projectiles != null || right.Projectiles != null)
                result.Projectiles = left.Projectiles ?? 0 + right.Projectiles ?? 0;

            if (left.Heat != null || right.Heat != null)
                result.Heat = left.Heat ?? 0 + right.Heat ?? 0;
            if (left.Recoil != null || right.Recoil != null)
                result.Recoil = left.Recoil ?? 0 + right.Recoil ?? 0;

            if (left.AccuracyMod != null || right.AccuracyMod != null)
                result.AccuracyMod = left.AccuracyMod ?? 0 + right.AccuracyMod ?? 0;
            if (left.EvasionIgnored != null || right.EvasionIgnored != null)
                result.EvasionIgnored = left.EvasionIgnored ?? 0 + right.EvasionIgnored ?? 0;
            if (left.CritChanceBonus != null || right.CritChanceBonus != null)
                result.CritChanceBonus = left.CritChanceBonus ?? 0 + right.CritChanceBonus ?? 0;

            if (left.MinRange != null || right.MinRange != null)
                result.MinRange = left.MinRange ?? 0 + right.MinRange ?? 0;
            if (left.ShortRange != null || right.ShortRange != null)
                result.ShortRange = left.ShortRange ?? 0 + right.ShortRange ?? 0;
            if (left.MediumRange != null || right.MediumRange != null)
                result.MediumRange = left.MediumRange ?? 0 + right.MediumRange ?? 0;
            if (left.LongRange != null || right.LongRange != null)
                result.LongRange = left.LongRange ?? 0 + right.LongRange ?? 0;
            if (left.MaxRange != null || right.MaxRange != null)
                result.MaxRange = left.MaxRange ?? 0 + right.MaxRange ?? 0;

            if (left.ClusteringMod != null || right.ClusteringMod != null)
                result.ClusteringMod = left.ClusteringMod ?? 0 + right.ClusteringMod ?? 0;
            if (left.DamageStructure != null || right.DamageStructure != null)
                result.DamageStructure = left.DamageStructure ?? 0 + right.DamageStructure ?? 0;
            if (left.DirectFireAccuracy != null || right.DirectFireAccuracy != null)
                result.DirectFireAccuracy = left.DirectFireAccuracy ?? 0 + right.DirectFireAccuracy ?? 0;

            return result;
        }

        public static bool operator ==(WeaponTableData left, WeaponTableData right)
        {
            if (left.AmmoTypeString != right.AmmoTypeString) return false;
            if (left.HardpointType != right.HardpointType) return false;

            if (left.WeaponTonnage != right.WeaponTonnage) return false;
            if (left.WeaponSlots != right.WeaponSlots) return false;

            if (left.DamageNormal != right.DamageNormal) return false;
            if (left.DamageVarianceNormal != right.DamageVarianceNormal) return false;
            if (left.DamageHeat != right.DamageHeat) return false;
            if (left.DamageStab != right.DamageStab) return false;

            if (left.Shots != right.Shots) return false;
            if (left.Projectiles != right.Projectiles) return false;
            if (left.Heat != right.Heat) return false;
            if (left.Recoil != right.Recoil) return false;

            if (left.AccuracyMod != right.AccuracyMod) return false;
            if (left.EvasionIgnored != right.EvasionIgnored) return false;
            if (left.CritChanceBonus != right.CritChanceBonus) return false;

            if (left.MinRange != right.MinRange) return false;
            if (left.ShortRange != right.ShortRange) return false;
            if (left.MediumRange != right.MediumRange) return false;
            if (left.LongRange != right.LongRange) return false;
            if (left.MaxRange != right.MaxRange) return false;

            if (left.FiresInMelee != right.FiresInMelee) return false;
            if (left.Bonuses.Count() != right.Bonuses.Count()) return false;

            if (left.ClusteringMod != right.ClusteringMod) return false;
            if (left.DamageStructure != right.DamageStructure) return false;
            if (left.DirectFireAccuracy != right.DirectFireAccuracy) return false;

            return true;
        }
        public static bool operator !=(WeaponTableData left, WeaponTableData right)
        {
            if(left == right)
                return false;
            return true;
        }

        public string Id { get; set; }
        public string UiName { get; set; }
        public string? Description { get; set; }
        public string? AmmoTypeString { get; set; }
        public string? HardpointType { get; set; }
        public int? WeaponTonnage { get; set; }
        public int? WeaponSlots { get; set; }
        public double? DamageNormal { get; set; }
        public double? DamageVarianceNormal { get; set; }
        public double? DamageHeat { get; set; }
        public double? DamageStab { get; set; }
        public int? Shots { get; set; }
        public int? Projectiles { get; set; }
        public int? Heat { get; set; }
        public int? Recoil { get; set; }
        public int? AccuracyMod { get; set; }
        public int? EvasionIgnored { get; set; }
        public double? CritChanceBonus { get; set; }
        public int? MinRange { get; set; }
        public int? ShortRange { get; set; }
        public int? MediumRange { get; set; }
        public int? LongRange { get; set; }
        public int? MaxRange { get; set; }
        public bool? FiresInMelee { get; set; }
        public List<string> Bonuses { get; set; } = new List<string>();

        public double? ClusteringMod { get; set; }
        public double? DamageStructure { get; set; }
        public double? DirectFireAccuracy { get; set; }

        private static List<string> BonusesNotToPrint = new List<string>()
        {
            "NoFireInMelee",
            "PipsIgnored",
            "EvasionIgnored",
            "WpnRecoil",
            "WpnAccuracy",
            "WpnCrits",
            "WeaponDamage",
            "StructureDmgMod",
            "Crits",
            "ProjectilesPerShot",
            "NumberofBursts",
            "ProjectileBurst",
            "VariableBurstCount",
            "IsMortar",
            "IsRifle",
            "IsArtillery",
            "IsRocket",
            "IsNOOK",
            "IsInfernoRocket",
            "IsThunderbolt",
            "IsPlasma",
            "IsMG",
            "IsGauss",
            "IsHeavyGauss",
            "IsAPGauss",
            "IsAC",
            "IsLRM",
            "IsSRM",
            "IsLightAC",
            "HeavyLaser",
            "Bombast",
            "IsLaser",
            "ChemicalLaser",
            "Narc",
            "NarcLauncher",
            "LBX",
            "SBGR",
            "HAGR",
            "Ultra",
            "Rotary",
            "XPulse",
            "Pulse",
            "ERLaser",
            "Streak",
            "FTLLRM",
            "MML",
            "ATM",
            "iATM",
            "MRM",
            "MinRange",
            "LongRange",
            "MaxRange",
            "Range",
            "DBAC",
            "MassDriver",
            "ModalLaser",
            "ModalBase",
            "ModalDmg",
            "ModalAcc",
            "ModalRange",
            "APM",
            "DBACModes",
            "DBACStab",
            "IsCannon",
            "IsGrenade",
            "VSPL",
            "VSPLShortMax",
            "VSPLMedMin",
            "VSPLMedMax",
            "VSPLLongMin",
            "VSPLLongMax",
            "vMRM",
            "vMRMBase",
            "vMRMDmg",
            "vMRMAcc",
            "vMRMHeat",
            "ModalWeapon",
            "ModalWeapon1",
            "ModalWeapon2",
            "ModalWeapon3",
            "Streak",
            "InsanityPPC",
            "Thunder",
            "ThunderAugmented",
            "ThunderCripple",
            "ThunderBurnReactionDestroy"
        };
    }

    public struct PlanetData
    {
        public PlanetData(JsonDocument planetDef)
        {
            JsonElement planetDescription = planetDef.RootElement.GetProperty("Description");
            Id = planetDescription.GetProperty("Id").ToString();
            Name = planetDescription.GetProperty("Name").ToString();
            Description = planetDescription.GetProperty("Details").ToString();

            foreach(JsonElement tag in planetDef.RootElement.GetProperty("Tags").GetProperty("items").EnumerateArray())
                Tags.Add(tag.ToString());

            foreach (JsonElement biome in planetDef.RootElement.GetProperty("SupportedBiomes").EnumerateArray())
                BiomeIds.Add(biome.ToString());

            foreach (JsonElement shopList in planetDef.RootElement.GetProperty("SystemShopItems").EnumerateArray())
                ItemCollections.Add(shopList.ToString());

            Difficulty = planetDef.RootElement.GetProperty("DefaultDifficulty").GetInt32();

            foreach (JsonElement employer in planetDef.RootElement.GetProperty("contractEmployerIDs").EnumerateArray())
                EmployerIds.Add(employer.ToString());

            foreach (JsonElement target in planetDef.RootElement.GetProperty("contractTargetIDs").EnumerateArray())
                TargetIds.Add(target.ToString());
        }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public List<string> BiomeIds { get; set; } = new List<string>();
        public List<string> ItemCollections { get; set; } = new List<string>();
        public int Difficulty { get; set; }
        public List<string> EmployerIds { get; set; } = new List<string>();
        public List<string> TargetIds { get; set; } = new List<string>();
    }

    public struct BasicLinkData
    {
        public BasicLinkData(string link, string uiName)
        {
            Link = link;
            UiName = uiName;
        }
        public string Link { get; set; }
        public string UiName { get; set; }
    }

    public enum Operation
    {
        Add,
        Multiply,
        None
    }

    public enum MovementType
    {
        Walk,
        Sprint,
        Jump
    }

    public enum GearCategory
    {
        None,
        Gyro,
        EngineShield,
        EngineCore,
        HeatsinkKit,
        EngineHeatsinks,
        Armor,
        Structure,
        WeaponAttachment,
        MeleeWeapon,
        Weapon,
        Cockpit,
        LifeSupportA,
        LifeSupportB,
        Heatsink,
        Quirk
    }

    public enum StoreHeadingsGroup
    {
        Weapons,
        Ammunition,
        Equipment,
        FullMechs,
        MechParts,
        Vehicles,
        BattleArmor,
        Contracts,
        Reference
    }

    public static class CompareStringsWithNumbersByNumericalOrder
    {
        public static int CompareStrings(string caller, string target)
        {
            if(caller == null || target == null)
            {
                if (caller == null)
                    return -1;
                else 
                    return 1;
            }

            for(int charPos = 0; charPos < (caller.Length <= target.Length ? caller.Length : target.Length); charPos++)
            {
                if (Char.IsDigit(caller[charPos]))
                {
                    if (Char.IsDigit(target[charPos]))
                    {
                        int callerOffset = 0;
                        int targetOffset = 0;
                        string callerIntStr = "" + caller[charPos];
                        string targetIntStr = "" + target[charPos];
                        while (((charPos + callerOffset + 1) < caller.Length && Char.IsDigit(caller[charPos + callerOffset + 1]))
                            || ((charPos + targetOffset + 1) < target.Length && Char.IsDigit(target[charPos + targetOffset + 1])))
                        {
                            if((charPos + callerOffset + 1) < caller.Length && Char.IsDigit(caller[charPos + callerOffset + 1]))
                            {
                                callerOffset++;
                                callerIntStr += caller[charPos + callerOffset];
                            }
                            if((charPos + targetOffset + 1) < target.Length && Char.IsDigit(target[charPos + targetOffset + 1]))
                            {
                                targetOffset++;
                                targetIntStr += target[charPos + targetOffset];
                            }
                        }
                        int callerInt = Int32.Parse(callerIntStr);
                        int targetInt = Int32.Parse(targetIntStr);
                        if (callerInt > targetInt)
                            return 1;
                        else if (callerInt < targetInt)
                            return -1;
                        else
                            charPos = charPos + callerOffset;
                    }
                    else
                        return 1;
                }
                else if (Char.IsDigit(target[charPos]))
                    return -1;
                else if (caller[charPos] != target[charPos])
                    return caller[charPos].CompareTo(target[charPos]);
            }
            if(caller.Length < target.Length) 
                return -1;
            else if (caller.Length > target.Length)
                return 1;
            return 0;
        }
    }
}
