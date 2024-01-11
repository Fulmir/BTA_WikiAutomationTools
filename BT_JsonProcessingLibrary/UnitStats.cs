using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UtilityClassLibrary;

namespace BT_JsonProcessingLibrary
{
    public abstract class UnitStats
    {
        public string VariantName { get; set; }
        public string ChassisName { get; set; }
        public int Weight { get; set; } = 0;
        public string EngineTypeId { get; set; } = string.Empty;
        public string EngineCoreId { get; set; } = string.Empty;
        public int EngineSize { get; set; } = 0;
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
        public virtual string[] Locations { get; } = new string[0];
        public List<EquipmentData> Weapons { get; set; } = new List<EquipmentData>();
        public List<EquipmentData> Ammo { get; set; } = new List<EquipmentData>();
        public List<EquipmentData> UtilityGear { get; set; } = new List<EquipmentData>();
        public double WalkSpeed { get; set; } = 0;
        public double RunSpeed { get; set; } = 0;
        public double JumpDistance { get; set; } = 0;
        public JsonDocument ChassisDefFile { get; set; }
        public JsonDocument UnitDefFile { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public List<string> WikiTags { get; set; } = new List<string>();
        public bool PlayerControllable { get; set; }
        public bool Blacklisted { get; set; }


        public UnitStats(string chassisName, string variantName, BasicFileData chassisDef, BasicFileData unitDef)
        {
            ChassisName = chassisName;
            VariantName = variantName;

            ChassisDefFile = JsonDocument.Parse(new StreamReader(chassisDef.Path).ReadToEnd(), UtilityStatics.GeneralJsonDocOptions);
            MechDefFile = JsonDocument.Parse(new StreamReader(mechDef.Path).ReadToEnd());

            CalculateMechStats();
        }
    }
}
