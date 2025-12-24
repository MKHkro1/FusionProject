using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using UnityEngine;

namespace Wish.BepInEx
{
    /// <summary>
    /// Money.ReinforcePlant 补丁：处理纠缠之缘的金钱大招
    /// </summary>
    [HarmonyPatch(typeof(Money), "ReinforcePlant")]
    internal static class MoneyReinforcePlantPatch
    {
        private static readonly PlantType TargetType = (PlantType)Core.PlantId;
        private const int DrawCost = 1600;

        [HarmonyPrefix]
        private static bool Prefix(Plant plant)
        {
            if (plant == null || plant.thePlantType != TargetType)
                return true; // 其他植物使用原版逻辑

            try
            {
                // 检查技能冷却时间
                if (plant.flashCountDown > 0f)
                {
                    Core.Logger?.LogInfo($"[纠缠之缘] 技能冷却中，剩余 {plant.flashCountDown:F1} 秒");
                    return false;
                }

                // 检查金钱是否足够
                if (Board.Instance.theMoney < DrawCost)
                {
                    Core.Logger?.LogInfo($"[纠缠之缘] 金钱不足，需要 {DrawCost}，当前 {Board.Instance.theMoney}");
                    return false;
                }

                // 获取 GoldSunflower 组件
                var goldSunflower = plant.TryCast<GoldSunflower>();
                if (goldSunflower == null)
                {
                    goldSunflower = plant.GetComponent<GoldSunflower>();
                }

                if (goldSunflower == null)
                {
                    Core.Logger?.LogWarning("[纠缠之缘] 无法获取 GoldSunflower 组件");
                    return false;
                }

                // 消耗金钱
                Board.Instance.theMoney -= DrawCost;

                // 设置技能冷却为10秒
                plant.flashCountDown = 10f;

                // 获取或创建概率状态
                if (!Core.ProbabilityStates.TryGetValue(goldSunflower, out var probState))
                {
                    probState = new Core.ProbabilityState();
                    Core.ProbabilityStates[goldSunflower] = probState;
                }

                // 执行抽卡逻辑
                GoldSunflowerSuperSkillPatch.StartDrawCardCoroutinePublic(goldSunflower, probState);

                Core.Logger?.LogInfo($"[纠缠之缘] 大招执行成功，消耗 {DrawCost} 金钱");
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[纠缠之缘] 大招执行失败: {ex.Message}\n{ex.StackTrace}");
            }

            return false; // 拦截原版逻辑
        }
    }

    /// <summary>
    /// 处理Wish植物的SuperSkill（大招），实现抽卡逻辑
    /// </summary>
    [HarmonyPatch(typeof(GoldSunflower), "SuperSkill")]
    internal static class GoldSunflowerSuperSkillPatch
    {
        private static readonly PlantType TargetType = (PlantType)Core.PlantId;
        private const int DrawCost = 1600;

        [HarmonyPrefix]
        private static bool Prefix(GoldSunflower __instance, ref bool __result)
        {
            try
            {
                if (__instance == null || __instance.thePlantType != TargetType)
                    return true;

                if (__instance.flashCountDown > 0f)
                {
                    __result = false;
                    return false;
                }

                var board = Board.Instance;
                if (board == null)
                {
                    __result = false;
                    return false;
                }

                if (board.theMoney < DrawCost)
                {
                    Core.Logger?.LogInfo($"[纠缠之缘] 金钱不足，需要 {DrawCost}，当前 {board.theMoney}");
                    __result = false;
                    return false;
                }

                board.theMoney -= DrawCost;
                __instance.flashCountDown = 10f;

                if (!Core.ProbabilityStates.TryGetValue(__instance, out var probState))
                {
                    probState = new Core.ProbabilityState();
                    Core.ProbabilityStates[__instance] = probState;
                }

                StartDrawCardCoroutine(__instance, probState);

                __result = true;
                return false;
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[纠缠之缘] SuperSkill处理失败：{ex.Message}\n{ex.StackTrace}");
                __result = false;
                return false;
            }
        }

        /// <summary>
        /// 公开的启动协程方法，供 MoneyReinforcePlantPatch 调用
        /// </summary>
        internal static void StartDrawCardCoroutinePublic(GoldSunflower plant, Core.ProbabilityState probState)
        {
            StartDrawCardCoroutine(plant, probState);
        }

        private static void StartDrawCardCoroutine(GoldSunflower plant, Core.ProbabilityState probState)
        {
            var coroutine = MonoBehaviourExtensions.StartCoroutine(plant, DrawCardCoroutine(plant, probState));
            ActiveCoroutines[plant] = coroutine;
        }

