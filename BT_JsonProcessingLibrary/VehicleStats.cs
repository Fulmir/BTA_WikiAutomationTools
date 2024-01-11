using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UtilityClassLibrary;

namespace BT_JsonProcessingLibrary
{
    public class VehicleStats : UnitStats
    {
        //public string UiName { get; set; }
        //public string ChassisName { get; set; }
        //public int Weight { get; set; } = 0;
        //public string EngineTypeId { get; set; } = string.Empty;
        //public string EngineCoreId { get; set; } = string.Empty;
        //public int EngineSize { get; set; } = 0;
        public VehicleMovementTypes VehicleMoveType { get; set; }
        public string HeatsinkTypeId { get; set; } = string.Empty;
        public string StructureTypeId { get; set; } = string.Empty;
        public string ArmorTypeId { get; set; } = string.Empty;
        public double TotalDamage { get; set; } = 0;
        public double TotalDamageHeat { get; set; } = 0;
        public double TotalDamageStability { get; set; } = 0;
        public Dictionary<string, double> StructureByLocation { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, List<Statisticdata>> StructureModifiersByLocation { get; set; } = new Dictionary<string, List<Statisticdata>>();
        public double GlobalStructureModifier { get; set; } = 1;
        public Dictionary<string, double> ArmorByLocation { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, List<Statisticdata>> ArmorModifiersByLocation { get; set; } = new Dictionary<string, List<Statisticdata>>();
        public double GlobalArmorModifier { get; set; } = 1;
        public Dictionary<string, List<EquipmentData>> VehicleGearByLocation { get; set; } = new Dictionary<string, List<EquipmentData>>();
        public override string[] Locations { get; } = { "Front", "Left", "Right", "Rear", "Turret" };
        //public List<EquipmentData> Weapons { get; set; } = new List<EquipmentData>();
        //public List<EquipmentData> Ammo { get; set; } = new List<EquipmentData>();
        //public List<EquipmentData> UtilityGear { get; set; } = new List<EquipmentData>();
        public double WalkSpeed { get; set; } = 0;
        public double RunSpeed { get; set; } = 0;
        //public JsonDocument ChassisDefFile { get; set; }
        //public JsonDocument UnitDefFile { get; set; }
        //public List<string> Tags { get; set; } = new List<string>();
        //public List<string> WikiTags { get; set; } = new List<string>();
        //public bool PlayerControllable { get; set; }
        //public bool Blacklisted { get; set; }


        public VehicleStats(BasicFileData vehicleChassisDef, BasicFileData vehicleDef)
        {
            ChassisDefFile = JsonDocument.Parse(new StreamReader(vehicleChassisDef.Path).ReadToEnd());
            UnitDefFile = JsonDocument.Parse(new StreamReader(vehicleDef.Path).ReadToEnd());

            VariantName = ModJsonHandler.GetUiNameFromJsonDoc(UnitDefFile);
            ChassisName = ModJsonHandler.GetNameFromJsonDoc(ChassisDefFile);

            InstatiateLocationData();

            CalculateVehicleStats();
        }

        private void InstatiateLocationData()
        {
            foreach (string location in Locations)
            {
                VehicleGearByLocation.Add(location, new List<EquipmentData>());
                ArmorModifiersByLocation.Add(location, new List<Statisticdata>());
                StructureModifiersByLocation.Add(location, new List<Statisticdata>());
            }
        }

        private void CalculateVehicleStats()
        {
            PopulateTagsForVehicle();

            MovementDecode();

            PopulateArmorAndStructureByLocation();

            SetPlayerControllableFromTags();

            GetVehicleGearList();

            GetCoreGear();

            GetUnitTonnage();

            CalculateAllMovements();

            CalculateDamageTotals();
        }

        public void OutputStatsToFile(StreamWriter writer)
        {
            OutputStatsToString(writer);
        }

        public void OutputStatsToString(TextWriter writer)
        {
            writer.WriteLine(OutputTableLine(VariantName));
            writer.WriteLine(OutputTableLine(Weight + "t"));
            writer.WriteLine(OutputTableLine(PlayerControllable ? "Yes" : "No"));
            writer.WriteLine(OutputTableLine(ConvertVehicleMovementTypeToString(VehicleMoveType)));
            writer.WriteLine(OutputTableLine($"{WalkSpeed.ToString()}/{RunSpeed.ToString()}"));
            writer.WriteLine(OutputTableLine(EngineDecode(EngineTypeId)));
            writer.WriteLine(OutputTableLine(EngineSize.ToString()));
            writer.WriteLine(OutputTableLine(TotalLocationStatType(ArmorByLocation, ArmorModifiersByLocation)));
            writer.WriteLine(OutputTableLine(TotalLocationStatType(StructureByLocation, StructureModifiersByLocation)));
            foreach(string location in Locations)
            {
                writer.WriteLine(OutputTableLine(PrintLocationArmorAndStructure(location)));
            }
            writer.WriteLine(OutputTableLine(OutputEquipmentForTable(UtilityGear)));
            writer.WriteLine(OutputTableLine(OutputEquipmentForTable(Weapons)));
            writer.WriteLine(OutputTableLine(OutputEquipmentForTable(Ammo)));
            writer.WriteLine("|-");
        }

        public void OutputVehicleToPageTab(TextWriter writer)
        {
            writer.WriteLine($"<tab name=\"{VariantName}\">");
            writer.WriteLine("{{InfoboxVehicle");
            writer.WriteLine(OutputTableLine($"vehiclename = {VariantName}"));
            writer.WriteLine(OutputTableLine($"image = Vehicle_{ChassisName}.png"));
            writer.WriteLine(OutputTableLine("controllable = Yes"));
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
            if(StructureByLocation.ContainsKey("Turret"))
                writer.WriteLine(OutputTableLine($"turretarmor = {PrintLocationArmorAndStructure("Turret")}"));

            writer.WriteLine(OutputTableLine($"weapon1 = {OutputEquipmentForTable(Weapons)}"));

            writer.WriteLine(OutputTableLine($"ammo1 = {OutputEquipmentForTable(Ammo)}"));

            writer.WriteLine(OutputTableLine($"gear1 = {OutputEquipmentForTable(UtilityGear)}"));

            writer.WriteLine("}}");
            writer.WriteLine("<br>");
            writer.WriteLine("===Description===");

            string baseDescription = ModJsonHandler.GetDescriptionDetailsFromJsonDoc(UnitDefFile);
            string[] tempDescriptionParse = baseDescription.Split("<b>");
            string communityContentText = "";
            if(baseDescription.Contains("COMMUNITY CONTENT"))
                communityContentText = "<b>" + tempDescriptionParse[tempDescriptionParse.Length - 1];

            writer.WriteLine(tempDescriptionParse[0] + communityContentText);

            writer.WriteLine("<br>");
            writer.WriteLine("===Factions===");
            FactionDataHandler.TagsListToFactionsSection(Tags, writer);
            writer.WriteLine("</tab>");
        }

        private string OutputTableLine(string line)
        {
            return "| " + line;
        }

        private string ConvertVehicleMovementTypeToString(VehicleMovementTypes type)
        {
            switch (type)
            {
                case VehicleMovementTypes.Wheeled:
                    return "Wheeled";
                case VehicleMovementTypes.Tracked:
                    return "Tracked";
                case VehicleMovementTypes.Hover:
                    return "Hover";
                case VehicleMovementTypes.VTOL:
                    return "VTOL";
                case VehicleMovementTypes.Jet:
                    return "Jet";
            }

            return "ERROR";
        }

        private string TotalLocationStatType(Dictionary<string, double> locationMap, Dictionary<string, List<Statisticdata>> locationModifierMap)
        {
            double totalValue = 0;
            foreach(string location in Locations)
            {
                if(locationMap.ContainsKey(location))
                    totalValue += GetFinalStatForLocation(location, locationMap, locationModifierMap);
            }

            return totalValue.ToString();
        }

        private string PrintLocationArmorAndStructure(string location)
        {
            if (!StructureByLocation.ContainsKey(location))
                return "N/A";

            return $"{GetFinalStatForLocation(location, ArmorByLocation, ArmorModifiersByLocation)}/{GetFinalStatForLocation(location, StructureByLocation, StructureModifiersByLocation)}";
        }

        public double GetFinalStatForLocation(string location, Dictionary<string, double> locationMap, Dictionary<string, List<Statisticdata>> locationModifierMap)
        {
            double armorAdd = 0;
            double armorMulti = GlobalArmorModifier;

            foreach (Statisticdata statData in locationModifierMap[location])
            {
                if (statData.operation.Equals("Float_Add"))
                    armorAdd += Double.Parse(statData.modValue);
                if (statData.operation.Equals("Float_Multiply"))
                    armorMulti *= Double.Parse(statData.modValue);
            }

            return (locationMap[location] + armorAdd) * armorMulti;
        }

        private string OutputEquipmentForTable(List<EquipmentData> equipmentData)
        {
            StringBuilder gearStringBuilder = new StringBuilder();

            if(equipmentData.Count == 0)
                gearStringBuilder.Append("None");

            bool first = true;

            foreach(EquipmentData equipment in equipmentData)
            {
                if (!first)
                    gearStringBuilder.Append("<br>");
                else first = false;

                gearStringBuilder.Append(equipment.UIName);
            }

            return gearStringBuilder.ToString();
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

            return engineShieldGearId + " TYPE NOT FOUND";
        }

        private void GetVehicleGearList()
        {
            if (UnitDefFile.RootElement.TryGetProperty("inventory", out JsonElement gearInventory))
                foreach (JsonElement gear in gearInventory.EnumerateArray())
                {
                    string gearLocation = gear.GetProperty("MountedLocation").ToString();
                    string gearId = gear.GetProperty("ComponentDefID").ToString();
                    string componentType = gear.GetProperty("ComponentDefType").ToString();

                    if (MechGearHandler.TryGetEquipmentData(gearId, out EquipmentData equipmentData))
                    {
                        VehicleGearByLocation[gearLocation].Add(equipmentData);

                        if(equipmentData.GearType.Contains(GearCategory.ArmorModItem))
                        {
                            if (equipmentData.LocalArmorFactor != null)
                                ArmorModifiersByLocation[gearLocation].Add(equipmentData.LocalArmorFactor);
                            if(equipmentData.GlobalArmorFactor.HasValue)
                                GlobalArmorModifier *= equipmentData.GlobalArmorFactor.Value;
                        }
                        if (equipmentData.GearType.Contains(GearCategory.StructureModItem))
                        {
                            if (equipmentData.LocalStructureFactor != null)
                                StructureModifiersByLocation[gearLocation].Add(equipmentData.LocalStructureFactor);
                            if(equipmentData.GlobalStructureFactor.HasValue)
                                GlobalStructureModifier *= equipmentData.GlobalStructureFactor.Value;
                        }

                        if (componentType == "Weapon")
                            Weapons.Add(equipmentData);
                        else if (componentType == "AmmunitionBox")
                            Ammo.Add(equipmentData);
                        else
                        {
                            if (equipmentData.GearType.Contains(GearCategory.TankStuff))
                            {
                                if (IsListableTankEquipment(gearId))
                                    UtilityGear.Add(equipmentData);
                            }
                            else if(!equipmentData.IsCoreGear() && !equipmentData.GearType.Contains(GearCategory.Heatsink))
                                UtilityGear.Add(equipmentData);
                        }
                    }
                }
            else
                Logging.AddLogToQueue($"FAILURE TO GET VEHICLE GEAR {ChassisName}", LogLevel.Error, LogCategories.VehicleDefs);
        }

        private bool IsListableTankEquipment(string gearId)
        {
            switch (gearId)
            {
                case "Tank_CASE":
                    return true;
                case "Tank_CASE_CLAN":
                    return true;
                case "Gear_Vehicle_Hard_Target":
                    return true;
                case "Gear_Vehicle_LOW_PROFILE":
                    return true;
                case "Gear_Vehicle_TargetingComputer":
                    return true;
                case "Tank_InfantryCompartment":
                    return true;
                case "Tank_BattleArmorBay":
                    return true;
                case "Tank_BattleArmorFiringPorts":
                    return true;
                case "Gear_VTOL_Airlift_Hoists":
                    return true;
                case "Gear_VTOL_Armor_Plating":
                    return true;
                case "Gear_VTOL_Reinforced":
                    return true;
                case "Gear_VTOL_Chin_Turret":
                    return true;
                case "Gear_Airship_Propulsion":
                    return true;
                case "Gear_Vehicle_Comm_Suite":
                    return true;
            }
            return false;
        }

        private void GetUnitTonnage()
        {
            if (ChassisDefFile.RootElement.TryGetProperty("Tonnage", out JsonElement tonnage))
                Weight = tonnage.GetInt32();
            else
                Logging.AddLogToQueue($"FAILURE TO GET TONNAGE {ChassisName}", LogLevel.Error, LogCategories.VehicleDefs);
        }

        private void GetCoreGear()
        {
            List<EquipmentData> AllGearList = new List<EquipmentData>();

            foreach (string location in Locations)
            {
                AllGearList.AddRange(VehicleGearByLocation[location]);
            }

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
                    }
                }
            }
        }

