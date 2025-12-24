using System;
using HarmonyLib;
using UnityEngine;

namespace UltimateApocalypseChomper.BepInEx
{
    /// <summary>
    /// UltimateChomper.AnimShoot 补丁
    /// </summary>
    [HarmonyPatch(typeof(UltimateChomper), "AnimShoot")]
    internal static class UltimateChomperAnimShootPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(UltimateChomper __instance)
        {
            if (__instance == null || __instance.thePlantType != (PlantType)Core.PlantId)
                return true;
            var comp = __instance.GetComponent<UltimateApocalypseChomperComponent>();
            if (comp == null) return true;
            try
            {
                return !comp.PerformGroupAttack();
            }
            catch (Exception ex)
            {
                Core.PluginLog?.LogError($"[究极天启樱龙] AnimShoot 前缀异常：{ex}");
                return true;
            }
        }
    }

    /// <summary>
    /// UltimateChomper.BiteEvent 补丁
    /// </summary>
    [HarmonyPatch(typeof(UltimateChomper), "BiteEvent")]
    internal static class UltimateChomperBiteEventPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(UltimateChomper __instance)
        {
            try
            {
                if (__instance == null) return true;
                if (__instance.thePlantType != (PlantType)Core.PlantId) return true;
                try
                {
                    if (GameAPP.theGameStatus != GameStatus.InGame && 
                        (__instance.targetZombie == null || __instance.targetZombie.beforeDying || __instance.targetZombie.isMindControlled))
                    {
                        __instance.targetZombie = null;
                        __instance.ChomperSearchZombie(null);
                    }
                }
                catch { }
                
                if (__instance.targetZombie == null)
                {
                    __instance.theStatus = 0;
                    __instance.attributeCountdown = 0f;
                    return false;
                }
                return true;
            }
            catch { return true; }
        }
    }

    /// <summary>
    /// UltimateChomper.Bite 补丁
    /// </summary>
    [HarmonyPatch(typeof(UltimateChomper), "Bite")]
    internal static class UltimateChomperBitePatch
    {
        [HarmonyPostfix]
        public static void Postfix(UltimateChomper __instance)
        {
            try
            {
                if (__instance == null) return;
                if (__instance.thePlantType == (PlantType)Core.PlantId)
                    __instance.theStatus = 0;
            }
            catch { }
        }
    }

    /// <summary>
    /// Plant.TakeDamage 补丁
    /// </summary>
    [HarmonyPatch(typeof(Plant), nameof(Plant.TakeDamage))]
    internal static class PlantTakeDamagePatch
    {
        private const int DamageCap = 500;

        [HarmonyPrefix]
        public static bool Prefix(Plant __instance, ref int damage)
        {
            try
            {
                if (__instance == null) return true;
                if (__instance.thePlantType != (PlantType)Core.PlantId) return true;
                if (damage > DamageCap) damage = DamageCap;

                var comp = __instance.gameObject?.GetComponent<UltimateApocalypseChomperComponent>();
                return comp != null ? comp.HandleIncomingDamage(__instance, ref damage) : true;
            }
            catch (Exception ex)
            {
                Core.PluginLog?.LogError($"[究极天启樱龙] TakeDamage补丁错误：{ex}");
                return true;
            }
        }
    }

    /// <summary>
    /// BombCherry.PlantTakeDamage 补丁
    /// </summary>
    [HarmonyPatch(typeof(BombCherry), "PlantTakeDamage")]
    internal static class BombCherryPlantTakeDamagePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(ref Plant plant)
        {
            if (plant == null || (UnityEngine.Object)plant == null) return true;
            if (plant.thePlantType != (PlantType)Core.PlantId) return true;
            if (plant.gameObject == null) return true;
            
            try
            {
                var component = plant.GetComponent<UltimateChomper>();
                if (component != null)
                {
                    var comp = plant.gameObject.GetComponent<UltimateApocalypseChomperComponent>();
                    if (comp != null) comp.HealPlant(component, 3200f);
                }
            }
            catch { }
            return false;
        }
    }
}
