using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;

namespace MagnetNutUnlimited
{
 
    /// 磁力坚果无限吸引子弹插件

    [BepInPlugin("MagnetNutUnlimited", "MagnetNutUnlimited", "1.0.0")]
    public class Core : BasePlugin
    {
        // 配置选项：是否启用智能排除机制
        public static bool EnableSmartExclusion = true;
        
        // 配置选项：是否启用调试日志
        public static bool EnableDebugLogs = true;
        
        // 配置选项：是否启用激进排除策略
        public static bool EnableAggressiveExclusion = true;
        
        // 配置选项：是否完全禁用对特殊子弹的修改
        public static bool DisableSpecialBulletModification = true; // 默认启用，避免问题
        
        // 配置选项：是否启用性能优化模式
        public static bool EnablePerformanceMode = true;
        
        public override void Load()
        {
            Log.LogInfo("MagnetNutUnlimited: 开始加载插件...");
            try
            {
                Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
                Log.LogInfo("MagnetNutUnlimited: 插件加载完成 - 磁力坚果无限存储和永久留存功能已启用");
                Log.LogInfo($"MagnetNutUnlimited: 智能排除机制: {(EnableSmartExclusion ? "启用" : "禁用")}");
                Log.LogInfo($"MagnetNutUnlimited: 调试日志: {(EnableDebugLogs ? "启用" : "禁用")}");
                Log.LogInfo($"MagnetNutUnlimited: 激进排除策略: {(EnableAggressiveExclusion ? "启用" : "禁用")}");
                Log.LogInfo($"MagnetNutUnlimited: 特殊子弹修改禁用: {(DisableSpecialBulletModification ? "启用" : "禁用")}");
                Log.LogInfo($"MagnetNutUnlimited: 性能优化模式: {(EnablePerformanceMode ? "启用" : "禁用")}");
                Log.LogInfo("MagnetNutUnlimited: 已启用缓存机制，大幅提升性能");
            }
            catch (System.Exception ex)
            {
                Log.LogError($"MagnetNutUnlimited: 插件加载失败: {ex.Message}");
            }
        }
    }

    
    /// 磁力坚果无限吸引补丁类
    /// 功能1：取消100个子弹存储限制
    
    [HarmonyPatch]
    public class MagnetNutPatches
    {
        