        private void MovementDecode()
        {
            if (VariantName.Contains("Aerospace"))
                VehicleMoveType = VehicleMovementTypes.Jet;
            else if (Tags.Contains("unit_vtol"))
                VehicleMoveType = VehicleMovementTypes.VTOL;
            else if (Tags.Contains("unit_hover"))
                VehicleMoveType = VehicleMovementTypes.Hover;
            else if (Tags.Contains("unit_wheeled") || Tags.Contains("unit_wheels"))
                VehicleMoveType = VehicleMovementTypes.Wheeled;
            else if (Tags.Contains("unit_tracked") || Tags.Contains("unit_tracks"))
                VehicleMoveType = VehicleMovementTypes.Tracked;
        }

        private void SetPlayerControllableFromTags()
        {
            if (Tags.Contains("unit_controllableTank"))
                PlayerControllable = true;
            else
                PlayerControllable = false;
        }

        private void CalculateAllMovements()
        {
            string movementDef = ChassisDefFile.RootElement.GetProperty("MovementCapDefID").ToString();

            if (!movementDef.Contains('-'))
            {
                Logging.AddLogToQueue($"Vehicle has incorrect movement def: {ModJsonHandler.GetIdFromJsonDoc(ChassisDefFile)}", LogLevel.Warning, LogCategories.VehicleDefs);

                WalkSpeed = 0;
                RunSpeed = 0;
            }
            else
            {
                string[] moveSpeeds = movementDef.Split('_')[1].Replace("cv", "").Split('-');

                WalkSpeed = Int32.Parse(moveSpeeds[0]);
                RunSpeed = Int32.Parse(moveSpeeds[1]);
            }
        }

        private void CalculateDamageTotals()
        {
            TotalDamage = 0;
            TotalDamageHeat = 0;
            TotalDamageStability = 0;

            foreach(EquipmentData weapon in Weapons)
            {
                TotalDamage += (weapon.Damage ?? 0) * (weapon.Shots ?? 0);
                TotalDamageHeat += (weapon.DamageHeat ?? 0) * (weapon.Shots ?? 0);
                TotalDamageStability += (weapon.DamageStability ?? 0) * (weapon.Shots ?? 0);
            }
        }

        private void PopulateTagsForVehicle()
        {
            if (UnitDefFile.RootElement.TryGetProperty("VehicleTags", out JsonElement chassisTags))
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

            if (Tags.Contains("BLACKLISTED"))
                Blacklisted = true;
            else
                Blacklisted = false;
        }

        private void PopulateArmorAndStructureByLocation()
        {
            foreach(JsonElement locationData in UnitDefFile.RootElement.GetProperty("Locations").EnumerateArray())
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
                    Logging.AddLogToQueue($"Invalid location {location} found in vehicle {VariantName}", LogLevel.Error, LogCategories.VehicleDefs);
                }
            }
        }
    }
}
