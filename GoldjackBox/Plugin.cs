using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using CustomizeLib.BepInEx;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UltimateGoldJackBoxZombieMod
{
	[BepInPlugin("com.deqing.UltimateGoldJackBoxZombieMod", "UltimateGoldJackBoxZombieMod", "1.0.0")]
	public class Plugin : BasePlugin
	{
		public override void Load()
		{
			if (Plugin.Log == null)
			{
				Plugin.Log = new ManualLogSource("UltimateGoldJackBoxZombieMod");
				BepInEx.Logging.Logger.Sources.Add(Plugin.Log);
			}
			this._harmony = new Harmony("com.jackboxoverride.mod");
			this._harmony.PatchAll();
			ClassInjector.RegisterTypeInIl2Cpp<UltimateGoldJackBox>();
			ClassInjector.RegisterTypeInIl2Cpp<ZombieSpawner>();
			this.RegisterNewZombieType();
			Plugin.LoadAssetBundle();
			Plugin.Log.LogInfo("UltimateGoldJackBoxZombieMod loaded");
		}

		public static void LoadAssetBundle()
		{
			AssetBundle assetBundle = CustomCore.GetAssetBundle(Assembly.GetExecutingAssembly(), "goldjackbox");
			if (assetBundle == null)
			{
				ManualLogSource log = Plugin.Log;
				if (log == null)
				{
					return;
				}
				log.LogError("究极金丑插件: 无法加载金丑资源包");
				return;
			}
			else
			{
				bool flag;
				foreach (UnityEngine.Object @object in assetBundle.LoadAllAssets())
				{
					ManualLogSource log2 = Plugin.Log;
					if (log2 != null)
					{
						BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(15, 2, out flag);
						if (flag)
						{
							bepInExInfoLogInterpolatedStringHandler.AppendLiteral("asset: ");
							bepInExInfoLogInterpolatedStringHandler.AppendFormatted<string>(@object.name);
							bepInExInfoLogInterpolatedStringHandler.AppendLiteral(", type: ");
							bepInExInfoLogInterpolatedStringHandler.AppendFormatted<string>(@object.GetType().FullName);
						}
						log2.LogInfo(bepInExInfoLogInterpolatedStringHandler);
					}
				}
				GameObject asset = assetBundle.GetAsset<GameObject>("JackboxJumpZombie");
				if (!(asset == null))
				{
					ManualLogSource log3 = Plugin.Log;
					if (log3 != null)
					{
						log3.LogInfo("究极金丑插件: 成功获取UltimateGoldJackBox预制体");
					}
					AssetBundle assetBundle2 = CustomCore.GetAssetBundle(Assembly.GetExecutingAssembly(), "jackboxjumppreview");
					if (assetBundle2 != null)
					{
						foreach (UnityEngine.Object object2 in assetBundle2.LoadAllAssets())
						{
							ManualLogSource log2 = Plugin.Log;
							if (log2 != null)
							{
								BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(23, 2, out flag);
								if (flag)
								{
									bepInExInfoLogInterpolatedStringHandler.AppendLiteral("preview asset: ");
									bepInExInfoLogInterpolatedStringHandler.AppendFormatted<string>(object2.name);
									bepInExInfoLogInterpolatedStringHandler.AppendLiteral(", type: ");
									bepInExInfoLogInterpolatedStringHandler.AppendFormatted<string>(object2.GetType().FullName);
								}
								log2.LogInfo(bepInExInfoLogInterpolatedStringHandler);
							}
							GameObject gameObject = object2.TryCast<GameObject>();
							if (gameObject != null)
							{
								SpriteRenderer component = gameObject.GetComponent<SpriteRenderer>();
								if (component != null && component.sprite != null)
								{
									Plugin._pendingZombieSprite = component.sprite;
									CustomCore.RegisterCustomSprite(801, Plugin._pendingZombieSprite);
									log2 = Plugin.Log;
									if (log2 != null)
									{
										BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(27, 1, out flag);
										if (flag)
										{
											bepInExInfoLogInterpolatedStringHandler.AppendLiteral("究极金丑插件: 成功从预制体 ");
											bepInExInfoLogInterpolatedStringHandler.AppendFormatted<string>(object2.name);
											bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" 加载预览图Sprite");
										}
										log2.LogInfo(bepInExInfoLogInterpolatedStringHandler);
										break;
									}
									break;
								}
							}
							Sprite sprite = object2.TryCast<Sprite>();
							if (sprite != null)
							{
								Plugin._pendingZombieSprite = sprite;
								CustomCore.RegisterCustomSprite(801, Plugin._pendingZombieSprite);
								log2 = Plugin.Log;
								if (log2 != null)
								{
									BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(24, 1, out flag);
									if (flag)
									{
										bepInExInfoLogInterpolatedStringHandler.AppendLiteral("究极金丑插件: 成功直接加载预览图Sprite ");
										bepInExInfoLogInterpolatedStringHandler.AppendFormatted<string>(object2.name);
									}
									log2.LogInfo(bepInExInfoLogInterpolatedStringHandler);
									break;
								}
								break;
							}
						}
						if (Plugin._pendingZombieSprite == null)
						{
							ManualLogSource log4 = Plugin.Log;
							if (log4 != null)
							{
								log4.LogWarning("究极金丑插件: 无法从预览图AssetBundle中获取Sprite");
							}
						}
					}
					else
					{
						ManualLogSource log5 = Plugin.Log;
						if (log5 != null)
						{
							log5.LogWarning("究极金丑插件: 无法加载预览图资源包jackboxjumppreview");
						}
					}
					int spriteId = (Plugin._pendingZombieSprite != null) ? 801 : -1;
					CustomCore.RegisterCustomZombie<Jackbox_c, UltimateGoldJackBox>(Plugin.theNewZombieType, asset, spriteId, 100, 72003, 0, 0);
					string name = $"究极黄金玩偶匣跳跳王({(int)Plugin.theNewZombieType})";
					string description = "<color=#3D1400>作者：</color><color=#FF0000>不爱染发的赛亚人、文枭S、铁甲机鱼、梧萱梦汐X</color>\n<color=#3D1400>韧性：</color><color=red>54000</color>\n<color=#3D1400>伤害：</color><color=red>1000</color>\n<color=#3D1400>特点：</color><color=red>领袖僵尸。与玩偶匣跳跳王有5%伴生。免疫击退、定身、冻结、寒冷、蒜毒、黄油、啃咬。未下跳杆时减伤40%，限伤5000；下跳杆后减伤70%，限伤3000。每次爆炸/下跳杆/死亡时，复活周围3x3血量最高的最多5个僵尸，不包含自身类型，包括究极僵尸。爆炸时同时吸取复活僵尸血量的金币数并随机传送到最右侧随机一行。下跳杆时也会爆炸，不会传送。小跳间隔时间为2～5秒。</color>\n<color=red>只有周围死亡僵尸超过随机1～3个且僵尸总血量（包括一级防具）超过随机5000～20000血量（血量会因不同模式有调整）时才下跳杆。下跳杆后，每5秒吸取3000～8000金币，并恢复金币数/10的血量，到达100%时不再回血。下跳杆后周围的僵尸减伤50%，限伤3000，并免疫定身、击退和黄油。当吸取金币数到达60000时，获得一次复活机会。（复活：从最右侧随机一行复活）\n当全程没有下跳杆死亡时，可以一直复活。在场时，不能魅惑跳跳类僵尸。</color>\n";
					CustomCore.AddZombieAlmanacStrings((int)Plugin.theNewZombieType, name, description);
					return;
				}
				ManualLogSource log6 = Plugin.Log;
				if (log6 == null)
				{
					return;
				}
				log6.LogError("究极金丑插件: 无法从AssetBundle中获取UltimateGoldJackBox预制体");
				return;
			}
		}

		private void RegisterNewZombieType()
		{
			Array values = Enum.GetValues(typeof(ZombieType));
			int num = 0;
			foreach (object obj in values)
			{
				ZombieType zombieType = (ZombieType)obj;
				if ((int)zombieType > num)
				{
					num = (int)zombieType;
				}
			}
			Plugin.theNewZombieType = (ZombieType)(num + 1);
			ManualLogSource log = Plugin.Log;
			if (log != null)
			{
				bool flag;
				BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(39, 1, out flag);
				if (flag)
				{
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("RegisterNewZombieType theNewZombieType:");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted<int>((int)Plugin.theNewZombieType);
				}
				log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
			}
		}

		public static void TeleportPosition(Jackbox_c zombie, int lastCount = 0)
		{
			int rowNum = Board.Instance.rowNum;
			int row = Random.Range(0, rowNum);
			Vector3 position = zombie.axis.position;
			int lastCount2 = (lastCount > 0) ? (lastCount - 1) : -1;
			ZombieSpawner.Instance.SpawnZombieAt(position, lastCount2, (int)Plugin.theNewZombieType, true, 9f, row);
		}

		public static void CallBase<TBase>(object instance, string methodName, params object[] args)
		{
			MethodInfo methodInfo = AccessTools.Method(typeof(TBase), methodName, null, null);
			if (methodInfo == null)
			{
				return;
			}
			methodInfo.Invoke(instance, args);
		}

		public static void HandleSmallJumpCycle(Jackbox_c instance, JumpDataStore.JumpData data, Vector3 currentPos)
		{
			if (data.IsInSmallJump)
			{
				return;
			}
			if (instance.rb != null)
			{
				instance.rb.velocity = Vector2.zero;
			}
			data.SmallJumpTimer += Time.deltaTime;
			if (data.SmallJumpTimer >= data.NextSmallJumpTime)
			{
				Plugin.TriggerSmallJump(instance, data, currentPos);
				data.SmallJumpTimer = 0f;
				data.NextSmallJumpTime = Random.Range(2f, 6f);
			}
		}

		public static float GetLandY(Jackbox_c instance, float x)
		{
			float result;
			try
			{
				Mouse instance2 = Mouse.Instance;
				if (instance2 == null)
				{
					result = 0f;
				}
				else
				{
					float num = instance2.GetLandY(x, instance.theZombieRow);
					if (instance.board != null && instance.board.boardTag.isRoof)
					{
						num += 0.3f;
					}
					result = num;
				}
			}
			catch
			{
				result = 0f;
			}
			return result;
		}

		public static float getHealthInTravel()
		{
			int theCurrentSurvivalRound = Board.Instance.theCurrentSurvivalRound;
			float num = (float)theCurrentSurvivalRound * 0.2f;
			if (Board.Instance.boardTag.isRogue)
			{
				if (theCurrentSurvivalRound >= 1)
				{
					int num2 = (theCurrentSurvivalRound < 4) ? theCurrentSurvivalRound : 4;
					num += (float)num2 * 0.2f;
				}
				if (theCurrentSurvivalRound >= 5)
				{
					int num3 = (theCurrentSurvivalRound - 4 < 4) ? (theCurrentSurvivalRound - 4) : 4;
					num += (float)num3 * 0.4f;
				}
				if (theCurrentSurvivalRound >= 9)
				{
					int num4 = (theCurrentSurvivalRound - 8 < 4) ? (theCurrentSurvivalRound - 8) : 4;
					num += (float)num4 * 0.6f;
				}
				if (theCurrentSurvivalRound >= 13)
				{
					int num5 = (theCurrentSurvivalRound - 12 < 4) ? (theCurrentSurvivalRound - 12) : 4;
					num += (float)num5;
				}
			}
			if (GameAPP.difficulty == 5)
			{
				num *= 1.5f;
			}
			if (Board.Instance.boardTag.isEndless)
			{
				num = (float)theCurrentSurvivalRound * 0.2f;
			}
			if (Lawnf.TravelCurse())
			{
				num += num;
			}
			return Mathf.Min(100f, num + 1f);
		}

		public static void zombieEvent(Zombie __instance, List<ZombieDieRecord> recodes, string msg)
		{
			foreach (ZombieDieRecord zombieDieRecord in recodes)
			{
				float boxXFromColumn = Mouse.Instance.GetBoxXFromColumn(zombieDieRecord.col);
				float boxYFromRow = Mouse.Instance.GetBoxYFromRow(zombieDieRecord.row);
				Vector3 position = new Vector3(boxXFromColumn, boxYFromRow, 0f);
				ZombieSpawner.Instance.SpawnZombieAt(position, 0, (int)zombieDieRecord.zombieType, false, boxXFromColumn, zombieDieRecord.row);
			}
		}

		public static void TriggerSmallJump(Jackbox_c instance, JumpDataStore.JumpData data, Vector3 currentPos)
		{
			data.IsInSmallJump = true;
			data.SmallJumpProgress = 0f;
			data.SmallJumpStartX = currentPos.x;
			data.SmallJumpTargetX = currentPos.x - 1f;
			if (instance.anim != null)
			{
				instance.anim.SetTrigger("jump");
			}
			try
			{
				GameAPP.PlaySound(109, 0.5f, 1f);
			}
			catch
			{
			}
		}

		public static new ManualLogSource Log;
		public static ZombieRecordManager zombieRecordManager = new ZombieRecordManager(5);
		public static GoldRecordManager goldManager = new GoldRecordManager();
		public static ZombieType theNewZombieType;
		public static int buff0 = -1;
		public static int theNewTravelId = -1;
		public Harmony _harmony;
		private const int PreviewSpriteId = 801;
		internal static Sprite _pendingZombieSprite;
	}
}
