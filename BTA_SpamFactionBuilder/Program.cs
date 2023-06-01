using BT_JsonProcessingLibrary;
using System.Collections.Generic;
using System.Text.Json;
using System.Xml.Linq;

namespace BTA_SpamFactionBuilder
{
    internal class Program
    {
        static void Main(string[] args)
        {
            FactionDataProcessor processor = new FactionDataProcessor();

            FactionDataHandler.OutputIdsToNamesFile();

            processor.OutputSpamFactionsToParentsTranslation();


            processor.OutputMercFactionInfo();

            processor.OutputFactionSubCommands();
        }
    }
}