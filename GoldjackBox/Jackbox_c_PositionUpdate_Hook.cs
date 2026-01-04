using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace UltimateGoldJackBoxZombieMod
{
    [HarmonyPatch(typeof(Jackbox_b), "PositionUpdate")]
    public class Jackbox_c_PositionUpdate_Hook
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
                        return false;
                    }
                    JumpDataStore.JumpData orCreate = JumpDataStore.GetOrCreate(component.zombie);
                    if (orCreate.IsInBigJump && (int)component.zombie.theStatus == 20)
                    {
                        HandleBigJumpPhysics(component.zombie, orCreate);
                        return false;
                    }
                    if (orCreate.IsInSmallJump)
                    {
                        HandleSmallJumpAnimation(component.zombie, orCreate);
                        return false;
                    }
                    Vector3 position = component.zombie.axis.position;
                    if ((int)component.zombie.theStatus != 14 && (int)component.zombie.theStatus != 0)
                    {
                        if (component.zombie.rb != null)
                        {
                            component.zombie.rb.velocity = Vector2.zero;
                            Vector3 position2 = component.zombie.axis.position;
                            float landY = Plugin.GetLandY(component.zombie, position2.x);
                            Vector3 vector = new Vector3(position2.x, landY, 0f);
                            component.zombie.AdjustPosition(vector);
                        }
                        return false;
                    }
                    if (_callingBase.Contains(component.zombie))
                    {
                        return false;
                    }
                    _callingBase.Add(component.zombie);
                    Plugin.CallBase<Zombie>(component.zombie, "PositionUpdate", Array.Empty<object>());
                    return false;
                }
            }
            catch (Exception)
            {
                return true;
            }
            return true;
        }

        private static void HandleBigJumpPhysics(Jackbox_c instance, JumpDataStore.JumpData data)
        {
            if (instance.rb == null)
            {
                return;
            }
            instance.rb.velocity = new Vector2(instance.vx, instance.vy);
            instance.vy -= instance.dy * Time.deltaTime;
            Vector3 position = instance.axis.position;
            float landY = Plugin.GetLandY(instance, position.x);
            if (instance.vy < 0f && position.y <= landY)
            {
                instance.theStatus = (ZombieStatus)14;
                if (instance.anim != null)
                {
                    instance.anim.SetTrigger("jumpOver");
                }
                SpriteRenderer? component = instance.axis.GetComponent<SpriteRenderer>();
                if (component != null)
                {
                    component.enabled = true;
                }
                instance.rb.velocity = Vector2.zero;
                Vector3 vector = new Vector3(position.x, landY, 0f);
                instance.AdjustPosition(vector);
                data.IsInBigJump = false;
                data.HasBigJumped = true;
                data.SmallJumpTimer = 0f;
            }
        }

        private static void HandleSmallJumpAnimation(Jackbox_c instance, JumpDataStore.JumpData data)
        {
            data.SmallJumpProgress += Time.deltaTime / 0.5f;
            if (data.SmallJumpProgress >= 1f)
            {
                data.SmallJumpProgress = 1f;
                data.IsInSmallJump = false;
                if (instance.anim != null)
                {
                    instance.anim.SetTrigger("jumpOver");
                }
                try
                {
                    GameAPP.PlaySound(109, 0.5f, 1f);
                }
                catch
                {
                }
            }
            float smallJumpProgress = data.SmallJumpProgress;
            float num = Mathf.Lerp(data.SmallJumpStartX, data.SmallJumpTargetX, smallJumpProgress);
            float landY = Plugin.GetLandY(instance, num);
            Vector3 vector = new Vector3(num, landY, 0f);
            instance.AdjustPosition(vector);
            if (instance.rb != null)
            {
                instance.rb.velocity = Vector2.zero;
            }
            if (instance.axis != null)
            {
                data.SavedPosition = instance.axis.position;
            }
        }
    }
}
