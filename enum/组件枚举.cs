
using System;

namespace GameEnums
{

    /// 物品类型枚举 - 用于CreateItem.SetCoin的第三个参数
    /// 完整的游戏物品ID定义（从反编译文件中提取）

    public enum ItemType
    {
        NormalSun = 0,     // 普通阳光
        BigSun = 1,        // 大阳光
        SmallSun = 2,      // 小阳光
        Bucket = 4,        // 铁桶
        Helmet = 6,        // 橄榄球帽
        Jackbox = 7,       // 蹦蹦跳杆
        Pickaxe = 8,       // 镐子
        LittleSun = 13,    // 小阳光
        SilverCoin = 34,   // 银币
        GoldCoin = 35,     // 金币
        DiamondCoin = 36,  // 钻石金币
        Bean = 37,         // 豆子
        SmallSilverCoin = 38, // 小银币
        SmallGoldCoin = 39,   // 小金币
        Machine = 41,      // 机器
        Portal = 42,       // 传送门
        BlueSun = 53,      // 蓝阳光
        SmallBlueSun = 54, // 小蓝阳光
    }


    /// 桶类型枚举 - 用于物品融合
    /// 完整的桶类型ID定义（从反编译文件中提取）

    public enum BucketType
    {
        Bucket = 0,        // 铁桶
        Helmet = 1,        // 橄榄球帽
        Jackbox = 2,       // 蹦蹦跳杆
        Pickaxe = 3,       // 镐子
        Machine = 4,       // 机器
        SuperMachine = 5,  // 超级机器
        Jumper = 6,        // 跳跃者
        Ladder = 7,        // 梯子
        IronHead = 8,      // 铁头
        RedIronHead = 9,   // 红铁头
        Door = 10,         // 门
        PortalHeart = 11,  // 传送门核心
    }


    /// 植物类型枚举 - 基础植物类型
    /// 注意：完整版本包含1300+个植物类型，这里只列出基础类型

    public enum PlantType
    {
        // 基础植物
        Nothing = -1,      // 无
        Peashooter = 0,    // 豌豆射手
        SunFlower = 1,     // 向日葵
        CherryBomb = 2,    // 樱桃炸弹
        WallNut = 3,       // 坚果墙
        PotatoMine = 4,    // 土豆地雷
        Chomper = 5,       // 大嘴花
        SmallPuff = 6,     // 小喷菇
        FumeShroom = 7,    // 大喷菇
        HypnoShroom = 8,   // 魅惑菇
        ScaredyShroom = 9, // 胆小菇
        IceShroom = 10,    // 寒冰菇
        DoomShroom = 11,   // 毁灭菇
        LilyPad = 12,      // 荷叶
        Squash = 13,       // 倭瓜
        ThreePeater = 14,  // 三线射手
        Tanglekelp = 15,   // 缠绕水草
        Jalapeno = 16,     // 火爆辣椒
        Caltrop = 17,      // 地刺
        TorchWood = 18,    // 火炬树桩
        SeaShroom = 19,    // 海蘑菇
        Plantern = 20,     // 路灯花
        Cactus = 21,       // 仙人掌
        Blover = 22,       // 三叶草
        StarFruit = 23,    // 杨桃
        Pumpkin = 24,      // 南瓜头
        Magnetshroom = 25, // 磁力菇
        Cabbagepult = 26,  // 卷心菜投手
        Pot = 27,          // 花盆
        Cornpult = 28,     // 玉米投手
        Garlic = 29,       // 大蒜
        Umbrellaleaf = 30, // 叶子保护伞
        Marigold = 31,     // 金盏花
        Melonpult = 32,    // 西瓜投手
        
        // 自定义植物ID（从代码中收集）
        SuperDiamondNut = 161,
        BigDiamondNut = 162,
        BigIronGatlingPea = 1680,
        SuperIronGatling = 163,
        IceDoomScaredyShroom = 304,
        
        // 注意：完整版本包含1300+个植物类型，包括各种融合植物
    }


    /// 僵尸类型枚举 - 基础僵尸类型
    /// 注意：完整版本包含380+个僵尸类型，这里只列出基础类型

