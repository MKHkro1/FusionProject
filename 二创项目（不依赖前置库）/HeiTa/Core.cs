using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Il2CppGameObjectList = Il2CppSystem.Collections.Generic.List<UnityEngine.GameObject>;

namespace HeiTa.BepInEx
{
    /// <summary>
    /// 黑塔二创植物插件入口
    /// </summary>
    [BepInPlugin("inf75.heita", "HeiTa", "1.0.0")]
    public class Core : BasePlugin
    {
        /// <summary>
        /// 黑塔植物韧性
        /// </summary>
        private const int PLANT_TOUGHNESS = 300;

        /// <summary>
        /// 黑塔冷却时间（秒）
        /// </summary>
        private const float PLANT_COOLDOWN = 45f;

        /// <summary>
        /// 黑塔阳光消耗
        /// </summary>
        private const int PLANT_SUN_COST = 50;

        internal static ManualLogSource? Logger;
        internal static AssetBundle? CachedBundle;

        public override void Load()
        {
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
                Logger = Log;

                // 注册 Harmony 补丁
                Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

                // 注册自定义组件类型到 IL2CPP
                ClassInjector.RegisterTypeInIl2Cpp<HeiTaPlant>();

                Log.LogInfo("[HeiTa] 插件加载完成，等待 GameAPP 初始化后注册植物。");
            }
            catch (Exception ex)
            {
                Log.LogError($"[HeiTa] 插件加载失败: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// 从嵌入资源加载 AssetBundle
        /// </summary>
        internal static AssetBundle? LoadEmbeddedAssetBundle(string bundleName)
        {
            if (CachedBundle != null)
                return CachedBundle;

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceNames = assembly.GetManifestResourceNames();
                string? matchedName = null;

                foreach (var name in resourceNames)
                {
                    if (name.EndsWith(bundleName, StringComparison.OrdinalIgnoreCase) ||
                        name.Equals(bundleName, StringComparison.OrdinalIgnoreCase))
                    {
                        matchedName = name;
                        break;
                    }
                }

                if (matchedName == null)
                {
                    Logger?.LogError($"[HeiTa] 未找到嵌入资源: {bundleName}");
                    return null;
                }

                using var stream = assembly.GetManifestResourceStream(matchedName);
                if (stream == null)
                {
                    Logger?.LogError($"[HeiTa] 无法读取嵌入资源流: {matchedName}");
                    return null;
                }

                var bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);
                CachedBundle = AssetBundle.LoadFromMemory(bytes);
                return CachedBundle;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"[HeiTa] 加载嵌入资源失败: {ex.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// GameAPP.Awake 后注册植物
    /// </summary>
    [HarmonyPatch(typeof(GameAPP), "Awake")]
    internal static class GameAppAwakePatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            try
            {
                // 加载 AssetBundle
                AssetBundle? assetBundle = Core.LoadEmbeddedAssetBundle("heita");
                if (assetBundle == null)
                {
                    Core.Logger?.LogError("[HeiTa] 资源包 heita 加载失败，请检查文件是否存在以及 csproj 中的 EmbeddedResource 设置。");
                    return;
                }

                // 读取预制体与预览图
                GameObject? prefab = assetBundle.LoadAsset("HeiTaPrefab")?.TryCast<GameObject>();
                GameObject? preview = assetBundle.LoadAsset("HeiTaPreview")?.TryCast<GameObject>();

                if (prefab == null)
                {
                    Core.Logger?.LogError("[HeiTa] 预制体 HeiTaPrefab 加载失败，请检查 AssetBundle 内资源名称。");
                    return;
                }

                if (preview == null)
                {
                    Core.Logger?.LogError("[HeiTa] 预览图 HeiTaPreview 加载失败，请检查 AssetBundle 内资源名称。");
                    return;
                }

                Core.Logger?.LogInfo("[HeiTa] 成功加载预制体与预览图。");

                // 手动注册植物
                ManualRegisterPlant(prefab, preview);
                
                // 注册彩卡
                RegisterColorfulCard();

                Core.Logger?.LogInfo($"[HeiTa] 黑塔植物注册完成，植物 ID: {HeiTaPlant.PlantID}");
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[HeiTa] 注册植物失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void ManualRegisterPlant(GameObject prefab, GameObject preview)
        {
            var plantType = (PlantType)HeiTaPlant.PlantID;
            var res = GameAPP.resourcesManager;

            // 添加 Plant 组件
            var plant = prefab.GetComponent<Plant>();
            if (plant == null)
            {
                plant = prefab.AddComponent<Plant>();
            }
            plant.thePlantType = plantType;

            // 添加自定义组件
            if (prefab.GetComponent<HeiTaPlant>() == null)
            {
                prefab.AddComponent<HeiTaPlant>();
            }

            // 注册到 ResourcesManager
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

            // 注册 PlantData
            try
            {
                EnsurePlantDataCapacity(HeiTaPlant.PlantID);

                var data = Activator.CreateInstance(typeof(PlantDataLoader.PlantData_)) as PlantDataLoader.PlantData_;
                if (data != null)
                {
                    data.field_Public_PlantType_0 = plantType;
                    data.field_Public_Int32_0 = 300;     // hp (韧性)
                    data.field_Public_Int32_1 = 50;      // sun cost
                    data.field_Public_Single_0 = 0f;     // attack interval
                    data.field_Public_Single_1 = 0f;     // produce interval
                    data.field_Public_Single_2 = 45f;    // cd
                    data.attackDamage = 0;

                    if (PlantDataLoader.plantData != null && PlantDataLoader.plantData.Length > HeiTaPlant.PlantID)
                        PlantDataLoader.plantData[HeiTaPlant.PlantID] = data;

                    PlantDataLoader.plantDatas[plantType] = data;
                }
            }
            catch (Exception ex)
            {
                Core.Logger?.LogWarning($"[HeiTa] 写入 PlantData 失败：{ex.Message}");
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
                Core.Logger?.LogInfo($"[HeiTa] PlantData 扩容至 {newLen} 以容纳 PlantId {plantId}。");
            }
            catch (Exception ex)
            {
                Core.Logger?.LogWarning($"[HeiTa] PlantData 扩容失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 注册为彩卡（可多次选取）
        /// </summary>
        private static void RegisterColorfulCard()
        {
            try
            {
                CustomCardRegistry.RegisterToColorfulCards((PlantType)HeiTaPlant.PlantID);
                Core.Logger?.LogInfo("[HeiTa] 彩卡注册成功。");
            }
            catch (Exception ex)
            {
                Core.Logger?.LogWarning($"[HeiTa] 注册彩卡失败：{ex.Message}");
            }
        }
    }

    /// <summary>
    /// Hook TypeMgr.FlyingPlants 方法（如果需要浮空植物功能）
    /// </summary>
    [HarmonyPatch(typeof(TypeMgr), nameof(TypeMgr.FlyingPlants))]
    internal static class TypeMgrFlyingPlantsPatch
    {
        [HarmonyPostfix]
        private static void Postfix(PlantType thePlantType, ref bool __result)
        {
            // 黑塔不需要浮空，这里留空
            // 如果需要可以添加: if (thePlantType == (PlantType)HeiTaPlant.PlantID) __result = true;
        }
    }

    /// <summary>
    /// 自定义卡片注册表（用于彩卡注册）
    /// </summary>
    internal static class CustomCardRegistry
    {
        // 存储自定义卡片：PlantType -> 父Transform获取函数列表
        internal static readonly Dictionary<PlantType, List<Func<Transform?>>> CustomCards 
            = new Dictionary<PlantType, List<Func<Transform?>>>();

        public static void RegisterToColorfulCards(PlantType plantType)
        {
            var parentGetters = new List<Func<Transform?>> { GetColorfulCardParent };
            
            if (!CustomCards.ContainsKey(plantType))
            {
                CustomCards.Add(plantType, parentGetters);
            }
            else
            {
                CustomCards[plantType].AddRange(parentGetters);
            }
        }

        /// <summary>
        /// 获取彩卡父节点
        /// </summary>
        internal static Transform? GetColorfulCardParent()
        {
            try
            {
                // 检查是否在游戏中
                if (Board.Instance == null)
                    return null;

                // 非IZ模式
                if (!Board.Instance.isIZ)
                {
                    if (InGameUI.Instance != null)
                    {
                        var seedBank = InGameUI.Instance.SeedBank;
                        if (seedBank != null)
                        {
                            var parent = seedBank.transform.parent;
                            if (parent != null)
                            {
                                var colorfulCards = parent.FindChild("Bottom/SeedLibrary/Grid/ColorfulCards/Page1");
                                if (colorfulCards != null)
                                    return colorfulCards;
                            }
                        }
                    }
                }
                else
                {
                    // IZ模式
                    if (IZBottomMenu.Instance != null)
                    {
                        var plantLibrary = IZBottomMenu.Instance.plantLibrary;
                        if (plantLibrary != null)
                        {
                            var colorfulCards = plantLibrary.transform.FindChild("Grid/ColorfulCards/Page1");
                            if (colorfulCards != null)
                                return colorfulCards;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Core.Logger?.LogWarning($"[HeiTa] 获取彩卡父节点失败：{ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 获取彩卡模板GameObject（CattailGirl）
        /// </summary>
        internal static GameObject? GetColorfulCardGameObject()
        {
            try
            {
                if (Board.Instance == null)
                    return null;

                if (!Board.Instance.isIZ)
                {
                    if (InGameUI.Instance != null)
                    {
                        var seedBank = InGameUI.Instance.SeedBank;
                        if (seedBank != null)
                        {
                            var parent = seedBank.transform.parent;
                            if (parent != null)
                            {
                                var cattailGirl = parent.FindChild("Bottom/SeedLibrary/Grid/ColorfulCards/Page1/CattailGirl");
                                if (cattailGirl != null)
                                    return cattailGirl.gameObject;
                            }
                        }
                    }
                }
                else
                {
                    if (IZBottomMenu.Instance != null)
                    {
                        var plantLibrary = IZBottomMenu.Instance.plantLibrary;
                        if (plantLibrary != null)
                        {
                            var cattailGirl = plantLibrary.transform.FindChild("Grid/ColorfulCards/Page1/CattailGirl");
                            if (cattailGirl != null)
                                return cattailGirl.gameObject;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Core.Logger?.LogWarning($"[HeiTa] 获取彩卡模板失败：{ex.Message}");
            }
            return null;
        }
    }

    /// <summary>
    /// SeedLibrary补丁：在种子库中创建彩卡UI
    /// </summary>
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
                {
                    Core.Logger?.LogWarning("[HeiTa] 无法获取彩卡模板，跳过彩卡UI创建。");
                    return;
                }

                // 获取已选卡片列表，避免重复创建
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

                // 记录已创建的卡片，避免重复
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

                        // 检查是否已创建
                        if (createdCards.ContainsKey(plantType) && createdCards[plantType].Contains(parentTransform))
                            continue;

                        // 创建彩卡UI
                        var cardGO = UnityEngine.Object.Instantiate(colorfulCardTemplate, parentTransform);
                        if (cardGO == null)
                            continue;

                        cardGO.SetActive(true);
                        cardGO.transform.position = colorfulCardTemplate.transform.position;
                        cardGO.transform.localPosition = colorfulCardTemplate.transform.localPosition;
                        cardGO.transform.localScale = colorfulCardTemplate.transform.localScale;
                        cardGO.transform.localRotation = colorfulCardTemplate.transform.localRotation;

                        // 设置卡片图标
                        var iconImage = cardGO.transform.GetChild(0)?.GetChild(0)?.GetComponent<Image>();
                        if (iconImage != null && GameAPP.resourcesManager.plantPreviews.ContainsKey(plantType))
                        {
                            var preview = GameAPP.resourcesManager.plantPreviews[plantType];
                            if (preview != null)
                            {
                                var spriteRenderer = preview.GetComponent<SpriteRenderer>();
                                if (spriteRenderer != null)
                                {
                                    iconImage.sprite = spriteRenderer.sprite;
                                    iconImage.SetNativeSize();
                                }
                            }
                        }

                        // 设置阳光消耗文本
                        var costText = cardGO.transform.GetChild(0)?.GetChild(1)?.GetComponent<TextMeshProUGUI>();
                        if (costText != null && PlantDataLoader.plantDatas.ContainsKey(plantType))
                        {
                            var plantData = PlantDataLoader.plantDatas[plantType];
                            if (plantData != null)
                            {
                                costText.text = plantData.field_Public_Int32_1.ToString();
                            }
                        }

                        // 设置CardUI组件
                        var cardUI = cardGO.transform.GetChild(1)?.GetComponent<CardUI>();
                        if (cardUI != null)
                        {
                            cardUI.gameObject.SetActive(true);
                            Mouse.Instance?.ChangeCardSprite(plantType, cardUI);
                            
                            var boxCollider = cardGO.transform.GetChild(1)?.GetComponent<BoxCollider2D>();
                            if (boxCollider != null)
                                boxCollider.enabled = true;

                            // 调整图标大小
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

                            // 如果已选择该卡片，隐藏CardUI
                            if (selectedCards.Contains(plantType))
                            {
                                cardGO.transform.GetChild(1)?.gameObject.SetActive(false);
                            }
                        }

                        // 记录已创建
                        if (!createdCards.ContainsKey(plantType))
                        {
                            createdCards.Add(plantType, new List<Transform> { parentTransform });
                        }
                        else
                        {
                            createdCards[plantType].Add(parentTransform);
                        }

                        Core.Logger?.LogInfo($"[HeiTa] 彩卡UI创建成功: {plantType}");
                    }
                }
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[HeiTa] 创建彩卡UI失败：{ex.Message}\n{ex.StackTrace}");
            }
        }
    }

    /// <summary>
    /// 让黑塔成为彩卡，并支持多次选卡（最多生成 14 张拷贝）
    /// </summary>
    [HarmonyPatch(typeof(CardUI), nameof(CardUI.Start))]
    internal static class CardUIStartPatch
    {
        private const int MaxCopies = 14;
        internal static int Spawned = 0;

        private static readonly PlantType TargetType = (PlantType)HeiTaPlant.PlantID;

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
                Core.Logger?.LogWarning($"[HeiTa] 复制彩卡失败：{ex.Message}");
            }
        }
    }

    /// <summary>
    /// 重置彩卡计数器
    /// </summary>
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
    /// 注册图鉴文本
    /// </summary>
    [HarmonyPatch(typeof(AlmanacPlantMenu))]
    internal static class AlmanacPlantMenuPatch
    {
        private static readonly PlantType TargetType = (PlantType)HeiTaPlant.PlantID;
        private static readonly string AlmanacTitle = $"黑塔 ({HeiTaPlant.PlantID})";
        private static readonly string AlmanacDescription =
            @"黑塔。

<color=#3D1400>作者：</color><color=red>梧萱梦汐X</color>
<color=#3D1400>韧性：</color><color=red>300</color>
<color=#3D1400>阳光：</color><color=red>50</color>
<color=#3D1400>冷却：</color><color=red>45秒</color>

<color=green>【效果】</color>
1. 触发时：可随机开出任意旅行词条（包含植物/僵尸词条）。
2. 死亡时：为<color=red>全场所有僵尸</color>附加<color=red>1000 冻结值</color>。";

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
                Core.Logger?.LogWarning($"[HeiTa] 注册图鉴文本失败：{ex.Message}");
            }
        }
    }
}


