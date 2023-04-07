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
        Engine,
        EngineCore,
        HeatsinkKit,
        EngineHeatsinks,
        Armor,
        Structure,
        Weapon,
        Cockpit,
        LifeSupport,
        Heatsink,
        Gear
    }
}
