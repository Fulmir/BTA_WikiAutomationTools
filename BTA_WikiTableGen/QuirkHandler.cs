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

        static List<string> LimitedBonusStrings = new List<string>() { "ArmAccuracy", "MinWeightJJ", "MaxWeightJJ", "MaxCountJJ" };

        static List<string> BonusOutputBlacklist = new List<string>() { "GyroStab", "IsGyro", "FCS",  };

        private static Dictionary<string, QuirkDef> QuirkLookupTable = new Dictionary<string, QuirkDef>();
        private static Dictionary<string, BonusDef> BaseBonusLookupTable = new Dictionary<string, BonusDef>();

        public static void CreateQuirkBonusesIndex(string modsFolder)
        {
            string mechEngBonusDefFile = Path.Combine(modsFolder, "BT Advanced Core\\settings\\bonusDescriptions\\BonusDescriptions_MechEngineer.json");

            JsonDocument mechEngBonusDescriptions = JsonDocument.Parse(new StreamReader(mechEngBonusDefFile).ReadToEnd());

            double LongLengthTotal = 0;
            double LongLengthCount = 0;
            double FullLengthTotal = 0;
            double FullLengthCount = 0;

            foreach(JsonElement bonusDesc in mechEngBonusDescriptions.RootElement.GetProperty("Settings").EnumerateArray())
            {
                var tempBonus = new BonusDef()
                {
                    BonusId = bonusDesc.GetProperty("Bonus").ToString(),
                    LongDescription = bonusDesc.GetProperty("Long").ToString(),
                    StackingLimit = CheckIfQuirkBonusLimit(bonusDesc.GetProperty("Bonus").ToString()) ? 1 : -1
                };
                if(bonusDesc.TryGetProperty("Full", out JsonElement fullText))
                {
                    tempBonus.FullDescription = fullText.ToString();
                    FullLengthTotal += tempBonus.FullDescription.Length;
                    FullLengthCount++;
                }

                LongLengthTotal += tempBonus.LongDescription.Length;
                LongLengthCount++;

                BaseBonusLookupTable.Add(bonusDesc.GetProperty("Bonus").ToString(), tempBonus);
            }

            Console.WriteLine("");
            Console.WriteLine($"Full Description Average Length: {FullLengthTotal/FullLengthCount}");
            Console.WriteLine("");
            Console.WriteLine($"Long Description Average Length: {LongLengthTotal/LongLengthCount}");
            Console.WriteLine("");

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
            bool prevEndedInPunctuation = false;
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
                                return (possiblyPercentage[1].Value.Contains('+') ? "+" : "") + modVal + possiblyPercentage[2].Value;
                            }
                            return (possiblyPercentage[1].Value.Contains('+') ? "+" : "") + modVal + "";
                        }
                        return val;
                    }).ToList();
                }

                bool fullDescUsable = true;
                if(bonus.FullDescription != null && fullDesc)
                {
                    if (tempBonusValues.Count() > 0 && tempBonusValues[0] != "" && !bonus.LongDescription.Contains("{0}"))
                        fullDescUsable = true;
                    else if (bonus.FullDescription.Length > 55)
                    {
                        fullDescUsable = false;
                        //if(!new List<string> { "Omni", "IndividualResolve", "MaxResolveIncrease", "MultiTrac", "BAMounts", "360Twist", "NoBleedout" }.Contains(bonus.BonusId))
                        //{
                        //    Console.WriteLine($"DISCARDED FULL DESCRIPTION: {bonus.BonusId}");
                        //    Console.WriteLine($"Full: {string.Format(bonus.FullDescription, tempBonusValues.ToArray())}");
                        //    Console.WriteLine($"Long: {string.Format(bonus.LongDescription, tempBonusValues.ToArray())}");
                        //    Console.WriteLine("");
                        //}
                    }

                    if (bonus.FullDescription.Contains("{0}") && !bonus.LongDescription.Contains("{0}"))
                    {
                        //Console.WriteLine($"Bonus: {bonus.BonusId} does not have parameter in Long Description.");
                        //Console.WriteLine($"Full: {bonus.FullDescription}");
                        //Console.WriteLine($"Long: {bonus.LongDescription}");
                        //Console.WriteLine("");
                    }
                }
                if (first) first = false;
                else
                {
                    if (!prevEndedInPunctuation)
                        stringWriter.Write(", ");
                    stringWriter.Write(" ");
                }

                string tempBonusWrite = string.Format((fullDesc && fullDescUsable) ? bonus.FullDescription ?? bonus.LongDescription : bonus.LongDescription, tempBonusValues.ToArray()).Trim();
                stringWriter.Write(tempBonusWrite);

                if(new List<char> { ',', '.', '!', '?' }.Contains(tempBonusWrite.Last()))
                    prevEndedInPunctuation = true;
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
