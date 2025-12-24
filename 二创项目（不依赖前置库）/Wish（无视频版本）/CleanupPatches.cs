using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Wish.BepInEx
{
    /// <summary>
    /// 监听植物销毁事件，清理对应的协程（无视频版本）
    /// </summary>
    [HarmonyPatch(typeof(Plant), "OnDestroy")]
    internal static class PlantOnDestroyPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Plant __instance)
        {
            try
            {
                if (__instance == null || __instance.thePlantType != (PlantType)Core.PlantId)
                    return;

                if (__instance is GoldSunflower goldSunflower)
                {
                    if (GoldSunflowerSuperSkillPatch.ActiveCoroutines.TryGetValue(goldSunflower, out var coroutine))
                    {
                        try
                        {
                            if (coroutine != null)
                            {
                                goldSunflower.StopCoroutine(coroutine);
                            }
                        }
                        catch (Exception ex)
                        {
                            Core.Logger?.LogError($"[纠缠之缘] 停止协程失败：{ex.Message}");
                        }
                        GoldSunflowerSuperSkillPatch.ActiveCoroutines.Remove(goldSunflower);
                    }

                    Core.ProbabilityStates.Remove(goldSunflower);
                }
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[纠缠之缘] PlantOnDestroy处理失败：{ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(Board), "Update")]
    internal static class BoardUpdatePatch
    {
        private static float lastCleanupTime = 0f;
        private const float CleanupInterval = 10f;
        private static int frameSkipCounter = 0;
        private const int FrameSkipInterval = 60;

        [HarmonyPostfix]
        private static void Postfix()
        {
            frameSkipCounter++;
            if (frameSkipCounter < FrameSkipInterval)
                return;
            frameSkipCounter = 0;

            int count = Core.ProbabilityStates.Count;
            int coroutineCount = GoldSunflowerSuperSkillPatch.ActiveCoroutines.Count;

            if (count == 0 && coroutineCount == 0)
                return;

            try
            {
                float currentTime = Time.time;
                if (currentTime - lastCleanupTime < CleanupInterval)
                    return;

                lastCleanupTime = currentTime;

                var toRemove = new List<GoldSunflower>(Math.Min(count / 4, 32));

                foreach (var kvp in Core.ProbabilityStates)
                {
                    var plant = kvp.Key;
                    if (plant == null)
                    {
                        toRemove.Add(plant!);
                        continue;
                    }

                    var gameObj = plant.gameObject;
                    if (gameObj == null)
                    {
                        toRemove.Add(plant);
                        continue;
                    }

                    if (!gameObj.activeInHierarchy)
                    {
                        toRemove.Add(plant);
                    }
                }

                if (toRemove.Count > 0)
                {
                    foreach (var plant in toRemove)
                    {
                        Core.ProbabilityStates.Remove(plant);
                    }
                }

                var coroutinesToRemove = new List<GoldSunflower>();
                foreach (var kvp in GoldSunflowerSuperSkillPatch.ActiveCoroutines)
                {
                    var plant = kvp.Key;
                    var coroutine = kvp.Value;

                    bool plantDestroyed = false;
                    try
                    {
                        plantDestroyed = plant == null || plant.gameObject == null || !plant.gameObject.activeInHierarchy;
                    }
                    catch
                    {
                        plantDestroyed = true;
                    }

                    if (plantDestroyed)
                    {
                        if (coroutine != null && plant != null)
                        {
                            try
                            {
                                plant.StopCoroutine(coroutine);
                            }
                            catch { }
                        }

                        if (plant != null)
                            coroutinesToRemove.Add(plant);
                    }
                }

                if (coroutinesToRemove.Count > 0)
                {
                    foreach (var plant in coroutinesToRemove)
                    {
                        GoldSunflowerSuperSkillPatch.ActiveCoroutines.Remove(plant);
                    }
                }
            }
            catch
            {
                // 忽略清理错误
            }
        }
    }
}
