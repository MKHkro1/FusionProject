// #define DEBUG_FEATURE__ENABLE_MULTI_LEVEL_BUFF // ���ö༶����
// #define DEBUG_FEATURE__ENABLE_AUTO_EXTENSION // �����Զ���չ

using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System.Text;
using System.Text.Json;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Object;

///
///Credit to likefengzi(https://github.com/likefengzi)(https://space.bilibili.com/237491236)
///
namespace CustomizeLib.BepInEx
{
    /// <summary>
    /// ע���ں������䷽
    /// </summary>
    [HarmonyPatch(typeof(MixBomb), nameof(MixBomb.AttributeEvent))]
    public class MixBombPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(MixBomb __instance)
        {
            bool success = false;
            if (__instance is not null)
            {
                List<Plant> plants = Lawnf.Get1x1Plants(__instance.thePlantColumn, __instance.thePlantRow).ToArray().ToList();
                if (plants is null)
                    return true;
                foreach (Plant plant in plants)
                {
                    if (plant is not null && CustomCore.CustomMixBombFusions.Keys.Any(k => k.Item2 == plant.thePlantType))
                    {
                        List<(PlantType, PlantType, PlantType)> mixBombFusions = CustomCore.CustomMixBombFusions
                            .Where(kvp => kvp.Key.Item2 == plant.thePlantType)
                            .Select(kvp => kvp.Key)
                            .ToList();
                        List<Plant> leftPlant = Lawnf.Get1x1Plants(__instance.thePlantColumn - 1, __instance.thePlantRow).ToArray().ToList();
                        List<Plant> rightPlant = Lawnf.Get1x1Plants(__instance.thePlantColumn + 1, __instance.thePlantRow).ToArray().ToList();
                        foreach ((PlantType, PlantType, PlantType) fusion in mixBombFusions)
                        {
                            Plant? firstLeftPlant = leftPlant.FirstOrDefault(p => p.thePlantType == fusion.Item1);
                            Plant? firstRightPlant = rightPlant.FirstOrDefault(p => p.thePlantType == fusion.Item3);
                            if (firstLeftPlant == null || firstRightPlant == null)
                            {
                                CustomCore.CustomMixBombFusions[fusion].Item2[UnityEngine.Random.Range(0, CustomCore.CustomMixBombFusions[fusion].Item2.Count)](firstLeftPlant, plant, firstRightPlant);
                                continue;
                            }
                            if (leftPlant.Any(p => p.thePlantType == fusion.Item1) && rightPlant.Any(p => p.thePlantType == fusion.Item3))
                            {
                                CustomCore.CustomMixBombFusions[fusion].Item1[UnityEngine.Random.Range(0, CustomCore.CustomMixBombFusions[fusion].Item1.Count)](firstLeftPlant, plant, firstRightPlant);
                                success = true;
                            }
                            else
                            {
                                CustomCore.CustomMixBombFusions[fusion].Item2[UnityEngine.Random.Range(0, CustomCore.CustomMixBombFusions[fusion].Item2.Count)](firstLeftPlant, plant, firstRightPlant);
                            }
                        }
                    }
                }
            }
            if (__instance is not null && success)
                __instance.Die();
            if (success)
                return false;
            return true;
        }
    }

    /// <summary>
    /// ע�����ʹ���¼�
    /// </summary>
    [HarmonyPatch(typeof(Fertilize))]
    public class FertilizePatch
    {
        [HarmonyPatch(nameof(Fertilize.Upgrade))]
        [HarmonyPostfix]
        public static void PostUpgrade(Fertilize __instance)
        {
            if (__instance == null || __instance.theTargetPlant == null) return;

            int column = __instance.theTargetPlant.thePlantColumn;
            int row = __instance.theTargetPlant.thePlantRow;

            List<Plant> plants = Lawnf.Get1x1Plants(column, row).ToArray().ToList<Plant>(); // ��ȡֲ�il2cpp�Ѱ���
            if (plants == null) return;

            for (int i = 0; i < plants.Count; i++)
            {
                Plant plant = plants[i];
                if (plant == null) continue;
                if (plant.thePlantColumn != column || plant.thePlantRow != row) continue;
                if (Board.Instance == null) return;

                if (CustomCore.CustomUseFertilize.ContainsKey(plant.thePlantType))
                {
                    CustomCore.CustomUseFertilize[plant.thePlantType](plant);
                }
            }

            UnityEngine.Object.Destroy(__instance.gameObject);
        }
    }

    [HarmonyPatch(typeof(AlmanacMenu))]
    public static class AlmanacMenuPatch
    {
        [HarmonyPatch(nameof(AlmanacMenu.Awake))]
        [HarmonyPostfix]
        public static void PostAwake(AlmanacMenu __instance)
        {
            __instance.transform.FindChild("AlmanacPlant2").FindChild("Cards").GetComponent<GridManager>().maxY = GameAPP.resourcesManager.allPlants.Count / 9 * 1.5f;
        }
    }

    /// <summary>
    /// ��ʼ��������ʾ������ť������Ƥ��
    /// </summary>
    /// <param name="__instance"></param>
    /// <returns></returns>
    /// <summary>
    /// ֲ��ͼ��
    /// </summary>
    [HarmonyPatch(typeof(AlmanacPlantBank))]
    public static class AlmanacMgrPatch
    {
        /// <summary>
        /// ��ʼ��������ʾ������ť������Ƥ��
        /// </summary>
        /// <param name="__instance"></param>
        /// <returns></returns>
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void PostStart(AlmanacPlantBank __instance)
        {
            PlantType plantType = (PlantType)__instance.theSeedType;
            //���μ���Ƥ��
            if (!CustomCore.CustomPlantsSkin.ContainsKey(plantType))
            {
                //�Ƿ���Ƥ���ɹ�
                bool buttonFlag = __instance.skinButton.active;
                //exe��λ��
                string? fullName = Directory.GetParent(Application.dataPath)?.FullName;
                if (fullName != null)
                {
                    //Ѱ��Mods/Skin/
                    string modsPath = Path.Combine(fullName, "BepInEx", "plugins", "Skin");
                    if (Directory.Exists(modsPath))
                    {
                        //ֻҪskin_��ͷ���ļ�
                        string[] files = Directory.GetFiles(modsPath, "skin_*");

                        foreach (string file in files)
                        {
                            try
                            {
                                //����ļ���"Skin_"�����idƥ��
                                if (((int)plantType).ToString() == Path.GetFileName(file)[5..])
                                {
                                    //������Դ�ļ�
                                    AssetBundle ab = AssetBundle.LoadFromFile(file);
                                    //���Լ���json
                                    bool jsonFlag = false;
                                    CustomPlantData plantDataFromJson = default;
                                    CustomPlantAlmanac plantAlmanac = default;
                                    Dictionary<int, int> bulletTypesFormJson = new Dictionary<int, int>();
                                    foreach (string jsonFile in files)
                                    {
                                        try
                                        {
                                            if (((int)plantType) + ".json" ==
                                                Path.GetFileName(jsonFile)[5..])
                                            {
                                                // ��ȡ JSON �ļ�����
                                                string jsonContent = File.ReadAllText(jsonFile);

                                                // �����л� JSON ����
                                                var options = new JsonSerializerOptions
                                                {
                                                    PropertyNameCaseInsensitive = true // ���������ִ�Сд����������ƥ��
                                                };

                                                JsonSkinObject? root =
                                                    JsonSerializer.Deserialize<JsonSkinObject>(jsonContent, options);

                                                // ��������
                                                if (root != null)
                                                {
                                                    plantDataFromJson = root.CustomPlantData;
                                                    root.TypeMgrExtraSkin.AddValueToTypeMgrExtraSkinBackup(plantType);
                                                    bulletTypesFormJson = root.CustomBulletType;
                                                    plantAlmanac = root.PlantAlmanac;
                                                }

                                                //�ҵ���json�ļ����ɹ�����
                                                jsonFlag = true;
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            Console.WriteLine(e);
                                        }
                                    }

                                    //�����Ƥ��Ԥ����
                                    GameObject? newPrefab = null;
                                    try
                                    {
                                        newPrefab = ab.GetAsset<GameObject>("Prefab");
                                        newPrefab.tag = "Plant";
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine(e);
                                    }

                                    //�����Ƥ��Ԥ��ͼ
                                    GameObject? newPreview = null;
                                    try
                                    {
                                        newPreview = ab.GetAsset<GameObject>("Preview");
                                        newPreview.tag = "Preview";
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine(e);
                                    }

                                    //�ɹ�����Ԥ����
                                    if (newPrefab != null)
                                    {
                                        //�ɵ�Ԥ����
                                        GameObject prefab;
                                        try
                                        {
                                            prefab = GameAPP.resourcesManager.plantPrefabs[jsonFlag ? (PlantType)plantDataFromJson.ID : plantType];
                                        }
                                        catch (Exception e)
                                        {
                                            Console.WriteLine(e);
                                            prefab = GameAPP.resourcesManager.plantPrefabs[plantType];
                                        }

                                        //�õ��ű�
                                        Plant plant = prefab.GetComponent<Plant>();
                                        //���ӵ��µ�Ԥ������
                                        newPrefab.AddComponent(plant.GetIl2CppType());
                                        CustomPlantMonoBehaviour temp =
                                            newPrefab.AddComponent<CustomPlantMonoBehaviour>();
                                        CustomPlantMonoBehaviour.BulletTypes.Add(plantType, bulletTypesFormJson);

                                        Plant newPlant = newPrefab.GetComponent<Plant>();

                                        //ָ��id
                                        newPlant.thePlantType = plantType;

                                        //shoot��Ա�������⣬���
                                        newPlant.shoot = null;
                                        newPlant.shoot2 = null;
                                        //ָ��shoot
                                        try
                                        {
                                            newPlant.FindShoot(newPrefab.transform);
                                        }
                                        catch (Exception e)
                                        {
                                            Console.WriteLine(e);
                                        }

                                        //将皮肤添加到_plantPrefabs和_plantPreviews列表中
                                        if (!GameAPP.resourcesManager._plantPrefabs.ContainsKey(plantType))
                                        {
                                            GameAPP.resourcesManager._plantPrefabs[plantType] = new Il2CppSystem.Collections.Generic.List<GameObject>();
                                        }
                                        if (!GameAPP.resourcesManager._plantPreviews.ContainsKey(plantType))
                                        {
                                            GameAPP.resourcesManager._plantPreviews[plantType] = new Il2CppSystem.Collections.Generic.List<GameObject>();
                                        }

                                        // 添加皮肤预制体到列表
                                        GameAPP.resourcesManager._plantPrefabs[plantType].Add(newPrefab);
                                        if (newPreview != null)
                                        {
                                            GameAPP.resourcesManager._plantPreviews[plantType].Add(newPreview);
                                        }
                                        else
                                        {
                                            // 如果没有自定义预览，使用原始预览
                                            GameObject originalPreview = GameAPP.resourcesManager.plantPreviews[plantType];
                                            if (originalPreview != null)
                                            {
                                                GameAPP.resourcesManager._plantPreviews[plantType].Add(originalPreview);
                                            }
                                        }
                                    }

                                    CustomPlantData newCustomPlantData = default;
                                    //�ж��Ƿ�ɹ����ض�Ӧ��json
                                    if (jsonFlag)
                                    {
                                        //ʹ��json�е�����
                                        newCustomPlantData = new()
                                        {
                                            ID = (int)plantType,
                                            PlantData = plantDataFromJson.PlantData,
                                            Prefab = GameAPP.resourcesManager.plantPrefabs[plantType],
                                            Preview = GameAPP.resourcesManager.plantPreviews[plantType]
                                        };
                                    }
                                    else
                                    {
                                        //û��json�ļ���ʹ��Ĭ������
                                        //���ݼ��ص��Զ���Ƥ����
                                        newCustomPlantData = new()
                                        {
                                            ID = (int)plantType,
                                            PlantData = PlantDataLoader.plantDatas[plantType],
                                            Prefab = GameAPP.resourcesManager.plantPrefabs[plantType],
                                            Preview = GameAPP.resourcesManager.plantPreviews[plantType]
                                        };
                                    }

                                    //�ɹ���ȡ��˭�ͼ���˭
                                    if (newPrefab != null)
                                    {
                                        newCustomPlantData.Prefab = newPrefab;
                                    }

                                    if (newPreview != null)
                                    {
                                        newCustomPlantData.Preview = newPreview;
                                    }

                                    CustomCore.CustomPlantsSkin.Add(plantType, newCustomPlantData);
                                    //����ͼ��
                                    try
                                    {
                                        CustomCore.PlantsSkinAlmanac.Add(plantType, jsonFlag ?
                                            (plantAlmanac.Name, plantAlmanac.Description) : null);
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine(e);
                                    }

                                    //��Ƥ������ť������ʾ
                                    buttonFlag = true;
                                    CustomCore.CustomPlantsSkinActive[plantType] = false;
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                        }
                    }
                }

                __instance.skinButton.SetActive(buttonFlag);
            }
            else
            {
                //��Ƥ������ť������ʾ
                __instance.skinButton.SetActive(true);
            }

            if (CustomCore.CustomPlants.ContainsKey(plantType))
            {
                //����ֲ���ť������ʾ
                __instance.skinButton.SetActive(CustomCore.CustomPlantsSkin.ContainsKey(plantType));
            }
        }

        /// <summary>
        /// ��json����ֲ����Ϣ
        /// </summary>
        /// <param name="__instance"></param>
        /// <returns></returns>
        [HarmonyPatch("InitNameAndInfoFromJson")]
        [HarmonyPrefix]
        public static bool PreInitNameAndInfoFromJson(AlmanacPlantBank __instance)
        {
            //����Զ���ֲ��ͼ����Ϣ����
            if (CustomCore.PlantsAlmanac.ContainsKey((PlantType)__instance.theSeedType))
            {
                //����ͼ���ϵ����
                for (int i = 0; i < __instance.transform.childCount; i++)
                {
                    Transform childTransform = __instance.transform.GetChild(i);
                    if (childTransform == null)
                    {
                        continue;
                    }

                    //ֲ������
                    if (childTransform.name == "Name")
                    {
                        childTransform.GetComponent<TextMeshPro>().text =
                            CustomCore.PlantsAlmanac[(PlantType)__instance.theSeedType].Item1;
                        childTransform.GetChild(0).GetComponent<TextMeshPro>().text =
                            CustomCore.PlantsAlmanac[(PlantType)__instance.theSeedType].Item1;
                    }

                    //ֲ����Ϣ
                    if (childTransform.name == "Info")
                    {
                        TextMeshPro info = childTransform.GetComponent<TextMeshPro>();
                        info.overflowMode = TextOverflowModes.Page;
                        info.fontSize = 40;
                        info.text = CustomCore.PlantsAlmanac[(PlantType)__instance.theSeedType].Item2;
                        __instance.introduce = info;
                    }

                    //ֲ������
                    if (childTransform.name == "Cost")
                    {
                        childTransform.GetComponent<TextMeshPro>().text = "";
                    }
                }

                //���ԭʼ�ļ���
                return false;
            }

            if (CustomCore.CustomPlantsSkinActive.ContainsKey((PlantType)__instance.theSeedType) && CustomCore.PlantsSkinAlmanac.ContainsKey((PlantType)__instance.theSeedType) && CustomCore.CustomPlantsSkinActive[(PlantType)__instance.theSeedType])
            {
                var alm = CustomCore.PlantsSkinAlmanac[(PlantType)__instance.theSeedType];
                if (alm is null) return true;
                var almanac = alm.Value;
                //����ͼ���ϵ����
                for (int i = 0; i < __instance.transform.childCount; i++)
                {
                    Transform childTransform = __instance.transform.GetChild(i);
                    if (childTransform == null)
                    {
                        continue;
                    }

                    //ֲ������
                    if (childTransform.name == "Name")
                    {
                        childTransform.GetComponent<TextMeshPro>().text = almanac.Item1;
                        childTransform.GetChild(0).GetComponent<TextMeshPro>().text = almanac.Item1;
                    }

                    //ֲ����Ϣ
                    if (childTransform.name == "Info")
                    {
                        TextMeshPro info = childTransform.GetComponent<TextMeshPro>();
                        info.overflowMode = TextOverflowModes.Page;
                        info.fontSize = 40;
                        info.text = almanac.Item2;
                        __instance.introduce = info;
                    }

                    //ֲ������
                    if (childTransform.name == "Cost")
                    {
                        childTransform.GetComponent<TextMeshPro>().text = "";
                    }
                }

                //���ԭʼ�ļ���
                return false;
            }

            return true;
        }

        /// <summary>
        /// ͼ������갴�£����ڷ�ҳ
        /// </summary>
        /// <param name="__instance"></param>
        /// <returns></returns>
        [HarmonyPatch("OnMouseDown")]
        [HarmonyPrefix]
        public static bool PreOnMouseDown(AlmanacPlantBank __instance)
        {
            //�Ҳ���ʾ
            __instance.introduce =
                __instance.gameObject.transform.FindChild("Info").gameObject.GetComponent<TextMeshPro>();
            //ҳ��
            __instance.pageCount = __instance.introduce.m_pageNumber * 1;
            //��һҳ
            if (__instance.currentPage <= __instance.introduce.m_pageNumber)
            {
                ++__instance.currentPage;
            }
            else
            {
                __instance.currentPage = 1;
            }

            //��ҳ
            __instance.introduce.pageToDisplay = __instance.currentPage;

            //���ԭʼ��ҳ
            return false;
        }
    }

    [HarmonyPatch(typeof(AlmanacMgrZombie))]
    public static class AlmanacMgrZombiePatch
    {
        [HarmonyPatch("InitNameAndInfoFromJson")]
        [HarmonyPrefix]
        public static bool PreInitNameAndInfoFromJson(AlmanacMgrZombie __instance)
        {
            if (CustomCore.ZombiesAlmanac.ContainsKey(__instance.theZombieType))
            {
                for (int i = 0; i < __instance.transform.childCount; i++)
                {
                    Transform childTransform = __instance.transform.GetChild(i);
                    if (childTransform == null)
                        continue;
                    if (childTransform.name == "Name")
                    {
                        childTransform.GetComponent<TextMeshPro>().text = CustomCore.ZombiesAlmanac[__instance.theZombieType].Item1;
                        childTransform.GetChild(0).GetComponent<TextMeshPro>().text = CustomCore.ZombiesAlmanac[__instance.theZombieType].Item1;
                    }
                    if (childTransform.name == "Info")
                    {
                        TextMeshPro info = childTransform.GetComponent<TextMeshPro>();
                        info.overflowMode = TextOverflowModes.Page;
                        info.fontSize = 40;
                        info.text = CustomCore.ZombiesAlmanac[__instance.theZombieType].Item2;
                        __instance.introduce = info;
                    }
                    if (childTransform.name == "Cost")
                        childTransform.GetComponent<TextMeshPro>().text = "";
                }
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(ConveyBeltMgr))]
    public static class ConveyBeltMgrPatch
    {
        [HarmonyPatch(nameof(ConveyBeltMgr.Awake))]
        [HarmonyPostfix]
        public static void PostAwake(ConveyBeltMgr __instance)
        {
            if (Utils.IsCustomLevel(out var levelData) && levelData.BoardTag.isConvey && levelData.ConveyBeltPlantTypes().Count > 0)
            {
                __instance.plants = levelData.ConveyBeltPlantTypes().ToIl2CppList();
            }
        }

        [HarmonyPatch(nameof(ConveyBeltMgr.GetCardPool))]
        [HarmonyPostfix]
        public static void PostGetCardPool(ref Il2CppSystem.Collections.Generic.List<PlantType> __result)
        {
            if (Utils.IsCustomLevel(out var levelData) && levelData.BoardTag.isConvey && levelData.ConveyBeltPlantTypes().Count > 0)
            {
                __result = levelData.ConveyBeltPlantTypes().ToIl2CppList();
            }
        }
    }

    /// <summary>
    /// Ϊ����ֲ�︽��ֲ������
    /// </summary>
    [HarmonyPatch(typeof(CreatePlant))]
    public static class CreatePlantPatch
    {
        [HarmonyPatch(nameof(CreatePlant.SetPlant))]
        [HarmonyPostfix]
        public static void Postfix_SetPlant(CreatePlant __instance, ref int newColumn, ref int newRow, ref GameObject __result)
        {
            if (__result is not null && __result.TryGetComponent<Plant>(out var plant) &&
                CustomCore.CustomPlantTypes.Contains(plant.thePlantType))
            {
                TypeMgr.GetPlantTag(plant);
            }
        }

        [HarmonyPatch(nameof(CreatePlant.LimTravel))]
        [HarmonyPostfix]
        public static void Postfix_LimTravel(CreatePlant __instance, ref PlantType theSeedType, ref bool __result)
        {
            bool isCanSet = false;
            if (TravelMgr.Instance != null && TravelMgr.Instance.ulockTemp.Contains(theSeedType))
                isCanSet = true;
            if (__instance.board.boardTag.enableAllTravelPlant || __instance.board.boardTag.enableTravelPlant || __instance.board.boardTag.isTravel)
                isCanSet = true;

            if (CustomCore.CustomUltimatePlants.Contains(theSeedType) && !isCanSet)
            {
                __result = true;
                InGameText.Instance.ShowText("���䷽����������ϵ�л���Ԩ����", 3f, false);
            }
        }

        [HarmonyPatch(nameof(CreatePlant.MixBombCheck))]
        [HarmonyPrefix]
        public static bool Prefix_MixBombCheck(CreatePlant __instance, ref int theBoxColumn, ref int theBoxRow, ref bool __result)
        {
            List<Plant> plants = Lawnf.Get1x1Plants(theBoxColumn, theBoxRow).ToArray().ToList();
            foreach (var plant in plants)
            {
                if (plant == null) continue;
                if (CustomCore.CustomMixBombFusions.Any(kvp => kvp.Key.Item2 == plant.thePlantType))
                {
                    __result = true;
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Lawnf))]
    public class LawnfPatch
    {
        [HarmonyPatch(nameof(Lawnf.GetUpgradedPlantCost))]
        [HarmonyPrefix]
        public static bool Prefix(ref PlantType thePlantType, ref int targetLevel, ref int __result)
        {
            if (CustomCore.CustomUltimatePlants.Contains(thePlantType))
            {
                __result = 1500 * (targetLevel) * (targetLevel + 1) / 2;
                return false;
            }
            return true;
        }

        [HarmonyPatch(nameof(Lawnf.IsUltiPlant))]
        [HarmonyPrefix]
        public static bool Prefix(ref PlantType thePlantType, ref bool __result)
        {
            if (CustomCore.CustomPlantTypes.Contains(thePlantType))
            {
                __result = CustomCore.CustomUltimatePlants.Contains(thePlantType);
                return false;
            }
            return true;
        }

        [HarmonyPatch(nameof(Lawnf.GetUltimatePlants))]
        [HarmonyPostfix]
        public static void Postfix(ref Il2CppSystem.Collections.Generic.List<PlantType> __result)
        {
            foreach (PlantType plantType in CustomCore.CustomUltimatePlants)
            {
                if (!__result.Contains(plantType))
                {
                    __result.Add(plantType);
                }
            }
        }
    }

    /// <summary>
    /// �������Button�����ض���ֲ�����
    /// </summary>
    [HarmonyPatch(typeof(UIButton))]
    public static class HideCustomPlantCards
    {
        [HarmonyPatch(nameof(UIButton.OnMouseUpAsButton))]
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (SelectCustomPlants.MyPageParent != null && SelectCustomPlants.MyPageParent.active)
                SelectCustomPlants.MyPageParent.SetActive(false);
        }

        [HarmonyPatch(nameof(UIButton.Start))]
        [HarmonyPostfix]
        public static void PostfixStart(UIButton __instance)
        {
            if (__instance.name == "LastPage" && Board.Instance != null && Board.Instance.isIZ)
            {
                SelectCustomPlants.InitCustomCards_IZ();
            }
        }
    }

    [HarmonyPatch(typeof(InGameUI))]
    public static class InGameUIPatch
    {
        [HarmonyPatch(nameof(InGameUI.SetUniqueText))]
        [HarmonyPostfix]
        public static void PostSetUniqueText(InGameUI __instance, ref Il2CppReferenceArray<TextMeshProUGUI> T)
        {
            if (GameAPP.theBoardType is (LevelType)66)
            {
                __instance.ChangeString(T, CustomCore.CustomLevels[GameAPP.theBoardLevel].Name());
            }
        }

        [HarmonyPatch(nameof(InGameUI.MoveCard))]
        [HarmonyPrefix]
        public static void PreMoveCard(ref CardUI card)
        {
            foreach (CheckCardState check in CustomCore.checkBehaviours)
            {
                if (check != null)
                {
                    check.movingCardUI = card;
                    check.CheckState();
                }
            }
        }

        [HarmonyPatch(nameof(InGameUI.RemoveCardFromBank))]
        [HarmonyPostfix]
        public static void PostReMoveCardFromBank(ref CardUI card)
        {
            foreach (CheckCardState check in CustomCore.checkBehaviours)
            {
                if (check != null)
                {
                    check.movingCardUI = card;
                    check.CheckState();
                }
            }
        }
    }

    [HarmonyPatch(typeof(InitBoard))]
    public static class InitBoardPatch
    {
        [HarmonyPatch(nameof(InitBoard.PreSelectCard))]
        [HarmonyPostfix]
        public static void PostPreSelectCard(InitBoard __instance)
        {
            if (GameAPP.theBoardType is (LevelType)66)
            {
                foreach (var c in CustomCore.CustomLevels[GameAPP.theBoardLevel].PreSelectCards())
                {
                    __instance.PreSelect(c);
                }
            }
        }

        [HarmonyPatch(nameof(InitBoard.RightMoveCamera))]
        [HarmonyPostfix]
        public static void PostRightMoveCamera()
        {
            if (GameAPP.theBoardType is not (LevelType)66) return;
            var levelData = CustomCore.CustomLevels[GameAPP.theBoardLevel];
            var travelMgr = GameAPP.gameAPP.GetOrAddComponent<TravelMgr>();
            foreach (var a in levelData.AdvBuffs())
            {
                if (a >= 0 && a < travelMgr.advancedUpgrades.Count)
                {
                    travelMgr.advancedUpgrades[a] = true;
                }
            }
            foreach (var u in levelData.UltiBuffs())
            {
                if (u.Item1 >= 0 && u.Item1 < travelMgr.ultimateUpgrades.Count && u.Item2 >= 0)
                {
                    travelMgr.ultimateUpgrades[u.Item1] = u.Item2;
                }
            }
            foreach (var p in levelData.UnlockPlants())
            {
                if (p >= 0 && p < travelMgr.unlockPlant.Count)
                {
                    travelMgr.unlockPlant[p] = true;
                }
            }
            foreach (var d in levelData.Debuffs())
            {
                if (d >= 0 && d < travelMgr.debuff.Count)
                {
                    travelMgr.debuff[d] = true;
                }
            }
        }

        [HarmonyPatch(nameof(InitBoard.MoveOverEvent))]
        [HarmonyPrefix]
        public static bool PreMoveOverEvent(InitBoard __instance, ref string direction)
        {
            if (GameAPP.theBoardType is not (LevelType)66) return true;
            var levelData = CustomCore.CustomLevels[GameAPP.theBoardLevel];
            if (direction == "right")
            {
                if (__instance.board is not null)
                {
                    if (__instance.board.cardSelectable)
                    {
                        // ������Ϸ״̬
                        GameAPP.theGameStatus = GameStatus.Selecting;

                        // UI����
                        InGameUI.Instance.ConveyorBelt.SetActive(false);
                        InGameUI.Instance.Bottom.SetActive(true);

                        // ����Э���ƶ�UIԪ��
                        __instance.StartCoroutine(__instance.MoveDirection(InGameUI.Instance.SeedBank, 79f, 0));
                        __instance.StartCoroutine(__instance.MoveDirection(InGameUI.Instance.Bottom, 525f, 1));
                    }
                    else
                    {
                        // �ӳ�ִ�з���
                        __instance.Invoke("LeftMoveCamera", 1.5f);
                        InGameUI.Instance.Bottom.SetActive(false);
                    }
                }
            }
            else if (direction == "left")
            {
                if (__instance.board is null) return false;

                if (!__instance.board.cardSelectable)
                {
                    if (__instance.board.cardBank)
                    {
                        __instance.StartCoroutine(__instance.MoveDirection(InGameUI.Instance.SeedBank, 79f, 0));
                        __instance.AddCard();
                    }
                    else
                    {
                        InGameUI.Instance.SeedBank.SetActive(false);
                    }
                    InGameUI.Instance.Bottom.SetActive(false);
                }

                // ��������Э��
                __instance.StartCoroutine(__instance.DecreaseVolume());

                // ����UIλ��
                InGameUI.Instance.LowerUI();

                // ��ʼ����ݻ����ض�ģʽ�£�
                if (!__instance.board.boardTag.disableMower)
                {
                    __instance.InitMower();
                }

                // ��Ч���ƶ�
                if (__instance.board.fog != null)
                {
                    Vector3 fogPosition = __instance.board.fog.transform.position;
                    Vector3 boardPosition = __instance.board.background.transform.position;

                    FogMgr.Instance.MoveObject(
                        new(fogPosition.x,
                        fogPosition.y,
                        boardPosition.z),
                        10f  // �ƶ��ٶ�
                    );
                }

                // BOSSս���⴦��
                float invokeDelay = 0.5f;
                if (__instance.board.boardTag.isBoss || __instance.board.boardTag.isBoss2)
                {
                    GameObject zombie = CreateZombie.Instance.SetZombie(0, levelData.RealBoss2 ? ZombieType.ZombieBoss2 : ZombieType.ZombieBoss, 0f);
                    Zombie zombieComp = zombie.GetComponent<Zombie>();

                    if (__instance.board.boss2)
                    {
                        Lawnf.SetZombieHealth(zombieComp, 5f);
                    }
                    invokeDelay = 3.5f;
                    __instance.board.boss2 = levelData.RealBoss2;
                }

                // �ӳٵ��÷���
                __instance.Invoke("ReadySetPlant", invokeDelay);
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(InitZombieList))]
    public static class InitZombieListPatch
    {
        [HarmonyPatch(nameof(InitZombieList.InitZombie))]
        [HarmonyPostfix]
        public static void PostInitZombie()
        {
            if (Utils.IsCustomLevel(out var levelData))
            {
                foreach (var z in levelData.ZombieList())
                {
                    InitZombieList.zombieTypeList.Add(z);
                    InitZombieList.allow[(int)z] = true;
                    for (int i = 0; i < InitZombieList.zombieList.Count; i++)
                    {
                        Il2CppSystem.Collections.Generic.List<ZombieType> zombieList = InitZombieList.zombieList[i];
                        InitZombieList.zombieList.Clear();
                        int rand = UnityEngine.Random.Range(3, 10);
                        if (i % 10 == 0)
                            rand = UnityEngine.Random.Range(8, 15);
                        if (i <= 3)
                            rand = UnityEngine.Random.Range(1, 5);
                        for (int j = 0; j < rand; j++)
                        {
                            int rand_index = UnityEngine.Random.Range(0, levelData.ZombieList().Count);
                            zombieList.Add(levelData.ZombieList()[rand_index]);
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Money))]
    public static class MoneyPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("ReinforcePlant")]
        public static bool PreReinforcePlant(Money __instance, ref Plant plant)
        {
            if (CustomCore.SuperSkills.ContainsKey(plant.thePlantType))
            {
                var cost = CustomCore.SuperSkills[plant.thePlantType].Item1(plant);

                if (Board.Instance.theMoney < cost)
                {
                    InGameText.Instance.ShowText($"������Ҫ{cost}���", 5);
                    return false;
                }
                if (plant.SuperSkill())
                {
                    CustomCore.SuperSkills[plant.thePlantType].Item2(plant);
                    plant.AnimSuperShoot();
                    __instance.UsedEvent(plant.thePlantColumn, plant.thePlantRow, cost);
                    __instance.OtherSuperSkill(plant);
                }
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Mouse))]
    public static class MousePatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("GetPlantsOnMouse")]
        public static void PostGetPlantsOnMouse(ref Il2CppSystem.Collections.Generic.List<Plant> __result)
        {
            for (int i = __result.Count - 1; i >= 0; i--)
            {
                if (__result[i] is not null && TypeMgr.BigNut(__result[i].thePlantType))
                {
                    __result.RemoveAt(i);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("LeftClickWithNothing")]
        public static void PostLeftClickWithNothing()
        {
            // 修正：Physics2D.RaycastAll 返回 RaycastHit2D[]，逐一检查每个 hit 的 collider.gameObject
            foreach (RaycastHit2D hit in Physics2D.RaycastAll(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero))
            {
                GameObject gameObject = hit.collider != null ? hit.collider.gameObject : null;
                if (gameObject != null && gameObject.TryGetComponent<Plant>(out var plant) && CustomCore.CustomPlantClicks.ContainsKey(plant.thePlantType))
                {
                    CustomCore.CustomPlantClicks[plant.thePlantType](plant);
                    return;
                }
            }
        }
    }

    [HarmonyPatch(typeof(NoticeMenu), nameof(NoticeMenu.Start))]
    public static class NoticeMenuPatch
    {
        [HarmonyPrefix]
        public static void Postfix()
        {
#if DEBUG_FEATURE__ENABLE_AUTO_EXTENSION
            #region �Զ�����
            // ����plantData
            if (CustomCore.CustomPlants.Count > 0)
            {
                long size_plantData = (int)CustomCore.CustomPlants.Keys.Max() < PlantDataLoader.plantData.Length ? PlantDataLoader.plantData.Length : (int)CustomCore.CustomPlants.Keys.Max();
                PlantDataLoader.PlantData_[] plantData = new PlantDataLoader.PlantData_[size_plantData + 1];
                Array.Copy(PlantDataLoader.plantData, plantData, PlantDataLoader.plantData.Length);
                PlantDataLoader.plantData = plantData;
            }

            // ����particlePrefab
            if (CustomCore.CustomParticles.Count > 0)
            {
                long size_particlePrefab = (int)CustomCore.CustomParticles.Keys.Max() < GameAPP.particlePrefab.Length ? GameAPP.particlePrefab.Length : (int)CustomCore.CustomParticles.Keys.Max();
                GameObject[] particlePrefab = new GameObject[size_particlePrefab + 1];
                Array.Copy(GameAPP.particlePrefab, particlePrefab, GameAPP.particlePrefab.Length);
                GameAPP.particlePrefab = particlePrefab;
            }

            // ����spritePrefab
            if (CustomCore.CustomSprites.Count > 0)
            {
                long size_spritePrefab = CustomCore.CustomSprites.Keys.Max() < GameAPP.spritePrefab.Length ? GameAPP.spritePrefab.Length : CustomCore.CustomSprites.Keys.Max();
                Sprite[] spritePrefab = new Sprite[size_spritePrefab + 1];
                Array.Copy(GameAPP.spritePrefab, spritePrefab, GameAPP.spritePrefab.Length);
                GameAPP.spritePrefab = spritePrefab;
            }
#endregion
#endif
            foreach (var plant in CustomCore.CustomPlants)//����ֲ��
            {
                GameAPP.resourcesManager.plantPrefabs[plant.Key] = plant.Value.Prefab;//ע��Ԥ����
                GameAPP.resourcesManager.plantPrefabs[plant.Key].tag = "Plant";//�����tag
                if (!GameAPP.resourcesManager.allPlants.Contains(plant.Key))
                    GameAPP.resourcesManager.allPlants.Add(plant.Key);//ע��ֲ������
                if (plant.Value.PlantData is not null)
                {
                    PlantDataLoader.plantData[(int)plant.Key] = plant.Value.PlantData;//ע��ֲ������
                    PlantDataLoader.plantDatas.Add(plant.Key, plant.Value.PlantData);
                }
                GameAPP.resourcesManager.plantPreviews[plant.Key] = plant.Value.Preview;//ע��ֲ��Ԥ��
                GameAPP.resourcesManager.plantPreviews[plant.Key].tag = "Preview";//���޴�tag
            }
            Il2CppSystem.Array array = MixData.data.Cast<Il2CppSystem.Array>();//ע���ں��䷽
            foreach (var f in CustomCore.CustomFusions)
            {
                array.SetValue(f.Item1, f.Item2, f.Item3);
            }

            foreach (var z in CustomCore.CustomZombies)//ע�������ʬ
            {
                if (!GameAPP.resourcesManager.allZombieTypes.Contains(z.Key))
                    GameAPP.resourcesManager.allZombieTypes.Add(z.Key);//ע�Ὡʬ����
                GameAPP.resourcesManager.zombiePrefabs[z.Key] = z.Value.Item1;//ע�ὩʬԤ����
                GameAPP.resourcesManager.zombiePrefabs[z.Key].tag = "Zombie";//���޴�tag
            }

            foreach (var bullet in CustomCore.CustomBullets)//ע������ӵ�
            {
                GameAPP.resourcesManager.bulletPrefabs[bullet.Key] = bullet.Value;//ע���ӵ�Ԥ����
                if (!GameAPP.resourcesManager.allBullets.Contains(bullet.Key))
                    GameAPP.resourcesManager.allBullets.Add(bullet.Key);//ע���ӵ�����
            }

            foreach (var par in CustomCore.CustomParticles)//ע������Ч��
            {
                GameAPP.particlePrefab[(int)par.Key] = par.Value;
                GameAPP.resourcesManager.particlePrefabs[par.Key] = par.Value;//ע������Ч��Ԥ����
                if (!GameAPP.resourcesManager.allParticles.Contains(par.Key))
                    GameAPP.resourcesManager.allParticles.Add(par.Key);//ע������Ч������
            }

            foreach (var spr in CustomCore.CustomSprites)//ע���Զ��徫����ͼ
            {
                GameAPP.spritePrefab[spr.Key] = spr.Value;
            }
        }
    }

    [HarmonyPatch(typeof(Plant))]
    public static class PlantPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("UseItem")]
        public static void PostUseItem(Plant __instance, ref BucketType type, ref Bucket bucket)
        {
            if (CustomCore.CustomUseItems.ContainsKey((__instance.thePlantType, type)))
            {
                CustomCore.CustomUseItems[(__instance.thePlantType, type)](__instance);
                UnityEngine.Object.Destroy(bucket.gameObject);
            }
        }
    }
    /// <summary>
    /// ˢ�¿�����ͼ
    /// </summary>
    [HarmonyPatch(typeof(SeedLibrary))]
    public static class SeedLibraryPatch
    {
        [HarmonyPatch(nameof(SeedLibrary.Start))]
        [HarmonyPostfix]
        public static void PostStart(SeedLibrary __instance)
        {
            // ע���Զ��忨��
            GameObject? MyColorfulCard = Utils.GetColorfulCardGameObject();
            Dictionary<PlantType, List<Transform?>> parents_colorful = new Dictionary<PlantType, List<Transform?>>();
            List<PlantType> cardsOnSeedBank = new List<PlantType>();
            Dictionary<PlantType, List<bool>> cardsOnSeedBankExtra = new Dictionary<PlantType, List<bool>>();
            GameObject? seedGroup = null;
            if (Board.Instance != null && !Board.Instance.isIZ)
                seedGroup = InGameUI.Instance.SeedBank.transform.GetChild(0).gameObject;
            else if (Board.Instance != null && Board.Instance.isIZ)
                seedGroup = InGameUI_IZ.Instance.transform.FindChild("SeedBank/SeedGroup").gameObject;
            if (seedGroup == null)
                return;
            for (int i = 0; i < seedGroup.transform.childCount; i++)
            {
                GameObject seed = seedGroup.transform.GetChild(i).gameObject;
                if (seed.transform.childCount > 0)
                {
                    cardsOnSeedBank.Add(seed.transform.GetChild(0).GetComponent<CardUI>().thePlantType);
                    if (!cardsOnSeedBankExtra.ContainsKey(seed.transform.GetChild(0).GetComponent<CardUI>().thePlantType))
                        cardsOnSeedBankExtra.Add(seed.transform.GetChild(0).GetComponent<CardUI>().thePlantType, new List<bool>() { seed.transform.GetChild(0).GetComponent<CardUI>().isExtra });
                    else
                        cardsOnSeedBankExtra[seed.transform.GetChild(0).GetComponent<CardUI>().thePlantType].Add(seed.transform.GetChild(0).GetComponent<CardUI>().isExtra);
                }
            }
            if (MyColorfulCard == null)
                return;
            foreach (var card in CustomCore.CustomCards)
            {
                foreach (Func<Transform?> cardFunc in card.Value)
                {
                    Transform? result = cardFunc();
                    if (!(parents_colorful.ContainsKey(card.Key) && parents_colorful[card.Key].Contains(result)))
                    {
                        GameObject TempCard = Instantiate(MyColorfulCard, result);
                        if (TempCard != null)
                        {
                            //���ø��ڵ�
                            //����
                            TempCard.SetActive(true);
                            //����λ��
                            TempCard.transform.position = MyColorfulCard.transform.position;
                            TempCard.transform.localPosition = MyColorfulCard.transform.localPosition;
                            TempCard.transform.localScale = MyColorfulCard.transform.localScale;
                            TempCard.transform.localRotation = MyColorfulCard.transform.localRotation;
                            //����ͼƬ
                            // ���ñ���ֲ��ͼ��
                            Image image = TempCard.transform.GetChild(0).GetChild(0).GetComponent<Image>();
                            image.sprite = GameAPP.resourcesManager.plantPreviews[card.Key].GetComponent<SpriteRenderer>().sprite;
                            image.SetNativeSize();
                            // ���ñ����۸�
                            TempCard.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text = PlantDataLoader.plantDatas[card.Key].field_Public_Int32_1.ToString();
                            //��Ƭ
                            CardUI component = TempCard.transform.GetChild(1).GetComponent<CardUI>();
                            component.gameObject.SetActive(true);
                            //�޸�ͼƬ
                            Mouse.Instance.ChangeCardSprite(card.Key, component);
                            // �޸�����
                            TempCard.transform.GetChild(1).GetComponent<BoxCollider2D>().enabled = true;
                            RectTransform bgRect = TempCard.transform.GetChild(0).GetChild(0).GetComponent<RectTransform>();
                            RectTransform packetRect = TempCard.transform.GetChild(1).GetChild(0).GetComponent<RectTransform>();
                            bgRect.localScale = packetRect.localScale;
                            bgRect.sizeDelta = packetRect.sizeDelta;
                            //��������
                            component.thePlantType = card.Key;
                            component.theSeedType = (int)card.Key;
                            component.theSeedCost = PlantDataLoader.plantDatas[card.Key].field_Public_Int32_1;
                            component.fullCD = PlantDataLoader.plantDatas[card.Key].field_Public_Single_2;
                            if (cardsOnSeedBank.Contains(card.Key))
                                TempCard.transform.GetChild(1).gameObject.SetActive(false);
                            CheckCardState customComponent = TempCard.AddComponent<CheckCardState>();
                            customComponent.card = TempCard;
                            customComponent.cardType = component.thePlantType;
                            if (!parents_colorful.ContainsKey(card.Key))
                                parents_colorful.Add(card.Key, new List<Transform?>() { result });
                            else
                                parents_colorful[card.Key].Add(result);
                        }
                    }
                }
            }

            GameObject? MyNormalCard = Utils.GetNormalCardGameObject();
            Dictionary<PlantType, List<Transform?>> parents_normal = new Dictionary<PlantType, List<Transform?>>();
            if (MyNormalCard == null)
                return;
            foreach (var card in CustomCore.CustomNormalCards)
            {
                foreach (Func<Transform?> cardFunc in card.Value)
                {
                    Transform? result = cardFunc();
                    if (!(parents_normal.ContainsKey(card.Key) && parents_normal[card.Key].Contains(result)))
                    {
                        GameObject TempCard = Instantiate(MyNormalCard, result);
                        if (TempCard != null)
                        {
                            //���ø��ڵ�
                            //����
                            TempCard.SetActive(true);
                            //����λ��
                            TempCard.transform.position = MyNormalCard.transform.position;
                            TempCard.transform.localPosition = MyNormalCard.transform.localPosition;
                            TempCard.transform.localScale = MyNormalCard.transform.localScale;
                            TempCard.transform.localRotation = MyNormalCard.transform.localRotation;
                            //����ͼƬ
                            // ���ñ���ֲ��ͼ��
                            Image image = TempCard.transform.GetChild(0).GetChild(0).GetComponent<Image>();
                            image.sprite = GameAPP.resourcesManager.plantPreviews[card.Key].GetComponent<SpriteRenderer>().sprite;
                            image.SetNativeSize();
                            // ���ñ����۸�
                            TempCard.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text = PlantDataLoader.plantDatas[card.Key].field_Public_Int32_1.ToString();
                            //��Ƭ
                            CardUI component = TempCard.transform.GetChild(2).GetComponent<CardUI>(); // ����
                            component.gameObject.SetActive(true);
                            CardUI component1 = TempCard.transform.GetChild(1).GetComponent<CardUI>(); // ����
                            component1.gameObject.SetActive(true);
                            //�޸�ͼƬ
                            Mouse.Instance.ChangeCardSprite(card.Key, component);
                            Mouse.Instance.ChangeCardSprite(card.Key, component1);
                            // �޸�����
                            TempCard.transform.GetChild(2).GetComponent<BoxCollider2D>().enabled = true;
                            TempCard.transform.GetChild(1).GetComponent<BoxCollider2D>().enabled = true;
                            RectTransform bgRect = TempCard.transform.GetChild(0).GetChild(0).GetComponent<RectTransform>();
                            RectTransform packetRect = TempCard.transform.GetChild(2).GetChild(0).GetComponent<RectTransform>();
                            bgRect.localScale = packetRect.localScale;
                            bgRect.sizeDelta = packetRect.sizeDelta;
                            //��������
                            component.thePlantType = card.Key;
                            component.theSeedType = (int)card.Key;
                            component.theSeedCost = PlantDataLoader.plantDatas[card.Key].field_Public_Int32_1;
                            component.fullCD = PlantDataLoader.plantDatas[card.Key].field_Public_Single_2;
                            //���ø�������
                            component1.thePlantType = card.Key;
                            component1.theSeedType = (int)card.Key;
                            component1.theSeedCost = PlantDataLoader.plantDatas[card.Key].field_Public_Int32_1 * 2;
                            component1.fullCD = PlantDataLoader.plantDatas[card.Key].field_Public_Single_2;
                            if (cardsOnSeedBankExtra.ContainsKey(card.Key) && cardsOnSeedBankExtra[card.Key].Contains(true))
                                TempCard.transform.GetChild(1).gameObject.SetActive(false);
                            if (cardsOnSeedBankExtra.ContainsKey(card.Key) && cardsOnSeedBankExtra[card.Key].Contains(false))
                                TempCard.transform.GetChild(2).gameObject.SetActive(false);
                            CheckCardState customComponent = TempCard.AddComponent<CheckCardState>();
                            customComponent.card = TempCard;
                            customComponent.cardType = component.thePlantType;
                            customComponent.isNormalCard = true;
                            if (!parents_normal.ContainsKey(card.Key))
                                parents_normal.Add(card.Key, new List<Transform?>() { result });
                            else
                                parents_normal[card.Key].Add(result);
                        }
                    }
                }
            }
        }
    }
    /// <summary>
    /// ����һ����Ϸ����ʾ����ֲ��Button
    /// </summary>
    [HarmonyPatch(typeof(Board), nameof(Board.Start))]
    public static class Board_Patch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            SelectCustomPlants.InitCustomCards();
        }
    }

#if DEBUG_FEATURE__ENABLE_MULTI_LEVEL_BUFF
    #region �༶����ͬ��
    [HarmonyPatch(typeof(RandomZombie))]
    public static class RandomZombie_Patch
    {
        [HarmonyPatch(nameof(RandomZombie.FirstArmorFall))]
        [HarmonyPostfix]
        public static void Postfix()
        {
            // ��ͨ������Ӧ����
            for (int i = (int)CustomCore.variables[0]; i < TravelMgr.advancedBuffs.Count; i++)
            {
                var result = Utils.IsMultiLevelBuff(BuffType.AdvancedBuff, i);
                var index = CustomCore.CustomBuffsLevel.Where(kvp => kvp.Key.Item1 == BuffType.AdvancedBuff && kvp.Key.Item3 == i).Select(kvp => kvp.Key.Item2).ToList();
                foreach (var ii in index)
                    if (result.Item1 && TravelMgr.Instance.ultimateUpgrades[ii] == 0 && TravelMgr.Instance.advancedUpgrades[i])
                        foreach (var value in result.Item2)
                            TravelMgr.Instance.ultimateUpgrades[(int)CustomCore.variables[0] + value.Item2] = 1;
            }
            // Debuff������Ӧ����
            for (int i = (int)CustomCore.variables[0]; i < TravelMgr.debuffs.Count; i++)
            {
                var result = Utils.IsMultiLevelBuff(BuffType.Debuff, i);
                var index = CustomCore.CustomBuffsLevel.Where(kvp => kvp.Key.Item1 == BuffType.Debuff && kvp.Key.Item3 == i).Select(kvp => kvp.Key.Item2).ToList();
                foreach (var ii in index)
                    if (result.Item1 && TravelMgr.Instance.ultimateUpgrades[i] == 0 && TravelMgr.Instance.debuff[i])
                        foreach (var value in result.Item2)
                            TravelMgr.Instance.ultimateUpgrades[(int)CustomCore.variables[0] + value.Item2] = 1;
            }
        }
    }
    #endregion
#endif

    /// <summary>
    /// �������
    /// </summary>
    [HarmonyPatch(typeof(SkinButton), nameof(SkinButton.OnMouseUpAsButton))]
    public static class SkinButton_OnMouseUpAsButton
    {
        [HarmonyPrefix]
        public static bool Prefix(SkinButton __instance)
        {
            PlantType plantType = (PlantType)__instance.showPlant.theSeedType;
            if (CustomCore.CustomPlantsSkin.ContainsKey(plantType))
            {
                CustomPlantData customPlantData = CustomCore.CustomPlantsSkin[plantType];
                //����Ԥ��������
                (GameAPP.resourcesManager.plantPrefabs[plantType], customPlantData.Prefab) =
                    (customPlantData.Prefab, GameAPP.resourcesManager.plantPrefabs[plantType]);

                //����Ԥ��ͼ
                (GameAPP.resourcesManager.plantPreviews[plantType], customPlantData.Preview) =
                    (customPlantData.Preview, GameAPP.resourcesManager.plantPreviews[plantType]);

                //����ֲ������
                if (customPlantData.PlantData is not null)
                {
                    (PlantDataLoader.plantData[(int)plantType], customPlantData.PlantData) =
                        (customPlantData.PlantData, PlantDataLoader.plantData[(int)plantType]);
                    PlantDataLoader.plantDatas[plantType] = PlantDataLoader.plantData[(int)plantType];
                }
                CustomCore.CustomPlantsSkin[plantType] = customPlantData;

                //���������б�
                Extensions.SwapTypeMgrExtraSkinAndBackup(plantType);

                //GameObject prefab = GameAPP.resourcesManager.plantPrefabs[(PlantType)__instance.showPlant.theSeedType];

                //Transform transform = AlmanacMenu.Instance.currentShowCtrl.localShowPlant.transform.parent;

                //�ɵģ����������ݾ�����
                GameObject oldGameObject = AlmanacMenu.Instance.currentShowCtrl.localShowPlant;
                oldGameObject.name = "ToDestroy";
                // //ʵ�����µ�
                // AlmanacMenu.Instance.currentShowCtrl.localShowPlant = UnityEngine.Object.Instantiate(prefab, transform);
                // //ͬ��λ��
                // AlmanacMenu.Instance.currentShowCtrl.localShowPlant.transform.position =
                //     oldGameObject.transform.position;
                // AlmanacMenu.Instance.currentShowCtrl.localShowPlant.transform.localPosition =
                //     oldGameObject.transform.localPosition;

                //���پɵ�
                UnityEngine.Object.Destroy(oldGameObject);

                //����Ƿ񻻷�
                CustomCore.CustomPlantsSkinActive[plantType] = !CustomCore.CustomPlantsSkinActive[plantType];
                //__instance.showPlant.gameObject.SetActive(false);
                __instance.showPlant.InitNameAndInfoFromJson();
                AlmanacMenu.Instance.currentShowCtrl.localShowPlant =
                    AlmanacMenu.Instance.currentShowCtrl.SetPlant((int)plantType);

                if (AlmanacMenu.Instance.currentShowCtrl.localShowPlant.GetComponent<CustomPlantMonoBehaviour>() !=
                    null)
                {
                    UnityEngine.Object.Destroy(AlmanacMenu.Instance.currentShowCtrl.localShowPlant
                        .GetComponent<CustomPlantMonoBehaviour>());
                }

                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// ���������ı�Ⱦɫ
    /// </summary>
    [HarmonyPatch(typeof(TravelBuffOptionButton))]
    public static class TravelBuffOptionButtonPatch
    {
        [HarmonyPatch(nameof(TravelBuffOptionButton.SetBuff))]
        public static void PostSetBuff(TravelBuffOptionButton __instance, ref BuffType buffType, ref int buffIndex)
        {
            if (buffType is BuffType.AdvancedBuff && CustomCore.CustomAdvancedBuffs.ContainsKey(buffIndex)
                && CustomCore.CustomAdvancedBuffs[buffIndex].Item5 is not null)
            {
                __instance.introduce.text = $"<color={CustomCore.CustomAdvancedBuffs[buffIndex].Item5}>{__instance.introduce.text}</color>";
            }
        }
        
        /// <summary>
         /// ǿ��������ʾֲ���޸�
         /// </summary>
        [HarmonyPatch(nameof(TravelBuffOptionButton.SetPlant), new Type[] { })]
        [HarmonyPrefix]
        public static bool PreSetPlant(TravelBuffOptionButton __instance)
        {
            var list = CustomCore.CustomUltimateBuffs.
                Where(kvp => kvp.Key == __instance.buffIndex).
                ToList();
            if (__instance.buffType == BuffType.UltimateBuff && list.Count > 0)
            {
                foreach (var value in list)
                {
                    if (value.Value.Item1 == PlantType.Nothing)
                        __instance.SetPlant(PlantType.EndoFlame);
                    else
                        __instance.SetPlant(value.Value.Item1);
                }
                return false;
            }
            return true;
        }
    }
#if DEBUG_FEATURE__ENABLE_MULTI_LEVEL_BUFF
    #region �༶�����޸�����
    [HarmonyPatch(typeof(TravelLookMenu))]
    public static class TravelLookMenuPatch
    {
        /// <summary>
        /// �޸����飬��Ȼ���һ��
        /// </summary>
        [HarmonyPatch(nameof(TravelLookMenu.GetUltiBuffs))]
        [HarmonyPostfix]
        public static void PostGetUltiBuffs(TravelLookMenu __instance, ref Il2CppSystem.Collections.Generic.List<Vector2Int> __result)
        {
            Il2CppSystem.Collections.Generic.List<Vector2Int> result = new Il2CppSystem.Collections.Generic.List<Vector2Int>();

            // ������������
            for (int i = 0; i < __instance.manager.ultimateUpgrades.Length; i++)
            {
                if (CustomCore.CustomBuffsLevel.Count > 0 && CustomCore.CustomBuffsLevel.Any(kvp => ((int)CustomCore.variables[0] + kvp.Key.Item2) == i && kvp.Key.Item1 != BuffType.UltimateBuff))
                    continue;
                if (CustomCore.CustomBuffsLevel.Count > 0 && CustomCore.CustomBuffsLevel.Any(kvp => kvp.Key.Item1 == BuffType.UltimateBuff) && i == __instance.manager.ultimateUpgrades.Length - 1)
                    break;
                // ����Ƿ��ѽ�������ʾ����
                if (__instance.manager.ultimateUpgrades[i] != 0 || __instance.showAll)
                {
                    // ���������͵ȼ�������б�
                    result.Add(new Vector2Int(i, __instance.manager.ultimateUpgrades[i]));
                }
            }
            __result = result;
        }
    }
    #endregion
#endif

    [HarmonyPatch(typeof(TravelBuff))]
    public static class TravelBuffPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("ChangeSprite")]
        public static void PreChangeSprite(TravelBuff __instance)
        {
            var list = CustomCore.CustomUltimateBuffs.
                    Where(kvp => kvp.Key == __instance.theBuffNumber).
                    Select(kvp => kvp.Value).
                    ToList();
            if (__instance.theBuffType == (int)BuffType.UltimateBuff && list.Count > 0)
            {
                foreach (var item in list)
                {
                    if (item.Item1 == PlantType.Nothing)
                        __instance.thePlantType = PlantType.EndoFlame;
                    else
                        __instance.thePlantType = item.Item1;
                }
            }

            if (__instance.theBuffType == 1 && CustomCore.CustomAdvancedBuffs.ContainsKey(__instance.theBuffNumber))
            {
                __instance.thePlantType = CustomCore.CustomAdvancedBuffs[__instance.theBuffNumber].Item1;
            }
        }
    }

    /// <summary>
    /// ���������ı�Ⱦɫ
    /// </summary>
    [HarmonyPatch(typeof(TravelLookBuff))]
    public static class TravelLookBuffPatch
    {
        [HarmonyPatch(nameof(TravelLookBuff.SetBuff))]
        [HarmonyPostfix]
        public static void PostSetBuff(TravelLookBuff __instance, ref BuffType buffType, ref int buffIndex)
        {
            if (buffType is BuffType.AdvancedBuff && CustomCore.CustomAdvancedBuffs.ContainsKey(buffIndex)
                && CustomCore.CustomAdvancedBuffs[buffIndex].Item5 is not null)
            {
                __instance.introduce.text = $"<color={CustomCore.CustomAdvancedBuffs[buffIndex].Item5}>{__instance.introduce.text}</color>";
            }

#if DEBUG_FEATURE__ENABLE_MULTI_LEVEL_BUFF
            #region �༶������ʾ�޸�
            // �༶������ʾ�޸�
            if (__instance == null)
                return;
            var result = Utils.IsMultiLevelBuff(__instance.buffType, __instance.buffIndex);
            if (result.Item1)
            {
                foreach (var value in result.Item2)
                {
                    int index = (int)CustomCore.variables[0] + value.Item2;
                    Il2CppStructArray<int> upgrades = __instance.manager.ultimateUpgrades;
                    if (TravelLookMenu.Instance.showAll)
                    {
                        __instance.SetText(upgrades[index] != 0, upgrades[index]);
                        if (upgrades[index] <= CustomCore.CustomBuffsLevel[value] &&
                            upgrades[index] != 0)
                        {
                            if (CustomCore.CustomBuffsLevel[value] > 1)
                                __instance.SetText($"�ѿ�����{upgrades[index]}����");
                            else
                                __instance.SetText($"�ѿ���");
                        }
                        else
                        {
                            __instance.SetText("�ѹر�");
                        }
                    }
                    else
                    {
                        if (upgrades[index] < CustomCore.CustomBuffsLevel[value] && CustomCore.CustomBuffsLevel[value] != 1)
                        {
                            if (upgrades[index] >= CustomCore.CustomBuffsLevel[value])
                            {
                                __instance.SetText("������");
                            }
                            else
                                __instance.SetText($"{upgrades[index]}��");
                        }
                        if (upgrades[index] >= CustomCore.CustomBuffsLevel[value])
                        {
                            __instance.SetText("������");
                        }
                    }
                }
            }
            #endregion
#endif
        }

#if DEBUG_FEATURE__ENABLE_MULTI_LEVEL_BUFF
        #region �༶��������
        /// <summary>
        /// �߼�������������
        /// </summary>
        [HarmonyPatch(nameof(TravelLookBuff.OnMouseUpAsButton))]
        [HarmonyPrefix]
        public static bool PreOnMouseUpAsButton(TravelLookBuff __instance)
        {
            var result = Utils.IsMultiLevelBuff(__instance.buffType, __instance.buffIndex);
            bool reset = false;
            if (result.Item1)
            {
                foreach (var value in result.Item2)
                {
                    int index = (int)CustomCore.variables[0] + value.Item2;
                    if (TravelLookMenu.Instance.showAll)
                    {
                        Il2CppStructArray<int> upgrades = __instance.manager.ultimateUpgrades;
                        upgrades[index] = upgrades[index] + 1;
                        if (upgrades[index] > CustomCore.CustomBuffsLevel[value])
                            upgrades[index] = 0;
                        __instance.SetText(upgrades[index] != 0, upgrades[index]);
                        if (upgrades[index] <= CustomCore.CustomBuffsLevel[value] &&
                            upgrades[index] != 0)
                        {
                            if (CustomCore.CustomBuffsLevel[value] > 1)
                                __instance.SetText($"�ѿ�����{upgrades[index]}����");
                            else
                                __instance.SetText($"�ѿ���");
                        }
                        return false;
                    }
                    else
                    {
                        Il2CppStructArray<int> upgrades = __instance.manager.ultimateUpgrades;
                        if (upgrades[index] < CustomCore.CustomBuffsLevel[value] && Lawnf.TravelAdvanced(54) && CustomCore.CustomBuffsLevel[value] != 1)
                        {
                            upgrades[index] = upgrades[index] + 1;
                            reset = true;
                            if (upgrades[index] >= CustomCore.CustomBuffsLevel[value])
                                __instance.SetText("������");
                            else
                                __instance.SetText($"{upgrades[index]}��");
                        }
                        if (upgrades[index] >= CustomCore.CustomBuffsLevel[value])
                        {
                            __instance.SetText("������");
                        }
                    }
                }
            }
            if (reset)
            {
                __instance.manager.advancedUpgrades[54] = false;
                return false;
            }
            return true;
        }
        #endregion
#endif
    }

    [HarmonyPatch(typeof(TravelMgr))]
    public static class TravelMgrPatch
    {
        [HarmonyPatch("Awake")]
        [HarmonyPrefix]
        public static void PreAwake(TravelMgr __instance)
        {
            if (CustomCore.CustomAdvancedBuffs.Count > 0)
            {
                bool[] newAdv = new bool[__instance.advancedUpgrades.Count + CustomCore.CustomAdvancedBuffs.Count];
                Array.Copy(__instance.advancedUpgrades, newAdv, __instance.advancedUpgrades.Length);
                __instance.advancedUpgrades = newAdv;
                // Note: advancedUnlockRound was removed in 3.1 version
            }
            if (CustomCore.CustomUltimateBuffs.Count > 0)//ǿ������
            {
                int[] newUlti = new int[__instance.ultimateUpgrades.Count + CustomCore.CustomUltimateBuffs.Count];
                // �༶������ʼ������������ʱ����ȡ��ע�� int[] newUlti = new int[__instance.ultimateUpgrades.Count + CustomCore.CustomBuffsLevel.Count(kvp => kvp.Key.Item1 == BuffType.UltimateBuff && kvp.Value != 1)];
                Array.Copy(__instance.ultimateUpgrades, newUlti, __instance.ultimateUpgrades.Length);
                __instance.ultimateUpgrades = newUlti;
            }
            if (CustomCore.CustomDebuffs.Count > 0)
            {
                bool[] newdeb = new bool[__instance.debuff.Count + CustomCore.CustomDebuffs.Count];
                Array.Copy(__instance.debuff, newdeb, __instance.debuff.Length);
                __instance.debuff = newdeb;
            }

#if DEBUG_FEATURE__ENABLE_MULTI_LEVEL_BUFF
            #region �༶��������
            if (CustomCore.CustomBuffsLevel.Count > 0)//�߼�����
            {
                CustomCore.variables[0] = __instance.ultimateUpgrades.Length;
                int length = CustomCore.CustomBuffsLevel.Count(kvp => kvp.Value != 1);
                int[] newLevel = new int[__instance.ultimateUpgrades.Length + length];
                Array.Copy(__instance.ultimateUpgrades, newLevel, __instance.ultimateUpgrades.Length);
                __instance.ultimateUpgrades = newLevel;
            }
            #endregion
#endif

            foreach (PlantType plantType in CustomCore.CustomUltimatePlants) // ע��ǿ��ֲ��
            {
                TravelMgr.allStrongUltimtePlant.Add(plantType);
            }
        }

        [HarmonyPatch("GetAdvancedBuffPool")]
        [HarmonyPostfix]
        public static void PostGetAdvancedBuffPool(ref Il2CppSystem.Collections.Generic.List<int> __result)
        {
            for (int i = __result.Count - 1; i >= 0; i--)
            {
                if (CustomCore.CustomAdvancedBuffs.ContainsKey(__result[i]) && !CustomCore.CustomAdvancedBuffs[__result[i]].Item3())
                {
                    __result.Remove(__result[i]);
                }
            }
        }

        [HarmonyPatch(nameof(TravelMgr.GetAdvancedText))]
        [HarmonyPostfix]
        public static void PostGetAdvancedText(ref int index, ref string __result)
        {
            if (CustomCore.CustomAdvancedBuffs.ContainsKey(index) && CustomCore.CustomAdvancedBuffs[index].Item5 is not null)
            {
                __result = $"<color={CustomCore.CustomAdvancedBuffs[index].Item5}>{__result}</color>";
            }
        }

        [HarmonyPatch(nameof(TravelMgr.GetPlantTypeByAdvBuff))]
        [HarmonyPostfix]
        public static void PostGetPlantTypeByAdvBuff(ref int index, ref PlantType __result)
        {
            if (CustomCore.CustomAdvancedBuffs.ContainsKey(index) && CustomCore.CustomAdvancedBuffs[index].Item1 is not PlantType.Nothing)
            {
                __result = CustomCore.CustomAdvancedBuffs[index].Item1;
            }
        }

#if DEBUG_FEATURE__ENABLE_MULTI_LEVEL_BUFF
        #region �༶����ͬ��
        [HarmonyPatch(nameof(TravelMgr.Start))]
        [HarmonyPostfix]
        public static void PostStart(TravelMgr __instance)
        {
            // ��ͨ������Ӧ����
            for (int i = (int)CustomCore.variables[0]; i < __instance.advancedUpgrades.Count; i++)
            {
                var result = Utils.IsMultiLevelBuff(BuffType.AdvancedBuff, i);
                foreach (var ii in result.Item2)
                    if (result.Item1 && __instance.ultimateUpgrades[(int)CustomCore.variables[0] + ii.Item2] == 0 && __instance.advancedUpgrades[i])
                        foreach (var value in result.Item2)
                            __instance.ultimateUpgrades[(int)CustomCore.variables[0] + value.Item2] = 1;
            }
            // Debuff������Ӧ����
            for (int i = (int)CustomCore.variables[0]; i < __instance.debuff.Count; i++)
            {
                var result = Utils.IsMultiLevelBuff(BuffType.Debuff, i);
                foreach (var ii in result.Item2)
                    if (result.Item1 && __instance.ultimateUpgrades[(int)CustomCore.variables[0] + ii.Item2] == 0 && __instance.debuff[i])
                        foreach (var value in result.Item2)
                            __instance.ultimateUpgrades[(int)CustomCore.variables[0] + value.Item2] = 1;
            }
        }
        #endregion
#endif
    }

    [HarmonyPatch(typeof(TravelStore))]
    public static class TravelStorePatch
    {
        [HarmonyPatch("RefreshBuff")]
        [HarmonyPostfix]
        public static void PostRefreshBuff(TravelStore __instance)
        {
            foreach (var travelBuff in __instance.gameObject.GetComponentsInChildren<TravelBuff>())
            {
                if (travelBuff.theBuffType is (int)BuffType.AdvancedBuff &&
                    CustomCore.CustomAdvancedBuffs.ContainsKey(travelBuff.theBuffNumber))
                {
                    travelBuff.cost = CustomCore.CustomAdvancedBuffs[travelBuff.theBuffNumber].Item4;
                    travelBuff.transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>().text =
                        $"��{CustomCore.CustomAdvancedBuffs[travelBuff.theBuffNumber].Item4}";
                }

                if (travelBuff.theBuffType is (int)BuffType.UltimateBuff &&
                    CustomCore.CustomUltimateBuffs.ContainsKey(travelBuff.theBuffNumber))
                {
                    travelBuff.cost = CustomCore.CustomUltimateBuffs[travelBuff.theBuffNumber].Item3;
                    travelBuff.transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>().text =
                        $"��{CustomCore.CustomUltimateBuffs[travelBuff.theBuffNumber].Item3.ToString()}";
                }
            }
        }
    }

    [HarmonyPatch(typeof(TypeMgr))]
    public static class TypeMgrPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("BigNut")]
        public static bool PreBigNut(ref PlantType theSeedType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.BigNut.Contains(theSeedType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.BigNut.TryGetValue(theSeedType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("BigZombie")]
        public static bool PreBigZombie(ref ZombieType theZombieType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.BigZombie.Contains(theZombieType))
            {
                __result = true;
                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("DoubleBoxPlants")]
        public static bool PreDoubleBoxPlants(ref PlantType thePlantType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.DoubleBoxPlants.Contains(thePlantType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.DoubleBoxPlants.TryGetValue(thePlantType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        /*[HarmonyPrefix]
        [HarmonyPatch("EliteZombie")]
        public static bool PreEliteZombie(ref ZombieType theZombieType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.EliteZombie.Contains(theZombieType))
            {
                __result = true;
                return false;
            }

            return true;
        }*/

        [HarmonyPrefix]
        [HarmonyPatch("FlyingPlants")]
        public static bool PreFlyingPlants(ref PlantType thePlantType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.FlyingPlants.Contains(thePlantType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.FlyingPlants.TryGetValue(thePlantType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("GetPlantTag")]
        public static bool PreGetPlantTag(ref Plant plant)
        {
            if (CustomCore.CustomPlantTypes.Contains(plant.thePlantType))
            {
                plant.plantTag = new()
                {
                    icePlant = TypeMgr.IsIcePlant(plant.thePlantType),
                    caltropPlant = TypeMgr.IsCaltrop(plant.thePlantType),
                    doubleBoxPlant = TypeMgr.DoubleBoxPlants(plant.thePlantType),
                    firePlant = TypeMgr.IsFirePlant(plant.thePlantType),
                    flyingPlant = TypeMgr.FlyingPlants(plant.thePlantType),
                    lanternPlant = TypeMgr.IsPlantern(plant.thePlantType),
                    smallLanternPlant = TypeMgr.IsSmallRangeLantern(plant.thePlantType),
                    magnetPlant = TypeMgr.IsMagnetPlants(plant.thePlantType),
                    nutPlant = TypeMgr.IsNut(plant.thePlantType),
                    tallNutPlant = TypeMgr.IsTallNut(plant.thePlantType),
                    potatoPlant = TypeMgr.IsPotatoMine(plant.thePlantType),
                    potPlant = TypeMgr.IsPot(plant.thePlantType),
                    puffPlant = TypeMgr.IsPuff(plant.thePlantType),
                    pumpkinPlant = TypeMgr.IsPumpkin(plant.thePlantType),
                    spickRockPlant = TypeMgr.IsSpickRock(plant.thePlantType),
                    tanglekelpPlant = TypeMgr.IsTangkelp(plant.thePlantType),
                    waterPlant = TypeMgr.IsWaterPlant(plant.thePlantType),
                };

                return false;
            }

            if (CustomCore.CustomPlantsSkin.ContainsKey(plant.thePlantType))
            {
                plant.plantTag = new()
                {
                    icePlant = TypeMgr.IsIcePlant(plant.thePlantType),
                    caltropPlant = TypeMgr.IsCaltrop(plant.thePlantType),
                    doubleBoxPlant = TypeMgr.DoubleBoxPlants(plant.thePlantType),
                    firePlant = TypeMgr.IsFirePlant(plant.thePlantType),
                    flyingPlant = TypeMgr.FlyingPlants(plant.thePlantType),
                    lanternPlant = TypeMgr.IsPlantern(plant.thePlantType),
                    smallLanternPlant = TypeMgr.IsSmallRangeLantern(plant.thePlantType),
                    magnetPlant = TypeMgr.IsMagnetPlants(plant.thePlantType),
                    nutPlant = TypeMgr.IsNut(plant.thePlantType),
                    tallNutPlant = TypeMgr.IsTallNut(plant.thePlantType),
                    potatoPlant = TypeMgr.IsPotatoMine(plant.thePlantType),
                    potPlant = TypeMgr.IsPot(plant.thePlantType),
                    puffPlant = TypeMgr.IsPuff(plant.thePlantType),
                    pumpkinPlant = TypeMgr.IsPumpkin(plant.thePlantType),
                    spickRockPlant = TypeMgr.IsSpickRock(plant.thePlantType),
                    tanglekelpPlant = TypeMgr.IsTangkelp(plant.thePlantType),
                    waterPlant = TypeMgr.IsWaterPlant(plant.thePlantType)
                };

                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IsCaltrop")]
        public static bool PreIsCaltrop(ref PlantType theSeedType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.IsCaltrop.Contains(theSeedType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.IsCaltrop.TryGetValue(theSeedType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IsFirePlant")]
        public static bool PreIsFirePlant(ref PlantType theSeedType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.IsFirePlant.Contains(theSeedType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.IsFirePlant.TryGetValue(theSeedType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IsIcePlant")]
        public static bool PreIsIcePlant(ref PlantType theSeedType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.IsIcePlant.Contains(theSeedType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.IsIcePlant.TryGetValue(theSeedType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IsMagnetPlants")]
        public static bool PreIsMagnetPlants(ref PlantType thePlantType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.IsMagnetPlants.Contains(thePlantType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.IsMagnetPlants.TryGetValue(thePlantType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IsNut")]
        public static bool PreIsNut(ref PlantType theSeedType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.IsNut.Contains(theSeedType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.IsNut.TryGetValue(theSeedType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IsPlantern")]
        public static bool PreIsPlantern(ref PlantType theSeedType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.IsPlantern.Contains(theSeedType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.IsPlantern.TryGetValue(theSeedType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IsPot")]
        public static bool PreIsPot(ref PlantType thePlantType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.IsPot.Contains(thePlantType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.IsPot.TryGetValue(thePlantType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IsPotatoMine")]
        public static bool PreIsPotatoMine(ref PlantType theSeedType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.IsPotatoMine.Contains(theSeedType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.IsPotatoMine.TryGetValue(theSeedType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IsPuff")]
        public static bool PreIsPuff(ref PlantType theSeedType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.IsPuff.Contains(theSeedType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.IsPuff.TryGetValue(theSeedType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IsPumpkin")]
        public static bool PreIsPumpkin(ref PlantType theSeedType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.IsPumpkin.Contains(theSeedType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.IsPumpkin.TryGetValue(theSeedType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IsSmallRangeLantern")]
        public static bool PreIsSmallRangeLantern(ref PlantType theSeedType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.IsSmallRangeLantern.Contains(theSeedType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.IsSmallRangeLantern.TryGetValue(theSeedType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IsSpecialPlant")]
        public static bool PreIsSpecialPlant(ref PlantType theSeedType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.IsSpecialPlant.Contains(theSeedType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.IsSpecialPlant.TryGetValue(theSeedType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IsSpickRock")]
        public static bool PreIsSpickRock(ref PlantType theSeedType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.IsSpickRock.Contains(theSeedType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.IsSpickRock.TryGetValue(theSeedType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IsTallNut")]
        public static bool PreIsTallNut(ref PlantType theSeedType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.IsTallNut.Contains(theSeedType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.IsTallNut.TryGetValue(theSeedType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IsTangkelp")]
        public static bool PreIsTangkelp(ref PlantType theSeedType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.IsTangkelp.Contains(theSeedType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.IsTangkelp.TryGetValue(theSeedType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IsWaterPlant")]
        public static bool PreIsWaterPlant(ref PlantType theSeedType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.IsWaterPlant.Contains(theSeedType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.IsWaterPlant.TryGetValue(theSeedType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("UmbrellaPlants")]
        public static bool PreUmbrellaPlants(ref PlantType thePlantType, ref bool __result)
        {
            if (CustomCore.TypeMgrExtra.UmbrellaPlants.Contains(thePlantType))
            {
                __result = true;
                return false;
            }

            if (CustomCore.TypeMgrExtraSkin.UmbrellaPlants.TryGetValue(thePlantType, out int value))
            {
                switch (value)
                {
                    case -1:
                        return true;

                    case 0:
                        __result = false;
                        return false;

                    case 1:
                        __result = true;
                        return false;
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(UIMgr))]
    public static class UIMgrPatch
    {
        [HarmonyPatch(nameof(UIMgr.EnterChallengeMenu))]
        [HarmonyPostfix]
        public static void PostEnterChallengeMenu()
        {
            var levels = GameAPP.canvas.GetChild(0).FindChild("Levels");
            var firstBtns = levels.FindChild("FirstBtns");
            if (firstBtns.FindChild("CustomLevels") is null || firstBtns.FindChild("CustomLevels").IsDestroyed())
            {
                GameObject custom = UnityEngine.Object.Instantiate(firstBtns.GetChild(0).gameObject, firstBtns);
                custom.name = "CustomLevels";
                custom.transform.localPosition = new(-150, 30, 0);
                var window = custom.transform.FindChild("Window");
                window.FindChild("Name").GetComponent<TextMeshProUGUI>().text = "�����ؿ�";
                var adv = levels.FindChild("PageAdvantureLevel");
                var customLevels = UnityEngine.Object.Instantiate(adv.gameObject, levels);
                customLevels.active = false;
                customLevels.name = "PageCustomLevel";
                var pages = customLevels.transform.FindChild("Pages");
                var levelSample = UnityEngine.Object.Instantiate(pages.FindChild("Page1").FindChild("Lv1").gameObject);
                foreach (var l in pages.FindChild("Page1").GetComponentsInChildren<Transform>(true))
                {
                    UnityEngine.Object.Destroy(l.gameObject);
                }
                var pageSample = UnityEngine.Object.Instantiate(pages.FindChild("Page1").gameObject);
                UnityEngine.Object.Destroy(pages.FindChild("Page1").gameObject);
                UnityEngine.Object.Destroy(pages.FindChild("Page2").gameObject);
                UnityEngine.Object.Destroy(pages.FindChild("Page3").gameObject);
                int levelIndex = 0;
                int columnIndex = 0;
                int rowIndex = 0;
                int pageIndex = 0;
                foreach (var level in CustomCore.CustomLevels)
                {
                    if (levelIndex % 18 is 0)
                    {
                        UnityEngine.Object.Instantiate(pageSample, pages).name = $"Pages{levelIndex / 18 + 1}";
                    }
                    columnIndex = levelIndex % 6;
                    rowIndex = levelIndex / 6;
                    pageIndex = rowIndex / 3;
                    var levelBtn = UnityEngine.Object.Instantiate(levelSample, pages.FindChild($"Pages{levelIndex / 18 + 1}"));
                    levelBtn.transform.localPosition = new(-50 + 150 * columnIndex, 60 - 130 * rowIndex, 0);
                    levelBtn.transform.GetChild(0).GetComponent<UnityEngine.UI.Image>().sprite = level.Logo;
                    levelBtn.transform.GetChild(1).GetComponent<Advanture_Btn>().levelType = (LevelType)66;
                    levelBtn.transform.GetChild(1).GetComponent<Advanture_Btn>().buttonNumber = level.ID;
                    levelBtn.transform.GetChild(1).GetChild(0).GetComponent<TextMeshProUGUI>().text = level.Name();
                    levelIndex++;
                }
                window.GetComponent<FirstBtns>().pageToOpen = customLevels;
                window.GetComponent<FirstBtns>().originPosition = new(-150, 30, 0);
                UnityEngine.Object.Destroy(pageSample);
                UnityEngine.Object.Destroy(levelSample);
            }
        }

        [HarmonyPatch(nameof(UIMgr.EnterGame))]
        [HarmonyPrefix]
        public static bool PreEnterGame(ref int levelType, ref int levelNumber, ref int id, ref string name)
        {
            if (levelType is not 66) return true;
            var levelData = CustomCore.CustomLevels[levelNumber];

            // ����UI��Դ
            GameAPP.UIManager.PopAll();

            // �������
            CamaraFollowMouse.Instance.ResetCamera();

            // ������Ϸ�ٶ�
            Time.timeScale = GameAPP.gameSpeed;

            // ���õ�ǰ�ؿ���Ϣ
            GameAPP.theBoardType = (LevelType)levelType;
            GameAPP.theBoardLevel = levelNumber;

            // �������е�Travel������
            if (TravelMgr.Instance != null)
            {
                UnityEngine.Object.Destroy(TravelMgr.Instance);
                TravelMgr.Instance = null;
            }

            // ������Ϸ��
            GameObject boardGO = new("Board");
            GameAPP.board = boardGO;
            Board board = boardGO.AddComponent<Board>();
            board.boardTag = levelData.BoardTag;
            board.rowNum = levelData.RowCount;
            board.theMaxWave = levelData.WaveCount();
            board.cardSelectable = levelData.NeedSelectCard;
            board.theSun = levelData.Sun();
            board.zombieDamageAdder = levelData.ZombieHealthRate();
            board.seedPool = levelData.SeedRainPlantTypes().ToIl2CppList();
            levelData.PostBoard(board);
            // ��ȡ�������ͺ͵�ͼ·��
            GameObject mapInstance = MapData_cs.GetMap(levelData.SceneType, board);
            if (mapInstance != null)
            {
                mapInstance = UnityEngine.Object.Instantiate(mapInstance, boardGO.transform);
                board.ChangeMap(mapInstance);
            }
            return false;

            // ���ز�ʵ������ͼ

            InitZombieList.InitZombie((LevelType)levelType, levelNumber);

            // �������ֲ���ʼ��Ϸ
            GameAPP.Instance.PlayMusic(MusicType.SelectCard);
            GameAPP.theGameStatus = GameStatus.InInterlude;

            // ��ʼ����Ϸ��
            levelData.PreInitBoard();

            levelData.PostInitBoard(board.gameObject.AddComponent<InitBoard>());
            foreach (var p in levelData.PrePlants())
            {
                CreatePlant.Instance.SetPlant(p.Item1, p.Item2, p.Item3);
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(ZombieDataManager))]
    public static class ZombieDataPatch
    {
        [HarmonyPatch(nameof(ZombieDataManager.LoadData))]
        [HarmonyPostfix]
        public static void InitZombieData()
        {
            foreach (var z in CustomCore.CustomZombies)
            {
                ZombieDataManager.zombieDataDic[z.Key] = z.Value.Item3;
            }
        }
    }
}