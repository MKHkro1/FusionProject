using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace TallGarlicNut.BepInEx
{
    /// TypeMgr.UncrashablePlant方法的补丁类
    /// 使内鬼-蒜毒高坚果免疫碾压和砸击伤害
    [HarmonyPatch(typeof(TypeMgr), "UncrashablePlant")]
    public class TypeMgrUncrashablePlantPatch
    {
        /// 拦截UncrashablePlant方法调用
        /// 为内鬼-蒜毒高坚果提供碾压免疫
        /// <param name="plant">要检查的植物</param>
        /// <param name="__result">原始方法的返回值</param>
        /// <returns>是否继续执行原始方法</returns>
        [HarmonyPrefix]
        public static bool Prefix(ref Plant plant, ref bool __result)
        {
            try
            {
                // 检查是否为内鬼-蒜毒高坚果
                if (IsTallGarlicNut(plant))
                {
                    // 设置为不可碾压，阻止原始方法执行
                    __result = true;
                    return false;
                }

                // 对于其他植物，继续执行原始方法
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"TallGarlicNut: 检查植物碾压免疫时发生错误: {ex.Message}\n{ex.StackTrace}");
                
                // 发生错误时，继续执行原始方法以确保游戏稳定性
                return true;
            }
        }

        #region 私有方法
        /// 检查是否为内鬼-蒜毒高坚果
        /// <param name="plant">植物实例</param>
        /// <returns>是否为内鬼-蒜毒高坚果</returns>
        private static bool IsTallGarlicNut(Plant plant)
        {
            return plant != null && plant.thePlantType == (PlantType)TallGarlicNut.PlantID;
        }
        #endregion
    }
}