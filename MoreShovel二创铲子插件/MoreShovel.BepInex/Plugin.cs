using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace MoreShovel.BepInex;

[BepInPlugin("tyyh.moreshovel","MoreShovel","1.1.0")]
public class Plugin : BasePlugin
{
    public override void Load()
    {
        // Plugin startup logic
        Debug.Log("[更多铲子] 插件已加载");
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        ClassInjector.RegisterTypeInIl2Cpp<Core>();
    }
}

[HarmonyPatch(typeof(NoticeMenu), nameof(NoticeMenu.Awake))]
public static class P
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        GameAPP.Instance.AddComponent<Core>();
    }
}

public class Core : MonoBehaviour
{
    public enum ShovelStatus
    {
        Normal,
        Iron,
        Gold,
        Diamond,
        Ice,
        Sun,
        Hypno,
        Seed,
        Pumpkin,
        Hamburger,
        Cherry,
        Star,
        Jala
    }

    public static readonly int ShovelStatusNum = 13;
    public static ShovelStatus status = ShovelStatus.Normal;
    private static readonly Dictionary<ShovelStatus, Sprite> ShovelStatus2Sprite = new()
    {
        { ShovelStatus.Normal, LoadSpriteFromFile("./BepInEx/plugins/MoreShovels/normalshovel.png") },
        { ShovelStatus.Iron, LoadSpriteFromFile("./BepInEx/plugins/MoreShovels/ironshovel.png") },
        { ShovelStatus.Gold, LoadSpriteFromFile("./BepInEx/plugins/MoreShovels/goldshovel.png") },
        { ShovelStatus.Diamond, LoadSpriteFromFile("./BepInEx/plugins/MoreShovels/diamondshovel.png") },
        { ShovelStatus.Ice, LoadSpriteFromFile("./BepInEx/plugins/MoreShovels/iceshovel.png") },
        { ShovelStatus.Sun, LoadSpriteFromFile("./BepInEx/plugins/MoreShovels/sunshovel.png") },
        { ShovelStatus.Hypno, LoadSpriteFromFile("./BepInEx/plugins/MoreShovels/hypnoshovel.png") },
        { ShovelStatus.Seed, LoadSpriteFromFile("./BepInEx/plugins/MoreShovels/seedshovel.png") },
        { ShovelStatus.Pumpkin, LoadSpriteFromFile("./BepInEx/plugins/MoreShovels/pumpkinshovel.png") } ,
        { ShovelStatus.Hamburger, LoadSpriteFromFile("./BepInEx/plugins/MoreShovels/hamburgershovel.png") },
        { ShovelStatus.Cherry, LoadSpriteFromFile("./BepInEx/plugins/MoreShovels/cherryshovel.png") },
        { ShovelStatus.Star, LoadSpriteFromFile("./BepInEx/plugins/MoreShovels/starshovel.png") },
        { ShovelStatus.Jala, LoadSpriteFromFile("./BepInEx/plugins/MoreShovels/jalashovel.png") }
    };
    private static readonly Dictionary<ShovelStatus, string> ShovelStatus2string = new()
    {
        { ShovelStatus.Normal, "小石铲(Ctrl+Z切换)" },
        { ShovelStatus.Iron, "铁铲子"},
        { ShovelStatus.Gold, "金铲子" },
        { ShovelStatus.Diamond, "钻石铲" },
        { ShovelStatus.Ice, "冰铲子" },
        { ShovelStatus.Sun, "阳光铲" },
        { ShovelStatus.Hypno, "魅惑铲" },
        { ShovelStatus.Seed, "卡槽铲" },
        { ShovelStatus.Pumpkin, "南瓜铲" } ,
        { ShovelStatus.Hamburger, "汉堡铲" },
        { ShovelStatus.Cherry, "樱桃铲" },
        { ShovelStatus.Star, "星星铲" },
        { ShovelStatus.Jala, "辣椒铲" }
    };

