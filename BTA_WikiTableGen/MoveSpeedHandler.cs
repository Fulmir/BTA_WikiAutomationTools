using BT_JsonProcessingLibrary;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BTA_WikiTableGen
{
    internal static class MoveSpeedHandler
    {
        static Regex walkSpeedSearch = new Regex("\"statName\": \"WalkSpeed\"", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static Regex runSpeedSearch = new Regex("\"statName\": \"CBTBE_RunMultiMod\"", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        static Regex jumpDistanceSearch = new Regex("\"statName\": \"JumpDistanceMultiplier\"", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static Regex jumpJetSearch = new Regex("\"JumpCapacity\"", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        static string[] fileFilterSearch = { @"Gear*.json", @"emod*.json" };

        static ConcurrentDictionary<string, MovementItem> WalkSpeedEffects = new ConcurrentDictionary<string, MovementItem>();
        static ConcurrentDictionary<string, MovementItem> SprintSpeedEffects = new ConcurrentDictionary<string, MovementItem>();
        static ConcurrentDictionary<string, MovementItem> JumpJetItems = new ConcurrentDictionary<string, MovementItem>();
        static ConcurrentDictionary<string, MovementItem> JumpDistanceItems = new ConcurrentDictionary<string, MovementItem>();

        static bool MovementSearchDone = false;

        public static void InstantiateMoveSpeedHandler(string modsFolder)
        {
            if (!MovementSearchDone)
            {
                List<BasicFileData> gearFiles = new List<BasicFileData>();
                foreach (string pattern in fileFilterSearch)
                {
                    gearFiles.AddRange(ModJsonHandler.SearchFiles(modsFolder, pattern));
                }

                Parallel.ForEach(gearFiles, new ParallelOptions() { MaxDegreeOfParallelism = 8 }, (gearFile) =>
                {
                    SearchFileForMovement(gearFile.Path);
                });
                //foreach (BasicFileData gearFile in gearFiles)
                //{
                //    SearchFileForMovement(gearFile.Path);
                //}
                MovementSearchDone = true;
            }
        }

        public static bool TryGetMovementEffectsForGear(List<string> gearIds, out Dictionary<MovementType, List<MovementItem>> movements)
        {
            movements = new Dictionary<MovementType, List<MovementItem>>();
            bool found = false;

            movements.Add(MovementType.Walk, new List<MovementItem>());
            movements.Add(MovementType.Sprint, new List<MovementItem>());
            movements.Add(MovementType.Jump, new List<MovementItem>());

            foreach(string gearId in gearIds)
            {
                if (WalkSpeedEffects.Keys.Contains(gearId))
                {
                    found = true;
                    movements[MovementType.Walk].Add(WalkSpeedEffects[gearId]);
                }
                if (SprintSpeedEffects.Keys.Contains(gearId))
                {
                    found = true;
                    movements[MovementType.Sprint].Add(SprintSpeedEffects[gearId]);
                }
                if (JumpJetItems.Keys.Contains(gearId))
                {
                    found = true;
                    movements[MovementType.Jump].Add(JumpJetItems[gearId]);
                }
                if (JumpDistanceItems.Keys.Contains(gearId))
                {
                    found = true;
                    movements[MovementType.Jump].Add(JumpDistanceItems[gearId]);
                }
            }

            return found;
        }

        public static double GetAdjustedWalkSpeed(double baseWalkSpeedInMeters, List<MovementItem> movements)
        {
            double movementAdd = 0;
            double movementMultiply = 1;

            foreach(MovementItem movementItem in movements)
            {
                if(movementItem.MoveType == MovementType.Walk)
                {
                    if(movementItem.Operation == Operation.Add)
                    {
                        movementAdd += movementItem.Value;
                    } else if(movementItem.Operation == Operation.Multiply)
                    {
                        movementMultiply *= movementItem.Value;
                    }
                }
            }

            return baseWalkSpeedInMeters * (1 + movementAdd) * movementMultiply;
        }

        public static double GetAdjustedSprintSpeed(double adjustedWalkSpeed, List<MovementItem> movements)
        {
            double movementAdd = 1.5;
            double movementMultiply = 1;

            foreach (MovementItem movementItem in movements)
            {
                if (movementItem.MoveType == MovementType.Sprint)
                {
                    if (movementItem.Operation == Operation.Add)
                    {
                        movementAdd += movementItem.Value;
                    }
                    else if (movementItem.Operation == Operation.Multiply)
                    {
                        movementMultiply *= movementItem.Value;
                    }
                }
            }

            return adjustedWalkSpeed * (movementAdd) * movementMultiply;
        }

        public static double GetJumpDistance(List<MovementItem> movements)
        {
            double baseJumpDistance = 0;
            double jumpMultiply = 1;

            foreach (MovementItem movementItem in movements)
            {
                if (movementItem.MoveType == MovementType.Jump)
                {
                    if (movementItem.Operation == Operation.Add)
                    {
                        baseJumpDistance += movementItem.Value;
                    }
                    else if (movementItem.Operation == Operation.Multiply)
                    {
                        jumpMultiply *= movementItem.Value;
                    }
                }
            }

            return baseJumpDistance * jumpMultiply;
        }

        private static void SearchFileForMovement(string filePath)
        {
            StreamReader streamReader = new StreamReader(filePath);
            string fileContents = streamReader.ReadToEnd();
            JsonDocument GearJsonDoc = JsonDocument.Parse(fileContents);

            if(walkSpeedSearch.IsMatch(fileContents))
            {
                AddWalkSpeedEffect(GearJsonDoc);
            }
            if(runSpeedSearch.IsMatch(fileContents))
            {
                AddRunSpeedEffect(GearJsonDoc);
            }
            if(jumpJetSearch.IsMatch(fileContents))
            {
                AddJumpJetEffect(GearJsonDoc);
            }
            if(jumpDistanceSearch.IsMatch(fileContents))
            {
                AddJumpMultiplierEffect(GearJsonDoc);
            }
        }

        private static void AddWalkSpeedEffect(JsonDocument gearJsonContents)
        {
            var statusEffects = gearJsonContents.RootElement.GetProperty("statusEffects").EnumerateArray();
            string gearUiName = gearJsonContents.RootElement.GetProperty("Description").GetProperty("UIName").ToString();
            string gearId = gearJsonContents.RootElement.GetProperty("Description").GetProperty("Id").ToString();
            foreach ( var statusEffect in statusEffects)
            {
                if(TryGetSpecificMovementEffect(statusEffect, MovementType.Walk, gearUiName, gearId, out var movementEffect))
                {
                    WalkSpeedEffects[movementEffect.GearId] = movementEffect;
                }
            }
        }

        private static void AddRunSpeedEffect(JsonDocument gearJsonContents)
        {
            var statusEffects = gearJsonContents.RootElement.GetProperty("statusEffects").EnumerateArray();
            string gearUiName = gearJsonContents.RootElement.GetProperty("Description").GetProperty("UIName").ToString();
            string gearId = gearJsonContents.RootElement.GetProperty("Description").GetProperty("Id").ToString();
            foreach (var statusEffect in statusEffects)
            {
                if (TryGetSpecificMovementEffect(statusEffect, MovementType.Sprint, gearUiName, gearId, out var movementEffect))
                {
                    SprintSpeedEffects[movementEffect.GearId] = movementEffect;
                }
            }
        }

        private static void AddJumpJetEffect(JsonDocument gearJsonContents)
        {
            var statusEffects = gearJsonContents.RootElement.GetProperty("statusEffects").EnumerateArray();
            string gearUiName = gearJsonContents.RootElement.GetProperty("Description").GetProperty("UIName").ToString();
            string gearId = gearJsonContents.RootElement.GetProperty("Description").GetProperty("Id").ToString();
            foreach (var statusEffect in statusEffects)
            {
                if (statusEffect.TryGetProperty("JumpCapacity", out JsonElement value))
                {
                    var movementEffect = new MovementItem()
                    {
                        GearId = gearId,
                        EffectId = statusEffect.GetProperty("Description").GetProperty("Id").ToString(),
                        UIName = gearUiName,
                        MoveType = MovementType.Jump,
                        Operation = Operation.Add,
                        Value = value.GetDouble()
                    };
                    JumpJetItems[movementEffect.GearId] = movementEffect;
                }
            }
        }

        private static void AddJumpMultiplierEffect(JsonDocument gearJsonContents)
        {
            var statusEffects = gearJsonContents.RootElement.GetProperty("statusEffects").EnumerateArray();
            string gearUiName = gearJsonContents.RootElement.GetProperty("Description").GetProperty("UIName").ToString();
            string gearId = gearJsonContents.RootElement.GetProperty("Description").GetProperty("Id").ToString();
            foreach (var statusEffect in statusEffects)
            {
                if (TryGetSpecificMovementEffect(statusEffect, MovementType.Jump, gearUiName, gearId, out var movementEffect))
                {
                    JumpDistanceItems[movementEffect.GearId] = movementEffect;
                }
            }
        }

        private static bool TryGetSpecificMovementEffect(JsonElement statusEffect, MovementType moveType, string gearUiName, string gearId, out MovementItem movementEffect)
        {
            if (statusEffect.GetProperty("statisticData").GetProperty("statName").ToString().Equals(MovementTypeToStatName(moveType)))
            {
                movementEffect = new MovementItem()
                {
                    GearId = gearId,
                    EffectId = statusEffect.GetProperty("Description").GetProperty("Id").ToString(),
                    UIName = gearUiName,
                    MoveType = moveType,
                    Operation = StringToOperation(statusEffect.GetProperty("statisticData").GetProperty("operation").ToString()),
                    Value = Convert.ToDouble(statusEffect.GetProperty("statisticData").GetProperty("modValue").ToString())
                };

                return true;
            }
            movementEffect = new MovementItem();
            return false;
        }

        private static string MovementTypeToStatName(MovementType movementType)
        {
            switch (movementType)
            {
                case MovementType.Walk:
                    return "WalkSpeed";
                case MovementType.Sprint:
                    return "CBTBE_RunMultiMod";
                case MovementType.Jump:
                    return "JumpDistanceMultiplier";
            }
            return "";
        }

        private static Operation StringToOperation(string oper)
        {
            switch(oper)
            {
                case "Float_Add":
                    return Operation.Add;
                case "Float_Multiply":
                    return Operation.Multiply;
            }
            return Operation.None;
        }
    }
}
