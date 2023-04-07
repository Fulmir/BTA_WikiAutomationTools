using BT_JsonProcessingLibrary;
using BTA_WikiTableGen;
using System.Text.RegularExpressions;

class Program
{
    static void Main(string[] args)
    {
        using StreamReader inputFile = new("MechInputFile.txt");
        List<string> inputFileValues = new List<string>();
        bool useFile = false;

        if(inputFile.Peek() != -1)
        {
            useFile= true;

            while(!inputFile.EndOfStream)
            {
                inputFileValues.Add(inputFile.ReadLine()??"ERROR");
            }
        }

        Console.WriteLine("File path to top level folder to search? eg: C:/Games/...");
        Console.WriteLine("If blank defaults to: C:\\Program Files (x86)\\Steam\\steamapps\\common\\BATTLETECH\\Mods\\");
        Console.Write(":");
        string modsFolder = Console.ReadLine() ?? "";
        if (string.IsNullOrEmpty(modsFolder))
            modsFolder = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\BATTLETECH\\Mods\\";
        Console.WriteLine("");

        MoveSpeedHandler.InstantiateMoveSpeedHandler(modsFolder);

        List<MechStats> listOfMechs = new List<MechStats>();

        if(useFile)
        {
            foreach(string line in inputFileValues)
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
            MechFileSearch.GetAllMechsFromDefs(modsFolder);

            Console.WriteLine("Populated all mech defs!");
        }
    }

    public static void CreateMechEntry(string mechModel, List<string> fileInputs = null)
    {
        if (fileInputs == null)
        {
            Console.WriteLine("Model of mech? eg: \"AS7-A\"");
            Console.Write(":");
            mechModel = Console.ReadLine() ?? "";
            Console.WriteLine("");
        }
        else
            mechModel = fileInputs[0];

        using StreamWriter outputFile = new("MechTableOutput.txt", append: true);
    }

}