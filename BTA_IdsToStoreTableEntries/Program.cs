using BT_JsonProcessingLibrary;
using System.Text.Json;
using System.Text.RegularExpressions;

class Program
{
    static void Main(string[] args)
    {
        using StreamReader inputFile = new("ListOfIds.txt");
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
        Console.WriteLine("If blank defaults to: C:\\Program Files (x86)\\Steam\\steamapps\\common\\BATTLETECH\\Mods");
        Console.Write(":");
        string filePath = Console.ReadLine() ?? "";
        if (string.IsNullOrEmpty(filePath))
            filePath = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\BATTLETECH\\Mods";
        Console.WriteLine("");

        if (useFile)
        {
            List<ItemInfo> itemData = new List<ItemInfo>();
            foreach(string itemId in inputFileValues) {

                itemData.Add(GetItemInfo(filePath, itemId));
            }
            CreateStoreTableEntry(itemData);
        }
        else
        {
            //while (true)
            //{
            //    CreateStoreTableEntry(filePath);

            //    Console.WriteLine("Done? (y/n)...");

            //    string? done = Console.ReadLine();
            //    if (!string.IsNullOrEmpty(done) && done.ToLower() == "y")
            //        break;
            //}
            Console.WriteLine("INPUT FILE 'S BLANK CAPN'!!!");
        }
    }

    public static ItemInfo GetItemInfo(string filePath, string itemId)
    {
        List<BasicFileData> files = ModJsonHandler.SearchFiles(itemId + ".json", filePath);

        Console.WriteLine("");
        Console.WriteLine("Found the following files for " + itemId + ": ");
        foreach (BasicFileData file in files)
        {
            Console.WriteLine(file.FileName);
        }
        Console.WriteLine("");

        ItemInfo item = new ItemInfo();
        BasicFileData luckyFile;

        if (files.Count > 0)
            luckyFile = files[0];
        else
            return item;

        item.Id = itemId;

        JsonElement description;
        if (JsonDocument.Parse(File.ReadAllText(luckyFile.Path + @"\" + luckyFile.FileName)).RootElement.TryGetProperty("Description", out description))
        {
            JsonElement tempName;
            JsonElement tempUiName;
            JsonElement tempModel;

            if (description.TryGetProperty("Name", out tempName))
                item.Name = tempName.GetString();
            else
                item.Name = "ERROR NO NAME FOUND";

            if (description.TryGetProperty("UIName", out tempUiName))
                item.UIName = tempUiName.GetString();
            else
                item.UIName = "ERROR NO UI NAME FOUND";

            if (description.TryGetProperty("Model", out tempModel))
                item.Model = tempModel.GetString();
            else
                item.Model = "ERROR NO MODEL FOUND";
        }

        return item;
    }

    public static void CreateStoreTableEntry(List<ItemInfo> items)
    {
        List<string> mechEntries = new List<string>();
        List<string> vehicleEntries = new List<string>();
        List<string> battleArmorEntries = new List<string>();
        List<string> gearEntries = new List<string>();
        List<string> weaponEntries = new List<string>();
        List<string> ammoEntries = new List<string>();
        List<string> contractEntries = new List<string>();

        foreach(ItemInfo item in items)
        {
            string[] parts = item.Id.Split('_');

            switch (parts[0].ToLower())
            {
                case "weapon":
                    weaponEntries.Add(ProcessWeaponEntry(item));
                    break;
                case "ammo":
                    ammoEntries.Add(ProcessAmmoEntry(item));
                    break;
                case "vehicledef":
                    vehicleEntries.Add(ProcessVehicleEntry(item));
                    break;
                case "gear":
                    if (parts[1].ToLower() == "contract")
                    {
                        contractEntries.Add(ProcessContractEntry(item));
                    }
                    else
                    {
                        gearEntries.Add(ProcessGearEntry(item));
                    }
                    break;
                case "mechdef":
                    if (parts[1].ToLower() == "ba")
                    {
                        battleArmorEntries.Add(ProcessBattleArmorEntry(item));
                    }
                    else
                    {
                        mechEntries.Add(ProcessMechEntry(item));
                    }
                    break;
            }
        }


        using StreamWriter outputFile = new("StoreItemOutputInfo.txt", append: true);

        outputFile.WriteLine(OutputListToTableColumn(mechEntries));
        outputFile.WriteLine(OutputListToTableColumn(vehicleEntries));
        outputFile.WriteLine(OutputListToTableColumn(battleArmorEntries));
        outputFile.WriteLine(OutputListToTableColumn(gearEntries));
        outputFile.WriteLine(OutputListToTableColumn(weaponEntries));
        outputFile.WriteLine(OutputListToTableColumn(ammoEntries));
        outputFile.WriteLine(OutputListToTableColumn(contractEntries));
    }

    private static string OutputListToTableColumn(List<string> list)
    {
        string output = "|";

        foreach (string item in list)
        {
            output += item + "<br>";
        }

        if(list.Count == 0)
        {
            output += "None";
        }

        return output;
    }

    private static string ProcessMechEntry(ItemInfo item)
    {
        string[] idSplit = item.Id.Split('_');
        
        string designation = idSplit[idSplit.Length - 1];

        return "[[" + item.Name.Replace(" ", "_") + "#" + designation + "|" + item.Name + "]]";
    }

    private static string ProcessBattleArmorEntry(ItemInfo item)
    {
        return "[[" + item.Name.Replace(" ", "_") + "|" + item.Name + "]]";
    }

    private static string ProcessGearEntry(ItemInfo item)
    {
        return "[[REPLACE ME#" + item.Name.Replace(" ", "_") + "|" + item.Name + "]]";
    }

    private static string ProcessContractEntry(ItemInfo item)
    {
        return "[[Contracts#" + item.Name.Replace(" ", "_") + "|" + item.Name + "]]";
    }

    private static string ProcessVehicleEntry(ItemInfo item)
    {
        return "[[" + item.Name.Replace(" ", "_") + "#" + item.Name + "|" + item.Name + "]]CHECK ME";
    }

    private static string ProcessAmmoEntry(ItemInfo item)
    {
        return "[[Ammunition#" + item.Model.Replace("/", ".2F") + "|" + item.Name + "]]";
    }

    private static string ProcessWeaponEntry(ItemInfo item)
    {
        return "[[" + "Weapons#REPLACE ME|" + item.Name + "]]";
    }

    static string TranslateIdToLink(string itemId)
    {
        string[] parts = itemId.Split('_');

        switch (parts[0].ToLower())
        {
            case "weapon":
                return "Weapons#";
            case "ammo":
                return "Ammunition#";
            case "vehicledef":
                return "vehicle";
            case "gear":
                if (parts[1].ToLower() == "contract")
                {
                    return "Contracts#";
                }
                else
                {
                    return "gear";
                }
            case "mechdef":
                if (parts[1].ToLower() == "ba")
                {
                    return "ba";
                }
                else
                {
                    return "mech";
                }
        }
        return "ERROR";
    }

    public struct ItemInfo
    {
        public string Id;
        public string UIName;
        public string Name;
        public string Model;
    }
}