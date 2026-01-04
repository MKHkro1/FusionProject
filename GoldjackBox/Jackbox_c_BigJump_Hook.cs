using System;
using HarmonyLib;
using UnityEngine;

namespace UltimateGoldJackBoxZombieMod
{
    [HarmonyPatch(typeof(Jackbox_b), "BigJump")]
    public class Jackbox_c_BigJump_Hook
    {
        [HarmonyPrefix]
        public static bool Prefix(Zombie __instance)
        {
            try
            {
                if (__instance.theZombieType == Plugin.theNewZombieType)
                {
                    UltimateGoldJackBox? component = __instance.gameObject.GetComponent<UltimateGoldJackBox>();
                    if (component == null || component.zombie == null || component.zombie.axis == null)
                    {
                        return false;
                    }
                    JumpDataStore.JumpData orCreate = JumpDataStore.GetOrCreate(component.zombie);
                    if ((int)component.zombie.theStatus != 1)
                    {
                        component.zombie.theStatus = (ZombieStatus)20;
                    }
                    if (component.zombie.axis != null)
                    {
                        SpriteRenderer? component2 = component.zombie.axis.GetComponent<SpriteRenderer>();
                        if (component2 != null)
                        {
                            component2.enabled = false;
                        }
                    }
                    orCreate.IsInBigJump = true;
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