    public enum ZombieType
    {
        // 基础僵尸
        Nothing = -1,      // 无
        NormalZombie = 0,  // 普通僵尸
        FlagZombie = 1,    // 旗帜僵尸
        ConeZombie = 2,    // 路障僵尸
        PolevaulterZombie = 3, // 撑杆僵尸
        BucketZombie = 4,  // 铁桶僵尸
        PaperZombie = 5,   // 报纸僵尸
        DancePolZombie = 6, // 舞王僵尸
        DancePolZombie2 = 7, // 舞王僵尸2
        DoorZombie = 8,    // 门板僵尸
        FootballZombie = 9, // 橄榄球僵尸
        JacksonZombie = 10, // 迈克尔·杰克逊僵尸
        ZombieDuck = 11,   // 鸭子僵尸
        ConeZombieDuck = 12, // 路障鸭子僵尸
        BucketZombieDuck = 13, // 铁桶鸭子僵尸
        SubmarineZombie = 14, // 潜水僵尸
        ElitePaperZombie = 15, // 精英报纸僵尸
        DriverZombie = 16, // 司机僵尸
        SnorkleZombie = 17, // 潜水镜僵尸
        SuperDriver = 18,  // 超级司机
        Dolphinrider = 19, // 海豚骑士僵尸
        DrownZombie = 20,  // 溺水僵尸
        DollDiamond = 21,  // 钻石娃娃
        DollGold = 22,     // 黄金娃娃
        DollSilver = 23,   // 白银娃娃
        JackboxZombie = 24, // 蹦蹦跳僵尸
        BalloonZombie = 25, // 气球僵尸
        KirovZombie = 26,  // 基洛夫僵尸
        SnowDolphinrider = 27, // 雪海豚骑士
        MinerZombie = 28,  // 矿工僵尸
        IronBalloonZombie = 29, // 铁气球僵尸
        SuperJackboxZombie = 30, // 超级蹦蹦跳僵尸
        CatapultZombie = 31, // 投石车僵尸
        PogoZombie = 32,   // 跳跳僵尸
        LadderZombie = 33, // 梯子僵尸
        SuperPogoZombie = 34, // 超级跳跳僵尸
        Gargantuar = 35,   // 巨人僵尸
        RedGargantuar = 36, // 红眼巨人
        ImpZombie = 37,    // 小鬼僵尸
        IronGargantuar = 38, // 铁巨人
        IronRedGargantuar = 39, // 铁红眼巨人
        MachineNutZombie = 40, // 机器坚果僵尸
        SilverZombie = 41, // 银僵尸
        GoldZombie = 42,   // 金僵尸
        SuperGargantuar = 43, // 超级巨人
        ZombieBoss = 44,   // 僵尸Boss
        BungiZombie = 45,  // 蹦极僵尸
        ZombieBoss2 = 46,  // 僵尸Boss2
        
        // 注意：完整版本包含380+个僵尸类型，包括各种变体和组合
    }


    /// 子弹类型枚举 - 从README.md收集

    public enum BulletType
    {
        Bullet_pea = 0,
        Bullet_cherry = 1,
        Bullet_nut = 2,
        Bullet_superCherry = 3,
        Bullet_zombieBlock = 4,
        Bullet_zombieBlock2 = 5,
        Bullet_zombieBlock3 = 6,
        Bullet_potato = 7,
        Bullet_smallSun = 8,
        Bullet_puff = 9,
        Bullet_puffPea = 10,
        Bullet_ironPea = 11,
        Bullet_threeSpike = 12,
        Bullet_puffRandomColor = 13,
        Bullet_puffLove = 14,
        Bullet_snowPea = 15,
        Bullet_snowPuffPea = 16,
        Bullet_iceSpark = 17,
        Bullet_smallIceSpark = 18,
        Bullet_magicTrack = 20,
        Bullet_snowPuff = 21,
        Bullet_blackPuff = 22,
        Bullet_doom = 23,
        Bullet_iceDoom = 24,
        Bullet_firePea_yellow = 25,
        Bullet_firePea_orange = 26,
        Bullet_firePea_red = 27,
        Bullet_squash = 28,
        Bullet_kelp = 29,
        Bullet_fireKelp = 30,
        Bullet_squashKelp = 32,
        Bullet_normalTrack = 33,
        Bullet_iceTrack = 34,
        Bullet_fireTrack = 35,
        Bullet_cherrySquash = 36,
        Bullet_cactus = 37,
        Bullet_lanternCactus_glow = 38,
        Bullet_star = 39,
        Bullet_lanternStar = 40,
        Bullet_seaStar = 41,
        Bullet_cactusStar = 42,
        Bullet_jackboxStar = 43,
        Bullet_pickaxeStar = 44,
        Bullet_magnetStar = 45,
        Bullet_ironStar = 46,
        Bullet_magnetCactus = 47,
        Bullet_superStar = 48,
        Bullet_ultimateStar = 49,
        Bullet_firePea_small = 50,
    }


