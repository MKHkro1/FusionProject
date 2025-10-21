using System;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using UnityEngine;
using CustomizeLib.BepInEx;

namespace SolarEmperNutMod
{
    [BepInPlugin("solarempernut.mod", "Solar Emper-nut Mod", "1.0")]
    public class Core : BasePlugin
    {
        // 阳光帝果的植物ID
        private const int SOLAR_EMPER_NUT_ID = 905;
        // 巨型阳光坚果的植物ID
        private const int GIANT_SUN_NUT_ID = 251;

        public override void Load()
        {
            Console.OutputEncoding = Encoding.UTF8;
            
            // 注册阳光帝果的点击事件
            CustomCore.RegisterCustomPlantClickEvent(SOLAR_EMPER_NUT_ID, SolarEmperNutPatches.HandleSolarEmperNutClick);
            
            UnityEngine.Debug.Log("[SolarEmperNutMod] 插件已加载 - 使用CustomCore注册阳光帝果点击事件");
            UnityEngine.Debug.Log($"[SolarEmperNutMod] 已注册阳光帝果(ID: {SOLAR_EMPER_NUT_ID})的点击事件");
        }
    }
}