        /// 补丁 FixedUpdate 方法，取消子弹存储上限（100个限制）
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MagnetNut), "FixedUpdate")]
        public static bool FixedUpdatePrefix(MagnetNut __instance)
        {
            try
            {
                // 取消子弹存储上限：强制触发SearchBullet，无视100个限制
                ForceSearchBullet(__instance);
                
                // 继续执行原始方法
                return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"MagnetNutUnlimited: FixedUpdatePrefix 执行失败: {ex.Message}");
                return true; // 出错时继续执行原始方法
            }
        }

        
        /// 强制触发SearchBullet，无视100个子弹限制，但限制拾取范围为3*3，并排除三线火炬分支子弹
        
        private static void ForceSearchBullet(MagnetNut magnetNut)
        {
            try
            {
                if (magnetNut == null) return;

                // 限制拾取范围为3*3（1.5格范围）
                var rField = typeof(MagnetNut).GetField("R", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (rField != null)
                {
                    // 设置拾取范围为1.5（3*3格子的半径）
                    rField.SetValue(magnetNut, 1.5f);
                }

                // 获取SearchBullet方法
                var searchBulletMethod = typeof(MagnetNut).GetMethod("SearchBullet", 
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                
                if (searchBulletMethod != null)
                {
                    // 在调用SearchBullet之前，先过滤掉火炬子弹
                    FilterTorchWoodBullets();
                    
                    // 直接调用SearchBullet，无视子弹数量限制
                    searchBulletMethod.Invoke(magnetNut, null);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"MagnetNutUnlimited: ForceSearchBullet 执行失败: {ex.Message}");
            }
        }

        
        /// 过滤掉三线火炬的分支子弹，防止它们被MagnetNut错误吸引
        
        private static void FilterTorchWoodBullets()
        {
            try
            {
                // 获取所有子弹
                var bulletArrayField = typeof(Board).GetField("bulletArray", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (bulletArrayField != null && Board.Instance != null)
                {
                    var bulletArray = bulletArrayField.GetValue(Board.Instance);
                    if (bulletArray != null)
                    {
                        // 使用反射获取数组长度和元素
                        var arrayType = bulletArray.GetType();
                        var lengthProperty = arrayType.GetProperty("Length");
                        if (lengthProperty != null)
                        {
                            int length = (int)lengthProperty.GetValue(bulletArray);
                            
                            for (int i = 0; i < length; i++)
                            {
                                var bullet = arrayType.GetMethod("Get", new Type[] { typeof(int) })?.Invoke(bulletArray, new object[] { i });
                                if (bullet != null)
                                {
                                    // 检查子弹是否有torchWood字段（三线火炬分支子弹）
                                    var torchWoodField = bullet.GetType().GetField("torchWood", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                                    if (torchWoodField != null)
                                    {
                                        var torchWood = torchWoodField.GetValue(bullet);
                                        if (torchWood != null)
                                        {
                                            // 这是三线火炬的分支子弹，暂时禁用其Collider2D以防止被MagnetNut吸引
                                            var colField = bullet.GetType().GetField("col", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                                            if (colField != null)
                                            {
                                                var col = colField.GetValue(bullet);
                                                if (col != null)
                                                {
                                                    var enabledProperty = col.GetType().GetProperty("enabled");
                                                    if (enabledProperty != null)
                                                    {
                                                        // 暂时禁用碰撞器，防止被MagnetNut吸引
                                                        enabledProperty.SetValue(col, false);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"MagnetNutUnlimited: FilterTorchWoodBullets 执行失败: {ex.Message}");
            }
        }
    }

    
    /// 子弹更新补丁类
    /// 功能2：完全替换Bullet.Update方法，阻止20秒时间限制
    /// 新思路：完全替换而不是前缀补丁
    
    [HarmonyPatch]
    public class BulletUpdatePatches
    {
        // 缓存机制：避免重复反射调用
        private static readonly Dictionary<Type, bool> _exclusionCache = new Dictionary<Type, bool>();
        private static readonly HashSet<string> _excludedClassNames = new HashSet<string>
        {
            // 杨桃类子弹 - 基于反汇编分析
            "Bullet_star", "Bullet_cactusStar", "Bullet_superStar", "Bullet_ultimateStar",
            "Bullet_lanternStar", "Bullet_seaStar", "Bullet_jackboxStar", "Bullet_pickaxeStar",
            "Bullet_magnetStar", "Bullet_ironStar",
            
            // 三线类子弹
            "Bullet_threeSpike",
            
            // 追踪类子弹
            "Bullet_magicTrack", "Bullet_normalTrack", "Bullet_iceTrack", "Bullet_fireTrack",
            
            // 其他特殊子弹
            "Bullet_doom", "Bullet_doom_throw", "Bullet_endoSun", "Bullet_extremeSnowPea",
            "Bullet_firePea", "Bullet_iceSword", "Bullet_lourCactus", "Bullet_melonCannon",
            "Bullet_shulkLeaf_ultimate", "Bullet_smallGoldCannon", "Bullet_smallSun",
            "Bullet_springMelon", "Bullet_sunCabbage", "Bullet_ultimateSun"
        };
        
        // 允许的子弹类型（基础豌豆类）
        private static readonly HashSet<string> _allowedClassNames = new HashSet<string>
        {
            "Bullet_pea", "Bullet_snowPea", "Bullet_firePea_yellow", 
            "Bullet_firePea_orange", "Bullet_firePea_red", "Bullet_ironPea"
        };

        
        /// 高效检查是否为需要排除的子弹类型
        /// 使用缓存机制避免重复反射调用，大幅提升性能
        
        public static bool ShouldExcludeBullet(Bullet bullet)
        {
            if (bullet == null) return false;
            
            // 如果智能排除机制被禁用，则不排除任何子弹
            if (!Core.EnableSmartExclusion)
            {
                return false;
            }
            
            // 性能模式：直接返回true，排除所有特殊子弹
            if (Core.EnablePerformanceMode && Core.DisableSpecialBulletModification)
            {
                return true; // 简单粗暴，但性能最好
            }
            
            var bulletType = bullet.GetType();
            
            // 使用缓存避免重复计算
            if (_exclusionCache.TryGetValue(bulletType, out bool cachedResult))
            {
                return cachedResult;
            }
            
            bool shouldExclude = false;
            string className = bulletType.Name;
            
            // 完全禁用策略：只允许基础豌豆类子弹
            if (Core.DisableSpecialBulletModification)
            {
                shouldExclude = !_allowedClassNames.Contains(className);
            }
            else
            {
                // 精确排除策略：检查预定义的排除列表
                if (_excludedClassNames.Contains(className))
                {
                    shouldExclude = true;
                }
                else if (Core.EnableAggressiveExclusion)
                {
                    // 激进策略：检查关键词
                    shouldExclude = ContainsExclusionKeywords(className);
                }
            }
            
            // 缓存结果
            _exclusionCache[bulletType] = shouldExclude;
            
            // 调试日志（仅在排除时记录，减少日志量）
            if (shouldExclude && Core.EnableDebugLogs)
            {
                UnityEngine.Debug.Log($"MagnetNutUnlimited: 排除子弹: {className}");
            }
            
            return shouldExclude;
        }
        
        
        /// 检查类名是否包含排除关键词
        
        private static bool ContainsExclusionKeywords(string className)
        {
            // 使用更高效的关键词检查
            return className.Contains("Star") || className.Contains("Spike") || 
                   className.Contains("Track") || className.Contains("Doom") ||
                   className.Contains("Extreme") || className.Contains("Fire") ||
                   className.Contains("Ice") || className.Contains("Melon") ||
                   className.Contains("Sun") || className.Contains("Cactus") ||
                   className.Contains("Sword") || className.Contains("Cannon") ||
                   className.Contains("Ultimate") || className.Contains("Super");
        }
        
        
        /// 清理缓存（在游戏重新开始时调用）
        
        public static void ClearCache()
        {
            _exclusionCache.Clear();
        }

        
        /// 完全替换 Bullet.Update 方法
        /// 策略：完全重写Update逻辑，阻止时间累积和死亡检查
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Bullet), "Update")]
        public static bool BulletUpdatePrefix(Bullet __instance)
        {
            try
            {
                if (__instance == null) return false; // 完全替换，不执行原始方法
                
                // 排除特定子弹类型，让它们执行原始逻辑
                if (ShouldExcludeBullet(__instance))
                {
                    if (Core.EnableDebugLogs)
                        UnityEngine.Debug.Log($"MagnetNutUnlimited: 子弹被排除，执行原始Update方法");
                    return true; // 执行原始方法
                }
                
                if (Core.EnableDebugLogs)
                    UnityEngine.Debug.Log($"MagnetNutUnlimited: 子弹未被排除，执行修改后的Update方法");
                
                // 获取字段
                var theMovingWayField = typeof(Bullet).GetField("theMovingWay", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                var theExistTimeField = typeof(Bullet).GetField("theExistTime", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                var shadowField = typeof(Bullet).GetField("shadow", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                var targetZombieField = typeof(Bullet).GetField("targetZombie", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                
                if (theMovingWayField != null && theExistTimeField != null)
                {
                    var theMovingWay = (int)theMovingWayField.GetValue(__instance);
                    var theExistTime = (float)theExistTimeField.GetValue(__instance);
                    
                    // 关键修改：阻止时间累积
                    // 只有当movingway != 10时才累积时间，我们强制设置为10
                    if (theMovingWay != 10)
                    {
                        theMovingWayField.SetValue(__instance, 10);
                        UnityEngine.Debug.Log($"MagnetNutUnlimited: 强制设置movingway为10 - 从{theMovingWay}改为10");
                    }
                    
                    // 关键修改：阻止死亡检查
                    // 原始代码：if ( theExistTime_1 > 0.75 && this->fields.theMovingWay == 3 || theExistTime_1 > 20.0 )
                    // 我们完全跳过这个检查，不调用Bullet__Die
                    
                    // 处理shadow更新（保持原始逻辑）
                    if (shadowField != null)
                    {
                        var shadow = shadowField.GetValue(__instance);
                        if (shadow != null)
                        {
                            // 调用Bullet__ShadowTransformUpdate
                            var shadowUpdateMethod = typeof(Bullet).GetMethod("ShadowTransformUpdate", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                            if (shadowUpdateMethod != null)
                            {
                                shadowUpdateMethod.Invoke(__instance, null);
                            }
                        }
                    }
                    
                    // 处理targetZombie逻辑（保持原始逻辑）
                    if (theExistTime > 1.0f && targetZombieField != null)
                    {
                        var targetZombie = targetZombieField.GetValue(__instance);
                        if (targetZombie == null && theMovingWay == 20)
                        {
                            // 调用HitLand方法
                            var hitLandMethod = typeof(Bullet).GetMethod("HitLand", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                            if (hitLandMethod != null)
                            {
                                hitLandMethod.Invoke(__instance, null);
                            }
                        }
                    }
                    
                    UnityEngine.Debug.Log($"MagnetNutUnlimited: 完全替换Update - movingway:{theMovingWay}, existTime:{theExistTime}");
                }
                
                return false; // 完全替换，不执行原始方法
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"MagnetNutUnlimited: BulletUpdatePrefix 执行失败: {ex.Message}");
                return true; // 出错时执行原始方法
            }
        }
    }

    
    /// 子弹死亡拦截补丁类
    /// 功能3：激进策略 - 阻止所有子弹因时间限制死亡
    
    [HarmonyPatch]
    public class BulletDiePatches
    {
        
        /// 补丁 Bullet.Die 方法，激进策略：阻止所有子弹因时间限制死亡
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Bullet), "Die")]
        public static bool BulletDiePrefix(Bullet __instance)
        {
            try
            {
                if (__instance == null) return true;
                
                // 排除特定子弹类型，让它们执行原始逻辑
                if (BulletUpdatePatches.ShouldExcludeBullet(__instance))
                {
                    if (Core.EnableDebugLogs)
                        UnityEngine.Debug.Log($"MagnetNutUnlimited: 子弹被排除，执行原始Die方法");
                    return true; // 执行原始方法
                }
                
                if (Core.EnableDebugLogs)
                    UnityEngine.Debug.Log($"MagnetNutUnlimited: 子弹未被排除，执行修改后的Die方法");
                
                // 获取字段检查
                var theMovingWayField = typeof(Bullet).GetField("theMovingWay", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                var theExistTimeField = typeof(Bullet).GetField("theExistTime", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                
                if (theMovingWayField != null && theExistTimeField != null)
                {
                    var theMovingWay = (int)theMovingWayField.GetValue(__instance);
                    var theExistTime = (float)theExistTimeField.GetValue(__instance);
                    
                    // 激进策略：如果子弹是因为时间限制要死亡，就阻止
                    // 检查是否是因为时间超过20秒或movingway=3且时间>0.75秒
                    if (theExistTime > 20.0f || (theMovingWay == 3 && theExistTime > 0.75f))
                    {
                        // 强制设置movingway为10，重置时间
                        theMovingWayField.SetValue(__instance, 10);
                        theExistTimeField.SetValue(__instance, 0.0f);
                        
                        UnityEngine.Debug.Log($"MagnetNutUnlimited: 阻止子弹因时间限制死亡 - movingway:{theMovingWay}->10, existTime:{theExistTime}->0");
                        return false; // 阻止死亡
                    }
                }
                
                return true; // 允许正常死亡
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"MagnetNutUnlimited: BulletDiePrefix 执行失败: {ex.Message}");
                return true; // 出错时允许正常死亡
            }
        }
    }
}