using System;
using HarmonyLib;
using Il2CppSystem.Collections.Generic;

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
                zombieInfo.name = "究极黄金玩偶匣跳跳王(" + ((int)Plugin.theNewZombieType).ToString() + ")";
                zombieInfo.introduce = "作者：不爱染发的赛亚人、文枭S、铁甲机鱼、梧萱梦汐X\n\n韧性：54000\n\n伤害：1000\n\n特点：领袖僵尸。免疫击退、定身、冻结、寒冷、蒜毒、黄油、啃咬。未下跳杆时减伤40%，限伤5000；下跳杆后减伤70%，限伤3000。每次爆炸/下跳杆/死亡时，复活周围3x3血量最高的最多5个僵尸，不包含自身类型，包括究极僵尸。只能复活僵尸一次（一次性复活能力），且每种僵尸只能复活一只，挪动手套后会重新计算，自身复活之后将丢失复活僵尸的能力。爆炸时同时吸取复活僵尸血量的金币数并随机传送到最右侧随机一行。下跳杆时也会爆炸，不会传送。小跳间隔时间为2～5秒。不会进行大跳，只会进行小跳，移速慢。\n\n只有周围死亡僵尸超过随机3～5个且僵尸总血量（包括一级防具）超过随机15000～54000血量（血量会因不同模式有调整）时才下跳杆。每5秒吸取3000～8000金币，并恢复金币数的血量，到达100%时不再回血。周围的僵尸减伤50%，限伤3000，并免疫定身、击退和黄油。当吸取金币数到达60000时，获得一次复活机会。（复活：从最右侧随机一行复活）\n\n当全程没有下跳杆死亡时，执行复活机制（一次）。在场时，不能魅惑跳跳类僵尸。\n\n与玩偶匣跳跳王有5%伴生。1%概率在僵尸被伞斩杀成金佛后出现。";
                zombieInfo.info = "";
                zombieInfo.theZombieType = Plugin.theNewZombieType;
                AlmanacZombieMenu.ZombieAlmanacData.Add(Plugin.theNewZombieType, zombieInfo);
            }
            catch
            {
            }
        }

        // 词条已取消，移除TravelMgr.Awake补丁
    }

    /// <summary>
    /// 在NoticeMenu.Start之后注册僵尸预览图到zombieSprites字典
    /// </summary>
    [HarmonyPatch(typeof(NoticeMenu), "Start")]
    public static class ZombieSpritePatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            // 注册僵尸预览图到zombieSprites字典（用于图鉴等显示）
            if (Plugin._pendingZombieSprite != null && GameAPP.resourcesManager != null && GameAPP.resourcesManager.zombieSprites != null)
            {
                GameAPP.resourcesManager.zombieSprites[Plugin.theNewZombieType] = Plugin._pendingZombieSprite;
                Plugin.Log?.LogInfo("究极金丑插件: 成功注册预览图到zombieSprites字典");
            }
        }
    }
}
