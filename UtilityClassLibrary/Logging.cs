using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UtilityClassLibrary
{
    public struct LogMessage
    {
        public string Message;
        public LogCategories Category;
        public LogLevel Level;
    }

    public enum LogLevel
    { Debug, Info, Reporting, Warning, Error, Critical };

    public enum LogCategories
    { Immediate, Quirks, Affinities, MechDefs, VehicleDefs, Gear, Factions, Planets, Stores, Bonuses };

    public static class Logging
    {
        private static ConcurrentQueue<LogMessage> _LogMessages = new ConcurrentQueue<LogMessage>();
        private static Dictionary<LogCategories, List<LogMessage>> _ReportsByCategory = new Dictionary<LogCategories, List<LogMessage>>();

        public static bool ActiveLogging = false;

        public static async void AddLogToQueue(LogMessage message)
        {
            _LogMessages.Enqueue(message);
        }

        public static async void AddLogToQueue(string message, LogLevel level, LogCategories category)
        {
            _LogMessages.Enqueue(new LogMessage { Message = message, Category = category, Level = level});
        }

        public static async void ProcessLogMessages(CancellationToken cancellationToken = default)
        {
            ActiveLogging = true;

            Directory.CreateDirectory(".\\Logging");

            StreamWriter logWriter = new StreamWriter($".\\Logging\\BTA_WikiPageGenLog{UtilityStatics.LocalDateTimeToFileString()}.txt", false, Encoding.UTF8);

            while (!cancellationToken.IsCancellationRequested || !_LogMessages.IsEmpty)
            {
                if(_LogMessages.TryDequeue(out LogMessage message))
                {
                    if (message.Category != LogCategories.Immediate)
                    {
                        if(!_ReportsByCategory.ContainsKey(message.Category))
                            _ReportsByCategory.Add(message.Category, new List<LogMessage>());
                        _ReportsByCategory[message.Category].Add(message);
                    }
                    else
                    {
                        logWriter.WriteLine(GetMessagePrefixForLogLevel(message.Level) + message.Message);
                    }
                }
                else
                    await Task.Delay(1000);
            }
            
            foreach(LogCategories category in _ReportsByCategory.Keys)
            {
                logWriter.WriteLine();
                logWriter.WriteLine();
                logWriter.WriteLine(GetSectionTitleFromCategory(category));
                logWriter.WriteLine();

                foreach(LogMessage message in _ReportsByCategory[category])
                { 
                    logWriter.WriteLine(GetMessagePrefixForLogLevel(message.Level) + message.Message);
                }
            }

            logWriter.Close();

            logWriter.Dispose();

            ActiveLogging = false;
        }

        private static string GetSectionTitleFromCategory(LogCategories category)
        {
            switch(category)
            {
                case LogCategories.Immediate:
                    return "-------- STARTING BTA WIKI GENERATION --------";
                case LogCategories.Affinities:
                    return "-------- UNIT AFFINITY REPORT --------";
                case LogCategories.Gear:
                    return "-------- GEAR DATA REPORT --------";
                case LogCategories.MechDefs:
                    return "-------- MECH DEF DATA REPORT --------";
                case LogCategories.Quirks:
                    return "-------- QUIRK DATA REPORT --------";
                case LogCategories.VehicleDefs:
                    return "-------- VEHICLE DEF DATA REPORT --------";
                case LogCategories.Factions:
                    return "-------- FACTION DATA REPORT --------";
                case LogCategories.Planets:
                    return "-------- PLANET DATA REPORT --------";
                case LogCategories.Stores:
                    return "-------- STORE DATA REPORT --------";
                case LogCategories.Bonuses:
                    return "-------- GEAR BONUS DATA REPORT --------";
            }

            return $"-------- [ERROR] CATEGORY NOT FOUND {category.ToString()} --------";
        }

        private static string GetMessagePrefixForLogLevel(LogLevel logLevel)
        {
            switch(logLevel)
            {
                case LogLevel.Debug:
                    return "[DEBUG]: ";
                case LogLevel.Info:
                    return "[INFO]: ";
                case LogLevel.Warning:
                    return "[WARNING]: ";
                case LogLevel.Error:
                    return "[ERROR]: ";
                case LogLevel.Critical:
                    return "[CRITICAL]: ";
                case LogLevel.Reporting:
                    return "";
            }

            return "";
        }
    }
}