using System;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace SuperSunSniperPea
{
    /// <summary>
    /// SniperPea Update 方法的 Harmony 补丁
    /// 用于在 SniperPea 的 Update 方法执行前注入自定义逻辑
    /// </summary>
    [HarmonyPatch(typeof(SniperPea), "Update")]
    public class SniperPea_Update
    {
        /// <summary>
        /// Update 方法的前缀补丁
        /// 在 SniperPea 的 Update 方法执行前调用
        /// </summary>
        /// <param name="__instance">SniperPea 实例</param>
        [HarmonyPrefix]
        public static void Prefix(SniperPea __instance)
        {
            if (__instance == null) return;

            // 检查是否为我们的自定义植物
            bool isCustomPlant = __instance.thePlantType == (PlantType)SuperSunSniperPea.PlantID;

            if (isCustomPlant)
            {
                // 获取 SuperSunSniperPea 组件并更新伤害提升逻辑
                SuperSunSniperPea customComponent = __instance.gameObject.GetComponent<SuperSunSniperPea>();
                if (customComponent != null)
                {
                    customComponent.UpdateDamageBoost();
                }
            }
        }
    }
}