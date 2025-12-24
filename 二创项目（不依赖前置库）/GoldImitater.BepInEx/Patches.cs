using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Il2CppGameObjectList = Il2CppSystem.Collections.Generic.List<UnityEngine.GameObject>;

namespace GoldImitater.BepInEx
{
    [HarmonyPatch(typeof(GameAPP), "Awake")]
    internal static class GameAppAwakePatch
    {
        private static bool _registered = false;

        [HarmonyPostfix]
        private static void Postfix()
        {
            Core.Logger?.LogInfo("[GoldImitater] GameAPP.Awake Postfix 被调用");
            TryRegisterPlant();
        }

        internal static void TryRegisterPlant()
        {
            if (_registered)
            {
                Core.Logger?.LogInfo("[GoldImitater] 植物已注册，跳过");
                return;
            }

            try
            {
                Core.Logger?.LogInfo("[GoldImitater] 开始注册植物...");
                
                AssetBundle? assetBundle = Core.LoadEmbeddedAssetBundle("goldimitater");
                if (assetBundle == null)
                {
                    Core.Logger?.LogError("[GoldImitater] 资源包 goldimitater 加载失败");
                    return;
                }

                Core.Logger?.LogInfo("[GoldImitater] AssetBundle 加载成功");

                GameObject? prefab = assetBundle.LoadAsset("GoldImitaterPrefab")?.TryCast<GameObject>();
                GameObject? preview = assetBundle.LoadAsset("GoldImitaterPreview")?.TryCast<GameObject>();

                if (prefab == null)
                {
                    Core.Logger?.LogError("[GoldImitater] 预制体 GoldImitaterPrefab 加载失败");
                    return;
                }

                if (preview == null)
                {
                    Core.Logger?.LogError("[GoldImitater] 预览图 GoldImitaterPreview 加载失败");
                    return;
                }

                Core.Logger?.LogInfo("[GoldImitater] 成功加载预制体与预览图");

                ManualRegisterPlant(prefab, preview);
                RegisterColorfulCard();

                _registered = true;
                Core.Logger?.LogInfo($"[GoldImitater] 黄金模仿者植物注册完成，植物 ID: {GoldImitater.PlantID}");
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[GoldImitater] 注册植物失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void ManualRegisterPlant(GameObject prefab, GameObject preview)
        {
            var plantType = (PlantType)GoldImitater.PlantID;
            var res = GameAPP.resourcesManager;

            Core.Logger?.LogInfo("[GoldImitater] 开始注册植物到 ResourcesManager...");

            // 设置预制体标签（必须！游戏通过标签识别植物预制体）
            prefab.tag = "Plant";
            preview.tag = "Preview";
            Core.Logger?.LogInfo("[GoldImitater] 设置预制体标签完成");

            // 列出预制体上的所有组件
            var components = prefab.GetComponents<Component>();
            Core.Logger?.LogInfo($"[GoldImitater] 预制体上的组件数量: {components.Length}");
            foreach (var comp in components)
            {
                if (comp != null)
                {
                    Core.Logger?.LogInfo($"[GoldImitater] - 组件: {comp.GetType().Name}");
                }
            }

            // 添加 GoldImitater 组件（用于接收动画事件 AnimSpawn）
            var goldImitater = prefab.GetComponent<GoldImitater>();
            if (goldImitater == null)
            {
                Core.Logger?.LogInfo("[GoldImitater] 预制体没有 GoldImitater 组件，尝试添加...");
                try
                {
                    goldImitater = prefab.AddComponent<GoldImitater>();
                    Core.Logger?.LogInfo("[GoldImitater] 成功添加 GoldImitater 组件");
                }
                catch (Exception ex)
                {
                    Core.Logger?.LogError($"[GoldImitater] 添加 GoldImitater 组件失败: {ex.Message}");
                }
            }

            // 添加 Imitater 组件（黄金模仿者基于 Imitater 类型）
            // 必须在预制体上添加组件，否则游戏不知道如何处理这个自定义植物
            // 注意：原始代码使用 PeaShooter 作为基础，但我们的变身逻辑需要 Imitater 组件
            var imitater = prefab.GetComponent<Imitater>();
            if (imitater == null)
            {
                Core.Logger?.LogInfo("[GoldImitater] 预制体没有 Imitater 组件，尝试添加...");
                try
                {
                    imitater = prefab.AddComponent<Imitater>();
                    Core.Logger?.LogInfo("[GoldImitater] 成功添加 Imitater 组件");
                }
                catch (Exception ex)
                {
                    Core.Logger?.LogError($"[GoldImitater] 添加 Imitater 组件失败: {ex.Message}");
                }
            }
            
            if (imitater != null)
            {
                imitater.thePlantType = plantType;
                
                // 设置 axis 引用（Imitater 需要这个引用来获取位置）
                var axisTransform = prefab.transform.Find("axis");
                if (axisTransform == null)
                {
                    axisTransform = prefab.transform.Find("Axis");
                }
                if (axisTransform == null)
                {
                    // 如果没有 axis 子对象，创建一个
                    var axisObj = new GameObject("axis");
                    axisObj.transform.SetParent(prefab.transform);
                    axisObj.transform.localPosition = Vector3.zero;
                    axisTransform = axisObj.transform;
                    Core.Logger?.LogInfo("[GoldImitater] 创建了 axis 子对象");
                }
                imitater.axis = axisTransform;
                Core.Logger?.LogInfo("[GoldImitater] Imitater 组件配置完成");
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

            Core.Logger?.LogInfo("[GoldImitater] 植物已注册到 ResourcesManager");

            try
            {
                EnsurePlantDataCapacity(GoldImitater.PlantID);

                var data = Activator.CreateInstance(typeof(PlantDataLoader.PlantData_)) as PlantDataLoader.PlantData_;
                if (data != null)
                {
                    data.field_Public_PlantType_0 = plantType;
                    data.field_Public_Int32_0 = 300;
                    data.field_Public_Int32_1 = 50;
                    data.field_Public_Single_0 = 0f;
                    data.field_Public_Single_1 = 0f;
                    data.field_Public_Single_2 = 15f;
                    data.attackDamage = 0;

                    if (PlantDataLoader.plantData != null && PlantDataLoader.plantData.Length > GoldImitater.PlantID)
                        PlantDataLoader.plantData[GoldImitater.PlantID] = data;

                    PlantDataLoader.plantDatas[plantType] = data;
                    Core.Logger?.LogInfo("[GoldImitater] PlantData 注册成功");
                }
            }
            catch (Exception ex)
            {
                Core.Logger?.LogWarning($"[GoldImitater] 写入 PlantData 失败: {ex.Message}");
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
                Core.Logger?.LogInfo($"[GoldImitater] PlantData 扩容至 {newLen}");
            }
            catch (Exception ex)
            {
                Core.Logger?.LogWarning($"[GoldImitater] PlantData 扩容失败: {ex.Message}");
            }
        }

        private static void RegisterColorfulCard()
        {
            try
            {
                CustomCardRegistry.RegisterToColorfulCards((PlantType)GoldImitater.PlantID);
                Core.Logger?.LogInfo("[GoldImitater] 彩卡注册成功");
            }
            catch (Exception ex)
            {
                Core.Logger?.LogWarning($"[GoldImitater] 注册彩卡失败: {ex.Message}");
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
                Core.Logger?.LogError($"[GoldImitater] 创建彩卡UI失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 彩卡复制功能，让黄金模仿者可以多次选取（最多生成 14 张拷贝）
    /// </summary>
    [HarmonyPatch(typeof(CardUI), nameof(CardUI.Start))]
    internal static class CardUIStartPatch
    {
        private const int MaxCopies = 14;
        internal static int Spawned = 0;

        private static readonly PlantType TargetType = (PlantType)GoldImitater.PlantID;

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
                Core.Logger?.LogWarning($"[GoldImitater] 复制彩卡失败: {ex.Message}");
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

    [HarmonyPatch(typeof(AlmanacPlantMenu))]
    internal static class AlmanacPlantMenuPatch
    {
        private static readonly PlantType TargetType = (PlantType)GoldImitater.PlantID;
        private static readonly string AlmanacTitle = "黄金模仿者 (1931)";
        private static readonly string AlmanacDescription =
            "或许是宝藏呢？\n\n" +
            "<color=#3D1400>贴图作者：</color><color=red>@林秋-AutumnLin</color>\n" +
            "<color=#3D1400>特点：</color><color=red>短时间内变身随机召唤植物或僵尸。</color>\n\n" +
            "花费：<color=red>50</color>\n" +
            "冷却时间：<color=red>15秒</color>";

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
                Core.Logger?.LogWarning($"[GoldImitater] 注册图鉴文本失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Hook Imitater.AnimExplode 方法，让黄金模仿者使用自定义逻辑
    /// </summary>
    [HarmonyPatch(typeof(Imitater), nameof(Imitater.AnimExplode))]
    internal static class ImitaterAnimExplodePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Imitater __instance)
        {
            try
            {
                if (__instance == null)
                    return true;

                Core.Logger?.LogInfo($"[GoldImitater] Imitater.AnimExplode 被调用，植物类型: {__instance.thePlantType}");

                if (__instance.thePlantType != (PlantType)GoldImitater.PlantID)
                    return true;

                Core.Logger?.LogInfo("[GoldImitater] 检测到黄金模仿者，跳过原始逻辑（由 AnimSpawn 处理）");
                return false;
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[GoldImitater] Imitater.AnimExplode 补丁出错: {ex.Message}");
                return true;
            }
        }
    }
}
