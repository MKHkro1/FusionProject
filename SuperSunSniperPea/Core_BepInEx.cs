using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using CustomizeLib.BepInEx;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace SuperSunSniperPea
{
    [BepInPlugin("SuperSunSniperPea", "SuperSunSniperPea", "1.0.0")]
    public class Core : BasePlugin
    {
        public override void Load()
        {
            Console.OutputEncoding = Encoding.UTF8;

            Log.LogInfo("SuperSunSniperPea: 开始加载插件...");
            Harmony.CreateAndPatchAll(System.Reflection.Assembly.GetExecutingAssembly());
            Log.LogInfo("SuperSunSniperPea: Harmony补丁已注册");
            ClassInjector.RegisterTypeInIl2Cpp<SuperSunSniperPea>();
            Log.LogInfo("SuperSunSniperPea: 类型已注册到Il2Cpp");

            // 检查程序集中包含的资源
            Log.LogInfo("SuperSunSniperPea: 检查程序集中包含的资源...");
            string[] resourceNames = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames();
            Log.LogInfo($"SuperSunSniperPea: 找到 {resourceNames.Length} 个嵌入资源:");
            foreach (string name in resourceNames)
            {
                Log.LogInfo($"  - {name}");
            }
            AssetBundle.UnloadAllAssetBundles(false);
            AssetBundle assetBundle = CustomCore.GetAssetBundle(System.Reflection.Assembly.GetExecutingAssembly(),
                "SuperSunSniperPea.supersunsniperpea");
            if (assetBundle == null)
            {
                Log.LogError("SuperSunSniperPea: AssetBundle 加载失败，返回 null");
                return;
            }

            Log.LogInfo("SuperSunSniperPea: AssetBundle loaded successfully");

            CustomCore.RegisterCustomPlant<SniperPea, SuperSunSniperPea>(
                SuperSunSniperPea.PlantID,
                assetBundle.GetAsset<GameObject>("IceDoomSniperPeaPrefab"),
                assetBundle.GetAsset<GameObject>("IceDoomSniperPeaPreview"),
                new List<(int, int)>
                {
                    (1109, 1)
                },
                3f, 0f, 400, 300, 0f, 650);

            Log.LogInfo($"SuperSunSniperPea: 植物已注册，PlantID: {SuperSunSniperPea.PlantID}");

            CustomCore.AddPlantAlmanacStrings(
                SuperSunSniperPea.PlantID,
                "阳光狙击豌豆(" + SuperSunSniperPea.PlantID.ToString() + ")",
                "定期狙击僵尸，造成大量伤害，且掉落一些阳光\n\n" +
                "<color=#3D1400>贴图作者：@我是而非</color>\n" +
                "<color=#3D1400>伤害：</color><color=red>400/3秒</color>\n" +
                "<color=#3D1400>特点：</color><color=red>特点同狙击射手，攻击一次掉落25阳光，此外击杀僵尸时掉落僵尸血量*0.1的阳光</color>\n" +
                "<color=#3D1400>特殊技能：</color><color=red>每3秒有50%概率使伤害提升50倍，持续5秒</color>\n" +
                "<color=#3D1400>融合配方：</color><color=red>狙击射手+向日葵</color>\n\n" +
                "<color=#3D1400>(来自群友Oceanø)阳光狙击在面向太阳的时候常常想到，我能不能一枪给他打下来。正因如此他的同行常常按着他的头不让他射。</color>");
        }
    }
}