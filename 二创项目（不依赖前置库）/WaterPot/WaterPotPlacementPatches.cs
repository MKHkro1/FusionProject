using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace WaterPot.BepInEx
{
    internal static class WaterPotPlacementBypass
    {
        [ThreadStatic]
        private static bool s_active;

        [ThreadStatic]
        private static PlantType s_targetType;

        public static void Begin(PlantType plantType)
        {
            s_active = true;
            s_targetType = plantType;
        }

        public static void End()
        {
            s_active = false;
            s_targetType = default;
        }

        public static bool ShouldIgnore(PlantType plantType) => s_active && plantType == s_targetType;
    }

    internal static class WaterPotFusionBypass
    {
        [ThreadStatic]
        private static HashSet<PlantType>? s_ignoredTypes;

        [ThreadStatic]
        private static bool s_active;

        public static void Begin(IEnumerable<PlantType> plantTypes)
        {
            s_active = true;
            s_ignoredTypes ??= new HashSet<PlantType>();
            s_ignoredTypes.Clear();
            foreach (var type in plantTypes)
            {
                s_ignoredTypes.Add(type);
            }
        }

        public static void End()
        {
            s_active = false;
            s_ignoredTypes?.Clear();
        }

        public static bool ShouldIgnore(PlantType plantType) =>
            s_active && s_ignoredTypes != null && s_ignoredTypes.Contains(plantType);
    }

    [HarmonyPatch(typeof(Mouse), nameof(Mouse.LeftClickWithSomeThing))]
    internal static class Mouse_LeftClickWithSomeThing_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(Mouse __instance, ref bool __state)
        {
            __state = false;

            if (__instance == null)
            {
                return;
            }

            PlantType plantingType = __instance.thePlantTypeOnMouse;
            if ((int)plantingType < 0)
            {
                return;
            }

            if (!TypeMgr.IsWaterPlant(plantingType))
            {
                return;
            }

            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            Vector3 worldPos = camera.ScreenToWorldPoint(UnityEngine.Input.mousePosition);
            RaycastHit2D[] hits = Physics2D.RaycastAll(worldPos, Vector2.zero);
            if (hits == null || hits.Length == 0)
            {
                return;
            }

            bool hasWaterPot = false;
            HashSet<Plant> inspected = new();

            foreach (var hit in hits)
            {
                var collider = hit.collider;
                if (collider == null)
                {
                    continue;
                }

                if (!collider.TryGetComponent(out Plant plant))
                {
                    continue;
                }

                if (!inspected.Add(plant))
                {
                    continue;
                }

                if (plant.thePlantType == (PlantType)Core.PlantID)
                {
                    hasWaterPot = true;
                    continue;
                }

                if (plant.plantTag.pumpkinPlant)
                {
                    continue;
                }

                // 同格存在其它植物，仍按原始规则处理
                return;
            }

            if (hasWaterPot)
            {
                WaterPotPlacementBypass.Begin(plantingType);
                __state = true;
            }
        }

        [HarmonyPostfix]
        public static void Postfix(bool __state)
        {
            if (__state)
            {
                WaterPotPlacementBypass.End();
            }
        }
    }

    [HarmonyPatch(typeof(TypeMgr), nameof(TypeMgr.IsWaterPlant))]
    internal static class TypeMgr_IsWaterPlant_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(PlantType theSeedType, ref bool __result)
        {
            if (__result &&
                (WaterPotPlacementBypass.ShouldIgnore(theSeedType) ||
                 WaterPotFusionBypass.ShouldIgnore(theSeedType)))
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(CreatePlant), nameof(CreatePlant.CheckMix))]
    internal static class CreatePlant_CheckMix_WaterPotPatch
    {
        [HarmonyPrefix]
        public static void Prefix(int theColumn, int theRow)
        {
            if (!TryCollectOverrideTypes(theColumn, theRow, out var types))
            {
                WaterPotFusionBypass.End();
                return;
            }

            WaterPotFusionBypass.Begin(types!);
        }

        [HarmonyPostfix]
        public static void Postfix()
        {
            WaterPotFusionBypass.End();
        }

        internal static bool TryCollectOverrideTypes(int column, int row, out List<PlantType>? types)
        {
            types = null;
            var plants = Lawnf.Get1x1Plants(column, row);
            if (plants == null || plants.Count == 0)
            {
                return false;
            }

            bool hasWaterPot = false;

            for (int i = 0; i < plants.Count; i++)
            {
                var plant = plants[i];
                if (plant == null)
                {
                    continue;
                }

                if (plant.thePlantType == (PlantType)Core.PlantID)
                {
                    hasWaterPot = true;
                    continue;
                }

                if (TypeMgr.IsWaterPlant(plant.thePlantType))
                {
                    types ??= new List<PlantType>();
                    types.Add(plant.thePlantType);
                }
            }

            return hasWaterPot && types != null && types.Count > 0;
        }
    }

    [HarmonyPatch(typeof(CreatePlant), nameof(CreatePlant.MixFail))]
    internal static class CreatePlant_MixFail_WaterPotPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(int theBoxColumn, int theBoxRow, int newPlantType, ref bool __result, ref bool __state)
        {
            var plantType = (PlantType)newPlantType;
            bool isWaterPlant = TypeMgr.IsWaterPlant(plantType);
            bool hasWaterPot = WaterPotGridUtility.ContainsWaterPotAt(theBoxColumn, theBoxRow);

            if (isWaterPlant && hasWaterPot)
            {
                __result = false;
                return false;
            }

            if (!CreatePlant_CheckMix_WaterPotPatch.TryCollectOverrideTypes(theBoxColumn, theBoxRow, out var types))
            {
                WaterPotFusionBypass.End();
                __state = false;
                return true;
            }

            WaterPotFusionBypass.Begin(types!);
            __state = true;
            return true;
        }

        [HarmonyPostfix]
        public static void Postfix(bool __state)
        {
            if (__state)
            {
                WaterPotFusionBypass.End();
            }
        }
    }

    [HarmonyPatch(typeof(CreatePlant), nameof(CreatePlant.CheckBox))]
    internal static class CreatePlant_CheckBox_WaterPotRestrictionPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(int theBoxColumn, int theBoxRow, int theSeedType, ref bool __result, ref bool __state)
        {
            __state = false;
            if (!WaterPotGridUtility.ContainsWaterPotAt(theBoxColumn, theBoxRow))
            {
                return true;
            }

            var plantType = (PlantType)theSeedType;

            if (plantType == (PlantType)Core.PlantID)
            {
                return true;
            }

            if (TypeMgr.IsWaterPlant(plantType))
            {
                WaterPotPlacementBypass.Begin(plantType);
                __state = true;
                return true;
            }

            if (IsWaterPlantIgnoringBypass(plantType))
            {
                return true;
            }

            __result = false;
            return false;
        }

        private static bool IsWaterPlantIgnoringBypass(PlantType plantType)
        {
            if (WaterPotPlacementBypass.ShouldIgnore(plantType) ||
                WaterPotFusionBypass.ShouldIgnore(plantType))
            {
                return true;
            }

            return TypeMgr.IsWaterPlant(plantType);
        }

        [HarmonyPostfix]
        public static void Postfix(bool __state)
        {
            if (__state)
            {
                WaterPotPlacementBypass.End();
            }
        }
    }

    [HarmonyPatch(typeof(Board), nameof(Board.GetBoxType))]
    internal static class Board_GetBoxType_WaterPotPatch
    {
        private const int WaterBoxValue = 2;

        [HarmonyPostfix]
        public static void Postfix(int theColumn, int theRow, ref BoxType __result)
        {
            if ((int)__result == WaterBoxValue)
            {
                return;
            }

            if (WaterPotGridUtility.ContainsWaterPotAt(theColumn, theRow))
            {
                __result = (BoxType)WaterBoxValue;
            }
        }
    }

    [HarmonyPatch(typeof(Board), nameof(Board.Update))]
    internal static class Board_Update_WaterPotAutoPlacementPatch
    {
        private static float s_nextScanTime;
        private const float ScanInterval = 0.5f;
        private const float SpawnDelaySeconds = 2f;

        private static readonly Dictionary<Vector2Int, float> s_pendingSpawnTimes = new();

        [HarmonyPostfix]
        public static void Postfix(Board __instance)
        {
            if (__instance == null || CreatePlant.Instance == null)
            {
                return;
            }

            if (Time.time < s_nextScanTime)
            {
                return;
            }

            s_nextScanTime = Time.time + ScanInterval;
            TryAttachMissingWaterPots(__instance);
        }

        private static void TryAttachMissingWaterPots(Board board)
        {
            int columnCount = Mathf.Max(board.columnNum, 0);
            int rowCount = Mathf.Max(board.rowNum, 0);

            for (int column = 0; column < columnCount; column++)
            {
                for (int row = 0; row < rowCount; row++)
                {
                    if (!NeedsWaterPot(column, row, board))
                    {
                        ClearPendingRequest(column, row);
                        continue;
                    }

                    if (HasWaitedLongEnough(column, row))
                    {
                        TrySpawnWaterPot(column, row);
                    }
                }
            }
        }

        private static bool NeedsWaterPot(int column, int row, Board board)
        {
            if (WaterPotGridUtility.ContainsWaterPotAt(column, row))
            {
                return false;
            }

            if (!HasWaterPlant(column, row))
            {
                return false;
            }

            return !IsWaterBox(board, column, row);
        }

        private static bool HasWaterPlant(int column, int row)
        {
            var plants = Lawnf.Get1x1Plants(column, row);
            if (plants == null || plants.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < plants.Count; i++)
            {
                var plant = plants[i];
                if (plant == null)
                {
                    continue;
                }

                if (plant.thePlantType == (PlantType)Core.PlantID)
                {
                    return false;
                }

                if (TypeMgr.IsWaterPlant(plant.thePlantType))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsWaterBox(Board board, int column, int row)
        {
            try
            {
                return board.GetBoxType(column, row) == BoxType.Water;
            }
            catch
            {
                return false;
            }
        }

        private static void TrySpawnWaterPot(int column, int row)
        {
            try
            {
                ClearPendingRequest(column, row);
                CreatePlant.Instance.SetPlant(column, row, (PlantType)Core.PlantID);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WaterPot] 自动补全水盆失败({column},{row}): {ex.Message}");
            }
        }

        private static bool HasWaitedLongEnough(int column, int row)
        {
            var key = new Vector2Int(column, row);
            float now = Time.time;

            if (s_pendingSpawnTimes.TryGetValue(key, out var executeAt))
            {
                if (now >= executeAt)
                {
                    s_pendingSpawnTimes.Remove(key);
                    return true;
                }

                return false;
            }

            s_pendingSpawnTimes[key] = now + SpawnDelaySeconds;
            return false;
        }

        private static void ClearPendingRequest(int column, int row)
        {
            var key = new Vector2Int(column, row);
            s_pendingSpawnTimes.Remove(key);
        }
    }
}
