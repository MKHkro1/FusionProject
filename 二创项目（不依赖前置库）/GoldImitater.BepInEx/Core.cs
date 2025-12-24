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
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Il2CppGameObjectList = Il2CppSystem.Collections.Generic.List<UnityEngine.GameObject>;

namespace GoldImitater.BepInEx
{
    /// <summary>
    /// 黄金模仿者二创植物插件入口 - 3.2版本适配（不依赖CustomizeLib）
    /// </summary>
    [BepInPlugin("salmon.goldimitater", "GoldImitater", "2.0.0")]
    public class Core : BasePlugin
    {
        internal static ManualLogSource? Logger;
        internal static AssetBundle? CachedBundle;

        public override void Load()
        {
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
                Logger = Log;

                Log.LogInfo("[GoldImitater] 开始加载插件...");

                // 注册 Harmony 补丁
                var harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
                Log.LogInfo($"[GoldImitater] Harmony 补丁已注册，共 {harmony.GetPatchedMethods().Count()} 个方法被补丁");

                // 注册自定义组件类型到 IL2CPP
                ClassInjector.RegisterTypeInIl2Cpp<GoldImitater>();

                Log.LogInfo("[GoldImitater] 插件加载完成，等待 GameAPP 初始化后注册植物。");
                
                // 检查 GameAPP 是否已经初始化
                if (GameAPP.gameAPP != null)
                {
                    Log.LogWarning("[GoldImitater] GameAPP 已经初始化，尝试直接注册植物...");
                    // 如果 GameAPP 已经初始化，直接调用注册逻辑
                    GameAppAwakePatch.TryRegisterPlant();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[GoldImitater] 插件加载失败: {ex.Message}\n{ex.StackTrace}");
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
                    Logger?.LogError($"[GoldImitater] 未找到嵌入资源: {bundleName}");
                    return null;
                }

                using var stream = assembly.GetManifestResourceStream(matchedName);
                if (stream == null)
                {
                    Logger?.LogError($"[GoldImitater] 无法读取嵌入资源流: {matchedName}");
                    return null;
                }

                var bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);
                CachedBundle = AssetBundle.LoadFromMemory(bytes);
                return CachedBundle;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"[GoldImitater] 加载嵌入资源失败: {ex.Message}");
                return null;
            }
        }
    }
}