        internal static readonly Dictionary<GoldSunflower, Coroutine?> ActiveCoroutines = new Dictionary<GoldSunflower, Coroutine?>();
        internal static readonly HashSet<GameObject> ActiveVideos = new HashSet<GameObject>();

        private static bool IsPlantValid(GoldSunflower? plant)
        {
            if (plant == null)
                return false;
            var gameObj = plant.gameObject;
            return gameObj != null && gameObj.activeInHierarchy;
        }

        internal static void CleanupVideoObject(GameObject? videoObject, bool forceImmediate = false)
        {
            if (videoObject == null)
                return;

            try
            {
                var videoPlayer = videoObject.GetComponent<UnityEngine.Video.VideoPlayer>();
                if (videoPlayer != null)
                {
                    try { videoPlayer.Stop(); } catch { }
                    try { videoPlayer.renderMode = UnityEngine.Video.VideoRenderMode.MaterialOverride; } catch { }
                    try { videoPlayer.targetCamera = null; } catch { }
                    try { videoPlayer.clip = null; } catch { }
                    try { videoPlayer.enabled = false; } catch { }
                    try
                    {
                        if (forceImmediate)
                            UnityEngine.Object.DestroyImmediate(videoPlayer);
                        else
                            UnityEngine.Object.Destroy(videoPlayer);
                    }
                    catch { }
                }

                try { videoObject.SetActive(false); } catch { }
                ActiveVideos.Remove(videoObject);

                if (forceImmediate)
                    UnityEngine.Object.DestroyImmediate(videoObject);
                else
                    UnityEngine.Object.Destroy(videoObject);
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[纠缠之缘] 清理视频对象失败：{ex.Message}");
                try
                {
                    ActiveVideos.Remove(videoObject);
                    if (forceImmediate)
                        UnityEngine.Object.DestroyImmediate(videoObject);
                    else
                        UnityEngine.Object.Destroy(videoObject);
                }
                catch { }
            }
        }

