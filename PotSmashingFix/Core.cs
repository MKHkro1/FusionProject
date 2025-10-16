using System;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace PotSmashingFix
{
    [BepInPlugin("PotSmashingFix", "PotSmashingFix", "1.0.0")]
    public class Core : BasePlugin
    {
        public override void Load()
        {
            Console.OutputEncoding = Encoding.UTF8;
            UnityEngine.Debug.Log("PotSmashingFix: 开始加载砸罐子修复插件...");

            try
            {
                Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
                UnityEngine.Debug.Log("PotSmashingFix: Harmony补丁已注册");

                       UnityEngine.Debug.Log("PotSmashingFix: 插件加载完成");
                       UnityEngine.Debug.Log("PotSmashingFix: 功能说明:");
                       UnityEngine.Debug.Log("PotSmashingFix: 1. 多个罐子重叠时只砸开第一个罐子");
                       UnityEngine.Debug.Log("PotSmashingFix: 2. 小丑类的爆炸和巨人的砸击无法破坏罐子");
                       UnityEngine.Debug.Log("PotSmashingFix: 3. 土豆炸弹和大炸弹等AOE攻击无法破坏罐子");
                       UnityEngine.Debug.Log("PotSmashingFix: 4. 巨人僵尸忽略罐子，直接向前走");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"PotSmashingFix: 插件加载失败: {ex.Message}");
                UnityEngine.Debug.LogError($"PotSmashingFix: 错误详情: {ex.StackTrace}");
            }
        }
    }
}
