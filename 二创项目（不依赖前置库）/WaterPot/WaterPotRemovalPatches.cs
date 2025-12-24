using System;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace WaterPot.BepInEx
{
    internal static class WaterPotRemovalHelper
    {
        public static bool TryHandleShovelFallback()
        {
            var mouse = Mouse.Instance;
            if (mouse == null)
            {
                return false;
            }

            var itemOnMouse = mouse.theItemOnMouse;
            if (itemOnMouse == null || itemOnMouse.name != "Shovel")
            {
                return false;
            }

            var camera = Camera.main;
            if (camera == null)
            {
                return false;
            }

            Vector3 worldPos = camera.ScreenToWorldPoint(UnityEngine.Input.mousePosition);
            RaycastHit2D[] hits = Physics2D.RaycastAll(worldPos, Vector2.zero);
            if (hits == null || hits.Length == 0)
            {
                return false;
            }

            Plant? potPlant = null;
            Plant? topPlant = null;

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

                if (plant == null)
                {
                    continue;
                }

                if (plant.thePlantType == (PlantType)Core.PlantID)
                {
                    potPlant ??= plant;
                    continue;
                }

                if (topPlant == null)
                {
                    topPlant = plant;
                }
            }

            if (potPlant == null)
            {
                return false;
            }

            try
            {
                if (topPlant != null)
                {
                    topPlant.Die(Plant.DieReason.ByShovel);
                }
                else
                {
                    potPlant.Die(Plant.DieReason.ByShovel);
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WaterPot] Shovel fallback failed: {ex}");
                return false;
            }
        }
    }

    [HarmonyPatch(typeof(Mouse), nameof(Mouse.GetPlantsOnMouse))]
    internal static class Mouse_GetPlantsOnMouse_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(ref Il2CppStructArray<RaycastHit2D> hits, ref Il2CppSystem.Collections.Generic.List<Plant> __result)
        {
            __result = BuildPlantsUnderMouse();
            return false;
        }

        private static Il2CppSystem.Collections.Generic.List<Plant> BuildPlantsUnderMouse()
        {
            var list = new Il2CppSystem.Collections.Generic.List<Plant>();
            var camera = Camera.main;
            if (camera == null)
            {
                return list;
            }

            Vector3 worldPos = camera.ScreenToWorldPoint(UnityEngine.Input.mousePosition);
            RaycastHit2D[] managedHits = Physics2D.RaycastAll(worldPos, Vector2.zero);
            if (managedHits == null || managedHits.Length == 0)
            {
                return list;
            }

            foreach (var managedHit in managedHits)
            {
                var collider = managedHit.collider;
                if (collider == null)
                {
                    continue;
                }

                if (!collider.TryGetComponent(out Plant plant) || plant == null)
                {
                    continue;
                }

                bool exists = false;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] == plant)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    list.Add(plant);
                }
            }

            return list;
        }
    }

    [HarmonyPatch(typeof(Mouse), nameof(Mouse.GetPlantsOnMouse))]
    internal static class Mouse_GetPlantsOnMouse_FilterWaterPotPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref Il2CppSystem.Collections.Generic.List<Plant> __result)
        {
            if (__result == null || __result.Count <= 1)
            {
                return;
            }

            for (int i = __result.Count - 1; i >= 0; i--)
            {
                var candidate = __result[i];
                if (candidate == null || candidate.thePlantType != (PlantType)Core.PlantID)
                {
                    continue;
                }

                bool hasOtherSameCell = false;
                for (int j = 0; j < __result.Count; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    var other = __result[j];
                    if (other == null)
                    {
                        continue;
                    }

                    if (other.thePlantColumn == candidate.thePlantColumn &&
                        other.thePlantRow == candidate.thePlantRow)
                    {
                        hasOtherSameCell = true;
                        break;
                    }
                }

                if (hasOtherSameCell)
                {
                    __result.RemoveAt(i);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Mouse), nameof(Mouse.LeftClickWithSomeThing))]
    internal static class Mouse_LeftClickWithSomeThing_Finalizer
    {
        [HarmonyFinalizer]
        public static Exception? Finalizer(Exception __exception)
        {
            if (__exception is NullReferenceException && WaterPotRemovalHelper.TryHandleShovelFallback())
            {
                return null;
            }

            return __exception;
        }
    }

    [HarmonyPatch(typeof(Garden))]
    internal static class Garden_IsWaterBox_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Garden.IsWaterBox), typeof(int), typeof(int))]
        public static bool PrefixInt(Garden __instance, int theColumn, int theRow, ref bool __result)
        {
            if (WaterPotGridUtility.HasWaterPotNearby(theColumn, theRow))
            {
                __result = true;
                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Garden.IsWaterBox), typeof(Vector3Int))]
        public static bool PrefixVector(Garden __instance, Vector3Int box, ref bool __result)
        {
            if (WaterPotGridUtility.HasWaterPotNearby(box.x, box.y))
            {
                __result = true;
                return false;
            }

            return true;
        }
    }
}
