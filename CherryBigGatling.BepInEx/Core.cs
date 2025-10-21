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

namespace CherryBigGatling.BepInEx
{
    [BepInPlugin("inf75.cherrybiggatling", "CherryBigGatling", "1.0")]
    public class Core : BasePlugin
    {
        public override void Load()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            ClassInjector.RegisterTypeInIl2Cpp<CherryBigGatling>();
            AssetBundle assetBundle = CustomCore.GetAssetBundle(Assembly.GetExecutingAssembly(), "cherrybiggatling");
            CustomCore.RegisterCustomPlant<BigGatling, CherryBigGatling>(2005, assetBundle.GetAsset<GameObject>("CherryBigGatlingPrefab"), assetBundle.GetAsset<GameObject>("CherryBigGatlingPreview"), new List<ValueTuple<int, int>>(2)
            {
                new ValueTuple<int, int>(2, 1161),
                new ValueTuple<int, int>(1161, 2)
            }, 0.1f, 0f, 500, 1000, 1f, 1000);
            CustomCore.AddPlantAlmanacStrings(2005, "樱桃机枪炮台(2005)", "发射樱桃子弹的巨型机枪炮台\n\n<color=#3D1400>伤害：</color><color=red>500×3/0.1秒</color>\n<color=#3D1400>特点：</color><color=red>发射樱桃子弹对僵尸3×3范围造成500伤害</color>\n<color=#3D1400>融合配方：</color><color=red>巨型机枪豌豆+樱桃炸弹</color>\n\n<color=#3D1400>Boom！\"爆炸就是艺术！\"樱桃机枪炮台从战场上退役下来就一直念叨着，很明显，它做到了。</color>");
        }
    }
}
