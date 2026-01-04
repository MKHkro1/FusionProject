using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace UltimateGoldJackBoxZombieMod
{
    [HarmonyPatch(typeof(Jackbox_b), "Update")]
    public class Jackbox_c_Update_Hook
    {
        private static readonly HashSet<Jackbox_c> _callingBase = new HashSet<Jackbox_c>();

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
                        if (component?.zombie != null && _callingBase.Contains(component.zombie))
                        {
                            return true;
                        }
                        if (component?.zombie != null)
                        {
                            _callingBase.Add(component.zombie);
                            Plugin.CallBase<Zombie>(component.zombie, "Update", Array.Empty<object>());
                        }
                        return false;
                    }
                    else
                    {
                        JumpDataStore.JumpData orCreate = JumpDataStore.GetOrCreate(component.zombie);
                        Vector3 position = component.zombie.axis.position;
                        bool jumperStateRecord = Plugin.goldManager.GetJumperStateRecord(component.zombie);
                        if (!orCreate.HasBigJumped)
                        {
                            if (!jumperStateRecord)
                            {
                                HandleBigJumpTrigger(component.zombie, orCreate, position);
                            }
                        }
                        else if (!orCreate.IsInBigJump && !jumperStateRecord)
                        {
                            Plugin.HandleSmallJumpCycle(component.zombie, orCreate, position);
                        }
                        if (_callingBase.Contains(component.zombie))
                        {
                            return true;
                        }
                        _callingBase.Add(component.zombie);
                        Plugin.CallBase<Zombie>(component.zombie, "Update", Array.Empty<object>());
                        return false;
                    }
                }
            }
            catch (Exception)
            {
                return true;
            }
            return true;
        }

        private static void HandleBigJumpTrigger(Jackbox_c instance, JumpDataStore.JumpData data, Vector3 currentPos)
        {
            if (instance.isMindControlled)
            {
                if (currentPos.x >= instance.board.boardMaxX - 1.5f && !instance.jumped && (int)instance.theStatus != 0 && (int)instance.theStatus != 1)
                {
                    instance.theStatus = (ZombieStatus)14;
                }
                return;
            }
            if (currentPos.x <= instance.jumpX && currentPos.x > 1.5f && !instance.jumped)
            {
                instance.waitTime += Time.deltaTime;
                if ((int)instance.theStatus != 1 && (int)instance.theStatus != 0)
                {
                    instance.theStatus = (ZombieStatus)19;
                }
                if (instance.rb != null)
                {
                    instance.rb.velocity = Vector2.zero;
                }
                if (instance.waitTime >= 5f)
                {
                    if (instance.anim != null)
                    {
                        instance.anim.SetTrigger("jump");
                    }
                    instance.jumped = true;
                    instance.vx = -4.25f;
                    instance.vy = 6f;
                }
            }
            if (currentPos.x >= instance.board.boardMaxX - 1.5f && !instance.jumped && (int)instance.theStatus != 1 && (int)instance.theStatus != 0)
            {
                instance.theStatus = (ZombieStatus)14;
            }
        }
    }
}
