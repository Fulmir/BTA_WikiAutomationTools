using System.Text.Json;
using System.Text.RegularExpressions;

namespace BT_JsonProcessingLibrary
{
    public static class ModJsonHandler
    {
        public static List<BasicFileData> SearchFiles(string startingPath, string filePattern)
        {
            List<BasicFileData> values = new List<BasicFileData>();

            string? dirName = Path.GetDirectoryName(startingPath);
            string? fileName = Path.GetFileName(startingPath);

            var files = from file in Directory.EnumerateFiles(
                            string.IsNullOrWhiteSpace(dirName) ? "." : dirName,
                            filePattern,
                            SearchOption.AllDirectories)
                        select file;

            foreach (var file in files)
            {
                if(!file.Contains(".modtek"))
                    values.Add(new BasicFileData { Path = Path.GetFullPath(file), FileName = Path.GetFileName(file) });
            }

            return values;
        }

        public static JsonDocument GetJsonDocument(string filePath)
        {
            StreamReader reader = new StreamReader(filePath);
            return JsonDocument.Parse(reader.ReadToEnd());
        }

        public static string GetNameFromJsonDoc(JsonDocument jsonFile)
        {
            if (jsonFile.RootElement.TryGetProperty("Description", out JsonElement description))
            {
                if (description.TryGetProperty("Name", out JsonElement name))
                {
                    return name.ToString().Trim();
                }
            }
            return "ERROR GETTING NAME FROM JSON";
        }

        public static string GetUiNameFromJsonDoc(JsonDocument jsonFile)
        {
            if (jsonFile.RootElement.TryGetProperty("Description", out JsonElement description))
            {
                if (description.TryGetProperty("UIName", out JsonElement uiName))
                {
                    return uiName.ToString().Trim();
                }
                else if(description.TryGetProperty("Name", out JsonElement name))
                {
                    return name.ToString().Trim();
                }
            }
            return "ERROR GETTING UINAME FROM JSON";
        }

        public static string GetIdFromJsonDoc(JsonDocument jsonFile)
        {
            if (jsonFile.RootElement.TryGetProperty("Description", out JsonElement description))
            {
                if (description.TryGetProperty("Id", out JsonElement name))
                {
                    return name.ToString().Trim();
                }
            }
            return "ERROR GETTING ID FROM JSON";
        }

        public static string GetDescriptionDetailsFromJsonDoc(JsonDocument jsonFile)
        {
            if (jsonFile.RootElement.TryGetProperty("Description", out JsonElement description))
            {
                if (description.TryGetProperty("Details", out JsonElement name))
                {
                    return ConvertColorFormatToWiki(name.ToString().Trim());
                }
            }
            return "ERROR GETTING DETAILS FROM JSON";
        }

        public static BasicFileData GetMechDef(BasicFileData chassisDef)
        {
            string baseSubDirectory = chassisDef.Path.Remove(chassisDef.Path.Length - chassisDef.FileName.Length - 7 - 1);
            string mechFileName = chassisDef.FileName.Replace("chassisdef_", "mechdef_");

            return new BasicFileData() { Path = baseSubDirectory + "mech\\" + mechFileName, FileName = mechFileName };
        }

        public static BasicFileData GetChassisDef(JsonDocument mechDefJson, BasicFileData mechDef)
        {
            string mechFileName = mechDef.FileName.Replace("mechdef_", "chassisdef_");
            if (mechDefJson.RootElement.TryGetProperty("ChassisID", out JsonElement chassisId))
            {
                mechFileName = chassisId.ToString().Trim() + ".json";
            }

            string baseSubDirectory = mechDef.Path.Remove(mechDef.Path.Length - mechDef.FileName.Length - 4 - 1);

            return new BasicFileData() { Path = baseSubDirectory + "chassis\\" + mechFileName, FileName = mechFileName };
        }

