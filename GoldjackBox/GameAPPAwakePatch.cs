using System;
using HarmonyLib;
using Il2CppSystem.Collections.Generic;
using Random = UnityEngine.Random;

namespace UltimateGoldJackBoxZombieMod
{
	[HarmonyPatch]
	public class GameAPPAwakePatch
	{
		[HarmonyPatch(typeof(AlmanacZombieMenu), "Awake")]
		[HarmonyPostfix]
		public static void Awake()
		{
		}

		[HarmonyPatch(typeof(AlmanacZombieMenu), "InitNameAndInfoFromJson")]
		[HarmonyPostfix]
		public static void PostInitNameAndInfoFromJson()
		{
			try
			{
				Dictionary<ZombieType, ZombieInfo> zombieAlmanacData = AlmanacZombieMenu.ZombieAlmanacData;
				ZombieInfo zombieInfo = new ZombieInfo();
				zombieInfo.name = $"究极黄金玩偶匣跳跳王({(int)Plugin.theNewZombieType})";
				zombieInfo.introduce = "<color=#3D1400>作者：</color><color=#FF0000>不爱染发的赛亚人、文枭S、铁甲机鱼、梧萱梦汐X</color>\n<color=#3D1400>韧性：</color><color=red>54000</color>\n<color=#3D1400>伤害：</color><color=red>1000</color>\n<color=#3D1400>特点：</color><color=red>领袖僵尸。与玩偶匣跳跳王有5%伴生。免疫击退、定身、冻结、寒冷、蒜毒、黄油、啃咬。未下跳杆时减伤40%，限伤5000；下跳杆后减伤70%，限伤3000。每次爆炸/下跳杆/死亡时，复活周围3x3血量最高的最多5个僵尸，不包含自身类型，包括究极僵尸。爆炸时同时吸取复活僵尸血量的金币数并随机传送到最右侧随机一行。下跳杆时也会爆炸，不会传送。小跳间隔时间为2～5秒。</color>\n<color=red>只有周围死亡僵尸超过随机3～5个且僵尸总血量（包括一级防具）超过随机15000～54000血量（血量会因不同模式有调整）时才下跳杆。下跳杆后，每5秒吸取3000～8000金币，并恢复金币数/10的血量，到达100%时不再回血。下跳杆后周围的僵尸减伤50%，限伤3000，并免疫定身、击退和黄油。当吸取金币数到达60000时，获得一次复活机会。（复活：从最右侧随机一行复活）\n当全程没有下跳杆死亡时，执行复活机制。在场时，不能魅惑跳跳类僵尸。</color>\n";
				zombieInfo.info = "";
				zombieInfo.theZombieType = Plugin.theNewZombieType;
				AlmanacZombieMenu.ZombieAlmanacData.Add(Plugin.theNewZombieType, zombieInfo);
			}
			catch
			{
			}
		}

		// 伴生机制：与玩偶匣跳跳王（僵尸ID：325）有5%伴生
		[HarmonyPatch(typeof(CreateZombie), "SetZombie")]
		[HarmonyPostfix]
		public static void SetZombie_Postfix(CreateZombie __instance, int theRow, ZombieType theZombieType, float theX, bool isIdle)
		{
			// 当生成玩偶匣跳跳王(325)时，有5%概率伴生究极黄金玩偶匣跳跳王
			if ((int)theZombieType == 325)
			{
				if (Random.Range(0f, 100f) < 5f)
				{
					CreateZombie.Instance.SetZombie(theRow, Plugin.theNewZombieType, theX + 0.5f, isIdle);
				}
			}
		}
	}
}
