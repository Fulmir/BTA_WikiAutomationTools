using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
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

    public struct EquipmentData
    {
        public string Id { get; set; }
        public string UIName { get; set; }
        public double Tonnage { get; set; }
        public List<GearCategory> GearType { get; set; }
        public double? StructureFactor { get; set; }
        public JsonDocument GearJsonDoc { get; set; }
        public bool? IsQuirk { get; set; }
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
    }

    public struct MechNameCounter
    {
        public string MechName { get; set; }
        public int UseCount { get; set; }
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
        Weapon,
        Cockpit,
        LifeSupportA,
        LifeSupportB,
        Heatsink,
        Quirk
    }
}
