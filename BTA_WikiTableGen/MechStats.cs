using BT_JsonProcessingLibrary;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BTA_WikiTableGen
{
    internal class MechStats
    {
        public string MechName { get; set; }
        public string MechModel { get; set; }
        public int MechTonnage { get; set; } = 0;
        public string Role { get; set; } = string.Empty;
        public Dictionary<string, int> Hardpoints { get; set; } = new Dictionary<string, int>()
        {
            { "ballistic", 0},
            { "energy", 0 },
            { "missile", 0 },
            { "antipersonnel", 0 },
            { "omni", 0 }
        };
        public string EngineTypeId { get; set; } = string.Empty;
        public string EngineCoreId { get; set; } = string.Empty;
        public int EngineSize { get; set; } = 0;
        public string HeatsinkTypeId { get; set; } = string.Empty;
        public string StructureTypeId { get; set; } = string.Empty;
        public string ArmorTypeId { get; set; } = string.Empty;
        public string GyroTypeId { get; set; } = string.Empty;
        public double? CoreTonnage { get; set; }
        public double? BareTonnage { get; set; }
        public double WalkSpeed { get; set; } = 0;
        public double RunSpeed { get; set; } = 0;
        public double JumpDistance { get; set; } = 0;
        public JsonDocument ChassisDefFile { get; set; }
        public JsonDocument MechDefFile { get; set; }
        public List<EquipmentData> DefaultGear { get; set; } = new List<EquipmentData>();
        public List<EquipmentData> FixedGear { get; set; } = new List<EquipmentData>();
        public List<EquipmentData> BaseGear { get; set; } = new List<EquipmentData>();
        public Dictionary<string, QuirkDef> MechQuirks { get; set; } = new Dictionary<string, QuirkDef>();
        public AffinityDef? MechAffinity { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public bool Blacklisted { get; set; }
        public string? PrefabId { get; set; }
        public string PrefabIdentifier { get; set; }

        public MechStats(string mechModel, string modsFolder)
        {
            MechModel = mechModel;

            List<BasicFileData> files = ModJsonHandler.SearchFiles(modsFolder, @"*_" + MechModel.Replace(' ', '_') + ".json");

            if (files.Count > 2 || files.Count < 2)
            {
                Console.WriteLine("");
                Console.WriteLine("Found the WRONG NUMBER OF FILES for " + MechModel + ": ");
                foreach (BasicFileData file in files)
                {
                    Console.WriteLine(file.FileName);
                }
                Console.WriteLine("");
            }

            foreach (BasicFileData file in files)
            {
                if (file.FileName.StartsWith("mechdef"))
                {
                    MechDefFile = JsonDocument.Parse(new StreamReader(file.Path).ReadToEnd());
                } else if (file.FileName.StartsWith("chassisdef"))
                {
                    ChassisDefFile = JsonDocument.Parse(new StreamReader(file.Path).ReadToEnd());
                }
            }

            CalculateMechStats();
        }

        public MechStats(string chassisName, string variantName, BasicFileData chassisDef, BasicFileData mechDef)
        {
            MechName = chassisName;
            MechModel = variantName;

            ChassisDefFile = JsonDocument.Parse(new StreamReader(chassisDef.Path).ReadToEnd());
            MechDefFile = JsonDocument.Parse(new StreamReader(mechDef.Path).ReadToEnd());

            CalculateMechStats();
        }

        private void CalculateMechStats()
        {
            PopulateTagsForMech();

            GetFixedGearList();

            GetBaseGearList();

            GetDefaultGearList();

            GetUnitTonnage();

            GetUnitStockRole();

            GetCoreGear();

            CalculateAllMovements();

            CountWeaponHardpoints();

            if (AffinityHandler.TryGetAssemblyVariant(this, out AssemblyVariant variant))
            {
                PrefabId = $"{variant.PrefabId}_{MechTonnage}";
                if (AffinityHandler.TryGetAffinityForMech(PrefabId, this.GetPrefabIdentifier(), out AffinityDef tempAffinityDef))                    MechAffinity = tempAffinityDef;
            }
            else
            {
                PrefabIdentifier = $"{this.GetPrefabIdentifier()}_{MechTonnage}";
                if (AffinityHandler.TryGetAffinityForMech(null, PrefabIdentifier, out AffinityDef tempAffinityDef))
                    MechAffinity = tempAffinityDef;
            }

            CoreTonnage = MechTonnageCalculator.GetCoreWeight(this);

            if (!GyroTypeId.Contains("Omni"))
                BareTonnage = MechTonnageCalculator.GetBareWeight(this);
        }

        public void OutputStatsToFile(StreamWriter writer)
        {
            writer.WriteLine( OutputTableLine( MechModel ));
            writer.WriteLine( OutputTableLine( MechTonnage + "t" ));
            writer.WriteLine( OutputTableLine( Role ));
            foreach (string hardpointType in Hardpoints.Keys)
            {
                writer.WriteLine( OutputTableLine( Hardpoints[hardpointType].ToString()));
            }
            writer.WriteLine( OutputTableLine( EngineDecode(EngineTypeId) ));
            writer.WriteLine( OutputTableLine( EngineSize.ToString() ));
            writer.WriteLine( OutputTableLine( HeatsinkDecode(HeatsinkTypeId) ));
            writer.WriteLine( OutputTableLine( StructureDecode(StructureTypeId) ));
            writer.WriteLine( OutputTableLine( ArmorDecode(ArmorTypeId) ));
            writer.WriteLine( OutputTableLine( "None" ) );
            writer.WriteLine( OutputTableLine( CoreTonnage == null ? "N/A" : CoreTonnage + "t" ) );
            writer.WriteLine( OutputTableLine( BareTonnage == null ? "N/A" : BareTonnage + "t" ) );
            writer.WriteLine( OutputTableLine( WalkSpeed.ToString() ) );
            writer.WriteLine( OutputTableLine( RunSpeed.ToString() ) );
            writer.WriteLine( OutputTableLine( JumpDistance.ToString() ) );
            writer.WriteLine( OutputTableLine( "-" ) );
        }

        public void OutputStatsToString(StringWriter writer)
        {
            writer.WriteLine(OutputTableLine(MechModel));
            writer.WriteLine(OutputTableLine(MechTonnage + "t"));
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
            writer.WriteLine(OutputTableLine("None"));
            writer.WriteLine(OutputTableLine(CoreTonnage == null ? "N/A" : CoreTonnage + "t"));
            writer.WriteLine(OutputTableLine(BareTonnage == null ? "N/A" : BareTonnage + "t"));
            writer.WriteLine(OutputTableLine(WalkSpeed.ToString()));
            writer.WriteLine(OutputTableLine(RunSpeed.ToString()));
            writer.WriteLine(OutputTableLine(JumpDistance.ToString()));
            writer.WriteLine(OutputTableLine("-"));
        }

        private string OutputTableLine(string line)
        {
            return "|" + line;
        }

        private string EngineDecode(string engineRegexOutput)
        {
            switch (engineRegexOutput)
            {
                case "emod_engineslots_std_center":
                    return "STD";
                case "emod_engineslots_light_center":
                    return "LFE";
                case "emod_engineslots_xl_center":
                    return "XL";
                case "emod_engineslots_cxl_center":
                    return "cXL";
                case "emod_engineslots_xxl_center":
                    return "XXL";
                case "emod_engineslots_cxxl_center":
                    return "cXXL";
                case "emod_engineslots_Primitive_center":
                    return "PFE";
                case "emod_engineslots_fission":
                    return "Fission";
                case "emod_engineslots_sxl_center":
                    return "sXL";
                case "emod_engineslots_dense_center":
                    return "DFE";
                case "emod_Engine_LAM":
                    return "LAM";
                case "emod_engineslots_compact_center":
                    return "CFE";
                case "emod_engineslots_FuelCell":
                    return "FCE";
                case "emod_engineslots_ICE_center":
                    return "ICE";
                case "emod_engineslots_juryrigxl_center":
                    return "JRXL";
                case "emod_engineslots_3g_center":
                    return "3GE";
                case "emod_engineslots_3gxxl_center":
                    return "3G-XXL";
            }

            return engineRegexOutput + " TYPE NOT FOUND";
        }

        private string HeatsinkDecode(string heatsinksRegexOutput)
        {
            switch (heatsinksRegexOutput)
            {
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

            return heatsinksRegexOutput + " TYPE NOT FOUND";
        }

        private string StructureDecode(string structureRegexOutput)
        {
            switch (structureRegexOutput)
            {
                case "emod_structureslots_standard":
                    return "Standard";
                case "emod_structureslots_endosteel":
                    return "Endo";
                case "emod_structureslots_clanendosteel":
                    return "Clan Endo";
                case "Gear_structureslots_Composite":
                    return "Composite";
                case "emod_structureslots_endocomposite":
                    return "Endo-Composite";
                case "emod_structureslots_clanendorigged":
                    return "Cobbled Endo";
                case "emod_structureslots_endo_standard_hybrid":
                    return "Hybrid Endo";
                case "emod_structureslots_endosteel_3G":
                    return "3G Endo";
                case "emod_structureslots_sanctuaryendocarbide":
                    return "Endo Carbide";
                case "emod_structureslots_sanctuaryendosteel":
                    return "Sanctuary Endo";
            }

            return structureRegexOutput + " TYPE NOT FOUND";
        }

        private string ArmorDecode(string armorRegexOutput)
        {
            switch (armorRegexOutput)
            {
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
                case "Gear_armorslots_Primitive":
                    return "Primitive";
                case "Gear_armorslots_Hardened":
                    return "Hardened";
                case "Gear_armorslots_Hardened_CLAN":
                    return "Clan Hardened";
                case "emod_armorslots_heavyplating":
                    return "Heavy";
                case "emod_armorslots_lightferrosfibrous":
                    return "Light Ferro";
                case "emod_armorslots_reactive":
                    return "Reactive";
                case "Gear_Reflective_Coating":
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
            }

            return armorRegexOutput + " TYPE NOT FOUND";
        }

        private void GetDefaultGearList()
        {
            foreach(string gearId in MechGearHandler.GetDefaultGearIdsForTags(this.Tags))
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

            foreach(EquipmentData data in allEquipment)
            {
                foreach(GearCategory gearCat in equipmentData.GearType)
                {
                    if(gearCat == GearCategory.Armor || gearCat == GearCategory.LifeSupportA || gearCat == GearCategory.LifeSupportB)
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
                    if(gear.TryGetProperty("ComponentDefID", out JsonElement itemId))
                        if(MechGearHandler.TryGetEquipmentData(itemId.ToString(), out EquipmentData equipmentData))
                        {
                            FixedGear.Add(equipmentData);
                            if(QuirkHandler.CheckGearIsQuirk(equipmentData, out QuirkDef tempQuirk))
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
                Console.WriteLine("FAILURE TO GET FIXED GEAR");
        }

        private void GetBaseGearList()
        {
            if (MechDefFile.RootElement.TryGetProperty("inventory", out JsonElement gearInventory))
                foreach (JsonElement gear in gearInventory.EnumerateArray())
                {
                    if(gear.TryGetProperty("ComponentDefID", out JsonElement itemId))
                        if (MechGearHandler.TryGetEquipmentData(itemId.ToString(), out EquipmentData equipmentData))
                        {
                            BaseGear.Add(equipmentData);
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
                Console.WriteLine("FAILURE TO GET BASE GEAR");
        }

        private void GetUnitTonnage()
        {
            if (ChassisDefFile.RootElement.TryGetProperty("Tonnage", out JsonElement tonnage))
                MechTonnage = tonnage.GetInt32();
            else
                Console.WriteLine("FAILURE TO GET TONNAGE");
        }

        private void GetUnitStockRole()
        {
            if (ChassisDefFile.RootElement.TryGetProperty("StockRole", out JsonElement role))
                Role = role.ToString();
            else
                Console.WriteLine("FAILURE TO GET STOCK ROLE");
        }

        private void GetCoreGear()
        {
            List<EquipmentData> AllGearList = new List<EquipmentData>();
            AllGearList.AddRange(FixedGear);
            AllGearList.AddRange(BaseGear);
            AllGearList.AddRange(DefaultGear);

            foreach (EquipmentData item in AllGearList)
            {
                foreach(GearCategory gearType in item.GearType)
                {
                    switch (gearType)
                    {
                        case GearCategory.Engine:
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
                    }
                }
            }
        }

        private void CalculateAllMovements()
        {
            double baseMoveInMeters = EngineSize / MechTonnage * 28;

            List<string> gearIds = new List<string>();
            gearIds.AddRange(from gearEntry in FixedGear select gearEntry.Id);
            gearIds.AddRange(from gearEntry in BaseGear select gearEntry.Id);
            gearIds.AddRange(from gearEntry in DefaultGear select gearEntry.Id);

            if (MoveSpeedHandler.TryGetMovementEffectsForGear(gearIds, out Dictionary<MovementType, List<MovementItem>> movements))
            {
                double adjustedWalkSpeed = MoveSpeedHandler.GetAdjustedWalkSpeed(baseMoveInMeters, movements[MovementType.Walk]);
                double adjustedSprintSpeed = MoveSpeedHandler.GetAdjustedSprintSpeed(adjustedWalkSpeed, movements[MovementType.Walk]);
                double jumpDistance = MoveSpeedHandler.GetJumpDistance(movements[MovementType.Walk]);

                WalkSpeed = ConvertMetersToHexes(adjustedWalkSpeed);
                RunSpeed = ConvertMetersToHexes(adjustedSprintSpeed);
                JumpDistance = jumpDistance;
            } else
            {
                WalkSpeed = ConvertMetersToHexes(baseMoveInMeters);
                RunSpeed = ConvertMetersToHexes(baseMoveInMeters * 1.5);
                JumpDistance = 0;
            }
        }

        private int ConvertMetersToHexes(double movementInMeters)
        {
            return (int)Math.Round(movementInMeters / 24, 0, MidpointRounding.ToZero);
        }

        private void CountWeaponHardpoints()
        {
            foreach(var location in ChassisDefFile.RootElement.GetProperty("Locations").EnumerateArray())
            {
                foreach(var hardpoint in location.GetProperty("Hardpoints").EnumerateArray())
                {
                    hardpoint.TryGetProperty("WeaponMount", out JsonElement mountType);
                    hardpoint.TryGetProperty("Omni", out JsonElement omniFlag);

                    if(omniFlag.GetBoolean())
                    {
                        Hardpoints["omni"]++;
                    }
                    else
                    {
                        Hardpoints[mountType.ToString().ToLower()]++;
                    }
                }
            }
        }

        private string GetPrefabIdentifier()
        {
            if(this.ChassisDefFile.RootElement.TryGetProperty("PrefabIdentifier", out JsonElement prefab))
                return prefab.ToString();
            else
            {
                Console.WriteLine($"MECH {this.MechModel} HAS NO PREFAB! WOOPS!");
                return "ERROR PREFAB";
            }
        }

        private void PopulateTagsForMech()
        {
            if(ChassisDefFile.RootElement.TryGetProperty("ChassisTags", out JsonElement chassisTags))
            {
                if (chassisTags.TryGetProperty("items", out JsonElement tagsElement))
                {
                    foreach (var tag in tagsElement.EnumerateArray())
                    {
                        Tags.Add(tag.ToString());
                    }
                }
            }
            if (MechDefFile.RootElement.TryGetProperty("MechTags", out JsonElement mechTags))
            {
                if (chassisTags.TryGetProperty("items", out JsonElement tagsElement))
                {
                    foreach (var tag in tagsElement.EnumerateArray())
                    {
                        Tags.Add(tag.ToString());
                    }
                }
            }

            if (Tags.Contains("BLACKLISTED") || Tags.Contains("NOSALVAGE"))
                Blacklisted = true;
            else
                Blacklisted = false;
        }
    }
}
