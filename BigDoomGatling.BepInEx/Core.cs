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

namespace BigDoomGatling.BepInEx
{
    [BepInPlugin("inf75.bigdoomgatling", "BigDoomGatling", "1.0")]
    public class Core : BasePlugin
    {
        public override void Load()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            ClassInjector.RegisterTypeInIl2Cpp<BigDoomGatling>();
            AssetBundle assetBundle = CustomCore.GetAssetBundle(Assembly.GetExecutingAssembly(), "bigDoomgatling");
            CustomCore.RegisterCustomPlant<BigGatling, BigDoomGatling>(1390, assetBundle.GetAsset<GameObject>("BigDoomGatlingPrefab"), assetBundle.GetAsset<GameObject>("BigDoomGatlingPreview"), new List<ValueTuple<int, int>>(2)
            {
                new ValueTuple<int, int>(1194, 1161),
                new ValueTuple<int, int>(1161, 1194)
            }, 0.1f, 0f, 80, 40000, 15f, 1000);
            CustomCore.AddPlantAlmanacStrings(1390, "Big Doom Gatling", "By VOIDMAN\n<color=#3D1400>");
        }
    }
}
