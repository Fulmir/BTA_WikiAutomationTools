﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UtilityClassLibrary;

namespace BT_JsonProcessingLibrary
{
    public class MechStats : UnitStats
    {
        //public string ChassisName { get; set; }
        public string MechGroupName
        {
            get
            {
                if (MechVariantDataOverrides.TryGetNameOverride(VariantName, out var nameOverride))
                    return nameOverride;
                return ChassisName;
            }
        }
        //public string VariantName { get; set; }
        //public int Weight { get; set; } = 0;
        public string Role { get; set; } = string.Empty;
        public Dictionary<string, int> Hardpoints { get; set; } = new Dictionary<string, int>()
        {
            { "ballistic", 0},
            { "energy", 0 },
            { "missile", 0 },
            { "artillery", 0 },
            { "antipersonnel", 0 },
            { "omni", 0 },
            { "bombbay", 0 },
            { "meleeweapon", 0 }
            //,
            //{ "battlearmor", 0 }
        };
        public override string[] Locations { get; } = { "Head", "CenterTorso", "LeftTorso", "RightTorso", "LeftArm", "RightArm", "LeftLeg", "RightLeg" };
        //public string EngineTypeId { get; set; } = string.Empty;
        //public string EngineCoreId { get; set; } = string.Empty;
        //public int EngineSize { get; set; } = 0;
        //public string HeatsinkTypeId { get; set; } = string.Empty;
        //public string StructureTypeId { get; set; } = string.Empty;
        //public string ArmorTypeId { get; set; } = string.Empty;
        public string GyroTypeId { get; set; } = string.Empty;
        public double? CoreTonnage { get; set; }
        public double? BareTonnage { get; set; }
        //public double WalkSpeed { get; set; } = 0;
        //public double RunSpeed { get; set; } = 0;
        //public double JumpDistance { get; set; } = 0;
        //public double TotalDamage { get; set; } = 0;
        //public double TotalDamageHeat { get; set; } = 0;
        //public double TotalDamageStability { get; set; } = 0;
        //public JsonDocument ChassisDefFile { get; set; }
        //public JsonDocument UnitDefFile { get; set; }
        public List<EquipmentData> DefaultGear { get; set; } = new List<EquipmentData>();
        public List<EquipmentData> FixedGear { get; set; } = new List<EquipmentData>();
        public List<EquipmentData> BaseGear { get; set; } = new List<EquipmentData>();
        public List<EquipmentData> MeleeWeapons { get; set; } = new List<EquipmentData>();
        public List<EquipmentData> RangedWeapons { get; set; } = new List<EquipmentData>();
        public Dictionary<string, QuirkDef> MechQuirks { get; set; } = new Dictionary<string, QuirkDef>();
        public AffinityDef? MechAffinity { get; set; }
        public AssemblyVariant? VariantAssemblyRules { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public bool Blacklisted { get; set; }
        public string? PrefabId { get; set; }
        public string PrefabIdentifier { get; set; }

        //public MechStats(string mechModel, string modsFolder)
        //{
        //    VariantName = mechModel;

        //    List<BasicFileData> files = ModJsonHandler.SearchFiles(modsFolder, @"*_" + VariantName.Replace(' ', '_') + ".json");

        //    if (files.Count > 2 || files.Count < 2)
        //    {
        //        string countProblem = $"Found {files.Count} for {VariantName}. File names are: ";
        //        foreach (BasicFileData file in files)
        //        {
        //            countProblem += $"\n\r{file.FileName}";
        //        }
        //        Logging.AddLogToQueue(countProblem, LogLevel.Warning, LogCategories.MechDefs);
        //    }

        //    foreach (BasicFileData file in files)
        //    {
        //        if (file.FileName.StartsWith("mechdef"))
        //        {
        //            UnitDefFile = JsonDocument.Parse(new StreamReader(file.Path).ReadToEnd());
        //        }
        //        else if (file.FileName.StartsWith("chassisdef"))
        //        {
        //            ChassisDefFile = JsonDocument.Parse(new StreamReader(file.Path).ReadToEnd());
        //        }
        //    }

        //    CalculateMechStats();
        //}

        public MechStats(string chassisName, string variantName, BasicFileData chassisDef, BasicFileData unitDef) : base(chassisName, variantName, chassisDef, unitDef)
        {
            ChassisName = chassisName;
            VariantName = variantName;

            ChassisDefFile = JsonDocument.Parse(new StreamReader(chassisDef.Path).ReadToEnd(), UtilityStatics.GeneralJsonDocOptions);
            UnitDefFile = JsonDocument.Parse(new StreamReader(unitDef.Path).ReadToEnd());

            CalculateMechStats();
        }

        private void CalculateMechStats()
        {
            PopulateTagsForMech();

            GetFixedGearList();

            GetBaseGearList();

            GetDefaultGearList();

            GetCoreGear();

            GetUnitTonnage();

            GetUnitStockRole();

            CalculateAllMovements();

            CountWeaponHardpoints();

            CalculateDamageTotals();

            if (AffinityHandler.TryGetAssemblyVariant(this, out AssemblyVariant variant))
            {
                PrefabId = $"{variant.PrefabId}_{Weight}";
                if (AffinityHandler.TryGetAffinityForMech(PrefabId, this.GetPrefabIdentifier(), out AffinityDef tempAffinityDef)) MechAffinity = tempAffinityDef;
                VariantAssemblyRules = variant;
            }
            else
            {
                PrefabIdentifier = $"{this.GetPrefabIdentifier()}_{Weight}";
                if (AffinityHandler.TryGetAffinityForMech(null, PrefabIdentifier, out AffinityDef tempAffinityDef))
                    MechAffinity = tempAffinityDef;
            }

            CoreTonnage = MechTonnageCalculator.GetCoreWeight(this);

            if (!GyroTypeId.Contains("Omni"))
                BareTonnage = MechTonnageCalculator.GetBareWeight(this);
        }

        public void OutputStatsToFile(StreamWriter writer)
        {
            OutputStatsToTableRowString(writer);
        }

        public void OutputStatsToTableRowString(TextWriter writer)
        {
            writer.WriteLine(OutputTableLine(VariantName));
            writer.WriteLine(OutputTableLine(Weight + "t"));
            writer.WriteLine(OutputTableLine(Role));
            foreach (string hardpointType in Hardpoints.Keys)
            {
                writer.WriteLine(OutputTableLine(Hardpoints[hardpointType].ToString()));
            }
            writer.WriteLine(OutputTableLine(EngineDecode(EngineTypeId)));
            writer.WriteLine(OutputTableLine(EngineSize.ToString()));
            writer.WriteLine(OutputTableLine(HeatsinkDecode(HeatsinkTypeId)));
            writer.WriteLine(OutputTableLine(StructureDecode(StructureTypeId)));
            writer.WriteLine(OutputTableLine(ArmorDecode(ArmorTypeId)));
            writer.WriteLine(OutputTableLine(OutputMeleeWeapons()));
            writer.WriteLine(OutputTableLine(CoreTonnage == null ? "N/A" : CoreTonnage + "t"));
            writer.WriteLine(OutputTableLine(BareTonnage == null ? "N/A" : BareTonnage + "t"));
            writer.WriteLine(OutputTableLine(WalkSpeed.ToString()));
            writer.WriteLine(OutputTableLine(RunSpeed.ToString()));
            writer.WriteLine(OutputTableLine(JumpDistance.ToString()));
            writer.WriteLine("|-");
        }


        public void OutputMechToPageTab(TextWriter writer)
        {
            writer.WriteLine($"<tab name=\"{VariantName}\">");
            writer.WriteLine("{{InfoboxVehicle");
            writer.WriteLine(OutputTableLine($"vehiclename = {VariantName}"));
            writer.WriteLine(OutputTableLine($"image = {ChassisName}.png"));
            writer.WriteLine(OutputTableLine($"class = {ModJsonHandler.GetWeightClassFromTonnage(Weight)}"));
            writer.WriteLine(OutputTableLine($"weight = {Weight}t"));
            writer.WriteLine(OutputTableLine($"speed = {WalkSpeed}/{RunSpeed}"));
            writer.WriteLine(OutputTableLine($"propulsion = {ConvertVehicleMovementTypeToString(VehicleMoveType)}"));

            writer.WriteLine(OutputTableLine($"maxdamage = {TotalDamage}"));
            writer.WriteLine(OutputTableLine($"maxstability = {TotalDamageStability}"));
            writer.WriteLine(OutputTableLine($"maxheat = {TotalDamageHeat}"));

            writer.WriteLine(OutputTableLine($"armor = {TotalLocationStatType(ArmorByLocation, ArmorModifiersByLocation)}"));
            writer.WriteLine(OutputTableLine($"structure = {TotalLocationStatType(StructureByLocation, StructureModifiersByLocation)}"));
            writer.WriteLine(OutputTableLine($"frontarmor = {PrintLocationArmorAndStructure("Front")}"));
            writer.WriteLine(OutputTableLine($"leftarmor = {PrintLocationArmorAndStructure("Left")}"));
            writer.WriteLine(OutputTableLine($"rightarmor = {PrintLocationArmorAndStructure("Right")}"));
            writer.WriteLine(OutputTableLine($"reararmor = {PrintLocationArmorAndStructure("Rear")}"));
            if (StructureByLocation.ContainsKey("Turret"))
                writer.WriteLine(OutputTableLine($"turretarmor = {PrintLocationArmorAndStructure("Turret")}"));

            writer.WriteLine(OutputTableLine($"weapon1 = {OutputEquipmentForTable(Weapons)}"));

            writer.WriteLine(OutputTableLine($"ammo1 = {OutputEquipmentForTable(Ammo)}"));

            writer.WriteLine(OutputTableLine($"gear1 = {OutputEquipmentForTable(UtilityGear)}"));

            writer.WriteLine("}}");
            writer.WriteLine("<br>");
            writer.WriteLine("===Description===");

            string baseDescription = ModJsonHandler.GetDescriptionDetailsFromJsonDoc(base.UnitDefFile);
            string[] tempDescriptionParse = baseDescription.Split("<b>");
            string communityContentText = "";
            if (baseDescription.Contains("COMMUNITY CONTENT"))
                communityContentText = "<b>" + tempDescriptionParse[tempDescriptionParse.Length - 1];

            writer.WriteLine(tempDescriptionParse[0] + communityContentText);

            writer.WriteLine("<br>");
            writer.WriteLine("===Factions===");
            FactionDataHandler.TagsListToFactionsSection(Tags, writer);
            writer.WriteLine("</tab>");
        }

        private string OutputMeleeWeapons()
        {
            string output = "";
            bool first = true;

            if(MeleeWeapons.Count == 0)
            {
                output = "None";
                return output;
            }

            foreach(EquipmentData weapon in MeleeWeapons)
            {
                if (first)
                    first = false;
                else
                    output += "\r\n";

                output += weapon.UIName;
            }

            return output;
        }

        private string OutputTableLine(string line)
        {
            return "| " + line;
        }

        private string EngineDecode(string engineShieldGearId)
        {
            switch (engineShieldGearId)
            {
                case "":
                    return "STD";
                case "emod_engineslots_std_center":
                    return "STD";
                case "emod_engineslots_light_center":
                    return "LFE";
                case "emod_engineslots_light_center_unique":
                    return "Unique LFE";
                case "emod_engineslots_xl_center":
                    return "XL";
                case "emod_engineslots_cxl_center":
                    return "cXL";
                case "emod_engineslots_protoxl_center":
                    return "Proto XL";
                case "emod_engineslots_xxl_center":
                    return "XXL";
                case "emod_engineslots_cxxl_center":
                    return "cXXL";
                case "emod_engineslots_primitive_center":
                    return "PFE";
                case "emod_engineslots_fission_center":
                    return "Fission";
                case "emod_engineslots_sxl_center":
                    return "sXL";
                case "emod_engineslots_dense_center":
                    return "DFE";
                case "emod_engineslots_lam_center":
                    return "LAM";
                case "emod_engineslots_compact_center":
                    return "CFE";
                case "emod_engineslots_fuelcell_center":
                    return "FCE";
                case "emod_engineslots_ICE_center":
                    return "ICE";
                case "emod_engineslots_juryrigxl_center":
                    return "JRXL";
                case "emod_engineslots_3g_center":
                    return "3GE";
                case "emod_engineslots_3gxxl_center":
                    return "3G-XXL";
                case "emod_engineslots_3gul_center":
                    return "3G-UL";
            }

            string badEngineType = "ENGINE TYPE NOT FOUND: " + engineShieldGearId;

            Logging.AddLogToQueue(badEngineType, LogLevel.Error, LogCategories.MechDefs);

            return badEngineType;
        }

        private string HeatsinkDecode(string heatsinkKidGearId)
        {
            switch (heatsinkKidGearId)
            {
                case "":
                    return "SHS";
                case "emod_kit_shs":
                    return "SHS";
                case "emod_kit_dhs":
                    return "DHS";
                case "emod_kit_cdhs":
                    return "cDHS";
                case "emod_kit_dhs_proto":
                    return "pDHS";
                case "emod_kit_sdhs":
                    return "sDHS";
                case "emod_kit_lhs":
                    return "LDHS";
                case "emod_kit_3ghs":
                    return "3GHS";
            }

            string badHeatsinkType = "HEATSINK TYPE NOT FOUND: " + heatsinkKidGearId;

            Logging.AddLogToQueue(badHeatsinkType, LogLevel.Error, LogCategories.MechDefs);

            return badHeatsinkType;
        }

        private string StructureDecode(string structureGearId)
        {
            switch (structureGearId)
            {
                case "":
                    return "Standard";
                case "emod_structureslots_standard":
                    return "Standard";
                case "emod_structureslots_endosteel":
                    return "Endo";
                case "emod_structureslots_clanendosteel":
                    return "Clan Endo";
                case "emod_structureslots_endosteelprototype":
                    return "Proto Endo";
                case "emod_structureslots_composite":
                    return "Composite";
                case "emod_structureslots_endocomposite":
                    return "Endo-Composite";
                case "emod_structureslots_clanendorigged":
                    return "Cobbled Endo";
                case "emod_structureslots_endo_standard_hybrid":
                    return "Hybrid Endo";
                case "emod_structureslots_hybrid_slots_0.25":
                    return "Hybrid Endo";
                case "emod_structureslots_endosteel_3G":
                    return "3G Endo";
                case "emod_structureslots_sanctuaryendocarbide":
                    return "Endo Carbide";
                case "emod_structureslots_sanctuaryendosteel":
                    return "Sanctuary Endo";
                case "emod_structureslots_reinforced":
                    return "Reinforced";
            }

            if (structureGearId.Contains("PrimitiveRugged"))
                return "Rugged";

            if (structureGearId.Contains("Reinforcement"))
                return "Reinforced";

            string badStructureType = "STRUCTURE TYPE NOT FOUND: " + structureGearId;

            Logging.AddLogToQueue(badStructureType, LogLevel.Error, LogCategories.MechDefs);

            return badStructureType;
        }

        private string ArmorDecode(string armorGearId)
        {
            switch (armorGearId)
            {
                case "":
                    return "Standard";
                case "emod_armorslots_standard":
                    return "Standard";
                case "emod_armorslots_clstandard":
                    return "Clan Standard";
                case "emod_armorslots_ferrosfibrous":
                    return "Ferro";
                case "emod_armorslots_clanferrosfibrous":
                    return "Clan Ferro";
                case "emod_armorslots_heavyferrosfibrous":
                    return "Heavy Ferro";
                case "emod_armorslots_clanferrolamellor":
                    return "Ferro Lamellor";
                case "emod_armorslots_primitive":
                    return "Primitive";
                case "emod_armorslots_hardened":
                    return "Hardened";
                case "emod_armorslots_hardened_CLAN":
                    return "Clan Hardened";
                case "emod_armorslots_heavyplating":
                    return "Heavy";
                case "emod_armorslots_lightferrosfibrous":
                    return "Light Ferro";
                case "emod_armorslots_reactive":
                    return "Reactive";
                case "emod_armorslots_reflective":
                    return "Reflective";
                case "emod_armorslots_lightplating":
                    return "Light";
                case "emod_armorslots_stealth":
                    return "Stealth";
                case "emod_armorslots_ultraferrofibrous":
                    return "Ultra Ferro";
                case "emod_armorslots_3rd_generation":
                    return "3G Ferro";
                case "emod_armorslots_3rd_generation_hardened":
                    return "3G Hardened";
                case "emod_armorslots_sanctuaryferrofibrous":
                    return "Sanctuary Ferro";
                case "emod_armorslots_sanctuaryferrovanadium":
                    return "Ferro Vanadium";
                case "emod_armorslots_industrial":
                    return "Industrial";
                case "emod_armorslots_ferroplating":
                    return "Ferro Plating";
            }

            string badArmorType = "ARMOR TYPE NOT FOUND: " + armorGearId;

            Logging.AddLogToQueue(badArmorType, LogLevel.Error, LogCategories.MechDefs);

            return badArmorType;
        }

        private void GetDefaultGearList()
        {
            foreach (string gearId in MechGearHandler.GetDefaultGearIdsForMechTags(this.Tags))
            {
                if (MechGearHandler.TryGetEquipmentData(gearId.ToString(), out EquipmentData equipmentData))
                {
                    if (CheckIsOverWrittenDefault(equipmentData))
                        continue;
                    DefaultGear.Add(equipmentData);
                    if (QuirkHandler.CheckGearIsQuirk(equipmentData, out QuirkDef tempQuirk))
                    {
                        if (MechQuirks.ContainsKey(tempQuirk.Id))
                        {
                            tempQuirk.InstanceCount++;
                        }
                        MechQuirks[tempQuirk.Id] = tempQuirk;
                    }
                }
            }
        }

        private bool CheckIsOverWrittenDefault(EquipmentData equipmentData)
        {
            List<EquipmentData> allEquipment = new List<EquipmentData>();
            allEquipment.AddRange(FixedGear);
            allEquipment.AddRange(BaseGear);

            foreach (EquipmentData data in allEquipment)
            {
                foreach (GearCategory gearCat in equipmentData.GearType)
                {
                    if (gearCat == GearCategory.Armor || gearCat == GearCategory.LifeSupportA || gearCat == GearCategory.LifeSupportB)
                    {
                        if (data.GearType.Contains(gearCat))
                            return true;
                    }
                }
            }
            return false;
        }

        private void GetFixedGearList()
        {
            if (ChassisDefFile.RootElement.TryGetProperty("FixedEquipment", out JsonElement fixedEquipment))
                foreach (JsonElement gear in fixedEquipment.EnumerateArray())
                {
                    if (gear.TryGetProperty("ComponentDefID", out JsonElement itemId))
                        if (MechGearHandler.TryGetEquipmentData(itemId.ToString(), out EquipmentData equipmentData))
                        {
                            FixedGear.Add(equipmentData);
                            if (QuirkHandler.CheckGearIsQuirk(equipmentData, out QuirkDef tempQuirk))
                            {
                                if (MechQuirks.ContainsKey(tempQuirk.Id))
                                {
                                    tempQuirk.InstanceCount++;
                                }
                                MechQuirks[tempQuirk.Id] = tempQuirk;
                            }
                        }
                }
            else
                Logging.AddLogToQueue($"FAILURE TO GET FIXED GEAR FOR {VariantName}", LogLevel.Error, LogCategories.MechDefs);
        }

        private void GetBaseGearList()
        {
            if (UnitDefFile.RootElement.TryGetProperty("inventory", out JsonElement gearInventory))
                foreach (JsonElement gear in gearInventory.EnumerateArray())
                {
                    if (gear.TryGetProperty("ComponentDefID", out JsonElement itemId))
                        if (MechGearHandler.TryGetEquipmentData(itemId.ToString(), out EquipmentData equipmentData))
                        {
                            BaseGear.Add(equipmentData);
                        }
                }
            else
                Logging.AddLogToQueue($"FAILURE TO GET BASE GEAR FOR {VariantName}", LogLevel.Error, LogCategories.MechDefs);
        }

        private void GetUnitTonnage()
        {
            if (ChassisDefFile.RootElement.TryGetProperty("Tonnage", out JsonElement tonnage))
                Weight = tonnage.GetInt32();
            else
                Logging.AddLogToQueue($"FAILURE TO GET TONNAGE FOR {VariantName}", LogLevel.Error, LogCategories.MechDefs);
        }

        private void GetUnitStockRole()
        {
            if (ChassisDefFile.RootElement.TryGetProperty("StockRole", out JsonElement role))
                Role = role.ToString();
            else
                Logging.AddLogToQueue($"FAILURE TO GET STOCK ROLE FOR {VariantName}", LogLevel.Error, LogCategories.MechDefs);
        }

        private void GetCoreGear()
        {
            List<EquipmentData> AllGearList = new List<EquipmentData>();
            AllGearList.AddRange(FixedGear);
            AllGearList.AddRange(BaseGear);
            AllGearList.AddRange(DefaultGear);

            foreach (EquipmentData item in AllGearList)
            {
                foreach (GearCategory gearType in item.GearType)
                {
                    switch (gearType)
                    {
                        case GearCategory.EngineShield:
                            EngineTypeId = item.Id;
                            break;
                        case GearCategory.EngineCore:
                            EngineSize = Convert.ToInt32(MechGearHandler.engineSizeRegex.Match(item.Id).Groups[0].Value);
                            EngineCoreId = item.Id;
                            break;
                        case GearCategory.HeatsinkKit:
                            HeatsinkTypeId = item.Id;
                            break;
                        case GearCategory.Structure:
                            StructureTypeId = item.Id;
                            break;
                        case GearCategory.Armor:
                            ArmorTypeId = item.Id;
                            break;
                        case GearCategory.Gyro:
                            GyroTypeId = item.Id;
                            break;
                        case GearCategory.MeleeWeapon:
                            MeleeWeapons.Add(item);
                            break;
                        case GearCategory.RangedWeapon:
                            RangedWeapons.Add(item);
                            break;
                    }
                }
            }
        }

        private void CalculateAllMovements()
        {
            double baseMoveInMeters = EngineSize / Weight * MoveSpeedHandler.BaseWalkSpeedMultiplier;

            List<string> gearIds = new List<string>();
            gearIds.AddRange(from gearEntry in FixedGear select gearEntry.Id);
            gearIds.AddRange(from gearEntry in BaseGear select gearEntry.Id);
            gearIds.AddRange(from gearEntry in DefaultGear select gearEntry.Id);

            if (MoveSpeedHandler.TryGetMovementEffectsForGear(gearIds, out Dictionary<MovementType, List<MovementItem>> movements))
            {
                double adjustedWalkSpeed = MoveSpeedHandler.GetAdjustedWalkSpeed(baseMoveInMeters, movements[MovementType.Walk]);
                double adjustedSprintSpeed = MoveSpeedHandler.GetAdjustedSprintSpeed(adjustedWalkSpeed, movements[MovementType.Sprint]);
                double jumpDistance = MoveSpeedHandler.GetJumpDistance(movements[MovementType.Jump]);

                WalkSpeed = ConvertMetersToHexes(adjustedWalkSpeed);
                RunSpeed = ConvertMetersToHexes(adjustedSprintSpeed);
                JumpDistance = ConvertMetersToHexes(jumpDistance);
            }
            else
            {
                WalkSpeed = ConvertMetersToHexes(baseMoveInMeters);
                RunSpeed = ConvertMetersToHexes(baseMoveInMeters * 1.5);
                JumpDistance = 0;
            }
        }

        private void CalculateDamageTotals()
        {
            TotalDamage = 0;
            TotalDamageHeat = 0;
            TotalDamageStability = 0;

            foreach (EquipmentData weapon in RangedWeapons)
            {
                TotalDamage += (weapon.Damage ?? 0) * (weapon.Shots ?? 0);
                TotalDamageHeat += (weapon.DamageHeat ?? 0) * (weapon.Shots ?? 0);
                TotalDamageStability += (weapon.DamageStability ?? 0) * (weapon.Shots ?? 0);
            }
        }

        private int ConvertMetersToHexes(double movementInMeters)
        {
            return (int)Math.Round(movementInMeters / MoveSpeedHandler.BaseHexSizeValue, 0, MidpointRounding.ToZero);
        }

        private void CountWeaponHardpoints()
        {
            foreach (var location in ChassisDefFile.RootElement.GetProperty("Locations").EnumerateArray())
            {
                foreach (var hardpoint in location.GetProperty("Hardpoints").EnumerateArray())
                {
                    // TODO: Remove this jank
                    JsonElement? mountType = null;
                    if(hardpoint.TryGetProperty("WeaponMountID", out JsonElement correctMountType))
                        mountType = correctMountType;
                    else if (hardpoint.TryGetProperty("WeaponMount", out JsonElement incorrectMountType))
                        mountType = incorrectMountType;
                    hardpoint.TryGetProperty("Omni", out JsonElement omniFlag);

                    if (omniFlag.GetBoolean())
                    {
                        Hardpoints["omni"]++;
                    }
                    else if(mountType.ToString().ToLower() != "battlearmor")
                    {
                        Hardpoints[mountType.ToString().ToLower()]++;
                    }
                }
            }
        }

        private void PopulateArmorAndStructureByLocation()
        {
            foreach (JsonElement locationData in base.UnitDefFile.RootElement.GetProperty("Locations").EnumerateArray())
            {
                string location = locationData.GetProperty("Location").ToString();
                int armorValue = locationData.GetProperty("CurrentArmor").GetInt32();
                int structureValue = locationData.GetProperty("CurrentInternalStructure").GetInt32();

                if (this.Locations.Contains(location))
                {
                    ArmorByLocation[location] = armorValue;
                    StructureByLocation[location] = structureValue;
                }
                else
                {
                    Logging.AddLogToQueue($"Invalid location {location} found in vehicle {VehicleUiName}", LogLevel.Error, LogCategories.VehicleDefs);
                }
            }
        }

        private string GetPrefabIdentifier()
        {
            if (this.ChassisDefFile.RootElement.TryGetProperty("PrefabIdentifier", out JsonElement prefab))
                return prefab.ToString();
            else
            {
                Logging.AddLogToQueue($"NO PREFAB IDENTIFIER FOR {this.VariantName}", LogLevel.Warning, LogCategories.MechDefs);
                return "ERROR PREFAB";
            }
        }

        private void PopulateTagsForMech()
        {
            if (ChassisDefFile.RootElement.TryGetProperty("ChassisTags", out JsonElement chassisTags))
            {
                if (chassisTags.TryGetProperty("items", out JsonElement tagsElement))
                {
                    foreach (var tag in tagsElement.EnumerateArray())
                    {
                        if (!Tags.Contains(tag.ToString()))
                            Tags.Add(tag.ToString());
                    }
                }
            }
            if (UnitDefFile.RootElement.TryGetProperty("MechTags", out JsonElement mechTags))
            {
                if (mechTags.TryGetProperty("items", out JsonElement tagsElement))
                {
                    foreach (var tag in tagsElement.EnumerateArray())
                    {
                        if (!Tags.Contains(tag.ToString()))
                            Tags.Add(tag.ToString());
                    }
                }
            }

            if (Tags.Contains("BLACKLISTED") || Tags.Contains("NOSALVAGE") || Tags.Contains("ProtoMech"))
                Blacklisted = true;
            else
                Blacklisted = false;
            if (MechFileSearch.WhitelistMechVariants.Contains(VariantName))
                Blacklisted = false;
        }
    }
}
