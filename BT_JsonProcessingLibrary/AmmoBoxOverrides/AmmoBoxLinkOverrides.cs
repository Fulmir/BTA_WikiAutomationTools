using BT_JsonProcessingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UtilityClassLibrary.WikiLinkOverrides
{
    public static class AmmoBoxLinkOverrides
    {
        private static Dictionary<string, string> AmmoTypesNamesOverridesList = new Dictionary<string, string>();
        private static Dictionary<string, BasicLinkData> AmmoCategoryLinkOverrides = new Dictionary<string, BasicLinkData>();
        public static void PopulateMechOverrides()
        {
            AmmoTypesNamesOverridesList = TextFileListProcessor.GetStringListFromFile(".\\AmmoBoxOverrides\\AmmoTypeLinkOverrides.txt").ToDictionary(
                (csvString) => csvString.Split(',')[0]
                , (csvString) => csvString.Split(',')[1]);
        }

        public static void PopulateAmmoCategoryOverrides()
        {
            foreach (string ammoOverride in TextFileListProcessor.GetStringListFromFile(".\\AmmoBoxOverrides\\AmmoCategoryLinkOverrides.txt"))
            {
                string[] csvString = ammoOverride.Split(',');
                AmmoCategoryLinkOverrides.Add(csvString[0], new BasicLinkData(csvString[1], csvString[2]));
            }
        }

        public static bool TryGetLinkOverride(string uiName, out string linkOverride)
        {
            if (AmmoTypesNamesOverridesList.Count == 0)
                PopulateMechOverrides();

            return AmmoTypesNamesOverridesList.TryGetValue(uiName, out linkOverride);
        }

        public static bool HasOverrideForVariant(string uiName)
        {
            if (AmmoTypesNamesOverridesList.Count == 0)
                PopulateMechOverrides();

            return AmmoTypesNamesOverridesList.ContainsKey(uiName);
        }

        public static bool TryGetCategoryLink(string ammoCategory, out string ammoTypeLink)
        {
            if (AmmoCategoryLinkOverrides.ContainsKey(ammoCategory))
            {
                ammoTypeLink = $"[[Ammunition#{MediaWikiTextEncoder.ConvertToMediaWikiSafeText(AmmoCategoryLinkOverrides[ammoCategory].Link)}|{AmmoCategoryLinkOverrides[ammoCategory].UiName}]]";
                return true;
            }
            ammoTypeLink = "N/A";
            return false;
        }
    }
}