    /// 网格物品类型枚举 - 完整的网格物品ID定义（从反编译文件中提取）

    public enum GridItemType
    {
        CraterDay = 0,     // 白天弹坑
        CraterNight = 1,   // 夜晚弹坑
        Ladder = 3,        // 梯子
        ScaryPot = 4,      // 恐怖罐子
        ScaryPot_plant = 5, // 植物恐怖罐子
        ScaryPot_zombie = 6, // 僵尸恐怖罐子
        Grave = 7,         // 坟墓
        IceBlock = 8,      // 冰块
    }


    /// 工具类型枚举 - 完整的工具类型ID定义（从反编译文件中提取）

    public enum ToolType
    {
        WaterCan = 0,      // 水壶
        Fertilize = 1,     // 肥料
        BugSpray = 2,      // 杀虫剂
        Phonograph = 3,    // 留声机
        Glove = 4,         // 手套
    }


    /// 僵尸状态枚举 - 完整的僵尸状态ID定义（从反编译文件中提取）

    public enum ZombieStatus
    {
        Default = 0,       // 默认状态
        Dying = 1,         // 死亡中
        Pol_run = 2,       // 撑杆跑
        Pol_jump = 3,      // 撑杆跳
        Paper_lookPaper = 4, // 报纸看报纸
        Paper_losePaper = 5, // 报纸丢失
        Paper_angry = 6,   // 报纸愤怒
        Snokle_inWater = 7, // 潜水镜在水中
        Dolphinrider_fast = 8, // 海豚骑士快速
        Dolphinrider_jump = 9, // 海豚骑士跳跃
        Flying = 10,       // 飞行
        Jackbox_surprise = 11, // 蹦蹦跳惊喜
        Miner_digging = 12, // 矿工挖掘
        Miner_rising = 13, // 矿工上升
        Polo_jump = 14,    // 撑杆跳
        Gargantuar_withImp = 15, // 巨人带小鬼
        Imp_fly = 16,      // 小鬼飞行
        Imp_Land = 17,     // 小鬼着陆
        WithLadder = 18,   // 带梯子
    }


    /// 移动方式枚举 - 用于CreateItem.SetCoin的第四个参数

    public enum MoveType
    {
        Default = 0,       // 默认移动
        Custom = 1,        // 自定义移动
        // 更多移动类型需要进一步收集
    }
}


/// 使用示例和说明

public class ItemUsageExamples
{

    /// CreateItem.SetCoin使用示例

    public void SetCoinExamples()
    {
        // 创建银币
        CreateItem.Instance.SetCoin(0, 0, (int)ItemType.SilverCoin, 0, Vector3.zero);
        
        // 创建金币
        CreateItem.Instance.SetCoin(0, 0, (int)ItemType.GoldCoin, 0, Vector3.zero);
        
        // 创建阳光
        CreateItem.Instance.SetCoin(0, 0, (int)ItemType.Sun, 0, Vector3.zero);
        
        // 创建钻石金币
        CreateItem.Instance.SetCoin(0, 0, (int)ItemType.DiamondCoin, 0, Vector3.zero);
    }


    /// 物品融合示例

    public void ItemFusionExamples()
    {
        // 注册铁桶与植物的融合
        CustomCore.RegisterCustomUseItemOnPlantEvent(
            PlantType.BigGatling, 
            BucketType.Bucket, 
            PlantType.BigIronGatlingPea
        );
    }
}
