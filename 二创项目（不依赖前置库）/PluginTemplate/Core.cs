using System;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace PluginTemplate.BepInEx
{
    /// <summary>
    /// 【模板】二创植物插件入口
    /// 
    /// 使用说明：
    /// 1. 修改 BepInPlugin 特性中的 GUID、名称、版本
    /// 2. 修改命名空间为你的插件名
    /// 3. 修改 BUNDLE_NAME 为你的 AssetBundle 文件名
    /// </summary>
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class Core : BasePlugin
    {
        // ==================== 插件信息配置 ====================
        public const string PLUGIN_GUID = "author.plugintemplate";  // 修改为：作者.插件名
        public const string PLUGIN_NAME = "PluginTemplate";          // 修改为：插件显示名称
        public const string PLUGIN_VERSION = "1.0.0";                // 修改为：版本号
        public const string BUNDLE_NAME = "plugintemplate";          // 修改为：AssetBundle 文件名

        // ==================== 静态成员 ====================
        internal static ManualLogSource? Logger;
        internal static AssetBundle? CachedBundle;

        /// <summary>
        /// 插件加载入口
        /// </summary>
        public override void Load()
        {
            try
            {
                // 设置控制台编码支持中文
                Console.OutputEncoding = Encoding.UTF8;
                Logger = Log;

                Log.LogInfo($"[{PLUGIN_NAME}] 开始加载插件...");

                // 1. 注册 Harmony 补丁
                var harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
                Log.LogInfo($"[{PLUGIN_NAME}] Harmony 补丁已注册，共 {harmony.GetPatchedMethods().Count()} 个方法");

                // 2. 注册自定义组件到 IL2CPP（重要！）
                ClassInjector.RegisterTypeInIl2Cpp<TemplateComponent>();

                Log.LogInfo($"[{PLUGIN_NAME}] 插件加载完成，等待游戏初始化...");

                // 3. 如果游戏已初始化，直接注册植物
                if (GameAPP.gameAPP != null)
                {
                    Log.LogWarning($"[{PLUGIN_NAME}] GameAPP 已初始化，直接注册植物");
                    GameAppAwakePatch.TryRegisterPlant();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[{PLUGIN_NAME}] 插件加载失败: {ex.Message}\n{ex.StackTrace}");
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

                // 查找匹配的嵌入资源
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
                    Logger?.LogError($"[{PLUGIN_NAME}] 未找到嵌入资源: {bundleName}");
                    Logger?.LogInfo($"[{PLUGIN_NAME}] 可用资源: {string.Join(", ", resourceNames)}");
                    return null;
                }

                // 读取资源流
                using var stream = assembly.GetManifestResourceStream(matchedName);
                if (stream == null)
                {
                    Logger?.LogError($"[{PLUGIN_NAME}] 无法读取资源流: {matchedName}");
                    return null;
                }

                // 从内存加载 AssetBundle
                var bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);
                CachedBundle = AssetBundle.LoadFromMemory(bytes);

                Logger?.LogInfo($"[{PLUGIN_NAME}] AssetBundle 加载成功: {bundleName}");
                return CachedBundle;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"[{PLUGIN_NAME}] 加载 AssetBundle 失败: {ex.Message}");
                return null;
            }
        }
    }
}
