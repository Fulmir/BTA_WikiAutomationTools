using BT_JsonProcessingLibrary;
using BTA_WikiTableGen;
using UtilityClassLibrary.WikiLinkOverrides;

namespace BTA_ItemPageGenerator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("File path to top level folder to search? eg: C:/Games/...");
            Console.WriteLine("If blank defaults to: C:\\Program Files (x86)\\Steam\\steamapps\\common\\BATTLETECH\\Mods\\");
            Console.Write(":");
            string modsFolder = Console.ReadLine() ?? "";
            if (string.IsNullOrEmpty(modsFolder))
                modsFolder = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\BATTLETECH\\Mods\\";
            Console.WriteLine("");

            BonusTextHandler.CreateEquipmentBonusesIndex(modsFolder);

            AmmoBoxLinkOverrides.PopulateAmmoCategoryOverrides();

            MechGearHandler.InstantiateModsFolder(modsFolder);
            MechGearHandler.PopulateGearData();

            WeaponAttachmentProcessor.GetAllGearAttachments(modsFolder);

            WeaponAttachmentProcessor.PrintGearEntriesToFile();
        }
    }
}