    public void Update()
    {
        if(!InGame())return;

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z))
            status = (ShovelStatus)(((int)status+1)%ShovelStatusNum);
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Z))
            status = (ShovelStatus)(((int)status-1+ShovelStatusNum)%ShovelStatusNum);
        
        GameObject shovel;
        Transform shovelbank;
        if (InGameUI.Instance != null)
        {
            shovelbank = InGameUI.Instance.transform.FindChild("ShovelBank");
            if (Mouse.Instance.theItemOnMouse == null)
            {
                shovel = shovelbank.FindChild("Shovel").gameObject;
            }
            else if (Mouse.Instance.theItemOnMouse.name == "Shovel")
            {
                shovel = Mouse.Instance.theItemOnMouse;
            }
            else return;
        }
        else if(InGameUI_IZ.Instance != null)
        {
            shovelbank = InGameUI_IZ.Instance.transform.FindChild("ShovelBank");
            if (Mouse.Instance.theItemOnMouse == null)
            {
                shovel = shovelbank.FindChild("Shovel").gameObject;
            }
            else if (Mouse.Instance.theItemOnMouse.name == "Shovel")
            {
                shovel = Mouse.Instance.theItemOnMouse;
            }
            else return;
        }else return;



        var image = shovel.GetComponent<Image>();
        image.sprite = ShovelStatus2Sprite[status];

        for (int i = 0; i < shovelbank.childCount; i++)
        {
            if (shovelbank.GetChild(i).TryGetComponent<TextMeshProUGUI>(out var component))
            {
                component.text = "快捷键: 1\n" + ShovelStatus2string[status];
                component.transform.localPosition = new Vector3(0, -52.2563f, 0);
            }
        }
        
    }

    private static bool InGame()
    {
        return Board.Instance is not null &&
               GameAPP.theGameStatus is not GameStatus.OpenOptions or GameStatus.OutGame or GameStatus.Almanac;
    }

    private static Sprite? LoadSpriteFromFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            // 从本地路径读取文件字节
            byte[] fileData = File.ReadAllBytes(filePath);

            // 创建一个新的Texture2D
            Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);

            // 加载图片数据到纹理中
            if (ImageConversion.LoadImage(texture, fileData))
            {
                // 使用图片纹理创建一个Sprite
                return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            }
        }

        return null;
    }
}


