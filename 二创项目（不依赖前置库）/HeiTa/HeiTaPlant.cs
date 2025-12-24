using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;

namespace HeiTa.BepInEx
{
    /// <summary>
    /// 黑塔核心逻辑（组件 + Harmony 补丁）
    /// </summary>
    public class HeiTaPlant : MonoBehaviour
    {
        /// <summary>
        /// 游戏内植物 ID：黑塔 = 2018
        /// </summary>
        public static int PlantID = 2018;

        private enum TravelBuffCategory
        {
            Advanced,
            Ultimate,
            Debuff
        }

        public HeiTaPlant(IntPtr ptr) : base(ptr)
        {
        }

        /// <summary>
        /// 初始化时调整黑塔贴图缩放
        /// </summary>
        private void Awake()
        {
            try
            {
                // 将整体缩放设置为 0.6f
                transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HeiTa] 在 Awake 中设置缩放失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 关联的基础植物组件
        /// </summary>
        public Plant Plant =>
            gameObject.GetComponent<Plant>();

        #region 旅行词条相关逻辑

        /// <summary>
        /// 黑塔触发时：随机开出任意类别的旅行词条（植物/僵尸词条都可抽）
        /// </summary>
        [HideFromIl2Cpp]
        public static void OpenRandomTravelEntry()
        {
            if (GameAPP.gameAPP == null)
            {
                Debug.LogWarning("[HeiTa] GameAPP.gameAPP 为 null，无法抽词条");
                return;
            }

            // 尝试多种方式获取 TravelMgr
            TravelMgr? travel = null;
            
            // 方式1：从 GameAPP.gameAPP 获取
            travel = GameAPP.gameAPP.GetComponent<TravelMgr>();
            
            // 方式2：如果方式1失败，尝试在整个场景中查找
            if (travel == null)
            {
                travel = UnityEngine.Object.FindObjectOfType<TravelMgr>();
                if (travel != null)
                {
                    Debug.Log("[HeiTa] 通过 FindObjectOfType 找到了 TravelMgr");
                }
            }
            
            // 方式3：如果还是找不到，尝试从 GameAPP.board 获取
            if (travel == null && GameAPP.board != null)
            {
                travel = GameAPP.board.GetComponent<TravelMgr>();
                if (travel != null)
                {
                    Debug.Log("[HeiTa] 从 GameAPP.board 找到了 TravelMgr");
                }
            }

            if (travel == null)
            {
                Debug.LogError("[HeiTa] 无法找到 TravelMgr 组件，可能是 Modified-Plus 插件冲突或游戏未初始化");
                try
                {
                    if (InGameText.Instance != null)
                    {
                        InGameText.Instance.ShowText("黑塔：无法找到词条管理器", 3f);
                    }
                }
                catch { }
                return;
            }

            List<(TravelBuffCategory category, int index)> candidates = [];
            int advancedCount = 0;
            int ultimateCount = 0;
            int debuffCount = 0;

            try
            {
                if (travel.advancedUpgrades != null)
                {
                    for (int i = 0; i < travel.advancedUpgrades.Count; i++)
                    {
                        if (!travel.advancedUpgrades[i])
                        {
                            candidates.Add((TravelBuffCategory.Advanced, i));
                            advancedCount++;
                        }
                    }
                }

                if (travel.ultimateUpgrades != null)
                {
                    for (int i = 0; i < travel.ultimateUpgrades.Count; i++)
                    {
                        if (travel.ultimateUpgrades[i] == 0)
                        {
                            candidates.Add((TravelBuffCategory.Ultimate, i));
                            ultimateCount++;
                        }
                    }
                }

                if (travel.debuff != null)
                {
                    for (int i = 0; i < travel.debuff.Count; i++)
                    {
                        if (!travel.debuff[i])
                        {
                            candidates.Add((TravelBuffCategory.Debuff, i));
                            debuffCount++;
                        }
                    }
                }
                
                Debug.Log($"[HeiTa] 找到可抽词条：高级 {advancedCount} 个，终极 {ultimateCount} 个，负面 {debuffCount} 个，总计 {candidates.Count} 个");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HeiTa] 收集可抽词条失败: {ex.Message}\n{ex.StackTrace}");
            }

            if (candidates.Count == 0)
            {
                Debug.LogWarning("[HeiTa] 没有可抽的词条，可能所有词条都已解锁");
                try
                {
                    if (InGameText.Instance != null)
                    {
                        InGameText.Instance.ShowText("黑塔：所有词条已解锁，无法抽取", 3f);
                    }
                }
                catch { }
                return;
            }

            var choice = candidates[UnityEngine.Random.RandomRangeInt(0, candidates.Count)];
            string? unlockedText = null;

            try
            {
                switch (choice.category)
                {
                    case TravelBuffCategory.Advanced:
                        if (travel.advancedUpgrades != null)
                        {
                            travel.advancedUpgrades[choice.index] = true;
                        }

                        try
                        {
                            var text = TravelMgr.advancedBuffs[choice.index];
                            if (!string.IsNullOrEmpty(text))
                            {
                                unlockedText = text;
                            }
                        }
                        catch { }
                        break;

                    case TravelBuffCategory.Ultimate:
                        if (travel.ultimateUpgrades != null)
                        {
                            travel.ultimateUpgrades[choice.index] = 1;
                        }

                        try
                        {
                            var text = TravelMgr.ultimateBuffs[choice.index];
                            if (!string.IsNullOrEmpty(text))
                            {
                                unlockedText = text;
                            }
                        }
                        catch { }
                        break;

                    case TravelBuffCategory.Debuff:
                        if (travel.debuff != null)
                        {
                            travel.debuff[choice.index] = true;
                        }

                        try
                        {
                            var text = TravelMgr.debuffs[choice.index];
                            if (!string.IsNullOrEmpty(text))
                            {
                                unlockedText = text;
                            }
                        }
                        catch { }
                        break;
                }
                
                // 关键修复：设置 BoardTag 标志，使游戏识别并应用词条效果
                // 这与 Modified-Plus 的处理方式一致
                try
                {
                    if (Board.Instance != null && GameAPP.board != null)
                    {
                        var board = GameAPP.board.GetComponent<Board>();
                        if (board != null)
                        {
                            var boardTag = board.boardTag;
                            boardTag.isTravel = true;
                            boardTag.enableTravelBuff = true;
                            Board.Instance.boardTag = boardTag;
                            Debug.Log("[HeiTa] 已设置 BoardTag 标志，词条效果应该会生效");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[HeiTa] 设置 BoardTag 失败: {ex.Message}\n{ex.StackTrace}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HeiTa] 设置词条状态失败: {ex.Message}\n{ex.StackTrace}");
            }

            // 把抽到的词条文本显示出来（如果没取到文本，就用一个简单的提示）
            try
            {
                if (InGameText.Instance != null)
                {
                    string msg = unlockedText is not null
                        ? $"抽到词条：{unlockedText}"
                        : "黑塔：抽到一个旅行词条";
                    InGameText.Instance.ShowText(msg, 4f);
                }
            }
            catch
            {
                // 显示文本失败不影响主逻辑
            }
        }

        #endregion

        #region 死亡时全场冻结逻辑

        /// <summary>
        /// 黑塔死亡时：为全场僵尸附加 1000 冻结值
        /// </summary>
        [HideFromIl2Cpp]
        public static void FreezeAllZombiesOnDeath()
        {
            if (Board.Instance == null || Board.Instance.zombieArray == null)
            {
                return;
            }

            var zombies = Board.Instance.zombieArray;
            int count = zombies.Count;

            for (int i = 0; i < count; i++)
            {
                var zombie = zombies[i];
                if (zombie == null || zombie.beforeDying || zombie.isMindControlled)
                {
                    continue;
                }

                // 只增加冻结值，具体减速/停滞效果沿用游戏原机制
                zombie.AddfreezeLevel(1000);
            }
        }

        #endregion

        #region Harmony 补丁

        /// <summary>
        /// 保护性补丁：卡槽/图鉴里绑定了新脚本但没有在局内，Plant.PlantUpdate 会空指针。
        /// 对黑塔实例在以下条件下直接跳过原始 Update：
        /// - 游戏主实例或棋盘未创建
        /// - 该植物未绑定棋盘或 Transform
        /// 这样能避免卡槽预览阶段的 NullReference。
        /// </summary>
        [HarmonyPatch(typeof(Plant), nameof(Plant.PlantUpdate))]
        public static class HeiTa_PlantUpdateSafePatch
        {
            [HarmonyPrefix]
            public static bool Prefix(Plant __instance)
            {
                try
                {
                    if (__instance == null)
                    {
                        return false;
                    }

                    if (__instance.thePlantType != (PlantType)PlantID)
                    {
                        return true; // 只拦截黑塔
                    }

                    // 不在局内或关键引用未就绪时跳过 Update，避免 NRE
                    if (GameAPP.gameAPP == null || GameAPP.board == null)
                    {
                        return false;
                    }

                    if (__instance.board == null || __instance.axis == null)
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[HeiTa] PlantUpdate 预检异常: {ex.Message}\n{ex.StackTrace}");
                    // 出现异常时不拦截原逻辑，避免影响正常局内流程
                    return true;
                }

                return true;
            }
        }

        /// <summary>
        /// 在 Plant.Die 调用之后执行：
        /// - 只对 ID 为 4000 的黑塔生效
        /// - 触发一次开出旅行词条
        /// - 同时给全场僵尸附加 1000 冻结值
        /// </summary>
        [HarmonyPatch(typeof(Plant), nameof(Plant.Die))]
        public static class HeiTa_PlantDiePatch
        {
            [HarmonyPostfix]
            public static void Postfix(Plant __instance, Plant.DieReason reason)
            {
                if (__instance == null)
                {
                    return;
                }

                if (__instance.thePlantType != (PlantType)PlantID)
                {
                    return;
                }

                try
                {
                    // 开出旅行词条
                    OpenRandomTravelEntry();

                    // 全场僵尸冻结
                    FreezeAllZombiesOnDeath();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[HeiTa] 在 Plant.Die 后处理黑塔效果时发生错误: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        /// <summary>
        /// 屏蔽黑塔的钻石模仿者原版爆炸逻辑，只保留抽词条 + 冻结（通过 Die 补丁实现）。
        /// </summary>
        [HarmonyPatch(typeof(DiamondImitater), nameof(DiamondImitater.AnimExplode))]
        public static class HeiTa_DiamondImitater_AnimExplodePatch
        {
            [HarmonyPrefix]
            public static bool Prefix(DiamondImitater __instance)
            {
                try
                {
                    if (__instance == null)
                    {
                        return true;
                    }

                    // 只拦截黑塔对应的钻石模仿者实例，其它钻石模仿者维持原有行为
                    if (__instance.thePlantType != (PlantType)HeiTaPlant.PlantID)
                    {
                        return true;
                    }

                    // 直接让植物死亡，后续逻辑交给 Plant.Die 的补丁处理
                    __instance.Die();
                    return false; // 跳过原始 AnimExplode（不会再召唤随机僵尸 / 植物 / 事件）
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[HeiTa] DiamondImitater.AnimExplode 补丁出错: {ex.Message}\n{ex.StackTrace}");
                    return true; // 出错时回退到原逻辑，避免游戏崩溃
                }
            }
        }

        /// <summary>
        /// 若黑塔 prefab 使用的是 Imitater 逻辑，同样屏蔽其原始爆炸逻辑。
        /// </summary>
        [HarmonyPatch(typeof(Imitater), nameof(Imitater.AnimExplode))]
        public static class HeiTa_Imitater_AnimExplodePatch
        {
            [HarmonyPrefix]
            public static bool Prefix(Imitater __instance)
            {
                try
                {
                    if (__instance == null)
                    {
                        return true;
                    }

                    if (__instance.thePlantType != (PlantType)HeiTaPlant.PlantID)
                    {
                        return true;
                    }

                    __instance.Die();
                    return false;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[HeiTa] Imitater.AnimExplode 补丁出错: {ex.Message}\n{ex.StackTrace}");
                    return true;
                }
            }
        }

        #endregion
    }
}


