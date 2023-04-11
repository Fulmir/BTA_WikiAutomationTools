using BT_JsonProcessingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BTA_WikiTableGen
{
    internal static class AffinityHandler
    {
        static string AffinitiesFolder = "MechAffinity\\AffinityDefs\\";
        static string ChassisAffinitiesPattern = "AffinityDef_chassis*.json";

        internal static Dictionary<string, AffinityDef> AffinityLookupTable = new Dictionary<string, AffinityDef>();

        internal static List<AffinityDef> AllAffinities = new List<AffinityDef>();

        // Affinities
        // Match to PrefabID or PrefabIdentifier. ID overrides Identifier/is checked first.
        public static void CreateAffinitiesIndex(string modsFolder)
        {
            string AffinitiesPath = modsFolder + AffinitiesFolder;
            List<BasicFileData> AffinityFiles = ModJsonHandler.SearchFiles(AffinitiesPath, ChassisAffinitiesPattern);

            foreach(BasicFileData affinity in AffinityFiles)
            {
                JsonDocument affinityJson = JsonDocument.Parse(new StreamReader(affinity.Path).ReadToEnd());
                JsonElement affinityData = affinityJson.RootElement.GetProperty("affinityData");

                AffinityDef affinityDef = new AffinityDef()
                {
                    Id = affinityJson.RootElement.GetProperty("id").ToString(),
                    UIName = affinityData.GetProperty("affinityLevels")[0].GetProperty("levelName").ToString(),
                    Description = affinityData.GetProperty("affinityLevels")[0].GetProperty("decription").ToString(),
                };

                AllAffinities.Add(affinityDef);

                foreach (JsonElement chassisName in affinityData.GetProperty("chassisNames").EnumerateArray())
                {
                    if (!AffinityLookupTable.ContainsKey(chassisName.ToString()))
                    {
                        AffinityLookupTable.Add(chassisName.ToString(), affinityDef);
                    }
                    else
                        Console.WriteLine("ERROR: Duplicate Affinity Found for Chassis!");
                }
            }
        }

        public static bool TryGetAffinityForMech(string? prefabId, string? prefabIdentifier, int mechTonnage, out AffinityDef mechAffinity)
        {
            mechAffinity = new AffinityDef();

            if(prefabId != null && AffinityLookupTable.ContainsKey($"{prefabId}_{mechTonnage}"))
            {
                mechAffinity = AffinityLookupTable[$"{prefabId}_{mechTonnage}"];
                return true;
            }
            else if (prefabIdentifier != null && AffinityLookupTable.ContainsKey($"{prefabIdentifier}_{mechTonnage}"))
            {
                mechAffinity = AffinityLookupTable[$"{prefabIdentifier}_{mechTonnage}"];
                return true;
            }

            return false;
        }

        public static bool TryGetAssemblyVariant(MechStats stats, out AssemblyVariant outVariant)
        {
            if(stats.ChassisDefFile.RootElement.TryGetProperty("Custom", out JsonElement custom))
            {
                if(custom.TryGetProperty("AssemblyVariant", out JsonElement variant))
                {
                    outVariant = new AssemblyVariant()
                    {
                        PrefabId = variant.GetProperty("PrefabID").ToString(),
                        Include = variant.GetProperty("Include").GetBoolean(),
                        Exclude = variant.GetProperty("Exclude").GetBoolean(),
                    };
                    return true;
                }
            }

            outVariant = new AssemblyVariant();
            return false;
        }

        public static List<AffinityDef> CompileAffinitiesForVariants(List<MechStats> mechList)
        {
            Dictionary<string, AffinityDef> affinities = new Dictionary<string, AffinityDef>();

            foreach(MechStats mech in mechList)
            {
                if (mech.MechAffinity.HasValue)
                    affinities[mech.MechAffinity.Value.Id] = mech.MechAffinity.Value;
                else
                    Console.WriteLine($"MECH {mech.MechModel} HAS NO AFFINITY!");
            }

            return affinities.Values.ToList();
        }

        public static void OutputAffinityToString(AffinityDef affinityDef, StringWriter stringWriter)
        {
            stringWriter.WriteLine($"'''[[Pilot_Affinities|Pilot Affinity: ]]''' {affinityDef.UIName}");
            stringWriter.WriteLine();
            stringWriter.WriteLine(affinityDef.Description);
            stringWriter.WriteLine();
            stringWriter.WriteLine();
        }
    }
}
