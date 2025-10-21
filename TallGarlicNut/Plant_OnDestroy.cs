using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace TallGarlicNut.BepInEx
{
    /// 植物销毁时的补丁类
    /// 处理内鬼-蒜毒高坚果死亡时的片甲不留技能
    [HarmonyPatch(typeof(Plant), "OnDestroy")]
    public class Plant_OnDestroy
    {
        #region 常量定义
        /// 究极黑橄榄大帅的僵尸ID
        private const int ULTIMATE_BLACK_OLIVE_ZOMBIE_ID = 229;
        
        ///僵尸生成位置（屏幕右侧）
        private const float ZOMBIE_SPAWN_POSITION = 9.9f;
        
        ///游戏进行中的状态值
        private const int GAME_PLAYING_STATUS = 0;
        #endregion
        
        /// 植物销毁后的处理逻辑
        /// 当内鬼-蒜毒高坚果死亡时，触发片甲不留技能
        /// <param name="__instance">被销毁的植物实例</param>
        [HarmonyPostfix]
        public static void Postfix(Plant __instance)
        {
            try
            {
                // 检查是否为内鬼-蒜毒高坚果
                if (!IsTallGarlicNut(__instance))
                {
                    return;
                }

                // 检查游戏状态和游戏板是否可用
                if (!IsGameStateValid())
                {
                    return;
                }

                // 执行片甲不留技能
                ExecuteAnnihilationSkill();
            }
            catch (Exception ex)
            {
                Debug.LogError($"TallGarlicNut: 植物销毁处理时发生错误: {ex.Message}\n{ex.StackTrace}");
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
        
        /// 检查游戏状态是否有效
        /// <returns>游戏状态是否有效</returns>
        private static bool IsGameStateValid()
        {
            return Board.Instance != null && GameAPP.theGameStatus == GAME_PLAYING_STATUS;
        }
        
        /// 执行片甲不留技能
        /// 在每一行召唤一个究极黑橄榄大帅
        private static void ExecuteAnnihilationSkill()
        {
            try
            {
                int rowCount = Board.Instance.rowNum;
                
                // 在每一行召唤究极黑橄榄大帅
                for (int row = 0; row < rowCount; row++)
                {
                    CreateZombie.Instance.SetZombie(
                        row,                                    // 行号
                        (ZombieType)ULTIMATE_BLACK_OLIVE_ZOMBIE_ID,         // 僵尸ID
                        ZOMBIE_SPAWN_POSITION,                  // 生成位置
                        false                                   // 是否为特殊僵尸
                    );
                }

                Debug.Log($"TallGarlicNut: 片甲不留技能已触发，在{rowCount}行召唤了究极黑橄榄大帅");
            }
            catch (Exception ex)
            {
                Debug.LogError($"TallGarlicNut: 执行片甲不留技能时发生错误: {ex.Message}");
            }
        }
        #endregion
    }
}