using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Il2CppGameObjectList = Il2CppSystem.Collections.Generic.List<UnityEngine.GameObject>;

namespace Wish.BepInEx
{
    [HarmonyPatch(typeof(GameAPP), "Awake")]
    internal static class GameAppAwakePatch
    {
        private static bool _registered = false;

        [HarmonyPostfix]
        private static void Postfix()
        {
            Core.Logger?.LogInfo("[纠缠之缘] GameAPP.Awake Postfix 被调用");
            TryRegisterPlant();
        }

        internal static void TryRegisterPlant()
        {
            if (_registered)
            {
                Core.Logger?.LogInfo("[纠缠之缘] 植物已注册，跳过");
                return;
            }

            try
            {
                Core.Logger?.LogInfo("[纠缠之缘] 开始注册植物...");
                
                AssetBundle? assetBundle = Core.LoadEmbeddedAssetBundle(Core.BundleName);
                if (assetBundle == null)
                {
                    Core.Logger?.LogError("[纠缠之缘] 资源包加载失败");
                    return;
                }

                Core.Logger?.LogInfo("[纠缠之缘] AssetBundle 加载成功");

                GameObject? prefab = assetBundle.LoadAsset("WishPrefab")?.TryCast<GameObject>();
                GameObject? preview = assetBundle.LoadAsset("WishPreview")?.TryCast<GameObject>();

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
                        else if (prefab == null)
                            prefab = go;
                        else if (preview == null)
                            preview = go;
                    }
                }

                if (prefab == null)
                {
                    Core.Logger?.LogError("[纠缠之缘] 预制体加载失败");
                    return;
                }

                if (preview == null)
                {
                    Core.Logger?.LogError("[纠缠之缘] 预览图加载失败");
                    return;
                }

                Core.Logger?.LogInfo("[纠缠之缘] 成功加载预制体与预览图");

                ManualRegisterPlant(prefab, preview);
                RegisterColorfulCard();

                _registered = true;
                Core.Logger?.LogInfo($"[纠缠之缘] 纠缠之缘植物注册完成，植物 ID: {Core.PlantId}");
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[纠缠之缘] 注册植物失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void ManualRegisterPlant(GameObject prefab, GameObject preview)
        {
            var plantType = (PlantType)Core.PlantId;
            var res = GameAPP.resourcesManager;

            Core.Logger?.LogInfo("[纠缠之缘] 开始注册植物到 ResourcesManager...");

            // 设置预制体标签
            prefab.tag = "Plant";
            preview.tag = "Preview";

            // 添加 GoldSunflower 组件
            var goldSunflower = prefab.GetComponent<GoldSunflower>();
            if (goldSunflower == null)
            {
                try
                {
                    goldSunflower = prefab.AddComponent<GoldSunflower>();
                    Core.Logger?.LogInfo("[纠缠之缘] 成功添加 GoldSunflower 组件");
                }
                catch (Exception ex)
                {
                    Core.Logger?.LogError($"[纠缠之缘] 添加 GoldSunflower 组件失败: {ex.Message}");
                }
            }

            if (goldSunflower != null)
            {
                goldSunflower.thePlantType = plantType;
            }

            res.plantPrefabs[plantType] = prefab;

            if (!res.allPlants.Contains(plantType))
                res.allPlants.Add(plantType);

            if (!res._plantPrefabs.ContainsKey(plantType))
            {
                var list = new Il2CppGameObjectList(1);
                list.Add(prefab);
                res._plantPrefabs.Add(plantType, list);
            }

            res.plantPreviews[plantType] = preview;

            if (!res._plantPreviews.ContainsKey(plantType))
            {
                var list = new Il2CppGameObjectList(1);
                list.Add(preview);
                res._plantPreviews.Add(plantType, list);
            }

            Core.Logger?.LogInfo("[纠缠之缘] 植物已注册到 ResourcesManager");

            // 注册 PlantData
            try
            {
                EnsurePlantDataCapacity(Core.PlantId);

                var data = Activator.CreateInstance(typeof(PlantDataLoader.PlantData_)) as PlantDataLoader.PlantData_;
                if (data != null)
                {
                    data.field_Public_PlantType_0 = plantType;
                    data.field_Public_Int32_0 = 888;     // hp (韧性)
                    data.field_Public_Int32_1 = 648;     // sun cost (花费阳光)
                    data.field_Public_Single_0 = 0f;     // attack interval
                    data.field_Public_Single_1 = 0f;     // produce interval
                    data.field_Public_Single_2 = 60f;    // cd (卡槽冷却60秒)
                    data.attackDamage = 0;

                    if (PlantDataLoader.plantData != null && PlantDataLoader.plantData.Length > Core.PlantId)
                        PlantDataLoader.plantData[Core.PlantId] = data;

                    PlantDataLoader.plantDatas[plantType] = data;
                    Core.Logger?.LogInfo("[纠缠之缘] PlantData 注册成功");
                }
            }
            catch (Exception ex)
            {
                Core.Logger?.LogWarning($"[纠缠之缘] 写入 PlantData 失败: {ex.Message}");
            }
        }

