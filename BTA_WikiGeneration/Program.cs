using BT_JsonProcessingLibrary;
using System.Diagnostics;
using System.Text.RegularExpressions;
using UtilityClassLibrary;
using UtilityClassLibrary.WikiLinkOverrides;

namespace BTA_WikiGeneration
{
    class Program
    {
        static void Main(string[] args)
        {
            CancellationTokenSource tokenSource = new CancellationTokenSource();

            Task.Run(() => Logging.ProcessLogMessages(tokenSource.Token));

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

            Console.WriteLine("Loading Widely Used Mod Data...");
            QuirkHandler.LoadQuirkHandlerData(modsFolder);

            MoveSpeedHandler.InstantiateMoveSpeedHandler(modsFolder);

            AffinityHandler.CreateAffinitiesIndex(modsFolder);

            PlanetDataHandler.PopulatePlanetFileData(modsFolder);

            FactionDataHandler.PopulateFactionDefData(modsFolder);

            BonusTextHandler.CreateEquipmentBonusesIndex(modsFolder);

            AmmoBoxLinkOverrides.PopulateAmmoCategoryOverrides();

            MechGearHandler.InstantiateModsFolder(modsFolder);
            MechGearHandler.PopulateGearData();

            MechFileSearch.GetAllMechsFromDefs(modsFolder);

            WeaponAttachmentProcessor.GetAllGearAttachments(modsFolder);
            Console.WriteLine("FINISHED: Loading Widely Used Mod Data...");

            Console.WriteLine("Creating Gear pages...");
            WeaponAttachmentProcessor.PrintGearEntriesToFile();
            Console.WriteLine("FINISHED: Creating Gear pages...");

            Console.WriteLine("Creating Store data pages...");
            StoreFileProcessor.LoadStoreFileData(modsFolder);

            StoreFileProcessor.OutputFactoryStoresToString();
            Console.WriteLine("FINISHED: Creating Store data pages...");

            Console.WriteLine("Creating faction data pages...");
            // Output faction names to file for Lua use
            FactionDataHandler.OutputIdsToNamesFile();

            FactionDataProcessor processor = new FactionDataProcessor(modsFolder);

            // Output SPAM factions to parent factions translation for Lua use
            processor.OutputSpamFactionsToParentsTranslation();

            // Output the Mercenary SPAM faction page data
            processor.OutputMercFactionInfo();

            // Output the Sub-Command factions to page data
            processor.OutputFactionSubCommands();
            Console.WriteLine("FINISHED: Creating faction data pages...");

            Console.WriteLine("Creating Mechs Table Page...");
            // Output mechs to big table
            MechFileSearch.OutputMechsToWikiTables();
            Console.WriteLine("FINISHED: Creating Mechs Table Page...");

            Console.WriteLine("Creating Vehicles Table Page...");
            VehicleFileSearch.GetAllVehiclesFromDefs(modsFolder);

            // Output vehicles to big table
            VehicleFileSearch.OutputVehiclesToWikiTables();
            Console.WriteLine("FINISHED: Creating Vehicles Table Page...");

            Console.WriteLine("Creating Individual Vehicle Pages...");
            // Output vehicles to individual pages
            VehicleFileSearch.PrintVehiclePagesToFiles();
            Console.WriteLine("FINISHED: Creating Individual Vehicle Pages...");

            tokenSource.Cancel();

            while (Logging.ActiveLogging)
                Task.Delay(1000);

            tokenSource.Dispose();
            Console.WriteLine("----- DONE -----");
        }
    }
}