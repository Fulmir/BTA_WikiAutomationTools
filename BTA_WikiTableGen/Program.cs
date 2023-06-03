using BT_JsonProcessingLibrary;
using System.Text.RegularExpressions;

class Program
{
    static void Main(string[] args)
    {
        using StreamReader inputFile = new("MechInputFile.txt");
        List<string> inputFileValues = new List<string>();
        bool useFile = false;

        if (inputFile.Peek() != -1)
        {
            useFile = true;

            while (!inputFile.EndOfStream)
            {
                inputFileValues.Add(inputFile.ReadLine() ?? "ERROR");
            }
        }

        Console.WriteLine("File path to top level folder to search? eg: C:/Games/...");
        Console.WriteLine("If blank defaults to: C:\\Program Files (x86)\\Steam\\steamapps\\common\\BATTLETECH\\Mods\\");
        Console.Write(":");
        string modsFolder = Console.ReadLine() ?? "";
        if (string.IsNullOrEmpty(modsFolder))
            modsFolder = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\BATTLETECH\\Mods\\";
        Console.WriteLine("");

        if(!modsFolder.EndsWith('\\'))
            modsFolder += "\\";

        QuirkHandler.LoadQuirkHandlerData(modsFolder);

        MoveSpeedHandler.InstantiateMoveSpeedHandler(modsFolder);

        AffinityHandler.CreateAffinitiesIndex(modsFolder);

        FactionDataHandler.PopulateFactionDefData(modsFolder);

        List<MechStats> listOfMechs = new List<MechStats>();

        if (useFile)
        {
            foreach (string line in inputFileValues)
            {
                listOfMechs.Add(new MechStats(line, modsFolder));
            }

            using (StreamWriter outputFile = new("MechTableOutput.txt", append: true))
            {
                foreach (MechStats mech in listOfMechs)
                {
                    mech.OutputStatsToFile(outputFile);
                }
            }
        }
        else
        {
            MechGearHandler.InstantiateModsFolder(modsFolder);

            MechFileSearch.GetAllMechsFromDefs(modsFolder);

            MechFileSearch.OutputMechsToWikiTables();

            VehicleFileSearch.GetAllVehiclesFromDefs(modsFolder);

            VehicleFileSearch.OutputVehiclesToWikiTables();

            VehicleFileSearch.PrintVehiclePagesToFiles();
        }
    }
}