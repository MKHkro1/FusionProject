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
using TMPro;
using UnityEngine.UI;
using Il2CppGameObjectList = Il2CppSystem.Collections.Generic.List<UnityEngine.GameObject>;

namespace WaterPot.BepInEx
{
    [BepInPlugin("inf75.waterpot", "WaterPot", "2.0.0")]
    public class Core : BasePlugin
    {
        public const int PlantID = 2012;
        internal const string BundleName = "waterpot";

        private const int PLANT_TOUGHNESS = 300;
        private const float PLANT_COOLDOWN = 7.5f;
        private const int PLANT_SUN_COST = 25;

        internal static ManualLogSource? Logger;
        internal static AssetBundle? CachedBundle;

        public override void Load()
        {
            Logger = Log;
            Console.OutputEncoding = Encoding.UTF8;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            ClassInjector.RegisterTypeInIl2Cpp<WaterPotComponent>();

            Logger.LogInfo("[WaterPot] 插件加载完成，等待 GameAPP 初始化后注册植物。");
        }

        internal static int GetPlantToughness() => PLANT_TOUGHNESS;
        internal static float GetPlantCooldown() => PLANT_COOLDOWN;
        internal static int GetPlantSunCost() => PLANT_SUN_COST;
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
                    Core.Logger?.LogError("[WaterPot] 未能加载资源包 waterpot，请检查文件是否放在插件同目录或 StreamingAssets/Mods 中。");
                    return;
                }

                if (!TryGetPrefabs(bundle, out var prefab, out var preview))
                {
                    Core.Logger?.LogError("[WaterPot] 资源包中未找到 WaterPotPrefab / WaterPotPreview。");
                    return;
                }

                EnsureWaterPotComponent(prefab);
                ManualRegister(prefab, preview);
                RegisterPotType();
                RegisterAlmanac();
                RegisterColorfulCard();

