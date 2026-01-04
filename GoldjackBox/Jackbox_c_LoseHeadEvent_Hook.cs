using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace UltimateGoldJackBoxZombieMod
{
	[HarmonyPatch(typeof(Jackbox_c), "LoseHeadEvent")]
	public class Jackbox_c_LoseHeadEvent_Hook
	{
		[HarmonyPrefix]
		public static bool Prefix(Jackbox_c __instance)
		{
			try
			{
				if (__instance.theZombieType == Plugin.theNewZombieType)
				{
					UltimateGoldJackBox component = __instance.gameObject.GetComponent<UltimateGoldJackBox>();
					if (component == null || component.zombie == null || component.zombie.axis == null)
					{
						return false;
					}
					if (component.zombie.theStatus == (ZombieStatus)0)
					{
						return false;
					}
					Vector3 position = component.zombie.axis.position;
					if (!component.zombie.isMindControlled)
					{
						UltimateGoldJackBox component2 = __instance.gameObject.GetComponent<UltimateGoldJackBox>();
						int column = __instance.Column;
						int count = 5;
						bool isUltimate = true;
						List<ZombieDieRecord> topRecordsAroundPosition = Plugin.zombieRecordManager.GetTopRecordsAroundPosition(component2.zombie.theZombieRow, column, isUltimate, count);
						if (topRecordsAroundPosition.Count > 0)
						{
							Plugin.zombieEvent(component2.zombie, topRecordsAroundPosition, "");
							float totalHealth = ZombieRecordManager.GetTotalHealth(topRecordsAroundPosition);
							component2.loseMoney(totalHealth);
						}
					}
					Lawnf.ZombieExplode(new Vector2(position.x, position.y + 0.6f), __instance.board, __instance.isMindControlled, __instance.theZombieRow, (Plant.DamageType)2);
					JumpDataStore.Remove(component.zombie);
					Plugin.goldManager.removeStateRecord(component.zombie);
					__instance.Die(2);
					return false;
				}
			}
			catch (Exception)
			{
				return true;
			}
			return true;
		}
	}
}
