using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BT_JsonProcessingLibrary
{
    public static class PlanetDataHandler
    {
        private static string worldDefsFolder = "InnerSphereMap\\StarSystems\\";
        private static string communityContentWorldsDefFolder = "Community Content\\planet\\";

        private static ConcurrentDictionary<string, PlanetData> planetDefs = new ConcurrentDictionary<string, PlanetData>();

        public static void PopulatePlanetFileData(string modsFolder)
        {
            List<BasicFileData> factionDefFiles = ModJsonHandler.SearchFiles(modsFolder + worldDefsFolder, @"starsystemdef_*.json");
            factionDefFiles.AddRange(ModJsonHandler.SearchFiles(modsFolder + communityContentWorldsDefFolder, @"starsystemdef_*.json"));

            ParallelOptions parallelOptions = new ParallelOptions();
            parallelOptions.MaxDegreeOfParallelism = 8;

            //foreach (BasicFileData planetFile in factionDefFiles)
            Parallel.ForEach(factionDefFiles, parallelOptions, planetFile =>
            {
                planetDefs.TryAdd(planetFile.FileName.Substring(0, planetFile.FileName.Length - 5), new PlanetData(JsonDocument.Parse(File.ReadAllText(planetFile.Path))));
            });
        }

        public static bool TryGetPlanetDataForId(string planetId, out PlanetData planetData)
        {
            return planetDefs.TryGetValue(planetId, out planetData);
        }

        public static bool TryGetPlanetsWithTag(string planetTag, out List<PlanetData> planetData)
        {
            planetData = new List<PlanetData>();
            foreach (PlanetData planet in planetDefs.Values)
            {
                if(planet.Tags.Contains(planetTag))
                    planetData.Add(planet);
            }

            return planetData.Count > 0;
        }
    }
}
