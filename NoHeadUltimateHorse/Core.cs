using System;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using CustomizeLib.BepInEx;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace NoHeadUltimateHorse.BepInEx
{
    [BepInPlugin("com.noheadultimatehorse.bepinex", "NoHeadUltimateHorse", "1.0.0")]
    public class Core : BasePlugin
    {
        public static Core? Instance { get; private set; }
        public ManualLogSource Logger { get; private set; } = null!;
        public static AssetBundle? NoHeadUltimateHorseBundle { get; private set; }

        public override void Load()
        {
            Instance = this;
            Logger = base.Log;

            try
            {
                Console.OutputEncoding = Encoding.UTF8;
                Logger.LogInfo("究极无头骑士插件: 开始加载...");

                ClassInjector.RegisterTypeInIl2Cpp<NoHeadUltimateHorseComponent>();
                ClassInjector.RegisterTypeInIl2Cpp<UndyingBuffComponent>();
				ClassInjector.RegisterTypeInIl2Cpp<UncappedPlantDamageComponent>();
                Logger.LogInfo("究极无头骑士插件: 组件已注册到Il2Cpp类型系统");

                var harmony = new Harmony("com.noheadultimatehorse.bepinex");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Logger.LogInfo("究极无头骑士插件: Harmony补丁已应用");

                LoadNoHeadUltimateHorseZombie();
            }
            catch (Exception ex)
            {
                Logger.LogError($"究极无头骑士插件: 插件加载失败: {ex.Message}");
                Logger.LogError($"究极无头骑士插件: 错误详情: {ex}");
            }
        }

        private void LoadNoHeadUltimateHorseZombie()
        {
            try
            {
                Logger.LogInfo("究极无头骑士插件: 开始加载究极无头骑士资源...");

                AssetBundle assetBundle = CustomCore.GetAssetBundle(Assembly.GetExecutingAssembly(), "noheadultimatehorse");
                NoHeadUltimateHorseBundle = assetBundle;

                if (assetBundle == null)
                {
                    Logger.LogError("究极无头骑士插件: 无法加载究极无头骑士资源包");
                    return;
                }

                Logger.LogInfo("究极无头骑士插件: 成功加载究极无头骑士资源包");

                GameObject zombiePrefab = assetBundle.GetAsset<GameObject>("NoHeadUltimateHorse");
                if (zombiePrefab == null)
                {
                    Logger.LogError("究极无头骑士插件: 无法从AssetBundle中获取NoHeadUltimateHorse预制体");
                    return;
                }

                Logger.LogInfo("究极无头骑士插件: 成功获取NoHeadUltimateHorse预制体");

                // 注册自定义僵尸类型
                CustomCore.RegisterCustomZombie<UltimateHorse, NoHeadUltimateHorseComponent>(
                    (ZombieType)NoHeadUltimateHorseComponent.ZombieID,
                    zombiePrefab,
                    -1,
                    100,
                    54000,
                    0,
                    0);

                Logger.LogInfo("究极无头骑士插件: 究极无头骑士注册成功");

                // 添加为领袖僵尸
                try
                {
                    var typeMgrExtraType = typeof(CustomCore).GetNestedType("TypeMgrExtra", BindingFlags.Public | BindingFlags.Static);
                    if (typeMgrExtraType != null)
                    {
                        var leaderZombieField = typeMgrExtraType.GetField("LeaderZombie", BindingFlags.Public | BindingFlags.Static);
                        if (leaderZombieField != null)
                        {
                            var leaderZombies = leaderZombieField.GetValue(null) as System.Collections.IList;
                            if (leaderZombies != null)
                            {
                                leaderZombies.Add((ZombieType)NoHeadUltimateHorseComponent.ZombieID);
                                Logger.LogInfo("究极无头骑士插件: 已添加为领袖僵尸");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"究极无头骑士插件: 添加领袖僵尸失败: {ex.Message}");
                }

                // 添加僵尸图鉴
                int zombieID = NoHeadUltimateHorseComponent.ZombieID;
                string zombieName = $"究极无头骑士({zombieID})";
                string almanacText = @"究极无头骑士(450)
黑大帅亚种

<color=#3D1400>韧性：</color><color=red>54000</color>
<color=#3D1400>攻击：</color><color=red>100</color>
<color=#3D1400>特点：</color><color=red>领袖僵尸。免疫击退、冻结、定身、寒冷、蒜毒。每0.1秒回血300。其攻击无视承伤，直接对植物造成真伤。</color>
<color=#3D1400>伴生机制：</color><color=red>黑大帅死亡后出现</color>
<color=#3D1400>入场能力：</color><color=red>入场时无敌3秒，并投掷长斧，对植物造成4次100点伤害，如果没有植物阻挡，最多能直接从第五列右侧出场。</color>
<color=#3D1400>挥砍攻击：</color><color=red>每0.1秒挥砍，对前方两格植物造成4次100点伤害。</color>
<color=#3D1400>解除植物限伤：</color><color=red>被其攻击到的植物的限伤将被取消。</color>
<color=#3D1400>诅咒效果：</color><color=red>被其攻击到的植物，不仅限伤失效，其反诅咒特性也同样失效，且会一直持续保持诅咒效果。</color>

<color=#3D1400>究极无头骑士，拥有强大的战斗能力和辅助能力</color>";

                CustomCore.AddZombieAlmanacStrings(zombieID, zombieName, almanacText);
                Logger.LogInfo("究极无头骑士插件: 究极无头骑士图鉴字符串添加成功");
            }
            catch (Exception ex)
            {
                Logger.LogError($"究极无头骑士插件: 加载究极无头骑士失败: {ex.Message}");
                Logger.LogError($"究极无头骑士插件: 错误详情: {ex}");
            }
        }
    }
}
