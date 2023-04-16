﻿using BT_JsonProcessingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UtilityClassLibrary;
using static System.Net.Mime.MediaTypeNames;

namespace BTA_WikiTableGen
{
    internal static class QuirkHandler
    {
        static Regex[] QuirkGearPatterns = {
            new Regex(".*_Quirk.*", RegexOptions.IgnoreCase|RegexOptions.Compiled),
            new Regex(".*Gyro.*Omni.*", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(".*Gyro.*Quad.*", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(".*Avionics.*", RegexOptions.IgnoreCase | RegexOptions.Compiled)
        };

        static List<string> DebugBlacklist = new List<string>();

        static List<string> GearBlacklistFromQuirkStatus = new List<string>() { "emod_armorslots_clstandard", "Gear_Cockpit_Industrial_AdvFCS", "Gear_Cockpit_Industrial" };

        static List<string> CommonQuirks = new List<string>();
        static List<string> HeadlineQuirks = new List<string>();

        static Regex GearTextQuirkCheckRegex = new Regex(@"(""no_salvage"").*(""BLACKLISTED"")", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        static Regex BonusNumberExtractor = new Regex("([+\\-\\d]+)([%]?)", RegexOptions.Compiled);

        static List<string> LimitedBonusStrings = new List<string>() { "ArmAccuracy", "MinWeightJJ", "MaxWeightJJ", "MaxCountJJ" };

        static List<string> BonusOutputBlacklist = new List<string>() { "GyroStab", "IsGyro", "FCS", };

        private static Dictionary<string, QuirkDef> QuirkLookupTable = new Dictionary<string, QuirkDef>();
        private static Dictionary<string, BonusDef> BaseBonusLookupTable = new Dictionary<string, BonusDef>();

        public static void LoadQuirkHandlerData(string modsFolder)
        {
            CommonQuirks = TextFileListProcessor.GetStringListFromFile(".\\CommonQuirksList.txt");
            CommonQuirks.Sort();
            HeadlineQuirks = TextFileListProcessor.GetStringListFromFile(".\\AdditionalHeadlineQuirksList.txt");
            HeadlineQuirks.AddRange(CommonQuirks);
            HeadlineQuirks.Sort();

            CreateQuirkBonusesIndex(modsFolder);
        }

        private static void CreateQuirkBonusesIndex(string modsFolder)
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

                BaseBonusLookupTable.Add(bonusDesc.GetProperty("Bonus").ToString(), tempBonus);
            }
        }

        public static string WriteOutCommonQuirkEffects()
        {
            StringBuilder sb = new StringBuilder();
            StringWriter writer = new StringWriter(sb);

            writer.WriteLine("==Quirks==");
            writer.WriteLine("The following quirks are present on a large amount of Mechs. In order to save space, here are their associated bonuses and penalties: ([[Mech_Quirks|For a full list of 'Mech quirks click here.]])");
            writer.WriteLine();
            writer.WriteLine();

            foreach (string quirkName in HeadlineQuirks)
            {
                OutputQuirkToString(QuirkLookupTable[quirkName], writer, true, true);
            }

            writer.Close();

            return sb.ToString();
        }

        public static bool CheckGearIsQuirk(EquipmentData equipmentData, out QuirkDef mechQuirk)
        {
            mechQuirk = new QuirkDef();
            if (GearBlacklistFromQuirkStatus.Contains(equipmentData.Id)
                || equipmentData.Id.Contains("engineslots")
                || equipmentData.GearType.Contains(GearCategory.MeleeWeapon))
            {
                return false;
            }

            if (QuirkLookupTable.ContainsKey(equipmentData.Id))
            {
                mechQuirk = QuirkLookupTable[equipmentData.Id];
                return true;
            }

            foreach (Regex regex in QuirkGearPatterns)
            {
                if (regex.IsMatch(equipmentData.Id))
                {
                    QuirkDef tempQuirkResult = GetQuirkFromGearJson(equipmentData.GearJsonDoc);
                    mechQuirk = tempQuirkResult;
                    QuirkLookupTable.Add(tempQuirkResult.Id, tempQuirkResult);
                    return true;
                }
            }

            if (GearTextQuirkCheckRegex.IsMatch(equipmentData.GearJsonDoc.RootElement.ToString()))
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
                foreach (KeyValuePair<string, QuirkDef> quirk in mech.MechQuirks)
                {
                    quirks[quirk.Key] = quirk.Value;
                }
            }

            return quirks.Values.ToList();
        }

