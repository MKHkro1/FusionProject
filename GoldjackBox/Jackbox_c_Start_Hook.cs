using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UltimateGoldJackBoxZombieMod
{
	[HarmonyPatch(typeof(Jackbox_c), "Start")]
	public class Jackbox_c_Start_Hook
	{
		[HarmonyPrefix]
		public static bool Prefix(Zombie __instance)
		{
			try
			{
				if (__instance.theZombieType == Plugin.theNewZombieType)
				{
					UltimateGoldJackBox component = __instance.gameObject.GetComponent<UltimateGoldJackBox>();
					if (component == null)
					{
						__instance.gameObject.AddComponent<UltimateGoldJackBox>();
					}
					component.originLocalScale = __instance.transform.localScale;
					component.Init(0);
					if (component.zombie == null || component.zombie.axis == null)
					{
						return false;
					}
					if (Jackbox_c_Start_Hook._callingBase.Contains(component.zombie))
					{
						return true;
					}
					Jackbox_c_Start_Hook._callingBase.Add(component.zombie);
					Plugin.CallBase<Zombie>(component.zombie, "Start", Array.Empty<object>());
					if (component.zombie.anim != null)
					{
						float num = Random.Range(1f, 1.5f);
						component.zombie.anim.SetFloat("jumpSpeed", num);
					}
					if (Board.Instance != null)
					{
						component.zombie.waitTime = 4.9f;
						component.zombie.jumpX = __instance.board.boardMaxX - 1.5f;
					}
					JumpDataStore.JumpData orCreate = JumpDataStore.GetOrCreate(component.zombie);
					orCreate.HasBigJumped = true;
					orCreate.IsInBigJump = false;
					orCreate.SmallJumpTimer = 0f;
					orCreate.NextSmallJumpTime = (float)Random.Range(2, 6);
					orCreate.IsInSmallJump = false;
					orCreate.OriginalWaitTime = component.zombie.waitTime;
					orCreate.OriginalJumpX = component.zombie.jumpX;
					if (component.zombie.axis != null)
					{
						orCreate.SavedPosition = component.zombie.axis.position;
					}
					float boxYFromRow = Mouse.Instance.GetBoxYFromRow(component.zombie.theZombieRow);
					Vector3 vector = new Vector3(component.zombie.transform.position.x, boxYFromRow - 0.7f, component.zombie.transform.position.z);
					component.zombie.AdjustPosition(vector);
					return false;
				}
			}
			catch (Exception)
			{
				return true;
			}
			return true;
		}

		private static readonly HashSet<Jackbox_c> _callingBase = new HashSet<Jackbox_c>();
	}
}
