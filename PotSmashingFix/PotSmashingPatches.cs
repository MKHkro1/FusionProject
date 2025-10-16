using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;

namespace PotSmashingFix
{
    [HarmonyPatch]
    public class PotSmashingPatches
    {
        private static ScaryPot? lastHitPot = null;
        private static float lastHitTime = 0f;
        private static readonly float overlapProtectionTime = 0.1f; // 100毫秒内的重叠保护

        /// <param name="__instance">ScaryPot 实例</param>
        /// <returns>是否允许执行原方法</returns>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ScaryPot), nameof(ScaryPot.Hitted))]
        public static bool Prefix_ScaryPotHitted(ScaryPot __instance)
        {
            try
            {
                float currentTime = Time.time;
                
                if (lastHitPot != null && 
                    lastHitPot != __instance && 
                    currentTime - lastHitTime < overlapProtectionTime)
                {
                    if (IsPotsOverlapping(lastHitPot, __instance))
                    {
                        UnityEngine.Debug.Log($"PotSmashingFix: 阻止重叠罐子被砸开 - 位置: {__instance.transform.position}");
                        return false; // 阻止重叠罐子被砸开
                    }
                }

                lastHitPot = __instance;
                lastHitTime = currentTime;
                
                UnityEngine.Debug.Log($"PotSmashingFix: 允许罐子被砸开 - 位置: {__instance.transform.position}");
                return true; // 允许正常砸开
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"PotSmashingFix: ScaryPot.Hitted 补丁执行失败: {ex.Message}");
                return true; // 出错时允许正常执行
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ScaryPot), nameof(ScaryPot.OnHitted))]
        public static bool Prefix_ScaryPotOnHitted(ScaryPot __instance)
        {
            try
            {
                // 检查是否为特殊攻击（小丑爆炸、巨人砸击等）
                if (IsSpecialAttack())
                {
                    UnityEngine.Debug.Log($"PotSmashingFix: 阻止特殊攻击破坏罐子 - 位置: {__instance.transform.position}");
                    return false; // 阻止特殊攻击破坏罐子
                }

                UnityEngine.Debug.Log($"PotSmashingFix: 允许罐子被正常攻击破坏 - 位置: {__instance.transform.position}");
                return true; // 允许正常攻击破坏
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"PotSmashingFix: ScaryPot.OnHitted 补丁执行失败: {ex.Message}");
                return true; // 出错时允许正常执行
            }
        }


        private static bool IsPotsOverlapping(ScaryPot pot1, ScaryPot pot2)
        {
            if (pot1 == null || pot2 == null) return false;

            Vector3 pos1 = pot1.transform.position;
            Vector3 pos2 = pot2.transform.position;

            // 检查位置是否相近（在同一个格子内）
            float distance = Vector3.Distance(pos1, pos2);
            float overlapThreshold = 0.5f; // 半个格子的距离

            return distance < overlapThreshold;
        }

        private static bool IsSpecialAttack()
        {
            try
            {
                // 获取当前调用栈
                System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
                
                // 检查调用栈中是否包含特殊攻击相关的方法
                for (int i = 0; i < stackTrace.FrameCount; i++)
                {
                    var frame = stackTrace.GetFrame(i);
                    var method = frame?.GetMethod();
                    var methodName = method?.Name ?? "";

                    // 检查是否为小丑爆炸相关
                    if (methodName.Contains("Jester") || 
                        methodName.Contains("Explode") ||
                        methodName.Contains("Bomb"))
                    {
                        UnityEngine.Debug.Log($"PotSmashingFix: 检测到小丑爆炸攻击 - 方法: {methodName}");
                        return true;
                    }

                    // 检查是否为巨人砸击相关
                    if (methodName.Contains("Gargantuar") || 
                        methodName.Contains("Crash") ||
                        methodName.Contains("Smash"))
                    {
                        UnityEngine.Debug.Log($"PotSmashingFix: 检测到巨人砸击攻击 - 方法: {methodName}");
                        return true;
                    }

                    // 检查是否为其他特殊攻击
                    if (methodName.Contains("AoeDamage") ||
                        methodName.Contains("BigBomb") ||
                        methodName.Contains("BombPotato"))
                    {
                        UnityEngine.Debug.Log($"PotSmashingFix: 检测到特殊AOE攻击 - 方法: {methodName}");
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"PotSmashingFix: 检查特殊攻击时出错: {ex.Message}");
                return false; // 出错时默认不是特殊攻击
            }
        }
    }

    [HarmonyPatch]
    public class GargantuarIgnorePotPatches
    {

        [HarmonyPrefix]
        [HarmonyPatch(typeof(IronGargantuar), nameof(IronGargantuar.OnTriggerEnter2D))]
        public static bool Prefix_IronGargantuarOnTriggerEnter2D(IronGargantuar __instance, UnityEngine.Collider2D collision)
        {
            try
            {
                if (collision == null) return true;

                // 检查碰撞对象是否为罐子
                var scaryPot = collision.GetComponent<ScaryPot>();
                if (scaryPot != null)
                {
                    UnityEngine.Debug.Log($"PotSmashingFix: 巨人僵尸忽略罐子碰撞 - 位置: {__instance.transform.position}");
                    return false; // 阻止巨人僵尸与罐子的碰撞处理
                }

                return true; // 允许其他碰撞正常处理
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"PotSmashingFix: IronGargantuar.OnTriggerEnter2D 补丁执行失败: {ex.Message}");
                return true; // 出错时允许正常执行
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Gargantuar), nameof(Gargantuar.AttackUpdate))]
        public static bool Prefix_GargantuarAttackUpdate(Gargantuar __instance)
        {
            try
            {
                // 检查巨人僵尸是否正在攻击罐子
                if (IsGargantuarAttackingPot(__instance))
                {
                    UnityEngine.Debug.Log($"PotSmashingFix: 阻止巨人僵尸攻击罐子 - 位置: {__instance.transform.position}");
                    return false; // 阻止巨人僵尸攻击罐子
                }

                return true; // 允许正常攻击其他目标
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"PotSmashingFix: Gargantuar.AttackUpdate 补丁执行失败: {ex.Message}");
                return true; // 出错时允许正常执行
            }
        }


        private static bool IsGargantuarAttackingPot(Gargantuar gargantuar)
        {
            try
            {
                // 简化逻辑：直接检查巨人僵尸附近是否有罐子
                // 如果巨人僵尸停止移动，很可能是在攻击罐子
                var zombie = gargantuar.GetComponent<Zombie>();
                if (zombie == null) return false;

                // 检查巨人僵尸是否停止移动（可能正在攻击罐子）
                var rigidbody = gargantuar.GetComponent<UnityEngine.Rigidbody2D>();
                if (rigidbody != null && rigidbody.velocity.magnitude < 0.1f)
                {
                    // 检查附近是否有罐子
                    var colliders = UnityEngine.Physics2D.OverlapCircleAll(
                        gargantuar.transform.position, 
                        1.0f); // 1格范围内的碰撞体

                    foreach (var collider in colliders)
                    {
                        if (collider.GetComponent<ScaryPot>() != null)
                        {
                            return true; // 附近有罐子，可能正在攻击
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"PotSmashingFix: 检查巨人僵尸攻击目标时出错: {ex.Message}");
                return false;
            }
        }
    }


    [HarmonyPatch]
    public class PotProtectionPatches
    {

        [HarmonyPrefix]
        [HarmonyPatch(typeof(AoeDamage), nameof(AoeDamage.BombPotato))]
        public static bool Prefix_AoeDamageBombPotato(AoeDamage __instance)
        {
            try
            {
                UnityEngine.Debug.Log("PotSmashingFix: 拦截土豆炸弹攻击，保护罐子");
                
                // 执行原方法但不影响罐子
                // 这里可以通过修改攻击范围或目标来保护罐子
                return true; // 允许执行，但会在 ScaryPot.OnHitted 中被拦截
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"PotSmashingFix: AoeDamage.BombPotato 补丁执行失败: {ex.Message}");
                return true;
            }
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(AoeDamage), nameof(AoeDamage.BigBomb))]
        public static bool Prefix_AoeDamageBigBomb(AoeDamage __instance)
        {
            try
            {
                UnityEngine.Debug.Log("PotSmashingFix: 拦截大炸弹攻击，保护罐子");
                return true; // 允许执行，但会在 ScaryPot.OnHitted 中被拦截
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"PotSmashingFix: AoeDamage.BigBomb 补丁执行失败: {ex.Message}");
                return true;
            }
        }
    }
}