        private static void EnsurePlantDataCapacity(int plantId)
        {
            try
            {
                var oldArr = PlantDataLoader.plantData;
                var needed = plantId + 1;
                if (oldArr != null && oldArr.Length > plantId)
                    return;

                var newLen = oldArr == null ? needed : Math.Max(needed, oldArr.Length * 2);
                var newArr = new Il2CppReferenceArray<PlantDataLoader.PlantData_>(newLen);

                if (oldArr != null)
                {
                    int copyLen = oldArr.Length;
                    for (int i = 0; i < copyLen; i++)
                    {
                        newArr[i] = oldArr[i];
                    }
                }

                PlantDataLoader.plantData = newArr;
                Core.Logger?.LogInfo($"[纠缠之缘] PlantData 扩容至 {newLen}");
            }
            catch (Exception ex)
            {
                Core.Logger?.LogWarning($"[纠缠之缘] PlantData 扩容失败: {ex.Message}");
            }
        }

        private static void RegisterColorfulCard()
        {
            try
            {
                CustomCardRegistry.RegisterToColorfulCards((PlantType)Core.PlantId);
                Core.Logger?.LogInfo("[纠缠之缘] 彩卡注册成功");
            }
            catch (Exception ex)
            {
                Core.Logger?.LogWarning($"[纠缠之缘] 注册彩卡失败: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(SeedLibrary), "Start")]
    internal static class SeedLibraryStartPatch
    {
        [HarmonyPostfix]
        private static void Postfix(SeedLibrary __instance)
        {
            try
            {
                var colorfulCardTemplate = CustomCardRegistry.GetColorfulCardGameObject();
                if (colorfulCardTemplate == null)
                    return;

                var selectedCards = new List<PlantType>();
                GameObject? seedGroup = null;

                if (Board.Instance != null && !Board.Instance.isIZ)
                {
                    seedGroup = InGameUI.Instance?.SeedBank?.transform.GetChild(0)?.gameObject;
                }
                else if (Board.Instance != null && Board.Instance.isIZ)
                {
                    seedGroup = InGameUI_IZ.Instance?.transform.FindChild("SeedBank/SeedGroup")?.gameObject;
                }

                if (seedGroup != null)
                {
                    for (int i = 0; i < seedGroup.transform.childCount; i++)
                    {
                        var child = seedGroup.transform.GetChild(i).gameObject;
                        if (child.transform.childCount > 0)
                        {
                            var cardUI = child.transform.GetChild(0).GetComponent<CardUI>();
                            if (cardUI != null)
                            {
                                selectedCards.Add(cardUI.thePlantType);
                            }
                        }
                    }
                }

                var createdCards = new Dictionary<PlantType, List<Transform>>();

                foreach (var kvp in CustomCardRegistry.CustomCards)
                {
                    var plantType = kvp.Key;
                    var parentGetters = kvp.Value;

                    foreach (var getParent in parentGetters)
                    {
                        var parentTransform = getParent();
                        if (parentTransform == null)
                            continue;

                        if (createdCards.ContainsKey(plantType) && createdCards[plantType].Contains(parentTransform))
                            continue;

                        var cardGO = UnityEngine.Object.Instantiate(colorfulCardTemplate, parentTransform);
                        if (cardGO == null)
                            continue;

                        cardGO.SetActive(true);
                        cardGO.transform.position = colorfulCardTemplate.transform.position;
                        cardGO.transform.localPosition = colorfulCardTemplate.transform.localPosition;
                        cardGO.transform.localScale = colorfulCardTemplate.transform.localScale;
                        cardGO.transform.localRotation = colorfulCardTemplate.transform.localRotation;

                        var iconImage = cardGO.transform.GetChild(0)?.GetChild(0)?.GetComponent<Image>();
                        if (iconImage != null && GameAPP.resourcesManager.plantPreviews.ContainsKey(plantType))
                        {
                            var previewObj = GameAPP.resourcesManager.plantPreviews[plantType];
                            if (previewObj != null)
                            {
                                var spriteRenderer = previewObj.GetComponent<SpriteRenderer>();
                                if (spriteRenderer != null)
                                {
                                    iconImage.sprite = spriteRenderer.sprite;
                                    iconImage.SetNativeSize();
                                }
                            }
                        }

                        var costText = cardGO.transform.GetChild(0)?.GetChild(1)?.GetComponent<TextMeshProUGUI>();
                        if (costText != null && PlantDataLoader.plantDatas.ContainsKey(plantType))
                        {
                            var plantData = PlantDataLoader.plantDatas[plantType];
                            if (plantData != null)
                            {
                                costText.text = plantData.field_Public_Int32_1.ToString();
                            }
                        }

                        var cardUI = cardGO.transform.GetChild(1)?.GetComponent<CardUI>();
                        if (cardUI != null)
                        {
                            cardUI.gameObject.SetActive(true);
                            Mouse.Instance?.ChangeCardSprite(plantType, cardUI);
                            
                            var boxCollider = cardGO.transform.GetChild(1)?.GetComponent<BoxCollider2D>();
                            if (boxCollider != null)
                                boxCollider.enabled = true;

                            var iconRect = cardGO.transform.GetChild(0)?.GetChild(0)?.GetComponent<RectTransform>();
                            var cardRect = cardGO.transform.GetChild(1)?.GetChild(0)?.GetComponent<RectTransform>();
                            if (iconRect != null && cardRect != null)
                            {
                                iconRect.localScale = cardRect.localScale;
                                iconRect.sizeDelta = cardRect.sizeDelta;
                            }

                            cardUI.thePlantType = plantType;
                            cardUI.theSeedType = (int)plantType;

                            if (PlantDataLoader.plantDatas.ContainsKey(plantType))
                            {
                                var plantData = PlantDataLoader.plantDatas[plantType];
                                if (plantData != null)
                                {
                                    cardUI.theSeedCost = plantData.field_Public_Int32_1;
                                    cardUI.fullCD = plantData.field_Public_Single_2;
                                }
                            }

                            if (selectedCards.Contains(plantType))
                            {
                                cardGO.transform.GetChild(1)?.gameObject.SetActive(false);
                            }
                        }

                        if (!createdCards.ContainsKey(plantType))
                        {
                            createdCards.Add(plantType, new List<Transform> { parentTransform });
                        }
                        else
                        {
                            createdCards[plantType].Add(parentTransform);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[纠缠之缘] 创建彩卡UI失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 彩卡复制功能
    /// </summary>
    [HarmonyPatch(typeof(CardUI), nameof(CardUI.Start))]
    internal static class CardUIStartPatch
    {
        private const int MaxCopies = 14;
        internal static int Spawned = 0;

        private static readonly PlantType TargetType = (PlantType)Core.PlantId;

        [HarmonyPostfix]
        private static void Postfix(CardUI __instance)
        {
            try
            {
                if (__instance.thePlantType != TargetType)
                    return;

                if (Spawned >= MaxCopies)
                    return;

                var parent = __instance.transform.parent;
                var pos = __instance.transform.position;
                
                var clone = UnityEngine.Object.Instantiate(__instance.gameObject, parent);
                clone.transform.position = pos;

                var fullCD = __instance.fullCD;
                __instance.CD = fullCD;
                
                var cloneCard = clone.GetComponent<CardUI>();
                if (cloneCard != null)
                {
                    cloneCard.CD = fullCD;
                }

                Spawned++;
            }
            catch (Exception ex)
            {
                Core.Logger?.LogWarning($"[纠缠之缘] 复制彩卡失败: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(Board), nameof(Board.Start))]
    internal static class BoardStartPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            CardUIStartPatch.Spawned = 0;
        }
    }

    /// <summary>
    /// 图鉴注册
    /// </summary>
    [HarmonyPatch(typeof(AlmanacPlantMenu))]
    internal static class AlmanacPlantMenuPatch
    {
        private static readonly PlantType TargetType = (PlantType)Core.PlantId;
        private static readonly string AlmanacTitle = $"纠缠之缘 ({Core.PlantId})";
        private static readonly string AlmanacDescription =
            "花费1600金钱进行抽卡。\n\n" +
            "<color=#3D1400>作者：</color><color=red>梧萱梦汐X</color>\n" +
            "<color=#3D1400>花费：</color><color=red>648 阳光</color>\n" +
            "<color=#3D1400>韧性：</color><color=red>888</color>\n" +
            "<color=#3D1400>冷却：</color><color=red>60 秒（卡槽冷却）</color>\n" +
            "<color=#3D1400>特点：</color><color=red>彩卡</color>\n\n" +
            "<color=#3D1400>消耗1600金钱抽取植物卡片，技能冷却10秒</color>\n\n" +
            "<color=#3D1400>抽卡概率（初始）：</color>\n" +
            "<color=red>80% 蓝卡：抽取惊喜礼盒卡片</color>\n" +
            "<color=red>15% 紫卡：掉落随机超极植物卡片</color>\n" +
            "<color=red>4% 金卡：随机究极植物卡片</color>\n" +
            "<color=red>1% 捕获明光：惊喜礼盒卡片+随机究极植物卡片，并消除一条僵尸词条或者获得一条植物词条</color>\n\n" +
            "<color=#3D1400>概率递增机制：</color>\n" +
            "<color=red>每抽取一次，如果没出金卡或者捕获明光，蓝卡将1%的概率分给紫卡和金卡（各0.5%），以此类推，直到抽出了金卡或捕获明光，然后重置概率。</color>";

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
            }
            catch (Exception ex)
            {
                Core.Logger?.LogWarning($"[纠缠之缘] 注册图鉴文本失败: {ex.Message}");
            }
        }
    }
}
