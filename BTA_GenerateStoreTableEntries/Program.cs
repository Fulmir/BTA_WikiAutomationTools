using BT_JsonProcessingLibrary;
using BTA_IdsToStoreTableEntries;
using System.Text.Json;
using System.Text.RegularExpressions;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("File path to top level folder to search? eg: C:/Games/...");
        Console.WriteLine("If blank defaults to: C:\\Program Files (x86)\\Steam\\steamapps\\common\\BATTLETECH\\Mods\\");
        Console.Write(":");
        string filePath = Console.ReadLine() ?? "";
        if (string.IsNullOrEmpty(filePath))
            filePath = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\BATTLETECH\\Mods\\";
        Console.WriteLine("");

        StoreFileProcessor.LoadStoreFileData(filePath);

        StoreFileProcessor.OutputFactoryStoresToString();

        StoreFileProcessor.OutputFactionStoresToString();
    }
}