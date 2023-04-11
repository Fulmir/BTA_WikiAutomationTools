using BT_JsonProcessingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace BTA_WikiTableGen
{
    internal static class QuirkHandler
    {
        static Regex[] QuirkGearPatterns = {
            new Regex(".*_Quirk.*", RegexOptions.IgnoreCase|RegexOptions.Compiled),
            new Regex(".*Gyro.*Omni.*", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(".*Gyro.*Quad.*", RegexOptions.IgnoreCase | RegexOptions.Compiled)
        };

        static Regex GearTextQuirkCheckRegex = new Regex("(\"no_salvage\").*(\"BLACKLISTED\")", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        static Regex PathQuirkExcludeRegex = new Regex(".*(VIPAdvanced|Battle Armor).*", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        static Regex BonusNumberExtractor = new Regex("([+\\-\\d]+)([%]?)", RegexOptions.Compiled);

        static List<string> LimitedBonusStrings = new List<string>() { "ArmAccuracy" };

        static List<string> BonusOutputBlacklist = new List<string>() { "GyroStab", "IsGyro" };

        private static Dictionary<string, QuirkDef> QuirkLookupTable = new Dictionary<string, QuirkDef>();
        private static Dictionary<string, BonusDef> BaseBonusLookupTable = new Dictionary<string, BonusDef>();

        public static void CreateQuirkBonusesIndex(string modsFolder)
        {
            string mechEngBonusDefFile = Path.Combine(modsFolder, "BT Advanced Core\\settings\\bonusDescriptions\\BonusDescriptions_MechEngineer.json");

            JsonDocument mechEngBonusDescriptions = JsonDocument.Parse(new StreamReader(mechEngBonusDefFile).ReadToEnd());

            foreach(JsonElement bonusDesc in mechEngBonusDescriptions.RootElement.GetProperty("Settings").EnumerateArray())
            {
                var tempBonus = new BonusDef()
                {
                    BonusId = bonusDesc.GetProperty("Bonus").ToString(),
                    LongDescription = bonusDesc.GetProperty("Long").ToString(),
                    StackingLimit = CheckIfQuirkBonusLimit(bonusDesc.GetProperty("Bonus").ToString()) ? 1 : -1
                };
                if(bonusDesc.TryGetProperty("Full", out JsonElement fullText))
                    tempBonus.FullDescription = fullText.ToString();

                BaseBonusLookupTable.Add(bonusDesc.GetProperty("Bonus").ToString(), tempBonus);
            }
        }

        public static bool CheckGearIsQuirk(EquipmentData equipmentData, out QuirkDef mechQuirk)
        {
            if(QuirkLookupTable.ContainsKey(equipmentData.Id))
            {
                mechQuirk = QuirkLookupTable[equipmentData.Id];
                return true;
            }
            mechQuirk = new QuirkDef();
            //if (PathQuirkExcludeRegex.IsMatch(filePath))
            //    return false;

            //string fileName = Path.GetFileName(filePath);

            foreach(Regex regex in QuirkGearPatterns)
            {
                if (regex.IsMatch(equipmentData.Id))
                {
                    QuirkDef tempQuirkResult = GetQuirkFromGearJson(equipmentData.GearJsonDoc);
                    mechQuirk = tempQuirkResult;
                    QuirkLookupTable.Add(tempQuirkResult.Id, tempQuirkResult);
                    return true;
                }
            }

            if (GearTextQuirkCheckRegex.IsMatch(equipmentData.GearJsonDoc.ToString()))
            {
                QuirkDef tempQuirkResult = GetQuirkFromGearJson(equipmentData.GearJsonDoc);
                mechQuirk = tempQuirkResult;
                QuirkLookupTable.Add(tempQuirkResult.Id, tempQuirkResult);
                return true;
            }

            return false;
        }

        public static List<QuirkDef> CompileQuirksForVariants(List<MechStats> mechList)
        {
            Dictionary<string, QuirkDef> quirks = new Dictionary<string, QuirkDef>();

            foreach (MechStats mech in mechList)
            {
                foreach(KeyValuePair<string, QuirkDef> quirk in mech.MechQuirks)
                {
                    quirks[quirk.Key] = quirk.Value;
                }
            }

            return quirks.Values.ToList();
        }

        public static void OutputQuirkToString(QuirkDef quirkDef, bool fullDesc, StringWriter stringWriter)
        {
            stringWriter.WriteLine($"'''[[Mech_Quirks|Mech Quirk: ]]''' {quirkDef.Name}");
            stringWriter.WriteLine();

            bool first = true;
            foreach(BonusDef bonus in quirkDef.QuirkBonuses)
            {
                if (BonusOutputBlacklist.Contains(bonus.BonusId))
                    continue;
                List<string> tempBonusValues = bonus.BonusValues;
                if (bonus.BonusValues.Count > 0 && quirkDef.InstanceCount > 1)
                {
                    tempBonusValues = bonus.BonusValues.Select((val) =>
                    {
                        if (BonusNumberExtractor.IsMatch(val))
                        {
                            GroupCollection possiblyPercentage = BonusNumberExtractor.Match(val).Groups;
                            double modVal = Convert.ToDouble(possiblyPercentage[1].Value) * quirkDef.InstanceCount;
                            if (possiblyPercentage.Count > 2)
                            {
                                return modVal + possiblyPercentage[2].Value;
                            }
                            return modVal + "";
                        }
                        return val;
                    }).ToList();
                }
                if (first) first = false;
                else stringWriter.Write(", ");
                stringWriter.Write(string.Format(fullDesc ? bonus.FullDescription?? bonus.LongDescription : bonus.LongDescription, tempBonusValues.ToArray()));
            }

            stringWriter.WriteLine();
            stringWriter.WriteLine();
        }

        private static QuirkDef GetQuirkFromGearJson(JsonDocument gearJson)
        {
            JsonElement bonuses = gearJson.RootElement.GetProperty("Custom").GetProperty("BonusDescriptions").GetProperty("Bonuses");

            QuirkDef output = new QuirkDef()
            {
                Id = gearJson.RootElement.GetProperty("Description").GetProperty("Id").ToString(),
                Name = gearJson.RootElement.GetProperty("Description").GetProperty("Name").ToString(),
                QuirkBonuses = new List<BonusDef>(),
                InstanceCount = 1
            };

            foreach(JsonElement bonusElement in bonuses.EnumerateArray())
            {
                string[] bonus = bonusElement.ToString().Split(':');

                BonusDef tempBonusDef = BaseBonusLookupTable[bonus[0]];

                if(bonus.Length > 1)
                    tempBonusDef.BonusValues = bonus[1].Split(",").Select((val) => val.Trim()).ToList();
                else
                    tempBonusDef.BonusValues = new List<string> { "" };

                output.QuirkBonuses.Add(tempBonusDef);
            }

            return output;
        }

        private static bool CheckIfQuirkBonusLimit(string bonusId)
        {
            return LimitedBonusStrings.Contains(bonusId);
        }
    }
}
