using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace UltimateGoldJackBoxZombieMod
{
	[HarmonyPatch]
	public class UltimateGoldJackBoxPatch
	{
		[HarmonyPatch(typeof(Zombie), "UpdateHealthText")]
		[HarmonyPostfix]
		public static void UpdateHealthText_Postfix(Zombie __instance)
		{
			if (__instance.theZombieType == Plugin.theNewZombieType)
			{
				UltimateGoldJackBox component = __instance.gameObject.GetComponent<UltimateGoldJackBox>();
				if (component == null)
				{
					return;
				}
				TextMeshPro healthText = component.zombie.healthText;
				TextMeshPro healthTextShadow = component.zombie.healthTextShadow;
				if (healthText == null)
				{
					return;
				}
				if (component.zombie.theMaxHealth <= 0)
				{
					return;
				}
				int num = component.zombie.theHealth - 18003;
				if (num <= 0)
				{
					num = 0;
				}
				string text = $"{num}/{component.zombie.theMaxHealth - 18003}";
				healthText.text = text;
				if (healthTextShadow != null)
				{
					healthTextShadow.text = text;
				}
			}
		}

		private static void FixSorting(Component comp)
		{
			if (comp == null)
			{
				return;
			}
			SortingGroup component = comp.GetComponent<SortingGroup>();
			if (component != null)
			{
				component.sortingLayerName = "Default";
				component.sortingOrder += 90000;
				component.sortAtRoot = true;
			}
		}

		[HarmonyPatch(typeof(Zombie), "InitHealth")]
		[HarmonyPostfix]
		public static void InitHealth_Postfix(Zombie __instance)
		{
			if (__instance.theZombieType == Plugin.theNewZombieType)
			{
				UltimateGoldJackBox component = __instance.gameObject.GetComponent<UltimateGoldJackBox>();
				if (component == null)
				{
					return;
				}
				TextMeshPro healthText = component.zombie.healthText;
				TextMeshPro healthTextShadow = component.zombie.healthTextShadow;
				if (healthText == null)
				{
					return;
				}
				string text = $"{component.zombie.theHealth - 18003}/{component.zombie.theMaxHealth - 18003}";
				healthText.color = new Color(1f, 0.84f, 0f);
				healthText.fontSize = 3f;
				healthText.text = text;
				if (healthTextShadow != null)
				{
					healthTextShadow.fontSize = 3f;
					healthTextShadow.text = text;
				}
			}
		}

		[HarmonyPatch(typeof(Zombie), "OnDestroy")]
		[HarmonyPostfix]
		public static void OnDestroy_Postfix(Zombie __instance)
		{
			if (__instance.theZombieType == Plugin.theNewZombieType)
			{
				UltimateGoldJackBox component = __instance.gameObject.GetComponent<UltimateGoldJackBox>();
				if (component == null || component.zombie == null)
				{
					return;
				}
				JumpDataStore.Remove(component.zombie);
				Plugin.goldManager.removeStateRecord(component.zombie);
			}
		}

		[HarmonyPatch(typeof(CreateZombie), "SetZombie")]
		[HarmonyPrefix]
		public static bool SetZombie_Prefix(Zombie __instance, int theRow, ref ZombieType theZombieType, float theX, bool isIdle)
		{
			return true;
		}

		[HarmonyPatch(typeof(Zombie), "Die")]
		[HarmonyPrefix]
		public static bool Die_Prefix(Zombie __instance, int reason)
		{
			Plugin.goldManager.RemoveRecord(__instance);
			if (__instance.theZombieType == Plugin.theNewZombieType)
			{
				UltimateGoldJackBox component = __instance.gameObject.GetComponent<UltimateGoldJackBox>();
				if (component == null)
				{
					return true;
				}
				bool jumperStateRecord = Plugin.goldManager.GetJumperStateRecord(component.zombie);
				if (reason == 2)
				{
					if (!jumperStateRecord)
					{
						// 没下撑杆时可以一直复活，移除hasUsedNoJumperRevive检查
						List<ZombieDieRecord> topRecordsAroundPosition = Plugin.zombieRecordManager.GetTopRecordsAroundPosition(__instance.theZombieRow, __instance.Column, true, 5);
						if (topRecordsAroundPosition.Count > 0)
						{
							Plugin.zombieEvent(component.zombie, topRecordsAroundPosition, "");
						}
						Plugin.goldManager.Clear();
						Plugin.TeleportPosition(component.zombie, 0);
					}
					return true;
				}
				if (reason == 3)
				{
					if (__instance.anim != null)
					{
						__instance.anim.SetTrigger("GoDie");
					}
					Plugin.goldManager.Clear();
					UnityEngine.Object.Destroy(component.gameObject);
					UnityEngine.Object.Destroy(__instance);
					return false;
				}
				if (jumperStateRecord)
				{
					if (component.ReduceJackBoxCount())
					{
						Plugin.goldManager.Clear();
						Plugin.TeleportPosition(component.zombie, component.lastCount);
					}
					else
					{
						List<ZombieDieRecord> topRecordsAroundPosition2 = Plugin.zombieRecordManager.GetTopRecordsAroundPosition(component.zombie.theZombieRow, __instance.Column, true, 5);
						Plugin.zombieEvent(component.zombie, topRecordsAroundPosition2, "");
						component.zombie.Die(3);
					}
					return true;
				}
				// 没下撑杆时可以一直复活，移除hasUsedNoJumperRevive检查
				Plugin.goldManager.Clear();
				Plugin.TeleportPosition(component.zombie, 0);
				return true;
			}
			else
			{
				if (__instance.theZombieType == (ZombieType)44 || __instance.theZombieType == (ZombieType)46)
				{
					return true;
				}
				ZombieDieRecord newRecord = new ZombieDieRecord(__instance.theZombieType, 0f, __instance.TotalFirstHealth, __instance.theZombieRow, Lawnf.GetColumnFromX(__instance.transform.position.x));
				Plugin.zombieRecordManager.AddRecord(newRecord);
				return true;
			}
		}

		[HarmonyPatch(typeof(TypeMgr), "IsGargantuar")]
		[HarmonyPostfix]
		public static void IsGargantuar_Postfix(ref ZombieType theZombieType, ref bool __result)
		{
			if (theZombieType == Plugin.theNewZombieType)
			{
				__result = true;
			}
		}

		[HarmonyPatch(typeof(TypeMgr), "UltimateZombie")]
		[HarmonyPostfix]
		private static void UltimateZombiePostfix(ref ZombieType theZombieType, ref bool __result)
		{
			if (theZombieType == Plugin.theNewZombieType)
			{
				__result = true;
			}
		}

		[HarmonyPatch(typeof(TypeMgr), "IsLeaderZombie")]
		[HarmonyPostfix]
		public static void IsLeaderZombie_Postfix(ref ZombieType theZombieType, ref bool __result)
		{
			if (theZombieType == Plugin.theNewZombieType)
			{
				__result = true;
			}
		}

		[HarmonyPatch(typeof(TypeMgr), "IsBossZombie")]
		[HarmonyPostfix]
		public static void IsBossZombie_Postfix(ref ZombieType theZombieType, ref bool __result)
		{
			if (theZombieType == Plugin.theNewZombieType)
			{
				__result = true;
			}
		}

		[HarmonyPatch(typeof(Zombie), "BodyTakeDamage")]
		[HarmonyPrefix]
		private static void BodyTakeDamage_Prefix(Zombie __instance, ref int theDamage)
		{
			if (__instance.theZombieType == Plugin.theNewZombieType)
			{
				UltimateGoldJackBox component = __instance.gameObject.GetComponent<UltimateGoldJackBox>();
				if (Plugin.goldManager.GetJumperStateRecord(component.zombie))
				{
					theDamage = (int)((float)theDamage * 0.3f);
					if (theDamage > 3000)
					{
						theDamage = 3000;
						return;
					}
				}
				else
				{
					theDamage = (int)((float)theDamage * 0.6f);
					if (theDamage > 5000)
					{
						theDamage = 5000;
					}
				}
				return;
			}
			HashSet<Zombie> records = Plugin.goldManager.GetRecords();
			if (records == null || records.Count <= 0)
			{
				return;
			}
			foreach (Zombie zombie in records)
			{
				if (zombie == __instance && !zombie.beforeDying)
				{
					theDamage = (int)((float)theDamage * 0.5f);
					if (theDamage > 3000)
					{
						theDamage = 3000;
					}
				}
			}
		}

		[HarmonyPatch(typeof(Jackbox_a), "LoseJumper")]
		[HarmonyPrefix]
		public static bool LoseJumper_Prefix(Zombie __instance)
		{
			if (__instance.theZombieType == Plugin.theNewZombieType)
			{
				bool isUltimate = true;
				int column = __instance.Column;
				List<ZombieDieRecord> topRecordsAroundPosition = Plugin.zombieRecordManager.GetTopRecordsAroundPosition(__instance.theZombieRow, column, isUltimate, 5);
				float num = 0f;
				foreach (ZombieDieRecord zombieDieRecord in topRecordsAroundPosition)
				{
					num += zombieDieRecord.health;
				}
				// 降低下撑杆条件：血量要求从15000-54000降低到5000-20000
				float num2 = (float)Random.Range(5000, 20001);
				// 降低下撑杆条件：僵尸数量要求从2-6降低到1-3
				int num3 = Random.Range(1, 4);
				float healthInTravel = Plugin.getHealthInTravel();
				float num4 = num2 * healthInTravel;
				if (topRecordsAroundPosition.Count < num3 || num < num4)
				{
					return false;
				}
				UltimateGoldJackBox component = __instance.GetComponent<UltimateGoldJackBox>();
				Plugin.zombieEvent(__instance, topRecordsAroundPosition, "LoseJumper");
				if (component != null && component.zombie != null)
				{
					Vector3 position = component.zombie.axis.position;
					Lawnf.ZombieExplode(new Vector2(position.x, position.y + 0.6f), __instance.board, __instance.isMindControlled, __instance.theZombieRow, (Plant.DamageType)2);
					component.zombie.theHealth = component.zombie.theMaxHealth;
					GoldBoxStateRecord goldBoxStateRecord = new GoldBoxStateRecord();
					goldBoxStateRecord.isLoseJumper = true;
					Plugin.goldManager.AddOrUpdatetStateRecord(component.zombie, goldBoxStateRecord);
					component.lastCount = 0;
				}
			}
			return true;
		}

		[HarmonyPatch(typeof(Zombie), "TakeDamage")]
		[HarmonyPrefix]
		public static bool TakeDamage_Prefix(Zombie __instance, DmgType theDamageType, ref int theDamage, bool fix)
		{
			if (__instance.theZombieType == Plugin.theNewZombieType)
			{
				UltimateGoldJackBox component = __instance.gameObject.GetComponent<UltimateGoldJackBox>();
				if (Plugin.goldManager.GetJumperStateRecord(component.zombie))
				{
					theDamage = (int)((float)theDamage * 0.3f);
					if (theDamage > 3000)
					{
						theDamage = 3000;
					}
				}
				else
				{
					theDamage = (int)((float)theDamage * 0.6f);
					if (theDamage > 5000)
					{
						theDamage = 5000;
					}
				}
			}
			else
			{
				HashSet<Zombie> records = Plugin.goldManager.GetRecords();
				if (records != null && records.Count > 0)
				{
					foreach (Zombie zombie in records)
					{
						if (zombie == __instance && !zombie.beforeDying)
						{
							theDamage = (int)((float)theDamage * 0.5f);
							if (theDamage > 3000)
							{
								theDamage = 3000;
							}
						}
					}
				}
			}
			return true;
		}

		[HarmonyPatch(typeof(Magnetshroom), "TryAttrackZombie")]
		[HarmonyPrefix]
		public static bool TryAttrackZombie_Prefix(Magnetshroom __instance, Zombie zombie, ref bool __result)
		{
			if (zombie.theZombieType == Plugin.theNewZombieType)
			{
				__result = false;
				return false;
			}
			return true;
		}

		[HarmonyPatch(typeof(Zombie), "KnockBack")]
		[HarmonyPrefix]
		public static bool KnockBack_Prefix(Zombie __instance, float x, Zombie.KnockBackReason reason)
		{
			try
			{
				if (__instance.theZombieType == Plugin.theNewZombieType)
				{
					__instance.gameObject.GetComponent<UltimateGoldJackBox>();
					return false;
				}
				HashSet<Zombie> records = Plugin.goldManager.GetRecords();
				if (records != null && records.Count > 0)
				{
					using (HashSet<Zombie>.Enumerator enumerator = records.GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							if (enumerator.Current == __instance)
							{
								return false;
							}
						}
					}
				}
			}
			catch (Exception)
			{
			}
			return true;
		}

		[HarmonyPatch(typeof(Zombie), "BeSmall")]
		[HarmonyPrefix]
		public static bool Prefix(Zombie __instance, float scale)
		{
			return __instance.theZombieType != Plugin.theNewZombieType;
		}

		[HarmonyPatch(typeof(CreateZombie), "SetZombieWithMindControl")]
		[HarmonyPrefix]
		public static bool CreateZombie_Prefix(CreateZombie __instance, int theRow, ZombieType theZombieType, float theX, bool withEffect)
		{
			if (theZombieType == Plugin.theNewZombieType)
			{
				return false;
			}
			bool flag = true;
			if (Plugin.goldManager.StateRecordCount() > 0)
			{
				flag = false;
			}
			return flag || (theZombieType != (ZombieType)32 && theZombieType != (ZombieType)34 && theZombieType != (ZombieType)324 && theZombieType != (ZombieType)325 && theZombieType != (ZombieType)326);
		}

		[HarmonyPatch(typeof(Zombie), "JalaedExplode")]
		[HarmonyPostfix]
		public static void JalaedExplode_Postfix(Zombie __instance, bool jala, int damage)
		{
			ZombieType theZombieType = __instance.theZombieType;
			ZombieType theNewZombieType = Plugin.theNewZombieType;
		}

		[HarmonyPatch(typeof(Zombie), "SetFreeze")]
		[HarmonyPrefix]
		public static bool SetFreeze_Prefix(Zombie __instance, float time, int theFreezeLevel)
		{
			return __instance.theZombieType != Plugin.theNewZombieType;
		}

		[HarmonyPatch(typeof(Zombie), "AddfreezeLevel")]
		[HarmonyPrefix]
		public static bool AddfreezeLevel_Prefix(Zombie __instance, int level)
		{
			return __instance.theZombieType != Plugin.theNewZombieType;
		}

		[HarmonyPatch(typeof(Zombie), "SetCold")]
		[HarmonyPrefix]
		public static bool SetCold_Prefix(Zombie __instance, float time)
		{
			return __instance.theZombieType != Plugin.theNewZombieType;
		}

		[HarmonyPatch(typeof(Zombie), "Buttered")]
		[HarmonyPrefix]
		public static bool Buttered_Prefix(Zombie __instance, float time, bool sprite)
		{
			if (__instance.theZombieType == Plugin.theNewZombieType)
			{
				return false;
			}
			HashSet<Zombie> records = Plugin.goldManager.GetRecords();
			if (records != null && records.Count > 0)
			{
				using (HashSet<Zombie>.Enumerator enumerator = records.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						if (enumerator.Current == __instance)
						{
							return false;
						}
					}
				}
				return true;
			}
			return true;
		}

		[HarmonyPatch(typeof(Zombie), "SetPortaled")]
		[HarmonyPrefix]
		public static bool SetPortaled_Prefix(Zombie __instance, float timer)
		{
			if (__instance.theZombieType == Plugin.theNewZombieType)
			{
				return false;
			}
			HashSet<Zombie> records = Plugin.goldManager.GetRecords();
			if (records != null && records.Count > 0)
			{
				using (HashSet<Zombie>.Enumerator enumerator = records.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						if (enumerator.Current == __instance)
						{
							return false;
						}
					}
				}
				return true;
			}
			return true;
		}

		[HarmonyPatch(typeof(Zombie), "SetPoison")]
		[HarmonyPrefix]
		public static bool SetPoison_Prefix(Zombie __instance)
		{
			return __instance.theZombieType != Plugin.theNewZombieType;
		}

		[HarmonyPatch(typeof(Zombie), "SetEmbered")]
		[HarmonyPrefix]
		public static bool SetEmbered_Prefix(Zombie __instance)
		{
			return __instance.theZombieType != Plugin.theNewZombieType;
		}

		[HarmonyPatch(typeof(Zombie), "SetGrap")]
		[HarmonyPrefix]
		public static bool SetGrap_Prefix(Zombie __instance, float time, bool land)
		{
			return __instance.theZombieType != Plugin.theNewZombieType;
		}

		[HarmonyPatch(typeof(Zombie), "SetMindControl")]
		[HarmonyPrefix]
		public static bool SetMindControl_Prefix(Zombie __instance, int controlLevel)
		{
			if (__instance.theZombieType == Plugin.theNewZombieType)
			{
				return false;
			}
			bool flag = true;
			if (Plugin.goldManager.StateRecordCount() > 0)
			{
				flag = false;
			}
			return flag || (__instance.theZombieType != (ZombieType)32 && __instance.theZombieType != (ZombieType)34 && __instance.theZombieType != (ZombieType)324 && __instance.theZombieType != (ZombieType)325 && __instance.theZombieType != (ZombieType)326);
		}

		[HarmonyPatch(typeof(ArmedGargantuar), "SetJalaed")]
		[HarmonyPrefix]
		public static bool SetJalaed_Prefix(Zombie __instance)
		{
			return __instance.theZombieType != Plugin.theNewZombieType;
		}

		[HarmonyPatch(typeof(UltimateChomper), "Bite")]
		[HarmonyPrefix]
		public static bool Bite_Prefix(UltimateChomper __instance, Zombie zombie)
		{
			return zombie.theZombieType != Plugin.theNewZombieType;
		}
	}
}
