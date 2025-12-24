using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace UltimateApocalypseChomper.BepInEx
{
    /// <summary>
    /// 究极天启樱龙插件入口 - 3.2版本适配（不依赖CustomizeLib）
    /// </summary>
    [BepInPlugin("inf75.ultimateapocalypsechomper", "UltimateApocalypseChomper", "2.0.0")]
    public class Core : BasePlugin
    {
        internal const int PlantId = 2039;
        internal const int PlantToughness = 32000;
        internal const int BaseAttackDamage = 4000;
        internal const int PlantSunCost = 1200;
        internal const float PlantCooldown = 75f;
        internal const string BundleName = "ultimateapocalypsechomper";

        internal static ManualLogSource? PluginLog { get; private set; }
        internal static AssetBundle? CachedBundle;

        // 旅行词条ID（在TravelMgr.Awake后动态注册）
        internal static int TravelBuffGluttonyId = -1;
        internal static int TravelBuffJudgementId = -1;
        internal static int TheNewTravelId = 0;

        // 词条文本列表
        internal static readonly List<string> UltimateBuffTexts = new List<string>
        {
            "饕餮巨嘴：攻击/索敌范围延长至 3 格，吞噬冷却 = 5 + ln(僵尸总血量) 秒（≤15 秒）",
            "天启神罚：所有回复 ×3，不死期间造成的最终伤害 ×4"
        };

        public override void Load()
        {
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
                PluginLog = Log;

                Log.LogInfo("[究极天启樱龙] 开始加载插件...");

                // 注册 Harmony 补丁
                var harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
                Log.LogInfo($"[究极天启樱龙] Harmony 补丁已注册，共 {harmony.GetPatchedMethods().Count()} 个方法");

                // 注册自定义组件到 IL2CPP
                ClassInjector.RegisterTypeInIl2Cpp<UltimateApocalypseChomperComponent>();
                Log.LogInfo("[究极天启樱龙] IL2CPP类型注册成功");

                Log.LogInfo("[究极天启樱龙] 插件加载完成，等待游戏初始化...");

                // 如果游戏已初始化，直接注册植物
                if (GameAPP.gameAPP != null)
                {
                    Log.LogWarning("[究极天启樱龙] GameAPP 已初始化，直接注册植物");
                    GameAppAwakePatch.TryRegisterPlant();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[究极天启樱龙] 插件加载失败: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// 从嵌入资源加载 AssetBundle
        /// </summary>
        internal static AssetBundle? LoadEmbeddedAssetBundle(string bundleName)
        {
            if (CachedBundle != null)
                return CachedBundle;

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceNames = assembly.GetManifestResourceNames();
                string? matchedName = null;

                foreach (var name in resourceNames)
                {
                    if (name.EndsWith(bundleName, StringComparison.OrdinalIgnoreCase) ||
                        name.Equals(bundleName, StringComparison.OrdinalIgnoreCase))
                    {
                        matchedName = name;
                        break;
                    }
                }

                if (matchedName == null)
                {
                    PluginLog?.LogError($"[究极天启樱龙] 未找到嵌入资源: {bundleName}");
                    PluginLog?.LogInfo($"[究极天启樱龙] 可用资源: {string.Join(", ", resourceNames)}");
                    return null;
                }

                using var stream = assembly.GetManifestResourceStream(matchedName);
                if (stream == null)
                {
                    PluginLog?.LogError($"[究极天启樱龙] 无法读取资源流: {matchedName}");
                    return null;
                }

                var bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);
                CachedBundle = AssetBundle.LoadFromMemory(bytes);

                PluginLog?.LogInfo($"[究极天启樱龙] AssetBundle 加载成功: {bundleName}");
                return CachedBundle;
            }
            catch (Exception ex)
            {
                PluginLog?.LogError($"[究极天启樱龙] 加载 AssetBundle 失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 注册旅行词条（在TravelMgr.Awake后调用）
        /// 此方法现在由 TravelMgrAwakePatch 调用
        /// </summary>
        internal static void RegisterTravelBuffs()
        {
            // 词条注册已移至 TravelMgrAwakePatch.PostAwake
            // 这里只记录日志
            if (TravelBuffGluttonyId >= 0)
                PluginLog?.LogInfo($"[究极天启樱龙] 饕餮巨嘴词条ID: {TravelBuffGluttonyId}");
            if (TravelBuffJudgementId >= 0)
                PluginLog?.LogInfo($"[究极天启樱龙] 天启神罚词条ID: {TravelBuffJudgementId}");
        }

        /// <summary>
        /// 尝试注册究极词条到TravelMgr.ultimateBuffs
        /// </summary>
        private static int TryRegisterUltimateBuff(string description)
        {
            try
            {
                // 检查 TravelMgr.ultimateBuffs 是否可用
                if (TravelMgr.ultimateBuffs == null)
                {
                    PluginLog?.LogWarning("[究极天启樱龙] TravelMgr.ultimateBuffs 为空，无法注册词条");
                    return -1;
                }

                // 获取当前最大ID
                int newId = TravelMgr.ultimateBuffs.Count;
                
                // 添加到词条字典
                TravelMgr.ultimateBuffs[newId] = description;

                return newId;
            }
            catch (Exception ex)
            {
                PluginLog?.LogWarning($"[究极天启樱龙] 注册词条失败: {ex.Message}");
                return -1;
            }
        }
    }
}