        public static void OutputQuirkToString(QuirkDef quirkDef, StringWriter stringWriter, bool forceFullDesc = false, bool newLineBetweenBonuses = false)
        {
            if (CommonQuirks.Contains(quirkDef.Id) && !forceFullDesc)
            {
                stringWriter.WriteLine($"'''[[Full_List_of_Mechs#Quirks|Mech Quirk:'''  {quirkDef.UiName}]]");
                stringWriter.WriteLine();
                stringWriter.WriteLine();
                return;
            }
            if (quirkDef.Id.Contains("Avionics") && !forceFullDesc)
            {
                stringWriter.WriteLine($"'''[[Full_List_of_Mechs#Quirks|Mech Quirk:'''  {quirkDef.UiName}]]");
                stringWriter.WriteLine();
                foreach (BonusDef bonus in quirkDef.QuirkBonuses)
                {
                    if (bonus.BonusId == "LAMStabTaken")
                    {
                        stringWriter.WriteLine(string.Format(bonus.LongDescription, bonus.BonusValues));
                        stringWriter.WriteLine();
                        return;
                    }
                }
            }

            stringWriter.WriteLine($"'''[[Mech_Quirks|Mech Quirk:]]'''  {quirkDef.UiName}");
            stringWriter.WriteLine();

            bool first = true;
            bool prevEndedInPunctuation = false;
            foreach (BonusDef bonus in quirkDef.QuirkBonuses)
            {
                if (BonusOutputBlacklist.Contains(bonus.BonusId))
                    continue;
                List<string> tempBonusValues = bonus.BonusValues;
                if (bonus.StackingLimit != 1 && bonus.BonusValues.Count > 0 && quirkDef.InstanceCount > 1)
                {
                    tempBonusValues = bonus.BonusValues.Select((val) =>
                    {
                        if (BonusNumberExtractor.IsMatch(val))
                        {
                            GroupCollection possiblyPercentage = BonusNumberExtractor.Match(val).Groups;
                            // Check that the stacking limit is -1 (unlimited) or that the Instance count is less than the stacking limit. If so then multiply the bonus.
                            double modVal = Convert.ToDouble(possiblyPercentage[1].Value) * ((quirkDef.InstanceCount <= bonus.StackingLimit || bonus.StackingLimit == -1) ? quirkDef.InstanceCount : bonus.StackingLimit);
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
                            Console.WriteLine($"DISCARDED FULL DESCRIPTION: {bonus.BonusId}");
                            Console.WriteLine($"Full: {string.Format(bonus.FullDescription, tempBonusValues.ToArray())}");
                            Console.WriteLine($"Long: {string.Format(bonus.LongDescription, tempBonusValues.ToArray())}");
                            Console.WriteLine("");
                            DebugBlacklist.Add(bonus.BonusId);
                        }
                    }
                }
                if (!newLineBetweenBonuses)
                {
                    if (first) first = false;
                    else
                    {
                        if (!prevEndedInPunctuation)
                            stringWriter.Write(", ");
                        stringWriter.Write(" ");
                    }
                }

                string tempBonusWrite = string.Format(fullDescUsable ? bonus.FullDescription ?? bonus.LongDescription : bonus.LongDescription, tempBonusValues.ToArray()).Trim();
                stringWriter.Write(tempBonusWrite);

                if (new List<char> { ',', '.', '!', '?' }.Contains(tempBonusWrite.Last()))
                {
                    prevEndedInPunctuation = true;
                    if(newLineBetweenBonuses)
                        stringWriter.WriteLine("<br/>");
                }
                else if(newLineBetweenBonuses)
                    stringWriter.WriteLine(".<br/>");
            }

            stringWriter.WriteLine();
            stringWriter.WriteLine();
        }

        private static QuirkDef GetQuirkFromGearJson(JsonDocument gearJson)
        {
            QuirkDef output = new QuirkDef()
            {
                Id = gearJson.RootElement.GetProperty("Description").GetProperty("Id").ToString(),
                Name = gearJson.RootElement.GetProperty("Description").GetProperty("Name").ToString(),
                UiName = gearJson.RootElement.GetProperty("Description").GetProperty("UIName").ToString(),
                QuirkBonuses = new List<BonusDef>(),
                InstanceCount = 1
            };

            JsonElement bonuses;
            if (gearJson.RootElement.GetProperty("Custom").TryGetProperty("BonusDescriptions", out JsonElement bonusDescs))
            {
                foreach (JsonElement bonusElement in bonusDescs.EnumerateArray())
                {
                    string[] bonus = bonusElement.ToString().Split(':');

                    BonusDef tempBonusDef = BaseBonusLookupTable[bonus[0]];

                    if (bonus.Length > 1)
                        tempBonusDef.BonusValues = bonus[1].Split(",").Select((val) => val.Trim()).ToList();
                    else
                        tempBonusDef.BonusValues = new List<string> { "" };

                    output.QuirkBonuses.Add(tempBonusDef);
                }
            }
            else
            {
                if (output.Id == "Gear_Cockpit_Tacticon_B2000_Battle_Computer")
                {
                    BonusDef tempBonusDef = BaseBonusLookupTable["Tacticon"];
                    tempBonusDef.BonusValues = new List<string> { "+1" };
                    output.QuirkBonuses.Add(tempBonusDef);
                }
                else if (output.Id == "Gear_Sensor_Prototype_EWE")
                {
                    BonusDef tempBonusDef = BaseBonusLookupTable["MissileDefense"];
                    tempBonusDef.BonusValues = new List<string> { "+3" };
                    output.QuirkBonuses.Add(tempBonusDef);
                    tempBonusDef = BaseBonusLookupTable["Visibility"];
                    tempBonusDef.BonusValues = new List<string> { "-15%" };
                    output.QuirkBonuses.Add(tempBonusDef);
                    tempBonusDef = BaseBonusLookupTable["Signature"];
                    tempBonusDef.BonusValues = new List<string> { "-15%" };
                    output.QuirkBonuses.Add(tempBonusDef);
                    tempBonusDef = BaseBonusLookupTable["ProbeBubble"];
                    tempBonusDef.BonusValues = new List<string> { "250" };
                    output.QuirkBonuses.Add(tempBonusDef);
                }
            }

            return output;
        }

        private static bool CheckIfQuirkBonusLimit(string bonusId)
        {
            return LimitedBonusStrings.Contains(bonusId);
        }
    }
}
