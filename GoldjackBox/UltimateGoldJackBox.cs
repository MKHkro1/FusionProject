using System;
using Il2CppSystem.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace UltimateGoldJackBoxZombieMod
{
	public class UltimateGoldJackBox : MonoBehaviour
	{
		public Jackbox_c zombie
		{
			get
			{
				return base.gameObject.GetComponent<Jackbox_c>();
			}
		}

		public TextMeshPro CenterPercentText { get; private set; }

		public void Init(int count)
		{
			if (this.zombie == null)
			{
				return;
			}
			GoldBoxStateRecord goldBoxStateRecord = new GoldBoxStateRecord();
			goldBoxStateRecord.isLoseJumper = false;
			Plugin.goldManager.AddOrUpdatetStateRecord(this.zombie, goldBoxStateRecord);
			this.lastCount = count;
			this.zombie.theAttackDamage = 1000;
			this.zombie.transform.localScale = this.originLocalScale * 1.0f;
		}

		public void InitCenterPercentText()
		{
			if (this.CenterPercentText != null)
			{
				return;
			}
			GameObject gameObject = new GameObject("CenterPercentText");
			gameObject.transform.SetParent(this.zombie.transform, false);
			Vector3 position = this.zombie.axis.position;
			position.y += 2.5f;
			gameObject.transform.position = position;
			this.CenterPercentText = gameObject.AddComponent<TextMeshPro>();
			this.CenterPercentText.font = GameAPP.font;
			this.CenterPercentText.fontSize = 8f;
			this.CenterPercentText.alignment = (TextAlignmentOptions)514;
			this.CenterPercentText.enableWordWrapping = false;
			this.CenterPercentText.color = Color.white;
			this.CenterPercentText.outlineWidth = 0.2f;
			this.CenterPercentText.outlineColor = Color.black;
			SortingGroup sortingGroup = this.CenterPercentText.gameObject.AddComponent<SortingGroup>();
			sortingGroup.sortAtRoot = true;
			sortingGroup.sortingLayerID = SortingLayer.NameToID("UI");
			sortingGroup.sortingOrder = 100;
			this.CenterPercentText.GetComponent<RectTransform>().sizeDelta = new Vector2(5f, 1f);
			string text = this.lastPercent.ToString("F1") + "%";
			this.CenterPercentText.text = (text ?? "");
			this.CenterPercentText.gameObject.SetActive(Board.Instance.showZombieHealth);
		}

		public void loseMoney(float percentFraction)
		{
			int num = Board.Instance.theMoney;
			if ((float)num < percentFraction)
			{
				return;
			}
			num -= (int)percentFraction;
			Board.Instance.theMoney = num;
		}

		public void UpdateCenterPercentText(float percentFraction, bool isLoseMoney = true)
		{
			if (this.CenterPercentText == null)
			{
				return;
			}
			this.lastCount.ToString();
			if (this.lastCount == 1)
			{
				this.CenterPercentText.text = "100%";
				return;
			}
			if (isLoseMoney)
			{
				this.loseMoney(percentFraction);
			}
			this.totalCoin += percentFraction;
			float num = 0f;
			if (this.totalCoin <= 60000f)
			{
				num = this.totalCoin / 60000f * 100f;
			}
			else
			{
				this.totalCoin = 0f;
				this.lastCount++;
			}
			this.lastPercent = Mathf.Clamp(num, 0f, 100f);
			this.lastCount.ToString();
			string text = this.lastPercent.ToString("F1") + "%";
			this.CenterPercentText.text = (text ?? "");
		}

		public bool ReduceJackBoxCount()
		{
			if (this.lastCount == 0)
			{
				return false;
			}
			this.lastCount--;
			string str = this.lastCount.ToString();
			string str2 = this.lastPercent.ToString() + "%";
			this.CenterPercentText.text = str + "\n" + str2;
			return true;
		}

		public void ShowCenterPercentText()
		{
			if (!Plugin.goldManager.GetJumperStateRecord(this.zombie))
			{
				return;
			}
			this.InitCenterPercentText();
			if (this.lastCount >= 1)
			{
				this.CenterPercentText.text = "100%";
				this.CenterPercentText.color = Color.red;
			}
			else
			{
				string text = this.lastPercent.ToString("F1") + "%";
				this.CenterPercentText.text = (text ?? "");
			}
			this.CenterPercentText.gameObject.SetActive(Board.Instance.showZombieHealth);
		}

		public void HideCenterPercentText()
		{
			if (this.CenterPercentText != null)
			{
				this.CenterPercentText.gameObject.SetActive(false);
			}
		}

		public void DestroyCenterPercentText()
		{
			if (this.CenterPercentText != null)
			{
				UnityEngine.Object.Destroy(this.CenterPercentText.gameObject);
				this.CenterPercentText = null;
			}
		}

		public void CreateCoinFlyToZombie(Zombie zombie, int coinType = 36)
		{
			if (zombie == null)
			{
				return;
			}
			Vector3 vector = new Vector3(-7.5f, -5f, 0f);
			Vector3 targetPosition = new Vector3(zombie.axis.position.x, zombie.axis.position.y + 1.5f, zombie.axis.position.z);
			GameObject[] array = GameAPP.itemPrefab;
			if (array == null || coinType >= array.Length)
			{
				return;
			}
			GameObject gameObject = array[coinType];
			if (gameObject == null)
			{
				return;
			}
			GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(gameObject, vector, Quaternion.identity);
			if (gameObject2 == null)
			{
				return;
			}
			CoinFlyAnimationManager.AddFlyingCoin(gameObject2, targetPosition, 1.2f);
		}

		public bool IsInRange3x3(Zombie z)
		{
			int theZombieRow = z.theZombieRow;
			int theZombieRow2 = this.zombie.theZombieRow;
			if (theZombieRow != theZombieRow2 && theZombieRow != theZombieRow2 + 1 && theZombieRow != theZombieRow2 - 1)
			{
				return false;
			}
			int column = z.Column;
			int column2 = this.zombie.Column;
			return column == column2 || column == column2 + 1 || column == column2 - 1;
		}

		public List<Zombie> Debuff3x3Zombie()
		{
			Plugin.goldManager.Clear();
			List<Zombie> allZombies = Lawnf.GetAllZombies(false);
			foreach (Zombie zombie in allZombies)
			{
				if (zombie.theStatus != (ZombieStatus)1 && this.IsInRange3x3(zombie))
				{
					Plugin.goldManager.AddRecord(zombie);
				}
			}
			return allZombies;
		}

		public void Awake()
		{
		}

		public void Update()
		{
			if (GameAPP.theGameStatus != (GameStatus)0)
			{
				return;
			}
			if (this.zombie == null)
			{
				return;
			}
			if (this.zombie.beforeDying)
			{
				return;
			}
			this.ShowCenterPercentText();
			CoinFlyAnimationManager.UpdateFlyingCoins(delegate
			{
			});
			if (Plugin.goldManager.GetJumperStateRecord(this.zombie))
			{
				int mask = LayerMask.GetMask(new string[]
				{
					"Zombie"
				});
				Physics2D.OverlapCircleAll(this.zombie.transform.position, 6f, mask);
				if (this.waitTime >= 5f)
				{
					this.waitTime = 0f;
					if (this.lastCount < 1)
					{
						int theMoney = Board.Instance.theMoney;
						int num = Random.Range(3000, 8001);
						if (theMoney >= num)
						{
							this.updateTime += Time.deltaTime;
							Board.Instance.theMoney -= theMoney;
							int i = num / 1000;
							while (i > 0)
							{
								i--;
								this.CreateCoinFlyToZombie(this.zombie, 36);
								CoinFlyAnimationManager.UpdateFlyingCoins(delegate
								{
								});
							}
							this.UpdateCenterPercentText((float)num, false);
						}
						this.zombie.Recover((float)(num / 10));
					}
				}
				else
				{
					this.waitTime += Time.deltaTime;
				}
				int theZombieRow = this.zombie.theZombieRow;
				int column = this.zombie.Column;
				this.Debuff3x3Zombie();
			}
		}

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
		public bool hasUsedNoJumperRevive;
	}
}
