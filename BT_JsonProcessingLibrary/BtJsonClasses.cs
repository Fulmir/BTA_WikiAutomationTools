using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BT_JsonProcessingLibrary
{

    public class StatusEffect
    {
        public Durationdata durationData { get; set; }
        public Targetingdata targetingData { get; set; }
        public string effectType { get; set; }
        public Description Description { get; set; }
        public Statisticdata? statisticData { get; set; }
        public string nature { get; set; }
    }

    public class Durationdata
    {
        public int duration { get; set; }
        public int stackLimit { get; set; }
    }

    public class Targetingdata
    {
        public string effectTriggerType { get; set; }
        public string specialRules { get; set; }
        public string effectTargetType { get; set; }
        public float range { get; set; }
        public bool? forcePathRebuild { get; set; }
        public bool? forceVisRebuild { get; set; }
        public bool? showInTargetPreview { get; set; }
        public bool? showInStatusPanel { get; set; }
    }

    public class Description
    {
        public int? Cost { get; set; }
        public int? Rarity { get; set; }
        public bool? Purchasable { get; set; }
        public string? Manufacturer { get; set; }
        public string? Model { get; set; }
        public string? UIName { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Details { get; set; }
        public string Icon { get; set; }
    }


    public class Statisticdata
    {
        public string statName { get; set; }
        public string operation { get; set; }
        public string modValue { get; set; }
        public string modType { get; set; }
        public bool? effectsPersistAfterDestruction { get; set; }
    }

}
