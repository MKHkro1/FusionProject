using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppGameObjectList = Il2CppSystem.Collections.Generic.List<UnityEngine.GameObject>;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace Wish.BepInEx
{
    /// <summary>
    /// 纠缠之缘植物插件入口 - 3.2版本适配（不依赖CustomizeLib）
    /// </summary>
    [BepInPlugin("com.wish.bepinex", "Wish", "2.0.0")]
    public class Core : BasePlugin
    {
        internal const int PlantId = 1771; // 植物ID
        internal const string BundleName = "wish";

        internal static ManualLogSource? Logger;
        internal static AssetBundle? CachedBundle;

        // 概率递增系统：存储每个植物的当前概率状态
        internal static readonly Dictionary<GoldSunflower, ProbabilityState> ProbabilityStates = new Dictionary<GoldSunflower, ProbabilityState>();

        // 超极植物卡片ID列表
        internal static readonly int[] SuperPlantIds = { 243, 249, 253, 1005, 1013, 1026, 1046, 1052, 1104, 1110, 1126, 1132, 1148, 1160, 1161, 1169, 1174, 1220, 1234, 1266, 1300, 1306, 1342 };
        
        // 缓存超极植物ID的HashSet，用于快速查找（性能优化）
        internal static readonly HashSet<int> SuperPlantIdsSet = new HashSet<int>(SuperPlantIds);

        // 究极植物卡片ID列表
        internal static readonly int[] UltimatePlantIds = BuildUltimatePlantIds();
        
        // 缓存究极植物ID的HashSet，用于快速查找
        internal static readonly HashSet<int> UltimatePlantIdsSet = new HashSet<int>(UltimatePlantIds);

        /// <summary>
        /// 构建究极植物ID列表
        /// </summary>
        private static int[] BuildUltimatePlantIds()
        {
            var list = new List<int>(128);
            list.AddRange(new[] { 227, 229, 234, 240, 242, 245 });
            for (int i = 300; i <= 305; i++) list.Add(i);
            for (int i = 900; i <= 911; i++) list.Add(i);
            for (int i = 913; i <= 917; i++) list.Add(i);
            for (int i = 919; i <= 937; i++) list.Add(i);
            list.Add(939);
            list.Add(940);
            for (int i = 942; i <= 949; i++) list.Add(i);
            for (int i = 951; i <= 959; i++) list.Add(i);
            for (int i = 961; i <= 970; i++) list.Add(i);
            return list.ToArray();
        }

        // 惊喜礼盒卡片ID
        internal const int GiftBoxCardId = 256;

        public override void Load()
        {
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
                Logger = Log;
                
                Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
                Logger.LogInfo("[纠缠之缘] 插件加载完成，等待 GameAPP 初始化后注册植物。");

                // 场景切换时清理残留的视频与协程
                try
                {
                    var sceneChangedHandler = DelegateSupport.ConvertDelegate<UnityAction<Scene, Scene>>(new Action<Scene, Scene>(OnSceneChanged));
                    SceneManager.add_activeSceneChanged(sceneChangedHandler);

                    var sceneUnloadedHandler = DelegateSupport.ConvertDelegate<UnityAction<Scene>>(new Action<Scene>(OnSceneUnloaded));
                    SceneManager.add_sceneUnloaded(sceneUnloadedHandler);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"[纠缠之缘] 场景事件注册失败：{ex.Message}");
                }
                
                // 检查 GameAPP 是否已经初始化
                if (GameAPP.gameAPP != null)
                {
                    Logger.LogWarning("[纠缠之缘] GameAPP 已经初始化，尝试直接注册植物...");
                    GameAppAwakePatch.TryRegisterPlant();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[纠缠之缘] 插件加载失败: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        private static void OnSceneChanged(Scene oldScene, Scene newScene)
        {
            Core.Logger?.LogInfo("[纠缠之缘] 场景切换，清理所有视频和协程");
            GoldSunflowerSuperSkillPatch.StopAllVideosAndCoroutines();
        }

        private static void OnSceneUnloaded(Scene scene)
        {
            Core.Logger?.LogInfo("[纠缠之缘] 场景卸载，清理所有视频和协程");
            GoldSunflowerSuperSkillPatch.StopAllVideosAndCoroutines();
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
                    Logger?.LogError($"[纠缠之缘] 未找到嵌入资源: {bundleName}");
                    return null;
                }

                using var stream = assembly.GetManifestResourceStream(matchedName);
                if (stream == null)
                {
                    Logger?.LogError($"[纠缠之缘] 无法读取嵌入资源流: {matchedName}");
                    return null;
                }

                var bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);
                CachedBundle = AssetBundle.LoadFromMemory(bytes);
                return CachedBundle;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"[纠缠之缘] 加载嵌入资源失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 概率状态类，用于跟踪每个植物的抽卡概率
        /// </summary>
        internal class ProbabilityState
        {
            public float BlueProbability = 80.0f;
            public float PurpleProbability = 15.0f;
            public float GoldProbability = 4.0f;
            public float SuperGoldProbability = 1.0f;

            public void Reset()
            {
                BlueProbability = 80.0f;
                PurpleProbability = 15.0f;
                GoldProbability = 4.0f;
                SuperGoldProbability = 1.0f;
            }

            public void IncrementProbabilities()
            {
                const float transferAmount = 1.0f;
                if (BlueProbability >= transferAmount)
                {
                    BlueProbability -= transferAmount;
                    const float halfTransfer = transferAmount * 0.5f;
                    PurpleProbability += halfTransfer;
                    GoldProbability += halfTransfer;
                }
            }
        }
    }
}