                Core.Logger?.LogInfo("[WaterPot] 水盆注册完成。");
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[WaterPot] 注册植物失败：{ex.Message}\n{ex.StackTrace}");
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
            catch { }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in candidates)
            {
                if (!seen.Add(path))
                    continue;

                try
                {
                    if (!File.Exists(path))
                        continue;

                    Core.Logger?.LogInfo($"[WaterPot] 尝试从 {path} 加载资源包。");
                    var bundle = AssetBundle.LoadFromFile(path);
                    if (bundle != null)
                    {
                        Core.CachedBundle = bundle;
                        return bundle;
                    }
                }
                catch (Exception ex)
                {
                    Core.Logger?.LogWarning($"[WaterPot] 加载资源包失败 {path}：{ex.Message}");
                }
            }

            // 尝试从嵌入资源加载
            try
            {
                Core.Logger?.LogInfo("[WaterPot] 尝试从嵌入资源加载 waterpot");
                var embedded = LoadEmbeddedAssetBundle(Assembly.GetExecutingAssembly(), Core.BundleName);
                if (embedded != null)
                {
                    Core.CachedBundle = embedded;
                    return embedded;
                }
            }
            catch (Exception ex)
            {
                Core.Logger?.LogWarning($"[WaterPot] 嵌入资源加载失败：{ex.Message}");
            }

            return null;
        }

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
                        name.EndsWith(bundleName + ".bundle", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals(bundleName, StringComparison.OrdinalIgnoreCase))
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

        private static readonly string PrefabName = "WaterPotPrefab";
        private static readonly string PreviewName = "WaterPotPreview";

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
                Core.Logger?.LogError($"[WaterPot] 解析资源包失败：{ex.Message}");
                return false;
            }

            return prefab != null && preview != null;
        }

        private static void EnsureWaterPotComponent(GameObject prefab)
        {
            try
            {
                var plant = prefab.GetComponent<Plant>();
                if (plant == null)
                {
                    Core.Logger?.LogInfo("[WaterPot] 预制体没有 Plant 组件，尝试添加...");
                    plant = prefab.AddComponent<Plant>();
                    Core.Logger?.LogInfo("[WaterPot] 成功添加 Plant 组件");
                }
                plant.thePlantType = (PlantType)Core.PlantID;
                plant.alwaysLightUp = true;
                plant.isShort = true;

                // 设置 axis 引用（Plant 需要这个引用来获取位置）
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
                    Core.Logger?.LogInfo("[WaterPot] 创建了 axis 子对象");
                }
                plant.axis = axisTransform;
                Core.Logger?.LogInfo("[WaterPot] Plant 组件配置完成");
            }
            catch (Exception ex)
            {
                Core.Logger?.LogWarning($"[WaterPot] 为预制体补充 Plant 组件失败：{ex.Message}");
            }
        }

        private static readonly PlantType PlantTypeCache = (PlantType)Core.PlantID;

        private static void ManualRegister(GameObject prefab, GameObject preview)
        {
            var res = GameAPP.resourcesManager;
            var plantType = PlantTypeCache;

            // 设置预制体标签（必须！游戏通过标签识别植物预制体）
            prefab.tag = "Plant";
            preview.tag = "Preview";
            Core.Logger?.LogInfo("[WaterPot] 设置预制体标签完成");

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
                EnsurePlantDataCapacity(Core.PlantID);

                var data = Activator.CreateInstance(typeof(PlantDataLoader.PlantData_)) as PlantDataLoader.PlantData_;
                if (data != null)
                {
                    data.field_Public_PlantType_0 = plantType;
                    data.field_Public_Int32_0 = Core.GetPlantToughness();  // hp
                    data.field_Public_Int32_1 = Core.GetPlantSunCost();    // sun cost
                    data.field_Public_Single_0 = 0f;                       // attack interval
                    data.field_Public_Single_1 = 0f;                       // produce interval
                    data.field_Public_Single_2 = Core.GetPlantCooldown();  // cd
                    data.attackDamage = 0;

                    if (PlantDataLoader.plantData != null && PlantDataLoader.plantData.Length > Core.PlantID)
                        PlantDataLoader.plantData[Core.PlantID] = data;

                    PlantDataLoader.plantDatas[plantType] = data;
                }
            }
            catch (Exception ex)
            {
                Core.Logger?.LogWarning($"[WaterPot] 写入 PlantData 失败：{ex.Message}");
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
                Core.Logger?.LogInfo($"[WaterPot] PlantData 扩容至 {newLen} 以容纳 PlantId {plantId}。");
            }
            catch (Exception ex)
            {
                Core.Logger?.LogWarning($"[WaterPot] PlantData 扩容失败：{ex.Message}");
            }
        }

        private static void RegisterPotType()
        {
            // 通过 Harmony Patch 实现 IsPot 判断
            Core.Logger?.LogInfo("[WaterPot] 花盆类型功能已通过 Harmony Patch 注册。");
        }

        private static void RegisterAlmanac()
        {
            Core.Logger?.LogInfo("[WaterPot] 图鉴文本功能已通过 Harmony Patch 注册。");
        }

        private static void RegisterColorfulCard()
        {
            try
            {
                CustomCardRegistry.RegisterToColorfulCards((PlantType)Core.PlantID);
                Core.Logger?.LogInfo("[WaterPot] 彩卡注册成功。");
            }
            catch (Exception ex)
            {
                Core.Logger?.LogWarning($"[WaterPot] 注册彩卡失败：{ex.Message}");
            }
        }
    }


    /// <summary>
    /// 自定义卡片注册表（用于彩卡注册）
    /// </summary>
    internal static class CustomCardRegistry
    {
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

        internal static Transform? GetColorfulCardParent()
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
                                var colorfulCards = parent.FindChild("Bottom/SeedLibrary/Grid/ColorfulCards/Page1");
                                if (colorfulCards != null)
                                    return colorfulCards;
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
                            var colorfulCards = plantLibrary.transform.FindChild("Grid/ColorfulCards/Page1");
                            if (colorfulCards != null)
                                return colorfulCards;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Core.Logger?.LogWarning($"[WaterPot] 获取彩卡父节点失败：{ex.Message}");
            }
            return null;
        }

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
                Core.Logger?.LogWarning($"[WaterPot] 获取彩卡模板失败：{ex.Message}");
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
                    return;
                }

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
                Core.Logger?.LogError($"[WaterPot] 创建彩卡UI失败：{ex.Message}\n{ex.StackTrace}");
            }
        }
    }

    /// <summary>
    /// Hook TypeMgr.IsPot 方法，使水盆被识别为花盆类型
    /// </summary>
    [HarmonyPatch(typeof(TypeMgr), nameof(TypeMgr.IsPot))]
    internal static class TypeMgrIsPotPatch
    {
        private static readonly PlantType TargetType = (PlantType)Core.PlantID;

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
        private static readonly PlantType TargetType = (PlantType)Core.PlantID;
        private static readonly string AlmanacTitle = $"水盆 ({Core.PlantID})";
        private static readonly string AlmanacDescription =
            "水上植物的\"花盆\"基座。\n\n" +
            "<color=#3D1400>作者：</color><color=red>梧萱梦汐X、红烧黛鱼</color>\n" +
            "<color=#3D1400>韧性：</color><color=red>300</color>\n" +
            "<color=#3D1400>消耗：</color><color=red>25</color>\n" +
            "<color=#3D1400>冷却：</color><color=red>7.5秒</color>\n\n" +
            "<color=green>【效果】</color>\n" +
            "承载水生植物的花盆，可在陆地上为水生植物刷新水盆（冷却2秒）。";

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
                Core.Logger?.LogWarning($"[WaterPot] 注册图鉴文本失败：{ex.Message}");
            }
        }
    }

    public class WaterPotComponent : MonoBehaviour
    {
        private Plant? _selfPlant;
        private Plant? _currentRider;
        private float _nextCheckTime;
        private readonly Dictionary<IntPtr, RiderState> _riderStates = new();

        public WaterPotComponent(IntPtr ptr) : base(ptr)
        {
        }

        private void Awake()
        {
            try
            {
                _selfPlant = GetComponent<Plant>();
                if (_selfPlant != null)
                {
                    _selfPlant.alwaysLightUp = true;
                    _selfPlant.isShort = true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WaterPot] 初始化失败: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            ReleaseRider();
        }

        private void Update()
        {
            if (_selfPlant == null || Board.Instance == null)
            {
                ReleaseRider();
                return;
            }

            if (_nextCheckTime > Time.time)
            {
                if (_currentRider != null)
                {
                    EnsureRiderState(_currentRider);
                }
                return;
            }

            _nextCheckTime = Time.time + 0.25f;
            SyncRider();
        }

        private void SyncRider()
        {
            Plant? rider = FindRider();

            if (rider == _currentRider)
            {
                if (rider != null)
                {
                    EnsureRiderState(rider);
                }
                return;
            }

            if (_currentRider != null)
            {
                ResetRiderState(_currentRider);
            }

            _currentRider = rider;

            if (_currentRider != null)
            {
                ApplyRiderState(_currentRider);
            }
        }

        private Plant? FindRider()
        {
            if (_selfPlant == null)
            {
                return null;
            }

            var plants = Lawnf.Get1x1Plants(_selfPlant.thePlantColumn, _selfPlant.thePlantRow);
            if (plants == null || plants.Count == 0)
            {
                return null;
            }

            for (int i = 0; i < plants.Count; i++)
            {
                var plant = plants[i];
                if (plant == null || plant == _selfPlant)
                {
                    continue;
                }

                if (!TypeMgr.IsWaterPlant(plant.thePlantType))
                {
                    continue;
                }

                return plant;
            }

            return null;
        }

        private void ReleaseRider()
        {
            if (_currentRider != null)
            {
                ResetRiderState(_currentRider);
                _currentRider = null;
            }
        }

        private class RiderState
        {
            public bool OriginalIsLily;
            public PlantType OriginalLilyType;
            public bool OriginalWaterTag;
        }

        private RiderState GetOrCreateState(Plant rider)
        {
            IntPtr key = rider.Pointer;
            if (!_riderStates.TryGetValue(key, out var state))
            {
                state = new RiderState
                {
                    OriginalIsLily = rider.isLily,
                    OriginalLilyType = rider.theLilyType,
                    OriginalWaterTag = rider.plantTag.waterPlant
                };
                _riderStates[key] = state;
            }

            return state;
        }

        private void RemoveState(Plant rider)
        {
            IntPtr key = rider.Pointer;
            if (_riderStates.ContainsKey(key))
            {
                _riderStates.Remove(key);
            }
        }

        private void ApplyRiderState(Plant rider)
        {
            try
            {
                GetOrCreateState(rider);
                EnsureRiderWaterlessTag(rider);
            }
            catch { }
        }

        private void EnsureRiderState(Plant rider)
        {
            GetOrCreateState(rider);
            EnsureRiderWaterlessTag(rider);
        }

        private static void EnsureRiderWaterlessTag(Plant rider)
        {
            var tag = rider.plantTag;
            if (!tag.waterPlant)
            {
                return;
            }

            tag.waterPlant = false;
            rider.plantTag = tag;
        }

        private void ResetRiderState(Plant rider)
        {
            try
            {
                if (_riderStates.TryGetValue(rider.Pointer, out var state))
                {
                    rider.isLily = state.OriginalIsLily;
                    rider.theLilyType = state.OriginalLilyType;
                    var tag = rider.plantTag;
                    tag.waterPlant = state.OriginalWaterTag;
                    rider.plantTag = tag;
                }
                else
                {
                    rider.isLily = false;
                    if (rider.theLilyType == PlantType.LilyPad)
                    {
                        rider.theLilyType = PlantType.Nothing;
                    }
                    var tag = rider.plantTag;
                    tag.waterPlant = TypeMgr.IsWaterPlant(rider.thePlantType);
                    rider.plantTag = tag;
                }

                RemoveState(rider);
            }
            catch { }
        }
    }
}
