using System;
using Il2CppSystem.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace UltimateGoldJackBoxZombieMod
{
    public class UltimateGoldJackBox : MonoBehaviour
    {
        public Jackbox_c zombie => gameObject.GetComponent<Jackbox_c>();
        public TextMeshPro? CenterPercentText { get; private set; }

        public int lastRow;
        public int lastCol;
        public int lastCount;
        public float lastPercent;
        public float totalCoin;
        public float waitTime;
        public float updateTime;
        public Vector3 originLocalScale = Vector3.zero;
        public bool isAddCoining;
        public float addCoiningTime = 3f;
        public bool hasUsedNoJumperRevive; // 标记是否已使用未下跳杆时的复活机会

        public void Init(int count)
        {
            if (zombie == null) return;
            GoldBoxStateRecord record = new GoldBoxStateRecord();
            record.isLoseJumper = false;
            Plugin.goldManager.AddOrUpdatetStateRecord(zombie, record);
            lastCount = count;
            zombie.theAttackDamage = 1000;
            zombie.transform.localScale = originLocalScale * 1.15f;
        }

        public void InitCenterPercentText()
        {
            if (CenterPercentText != null) return;
            GameObject textObj = new GameObject("CenterPercentText");
            textObj.transform.SetParent(zombie.transform, false);
            Vector3 pos = zombie.axis.position;
            pos.y += 2.5f;
            textObj.transform.position = pos;
            CenterPercentText = textObj.AddComponent<TextMeshPro>();
            CenterPercentText.font = GameAPP.font;
            CenterPercentText.fontSize = 8f;
            CenterPercentText.alignment = (TextAlignmentOptions)514;
            CenterPercentText.enableWordWrapping = false;
            CenterPercentText.color = Color.white;
            CenterPercentText.outlineWidth = 0.2f;
            CenterPercentText.outlineColor = Color.black;
            SortingGroup sortingGroup = CenterPercentText.gameObject.AddComponent<SortingGroup>();
            sortingGroup.sortAtRoot = true;
            sortingGroup.sortingLayerID = SortingLayer.NameToID("UI");
            sortingGroup.sortingOrder = 100;
            CenterPercentText.GetComponent<RectTransform>().sizeDelta = new Vector2(5f, 1f);
            string text = lastPercent.ToString("F1") + "%";
            CenterPercentText.text = text ?? "";
            CenterPercentText.gameObject.SetActive(Board.Instance.showZombieHealth);
        }

        public void loseMoney(float percentFraction)
        {
            int money = Board.Instance.theMoney;
            if ((float)money < percentFraction) return;
            money -= (int)percentFraction;
            Board.Instance.theMoney = money;
        }

        public void UpdateCenterPercentText(float percentFraction, bool isLoseMoney = true)
        {
            if (CenterPercentText == null) return;
            lastCount.ToString();
            if (lastCount == 1)
            {
                CenterPercentText.text = "100%";
                return;
            }
            if (isLoseMoney)
            {
                loseMoney(percentFraction);
            }
            totalCoin += percentFraction;
            float percent = 0f;
            if (totalCoin <= 60000f)
            {
                percent = totalCoin / 60000f * 100f;
            }
            else
            {
                totalCoin = 0f;
                lastCount++;
            }
            lastPercent = Mathf.Clamp(percent, 0f, 100f);
            lastCount.ToString();
            string text = lastPercent.ToString("F1") + "%";
            CenterPercentText.text = text ?? "";
        }

        public bool ReduceJackBoxCount()
        {
            if (lastCount == 0) return false;
            lastCount--;
            string countStr = lastCount.ToString();
            string percentStr = lastPercent.ToString() + "%";
            CenterPercentText!.text = countStr + "\n" + percentStr;
            return true;
        }

        public void ShowCenterPercentText()
        {
            if (!Plugin.goldManager.GetJumperStateRecord(zombie)) return;
            InitCenterPercentText();
            if (lastCount >= 1)
            {
                CenterPercentText!.text = "100%";
                CenterPercentText.color = Color.red;
            }
            else
            {
                string text = lastPercent.ToString("F1") + "%";
                CenterPercentText!.text = text ?? "";
            }
            CenterPercentText.gameObject.SetActive(Board.Instance.showZombieHealth);
        }

        public void HideCenterPercentText()
        {
            if (CenterPercentText != null)
            {
                CenterPercentText.gameObject.SetActive(false);
            }
        }

        public void DestroyCenterPercentText()
        {
            if (CenterPercentText != null)
            {
                UnityEngine.Object.Destroy(CenterPercentText.gameObject);
                CenterPercentText = null;
            }
        }

        public void CreateCoinFlyToZombie(Zombie zombie, int coinType = 36)
        {
            if (zombie == null) return;
            Vector3 startPos = new Vector3(-7.5f, -5f, 0f);
            Vector3 targetPos = new Vector3(zombie.axis.position.x, zombie.axis.position.y + 1.5f, zombie.axis.position.z);
            GameObject[]? itemPrefab = GameAPP.itemPrefab;
            if (itemPrefab == null || coinType >= itemPrefab.Length) return;
            GameObject? prefab = itemPrefab[coinType];
            if (prefab == null) return;
            GameObject? coinObj = UnityEngine.Object.Instantiate(prefab, startPos, Quaternion.identity);
            if (coinObj == null) return;
            CoinFlyAnimationManager.AddFlyingCoin(coinObj, targetPos, 1.2f);
        }

        public bool IsInRange3x3(Zombie z)
        {
            int zRow = z.theZombieRow;
            int myRow = zombie.theZombieRow;
            if (zRow != myRow && zRow != myRow + 1 && zRow != myRow - 1) return false;
            int zCol = z.Column;
            int myCol = zombie.Column;
            return zCol == myCol || zCol == myCol + 1 || zCol == myCol - 1;
        }

        public List<Zombie> Debuff3x3Zombie()
        {
            Plugin.goldManager.Clear();
            List<Zombie> allZombies = Lawnf.GetAllZombies(false);
            foreach (Zombie z in allZombies)
            {
                if ((int)z.theStatus != 1 && IsInRange3x3(z))
                {
                    Plugin.goldManager.AddRecord(z);
                }
            }
            return allZombies;
        }

        public void Awake()
        {
        }

        public void Update()
        {
            if ((int)GameAPP.theGameStatus != 0) return;
            if (zombie == null) return;
            if (zombie.beforeDying) return;
            ShowCenterPercentText();
            CoinFlyAnimationManager.UpdateFlyingCoins(() => { });
            if (Plugin.goldManager.GetJumperStateRecord(zombie))
            {
                int mask = LayerMask.GetMask("Zombie");
                Physics2D.OverlapCircleAll(zombie.transform.position, 6f, mask);
                if (waitTime >= 5f)
                {
                    waitTime = 0f;
                    // 只有当lastCount < 1时才吸取金币（即没有复活机会时）
                    // 当lastCount >= 1时表示已有复活机会，不再吸取金币和回血
                    if (lastCount < 1)
                    {
                        int money = Board.Instance.theMoney;
                        int coinAmount = UnityEngine.Random.Range(3000, 8001);
                        if (money >= coinAmount)
                        {
                            // 扣除吸取的金币数量
                            Board.Instance.theMoney -= coinAmount;
                            // 创建金币飞向僵尸的动画
                            int i = coinAmount / 1000;
                            while (i > 0)
                            {
                                i--;
                                CreateCoinFlyToZombie(zombie, 36);
                                CoinFlyAnimationManager.UpdateFlyingCoins(() => { });
                            }
                            // 更新金币累计和百分比显示
                            UpdateCenterPercentText((float)coinAmount, false);
                            // 回血：金币数/10
                            zombie.Recover((float)(coinAmount / 10));
                        }
                    }
                }
                else
                {
                    waitTime += Time.deltaTime;
                }
                int row = zombie.theZombieRow;
                int col = zombie.Column;
                Debuff3x3Zombie();
            }
        }
    }
}
