using System;
using HarmonyLib;

namespace BigDoomGatling.BepInEx
{
    [HarmonyPatch(typeof(BigGatling), "Shoot1")]
    public class BigGatling_Shoot1_Patch
    {
        private static bool Prefix(BigGatling __instance)
        {
            bool flag = __instance.GetComponent<BigDoomGatling>() != null;
            return !flag;
        }
    }
}
