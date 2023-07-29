using BT_JsonProcessingLibrary;
using System.Diagnostics;
using System.Text.RegularExpressions;
using UtilityClassLibrary.WikiLinkOverrides;

namespace BTA_WikiGeneration
{
    class Program
    {
        static void Main(string[] args)
        {
            string modsFolder = "";

            for (int index = 0; index < args.Length; index++)
            {
                if (args[index] == "-f")
                    modsFolder = args[index + 1];
            }

            if (modsFolder.Length == 0)
            {
                Console.WriteLine("File path to top level folder to search? eg: C:/Games/...");
                Console.WriteLine("If blank defaults to: C:\\Program Files (x86)\\Steam\\steamapps\\common\\BATTLETECH\\Mods\\");
                Console.Write(":");
                modsFolder = Console.ReadLine() ?? "";
                if (string.IsNullOrEmpty(modsFolder))
                    modsFolder = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\BATTLETECH\\Mods\\";
                Console.WriteLine("");
            }

            if (!modsFolder.EndsWith('\\'))
                modsFolder += "\\";


            QuirkHandler.LoadQuirkHandlerData(modsFolder);

            MoveSpeedHandler.InstantiateMoveSpeedHandler(modsFolder);

            AffinityHandler.CreateAffinitiesIndex(modsFolder);

            PlanetDataHandler.PopulatePlanetFileData(modsFolder);

            FactionDataHandler.PopulateFactionDefData(modsFolder);


            BonusTextHandler.CreateEquipmentBonusesIndex(modsFolder);

            AmmoBoxLinkOverrides.PopulateAmmoCategoryOverrides();

            MechGearHandler.InstantiateModsFolder(modsFolder);
            MechGearHandler.PopulateGearData();

            WeaponAttachmentProcessor.GetAllGearAttachments(modsFolder);

            WeaponAttachmentProcessor.PrintGearEntriesToFile();



            StoreFileProcessor.LoadStoreFileData(modsFolder);

            StoreFileProcessor.OutputFactoryStoresToString();

            StoreFileProcessor.OutputFactionStoresToString();



            MechFileSearch.GetAllMechsFromDefs(modsFolder);

            // Output faction names to file for Lua use
            FactionDataHandler.OutputIdsToNamesFile();

            FactionDataProcessor processor = new FactionDataProcessor(modsFolder);

            // Output SPAM factions to parent factions translation for Lua use
            processor.OutputSpamFactionsToParentsTranslation();

            // Output the Mercenary SPAM faction page data
            processor.OutputMercFactionInfo();

            // Output the Sub-Command factions to page data
            processor.OutputFactionSubCommands();

            // Output mechs to big table
            MechFileSearch.OutputMechsToWikiTables();

            VehicleFileSearch.GetAllVehiclesFromDefs(modsFolder);

            // Output vehicles to big table
            VehicleFileSearch.OutputVehiclesToWikiTables();

            // Output vehicles to individual pages
            VehicleFileSearch.PrintVehiclePagesToFiles();
        }
    }
}