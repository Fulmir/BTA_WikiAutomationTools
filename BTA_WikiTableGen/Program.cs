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
        Console.WriteLine("If blank defaults to: C:\\Program Files (x86)\\Steam\\steamapps\\common\\BATTLETECH\\Mods");
        Console.Write(":");
        string filePath = Console.ReadLine() ?? "";
        if (string.IsNullOrEmpty(filePath))
            filePath = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\BATTLETECH\\Mods";
        Console.WriteLine("");

        int startIndex = 0;
        int mechEntries = 6;
        int mechCount = inputFileValues.Count / mechEntries;

        if(useFile)
        {
            for(int i = 0;i < mechCount; i++)
            {
                List<string> singleMech = inputFileValues.GetRange(i*mechEntries, mechEntries);

                CreateMechEntry(filePath, singleMech);
            }
        }
        else
        {
            while (true)
            {
                CreateMechEntry(filePath);

                Console.WriteLine("Done? (y/n)...");

                string? done = Console.ReadLine();
                if (!string.IsNullOrEmpty(done) && done.ToLower() == "y")
                    break;
            }
        }
    }

    public static void CreateMechEntry(string filePath, List<string> fileInputs = null)
    {
        string mechModel = "";
        string baseTons = "";
        string freeTons = "";
        string walk = "";
        string sprint = "";
        string jump = "";


        if (fileInputs == null)
        {
            Console.WriteLine("Model of mech? eg: \"AS7-A\"");
            Console.Write(":");
            mechModel = Console.ReadLine() ?? "";
            Console.WriteLine("");
        }
        else
            mechModel = fileInputs[0];

        List<FoundFile> files = SearchFiles(mechModel, filePath);

        Console.WriteLine("");
        Console.WriteLine("Found the following files for " + mechModel + ": ");
        foreach (FoundFile file in files)
        {
            Console.WriteLine(file.fileName);
        }
        Console.WriteLine("");

        var matches = from file in files
                      from line in File.ReadLines(file.path + @"\" + file.fileName).Zip(Enumerable.Range(1, int.MaxValue), (s, i) => new { Num = i, Text = s, File = file.fileName })
                      select line.Text;

        var listOfMatches = matches.ToList();

        string mechTonnage = runRegexOnLines(listOfMatches, @"(?<=""Tonnage"":\s*)(\d{1,3})", "ERROR NO TONNAGE FOUND");
        string role = runRegexOnLines(listOfMatches, @"(?<=""StockRole"":\s*"")([\w\s\-',.&]+)", "ERROR NO ROLE FOUND");

        string[] hardpoints = getMechHardpoints(listOfMatches);
        string ballisticHp = hardpoints[0];
        string energyHp = hardpoints[1];
        string missileHp = hardpoints[2];
        string supportHp = hardpoints[3];
        string omniHp = hardpoints[4];

        string mechEngineType = EngineDecode(runRegexOnLines(listOfMatches, @"(emod_engine(?!_cooling|_\d+|.*size)([a-zA-Z_]+))", "ERROR NO ENGINE TYPE FOUND"));
        string mechEngineSize = runRegexOnLines(listOfMatches, @"(?<=emod_engine_)(\d+)", "ERROR NO ENGINE SIZE FOUND");
        string mechHeatsinkType = HeatsinkDecode(runRegexOnLines(listOfMatches, @"(emod_kit\w*)", "ERROR NO STRUCTURE FOUND"));
        string mechStructure = StructureDecode(runRegexOnLines(listOfMatches, @"(\w*structureslots\w*)", "ERROR NO STRUCTURE FOUND"));
        string mechArmor = ArmorDecode(runRegexOnLines(listOfMatches, @"(emod_armorslots\w*|Gear_armorslots\w|Gear_Reflective_Coating)", "ERROR NO ARMOR FOUND"));

        if(fileInputs == null)
        {
            Console.WriteLine("Enter Base Tonnage, eg: 34.5");
            Console.Write(":");
            baseTons = $"{Console.ReadLine() ?? ""}t";
            Console.WriteLine("");

            Console.WriteLine("Enter Free Tonnage, eg: 52.5");
            Console.Write(":");
            freeTons = $"{Console.ReadLine() ?? ""}t";
            if (freeTons == "t" || freeTons == "N/At")
                freeTons = "N/A";
            Console.WriteLine("");


            Console.WriteLine("Enter Walk Hexes");
            Console.Write(":");
            walk = Console.ReadLine() ?? "";
            Console.WriteLine("");

            Console.WriteLine("Enter Sprint Hexes");
            Console.Write(":");
            sprint = Console.ReadLine() ?? "";
            Console.WriteLine("");

            Console.WriteLine("Enter Jump Hexes");
            Console.Write(":");
            jump = Console.ReadLine() ?? "";
            Console.WriteLine("");
        } else
        {
            baseTons = fileInputs[1];
            freeTons = fileInputs[2];
            walk = fileInputs[3];
            sprint = fileInputs[4];
            jump = fileInputs[5];
        }

        using StreamWriter outputFile = new("MechTableOutput.txt", append: true);

        outputFile.WriteLine(OutputLine(mechModel));
        outputFile.WriteLine(OutputLine(mechTonnage + "t"));
        outputFile.WriteLine(OutputLine(role));
        outputFile.WriteLine(OutputLine(ballisticHp));
        outputFile.WriteLine(OutputLine(energyHp));
        outputFile.WriteLine(OutputLine(missileHp));
        outputFile.WriteLine(OutputLine(supportHp));
        outputFile.WriteLine(OutputLine(omniHp));
        outputFile.WriteLine(OutputLine(mechEngineType));
        outputFile.WriteLine(OutputLine(mechEngineSize));
        outputFile.WriteLine(OutputLine(mechHeatsinkType));
        outputFile.WriteLine(OutputLine(mechStructure));
        outputFile.WriteLine(OutputLine(mechArmor));
        outputFile.WriteLine(OutputLine("None"));
        outputFile.WriteLine(OutputLine(baseTons + "t"));
        outputFile.WriteLine(OutputLine(freeTons + "t"));
        outputFile.WriteLine(OutputLine(walk));
        outputFile.WriteLine(OutputLine(sprint));
        outputFile.WriteLine(OutputLine(jump));
        outputFile.WriteLine(OutputLine("-"));
    }

    static string OutputLine(string line)
    {
        return "|" + line;
    }

    static string runRegexOnLines(List<string> lines, string pattern, string error)
    {
        Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);

        foreach (string line in lines)
        {
            if (regex.IsMatch(line))
                return regex.Match(line).Groups[0].Value;
        }
        return error;
    }

    static string[] getMechHardpoints(List<string> lines)
    {
        string hardpointPattern = @"(?<=""WeaponMount"":\s*"")(Energy|Ballistic|Missile|AntiPersonnel)";
        string omniPattern = @"(?<=""Omni"":\s*)(true|false)";
        //string omniWeaponMounts = @"(?:""WeaponMount"":\s*)(""Energy""|""Ballistic""|""Missile""|""AntiPersonnel"")(?:,\s*""Omni"":\s*)()";
        Regex hardpointRegex = new Regex(hardpointPattern, RegexOptions.IgnoreCase);
        Regex omniRegex = new Regex(omniPattern, RegexOptions.IgnoreCase);

        //List<Match> matches = new List<Match>();
        List<Hardpoint> hardpoints = new List<Hardpoint>();

        Hardpoint currentHardpoint = new Hardpoint();

        bool hardpointMatch = false;
        foreach (string line in lines)
        {
            if (hardpointMatch)
            {
                if (omniRegex.IsMatch(line))
                {
                    currentHardpoint.omni = omniRegex.Match(line).Groups[0].Value == "true";
                    hardpoints.Add(currentHardpoint);
                    //matches.Add(omniRegex.Match(line));
                    hardpointMatch = false;
                }
            }
            if (hardpointRegex.IsMatch(line))
            {
                currentHardpoint.type = hardpointRegex.Match(line).Groups[0].Value;
                //matches.Add(hardpointRegex.Match(line));
                hardpointMatch = true;
            }
        }

        int ballistic = 0;
        int energy = 0;
        int missile = 0;
        int support = 0;
        int omni = 0;

        foreach(Hardpoint hardpoint in hardpoints)
        {
            if (hardpoint.omni)
                omni++;
            else
                switch (hardpoint.type.ToLower())
                {
                    case "ballistic":
                        ballistic++;
                        break;
                    case "energy":
                        energy++;
                        break;
                    case "missile":
                        missile++;
                        break;
                    case "antipersonnel":
                        support++;
                        break;
                }
        }

        return new string[] { ballistic.ToString(), energy.ToString(), missile.ToString(), support.ToString(), omni.ToString() };
    }

    static List<FoundFile> SearchFiles(string mechName, string startingPath)
    {
        string mechFilesPattern = @"*_" + mechName + ".json";
        //string mechFilesPattern = @"(chassisdef|mechdef).*" + mechName + "\\.json";

        List<FoundFile> values = new List<FoundFile>();

        string? dirName = Path.GetDirectoryName(startingPath);
        string? fileName = Path.GetFileName(startingPath);

        var files = from file in Directory.EnumerateFiles(
                        string.IsNullOrWhiteSpace(dirName) ? "." : dirName,
                        mechFilesPattern,
                        SearchOption.AllDirectories)
                    select file;

        foreach ( var file in files )
        {
            values.Add(new FoundFile { path = Path.GetDirectoryName(file), fileName = Path.GetFileName(file) });
        }

        return values;
    }

    static string EngineDecode(string engineRegexOutput)
    {
        switch (engineRegexOutput)
        {
            case "emod_engineslots_std_center":
                return "STD";
            case "emod_engineslots_light_center":
                return "LFE";
            case "emod_engineslots_xl_center":
                return "XL";
            case "emod_engineslots_cxl_center":
                return "cXL";
            case "emod_engineslots_xxl_center":
                return "XXL";
            case "emod_engineslots_cxxl_center":
                return "cXXL";
            case "emod_engineslots_Primitive_center":
                return "PFE";
            case "emod_engineslots_sxl_center":
                return "sXL";
            case "emod_engineslots_dense_center":
                return "DFE";
            case "emod_Engine_LAM":
                return "LAM";
            case "emod_engineslots_compact_center":
                return "CFE";
            case "emod_engineslots_FuelCell":
                return "FCE";
            case "emod_engineslots_ICE_center":
                return "ICE";
            case "emod_engineslots_juryrigxl_center":
                return "JRXL";
            case "emod_engineslots_3g_center":
                return "3GE";
        }

        return engineRegexOutput + " TYPE NOT FOUND";
    }

    static string HeatsinkDecode(string heatsinksRegexOutput)
    {
        switch (heatsinksRegexOutput)
        {
            case "emod_kit_shs":
                return "SHS";
            case "emod_kit_dhs":
                return "DHS";
            case "emod_kit_cdhs":
                return "cDHS";
            case "emod_kit_dhs_proto":
                return "pDHS";
            case "emod_kit_sdhs":
                return "sDHS";
            case "emod_kit_lhs":
                return "LDHS";
            case "emod_kit_3ghs":
                return "3GHS";
        }

        return heatsinksRegexOutput + " TYPE NOT FOUND";
    }

    static string StructureDecode(string structureRegexOutput)
    {
        switch (structureRegexOutput)
        {
            case "emod_structureslots_standard":
                return "Standard";
            case "emod_structureslots_endosteel":
                return "Endo";
            case "emod_structureslots_clanendosteel":
                return "Clan Endo";
            case "Gear_structureslots_Composite":
                return "Composite";
            case "emod_structureslots_endocomposite":
                return "Endo-Composite";
            case "emod_structureslots_clanendorigged":
                return "Cobbled Endo";
            case "emod_structureslots_endo_standard_hybrid":
                return "Hybrid Endo";
            case "emod_structureslots_endosteel_3G":
                return "3G Endo";
            case "emod_structureslots_sanctuaryendocarbide":
                return "Endo Carbide";
            case "emod_structureslots_sanctuaryendosteel":
                return "Sanctuary Endo";
        }

        return structureRegexOutput + " TYPE NOT FOUND";
    }

    static string ArmorDecode(string armorRegexOutput)
    {
        switch (armorRegexOutput)
        {
            case "emod_armorslots_standard":
                return "Standard";
            case "emod_armorslots_clstandard":
                return "Clan Standard";
            case "emod_armorslots_ferrosfibrous":
                return "Ferro";
            case "emod_armorslots_clanferrosfibrous":
                return "Clan Ferro";
            case "emod_armorslots_heavyferrosfibrous":
                return "Heavy Ferro";
            case "emod_armorslots_clanferrolamellor":
                return "Ferro Lamellor";
            case "Gear_armorslots_Primitive":
                return "Primitive";
            case "Gear_armorslots_Hardened":
                return "Hardened";
            case "Gear_armorslots_Hardened_CLAN":
                return "Clan Hardened";
            case "emod_armorslots_heavyplating":
                return "Heavy";
            case "emod_armorslots_lightferrosfibrous":
                return "Light Ferro";
            case "emod_armorslots_reactive":
                return "Reactive";
            case "Gear_Reflective_Coating":
                return "Reflective";
            case "emod_armorslots_lightplating":
                return "Light";
            case "emod_armorslots_stealth":
                return "Stealth";
            case "emod_armorslots_ultraferrofibrous":
                return "Ultra Ferro";
            case "emod_armorslots_3rd_generation":
                return "3G Ferro";
            case "emod_armorslots_sanctuaryferrofibrous":
                return "Sanctuary Ferro";
            case "emod_armorslots_sanctuaryferrovanadium":
                return "Ferro Vanadium";
            case "emod_armorslots_industrial":
                return "Industrial";
        }

        return armorRegexOutput + " TYPE NOT FOUND";
    }

    public struct FoundFile
    {
        public string fileName;
        public string path;
    }

    public struct Hardpoint
    {
        public string type;
        public bool omni;
    }
}