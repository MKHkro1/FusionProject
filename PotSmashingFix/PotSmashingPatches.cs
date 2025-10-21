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
        // 跟踪当前锤击事件中已经砸开的罐子
        private static readonly HashSet<ScaryPot> _hitPotsInCurrentSwing = new HashSet<ScaryPot>();
        // 跟踪当前锤击事件中已经处理的罐子（包括被阻止的）
        private static readonly HashSet<ScaryPot> _processedPotsInCurrentSwing = new HashSet<ScaryPot>();
        // 跟踪通过ScaryPot.Hitted调用的罐子
        private static readonly HashSet<ScaryPot> _hittedPots = new HashSet<ScaryPot>();

        // 补丁 ScaryPot$$Hitted 方法，确保一次锤击只能敲爆一个罐子
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ScaryPot), nameof(ScaryPot.Hitted))]
        public static bool Prefix_ScaryPotHitted(ScaryPot __instance)
        {
            // 记录所有调用栈信息用于调试
            UnityEngine.Debug.Log($"PotSmashingFix: ScaryPot.Hitted 被调用，开始分析调用栈...");
            LogStackTrace();

            // 通用检测：检查调用栈中是否包含任何与ProjectileZombie、Submarine相关的内容
            if (IsAnyProjectileZombieRelatedInStack())
            {
                UnityEngine.Debug.Log($"PotSmashingFix: 阻止ProjectileZombie攻击破坏罐子 - 调用栈检测");
                return false; // 阻止ProjectileZombie攻击破坏罐子
            }

            // 检查是否为ProjectileZombie攻击（导弹机械舰艇和雷鸣机械潜艇的雷鸣炮弹）
            if (IsProjectileZombieAttackInStack() || IsBombingAttack() || IsAnyProjectileZombieRelatedAttack())
            {
                UnityEngine.Debug.Log($"PotSmashingFix: 阻止ProjectileZombie攻击破坏罐子 - 专门检测");
                return false; // 阻止ProjectileZombie攻击破坏罐子
            }

            // 检查当前罐子是否已经在当前锤击中被处理过
            if (_processedPotsInCurrentSwing.Contains(__instance))
            {
                return false; // 阻止原始方法执行
            }

            // 检查是否已经有罐子在当前锤击中被砸开
            if (_hitPotsInCurrentSwing.Count > 0)
            {
                // 已经有罐子被砸开，阻止当前罐子
                _processedPotsInCurrentSwing.Add(__instance);
                return false;
            }

            // 当前罐子可以被打砸，标记为已砸开和已处理
            _hitPotsInCurrentSwing.Add(__instance);
            _processedPotsInCurrentSwing.Add(__instance);
            
            // 标记这个罐子是通过Hitted调用的，允许后续的OnHitted调用
            _hittedPots.Add(__instance);
            
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
                // 记录所有调用栈信息用于调试
                UnityEngine.Debug.Log($"PotSmashingFix: ScaryPot.OnHitted 被调用，开始分析调用栈...");
                LogStackTrace();

                // 检查是否正在处理小丑爆炸
                if (JackboxZombieProtectionPatches.IsProcessingJackboxExplosion())
                {
                    UnityEngine.Debug.Log($"PotSmashingFix: 阻止小丑爆炸破坏罐子 - OnHitted");
                    return false; // 阻止小丑爆炸破坏罐子
                }

                // 检查是否是通过ScaryPot.Hitted调用的（允许鼠标点击路径）
                if (_hittedPots.Contains(__instance))
                {
                    UnityEngine.Debug.Log($"PotSmashingFix: 允许通过ScaryPot.Hitted调用的OnHitted攻击破坏罐子");
                    // 清除标志，避免重复使用
                    _hittedPots.Remove(__instance);
                    return true; // 允许通过Hitted调用的OnHitted攻击
                }

                // 阻止所有直接调用OnHitted的攻击
                UnityEngine.Debug.Log($"PotSmashingFix: 阻止直接调用OnHitted的攻击破坏罐子 - 安全保护");
                return false; // 阻止所有直接调用OnHitted的攻击
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
        /// 检查调用栈中是否有ProjectileZombie相关的方法
        /// </summary>
        /// <returns>是否有ProjectileZombie攻击</returns>
        private static bool IsProjectileZombieAttackInStack()
        {
            try
            {
                // 获取当前调用栈
                System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
                
                // 检查调用栈中是否包含ProjectileZombie相关的方法
                for (int i = 0; i < stackTrace.FrameCount; i++)
                {
                    var frame = stackTrace.GetFrame(i);
                    var method = frame?.GetMethod();
                    var methodName = method?.Name ?? "";
                    var className = method?.DeclaringType?.Name ?? "";

                    // 跳过我们自己的方法
                    if (className.Contains("PotSmashingPatches"))
                    {
                        continue;
                    }

                    // 检查是否为ProjectileZombie相关
                    if (className.Contains("ProjectileZombie") || 
                        (className.Contains("Bullet") && methodName.Contains("OnTriggerEnter2D")) ||
                        (className.Contains("Submarine_b") || className.Contains("Submarine_c")))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"PotSmashingFix: 检查ProjectileZombie攻击调用栈时出错: {ex.Message}");
                return false; // 出错时默认没有ProjectileZombie攻击
            }
        }

        /// <summary>
        /// 检查是否为轰炸/物理爆炸类型的攻击
        /// </summary>
        /// <returns>是否为轰炸攻击</returns>
        private static bool IsBombingAttack()
        {
            try
            {
                // 获取当前调用栈
                System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
                
                // 检查调用栈中是否包含轰炸相关的方法
                for (int i = 0; i < stackTrace.FrameCount; i++)
                {
                    var frame = stackTrace.GetFrame(i);
                    var method = frame?.GetMethod();
                    var methodName = method?.Name ?? "";
                    var className = method?.DeclaringType?.Name ?? "";

                    // 跳过我们自己的方法
                    if (className.Contains("PotSmashingPatches"))
                    {
                        continue;
                    }

                    // 检查是否为轰炸/物理爆炸相关
                    if ((methodName.Contains("Explode") || methodName.Contains("Bomb") || methodName.Contains("HitLand") || methodName.Contains("HitZombie")) && 
                        (className.Contains("Bullet") || className.Contains("ProjectileZombie") || className.Contains("Submarine")))
                    {
                        return true;
                    }

                    // 检查是否为ProjectileZombie的特定攻击
                    if (className.Contains("ProjectileZombie") && 
                        (methodName.Contains("Update") || methodName.Contains("FixedUpdate") || methodName.Contains("RbUpdate")))
                    {
                        return true;
                    }

                    // 检查是否为Submarine相关的攻击
                    if ((className.Contains("Submarine_b") || className.Contains("Submarine_c")) && 
                        (methodName.Contains("AnimShoot") || methodName.Contains("SetBullet")))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"PotSmashingFix: 检查轰炸攻击调用栈时出错: {ex.Message}");
                return false; // 出错时默认不是轰炸攻击
            }
        }

        /// <summary>
        /// 检查调用栈中是否有任何与ProjectileZombie相关的攻击
        /// </summary>
        /// <returns>是否有ProjectileZombie相关攻击</returns>
        private static bool IsAnyProjectileZombieRelatedAttack()
        {
            try
            {
                // 获取当前调用栈
                System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
                
                // 检查调用栈中是否包含任何与ProjectileZombie相关的方法
                for (int i = 0; i < stackTrace.FrameCount; i++)
                {
                    var frame = stackTrace.GetFrame(i);
                    var method = frame?.GetMethod();
                    var methodName = method?.Name ?? "";
                    var className = method?.DeclaringType?.Name ?? "";

                    // 跳过我们自己的方法
                    if (className.Contains("PotSmashingPatches"))
                    {
                        continue;
                    }

                    // 检查是否为ProjectileZombie相关（更宽泛的检测）
                    if (className.Contains("ProjectileZombie") || 
                        className.Contains("Submarine_b") || 
                        className.Contains("Submarine_c") ||
                        (className.Contains("Bullet") && (methodName.Contains("OnTriggerEnter2D") || methodName.Contains("HitLand") || methodName.Contains("HitZombie"))) ||
                        (methodName.Contains("SetBullet") && (className.Contains("Submarine") || className.Contains("ProjectileZombie"))))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"PotSmashingFix: 检查ProjectileZombie相关攻击调用栈时出错: {ex.Message}");
                return false; // 出错时默认没有ProjectileZombie相关攻击
            }
        }

        /// <summary>
        /// 记录调用栈信息用于调试
        /// </summary>
        private static void LogStackTrace()
        {
            try
            {
                // 获取当前调用栈
                System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace(true);
                
                UnityEngine.Debug.Log($"PotSmashingFix: 调用栈信息 (共{stackTrace.FrameCount}层):");
                
                // 记录调用栈中的每一层
                for (int i = 0; i < Math.Min(stackTrace.FrameCount, 15); i++) // 记录前15层
                {
                    var frame = stackTrace.GetFrame(i);
                    var method = frame?.GetMethod();
                    var methodName = method?.Name ?? "Unknown";
                    var className = method?.DeclaringType?.Name ?? "Unknown";
                    var fileName = frame?.GetFileName() ?? "Unknown";
                    var lineNumber = frame?.GetFileLineNumber() ?? 0;
                    
                    UnityEngine.Debug.Log($"  [{i}] {className}.{methodName} (文件: {fileName}, 行: {lineNumber})");
                    
                    // 特别检查是否包含ProjectileZombie相关的内容
                    if (className.Contains("ProjectileZombie") || 
                        className.Contains("Submarine") ||
                        methodName.Contains("ProjectileZombie") ||
                        methodName.Contains("Submarine") ||
                        methodName.Contains("SetBullet") ||
                        methodName.Contains("AnimShoot"))
                    {
                        UnityEngine.Debug.Log($"    *** 发现ProjectileZombie相关内容: {className}.{methodName} ***");
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"PotSmashingFix: 记录调用栈时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 通用检测：检查调用栈中是否包含任何与ProjectileZombie、Submarine相关的内容
        /// </summary>
        /// <returns>是否有ProjectileZombie相关攻击</returns>
        private static bool IsAnyProjectileZombieRelatedInStack()
        {
            try
            {
                // 获取当前调用栈
                System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
                
                // 检查调用栈中是否包含任何与ProjectileZombie、Submarine相关的内容
                for (int i = 0; i < stackTrace.FrameCount; i++)
                {
                    var frame = stackTrace.GetFrame(i);
                    var method = frame?.GetMethod();
                    var methodName = method?.Name ?? "";
                    var className = method?.DeclaringType?.Name ?? "";

                    // 跳过我们自己的方法
                    if (className.Contains("PotSmashingPatches"))
                    {
                        continue;
                    }

                    // 通用检测：任何包含ProjectileZombie、Submarine、SetBullet、AnimShoot的内容
                    if (className.Contains("ProjectileZombie") || 
                        className.Contains("Submarine") ||
                        methodName.Contains("SetBullet") ||
                        methodName.Contains("AnimShoot") ||
                        methodName.Contains("ProjectileZombie"))
                    {
                        UnityEngine.Debug.Log($"PotSmashingFix: 检测到ProjectileZombie相关攻击 - 类名: {className}, 方法名: {methodName}");
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"PotSmashingFix: 通用检测ProjectileZombie相关攻击时出错: {ex.Message}");
                return false; // 出错时默认没有ProjectileZombie相关攻击
            }
        }

        /// <summary>
        /// 检查是否为鼠标点击攻击（正常砸罐子）
        /// </summary>
        /// <returns>是否为鼠标点击攻击</returns>
        private static bool IsMouseClickAttack()
        {
            try
            {
                // 获取当前调用栈
                System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
                
                // 检查调用栈中是否包含鼠标相关的方法
                for (int i = 0; i < stackTrace.FrameCount; i++)
                {
                    var frame = stackTrace.GetFrame(i);
                    var method = frame?.GetMethod();
                    var methodName = method?.Name ?? "";
                    var className = method?.DeclaringType?.Name ?? "";

                    // 跳过我们自己的方法
                    if (className.Contains("PotSmashingPatches"))
                    {
                        continue;
                    }

                    // 检查是否包含鼠标相关的方法
                    if (className.Contains("Mouse") || 
                        methodName.Contains("Mouse") ||
                        methodName.Contains("Click") ||
                        methodName.Contains("LeftClick"))
                    {
                        UnityEngine.Debug.Log($"PotSmashingFix: 检测到鼠标点击攻击 - 类名: {className}, 方法名: {methodName}");
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"PotSmashingFix: 检测鼠标点击攻击时出错: {ex.Message}");
                return false; // 出错时默认不是鼠标点击攻击
            }
        }

        /// <summary>
        /// 检查OnHitted调用栈中是否有鼠标点击相关的方法
        /// </summary>
        /// <returns>是否为鼠标点击的OnHitted</returns>
        private static bool IsMouseClickInOnHittedStack()
        {
            try
            {
                // 获取当前调用栈
                System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
                
                // 检查调用栈中是否包含鼠标相关的方法
                for (int i = 0; i < stackTrace.FrameCount; i++)
                {
                    var frame = stackTrace.GetFrame(i);
                    var method = frame?.GetMethod();
                    var methodName = method?.Name ?? "";
                    var className = method?.DeclaringType?.Name ?? "";

                    // 跳过我们自己的方法
                    if (className.Contains("PotSmashingPatches"))
                    {
                        continue;
                    }

                    // 检查是否包含鼠标相关的方法
                    if (className.Contains("Mouse") || 
                        methodName.Contains("Mouse") ||
                        methodName.Contains("Click") ||
                        methodName.Contains("LeftClick"))
                    {
                        UnityEngine.Debug.Log($"PotSmashingFix: 检测到鼠标点击的OnHitted - 类名: {className}, 方法名: {methodName}");
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"PotSmashingFix: 检测鼠标点击OnHitted时出错: {ex.Message}");
                return false; // 出错时默认不是鼠标点击
            }
        }

        /// <summary>
        /// 检查OnHitted是否是通过ScaryPot.Hitted调用的
        /// </summary>
        /// <returns>是否通过ScaryPot.Hitted调用</returns>
        private static bool IsCalledFromScaryPotHitted()
        {
            try
            {
                // 获取当前调用栈
                System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
                
                // 检查调用栈中是否包含ScaryPot.Hitted
                for (int i = 0; i < stackTrace.FrameCount; i++)
                {
                    var frame = stackTrace.GetFrame(i);
                    var method = frame?.GetMethod();
                    var methodName = method?.Name ?? "";
                    var className = method?.DeclaringType?.Name ?? "";

                    // 跳过我们自己的方法
                    if (className.Contains("PotSmashingPatches"))
                    {
                        continue;
                    }

                    // 检查是否包含ScaryPot.Hitted
                    if (className.Contains("ScaryPot") && methodName.Contains("Hitted"))
                    {
                        UnityEngine.Debug.Log($"PotSmashingFix: 检测到通过ScaryPot.Hitted调用的OnHitted - 类名: {className}, 方法名: {methodName}");
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"PotSmashingFix: 检测ScaryPot.Hitted调用时出错: {ex.Message}");
                return false; // 出错时默认不是通过Hitted调用
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

        // 在每帧结束时重置锤击状态，为下一次锤击做准备
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Board), nameof(Board.Update))] // 假设Board.Update每帧执行
        public static void Postfix_BoardUpdate()
        {
            // 清除当前锤击的状态，为下一次锤击做准备
            _hitPotsInCurrentSwing.Clear();
            _processedPotsInCurrentSwing.Clear();
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
                        5.0f); // 5格范围内的碰撞体

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
        /// 拦截 UltimateJackboxZombie.AnimPop 方法，让终极小丑跳跳王可以爆炸，但爆炸不影响罐子
        /// </summary>
        /// <param name="__instance">UltimateJackboxZombie 实例</param>
        /// <returns>是否允许执行原方法</returns>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UltimateJackboxZombie), nameof(UltimateJackboxZombie.AnimPop))]
        public static bool Prefix_UltimateJackboxZombieAnimPop(UltimateJackboxZombie __instance)
        {
            try
            {
                _isProcessingJackboxExplosion = true;
                return true; // 允许终极小丑跳跳王正常爆炸
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"PotSmashingFix: UltimateJackboxZombie.AnimPop 补丁执行失败: {ex.Message}");
                return true; // 出错时允许正常执行
            }
        }

        /// <summary>
        /// 拦截 UltimateJackboxZombie.AnimPop 方法的后置处理
        /// </summary>
        /// <param name="__instance">UltimateJackboxZombie 实例</param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UltimateJackboxZombie), nameof(UltimateJackboxZombie.AnimPop))]
        public static void Postfix_UltimateJackboxZombieAnimPop(UltimateJackboxZombie __instance)
        {
            try
            {
                _isProcessingJackboxExplosion = false;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"PotSmashingFix: UltimateJackboxZombie.AnimPop 后置补丁执行失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 拦截 JackboxJumpZombie.DieEvent 方法，让小丑跳跳僵尸可以爆炸，但爆炸不影响罐子
        /// </summary>
        /// <param name="__instance">JackboxJumpZombie 实例</param>
        /// <param name="reason">死亡原因</param>
        /// <returns>是否允许执行原方法</returns>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(JackboxJumpZombie), nameof(JackboxJumpZombie.DieEvent))]
        public static bool Prefix_JackboxJumpZombieDieEvent(JackboxJumpZombie __instance, int reason)
        {
            try
            {
                _isProcessingJackboxExplosion = true;
                return true; // 允许小丑跳跳僵尸正常死亡爆炸
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"PotSmashingFix: JackboxJumpZombie.DieEvent 补丁执行失败: {ex.Message}");
                return true; // 出错时允许正常执行
            }
        }

        /// <summary>
        /// 拦截 JackboxJumpZombie.DieEvent 方法的后置处理
        /// </summary>
        /// <param name="__instance">JackboxJumpZombie 实例</param>
        /// <param name="reason">死亡原因</param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(JackboxJumpZombie), nameof(JackboxJumpZombie.DieEvent))]
        public static void Postfix_JackboxJumpZombieDieEvent(JackboxJumpZombie __instance, int reason)
        {
            try
            {
                _isProcessingJackboxExplosion = false;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"PotSmashingFix: JackboxJumpZombie.DieEvent 后置补丁执行失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 拦截 Jackbox_a.LoseHeadEvent 方法，让一级小丑跳跳可以爆炸，但爆炸不影响罐子
        /// </summary>
        /// <param name="__instance">Jackbox_a 实例</param>
        /// <returns>是否允许执行原方法</returns>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Jackbox_a), nameof(Jackbox_a.LoseHeadEvent))]
        public static bool Prefix_Jackbox_aLoseHeadEvent(Jackbox_a __instance)
        {
            try
            {
                _isProcessingJackboxExplosion = true;
                return true; // 允许一级小丑跳跳正常爆炸
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"PotSmashingFix: Jackbox_a.LoseHeadEvent 补丁执行失败: {ex.Message}");
                return true; // 出错时允许正常执行
            }
        }

        /// <summary>
        /// 拦截 Jackbox_a.LoseHeadEvent 方法的后置处理
        /// </summary>
        /// <param name="__instance">Jackbox_a 实例</param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Jackbox_a), nameof(Jackbox_a.LoseHeadEvent))]
        public static void Postfix_Jackbox_aLoseHeadEvent(Jackbox_a __instance)
        {
            try
            {
                _isProcessingJackboxExplosion = false;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"PotSmashingFix: Jackbox_a.LoseHeadEvent 后置补丁执行失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 拦截 Jackbox_c.LoseHeadEvent 方法，让三级小丑跳跳可以爆炸，但爆炸不影响罐子
        /// </summary>
        /// <param name="__instance">Jackbox_c 实例</param>
        /// <returns>是否允许执行原方法</returns>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Jackbox_c), nameof(Jackbox_c.LoseHeadEvent))]
        public static bool Prefix_Jackbox_cLoseHeadEvent(Jackbox_c __instance)
        {
            try
            {
                _isProcessingJackboxExplosion = true;
                return true; // 允许三级小丑跳跳正常爆炸
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"PotSmashingFix: Jackbox_c.LoseHeadEvent 补丁执行失败: {ex.Message}");
                return true; // 出错时允许正常执行
            }
        }

        /// <summary>
        /// 拦截 Jackbox_c.LoseHeadEvent 方法的后置处理
        /// </summary>
        /// <param name="__instance">Jackbox_c 实例</param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Jackbox_c), nameof(Jackbox_c.LoseHeadEvent))]
        public static void Postfix_Jackbox_cLoseHeadEvent(Jackbox_c __instance)
        {
            try
            {
                _isProcessingJackboxExplosion = false;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"PotSmashingFix: Jackbox_c.LoseHeadEvent 后置补丁执行失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 拦截 SuperJackboxZombie.DieEvent 方法，让超级小丑僵尸死亡时可以爆炸，但爆炸不影响罐子
        /// </summary>
        /// <param name="__instance">SuperJackboxZombie 实例</param>
        /// <param name="reason">死亡原因</param>
        /// <returns>是否允许执行原方法</returns>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SuperJackboxZombie), nameof(SuperJackboxZombie.DieEvent))]
        public static bool Prefix_SuperJackboxZombieDieEvent(SuperJackboxZombie __instance, int reason)
        {
            try
            {
                _isProcessingJackboxExplosion = true;
                return true; // 允许超级小丑僵尸正常死亡爆炸
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"PotSmashingFix: SuperJackboxZombie.DieEvent 补丁执行失败: {ex.Message}");
                return true; // 出错时允许正常执行
            }
        }

        /// <summary>
        /// 拦截 SuperJackboxZombie.DieEvent 方法的后置处理
        /// </summary>
        /// <param name="__instance">SuperJackboxZombie 实例</param>
        /// <param name="reason">死亡原因</param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SuperJackboxZombie), nameof(SuperJackboxZombie.DieEvent))]
        public static void Postfix_SuperJackboxZombieDieEvent(SuperJackboxZombie __instance, int reason)
        {
            try
            {
                _isProcessingJackboxExplosion = false;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"PotSmashingFix: SuperJackboxZombie.DieEvent 后置补丁执行失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 拦截 UltimateJackboxZombie.DieEvent 方法，让终极小丑跳跳王死亡时可以爆炸，但爆炸不影响罐子
        /// </summary>
        /// <param name="__instance">UltimateJackboxZombie 实例</param>
        /// <param name="reason">死亡原因</param>
        /// <returns>是否允许执行原方法</returns>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UltimateJackboxZombie), nameof(UltimateJackboxZombie.DieEvent))]
        public static bool Prefix_UltimateJackboxZombieDieEvent(UltimateJackboxZombie __instance, int reason)
        {
            try
            {
                _isProcessingJackboxExplosion = true;
                return true; // 允许终极小丑跳跳王正常死亡爆炸
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"PotSmashingFix: UltimateJackboxZombie.DieEvent 补丁执行失败: {ex.Message}");
                return true; // 出错时允许正常执行
            }
        }

        /// <summary>
        /// 拦截 UltimateJackboxZombie.DieEvent 方法的后置处理
        /// </summary>
        /// <param name="__instance">UltimateJackboxZombie 实例</param>
        /// <param name="reason">死亡原因</param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UltimateJackboxZombie), nameof(UltimateJackboxZombie.DieEvent))]
        public static void Postfix_UltimateJackboxZombieDieEvent(UltimateJackboxZombie __instance, int reason)
        {
            try
            {
                _isProcessingJackboxExplosion = false;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"PotSmashingFix: UltimateJackboxZombie.DieEvent 后置补丁执行失败: {ex.Message}");
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