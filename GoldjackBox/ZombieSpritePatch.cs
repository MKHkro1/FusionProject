using System;
using BepInEx.Logging;
using HarmonyLib;

namespace UltimateGoldJackBoxZombieMod
{
	[HarmonyPatch(typeof(NoticeMenu), "Start")]
	public static class ZombieSpritePatch
	{
		[HarmonyPostfix]
		public static void Postfix()
		{
			if (Plugin._pendingZombieSprite != null && GameAPP.resourcesManager != null && GameAPP.resourcesManager.zombieSprites != null)
			{
				GameAPP.resourcesManager.zombieSprites[Plugin.theNewZombieType] = Plugin._pendingZombieSprite;
				ManualLogSource log = Plugin.Log;
				if (log == null)
				{
					return;
				}
				log.LogInfo("究极金丑插件: 成功注册预览图到zombieSprites字典");
			}
		}
	}
}
