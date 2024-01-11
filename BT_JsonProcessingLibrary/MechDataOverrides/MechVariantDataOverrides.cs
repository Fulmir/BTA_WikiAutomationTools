using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UtilityClassLibrary;

namespace BT_JsonProcessingLibrary
{
    public class MechVariantDataOverrides
    {
        private static Dictionary<string, string> LinkOverridesList = new Dictionary<string, string>();
        private static Dictionary<string, string> NameOverridesList = new Dictionary<string, string>();
        private static Dictionary<string, string> GroupingOverridesList = new Dictionary<string, string>();

        public static void PopulateOverrideLists()
        {
            PopulateMechLinkOverrides();
            PopulateMechNameOverrides();
            PopulateMechGroupingOverrides();
        }

        private static void PopulateMechLinkOverrides()
        {
            PopulateDictionaryFromTextFile(ref LinkOverridesList, ".\\MechDataOverrides\\VariantOverridesList.txt");
        }
        private static void PopulateMechNameOverrides()
        {
            PopulateDictionaryFromTextFile(ref NameOverridesList, ".\\MechDataOverrides\\NameOverridesList.txt");
        }
        private static void PopulateMechGroupingOverrides()
        {
            PopulateDictionaryFromTextFile(ref GroupingOverridesList, ".\\MechDataOverrides\\GroupedMechEntries.txt");
        }

        public static bool TryGetLinkOverride(string variant, out string linkOverride)
        {
            if(LinkOverridesList.Count == 0)
                PopulateMechLinkOverrides();

            return LinkOverridesList.TryGetValue(variant, out linkOverride);
        }

        public static bool HasLinkOverrideForVariant(string variant)
        {
            if (LinkOverridesList.Count == 0)
                PopulateMechLinkOverrides();

            return LinkOverridesList.ContainsKey(variant);
        }

        public static bool TryGetNameOverride(string variant, out string nameOverride)
        {
            if (NameOverridesList.Count == 0)
                PopulateMechNameOverrides();

            return NameOverridesList.TryGetValue(variant, out nameOverride);
        }

        public static bool HasNameOverrideForVariant(string variant)
        {
            if (NameOverridesList.Count == 0)
                PopulateMechNameOverrides();

            return NameOverridesList.ContainsKey(variant);
        }

        public static bool TryGetGroupOverride(string variant, out string groupOverride)
        {
            if (GroupingOverridesList.Count == 0)
                PopulateMechGroupingOverrides();

            return GroupingOverridesList.TryGetValue(variant, out groupOverride);
        }

        public static bool HasGroupOverrideForVariant(string variant)
        {
            if (GroupingOverridesList.Count == 0)
                PopulateMechGroupingOverrides();

            return GroupingOverridesList.ContainsKey(variant);
        }

        private static void PopulateDictionaryFromTextFile(ref Dictionary<string, string> targetDict, string filePath)
        {
            targetDict = TextFileListProcessor.GetStringListFromFile(filePath).ToDictionary(
                (csvString) => csvString.Split(',')[0]
                , (csvString) => csvString.Split(',')[1]);
        }
    }
}
