using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UtilityClassLibrary.WikiLinkOverrides
{
    public class WeaponLinkOverrides
    {
        private static Dictionary<string, string> WeaponOverridesList = new Dictionary<string, string>();
        private static Dictionary<string, string> WeaponOverridesRegexList = new Dictionary<string, string>();

        private static Dictionary<string, string> CommunityContentWeaponOverridesList = new Dictionary<string, string>();
        private static Dictionary<string, string> CommunityContentWeaponOverridesRegexList = new Dictionary<string, string>();

        private static Dictionary<string, string> BaWeaponOverridesList = new Dictionary<string, string>();

        private static string FolderAddress = "WikiLinkOverrides\\WeaponLinkOverrides\\";
        public static void PopulateWeaponOverrides()
        {
            PopulateNormalWeaponOverrides();

            PopulateCommunityContentWeaponOverrides();

            PopulateBattleArmorWeaponOverrides();
        }

        private static void PopulateNormalWeaponOverrides()
        {
            WeaponOverridesList = TextFileListProcessor.GetStringListFromFile($".\\{FolderAddress}WeaponOverridesList.txt").ToDictionary(
                (csvString) => csvString.Split(',')[0]
                , (csvString) => csvString.Split(',')[1]);

            WeaponOverridesRegexList = TextFileListProcessor.GetStringListFromFile($".\\{FolderAddress}WeaponOverridesRegexList.txt").ToDictionary(
                (csvString) => csvString.Split(',')[0]
                , (csvString) => csvString.Split(',')[1]);
        }

        private static void PopulateCommunityContentWeaponOverrides()
        {
            CommunityContentWeaponOverridesList = TextFileListProcessor.GetStringListFromFile($".\\{FolderAddress}CommunityContentWeaponOverridesList.txt").ToDictionary(
                (csvString) => csvString.Split(',')[0]
                , (csvString) => csvString.Split(',')[1]);

            CommunityContentWeaponOverridesRegexList = TextFileListProcessor.GetStringListFromFile($".\\{FolderAddress}CommunityContentWeaponOverridesRegexList.txt").ToDictionary(
                (csvString) => csvString.Split(',')[0]
                , (csvString) => csvString.Split(',')[1]);
        }

        private static void PopulateBattleArmorWeaponOverrides()
        {
            BaWeaponOverridesList = TextFileListProcessor.GetStringListFromFile($".\\{FolderAddress}BaWeaponOverridesList.txt").ToDictionary(
                (csvString) => csvString.Split(',')[0]
                , (csvString) => csvString.Split(',')[1]);
        }

        public static bool TryGetLinkOverride(string fileDefPath, string weaponId, string UiName, out string linkOverride)
        {
            if (fileDefPath.Contains("Battle Armor"))
            {
                if (BaWeaponOverridesList.Count() == 0)
                    PopulateBattleArmorWeaponOverrides();

                foreach(string checkString in BaWeaponOverridesList.Keys)
                    if(ItemLinkUtilities.RunStringCheckOnName(checkString, UiName))
                    {
                        linkOverride = BaWeaponOverridesList[checkString];
                        return true;
                    }
            }
            else if (fileDefPath.Contains("Community Content"))
            {
                if (CommunityContentWeaponOverridesList.Count() == 0 || CommunityContentWeaponOverridesRegexList.Count() == 0)
                    PopulateCommunityContentWeaponOverrides();

                foreach (string checkString in CommunityContentWeaponOverridesList.Keys)
                    if (ItemLinkUtilities.RunStringCheckOnName(checkString, UiName))
                    {
                        linkOverride = CommunityContentWeaponOverridesList[checkString];
                        return true;
                    }
                foreach (string checkString in CommunityContentWeaponOverridesRegexList.Keys)
                    if (ItemLinkUtilities.RunRegexCheckOnName(checkString, UiName))
                    {
                        linkOverride = CommunityContentWeaponOverridesRegexList[checkString];
                        return true;
                    }
            }
            else
            {
                if (WeaponOverridesList.Count() == 0 || WeaponOverridesRegexList.Count() == 0)
                    PopulateNormalWeaponOverrides();

                foreach (string checkString in WeaponOverridesList.Keys)
                    if (ItemLinkUtilities.RunStringCheckOnName(checkString, UiName))
                    {
                        linkOverride = WeaponOverridesList[checkString];
                        return true;
                    }
                foreach (string checkString in WeaponOverridesRegexList.Keys)
                    if (ItemLinkUtilities.RunRegexCheckOnName(checkString, UiName))
                    {
                        linkOverride = WeaponOverridesRegexList[checkString];
                        return true;
                    }
            }

            linkOverride = "ERROR";
            return false;
        }
    }
}