        public static string GetChassisDefId(JsonDocument mechDefJson, BasicFileData mechDef)
        {
            if (mechDefJson.RootElement.TryGetProperty("ChassisID", out JsonElement chassisId))
            {
                return chassisId.ToString().Trim();
            }

            return mechDef.FileName.Replace("mechdef_", "chassisdef_").Replace(".json", "");
        }

        public static BasicFileData GetVehicleChassisDef(BasicFileData vehicleDef)
        {
            string baseSubDirectory = vehicleDef.Path.Remove(vehicleDef.Path.Length - vehicleDef.FileName.Length-1);
            string vehicleFileName = vehicleDef.FileName.Replace("vehicledef_", "vehiclechassisdef_");

            return new BasicFileData() { Path = baseSubDirectory + "chassis\\" + vehicleFileName, FileName = vehicleFileName };
        }

        public static List<string> GetAllCategoryIds(JsonDocument jsonDoc)
        {
            if(jsonDoc.RootElement.TryGetProperty("Custom", out JsonElement custom))
            {
                if(custom.TryGetProperty("Category", out JsonElement category))
                {
                    if(category.ValueKind == JsonValueKind.Object)
                    {
                        return new List<string> { category.GetProperty("CategoryID").ToString() };
                    }
                    else if (category.ValueKind == JsonValueKind.Array)
                    {
                        List<string> outputList = new List<string>();
                        foreach(JsonElement categoryId in category.EnumerateArray())
                        {
                            outputList.Add(categoryId.GetProperty("CategoryID").ToString());
                        }
                        return outputList;
                    }
                }
            }

            return new List<string>();
        }

        private static Regex ColorCodeRegex = new Regex("(\\<color=)(?<colorCode>[0-9\\#A-F]{6,7})(>)", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

        public static string ConvertColorFormatToWiki(string toConvert)
        {
            string outputString = ColorCodeRegex.Replace(toConvert, "<span style=\"color:${colorCode};\">");
            return outputString.Replace("</color>", "</span>");
        }

        public static bool TryGetAuraEffectsForGear(JsonDocument gearJsonDoc, out List<StatusEffect> statusEffectList)
        {
            if(gearJsonDoc.RootElement.TryGetProperty("Auras", out JsonElement auras))
            {
                if(auras.TryGetProperty("statusEffects", out JsonElement auraStatusEffects))
                {
                    statusEffectList = auraStatusEffects.Deserialize<StatusEffect[]>().ToList();

                    if (statusEffectList.Count > 0)
                        return true;
                }
            }

            statusEffectList = new List<StatusEffect>();

            return false;
        }

        public static bool TryGetActivatableEffectsForGear(JsonDocument gearJsonDoc, out List<StatusEffect> statusEffectList)
        {
            if (gearJsonDoc.RootElement.TryGetProperty("Custom", out JsonElement auras))
            {
                if (auras.TryGetProperty("ActivatableComponent", out JsonElement activatableComponent))
                {
                    if (activatableComponent.TryGetProperty("statusEffects", out JsonElement activatableStatusEffects))
                    {
                        statusEffectList = activatableStatusEffects.Deserialize<StatusEffect[]>().ToList();

                        if (statusEffectList.Count > 0)
                            return true;
                    }
                }
            }

            statusEffectList = new List<StatusEffect>();

            return false;
        }

        public static bool TryGetBaseEffectsForGear(JsonDocument gearJsonDoc, out List<StatusEffect> statusEffectList)
        {
            if (gearJsonDoc.RootElement.TryGetProperty("statusEffects", out JsonElement gearStatusEffects))
            {
                if (gearStatusEffects.ToString() != "")
                    statusEffectList = gearStatusEffects.Deserialize<StatusEffect[]>().ToList();
                else
                {
                    statusEffectList = new List<StatusEffect>();
                    return false;
                }

                if (statusEffectList.Count > 0)
                    return true;
            }

            statusEffectList = new List<StatusEffect>();

            return false;
        }
    }
}