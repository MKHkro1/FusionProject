using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace UltimateGoldJackBoxZombieMod
{
    [HarmonyPatch]
    public class UltimateGoldJackBoxPatch
    {
        [HarmonyPatch(typeof(Zombie), "UpdateHealthText")]
        [HarmonyPostfix]
        public static void UpdateHealthText_Postfix(Zombie __instance)
        {
            if (__instance.theZombieType == Plugin.theNewZombieType)
            {
                UltimateGoldJackBox? component = __instance.gameObject.GetComponent<UltimateGoldJackBox>();
                if (component == null) return;
                TextMeshPro? healthText = component.zombie.healthText;
                TextMeshPro? healthTextShadow = component.zombie.healthTextShadow;
                if (healthText == null) return;
                if (component.zombie.theMaxHealth <= 0) return;
                int num = component.zombie.theHealth - 18003;
                if (num <= 0) num = 0;
                string text = $"{num}/{component.zombie.theMaxHealth - 18003}";
                healthText.text = text;
                if (healthTextShadow != null)
                {
                    healthTextShadow.text = text;
                }
            }
        }

        private static void FixSorting(Component comp)
        {
            if (comp == null) return;
            SortingGroup? sortingGroup = comp.GetComponent<SortingGroup>();
            if (sortingGroup != null)
            {
                sortingGroup.sortingLayerName = "Default";
                sortingGroup.sortingOrder += 90000;
                sortingGroup.sortAtRoot = true;
            }
        }

        [HarmonyPatch(typeof(Zombie), "InitHealth")]
        [HarmonyPostfix]
        public static void InitHealth_Postfix(Zombie __instance)
        {
            if (__instance.theZombieType == Plugin.theNewZombieType)
            {
                UltimateGoldJackBox? component = __instance.gameObject.GetComponent<UltimateGoldJackBox>();
                if (component == null) return;
                TextMeshPro? healthText = component.zombie.healthText;
                TextMeshPro? healthTextShadow = component.zombie.healthTextShadow;
                if (healthText == null) return;
                string text = $"{component.zombie.theHealth - 18003}/{component.zombie.theMaxHealth - 18003}";
                // 金色 RGB(255, 215, 0) 或 (1, 0.84, 0)
                healthText.color = new Color(1f, 0.84f, 0f);
                healthText.fontSize = 3f; // 缩小字体大小
                healthText.text = text;
                if (healthTextShadow != null)
                {
                    healthTextShadow.fontSize = 3f;
                    healthTextShadow.text = text;
                }
            }
        }

        [HarmonyPatch(typeof(Zombie), "OnDestroy")]
        [HarmonyPostfix]
        public static void OnDestroy_Postfix(Zombie __instance)
        {
            if (__instance.theZombieType == Plugin.theNewZombieType)
            {
                UltimateGoldJackBox? component = __instance.gameObject.GetComponent<UltimateGoldJackBox>();
                if (component == null || component.zombie == null) return;
                JumpDataStore.Remove(component.zombie);
                Plugin.goldManager.removeStateRecord(component.zombie);
            }
        }

        [HarmonyPatch(typeof(CreateZombie), "SetZombie")]
        [HarmonyPrefix]
        public static bool SetZombie_Prefix(Zombie __instance, int theRow, ref ZombieType theZombieType, float theX, bool isIdle)
        {
            return true;
        }

        [HarmonyPatch(typeof(Zombie), "Die")]
        [HarmonyPrefix]
        public static bool Die_Prefix(Zombie __instance, int reason)
        {
            Plugin.goldManager.RemoveRecord(__instance);
            if (__instance.theZombieType == Plugin.theNewZombieType)
            {
                UltimateGoldJackBox? component = __instance.gameObject.GetComponent<UltimateGoldJackBox>();
                if (component == null) return true;
                bool jumperStateRecord = Plugin.goldManager.GetJumperStateRecord(component.zombie);

                // reason == 2: 被吃掉
                if (reason == 2)
                {
                    if (!jumperStateRecord)
                    {
                        // 未下跳杆时，检查是否已经使用过复活机会
                        if (component.hasUsedNoJumperRevive)
                        {
                            // 已经复活过，不再复活
                            Plugin.goldManager.Clear();
                            return true;
                        }
                        List<ZombieDieRecord> topRecordsAroundPosition = Plugin.zombieRecordManager.GetTopRecordsAroundPosition(__instance.theZombieRow, __instance.Column, true, 5);
                        if (topRecordsAroundPosition.Count > 0)
                        {
                            Plugin.zombieEvent(component.zombie, topRecordsAroundPosition, "");
                        }
                        Plugin.goldManager.Clear();
                        Plugin.TeleportPosition(component.zombie, 0); // 传0，新僵尸会标记为已使用复活
                    }
                    return true;
                }

                // reason == 3: 真正死亡（不复活）
                if (reason == 3)
                {
                    if (__instance.anim != null)
                    {
                        __instance.anim.SetTrigger("GoDie");
                    }
                    Plugin.goldManager.Clear();
                    UnityEngine.Object.Destroy(component.gameObject);
                    UnityEngine.Object.Destroy(__instance);
                    return false;
                }

                // 其他死亡原因
                if (!jumperStateRecord)
                {
                    // 未下跳杆时，检查是否已经使用过复活机会
                    if (component.hasUsedNoJumperRevive)
                    {
                        // 已经复活过，不再复活
                        Plugin.goldManager.Clear();
                        return true;
                    }
                    Plugin.goldManager.Clear();
                    Plugin.TeleportPosition(component.zombie, 0); // 传0，新僵尸会标记为已使用复活
                    return true;
                }

                // 下跳杆后，使用lastCount控制复活次数
                if (component.ReduceJackBoxCount())
                {
                    Plugin.goldManager.Clear();
                    Plugin.TeleportPosition(component.zombie, component.lastCount);
                }
                else
                {
                    // lastCount为0，不能复活，执行真正死亡
                    List<ZombieDieRecord> topRecordsAroundPosition2 = Plugin.zombieRecordManager.GetTopRecordsAroundPosition(component.zombie.theZombieRow, __instance.Column, true, 5);
                    Plugin.zombieEvent(component.zombie, topRecordsAroundPosition2, "");
                    component.zombie.Die(3);
                }
                return true;
            }
            else
            {
                if ((int)__instance.theZombieType == 44 || (int)__instance.theZombieType == 46)
                {
                    return true;
                }
                ZombieDieRecord newRecord = new ZombieDieRecord(__instance.theZombieType, 0f, __instance.TotalFirstHealth, __instance.theZombieRow, Lawnf.GetColumnFromX(__instance.transform.position.x));
                Plugin.zombieRecordManager.AddRecord(newRecord);
                return true;
            }
        }

        [HarmonyPatch(typeof(TypeMgr), "IsGargantuar")]
        [HarmonyPostfix]
        public static void IsGargantuar_Postfix(ref ZombieType theZombieType, ref bool __result)
        {
            if (theZombieType == Plugin.theNewZombieType)
            {
                __result = true;
            }
        }

        [HarmonyPatch(typeof(TypeMgr), "UltimateZombie")]
        [HarmonyPostfix]
        private static void UltimateZombiePostfix(ref ZombieType theZombieType, ref bool __result)
        {
            if (theZombieType == Plugin.theNewZombieType)
            {
                __result = true;
            }
        }

        /// <summary>
        /// 将究极黄金玩偶匣跳跳王标记为领袖僵尸
        /// </summary>
        [HarmonyPatch(typeof(TypeMgr), "IsLeaderZombie")]
        [HarmonyPostfix]
        public static void IsLeaderZombie_Postfix(ref ZombieType theZombieType, ref bool __result)
        {
            if (theZombieType == Plugin.theNewZombieType)
            {
                __result = true;
            }
        }

        /// <summary>
        /// 将究极黄金玩偶匣跳跳王标记为Boss僵尸
        /// </summary>
        [HarmonyPatch(typeof(TypeMgr), "IsBossZombie")]
        [HarmonyPostfix]
        public static void IsBossZombie_Postfix(ref ZombieType theZombieType, ref bool __result)
        {
            if (theZombieType == Plugin.theNewZombieType)
            {
                __result = true;
            }
        }

        [HarmonyPatch(typeof(Zombie), "BodyTakeDamage")]
        [HarmonyPrefix]
        private static void BodyTakeDamage_Prefix(Zombie __instance, ref int theDamage)
        {
            if (__instance.theZombieType != Plugin.theNewZombieType)
            {
                HashSet<Zombie>? records = Plugin.goldManager.GetRecords();
                if (records == null || records.Count <= 0) return;
                foreach (Zombie zombie in records)
                {
                    if (zombie == __instance && !zombie.beforeDying)
                    {
                        theDamage = (int)((float)theDamage * 0.5f);
                        if (theDamage > 3000) theDamage = 3000;
                    }
                }
                return;
            }
            UltimateGoldJackBox? component = __instance.gameObject.GetComponent<UltimateGoldJackBox>();
            if (Plugin.goldManager.GetJumperStateRecord(component!.zombie))
            {
                theDamage = (int)((float)theDamage * 0.3f);
                if (theDamage > 3000) theDamage = 3000;
            }
            else
            {
                theDamage = (int)((float)theDamage * 0.6f);
                if (theDamage > 5000) theDamage = 5000;
            }
        }

        [HarmonyPatch(typeof(Jackbox_a), "LoseJumper")]
        [HarmonyPrefix]
        public static bool LoseJumper_Prefix(Zombie __instance)
        {
            if (__instance.theZombieType == Plugin.theNewZombieType)
            {
                bool isUltimate = true;
                int column = __instance.Column;
                List<ZombieDieRecord> topRecordsAroundPosition = Plugin.zombieRecordManager.GetTopRecordsAroundPosition(__instance.theZombieRow, column, isUltimate, 5);
                float num = 0f;
                foreach (ZombieDieRecord record in topRecordsAroundPosition)
                {
                    num += record.health;
                }
                float num2 = (float)UnityEngine.Random.Range(15000, 54001);
                int num3 = UnityEngine.Random.Range(3, 6); // 3~5个僵尸
                float healthInTravel = Plugin.getHealthInTravel();
                float num4 = num2 * healthInTravel;
                if (topRecordsAroundPosition.Count < num3 || num < num4)
                {
                    return false;
                }
                UltimateGoldJackBox? component = __instance.GetComponent<UltimateGoldJackBox>();
                Plugin.zombieEvent(__instance, topRecordsAroundPosition, "LoseJumper");
                if (component != null && component.zombie != null)
                {
                    Vector3 position = component.zombie.axis.position;
                    Lawnf.ZombieExplode(new Vector2(position.x, position.y + 0.6f), __instance.board, __instance.isMindControlled, __instance.theZombieRow, (Plant.DamageType)2);
                    component.zombie.theHealth = component.zombie.theMaxHealth;
                    GoldBoxStateRecord record2 = new GoldBoxStateRecord();
                    record2.isLoseJumper = true;
                    Plugin.goldManager.AddOrUpdatetStateRecord(component.zombie, record2);
                    component.lastCount = 0;
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(Zombie), "TakeDamage")]
        [HarmonyPrefix]
        public static bool TakeDamage_Prefix(Zombie __instance, DmgType theDamageType, ref int theDamage, bool fix)
        {
            if (__instance.theZombieType == Plugin.theNewZombieType)
            {
                UltimateGoldJackBox? component = __instance.gameObject.GetComponent<UltimateGoldJackBox>();
                if (Plugin.goldManager.GetJumperStateRecord(component!.zombie))
                {
                    theDamage = (int)((float)theDamage * 0.3f);
                    if (theDamage > 3000) theDamage = 3000;
                }
                else
                {
                    theDamage = (int)((float)theDamage * 0.6f);
                    if (theDamage > 5000) theDamage = 5000;
                }
            }
            else
            {
                HashSet<Zombie>? records = Plugin.goldManager.GetRecords();
                if (records != null && records.Count > 0)
                {
                    foreach (Zombie zombie in records)
                    {
                        if (zombie == __instance && !zombie.beforeDying)
                        {
                            theDamage = (int)((float)theDamage * 0.5f);
                            if (theDamage > 3000) theDamage = 3000;
                        }
                    }
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(Magnetshroom), "TryAttrackZombie")]
        [HarmonyPrefix]
        public static bool TryAttrackZombie_Prefix(Magnetshroom __instance, Zombie zombie, ref bool __result)
        {
            if (zombie.theZombieType == Plugin.theNewZombieType)
            {
                __result = false;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Zombie), "KnockBack")]
        [HarmonyPrefix]
        public static bool KnockBack_Prefix(Zombie __instance, float x, Zombie.KnockBackReason reason)
        {
            try
            {
                if (__instance.theZombieType == Plugin.theNewZombieType)
                {
                    __instance.gameObject.GetComponent<UltimateGoldJackBox>();
                    return false;
                }
                HashSet<Zombie>? records = Plugin.goldManager.GetRecords();
                if (records != null && records.Count > 0)
                {
                    foreach (Zombie z in records)
                    {
                        if (z == __instance) return false;
                    }
                }
            }
            catch (Exception) { }
            return true;
        }

        [HarmonyPatch(typeof(Zombie), "BeSmall")]
        [HarmonyPrefix]
        public static bool Prefix(Zombie __instance, float scale)
        {
            return __instance.theZombieType != Plugin.theNewZombieType;
        }

        [HarmonyPatch(typeof(CreateZombie), "SetZombieWithMindControl")]
        [HarmonyPrefix]
        public static bool CreateZombie_Prefix(CreateZombie __instance, int theRow, ZombieType theZombieType, float theX, bool withEffect)
        {
            if (theZombieType == Plugin.theNewZombieType) return false;
            bool flag = true;
            if (Plugin.goldManager.StateRecordCount() > 0) flag = false;
            return flag || ((int)theZombieType != 32 && (int)theZombieType != 34 && (int)theZombieType != 324 && (int)theZombieType != 325 && (int)theZombieType != 326);
        }

        [HarmonyPatch(typeof(Zombie), "JalaedExplode")]
        [HarmonyPostfix]
        public static void JalaedExplode_Postfix(Zombie __instance, bool jala, int damage)
        {
            ZombieType theZombieType = __instance.theZombieType;
            ZombieType theNewZombieType = Plugin.theNewZombieType;
        }

        [HarmonyPatch(typeof(Zombie), "SetFreeze")]
        [HarmonyPrefix]
        public static bool SetFreeze_Prefix(Zombie __instance, float time, int theFreezeLevel)
        {
            return __instance.theZombieType != Plugin.theNewZombieType;
        }

        [HarmonyPatch(typeof(Zombie), "AddfreezeLevel")]
        [HarmonyPrefix]
        public static bool AddfreezeLevel_Prefix(Zombie __instance, int level)
        {
            return __instance.theZombieType != Plugin.theNewZombieType;
        }

        [HarmonyPatch(typeof(Zombie), "SetCold")]
        [HarmonyPrefix]
        public static bool SetCold_Prefix(Zombie __instance, float time)
        {
            return __instance.theZombieType != Plugin.theNewZombieType;
        }

        [HarmonyPatch(typeof(Zombie), "Buttered")]
        [HarmonyPrefix]
        public static bool Buttered_Prefix(Zombie __instance, float time, bool sprite)
        {
            if (__instance.theZombieType == Plugin.theNewZombieType) return false;
            HashSet<Zombie>? records = Plugin.goldManager.GetRecords();
            if (records != null && records.Count > 0)
            {
                foreach (Zombie z in records)
                {
                    if (z == __instance) return false;
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(Zombie), "SetPortaled")]
        [HarmonyPrefix]
        public static bool SetPortaled_Prefix(Zombie __instance, float timer)
        {
            if (__instance.theZombieType == Plugin.theNewZombieType) return false;
            HashSet<Zombie>? records = Plugin.goldManager.GetRecords();
            if (records != null && records.Count > 0)
            {
                foreach (Zombie z in records)
                {
                    if (z == __instance) return false;
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(Zombie), "SetPoison")]
        [HarmonyPrefix]
        public static bool SetPoison_Prefix(Zombie __instance)
        {
            return __instance.theZombieType != Plugin.theNewZombieType;
        }

        [HarmonyPatch(typeof(Zombie), "SetEmbered")]
        [HarmonyPrefix]
        public static bool SetEmbered_Prefix(Zombie __instance)
        {
            return __instance.theZombieType != Plugin.theNewZombieType;
        }

        [HarmonyPatch(typeof(Zombie), "SetGrap")]
        [HarmonyPrefix]
        public static bool SetGrap_Prefix(Zombie __instance, float time, bool land)
        {
            return __instance.theZombieType != Plugin.theNewZombieType;
        }

        [HarmonyPatch(typeof(Zombie), "SetMindControl")]
        [HarmonyPrefix]
        public static bool SetMindControl_Prefix(Zombie __instance, int controlLevel)
        {
            if (__instance.theZombieType == Plugin.theNewZombieType) return false;
            bool flag = true;
            if (Plugin.goldManager.StateRecordCount() > 0) flag = false;
            return flag || ((int)__instance.theZombieType != 32 && (int)__instance.theZombieType != 34 && (int)__instance.theZombieType != 324 && (int)__instance.theZombieType != 325 && (int)__instance.theZombieType != 326);
        }

        [HarmonyPatch(typeof(ArmedGargantuar), "SetJalaed")]
        [HarmonyPrefix]
        public static bool SetJalaed_Prefix(Zombie __instance)
        {
            return __instance.theZombieType != Plugin.theNewZombieType;
        }

        [HarmonyPatch(typeof(UltimateChomper), "Bite")]
        [HarmonyPrefix]
        public static bool Bite_Prefix(UltimateChomper __instance, Zombie zombie)
        {
            return zombie.theZombieType != Plugin.theNewZombieType;
        }
    }

    /// <summary>
    /// 伴生机制 - 玩偶匣跳跳王(325)出现时，5%概率伴生究极黄金玩偶匣跳跳王
    /// </summary>
    [HarmonyPatch(typeof(Zombie))]
    public static class UltimateGoldJackBoxSpawnPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void PostStart(Zombie __instance)
        {
            try
            {
                // 当玩偶匣跳跳王（ID：325）出现时，有5%概率伴随出现究极黄金玩偶匣跳跳王
                if ((int)__instance.theZombieType == 325 && GameAPP.theGameStatus == GameStatus.InGame)
                {
                    // 5%概率伴随出现
                    if (UnityEngine.Random.Range(0f, 1f) < 0.05f)
                    {
                        Plugin.Log?.LogInfo("究极金丑插件: 玩偶匣跳跳王出现，概率伴随生成究极黄金玩偶匣跳跳王");
                        
                        CreateZombie.Instance.SetZombie(__instance.theZombieRow, Plugin.theNewZombieType, __instance.transform.position.x);
                        
                        // 播放生成特效
                        Vector3 position = __instance.transform.position;
                        UnityEngine.Object.Instantiate(GameAPP.particlePrefab[11], new Vector3(position.x, position.y + 1f, 0f), Quaternion.identity).transform.SetParent(GameAPP.board.transform);
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"究极金丑插件: 伴生机制失败: {ex.Message}");
            }
        }
    }
}
