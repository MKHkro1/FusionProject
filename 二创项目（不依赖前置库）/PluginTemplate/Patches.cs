using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Il2CppGameObjectList = Il2CppSystem.Collections.Generic.List<UnityEngine.GameObject>;

namespace PluginTemplate.BepInEx
{
    // ==================== GameAPP.Awake 补丁：注册植物 ====================
    [HarmonyPatch(typeof(GameAPP), "Awake")]
    internal static class GameAppAwakePatch
    {
        private static bool _registered = false;

        [HarmonyPostfix]
        private static void Postfix()
        {
            Core.Logger?.LogInfo($"[{Core.PLUGIN_NAME}] GameAPP.Awake Postfix 被调用");
            TryRegisterPlant();
        }

        internal static void TryRegisterPlant()
        {
            if (_registered)
            {
                Core.Logger?.LogInfo($"[{Core.PLUGIN_NAME}] 植物已注册，跳过");
                return;
            }

            try
            {
                Core.Logger?.LogInfo($"[{Core.PLUGIN_NAME}] 开始注册植物...");

                // 1. 加载 AssetBundle
                AssetBundle? assetBundle = Core.LoadEmbeddedAssetBundle(Core.BUNDLE_NAME);
                if (assetBundle == null)
                {
                    Core.Logger?.LogError($"[{Core.PLUGIN_NAME}] AssetBundle 加载失败");
                    return;
                }

                // 2. 加载预制体和预览图
                // 【修改】将 "TemplatePrefab" 和 "TemplatePreview" 改为你的资源名称
                GameObject? prefab = assetBundle.LoadAsset("TemplatePrefab")?.TryCast<GameObject>();
                GameObject? preview = assetBundle.LoadAsset("TemplatePreview")?.TryCast<GameObject>();

                if (prefab == null)
                {
                    Core.Logger?.LogError($"[{Core.PLUGIN_NAME}] 预制体加载失败");
                    // 列出 AssetBundle 中的所有资源
                    var allAssets = assetBundle.GetAllAssetNames();
                    Core.Logger?.LogInfo($"[{Core.PLUGIN_NAME}] 可用资源: {string.Join(", ", allAssets)}");
                    return;
                }

                if (preview == null)
                {
                    Core.Logger?.LogError($"[{Core.PLUGIN_NAME}] 预览图加载失败");
                    return;
                }

                Core.Logger?.LogInfo($"[{Core.PLUGIN_NAME}] 资源加载成功");

                // 3. 注册植物
                ManualRegisterPlant(prefab, preview);

                // 4. 注册彩卡（可选）
                CustomCardRegistry.RegisterToColorfulCards((PlantType)TemplateComponent.PlantID);

                _registered = true;
                Core.Logger?.LogInfo($"[{Core.PLUGIN_NAME}] 植物注册完成，ID: {TemplateComponent.PlantID}");
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[{Core.PLUGIN_NAME}] 注册植物失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void ManualRegisterPlant(GameObject prefab, GameObject preview)
        {
            var plantType = (PlantType)TemplateComponent.PlantID;
            var res = GameAPP.resourcesManager;

            // ========== 第一步：设置标签 ==========
            prefab.tag = "Plant";
            preview.tag = "Preview";
            Core.Logger?.LogInfo($"[{Core.PLUGIN_NAME}] 标签设置完成");

            // ========== 第二步：添加自定义组件 ==========
            var customComponent = prefab.GetComponent<TemplateComponent>();
            if (customComponent == null)
            {
                customComponent = prefab.AddComponent<TemplateComponent>();
                Core.Logger?.LogInfo($"[{Core.PLUGIN_NAME}] 自定义组件添加成功");
            }

            // ========== 第三步：添加基类组件 ==========
            // 【修改】根据你的植物类型选择合适的基类
            // 可选：PeaShooter, Imitater, Chomper, WallNut, TallNut, Sunflower, Fume 等
            var baseComponent = prefab.GetComponent<PeaShooter>();
            if (baseComponent == null)
            {
                baseComponent = prefab.AddComponent<PeaShooter>();
                Core.Logger?.LogInfo($"[{Core.PLUGIN_NAME}] 基类组件添加成功");
            }
            baseComponent.thePlantType = plantType;

            // ========== 第四步：设置 axis 引用 ==========
            var axisTransform = prefab.transform.Find("axis") ?? prefab.transform.Find("Axis");
            if (axisTransform == null)
            {
                var axisObj = new GameObject("axis");
                axisObj.transform.SetParent(prefab.transform);
                axisObj.transform.localPosition = Vector3.zero;
                axisTransform = axisObj.transform;
                Core.Logger?.LogInfo($"[{Core.PLUGIN_NAME}] 创建了 axis 子对象");
            }
            baseComponent.axis = axisTransform;

            // ========== 第五步：注册预制体 ==========
            res.plantPrefabs[plantType] = prefab;

            // ========== 第六步：添加到 allPlants ==========
            if (!res.allPlants.Contains(plantType))
                res.allPlants.Add(plantType);

            // ========== 第七步：注册到 _plantPrefabs ==========
            if (!res._plantPrefabs.ContainsKey(plantType))
            {
                var list = new Il2CppGameObjectList(1);
                list.Add(prefab);
                res._plantPrefabs.Add(plantType, list);
            }

            // ========== 第八步：注册预览图 ==========
            res.plantPreviews[plantType] = preview;

            if (!res._plantPreviews.ContainsKey(plantType))
            {
                var list = new Il2CppGameObjectList(1);
                list.Add(preview);
                res._plantPreviews.Add(plantType, list);
            }

            Core.Logger?.LogInfo($"[{Core.PLUGIN_NAME}] 预制体注册完成");

            // ========== 第九步：注册 PlantData ==========
            RegisterPlantData(plantType);
        }

        private static void RegisterPlantData(PlantType plantType)
        {
            int plantId = (int)plantType;

            try
            {
                // 确保数组容量
                EnsurePlantDataCapacity(plantId);

                // 创建 PlantData
                var data = new PlantDataLoader.PlantData_();
                
                // 【修改】根据你的植物设置属性
                data.field_Public_PlantType_0 = plantType;  // 植物类型
                data.field_Public_Int32_0 = 300;            // 血量/韧性
                data.field_Public_Int32_1 = 100;            // 阳光花费
                data.field_Public_Single_0 = 0f;            // 浮点参数1
                data.field_Public_Single_1 = 0f;            // 浮点参数2
                data.field_Public_Single_2 = 7.5f;          // 冷却时间（秒）
                data.attackDamage = 20;                     // 攻击伤害

                // 写入数组
                if (PlantDataLoader.plantData != null && PlantDataLoader.plantData.Length > plantId)
                    PlantDataLoader.plantData[plantId] = data;

                // 写入字典
                PlantDataLoader.plantDatas[plantType] = data;

                Core.Logger?.LogInfo($"[{Core.PLUGIN_NAME}] PlantData 注册成功");
            }
            catch (Exception ex)
            {
                Core.Logger?.LogWarning($"[{Core.PLUGIN_NAME}] PlantData 注册失败: {ex.Message}");
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
            Core.Logger?.LogInfo($"[{Core.PLUGIN_NAME}] PlantData 数组扩容至 {newLen}");
        }
    }

    // ==================== SeedLibrary.Start 补丁：创建彩卡 UI ====================
    [HarmonyPatch(typeof(SeedLibrary), "Start")]
    internal static class SeedLibraryStartPatch
    {
        [HarmonyPostfix]
        private static void Postfix(SeedLibrary __instance)
        {
            try
            {
                var colorfulCardTemplate = CustomCardRegistry.GetColorfulCardTemplate();
                if (colorfulCardTemplate == null)
                    return;

                // 获取已选择的卡片
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

                // 创建彩卡
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

                        // 克隆模板
                        var cardGO = UnityEngine.Object.Instantiate(colorfulCardTemplate, parentTransform);
                        if (cardGO == null)
                            continue;

                        cardGO.SetActive(true);
                        cardGO.transform.position = colorfulCardTemplate.transform.position;
                        cardGO.transform.localPosition = colorfulCardTemplate.transform.localPosition;
                        cardGO.transform.localScale = colorfulCardTemplate.transform.localScale;
                        cardGO.transform.localRotation = colorfulCardTemplate.transform.localRotation;

                        // 设置图标
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

                        // 设置花费文本
                        var costText = cardGO.transform.GetChild(0)?.GetChild(1)?.GetComponent<TextMeshProUGUI>();
                        if (costText != null && PlantDataLoader.plantDatas.ContainsKey(plantType))
                        {
                            var plantData = PlantDataLoader.plantDatas[plantType];
                            if (plantData != null)
                            {
                                costText.text = plantData.field_Public_Int32_1.ToString();
                            }
                        }

                        // 设置 CardUI
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

                            // 如果已选择，隐藏卡片
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
                Core.Logger?.LogError($"[{Core.PLUGIN_NAME}] 创建彩卡UI失败: {ex.Message}");
            }
        }
    }

    // ==================== AlmanacPlantMenu 补丁：注册图鉴文本 ====================
    [HarmonyPatch(typeof(AlmanacPlantMenu))]
    internal static class AlmanacPlantMenuPatch
    {
        private static readonly PlantType TargetType = (PlantType)TemplateComponent.PlantID;

        // 【修改】图鉴标题和描述
        private static readonly string AlmanacTitle = "模板植物 (1999)";
        private static readonly string AlmanacDescription =
            "这是植物的宝开语描述。\n\n" +
            "<color=#3D1400>作者：</color><color=red>@你的名字</color>\n" +
            "<color=#3D1400>画师：</color><color=red>@画师名字</color>\n\n" +
            "<color=#3D1400>韧性：</color><color=red>300</color>\n" +
            "<color=#3D1400>攻击：</color><color=red>20/2秒</color>\n" +
            "<color=#3D1400>花费：</color><color=red>100阳光</color>\n" +
            "<color=#3D1400>冷却：</color><color=red>7.5秒</color>\n\n" +
            "<color=#3D1400>特点：</color><color=red>在这里描述植物的特点...</color>\n\n" +
            "<color=#3D1400>融合配方：</color><color=red>植物A + 植物B</color>";

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
                Core.Logger?.LogInfo($"[{Core.PLUGIN_NAME}] 图鉴文本注册成功");
            }
            catch (Exception ex)
            {
                Core.Logger?.LogWarning($"[{Core.PLUGIN_NAME}] 图鉴文本注册失败: {ex.Message}");
            }
        }
    }

    // ==================== Board.Start 补丁：重置状态 ====================
    [HarmonyPatch(typeof(Board), nameof(Board.Start))]
    internal static class BoardStartPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            // 在每局游戏开始时重置状态
            // 可以在这里添加需要重置的变量
        }
    }
}
