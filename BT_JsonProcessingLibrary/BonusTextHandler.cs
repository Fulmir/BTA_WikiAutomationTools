using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UtilityClassLibrary;

namespace BT_JsonProcessingLibrary
{
    public static class BonusTextHandler
    {
        private static ConcurrentDictionary<string, BonusDef> BaseBonusLookupTable = new ConcurrentDictionary<string, BonusDef>();

        static List<string> LimitedBonusStrings = new List<string>() { "ArmAccuracy", "MinWeightJJ", "MaxWeightJJ", "MaxCountJJ" };

        static Regex BonusNumberExtractor = new Regex("([+\\-\\d]+)([%]?)", RegexOptions.Compiled);

        static List<string> DebugBlacklist = new List<string>();

        public static BonusDef GetBaseBonusDef(string id)
        {
            return BaseBonusLookupTable[id];
        }

        public static bool TryGetBaseBonusDef(string id, out BonusDef bonusDef)
        {
            if (BaseBonusLookupTable.ContainsKey(id))
            {
                bonusDef = BaseBonusLookupTable[id];
                return true;
            }
            else
            {
                bonusDef = new BonusDef();
                return false;
            }
        }

        public static bool ContainsBaseBonusDef(string id)
        {
            return BaseBonusLookupTable.ContainsKey(id);
        }

        public static void CreateEquipmentBonusesIndex(string modsFolder)
        {
            string mechEngBonusDefFile = Path.Combine(modsFolder, "BT Advanced Core\\settings\\bonusDescriptions\\BonusDescriptions_MechEngineer.json");

            JsonDocument mechEngBonusDescriptions = JsonDocument.Parse(new StreamReader(mechEngBonusDefFile).ReadToEnd());

            double LongLengthTotal = 0;
            double LongLengthCount = 0;
            double FullLengthTotal = 0;
            double FullLengthCount = 0;

            foreach (JsonElement bonusDesc in mechEngBonusDescriptions.RootElement.GetProperty("Settings").EnumerateArray())
            {
                var tempBonus = new BonusDef()
                {
                    BonusId = bonusDesc.GetProperty("Bonus").ToString(),
                    LongDescription = bonusDesc.GetProperty("Long").ToString(),
                    StackingLimit = CheckIfQuirkBonusLimit(bonusDesc.GetProperty("Bonus").ToString()) ? 1 : -1
                };
                if (bonusDesc.TryGetProperty("Full", out JsonElement fullText))
                {
                    tempBonus.FullDescription = fullText.ToString();
                    FullLengthTotal += tempBonus.FullDescription.Length;
                    FullLengthCount++;
                }

                LongLengthTotal += tempBonus.LongDescription.Length;
                LongLengthCount++;

                BaseBonusLookupTable.TryAdd(tempBonus.BonusId, tempBonus);
            }
        }

        public static string PrintBonusToString(BonusDef bonus, bool forceFullDesc = false, int gearInstanceCount = 1)
        {
            StringBuilder bonusOutputBuilder = new StringBuilder();

            List<string> tempBonusValues = bonus.BonusValues;
            if (bonus.StackingLimit != 1 && bonus.BonusValues.Count > 0 && gearInstanceCount > 1)
            {
                tempBonusValues = bonus.BonusValues.Select((val) =>
                {
                    if (BonusNumberExtractor.IsMatch(val))
                    {
                        GroupCollection possiblyPercentage = BonusNumberExtractor.Match(val).Groups;
                        // Check that the stacking limit is -1 (unlimited) or that the Instance count is less than the stacking limit. If so then multiply the bonus.
                        double modVal = Convert.ToDouble(possiblyPercentage[1].Value) * ((gearInstanceCount <= bonus.StackingLimit || bonus.StackingLimit == -1) ? gearInstanceCount : bonus.StackingLimit);
                        if (possiblyPercentage.Count > 2)
                        {
                            return (possiblyPercentage[1].Value.Contains('+') ? "+" : "") + modVal + possiblyPercentage[2].Value;
                        }
                        return (possiblyPercentage[1].Value.Contains('+') ? "+" : "") + modVal + "";
                    }
                    return val;
                }).ToList();
            }

            bool fullDescUsable = false;
            if (bonus.FullDescription != null)
            {
                if (forceFullDesc)
                    fullDescUsable = true;

                else if (tempBonusValues.Count() > 0 && tempBonusValues[0] != "" && !bonus.LongDescription.Contains("{0}"))
                    fullDescUsable = true;
                else if (bonus.FullDescription.Length > 40)
                {
                    fullDescUsable = false;
                    if (!DebugBlacklist.Contains(bonus.BonusId))
                    {
                        Logging.AddLogToQueue($"DISCARDED FULL DESCRIPTION: {bonus.BonusId}\n\r" +
                            $"Full: {string.Format(bonus.FullDescription, tempBonusValues.ToArray())}\n\r" +
                            $"Long: {string.Format(bonus.LongDescription, tempBonusValues.ToArray())}"
                            , LogLevel.Reporting, LogCategories.Bonuses);
                        DebugBlacklist.Add(bonus.BonusId);
                    }
                }
            }

            string tempBonusWrite = string.Format(fullDescUsable ? bonus.FullDescription ?? bonus.LongDescription : bonus.LongDescription, tempBonusValues.ToArray()).Trim();
            bonusOutputBuilder.Append(tempBonusWrite);

            return bonusOutputBuilder.ToString();
        }

        private static bool CheckIfQuirkBonusLimit(string bonusId)
        {
            return LimitedBonusStrings.Contains(bonusId);
        }
    }
}
