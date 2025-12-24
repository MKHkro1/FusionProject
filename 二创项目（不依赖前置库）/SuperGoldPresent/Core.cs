using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppGameObjectList = Il2CppSystem.Collections.Generic.List<UnityEngine.GameObject>;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SuperGoldPresent.BepInEx
{
    [BepInPlugin("com.supergoldpresent.bepinex", "SuperGoldPresent", "1.0.0")]
    public class Core : BasePlugin
    {
        internal const int PlantId = 1732;
        internal const string BundleName = "supergoldpresent";

        internal static ManualLogSource? Logger;
        internal static AssetBundle? CachedBundle;

        public override void Load()
        {
            Logger = Log;
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Logger.LogInfo("[SuperGoldPresent] 插件加载完成，等待 GameAPP 初始化后注册植物。");
        }
    }

    [HarmonyPatch(typeof(GameAPP), "Awake")]
    internal static class GameAppAwakePatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            try
            {
                var bundle = LoadBundle();
                if (bundle == null)
                {
                    Core.Logger?.LogError("[SuperGoldPresent] 未能加载资源包 supergoldpresent，请检查文件是否放在插件同目录或 StreamingAssets/Mods 中。");
                    return;
                }

                if (!TryGetPrefabs(bundle, out var prefab, out var preview))
                {
                    Core.Logger?.LogError("[SuperGoldPresent] 资源包中未找到 SuperGoldPresentPrefab / SuperGoldPresentPreview。");
                    return;
                }

                EnsurePresentComponent(prefab);
                ManualRegister(prefab, preview);
                RegisterFlyingPlant();
                RegisterAlmanac();
                RegisterColorfulCard();

                Core.Logger?.LogInfo("[SuperGoldPresent] 贪欲盒子注册完成。");
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[SuperGoldPresent] 注册植物失败：{ex.Message}\n{ex.StackTrace}");
            }
        }

        private static readonly string BundleNameUnity3d = Core.BundleName + ".unity3d";
        private static readonly string BundleNameBundle = Core.BundleName + ".bundle";

        private static AssetBundle? LoadBundle()
        {
            if (Core.CachedBundle != null)
                return Core.CachedBundle;

            var streamingMods = Path.Combine(Application.streamingAssetsPath, "Mods");
            var dataMods = Path.Combine(Application.dataPath, "StreamingAssets", "Mods");
            
            var candidates = new List<string>(9)
            {
                Path.Combine(streamingMods, Core.BundleName),
                Path.Combine(streamingMods, BundleNameUnity3d),
                Path.Combine(streamingMods, BundleNameBundle),
                Path.Combine(dataMods, Core.BundleName),
                Path.Combine(dataMods, BundleNameUnity3d),
                Path.Combine(dataMods, BundleNameBundle),
            };

            try
            {
                var asmDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (!string.IsNullOrEmpty(asmDir))
                {
                    candidates.Add(Path.Combine(asmDir, Core.BundleName));
                    candidates.Add(Path.Combine(asmDir, BundleNameUnity3d));
                    candidates.Add(Path.Combine(asmDir, BundleNameBundle));
                }
            }
            catch
            {
                // ignored
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in candidates)
            {
                if (!seen.Add(path))
                    continue;

                try
                {
                    if (!File.Exists(path))
                        continue;

                    Core.Logger?.LogInfo($"[SuperGoldPresent] 尝试从 {path} 加载资源包。");
                    var bundle = AssetBundle.LoadFromFile(path);
                    if (bundle != null)
                    {
                        Core.CachedBundle = bundle;
                        return bundle;
                    }
                }
                catch (Exception ex)
                {
                    Core.Logger?.LogWarning($"[SuperGoldPresent] 加载资源包失败 {path}：{ex.Message}");
                }
            }

            // 尝试从嵌入资源加载
            try
            {
                Core.Logger?.LogInfo("[SuperGoldPresent] 尝试从嵌入资源加载 supergoldpresent");
                var embedded = LoadEmbeddedAssetBundle(Assembly.GetExecutingAssembly(), Core.BundleName);
                if (embedded != null)
                {
                    Core.CachedBundle = embedded;
                    return embedded;
                }
            }
            catch (Exception ex)
            {
                Core.Logger?.LogWarning($"[SuperGoldPresent] 嵌入资源加载失败：{ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 从程序集嵌入资源加载 AssetBundle
        /// </summary>
        private static AssetBundle? LoadEmbeddedAssetBundle(Assembly assembly, string bundleName)
        {
            try
            {
                var resourceNames = assembly.GetManifestResourceNames();
                string? matchedName = null;
                
                foreach (var name in resourceNames)
                {
                    if (name.EndsWith(bundleName, StringComparison.OrdinalIgnoreCase) ||
                        name.EndsWith(bundleName + ".unity3d", StringComparison.OrdinalIgnoreCase) ||
                        name.EndsWith(bundleName + ".bundle", StringComparison.OrdinalIgnoreCase))
                    {
                        matchedName = name;
                        break;
                    }
                }

                if (matchedName == null)
                    return null;

                using var stream = assembly.GetManifestResourceStream(matchedName);
                if (stream == null)
                    return null;

                var bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);
                return AssetBundle.LoadFromMemory(bytes);
            }
            catch
            {
                return null;
            }
        }

        private static readonly string PrefabName = "SuperGoldPresentPrefab";
        private static readonly string PreviewName = "SuperGoldPresentPreview";

        private static bool TryGetPrefabs(AssetBundle bundle, out GameObject prefab, out GameObject preview)
        {
            prefab = null!;
            preview = null!;

            try
            {
                var assets = bundle.LoadAllAssets();
                GameObject? fallback1 = null;
                GameObject? fallback2 = null;
                
                foreach (var asset in assets)
                {
                    var go = asset.TryCast<GameObject>();
                    if (go == null) continue;

                    if (fallback1 == null)
                        fallback1 = go;
                    else if (fallback2 == null)
                        fallback2 = go;

                    var name = go.name;
                    if (name.Equals(PrefabName, StringComparison.OrdinalIgnoreCase))
                    {
                        prefab = go;
                        if (preview != null) break;
                    }
                    else if (name.Equals(PreviewName, StringComparison.OrdinalIgnoreCase))
                    {
                        preview = go;
                        if (prefab != null) break;
                    }
                }

                if (prefab == null)
                    prefab = fallback1 ?? fallback2 ?? null!;
                if (preview == null)
                    preview = fallback2 ?? fallback1 ?? null!;
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[SuperGoldPresent] 解析资源包失败：{ex.Message}");
                return false;
            }

            return prefab != null && preview != null;
        }

        private static void EnsurePresentComponent(GameObject prefab)
        {
            try
            {
                var present = prefab.GetComponent<Present>();
                if (present == null)
                {
                    present = prefab.AddComponent<Present>();
                }
                present.thePlantType = (PlantType)Core.PlantId;
            }
            catch (Exception ex)
            {
                Core.Logger?.LogWarning($"[SuperGoldPresent] 为预制体补充 Present 组件失败：{ex.Message}");
            }
        }

        private static readonly PlantType PlantTypeCache = (PlantType)Core.PlantId;

        private static void ManualRegister(GameObject prefab, GameObject preview)
        {
            var res = GameAPP.resourcesManager;
            var plantType = PlantTypeCache;

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

            // PlantData
            try
            {
                EnsurePlantDataCapacity(Core.PlantId);

                var data = Activator.CreateInstance(typeof(PlantDataLoader.PlantData_)) as PlantDataLoader.PlantData_;
                if (data != null)
                {
                    data.field_Public_PlantType_0 = plantType;
                    data.field_Public_Int32_0 = 300;     // hp
                    data.field_Public_Int32_1 = 200;     // sun cost
                    data.field_Public_Single_0 = 0f;     // attack interval
                    data.field_Public_Single_1 = 0f;     // produce interval
                    data.field_Public_Single_2 = 15f;    // cd
                    data.attackDamage = 0;

                    if (PlantDataLoader.plantData != null && PlantDataLoader.plantData.Length > Core.PlantId)
                        PlantDataLoader.plantData[Core.PlantId] = data;

                    PlantDataLoader.plantDatas[plantType] = data;
                }
            }
            catch (Exception ex)
            {
                Core.Logger?.LogWarning($"[SuperGoldPresent] 写入 PlantData 失败：{ex.Message}");
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
                Core.Logger?.LogInfo($"[SuperGoldPresent] PlantData 扩容至 {newLen} 以容纳 PlantId {plantId}。");
            }
            catch (Exception ex)
            {
                Core.Logger?.LogWarning($"[SuperGoldPresent] PlantData 扩容失败：{ex.Message}");
            }
        }

        private static void RegisterFlyingPlant()
        {
            // 浮空植物通过 TypeMgrFlyingPlantsPatch 实现
            // 这里只记录日志
            Core.Logger?.LogInfo("[SuperGoldPresent] 浮空植物功能已通过 Harmony Patch 注册。");
        }

        private static void RegisterAlmanac()
        {
            // 图鉴文本通过 AlmanacPlantBankPatch 实现
            // 这里只记录日志
            Core.Logger?.LogInfo("[SuperGoldPresent] 图鉴文本功能已通过 Harmony Patch 注册。");
        }

        /// <summary>
        /// 注册为彩卡（可多次选取）
        /// </summary>
        private static void RegisterColorfulCard()
        {
            try
            {
                CustomCardRegistry.RegisterToColorfulCards((PlantType)Core.PlantId);
                Core.Logger?.LogInfo("[SuperGoldPresent] 彩卡注册成功。");
            }
            catch (Exception ex)
            {
                Core.Logger?.LogWarning($"[SuperGoldPresent] 注册彩卡失败：{ex.Message}");
            }
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
                Core.Logger?.LogWarning($"[SuperGoldPresent] 获取彩卡父节点失败：{ex.Message}");
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
                Core.Logger?.LogWarning($"[SuperGoldPresent] 获取彩卡模板失败：{ex.Message}");
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
                    Core.Logger?.LogWarning("[SuperGoldPresent] 无法获取彩卡模板，跳过彩卡UI创建。");
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

                        Core.Logger?.LogInfo($"[SuperGoldPresent] 彩卡UI创建成功: {plantType}");
                    }
                }
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[SuperGoldPresent] 创建彩卡UI失败：{ex.Message}\n{ex.StackTrace}");
            }
        }
    }

    /// <summary>
    /// Hook TypeMgr.FlyingPlants 方法，使贪欲盒子被识别为浮空植物
    /// </summary>
    [HarmonyPatch(typeof(TypeMgr), nameof(TypeMgr.FlyingPlants))]
    internal static class TypeMgrFlyingPlantsPatch
    {
        private static readonly PlantType TargetType = (PlantType)Core.PlantId;

        [HarmonyPostfix]
        private static void Postfix(PlantType thePlantType, ref bool __result)
        {
            if (thePlantType == TargetType)
            {
                __result = true;
            }
        }
    }

    /// <summary>
    /// Hook AlmanacPlantMenu.InitNameAndInfoFromJson 方法，注册图鉴文本
    /// </summary>
    [HarmonyPatch(typeof(AlmanacPlantMenu))]
    internal static class AlmanacPlantMenuPatch
    {
        private static readonly PlantType TargetType = (PlantType)Core.PlantId;
        private static readonly string AlmanacTitle = $"贪欲盒子 ({Core.PlantId})";
        private static readonly string AlmanacDescription =
            "打开后随机生成一只僵尸，击败僵尸可获得奖励。\n\n" +
            "<color=#3D1400>作者：</color><color=red>梧萱梦汐X、城外的影</color>\n" +
            "<color=#3D1400>花费：</color><color=red>200</color>\n" +
            "<color=#3D1400>冷却：</color><color=red>15</color>\n" +
            "<color=#3D1400>特点：</color><color=red>彩卡，可多次选取，根据生成的僵尸类型决定击败掉落奖励。</color>\n\n" +
            "<color=red>55%出现随机普通僵尸，击败获得一张惊喜礼盒卡片；</color>\n" +
            "<color=red>25%出现随机究极僵尸，击败获得一张随机超级植物卡片；</color>\n" +
            "<color=red>15%出现随机进化僵尸，击败获得一张随机究极植物卡片；</color>\n" +
            "<color=red>5%出现随机boss僵尸，击败获得一个随机究极植物卡片和一张惊喜礼盒卡片，同时可消除一条随机僵尸词条或者获得一条随机植物词条。</color>\n\n" +
            "<color=#3D1400>宝开语：</color><color=red>贪欲礼盒常常在万圣节的夜晚中被大家作为礼物而送出去『想要拿到糖果可没那么简单，让那些讨糖的小鬼尝尝被捣蛋的滋味吧』</color>";

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
                Core.Logger?.LogWarning($"[SuperGoldPresent] 注册图鉴文本失败：{ex.Message}");
            }
        }
    }

    /// <summary>
    /// 让贪欲盒子成为彩卡，并支持多次选卡（参考 GoldImitater 实现，最多生成 14 张拷贝）。
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
                Core.Logger?.LogWarning($"[SuperGoldPresent] 复制彩卡失败：{ex.Message}");
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
    /// 处理贪欲盒子的开盒逻辑，生成僵尸并记录信息
    /// </summary>
    [HarmonyPatch(typeof(Present), "AnimEvent")]
    internal static class PresentAnimPatch
    {
        // 陆地权重池
        private static readonly int[] LandPool55 = BuildRange(new[] { (0, 42), (45, 45), (47, 53), (55, 59), (61, 76), (100, 125), (206, 206), (211, 211) });
        private static readonly int[] LandPool25 = BuildRange(new[] { (200, 205), (207, 210), (213, 217), (225, 225), (227, 227), (230, 230), (233, 233), (236, 239) });
        private static readonly int[] LandPool15 = BuildRange(new[] { (300, 335) });
        private static readonly int[] LandPool05 = BuildRange(new[] { (43, 43), (212, 212), (218, 218), (224, 224), (226, 226), (228, 229), (231, 232), (234, 235) });
        
        // 水路权重池
        private static readonly int[] WaterPool55 = BuildRange(new[] { (11, 14), (17, 17), (19, 19), (25, 27), (29, 29), (45, 45), (71, 71), (76, 76), (113, 113), (119, 119) });
        private static readonly int[] WaterPool25 = BuildRange(new[] { (200, 200), (205, 205), (214, 214), (236, 237), (239, 239) });
        private static readonly int[] WaterPool15 = BuildRange(new[] { (310, 314), (327, 329) });
        private static readonly int[] WaterPool05 = BuildRange(new[] { (234, 234) });
        
        private static readonly PlantType TargetType = (PlantType)Core.PlantId;
        
        // 缓存数组长度
        private static readonly int LandPool55Length = LandPool55.Length;
        private static readonly int LandPool25Length = LandPool25.Length;
        private static readonly int LandPool15Length = LandPool15.Length;
        private static readonly int LandPool05Length = LandPool05.Length;
        private static readonly int WaterPool55Length = WaterPool55.Length;
        private static readonly int WaterPool25Length = WaterPool25.Length;
        private static readonly int WaterPool15Length = WaterPool15.Length;
        private static readonly int WaterPool05Length = WaterPool05.Length;

        // 存储生成的僵尸信息，用于死亡时判断掉落
        private static readonly Dictionary<Zombie, ZombieDropInfo> ZombieDropInfos = new Dictionary<Zombie, ZombieDropInfo>();

        [HarmonyPrefix]
        private static bool Prefix(Present __instance)
        {
            if (__instance == null || __instance.thePlantType != TargetType)
                return true;

            try
            {
                var pos = __instance.transform.position;
                var row = __instance.thePlantRow;
                var col = __instance.thePlantColumn;
                
                CreateParticle.SetParticle(11, pos, row, true);

                // 检查是否水路 - 使用 Present 组件的 board 属性
                // 如果检测失败，默认使用陆地逻辑
                bool isWater = false;
                try
                {
                    isWater = IsWaterTile(__instance, col, row);
                }
                catch
                {
                    // 静默失败，使用默认值
                    isWater = false;
                }
                
                var spawnX = pos.x;
                const int maxRetries = 50; // 最大重试次数
                int attempts = 0;
                GameObject? go = null;
                Zombie? zombie = null;
                int zombieId = 0;
                // 优化：预设初始容量，减少扩容开销
                var triedIds = new HashSet<int>(16);
                
                // 循环尝试生成僵尸，如果失败则重新抽取
                while (attempts < maxRetries)
                {
                    zombieId = PickZombie(row, isWater);
                    
                    // 如果这个ID已经尝试过且失败了，直接跳过
                    if (triedIds.Contains(zombieId))
                    {
                        attempts++;
                        continue;
                    }
                    
                    // 生成僵尸
                    go = CreateZombie.Instance?.SetZombie(row, (ZombieType)zombieId, spawnX, false);
                    
                    if (go != null)
                    {
                        zombie = go.GetComponent<Zombie>();
                        if (zombie != null)
                        {
                            // 成功生成僵尸，跳出循环
                            break;
                        }
                    }
                    
                    // 生成失败，记录无效ID并重试
                    triedIds.Add(zombieId);
                    attempts++;
                    
                    // 优化：只在重试次数较少时输出详细日志，避免日志过多
                    if (attempts <= 3 && Core.Logger != null)
                    {
                        Core.Logger.LogWarning($"[SuperGoldPresent] 僵尸ID {zombieId} 生成失败，尝试重新抽取 (第 {attempts}/{maxRetries} 次)");
                    }
                }
                
                // 如果成功生成僵尸，记录掉落信息
                if (go != null && zombie != null)
                {
                    ZombieDropInfos[zombie] = new ZombieDropInfo
                    {
                        ZombieId = zombieId,
                        IsWater = isWater,
                        PresentPosition = pos
                    };
                    
                    // 只在多次重试后成功时才记录日志
                    if (attempts > 3 && Core.Logger != null)
                    {
                        Core.Logger.LogInfo($"[SuperGoldPresent] 经过 {attempts} 次尝试后成功生成僵尸ID {zombieId}");
                    }
                }
                else
                {
                    // 达到最大重试次数仍未成功，尝试使用默认僵尸ID
                    if (Core.Logger != null)
                    {
                        Core.Logger.LogError($"[SuperGoldPresent] 经过 {maxRetries} 次尝试后仍无法生成有效僵尸，尝试使用默认僵尸ID 0");
                    }
                    
                    // 使用默认僵尸ID作为后备方案
                    go = CreateZombie.Instance?.SetZombie(row, (ZombieType)0, spawnX, false);
                    if (go != null)
                    {
                        zombie = go.GetComponent<Zombie>();
                        if (zombie != null)
                        {
                            ZombieDropInfos[zombie] = new ZombieDropInfo
                            {
                                ZombieId = 0,
                                IsWater = isWater,
                                PresentPosition = pos
                            };
                            
                            if (Core.Logger != null)
                            {
                                Core.Logger.LogInfo("[SuperGoldPresent] 使用默认僵尸ID 0 成功生成僵尸");
                            }
                        }
                    }
                    
                    if ((go == null || zombie == null) && Core.Logger != null)
                    {
                        Core.Logger.LogError("[SuperGoldPresent] 使用默认僵尸ID也失败，盒子将打开但不会生成僵尸");
                    }
                }

                __instance.Die((Plant.DieReason)0);
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[SuperGoldPresent] 开盒生成僵尸失败：{ex.Message}");
            }

            return false;
        }


        /// <summary>
        /// 检查指定位置是否为水路
        /// </summary>
        private static bool IsWaterTile(Present present, int col, int row)
        {
            try
            {
                var board = present.board;
                if (board == null) return false;
                
                // 使用 Board.GetBoxType 方法检测水路（参考 WaterPot 项目）
                return board.GetBoxType(col, row) == BoxType.Water;
            }
            catch
            {
                // 静默失败，返回默认值
                return false;
            }
        }

        private static int PickZombie(int row, bool isWater)
        {
            int zombieId;
            int roll = UnityEngine.Random.Range(0, 100);
            
            if (isWater)
            {
                // 水路概率：55% / 25% / 15% / 5%
                if (roll < 55)
                {
                    zombieId = WaterPool55[UnityEngine.Random.Range(0, WaterPool55Length)];
                }
                else if (roll < 80)
                {
                    zombieId = WaterPool25[UnityEngine.Random.Range(0, WaterPool25Length)];
                }
                else if (roll < 95)
                {
                    zombieId = WaterPool15[UnityEngine.Random.Range(0, WaterPool15Length)];
                }
                else
                {
                    zombieId = WaterPool05[UnityEngine.Random.Range(0, WaterPool05Length)];
                }
            }
            else
            {
                // 陆地概率：55% / 25% / 15% / 5%
                if (roll < 55)
                {
                    zombieId = LandPool55[UnityEngine.Random.Range(0, LandPool55Length)];
                }
                else if (roll < 80)
                {
                    zombieId = LandPool25[UnityEngine.Random.Range(0, LandPool25Length)];
                }
                else if (roll < 95)
                {
                    zombieId = LandPool15[UnityEngine.Random.Range(0, LandPool15Length)];
                }
                else
                {
                    zombieId = LandPool05[UnityEngine.Random.Range(0, LandPool05Length)];
                }
            }
            
            return zombieId;
        }

        private static int[] BuildRange((int from, int to)[] ranges)
        {
            int total = 0;
            foreach (var (from, to) in ranges)
            {
                total += to - from + 1;
            }
            
            var result = new int[total];
            int idx = 0;
            foreach (var (from, to) in ranges)
            {
                for (int i = from; i <= to; i++)
                {
                    result[idx++] = i;
                }
            }
            return result;
        }

        /// <summary>
        /// 获取僵尸掉落信息
        /// </summary>
        internal static bool TryGetDropInfo(Zombie zombie, out ZombieDropInfo info)
        {
            if (ZombieDropInfos.TryGetValue(zombie, out info))
            {
                return true;
            }
            info = default;
            return false;
        }

        /// <summary>
        /// 移除僵尸掉落信息
        /// </summary>
        internal static void RemoveDropInfo(Zombie zombie)
        {
            ZombieDropInfos.Remove(zombie);
        }
    }

    /// <summary>
    /// 僵尸掉落信息
    /// </summary>
    internal struct ZombieDropInfo
    {
        public int ZombieId;
        public bool IsWater;
        public Vector3 PresentPosition;
    }

    /// <summary>
    /// 处理僵尸死亡时的掉落逻辑
    /// </summary>
    [HarmonyPatch(typeof(Zombie), "Die")]
    internal static class ZombieDiePatch
    {
        // 25%掉落的超极植物卡片ID列表
        private static readonly int[] SuperPlantIds = { 243, 249, 253, 1005, 1013, 1026, 1046, 1052, 1104, 1110, 1126, 1132, 1148, 1160, 1161, 1169, 1174, 1220, 1234, 1266, 1300, 1306, 1342 };
        
        // 15%和5%掉落的究极植物卡片ID列表
        private static readonly int[] UltimatePlantIds = BuildUltimatePlantIds();

        // 记录已经处理过奖励的僵尸，防止重复触发
        internal static readonly Dictionary<Zombie, bool> ProcessedZombies = new Dictionary<Zombie, bool>();

        // 缓存已验证的植物ID（true=有效，false=无效，null=未验证）
        private static readonly Dictionary<int, bool> PlantIdValidationCache = new Dictionary<int, bool>(128);

        // 缓存超极植物ID的HashSet，用于快速查找
        private static readonly HashSet<int> SuperPlantIdsSet = new HashSet<int>(SuperPlantIds);
        
        // 缓存究极植物ID的HashSet，用于快速查找
        private static readonly HashSet<int> UltimatePlantIdsSet = new HashSet<int>(UltimatePlantIds);

        /// <summary>
        /// 构建究极植物ID列表
        /// 包含：227、229、234、240、242、245、300~305、900~911、913~917、919~937、939、940、942~949、951~959、961~970
        /// </summary>
        private static int[] BuildUltimatePlantIds()
        {
            var list = new List<int>(128);
            // 单独的ID
            list.AddRange(new[] { 227, 229, 234, 240, 242, 245 });
            // 300~305
            for (int i = 300; i <= 305; i++) list.Add(i);
            // 900~911
            for (int i = 900; i <= 911; i++) list.Add(i);
            // 913~917
            for (int i = 913; i <= 917; i++) list.Add(i);
            // 919~937
            for (int i = 919; i <= 937; i++) list.Add(i);
            // 939, 940
            list.Add(939);
            list.Add(940);
            // 942~949
            for (int i = 942; i <= 949; i++) list.Add(i);
            // 951~959
            for (int i = 951; i <= 959; i++) list.Add(i);
            // 961~970
            for (int i = 961; i <= 970; i++) list.Add(i);
            return list.ToArray();
        }

        // 缓存 resourcesManager 引用（延迟初始化）
        private static ResourcesManager? _cachedResourcesManager;

        /// <summary>
        /// 获取缓存的 resourcesManager 引用
        /// </summary>
        private static ResourcesManager? GetResourcesManager()
        {
            if (_cachedResourcesManager == null)
            {
                _cachedResourcesManager = GameAPP.resourcesManager;
            }
            return _cachedResourcesManager;
        }

        /// <summary>
        /// 验证植物ID是否有效（植物是否存在），带缓存机制
        /// </summary>
        private static bool IsValidPlantId(int plantId)
        {
            // 检查缓存
            if (PlantIdValidationCache.TryGetValue(plantId, out var cachedResult))
            {
                return cachedResult;
            }

            bool isValid = false;
            try
            {
                var plantType = (PlantType)plantId;
                var res = GetResourcesManager();
                if (res != null)
                {
                    // 检查 plantPrefabs 中是否存在
                    if (res.plantPrefabs != null && res.plantPrefabs.ContainsKey(plantType))
                    {
                        var prefab = res.plantPrefabs[plantType];
                        if (prefab != null)
                        {
                            isValid = true;
                        }
                    }

                    // 如果 plantPrefabs 中没有，检查 PlantDataLoader
                    if (!isValid && PlantDataLoader.plantDatas != null && PlantDataLoader.plantDatas.ContainsKey(plantType))
                    {
                        var data = PlantDataLoader.plantDatas[plantType];
                        if (data != null)
                        {
                            isValid = true;
                        }
                    }
                }
            }
            catch
            {
                isValid = false;
            }

            // 缓存结果
            PlantIdValidationCache[plantId] = isValid;
            return isValid;
        }

        /// <summary>
        /// 安全掉落植物卡片，如果植物ID无效则重新抽取
        /// </summary>
        private static void SetDroppedCardSafe(Vector3 position, int plantId, bool isUltimate, int maxRetries = 50)
        {
            const PlantType GiftBoxCard = (PlantType)256;
            const int GiftBoxId = 256;
            
            // 如果是惊喜礼盒卡片（256），直接掉落（假设它总是有效的）
            if (plantId == GiftBoxId)
            {
                Lawnf.SetDroppedCard(position, GiftBoxCard);
                return;
            }

            int attempts = 0;
            var triedIds = new HashSet<int>(16);
            bool isSuperPlant = SuperPlantIdsSet.Contains(plantId);
            
            while (attempts < maxRetries)
            {
                // 如果当前ID有效且未尝试过，直接使用
                if (!triedIds.Contains(plantId) && IsValidPlantId(plantId))
                {
                    Lawnf.SetDroppedCard(position, (PlantType)plantId);
                    return;
                }

                // ID无效或已尝试过，记录并重新抽取
                triedIds.Add(plantId);
                attempts++;

                // 重新抽取（根据原始ID的范围决定如何抽取）
                if (isUltimate)
                {
                    // 究极植物：从列表中随机选择
                    plantId = UltimatePlantIds[UnityEngine.Random.Range(0, UltimatePlantIds.Length)];
                }
                else if (isSuperPlant)
                {
                    // 超极植物：从列表中随机选择
                    plantId = SuperPlantIds[UnityEngine.Random.Range(0, SuperPlantIds.Length)];
                }
                else
                {
                    // 未知范围，使用默认值
                    plantId = GiftBoxId;
                    break;
                }

                // 只在重试次数较少时输出日志
                if (attempts <= 3)
                {
                    Core.Logger?.LogWarning($"[SuperGoldPresent] 植物ID无效，尝试重新抽取 (第 {attempts}/{maxRetries} 次)");
                }
            }

            // 达到最大重试次数，使用惊喜礼盒作为后备
            if (attempts >= maxRetries)
            {
                Core.Logger?.LogWarning($"[SuperGoldPresent] 经过 {maxRetries} 次尝试后仍无法找到有效植物ID，使用惊喜礼盒卡片（256）作为后备");
            }
            Lawnf.SetDroppedCard(position, GiftBoxCard);
        }

        /// <summary>
        /// 从超极植物列表中安全抽取一个有效的植物ID
        /// </summary>
        private static int PickValidSuperPlantId(int maxRetries = 50)
        {
            int attempts = 0;
            var triedIds = new HashSet<int>(SuperPlantIds.Length);
            const int GiftBoxId = 256;
            
            while (attempts < maxRetries)
            {
                var index = UnityEngine.Random.Range(0, SuperPlantIds.Length);
                var plantId = SuperPlantIds[index];
                
                if (!triedIds.Contains(plantId) && IsValidPlantId(plantId))
                {
                    return plantId;
                }

                triedIds.Add(plantId);
                attempts++;
            }

            // 如果所有超极植物都无效，返回惊喜礼盒ID
            Core.Logger?.LogWarning("[SuperGoldPresent] 所有超极植物ID都无效，返回惊喜礼盒卡片ID（256）");
            return GiftBoxId;
        }

        /// <summary>
        /// 从究极植物列表中安全抽取一个有效的植物ID
        /// </summary>
        private static int PickValidUltimatePlantId(int maxRetries = 50)
        {
            int attempts = 0;
            var triedIds = new HashSet<int>(64);
            const int GiftBoxId = 256;
            
            while (attempts < maxRetries)
            {
                var plantId = UltimatePlantIds[UnityEngine.Random.Range(0, UltimatePlantIds.Length)];
                
                if (!triedIds.Contains(plantId) && IsValidPlantId(plantId))
                {
                    return plantId;
                }

                triedIds.Add(plantId);
                attempts++;
            }

            // 如果所有究极植物都无效，返回惊喜礼盒ID
            Core.Logger?.LogWarning("[SuperGoldPresent] 所有究极植物ID都无效，返回惊喜礼盒卡片ID（256）");
            return GiftBoxId;
        }

        [HarmonyPostfix]
        private static void Postfix(Zombie __instance)
        {
            if (__instance == null)
                return;

            try
            {
                // 防止重复处理
                if (ProcessedZombies.ContainsKey(__instance))
                    return;

                // 检查是否是贪欲盒子生成的僵尸
                if (!PresentAnimPatch.TryGetDropInfo(__instance, out var dropInfo))
                    return;

                // 根据僵尸ID和是否水路决定掉落
                ProcessDrop(dropInfo.ZombieId, dropInfo.IsWater, dropInfo.PresentPosition);

                // 标记为已处理并清理记录
                ProcessedZombies[__instance] = true;
                PresentAnimPatch.RemoveDropInfo(__instance);
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[SuperGoldPresent] 处理僵尸掉落失败：{ex.Message}\n{ex.StackTrace}");
            }
        }

        internal static void ProcessDrop(int zombieId, bool isWater, Vector3 position)
        {
            try
            {
                // 优化：预先计算掉落位置，避免重复创建 Vector2/Vector3
                var dropPos3 = new Vector3(position.x, position.y + 0.3f, position.z);

                if (isWater)
                {
                    // 水路掉落逻辑
                    if (IsWater55Zombie(zombieId))
                    {
                        // 55%: 惊喜礼盒卡片
                        SetDroppedCardSafe(dropPos3, 256, false);
                    }
                    else if (IsWater25Zombie(zombieId))
                    {
                        // 25%: 随机超极植物卡片
                        var superId = PickValidSuperPlantId();
                        SetDroppedCardSafe(dropPos3, superId, false);
                    }
                    else if (IsWater15Zombie(zombieId))
                    {
                        // 15%: 随机究极植物卡片
                        var ultimateId = PickValidUltimatePlantId();
                        SetDroppedCardSafe(dropPos3, ultimateId, true);
                    }
                    else if (zombieId == 234)
                    {
                        // 5%: 随机究极植物卡片 + 惊喜礼盒卡片 + 词条操作
                        var ultimateId = PickValidUltimatePlantId();
                        SetDroppedCardSafe(dropPos3, ultimateId, true);
                        SetDroppedCardSafe(dropPos3, 256, false);
                        ProcessBuffOperation();
                    }
                }
                else
                {
                    // 陆地掉落逻辑
                    if (IsLand55Zombie(zombieId))
                    {
                        // 55%: 惊喜礼盒卡片
                        SetDroppedCardSafe(dropPos3, 256, false);
                    }
                    else if (IsLand25Zombie(zombieId))
                    {
                        // 25%: 随机超极植物卡片
                        var superId = PickValidSuperPlantId();
                        SetDroppedCardSafe(dropPos3, superId, false);
                    }
                    else if (IsLand15Zombie(zombieId))
                    {
                        // 15%: 随机究极植物卡片
                        var ultimateId = PickValidUltimatePlantId();
                        SetDroppedCardSafe(dropPos3, ultimateId, true);
                    }
                    else if (IsLand05Zombie(zombieId))
                    {
                        // 5%: 随机究极植物卡片 + 惊喜礼盒卡片 + 词条操作
                        var ultimateId = PickValidUltimatePlantId();
                        SetDroppedCardSafe(dropPos3, ultimateId, true);
                        SetDroppedCardSafe(dropPos3, 256, false);
                        ProcessBuffOperation();
                    }
                }
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[SuperGoldPresent] 处理掉落失败：{ex.Message}");
            }
        }

        // 水路55%僵尸判断
        private static bool IsWater55Zombie(int zombieId)
        {
            return IsInRange(zombieId, 11, 14) || zombieId == 17 || zombieId == 19 
                   || IsInRange(zombieId, 25, 27) || zombieId == 29 || zombieId == 45 
                   || zombieId == 71 || zombieId == 76 || zombieId == 113 || zombieId == 119;
        }

        // 水路25%僵尸判断
        private static bool IsWater25Zombie(int zombieId)
        {
            return zombieId == 200 || zombieId == 205 || zombieId == 214 
                   || IsInRange(zombieId, 236, 237) || zombieId == 239;
        }

        // 水路15%僵尸判断
        private static bool IsWater15Zombie(int zombieId)
        {
            return IsInRange(zombieId, 310, 314) || IsInRange(zombieId, 327, 329);
        }

        // 陆地55%僵尸判断
        private static bool IsLand55Zombie(int zombieId)
        {
            return IsInRange(zombieId, 0, 42) || zombieId == 45 || IsInRange(zombieId, 47, 53) 
                   || IsInRange(zombieId, 55, 59) || IsInRange(zombieId, 61, 76) 
                   || IsInRange(zombieId, 100, 125) || zombieId == 206 || zombieId == 211;
        }

        // 陆地25%僵尸判断
        private static bool IsLand25Zombie(int zombieId)
        {
            return IsInRange(zombieId, 200, 205) || IsInRange(zombieId, 207, 210) 
                   || IsInRange(zombieId, 213, 217) || zombieId == 225 || zombieId == 227 
                   || zombieId == 230 || zombieId == 233 || IsInRange(zombieId, 236, 239);
        }

        // 陆地15%僵尸判断
        private static bool IsLand15Zombie(int zombieId)
        {
            return IsInRange(zombieId, 300, 335);
        }

        // 陆地5%僵尸判断
        private static bool IsLand05Zombie(int zombieId)
        {
            return zombieId == 43 || zombieId == 212 || zombieId == 218 || zombieId == 224 
                   || zombieId == 226 || IsInRange(zombieId, 228, 229) 
                   || IsInRange(zombieId, 231, 232) || IsInRange(zombieId, 234, 235);
        }

        internal static bool IsInRange(int value, int min, int max)
        {
            return value >= min && value <= max;
        }

        /// <summary>
        /// 处理词条操作：随机消除一条已生效的僵尸词条，如果没有则获得一条随机植物词条
        /// </summary>
        internal static void ProcessBuffOperation()
        {
            try
            {
                var travelMgr = TravelMgr.Instance;
                if (travelMgr == null)
                {
                    Core.Logger?.LogWarning("[SuperGoldPresent] TravelMgr 未找到，无法处理词条操作");
                    return;
                }

                // 获取已生效的僵尸词条（Debuff）
                var debuff = travelMgr.debuff;
                if (debuff != null && debuff.Count > 0)
                {
                    // 优化：先统计数量，再收集索引，减少不必要的列表操作
                    int activeCount = 0;
                    for (int i = 0; i < debuff.Count; i++)
                    {
                        if (debuff[i]) activeCount++;
                    }

                    if (activeCount > 0)
                    {
                        // 优化：预分配容量
                        var activeDebuffs = new List<int>(activeCount);
                        for (int i = 0; i < debuff.Count; i++)
                        {
                            if (debuff[i])
                            {
                                activeDebuffs.Add(i);
                            }
                        }

                        // 随机移除一条僵尸词条
                        var index = activeDebuffs[UnityEngine.Random.Range(0, activeDebuffs.Count)];
                        debuff[index] = false;
                        
                        // 获取词条文本（参考 HeiTa 项目）
                        string? debuffText = null;
                        try
                        {
                            if (TravelMgr.debuffs != null && index < TravelMgr.debuffs.Count)
                            {
                                debuffText = TravelMgr.debuffs[index];
                            }
                        }
                        catch { }
                        
                        // 显示游戏内提示（参考 HeiTa 项目格式）
                        try
                        {
                            if (InGameText.Instance != null)
                            {
                                string msg = !string.IsNullOrEmpty(debuffText)
                                    ? $"贪欲盒子：已消除词条\n{debuffText}"
                                    : $"贪欲盒子：已消除僵尸词条#{index}";
                                InGameText.Instance.ShowText(msg, 4f);
                            }
                        }
                        catch { }
                        
                        return;
                    }
                }

                // 如果没有僵尸词条，则添加一条随机植物词条（Advanced Buff）
                var advancedUpgrades = travelMgr.advancedUpgrades;
                if (advancedUpgrades == null || advancedUpgrades.Count == 0)
                {
                    Core.Logger?.LogWarning("[SuperGoldPresent] advancedUpgrades 为空，无法添加植物词条");
                    return;
                }

                // 获取所有未激活的植物词条ID（0-139，根据词条ID.txt）
                int maxIndex = Math.Min(advancedUpgrades.Count, 140); // 限制在0-139范围内
                int availableCount = 0;
                for (int i = 0; i < maxIndex; i++)
                {
                    if (!advancedUpgrades[i]) availableCount++;
                }

                if (availableCount > 0)
                {
                    // 优化：预分配容量
                    var availableBuffs = new List<int>(availableCount);
                    for (int i = 0; i < maxIndex; i++)
                    {
                        if (!advancedUpgrades[i])
                        {
                            availableBuffs.Add(i);
                        }
                    }

                    var randomBuff = availableBuffs[UnityEngine.Random.Range(0, availableBuffs.Count)];
                    advancedUpgrades[randomBuff] = true;
                    
                    // 获取词条文本（参考 HeiTa 项目）
                    string? buffText = null;
                    try
                    {
                        if (TravelMgr.advancedBuffs != null && randomBuff < TravelMgr.advancedBuffs.Count)
                        {
                            buffText = TravelMgr.advancedBuffs[randomBuff];
                        }
                    }
                    catch { }
                    
                    // 显示游戏内提示（参考 HeiTa 项目格式）
                    try
                    {
                        if (InGameText.Instance != null)
                        {
                            string msg = !string.IsNullOrEmpty(buffText)
                                ? $"贪欲盒子：已获得词条\n{buffText}"
                                : $"贪欲盒子：已获得植物词条#{randomBuff}";
                            InGameText.Instance.ShowText(msg, 4f);
                        }
                    }
                    catch { }
                }
                else
                {
                    // 显示游戏内提示（参考 HeiTa 项目格式）
                    try
                    {
                        if (InGameText.Instance != null)
                        {
                            InGameText.Instance.ShowText("贪欲盒子：所有词条已解锁，无法添加", 3f);
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[SuperGoldPresent] 处理词条操作失败：{ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 移除已处理的僵尸记录（用于清理）
        /// </summary>
        internal static void RemoveProcessedZombie(Zombie zombie)
        {
            if (zombie != null)
            {
                ProcessedZombies.Remove(zombie);
            }
        }

        /// <summary>
        /// 清理已销毁的僵尸记录（定期调用）
        /// </summary>
        internal static void CleanupProcessedZombies()
        {
            try
            {
                // 优化：使用 List 预分配容量，减少扩容
                var toRemove = new List<Zombie>(ProcessedZombies.Count / 4);
                foreach (var kvp in ProcessedZombies)
                {
                    var zombie = kvp.Key;
                    if (zombie == null || zombie.gameObject == null || !zombie.gameObject.activeInHierarchy)
                    {
                        toRemove.Add(zombie!); // 添加 null-forgiving 操作符，因为我们已经检查了 null
                    }
                }
                // 优化：批量移除
                foreach (var zombie in toRemove)
                {
                    ProcessedZombies.Remove(zombie);
                }
            }
            catch
            {
                // 忽略清理错误
            }
        }
    }

    /// <summary>
    /// 处理僵尸被销毁时的掉落逻辑（备用触发点，用于处理被魅惑或小推车碾死的情况）
    /// </summary>
    [HarmonyPatch(typeof(Zombie), "OnDestroy")]
    internal static class ZombieOnDestroyPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Zombie __instance)
        {
            try
            {
                if (__instance == null)
                    return;

                // 防止重复处理（如果 Die 已经处理过）
                if (ZombieDiePatch.ProcessedZombies.ContainsKey(__instance))
                {
                    ZombieDiePatch.RemoveProcessedZombie(__instance);
                    return;
                }

                // 检查是否是贪欲盒子生成的僵尸
                if (!PresentAnimPatch.TryGetDropInfo(__instance, out var dropInfo))
                    return;

                // 检查僵尸是否真的死亡（血量<=0 或 beforeDying 为 true）
                // 或者被魅惑后离开（isMindControlled）
                bool shouldDrop = false;
                try
                {
                    // 优化：合并条件判断
                    shouldDrop = __instance.theHealth <= 0 || __instance.beforeDying || __instance.isMindControlled;
                }
                catch
                {
                    // 如果无法检查状态，假设已死亡（可能是被销毁）
                    shouldDrop = true;
                }

                if (!shouldDrop)
                {
                    // 僵尸还活着且未被魅惑，可能是被移除而不是死亡，不触发奖励
                    PresentAnimPatch.RemoveDropInfo(__instance);
                    return;
                }

                // 根据僵尸ID和是否水路决定掉落
                ZombieDiePatch.ProcessDrop(dropInfo.ZombieId, dropInfo.IsWater, dropInfo.PresentPosition);

                // 标记为已处理并清理记录
                ZombieDiePatch.ProcessedZombies[__instance] = true;
                PresentAnimPatch.RemoveDropInfo(__instance);
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[SuperGoldPresent] OnDestroy 处理僵尸掉落失败：{ex.Message}\n{ex.StackTrace}");
            }
        }
    }

    /// <summary>
    /// 避免在种植时调用原版融合检查（CheckMix）访问超大 PlantId 导致越界。
    /// </summary>
    [HarmonyPatch(typeof(CreatePlant), "CheckMix")]
    internal static class CheckMixPatch
    {
        private static readonly PlantType TargetType = (PlantType)Core.PlantId;

        [HarmonyPrefix]
        private static bool Prefix(int theColumn, int theRow, PlantType theUsedType)
        {
            if (theUsedType == TargetType)
            {
                return false;
            }
            return true;
        }
    }


    /// <summary>
    /// 允许贪欲盒子在陆地和水路都能种植
    /// 已注册为浮空植物，不需要动态修改标记
    /// 这里只用于记录水路信息（如果需要）
    /// </summary>
    [HarmonyPatch(typeof(CreatePlant), "SetPlant", new Type[] { typeof(int), typeof(int), typeof(PlantType), typeof(Plant), typeof(Vector2), typeof(bool), typeof(bool), typeof(Plant) })]
    internal static class CreatePlantSetPlantPatch
    {
        private static readonly PlantType TargetType = (PlantType)Core.PlantId;

        [HarmonyPrefix]
        private static void Prefix(int newColumn, int newRow, ref PlantType theSeedType)
        {
            // 如果是贪欲盒子，记录位置信息（用于后续生成僵尸时判断）
            if (theSeedType == TargetType)
            {
                try
                {
                    var board = Board.Instance;
                    if (board != null && newColumn >= 0 && newRow >= 0)
                    {
                        // 检查是否为水路（用于后续生成僵尸时判断，但不影响种植）
                        // 如果检测失败，默认使用陆地逻辑
                        // 浮空植物可以在水路和陆地都种植，不需要修改标记
                        // 使用 GetBoxType 方法检测水路，避免直接访问 boxInfos 属性
                        // 优化：移除不必要的日志输出
                    }
                }
                catch (Exception ex)
                {
                    Core.Logger?.LogError($"[SuperGoldPresent] SetPlant Prefix 处理失败: {ex.Message}");
                }
            }
        }

        [HarmonyPostfix]
        private static void Postfix(int newColumn, int newRow, PlantType theSeedType)
        {
            // 浮空植物不需要恢复标记
        }

        private static bool IsWaterTileAt(Board board, int col, int row)
        {
            try
            {
                // 使用 Board.GetBoxType 方法检测水路（参考 WaterPot 项目）
                return board.GetBoxType(col, row) == BoxType.Water;
            }
            catch
            {
                // 静默失败，返回默认值
                return false;
            }
        }

    }
}


