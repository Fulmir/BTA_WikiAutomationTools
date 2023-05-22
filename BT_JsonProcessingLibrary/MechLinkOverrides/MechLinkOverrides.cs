using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UtilityClassLibrary;

namespace BT_JsonProcessingLibrary
{
    public class MechLinkOverrides
    {
        private static Dictionary<string, string> VariantOverridesList = new Dictionary<string, string>();
        public static void PopulateMechOverrides()
        {
            VariantOverridesList = TextFileListProcessor.GetStringListFromFile(".\\MechLinkOverrides\\VariantOverridesList.txt").ToDictionary(
                (csvString) => csvString.Split(',')[0]
                , (csvString) => csvString.Split(',')[1]);
        }

        public static bool TryGetLinkOverride(string variant, out string linkOverride)
        {
            if(VariantOverridesList.Count == 0)
                PopulateMechOverrides();

            return VariantOverridesList.TryGetValue(variant, out linkOverride);
        }

        public static bool HasOverrideForVariant(string variant)
        {
            if (VariantOverridesList.Count == 0)
                PopulateMechOverrides();

            return VariantOverridesList.ContainsKey(variant);
        }
    }
}
