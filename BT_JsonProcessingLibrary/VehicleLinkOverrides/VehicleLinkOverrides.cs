using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UtilityClassLibrary;

namespace BT_JsonProcessingLibrary
{
    public class VehicleLinkOverrides
    {
        private static Dictionary<string, string> VariantOverridesList = new Dictionary<string, string>();
        public static void PopulateVehicleOverrides()
        {
            VariantOverridesList = TextFileListProcessor.GetStringListFromFile(".\\VehicleLinkOverrides\\VehicleNameOverridesList.txt").ToDictionary(
                (csvString) => csvString.Split(',')[0]
                , (csvString) => csvString.Split(',')[1]);
        }

        public static bool TryGetLinkOverride(string vehicleChassisName, out string linkOverride)
        {
            if(VariantOverridesList.Count == 0)
                PopulateVehicleOverrides();

            return VariantOverridesList.TryGetValue(vehicleChassisName, out linkOverride);
        }

        public static bool HasOverrideForVehicleChassis(string vehicleChassisName)
        {
            if (VariantOverridesList.Count == 0)
                PopulateVehicleOverrides();

            return VariantOverridesList.ContainsKey(vehicleChassisName);
        }
    }
}
