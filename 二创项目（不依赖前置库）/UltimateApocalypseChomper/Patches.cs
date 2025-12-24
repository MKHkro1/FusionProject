using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Il2CppGameObjectList = Il2CppSystem.Collections.Generic.List<UnityEngine.GameObject>;

namespace UltimateApocalypseChomper.BepInEx
{
    /// <summary>
    /// TravelMgr.Awake 补丁：注册旅行词条
    /// </summary>
    [HarmonyPatch(typeof(TravelMgr))]
    internal static class TravelMgrAwakePatch
    {
        private static readonly PlantType TargetPlantType = (PlantType)Core.PlantId;

        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        public static void PostAwake(TravelMgr __instance)
        {
            try
            {
                Core.PluginLog?.LogInfo("[究极天启樱龙] TravelMgr.Awake Postfix 被调用");

                // 检查 ultimateBuffs 是否可用
                if (TravelMgr.ultimateBuffs == null)
                {
                    Core.PluginLog?.LogWarning("[究极天启樱龙] TravelMgr.ultimateBuffs 为空，跳过词条注册");
                    return;
                }

                // 获取当前词条数量作为新词条的起始ID
                if (Core.TheNewTravelId == 0)
                {
                    Core.TheNewTravelId = TravelMgr.ultimateBuffs.Count;
                    Core.PluginLog?.LogInfo($"[究极天启樱龙] 新词条起始ID: {Core.TheNewTravelId}");
                }

                // 注册词条到 ultimateBuffs 字典
                for (int i = 0; i < Core.UltimateBuffTexts.Count; i++)
                {
                    TravelMgr.ultimateBuffs[Core.TheNewTravelId + i] = Core.UltimateBuffTexts[i];
                }

                // 设置词条ID
                Core.TravelBuffGluttonyId = Core.TheNewTravelId;
                Core.TravelBuffJudgementId = Core.TheNewTravelId + 1;

                Core.PluginLog?.LogInfo($"[究极天启樱龙] 词条注册成功 - 饕餮巨嘴ID: {Core.TravelBuffGluttonyId}, 天启神罚ID: {Core.TravelBuffJudgementId}");

                // 扩展 ultimateUpgrades 数组（加锁保护）
                if (__instance.ultimateUpgrades != null)
                {
                    int newUltimateSize = __instance.ultimateUpgrades.Length + Core.UltimateBuffTexts.Count;
                    int[] newUltimateUpgrades = new int[newUltimateSize];
                    Array.Copy(__instance.ultimateUpgrades, newUltimateUpgrades, __instance.ultimateUpgrades.Length);
                    __instance.ultimateUpgrades = newUltimateUpgrades;
                    Core.PluginLog?.LogInfo($"[究极天启樱龙] ultimateUpgrades 数组扩展至 {newUltimateSize}");
                }

                // 添加植物到强究极植物列表
                if (TravelMgr.allStrongUltimtePlant != null && !TravelMgr.allStrongUltimtePlant.Contains(TargetPlantType))
                {
                    TravelMgr.allStrongUltimtePlant.Add(TargetPlantType);
                    Core.PluginLog?.LogInfo("[究极天启樱龙] 已添加到 allStrongUltimtePlant 列表");
                }
            }
            catch (Exception ex)
            {
                Core.PluginLog?.LogError($"[究极天启樱龙] TravelMgr.Awake 补丁失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// GetUltimateText 前缀补丁：返回自定义词条文本
        /// </summary>
        [HarmonyPatch("GetUltimateText")]
        [HarmonyPrefix]
        public static bool PrefixGetUltimateText(int index, ref string __result)
        {
            // 检查是否是我们注册的词条
            if (Core.TheNewTravelId > 0 && 
                index >= Core.TheNewTravelId && 
                index < Core.TheNewTravelId + Core.UltimateBuffTexts.Count)
            {
                __result = Core.UltimateBuffTexts[index - Core.TheNewTravelId];
                return false; // 跳过原方法
            }
            return true; // 继续执行原方法
        }
    }

    /// <summary>
    /// GameAPP.Awake 补丁：注册植物
    /// </summary>
    [HarmonyPatch(typeof(GameAPP), "Awake")]
    internal static class GameAppAwakePatch
    {
        private static bool _registered = false;

        [HarmonyPostfix]
        private static void Postfix()
        {
            Core.PluginLog?.LogInfo("[究极天启樱龙] GameAPP.Awake Postfix 被调用");
            TryRegisterPlant();
        }

        internal static void TryRegisterPlant()
        {
            if (_registered)
            {
                Core.PluginLog?.LogInfo("[究极天启樱龙] 植物已注册，跳过");
                return;
            }

            try
            {
                Core.PluginLog?.LogInfo("[究极天启樱龙] 开始注册植物...");

                // 加载 AssetBundle
                AssetBundle? assetBundle = Core.LoadEmbeddedAssetBundle(Core.BundleName);
                if (assetBundle == null)
                {
                    Core.PluginLog?.LogError("[究极天启樱龙] AssetBundle 加载失败");
                    return;
                }

                // 加载预制体和预览图
                GameObject? prefab = assetBundle.LoadAsset("UltimateApocalypseChomperPrefab")?.TryCast<GameObject>();
                GameObject? preview = assetBundle.LoadAsset("UltimateApocalypseChomperPreview")?.TryCast<GameObject>();

                // 如果找不到指定名称，尝试加载所有资源
                if (prefab == null || preview == null)
                {
                    var assets = assetBundle.LoadAllAssets();
                    foreach (var asset in assets)
                    {
                        var go = asset.TryCast<GameObject>();
                        if (go == null) continue;

                        if (prefab == null && go.name.Contains("Prefab", StringComparison.OrdinalIgnoreCase))
                            prefab = go;
                        else if (preview == null && go.name.Contains("Preview", StringComparison.OrdinalIgnoreCase))
                            preview = go;
                    }
                }

                if (prefab == null)
                {
                    Core.PluginLog?.LogError("[究极天启樱龙] 预制体加载失败");
                    var allAssets = assetBundle.GetAllAssetNames();
                    Core.PluginLog?.LogInfo($"[究极天启樱龙] 可用资源: {string.Join(", ", allAssets)}");
                    return;
                }

                if (preview == null)
                {
                    Core.PluginLog?.LogError("[究极天启樱龙] 预览图加载失败");
                    return;
                }

                Core.PluginLog?.LogInfo("[究极天启樱龙] 资源加载成功");

                // 注册植物
                ManualRegisterPlant(prefab, preview);

                // 注册旅行词条
                Core.RegisterTravelBuffs();

                _registered = true;
                Core.PluginLog?.LogInfo($"[究极天启樱龙] 植物注册完成，ID: {Core.PlantId}");
            }
            catch (Exception ex)
            {
                Core.PluginLog?.LogError($"[究极天启樱龙] 注册植物失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void ManualRegisterPlant(GameObject prefab, GameObject preview)
        {
            var plantType = (PlantType)Core.PlantId;
            var res = GameAPP.resourcesManager;

            // 设置标签
            prefab.tag = "Plant";
            preview.tag = "Preview";

            // 设置缩放
            prefab.transform.localScale = Vector3.one * 0.35f;
            preview.transform.localScale = Vector3.one * 0.3f;

            // 添加自定义组件
            var customComponent = prefab.GetComponent<UltimateApocalypseChomperComponent>();
            if (customComponent == null)
            {
                customComponent = prefab.AddComponent<UltimateApocalypseChomperComponent>();
                Core.PluginLog?.LogInfo("[究极天启樱龙] 自定义组件添加成功");
            }

            // 添加 UltimateChomper 基类组件
            var baseComponent = prefab.GetComponent<UltimateChomper>();
            if (baseComponent == null)
            {
                baseComponent = prefab.AddComponent<UltimateChomper>();
                Core.PluginLog?.LogInfo("[究极天启樱龙] UltimateChomper 组件添加成功");
            }
            baseComponent.thePlantType = plantType;
            baseComponent.thePlantMaxHealth = Core.PlantToughness;
            baseComponent.thePlantHealth = Core.PlantToughness;
            baseComponent.attackDamage = Core.BaseAttackDamage;

            // 设置坚果属性
            var plantTag = baseComponent.plantTag;
            plantTag.nutPlant = true;
            plantTag.tallNutPlant = true;
            baseComponent.plantTag = plantTag;

            // 设置 axis 引用
            var axisTransform = prefab.transform.Find("axis") ?? prefab.transform.Find("Axis") ?? prefab.transform.Find("Shoot") ?? prefab.transform.Find("shoot");
            if (axisTransform == null)
            {
                var axisObj = new GameObject("axis");
                axisObj.transform.SetParent(prefab.transform);
                axisObj.transform.localPosition = Vector3.zero;
                axisTransform = axisObj.transform;
            }
            baseComponent.axis = axisTransform;

            // 注册预制体
            res.plantPrefabs[plantType] = prefab;

            if (!res.allPlants.Contains(plantType))
                res.allPlants.Add(plantType);

            if (!res._plantPrefabs.ContainsKey(plantType))
            {
                var list = new Il2CppGameObjectList(1);
                list.Add(prefab);
                res._plantPrefabs.Add(plantType, list);
            }

            // 注册预览图
            res.plantPreviews[plantType] = preview;

            if (!res._plantPreviews.ContainsKey(plantType))
            {
                var list = new Il2CppGameObjectList(1);
                list.Add(preview);
                res._plantPreviews.Add(plantType, list);
            }

            Core.PluginLog?.LogInfo("[究极天启樱龙] 预制体注册完成");

            // 注册 PlantData
            RegisterPlantData(plantType);

            // 注册融合配方
            RegisterFusionRecipe();

            // 设置植物类型标记
            SetPlantTypeFlags(plantType);
        }

        /// <summary>
        /// 注册融合配方：究极樱桃战神(903) + 究极樱桃战神(903) = 究极天启樱龙(2039)
        /// </summary>
        private static void RegisterFusionRecipe()
        {
            try
            {
                var targetType = (PlantType)Core.PlantId;
                var ingredient = (PlantType)903; // 究极樱桃战神

                // 使用 MixData.AddDataUnordered 注册融合配方
                MixData.AddDataUnordered(ingredient, ingredient, targetType);

                Core.PluginLog?.LogInfo($"[究极天启樱龙] 融合配方注册成功: 903 + 903 = {Core.PlantId}");
            }
            catch (Exception ex)
            {
                Core.PluginLog?.LogError($"[究极天启樱龙] 融合配方注册失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void RegisterPlantData(PlantType plantType)
        {
            int plantId = (int)plantType;

            try
            {
                EnsurePlantDataCapacity(plantId);

                var data = new PlantDataLoader.PlantData_();
                data.field_Public_PlantType_0 = plantType;
                data.field_Public_Int32_0 = Core.PlantToughness;  // 韧性
                data.field_Public_Int32_1 = Core.PlantSunCost;    // 阳光花费
                data.field_Public_Single_0 = 1.75f;               // 攻击间隔
                data.field_Public_Single_1 = 0f;                  // 生产间隔
                data.field_Public_Single_2 = Core.PlantCooldown;  // 冷却时间
                data.attackDamage = Core.BaseAttackDamage;

                if (PlantDataLoader.plantData != null && PlantDataLoader.plantData.Length > plantId)
                    PlantDataLoader.plantData[plantId] = data;

                PlantDataLoader.plantDatas[plantType] = data;

                Core.PluginLog?.LogInfo("[究极天启樱龙] PlantData 注册成功");
            }
            catch (Exception ex)
            {
                Core.PluginLog?.LogWarning($"[究极天启樱龙] PlantData 注册失败: {ex.Message}");
            }
        }

        private static void EnsurePlantDataCapacity(int plantId)
        {
            var oldArr = PlantDataLoader.plantData;
            var needed = plantId + 1;

            if (oldArr != null && oldArr.Length > plantId)
                return;

            var newLen = oldArr == null ? needed : Math.Max(needed, oldArr.Length * 2);
            var newArr = new Il2CppReferenceArray<PlantDataLoader.PlantData_>(newLen);

            if (oldArr != null)
            {
                for (int i = 0; i < oldArr.Length; i++)
                    newArr[i] = oldArr[i];
            }

            PlantDataLoader.plantData = newArr;
            Core.PluginLog?.LogInfo($"[究极天启樱龙] PlantData 数组扩容至 {newLen}");
        }

        private static void SetPlantTypeFlags(PlantType plantType)
        {
            // 不调用 TypeMgr.IsNut，避免触发其他插件的有问题补丁
            // 如果需要将此植物标记为坚果类，应该添加自己的 TypeMgr.IsNut 补丁
            Core.PluginLog?.LogInfo("[究极天启樱龙] 植物类型标记设置完成");
        }
    }


    /// <summary>
    /// 图鉴注册
    /// </summary>
    [HarmonyPatch(typeof(AlmanacPlantMenu))]
    internal static class AlmanacPlantMenuPatch
    {
        private static readonly PlantType TargetType = (PlantType)Core.PlantId;

        private static readonly string AlmanacTitle = "究极天启樱龙（2039）";
        private static readonly string AlmanacDescription =
            "<color=#3D1400>作者：</color><color=#FF0000>梧萱梦汐X、文枭S、HaDemo!Doom?!、神秘樱草限时回归版</color>\n" +
            "<color=#3D1400>定位：</color><color=#FF0000>强究极·究极大嘴花</color>\n" +
            "<color=#3D1400>配方：</color><color=#FF0000>究极樱桃战神（903）+ 究极樱桃战神（903）</color>\n" +
            "<color=#3D1400>基础韧性：</color><color=#FF0000>32000（限伤 500）</color>\n" +
            "<color=#3D1400>阳光消耗：</color><color=#FF0000>1200</color>\n" +
            "<color=#3D1400>开场终结：</color><color=#FF0000>登场 5 秒后自动触发一次吞噬（机制同下），清空前方 1.5 格内的可吞僵尸并释放樱桃爆炸。</color>\n" +
            "<color=#3D1400>啃咬判定：</color><color=#FF0000>每 1.75 秒对 1.5 格范围内所有僵尸造成 4000 + [6 + 0.4 × 吞噬层数]% 最大生命值的伤害，每次啃咬回复等量生命，并发射造成 30% 伤害的爆裂樱桃子弹并附带 3% 伤害的裂片溅射。</color>\n" +
            "<color=#3D1400>吐息溅射：</color><color=#FF0000>爆裂子弹溅射范围为半径 1.5 格且无衰减，裂片樱桃继承火豆式溅射判定。</color>\n" +
            "<color=#3D1400>吞噬机制：</color><color=#FF0000>初次吞噬冷却 5 秒，其后吞噬时会吞噬 1.5 格范围内所有可吞僵尸并释放伤害为 50% 咬击总伤害的樱桃爆炸，回复（32000 + 范围内僵尸韧性总和）生命，吞噬冷却 = 8 + 3×ln(僵尸总血量)（≤40 秒）。</color>\n" +
            "<color=#3D1400>词条（饕餮巨嘴）：</color><color=#FF0000>攻击与索敌范围延长至 3 格，吞噬冷却 = 5 + ln(僵尸总血量)（≤15 秒）。</color>\n" +
            "<color=#3D1400>词条（天启神罚）：</color><color=#FF0000>所有回复 ×3，并且不死期间造成的最终伤害 ×4。</color>\n" +
            "<color=#3D1400>不死特性：</color><color=#FF0000>受到致死伤害时触发 5 秒\"不死\"状态（血量最低 1 点），结束后进入 5 秒冷却。</color>\n" +
            "<color=#3D1400>宝开语：</color><color=#FF0000>\"祂是世上最强的存在，当大地异动、熔岩迸发之时，就是祂现世的象征……\"随着最后一句台词被念完，究极天启樱龙也完成了本场拍摄——作为植物界的头号明星，究极天启樱龙十分在意自己的形象，并且在拍戏的时候，坚决使用自己的化妆师，别问他这是为什么</color>";

        [HarmonyPatch(nameof(AlmanacPlantMenu.InitNameAndInfoFromJson))]
        [HarmonyPostfix]
        public static void PostInitNameAndInfoFromJson()
        {
            try
            {
                var plantInfo = new AlmanacPlantBank.PlantInfo
                {
                    name = AlmanacTitle,
                    info = AlmanacDescription
                };
                AlmanacPlantMenu.PlantAlmanacData[TargetType] = plantInfo;
                Core.PluginLog?.LogInfo("[究极天启樱龙] 图鉴文本注册成功");
            }
            catch (Exception ex)
            {
                Core.PluginLog?.LogWarning($"[究极天启樱龙] 图鉴文本注册失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Board.Start 补丁：重置状态
    /// </summary>
    [HarmonyPatch(typeof(Board), nameof(Board.Start))]
    internal static class BoardStartPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            // 每局游戏开始时可以重置状态
        }
    }

    /// <summary>
    /// 判断是否为究极植物
    /// </summary>
    [HarmonyPatch(typeof(Lawnf), "IsUltiPlant")]
    internal static class LawnfIsUltiPlantPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref PlantType thePlantType, ref bool __result)
        {
            if (thePlantType == (PlantType)Core.PlantId)
            {
                __result = true;
            }
        }
    }

    /// <summary>
    /// 判断是否为坚果类植物
    /// </summary>
    [HarmonyPatch(typeof(TypeMgr), "IsNut")]
    internal static class TypeMgrIsNutPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(ref PlantType theSeedType, ref bool __result)
        {
            if (theSeedType == (PlantType)Core.PlantId)
            {
                __result = true;
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// 判断是否为高坚果类植物
    /// </summary>
    [HarmonyPatch(typeof(TypeMgr), "IsTallNut")]
    internal static class TypeMgrIsTallNutPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(ref PlantType theSeedType, ref bool __result)
        {
            if (theSeedType == (PlantType)Core.PlantId)
            {
                __result = true;
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// 获取卡片等级
    /// </summary>
    [HarmonyPatch(typeof(TreasureData), "GetCardLevel")]
    internal static class TreasureDataGetCardLevelPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref PlantType thePlantType, ref CardLevel __result)
        {
            if (thePlantType == (PlantType)Core.PlantId)
            {
                __result = (CardLevel)4; // 究极卡片等级
            }
        }
    }
}