        internal static void StopAllVideosAndCoroutines()
        {
            try
            {
                if (ActiveCoroutines.Count > 0)
                {
                    var toStop = new List<GoldSunflower>(ActiveCoroutines.Keys);
                    foreach (var plant in toStop)
                    {
                        try
                        {
                            if (plant != null && ActiveCoroutines.TryGetValue(plant, out var coroutine) && coroutine != null)
                            {
                                plant.StopCoroutine(coroutine);
                            }
                        }
                        catch { }
                    }
                    ActiveCoroutines.Clear();
                }

                if (ActiveVideos.Count > 0)
                {
                    var toDestroy = new List<GameObject>(ActiveVideos);
                    ActiveVideos.Clear();
                    foreach (var go in toDestroy)
                    {
                        if (go != null)
                        {
                            CleanupVideoObject(go, forceImmediate: true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[纠缠之缘] StopAllVideosAndCoroutines失败：{ex.Message}");
            }
        }

        private enum CardType { Blue, Purple, Gold, SuperGold }

        private class CardResult
        {
            public CardType Type;
            public string VideoName = string.Empty;
            public int PlantId1;
            public int PlantId2;
            public bool ShouldProcessBuff;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static IEnumerator DrawCardCoroutine(GoldSunflower plant, Core.ProbabilityState probState)
        {
            CardResult result = null!;
            bool hasError = false;
            
            try
            {
                result = DrawCard(probState);
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[纠缠之缘] 抽卡失败：{ex.Message}");
                hasError = true;
            }

            if (hasError || result == null)
            {
                ActiveCoroutines.Remove(plant);
                yield break;
            }

            if (!IsPlantValid(plant))
            {
                ActiveCoroutines.Remove(plant);
                yield break;
            }

            yield return PlayVideoCoroutine(result.VideoName, plant.transform.position, plant);

            if (!IsPlantValid(plant))
            {
                ActiveCoroutines.Remove(plant);
                yield break;
            }

            yield return ProcessDropCoroutine(result, plant.transform.position);

            if (!IsPlantValid(plant))
            {
                ActiveCoroutines.Remove(plant);
                yield break;
            }

            try
            {
                if (result.Type == CardType.Gold || result.Type == CardType.SuperGold)
                {
                    probState.Reset();
                    Core.Logger?.LogInfo("[纠缠之缘] 抽到金卡或捕获明光，概率已重置");
                }
                else
                {
                    probState.IncrementProbabilities();
                }

                Core.Logger?.LogInfo($"[纠缠之缘] 当前概率 - 蓝卡:{probState.BlueProbability:F2}% 紫卡:{probState.PurpleProbability:F2}% 金卡:{probState.GoldProbability:F2}% 捕获明光:{probState.SuperGoldProbability:F2}%");
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[纠缠之缘] 概率更新失败：{ex.Message}");
            }
            finally
            {
                ActiveCoroutines.Remove(plant);
            }
        }

        private static CardResult DrawCard(Core.ProbabilityState probState)
        {
            float roll = UnityEngine.Random.Range(0f, 100f);
            float current = 0f;

            current += probState.BlueProbability;
            if (roll < current)
            {
                return new CardResult
                {
                    Type = CardType.Blue,
                    VideoName = "bule",
                    PlantId1 = Core.GiftBoxCardId,
                    PlantId2 = 0,
                    ShouldProcessBuff = false
                };
            }

            current += probState.PurpleProbability;
            if (roll < current)
            {
                int superPlantId = Core.SuperPlantIds[UnityEngine.Random.Range(0, Core.SuperPlantIds.Length)];
                return new CardResult
                {
                    Type = CardType.Purple,
                    VideoName = "purple",
                    PlantId1 = superPlantId,
                    PlantId2 = 0,
                    ShouldProcessBuff = false
                };
            }

            current += probState.GoldProbability;
            if (roll < current)
            {
                int ultimatePlantId = Core.UltimatePlantIds[UnityEngine.Random.Range(0, Core.UltimatePlantIds.Length)];
                return new CardResult
                {
                    Type = CardType.Gold,
                    VideoName = "gold",
                    PlantId1 = ultimatePlantId,
                    PlantId2 = 0,
                    ShouldProcessBuff = false
                };
            }

            current += probState.SuperGoldProbability;
            if (roll < current)
            {
                int ultimatePlantId = Core.UltimatePlantIds[UnityEngine.Random.Range(0, Core.UltimatePlantIds.Length)];
                return new CardResult
                {
                    Type = CardType.SuperGold,
                    VideoName = "supergold",
                    PlantId1 = Core.GiftBoxCardId,
                    PlantId2 = ultimatePlantId,
                    ShouldProcessBuff = true
                };
            }

            return new CardResult
            {
                Type = CardType.Blue,
                VideoName = "bule",
                PlantId1 = Core.GiftBoxCardId,
                PlantId2 = 0,
                ShouldProcessBuff = false
            };
        }

        private static IEnumerator PlayVideoCoroutine(string videoName, Vector3 position, GoldSunflower? plant)
        {
            if (Core.CachedBundle == null)
            {
                Core.Logger?.LogWarning("[纠缠之缘] AssetBundle未加载，跳过视频播放");
                yield break;
            }

            UnityEngine.Video.VideoClip? videoClip = null;
            try
            {
                var obj = Core.CachedBundle.LoadAsset(videoName);
                if (obj != null)
                {
                    videoClip = obj.TryCast<UnityEngine.Video.VideoClip>();
                }
            }
            catch (Exception ex)
            {
                Core.Logger?.LogWarning($"[纠缠之缘] 加载视频资源失败: {videoName}, {ex.Message}");
            }

            if (videoClip == null)
            {
                Core.Logger?.LogWarning($"[纠缠之缘] 未找到视频资源: {videoName}");
                yield break;
            }

            GameObject? videoObject = null;
            UnityEngine.Video.VideoPlayer? videoPlayer = null;
            bool skipRequested = false;
            Camera? mainCamera = null;

            try
            {
                videoObject = new GameObject($"WishVideo_{videoName}");
                videoPlayer = videoObject.AddComponent<UnityEngine.Video.VideoPlayer>();
                ActiveVideos.Add(videoObject);
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[纠缠之缘] 创建视频对象失败：{ex.Message}");
                yield break;
            }

            try
            {
                videoPlayer!.clip = videoClip;
                videoPlayer.playOnAwake = false;
                videoPlayer.isLooping = false;

                mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    videoPlayer.renderMode = UnityEngine.Video.VideoRenderMode.CameraFarPlane;
                    videoPlayer.targetCamera = mainCamera;
                }
                else
                {
                    videoPlayer.renderMode = UnityEngine.Video.VideoRenderMode.MaterialOverride;
                }

                videoPlayer.Play();

                try
                {
                    if (InGameText.Instance != null)
                    {
                        InGameText.Instance.ShowText("点击右上角区域可跳过", 3f);
                    }
                }
                catch { }
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[纠缠之缘] 配置视频播放器失败：{ex.Message}");
                if (videoObject != null)
                {
                    ActiveVideos.Remove(videoObject);
                    UnityEngine.Object.Destroy(videoObject);
                }
                yield break;
            }

            yield return new WaitForSeconds(0.1f);

            var clipLength = (float)(videoPlayer!.clip?.length ?? 0f);
            float maxWaitDuration = Mathf.Max(clipLength + 0.5f, 1.5f);
            float elapsed = 0f;

            while (elapsed < maxWaitDuration)
            {
                if (mainCamera != null)
                {
                    try
                    {
                        if (Input.GetMouseButtonDown(0))
                        {
                            var mouse = Input.mousePosition;
                            var v = mainCamera.ScreenToViewportPoint(mouse);
                            if (v.x >= 0.9f && v.y >= 0.9f)
                            {
                                skipRequested = true;
                            }
                        }
                    }
                    catch { }
                }

                if (skipRequested)
                {
                    if (videoPlayer != null)
                    {
                        try { videoPlayer.Stop(); } catch { }
                    }
                    break;
                }

                bool plantValid = false;
                try
                {
                    plantValid = plant != null && IsPlantValid(plant);
                }
                catch
                {
                    plantValid = false;
                }

                if (!plantValid)
                {
                    if (videoObject != null)
                    {
                        CleanupVideoObject(videoObject, forceImmediate: false);
                        videoObject = null;
                        videoPlayer = null;
                    }
                    yield break;
                }

                if (videoObject == null || videoPlayer == null)
                    break;

                if (!videoObject.activeInHierarchy)
                    break;

                bool isPlaying = false;
                double currentTime = 0;
                long currentFrame = 0;
                long totalFrames = 0;
                try
                {
                    isPlaying = videoPlayer!.isPlaying;
                    currentTime = videoPlayer.time;
                    currentFrame = (long)videoPlayer.frame;
                    totalFrames = (long)videoPlayer.frameCount;
                }
                catch { }

                if (!isPlaying && currentTime > 0.05f)
                    break;

                if (totalFrames > 0 && currentFrame >= totalFrames - 1)
                    break;

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (videoObject != null)
            {
                CleanupVideoObject(videoObject, forceImmediate: false);
                videoObject = null;
                videoPlayer = null;
            }
        }

        private static IEnumerator ProcessDropCoroutine(CardResult result, Vector3 position)
        {
            var dropPos = new Vector3(position.x, position.y + 0.3f, position.z);

            try
            {
                SetDroppedCardSafe(dropPos, result.PlantId1);
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[纠缠之缘] 掉落第一张卡片失败：{ex.Message}");
            }

            if (result.PlantId2 > 0)
            {
                yield return new WaitForSeconds(0.3f);
                try
                {
                    SetDroppedCardSafe(dropPos, result.PlantId2);
                }
                catch (Exception ex)
                {
                    Core.Logger?.LogError($"[纠缠之缘] 掉落第二张卡片失败：{ex.Message}");
                }
            }

            if (result.ShouldProcessBuff)
            {
                try
                {
                    ProcessBuffOperation();
                }
                catch (Exception ex)
                {
                    Core.Logger?.LogError($"[纠缠之缘] 处理词条失败：{ex.Message}");
                }
            }
        }

        private static readonly Dictionary<int, bool> PlantIdValidationCache = new Dictionary<int, bool>(128);

        private static void SetDroppedCardSafe(Vector3 position, int plantId, int maxRetries = 50)
        {
            const PlantType GiftBoxCard = (PlantType)Core.GiftBoxCardId;
            const int GiftBoxId = Core.GiftBoxCardId;

            if (plantId == GiftBoxId)
            {
                Lawnf.SetDroppedCard(position, GiftBoxCard);
                return;
            }

            if (IsValidPlantId(plantId))
            {
                Lawnf.SetDroppedCard(position, (PlantType)plantId);
                return;
            }

            int attempts = 0;
            var triedIds = new HashSet<int>(16);
            bool isUltimatePlant = Core.UltimatePlantIdsSet.Contains(plantId);
            bool isSuperPlant = Core.SuperPlantIdsSet.Contains(plantId);
            
            while (attempts < maxRetries)
            {
                triedIds.Add(plantId);
                attempts++;

                if (isUltimatePlant)
                {
                    plantId = Core.UltimatePlantIds[UnityEngine.Random.Range(0, Core.UltimatePlantIds.Length)];
                }
                else if (isSuperPlant)
                {
                    plantId = Core.SuperPlantIds[UnityEngine.Random.Range(0, Core.SuperPlantIds.Length)];
                }
                else
                {
                    plantId = GiftBoxId;
                    break;
                }

                if (!triedIds.Contains(plantId) && IsValidPlantId(plantId))
                {
                    Lawnf.SetDroppedCard(position, (PlantType)plantId);
                    return;
                }
            }

            Lawnf.SetDroppedCard(position, GiftBoxCard);
        }

        private static ResourcesManager? _cachedResourcesManager;

        private static ResourcesManager? GetResourcesManager()
        {
            if (_cachedResourcesManager == null)
            {
                _cachedResourcesManager = GameAPP.resourcesManager;
            }
            return _cachedResourcesManager;
        }

        private static bool IsValidPlantId(int plantId)
        {
            if (PlantIdValidationCache.TryGetValue(plantId, out var cachedResult))
            {
                return cachedResult;
            }

            bool isValid = false;
            try
            {
                var plantType = (PlantType)plantId;
                var res = GetResourcesManager();
                if (res?.plantPrefabs != null && res.plantPrefabs.ContainsKey(plantType))
                {
                    var prefab = res.plantPrefabs[plantType];
                    if (prefab != null)
                    {
                        isValid = true;
                    }
                }

                if (!isValid && PlantDataLoader.plantDatas != null && PlantDataLoader.plantDatas.ContainsKey(plantType))
                {
                    var data = PlantDataLoader.plantDatas[plantType];
                    if (data != null)
                    {
                        isValid = true;
                    }
                }
            }
            catch
            {
                isValid = false;
            }

            if (PlantIdValidationCache.Count < 500)
            {
                PlantIdValidationCache[plantId] = isValid;
            }
            return isValid;
        }

        private static void ProcessBuffOperation()
        {
            try
            {
                var travelMgr = TravelMgr.Instance;
                if (travelMgr == null)
                {
                    Core.Logger?.LogWarning("[纠缠之缘] TravelMgr 未找到，无法处理词条操作");
                    return;
                }

                var debuff = travelMgr.debuff;
                if (debuff != null && debuff.Count > 0)
                {
                    var activeDebuffs = new List<int>(Math.Min(debuff.Count / 4, 32));
                    for (int i = 0; i < debuff.Count; i++)
                    {
                        if (debuff[i])
                        {
                            activeDebuffs.Add(i);
                        }
                    }

                    if (activeDebuffs.Count > 0)
                    {
                        var index = activeDebuffs[UnityEngine.Random.Range(0, activeDebuffs.Count)];
                        debuff[index] = false;

                        string? debuffText = null;
                        try
                        {
                            if (TravelMgr.debuffs != null && index < TravelMgr.debuffs.Count)
                            {
                                debuffText = TravelMgr.debuffs[index];
                            }
                        }
                        catch { }

                        try
                        {
                            if (InGameText.Instance != null)
                            {
                                string msg = !string.IsNullOrEmpty(debuffText)
                                    ? $"纠缠之缘：已消除词条\n{debuffText}"
                                    : $"纠缠之缘：已消除僵尸词条#{index}";
                                InGameText.Instance.ShowText(msg, 4f);
                            }
                        }
                        catch { }

                        return;
                    }
                }

                var advancedUpgrades = travelMgr.advancedUpgrades;
                if (advancedUpgrades == null || advancedUpgrades.Count == 0)
                {
                    Core.Logger?.LogWarning("[纠缠之缘] advancedUpgrades 为空，无法添加植物词条");
                    return;
                }

                int maxIndex = Math.Min(advancedUpgrades.Count, 140);
                var availableBuffs = new List<int>(Math.Min(maxIndex / 4, 32));
                for (int i = 0; i < maxIndex; i++)
                {
                    if (!advancedUpgrades[i])
                    {
                        availableBuffs.Add(i);
                    }
                }

                if (availableBuffs.Count > 0)
                {
                    var randomBuff = availableBuffs[UnityEngine.Random.Range(0, availableBuffs.Count)];
                    advancedUpgrades[randomBuff] = true;

                    string? buffText = null;
                    try
                    {
                        if (TravelMgr.advancedBuffs != null && randomBuff < TravelMgr.advancedBuffs.Count)
                        {
                            buffText = TravelMgr.advancedBuffs[randomBuff];
                        }
                    }
                    catch { }

                    try
                    {
                        if (InGameText.Instance != null)
                        {
                            string msg = !string.IsNullOrEmpty(buffText)
                                ? $"纠缠之缘：已获得词条\n{buffText}"
                                : $"纠缠之缘：已获得植物词条#{randomBuff}";
                            InGameText.Instance.ShowText(msg, 4f);
                        }
                    }
                    catch { }
                }
                else
                {
                    try
                    {
                        if (InGameText.Instance != null)
                        {
                            InGameText.Instance.ShowText("纠缠之缘：所有词条已解锁，无法添加", 3f);
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[纠缠之缘] 处理词条操作失败：{ex.Message}");
            }
        }
    }
}
