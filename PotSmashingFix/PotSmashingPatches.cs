using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace PotSmashingFix
{
    /// <summary>
    /// 核心砸罐子修复补丁
    /// </summary>
    [HarmonyPatch]
    public class PotSmashingPatches
    {
        private static readonly HashSet<ScaryPot> _hitPotsInFrame = new HashSet<ScaryPot>();

        // 补丁 ScaryPot$$Hitted 方法，用于处理多个罐子重叠时只砸开第一个罐子
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ScaryPot), nameof(ScaryPot.Hitted))]
        public static bool Prefix_ScaryPotHitted(ScaryPot __instance)
        {
            if (_hitPotsInFrame.Contains(__instance))
            {
                return false; // 阻止原始方法执行
            }

            Vector3 currentPotPosition = __instance.transform.position;

            List<ScaryPot> overlappingPots = new List<ScaryPot>();
            foreach (var plant in Board.Instance.plantArray)
            {
                if (plant != null && plant.gameObject.activeInHierarchy && TypeMgr.IsPot(plant.thePlantType))
                {
                    var pot = plant.GetComponent<ScaryPot>();
                    if (pot != null && IsPotsOverlapping(pot, __instance))
                    {
                        overlappingPots.Add(pot);
                    }
                }
            }
            overlappingPots = overlappingPots.OrderBy(pot => pot.GetInstanceID()).ToList();

            if (overlappingPots.Count > 1)
            {
                ScaryPot firstPot = overlappingPots.First();
                if (firstPot != __instance)
                {
                    return false; // 如果当前罐子不是第一个，则阻止其被砸开
                }
            }

            _hitPotsInFrame.Add(__instance);
            return true;
        }

        /// <summary>
        /// 拦截 ScaryPot.OnHitted 方法，实现特殊攻击保护
        /// </summary>
        /// <param name="__instance">ScaryPot 实例</param>
        /// <returns>是否允许执行原方法</returns>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ScaryPot), nameof(ScaryPot.OnHitted))]
        public static bool Prefix_ScaryPotOnHitted(ScaryPot __instance)
        {
            try
            {
                // 检查是否为特殊攻击（小丑爆炸、巨人砸击等）
                if (IsSpecialAttack())
                {
                    return false; // 阻止特殊攻击破坏罐子
                }

                // 额外检查：直接检查调用栈中是否有小丑爆炸相关的方法
                if (IsJackboxExplosionInStack())
                {
                    return false; // 阻止小丑爆炸破坏罐子
                }

                // 检查是否正在处理小丑爆炸
                if (JackboxZombieProtectionPatches.IsProcessingJackboxExplosion())
                {
                    return false; // 阻止小丑爆炸破坏罐子
                }

                return true; // 允许正常攻击破坏
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"PotSmashingFix: ScaryPot.OnHitted 补丁执行失败: {ex.Message}");
                return true; // 出错时允许正常执行
            }
        }

        /// <summary>
        /// 检查两个罐子是否重叠
        /// </summary>
        /// <param name="pot1">第一个罐子</param>
        /// <param name="pot2">第二个罐子</param>
        /// <returns>是否重叠</returns>
        private static bool IsPotsOverlapping(ScaryPot pot1, ScaryPot pot2)
        {
            if (pot1 == null || pot2 == null) return false;

            Vector3 pos1 = pot1.transform.position;
            Vector3 pos2 = pot2.transform.position;

            // 检查位置是否相近（在同一个格子内）
            float distance = Vector3.Distance(pos1, pos2);
            float overlapThreshold = 2.0f; // 2个格子的距离

            return distance < overlapThreshold;
        }

        /// <summary>
        /// 检查调用栈中是否有小丑爆炸相关的方法
        /// </summary>
        /// <returns>是否有小丑爆炸</returns>
        private static bool IsJackboxExplosionInStack()
        {
            try
            {
                // 获取当前调用栈
                System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
                
                // 检查调用栈中是否包含小丑爆炸相关的方法
                for (int i = 0; i < stackTrace.FrameCount; i++)
                {
                    var frame = stackTrace.GetFrame(i);
                    var method = frame?.GetMethod();
                    var methodName = method?.Name ?? "";
                    var className = method?.DeclaringType?.Name ?? "";

                    // 检查是否为小丑爆炸相关
                    if ((methodName.Contains("Explode") || methodName.Contains("AnimExplode")) && 
                        (className.Contains("Jackbox") || className.Contains("Jester") || className.Contains("Clown")))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"PotSmashingFix: 检查小丑爆炸调用栈时出错: {ex.Message}");
                return false; // 出错时默认没有小丑爆炸
            }
        }

        /// <summary>
        /// 检查当前是否为特殊攻击
        /// 通过分析调用栈来判断攻击类型
        /// </summary>
        /// <returns>是否为特殊攻击</returns>
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
                    var className = method?.DeclaringType?.Name ?? "";

                    // 检查是否为小丑爆炸相关 - 更精确的检测
                    if (methodName.Contains("Explode") && 
                        (className.Contains("Jackbox") || className.Contains("Jester") || className.Contains("Clown")))
                    {
                        return true;
                    }

                    // 检查是否为小丑爆炸动画相关
                    if (methodName.Contains("AnimExplode") && 
                        (className.Contains("Jackbox") || className.Contains("Jester") || className.Contains("Clown")))
                    {
                        return true;
                    }

                    // 检查是否为巨人砸击相关
                    if (methodName.Contains("Gargantuar") || 
                        methodName.Contains("Crash") ||
                        methodName.Contains("Smash") ||
                        methodName.Contains("AnimCrash"))
                    {
                        return true;
                    }

                    // 检查是否为其他特殊攻击
                    if (methodName.Contains("AoeDamage") ||
                        methodName.Contains("BigBomb") ||
                        methodName.Contains("BombPotato") ||
                        methodName.Contains("SmallBombPotato"))
                    {
                        return true;
                    }

                    // 检查是否为炸弹相关攻击
                    if (methodName.Contains("Bomb") && 
                        (className.Contains("AoeDamage") || className.Contains("Explosion")))
                    {
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

        // 在每帧结束时清除已处理的罐子列表
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Board), nameof(Board.Update))] // 假设Board.Update每帧执行
        public static void Postfix_BoardUpdate()
        {
            _hitPotsInFrame.Clear();
        }
    }

    /// <summary>
    /// 巨人僵尸忽略罐子补丁
    /// </summary>
    [HarmonyPatch]
    public class GargantuarIgnorePotPatches
    {
        /// <summary>
        /// 拦截 IronGargantuar.OnTriggerEnter2D 方法，让巨人僵尸忽略罐子
        /// </summary>
        /// <param name="__instance">IronGargantuar 实例</param>
        /// <param name="collision">碰撞对象</param>
        /// <returns>是否允许执行原方法</returns>
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
                    return false; // 忽略与罐子的碰撞
                }

                return true; // 允许其他碰撞正常处理
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"PotSmashingFix: IronGargantuar.OnTriggerEnter2D 补丁执行失败: {ex.Message}");
                return true; // 出错时允许正常执行
            }
        }

        /// <summary>
        /// 拦截 Gargantuar.AttackUpdate 方法，让巨人僵尸忽略罐子攻击
        /// </summary>
        /// <param name="__instance">Gargantuar 实例</param>
        /// <returns>是否允许执行原方法</returns>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Gargantuar), nameof(Gargantuar.AttackUpdate))]
        public static bool Prefix_GargantuarAttackUpdate(Gargantuar __instance)
        {
            try
            {
                // 检查巨人僵尸是否正在攻击罐子
                if (IsGargantuarAttackingPot(__instance))
                {
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

        /// <summary>
        /// 检查巨人僵尸是否正在攻击罐子
        /// </summary>
        /// <param name="gargantuar">巨人僵尸实例</param>
        /// <returns>是否正在攻击罐子</returns>
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

    /// <summary>
    /// 小丑僵尸爆炸保护补丁 - 让小丑可以爆炸，但爆炸不影响罐子
    /// </summary>
    [HarmonyPatch]
    public class JackboxZombieProtectionPatches
    {
        // 标记当前是否正在处理小丑爆炸
        private static bool _isProcessingJackboxExplosion = false;

        /// <summary>
        /// 拦截 JackboxZombie.Explode 方法，让小丑可以爆炸，但爆炸不影响罐子
        /// </summary>
        /// <param name="__instance">JackboxZombie 实例</param>
        /// <returns>是否允许执行原方法</returns>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(JackboxZombie), nameof(JackboxZombie.Explode))]
        public static bool Prefix_JackboxZombieExplode(JackboxZombie __instance)
        {
            try
            {
                _isProcessingJackboxExplosion = true;
                return true; // 允许小丑僵尸正常爆炸
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"PotSmashingFix: JackboxZombie.Explode 补丁执行失败: {ex.Message}");
                return true; // 出错时允许正常执行
            }
        }

        /// <summary>
        /// 拦截 JackboxZombie.Explode 方法的后置处理
        /// </summary>
        /// <param name="__instance">JackboxZombie 实例</param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(JackboxZombie), nameof(JackboxZombie.Explode))]
        public static void Postfix_JackboxZombieExplode(JackboxZombie __instance)
        {
            try
            {
                _isProcessingJackboxExplosion = false;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"PotSmashingFix: JackboxZombie.Explode 后置补丁执行失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 拦截 JackboxZombie.AnimExplode 方法，让小丑可以爆炸，但爆炸不影响罐子
        /// </summary>
        /// <param name="__instance">JackboxZombie 实例</param>
        /// <returns>是否允许执行原方法</returns>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(JackboxZombie), nameof(JackboxZombie.AnimExplode))]
        public static bool Prefix_JackboxZombieAnimExplode(JackboxZombie __instance)
        {
            try
            {
                _isProcessingJackboxExplosion = true;
                return true; // 允许小丑僵尸正常爆炸动画
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"PotSmashingFix: JackboxZombie.AnimExplode 补丁执行失败: {ex.Message}");
                return true; // 出错时允许正常执行
            }
        }

        /// <summary>
        /// 拦截 JackboxZombie.AnimExplode 方法的后置处理
        /// </summary>
        /// <param name="__instance">JackboxZombie 实例</param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(JackboxZombie), nameof(JackboxZombie.AnimExplode))]
        public static void Postfix_JackboxZombieAnimExplode(JackboxZombie __instance)
        {
            try
            {
                _isProcessingJackboxExplosion = false;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"PotSmashingFix: JackboxZombie.AnimExplode 后置补丁执行失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 拦截 SuperJackboxZombie.AnimExplode 方法，让超级小丑可以爆炸，但爆炸不影响罐子
        /// </summary>
        /// <param name="__instance">SuperJackboxZombie 实例</param>
        /// <returns>是否允许执行原方法</returns>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SuperJackboxZombie), nameof(SuperJackboxZombie.AnimExplode))]
        public static bool Prefix_SuperJackboxZombieAnimExplode(SuperJackboxZombie __instance)
        {
            try
            {
                _isProcessingJackboxExplosion = true;
                return true; // 允许超级小丑僵尸正常爆炸
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"PotSmashingFix: SuperJackboxZombie.AnimExplode 补丁执行失败: {ex.Message}");
                return true; // 出错时允许正常执行
            }
        }

        /// <summary>
        /// 拦截 SuperJackboxZombie.AnimExplode 方法的后置处理
        /// </summary>
        /// <param name="__instance">SuperJackboxZombie 实例</param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SuperJackboxZombie), nameof(SuperJackboxZombie.AnimExplode))]
        public static void Postfix_SuperJackboxZombieAnimExplode(SuperJackboxZombie __instance)
        {
            try
            {
                _isProcessingJackboxExplosion = false;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"PotSmashingFix: SuperJackboxZombie.AnimExplode 后置补丁执行失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查是否正在处理小丑爆炸
        /// </summary>
        /// <returns>是否正在处理小丑爆炸</returns>
        public static bool IsProcessingJackboxExplosion()
        {
            return _isProcessingJackboxExplosion;
        }
    }
}