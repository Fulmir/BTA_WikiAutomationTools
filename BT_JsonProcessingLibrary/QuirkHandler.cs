using BT_JsonProcessingLibrary;
using System;
using System.Collections.Concurrent;
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
    public static class QuirkHandler
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

        static List<string> BonusOutputBlacklist = new List<string>() { "GyroStab", "IsGyro", "FCS", };

        private static ConcurrentDictionary<string, QuirkDef> QuirkLookupTable = new ConcurrentDictionary<string, QuirkDef>();

        public static void LoadQuirkHandlerData(string modsFolder)
        {
            CommonQuirks = TextFileListProcessor.GetStringListFromFile(".\\DataListFiles\\CommonQuirksList.txt");
            CommonQuirks.Sort();
            HeadlineQuirks = TextFileListProcessor.GetStringListFromFile(".\\DataListFiles\\AdditionalHeadlineQuirksList.txt");
            HeadlineQuirks.AddRange(CommonQuirks);
            HeadlineQuirks.Sort();

            BonusTextHandler.CreateEquipmentBonusesIndex(modsFolder);
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
                    QuirkLookupTable.TryAdd(tempQuirkResult.Id, tempQuirkResult);
                    return true;
                }
            }

            if (GearTextQuirkCheckRegex.IsMatch(equipmentData.GearJsonDoc.RootElement.ToString()))
            {
                QuirkDef tempQuirkResult = GetQuirkFromGearJson(equipmentData.GearJsonDoc);
                mechQuirk = tempQuirkResult;
                QuirkLookupTable.TryAdd(tempQuirkResult.Id, tempQuirkResult);
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

                string tempBonusWrite = BonusTextHandler.PrintBonusToString(bonus, false, quirkDef.InstanceCount);
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
                Id = ModJsonHandler.GetIdFromJsonDoc(gearJson),
                Name = ModJsonHandler.GetNameFromJsonDoc(gearJson),
                UiName = ModJsonHandler.GetUiNameFromJsonDoc(gearJson),
                QuirkBonuses = new List<BonusDef>(),
                InstanceCount = 1
            };

            JsonElement bonuses;
            if (gearJson.RootElement.GetProperty("Custom").TryGetProperty("BonusDescriptions", out JsonElement bonusDescs))
            {
                foreach (JsonElement bonusElement in bonusDescs.EnumerateArray())
                {
                    string[] bonus = bonusElement.ToString().Split(':');

                    BonusDef tempBonusDef = BonusTextHandler.GetBaseBonusDef(bonus[0]);

                    tempBonusDef.PopulateBonusValues(bonus);

                    output.QuirkBonuses.Add(tempBonusDef);
                }
            }
            else
            {
                if (output.Id == "Gear_Cockpit_Tacticon_B2000_Battle_Computer")
                {
                    BonusDef tempBonusDef = BonusTextHandler.GetBaseBonusDef("Tacticon");
                    tempBonusDef.BonusValues = new List<string> { "+1" };
                    output.QuirkBonuses.Add(tempBonusDef);
                }
                else if (output.Id == "Gear_Sensor_Prototype_EWE")
                {
                    BonusDef tempBonusDef = BonusTextHandler.GetBaseBonusDef("MissileDefense");
                    tempBonusDef.BonusValues = new List<string> { "+3" };
                    output.QuirkBonuses.Add(tempBonusDef);
                    tempBonusDef = BonusTextHandler.GetBaseBonusDef("Visibility");
                    tempBonusDef.BonusValues = new List<string> { "-15%" };
                    output.QuirkBonuses.Add(tempBonusDef);
                    tempBonusDef = BonusTextHandler.GetBaseBonusDef("Signature");
                    tempBonusDef.BonusValues = new List<string> { "-15%" };
                    output.QuirkBonuses.Add(tempBonusDef);
                    tempBonusDef = BonusTextHandler.GetBaseBonusDef("ProbeBubble");
                    tempBonusDef.BonusValues = new List<string> { "250" };
                    output.QuirkBonuses.Add(tempBonusDef);
                }
            }

            return output;
        }
    }
}