[HarmonyPatch(typeof(Plant), nameof(Plant.Die))]
public class PlantDiePatch
{
    [HarmonyPrefix]
    public static bool Prefix(Plant __instance,Plant.DieReason reason)
    {
        if (reason is Plant.DieReason.ByShovel)
        {
            //MelonLogger.Msg($"{__instance.thePlantType}");
            int cost = PlantDataLoader.plantDatas[__instance.thePlantType].field_Public_Int32_1;
            cost = Math.Min(1000, cost);
            int count;
            Bullet? bullet;
            switch (Core.status)
            {
                case Core.ShovelStatus.Normal:
                    break;
                case Core.ShovelStatus.Iron:
                    List<int> IronItems = new List<int>() { 4, 6, 7, 8, 41 ,712};
                    count = 1;
                    if (cost >= 175) count = 2;
                    if (cost >= 500) count = 3;
                    for (int i = 0; i < count; i++)
                    {
                        int ironItem = IronItems[Random.RandomRangeInt(0,IronItems.Count)];
                        if (ironItem == 712)
                        {
                            var gameObject = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Items/PortalHeart"));
                            gameObject.transform.SetParent(GameAPP.board.transform);
                            gameObject.transform.position = __instance.axis.transform.position;
                        }
                        else
                            CreateItem.Instance.SetCoin(0, 0, ironItem, 0, __instance.axis.transform.position);
                    }
                    break;
                case Core.ShovelStatus.Gold:
                    int cost4 = cost * 4;
                    for (int i = 0; i < cost4 / 300; i++)
                    {
                        CreateItem.Instance.SetCoin(0, 0, 35, 0, __instance.axis.transform.position);
                    }
                    for (int i = 0; i < cost4 % 300 / 100; i++)
                    {
                        CreateItem.Instance.SetCoin(0, 0, 34, 0, __instance.axis.transform.position);
                    }
                    for (int i = 0; i < cost4 % 100 / 25; i++)
                    {
                        CreateItem.Instance.SetCoin(0, 0, 38, 0, __instance.axis.transform.position);
                    }
                    break;
                case Core.ShovelStatus.Diamond:
                    bool g = true;
                    for (int i = 0; i < cost/100+1; i++)
                    {
                        bullet = CreateBullet.Instance.SetBullet(
                            __instance.axis.transform.position.x-0.2f*i,
                            __instance.axis.transform.position.y+0.6f,
                            __instance.thePlantRow,
                            g?BulletType.Bullet_silverCoin:BulletType.Bullet_goldCoin,
                            BulletMoveWay.MoveRight);
                        bullet.Damage = g?20:30;
                        g = !g;
                    }
                    break;
                case Core.ShovelStatus.Ice:
                    for (int i = 0; i < cost/100+1; i++)
                    {
                        bullet = CreateBullet.Instance.SetBullet(
                            __instance.axis.transform.position.x-0.2f*i,
                            __instance.axis.transform.position.y+0.6f,
                            __instance.thePlantRow,
                            BulletType.Bullet_snowPea,
                            BulletMoveWay.MoveRight);
                        bullet.Damage = 25;
                    }
                    break;
                case Core.ShovelStatus.Sun:
                    int cost2 = (int)(cost * 0.2);
                    for (int i = 0; i < cost2 / 50; i++)
                    {
                        CreateItem.Instance.SetCoin(0, 0, 1, 0, __instance.axis.transform.position);
                    }
                    for (int i = 0; i < cost2 % 50 / 15; i++)
                    {
                        CreateItem.Instance.SetCoin(0, 0, 2, 0, __instance.axis.transform.position);
                    }
                    for (int i = 0; i < cost2 % 15 / 5; i++)
                    {
                        CreateItem.Instance.SetCoin(0, 0, 13, 0, __instance.axis.transform.position);
                    }
                    break;
                case Core.ShovelStatus.Hypno:
                    if(cost <= 100)
                        CreateZombie.Instance.SetZombieWithMindControl(__instance.thePlantRow, ZombieType.ImpZombie,
                            __instance.axis.transform.position.x);
                    else if(cost<=175)
                        CreateZombie.Instance.SetZombieWithMindControl(__instance.thePlantRow, ZombieType.NormalZombie,
                            __instance.axis.transform.position.x);
                    else if(cost <= 300)
                        CreateZombie.Instance.SetZombieWithMindControl(__instance.thePlantRow, ZombieType.PeaShooterZombie,
                            __instance.axis.transform.position.x);
                    else
                        CreateZombie.Instance.SetZombieWithMindControl(__instance.thePlantRow, ZombieType.IronPeaZombie,
                            __instance.axis.transform.position.x);
                    break;
                case Core.ShovelStatus.Seed:
                    int lesscd = 5;
                    if (cost >= 150) lesscd = 8;
                    if (cost >= 300) lesscd = 10;
                    if (cost >= 400) lesscd = 20;
                    
                    count = 0;
                    if (InGameUI.Instance != null)
                    {
                        foreach (var card in InGameUI.Instance.cardOnBank)
                        {
                            if(card == null)continue;
                            if(card.CD < card.fullCD)
                                count++;
                            if (card.theSeedType == (int)__instance.thePlantType)
                            {
                                card.CD = Math.Min(card.CD + lesscd, card.fullCD);
                                goto final;
                            }
                        }

                        if(count==0)goto final;
                    
                        foreach (var card in InGameUI.Instance.cardOnBank)
                        {
                            if(card == null)continue;
                            if(card.CD < card.fullCD)
                                card.CD = Math.Min(card.CD + lesscd/count, card.fullCD);
                        }
                    }
                    else
                    {
                        if (InGameUI_IZ.Instance != null)
                        {
                            foreach (var card in InGameUI_IZ.Instance.cardOnBank)
                            {
                                if(card == null)continue;
                                if(card.CD < card.fullCD)
                                    count++;
                                if (card.theSeedType == (int)__instance.thePlantType)
                                {
                                    card.CD = Math.Min(card.CD + lesscd, card.fullCD);
                                    goto final;
                                }
                            }

                            if(count==0)goto final;
                    
                            foreach (var card in InGameUI_IZ.Instance.cardOnBank)
                            {
                                if(card == null)continue;
                                if(card.CD < card.fullCD)
                                    card.CD = Math.Min(card.CD + lesscd/count, card.fullCD);
                            }
                        }
                    }
                    
                    final:break;
                case Core.ShovelStatus.Pumpkin:
                    int health = __instance.thePlantHealth;
                    List<Plant> list = new List<Plant>();
                    List<Plant> list2 = new List<Plant>();
                    foreach (var plant in Board.Instance.plantArray)
                    {
                        if(plant == null)continue;
                        if(plant == __instance)continue;
                        if (plant.thePlantRow == __instance.thePlantRow &&
                            plant.thePlantColumn == __instance.thePlantColumn)
                        {
                            list.Add(plant);
                        }
                        else if (Math.Abs(plant.thePlantRow - __instance.thePlantRow) <= 1 &&
                                 Math.Abs(plant.thePlantColumn - __instance.thePlantColumn) <= 1)
                        {
                            list2.Add(plant);
                        }
                    }
                    if (list.Count == 0)
                        list = list2;
                    foreach (var plant in list)
                        plant.Recover(health/list.Count);
                    
                    break;
                case Core.ShovelStatus.Hamburger:
                    List<BulletType> bullets = new List<BulletType>()
                    {
                        BulletType.Bullet_pea,
                        BulletType.Bullet_firePea_red,
                        BulletType.Bullet_butter,
                        BulletType.Bullet_melon,
                        BulletType.Bullet_fireMelon
                    };
                    int upcount = 2;
                    if (cost >= 150) upcount = 3;
                    if (cost >= 275) upcount = 4;
                    if (cost >= 450) upcount = 5;

                    for (int i = 0; i < cost/100+1; i++)
                    {
                        bullet = CreateBullet.Instance.SetBullet(
                            __instance.axis.transform.position.x-0.2f*i,
                            __instance.axis.transform.position.y+0.6f,
                            __instance.thePlantRow,
                            bullets[Random.Range(0, upcount)],
                            BulletMoveWay.MoveRight);
                        bullet.Damage = upcount switch
                        {
                            2 => 15,
                            3 => 25,
                            4 => 35,
                            _ => 40
                        };
                    }
                    
                    break;
                case Core.ShovelStatus.Cherry:
                    int damage = 150;
                    if (cost >= 100) damage = 200;
                    if (cost >= 200) damage = 300;
                    if (cost >= 350) damage = 400;
                    if (cost >= 500) damage = 550;
                    if (cost >= 800) damage = 700;
                    var obj = CreateParticle.SetParticle(14,__instance.axis.transform.position,__instance.thePlantRow);
                    var component = obj.GetComponent<BombCherry>();
                    component.bombRow = __instance.thePlantRow;
                    component.bombType = CherryBombType.Bullet;
                    component.explodeDamage = damage;
                    GameAPP.PlaySound(40,0.2f);
                    break;
                case Core.ShovelStatus.Star:
                    for (int i = 0; i < cost/100+1; i++)
                    {
                        for (int j = 0; j < 5; j++)
                        {
                            //float angle = i * 72f;
                            //float radians = angle * Mathf.Deg2Rad;
                            //float offsetX = Mathf.Sin(radians);
                            //float offsetY = Mathf.Cos(radians);

                            bullet = CreateBullet.Instance.SetBullet(
                                __instance.axis.transform.position.x,
                                __instance.axis.transform.position.y + 0.6f, 
                                __instance.thePlantRow,
                                BulletType.Bullet_star,
                                BulletMoveWay.Free);
                            
                            bullet.transform.Rotate(0,0,j * 72f);
                            bullet.Damage = 25;
                            bullet.transform.position += bullet.transform.up*0.3f*i;
                        }
                    }

                    break;
                case Core.ShovelStatus.Jala:
                {
                    damage = 150;
                    if (cost >= 200) damage = 200;
                    if (cost >= 350) damage = 300;
                    if (cost >= 500) damage = 450;
                    if (cost >= 800) damage = 600;
                    Board.Instance.CreateFireLine(__instance.thePlantRow, damage);
                    break;
                }
                default:
                    break;
            }
        }

        return true;
    }
}