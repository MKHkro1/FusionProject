using HarmonyLib;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.Injection;
using Unity.VisualScripting;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace SuperDoomSqualour.BepInEx;

[HarmonyPatch(typeof(Bullet_lourCactus))]
public static class Bullet_lourCactusPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("HitZombie")]
    public static bool PreHitZombie(Bullet_lourCactus __instance, ref Zombie zombie)
    {
        if (__instance.theBulletType is not (BulletType)904 || zombie.gameObject.IsDestroyed() ||
            zombie.isMindControlled) return true;
        var beforeDying = zombie.beforeDying;
        var wasDead = zombie.theHealth <= 0f;
        var mindCtrlled = false;
        if (Random.Range(0, 10) is 0 || Lawnf.TravelAdvanced(SuperDoomSqualour.Buff))
        {
            zombie.TakeDamage(DmgType.Normal, __instance.Damage, true);
            zombie.SetMindControl();
            zombie.isDoom = true;
            zombie.doomWithPit = false;
            zombie.SetColor(Zombie.ZombieColor.Doom);
            if (!zombie.isMindControlled) zombie.Die();
            mindCtrlled = true;
        }
        else
        {
            zombie.TakeDamage(DmgType.NormalAll, __instance.Damage, true);
            zombie.isDoom = true;
            zombie.doomWithPit = false;
            zombie.SetColor(Zombie.ZombieColor.Doom);
        }

        // 播放声音
        __instance.PlaySound(zombie);
        try
        {
            // 生成僚机
            if (__instance.lour is not null && __instance.lour.gameObject is not null &&
                !__instance.lour.gameObject.IsDestroyed()
                && __instance.lour.gameObject.TryGetComponent<SuperHypnoDoomCattailLour>(out var lour)
                && ((!beforeDying && zombie.beforeDying) ||
                    (zombie.theHealth <= 0f && !wasDead && !zombie.beforeDying) || mindCtrlled))
                lour.Supply();
        }
        catch
        {
        }

        return false;
    }
}

[HarmonyPatch(typeof(CattailLour))]
public static class CattailLourPatch
{
    [HarmonyPatch(nameof(CattailLour.Shoot1))]
    [HarmonyPrefix]
    public static bool PreShoot1(CattailLour __instance, ref Bullet __result) //重定向到新植物的发射方法
    {
        if (__instance.thePlantType is not (PlantType)178 ||
            !__instance.TryGetComponent<SuperHypnoDoomCattailLour>(out var lour)) return true;
        __result = lour.Shoot();
        return false;
    }
}

[HarmonyPatch(typeof(LourFly))]
public static class LourFlyPatch
{
    [HarmonyPatch("ShootUpdate")]
    [HarmonyPrefix]
    public static bool PreShootUpdate(LourFly __instance)
    {
        if (!__instance.TryGetComponent<SuperHypnoDoomCattailLour_fly>(out _)) return true;

        __instance.timer -= Time.deltaTime;
        if (__instance.timer < 0f && __instance.lour is not null)
        {
            __instance.timer = Random.Range(0.95f, 1.05f) * 0.1f;

            if (__instance.shootPos is not null)
            {
                var spawnPosition = __instance.shootPos.position;
                var damageMultiplier = Lawnf.TravelAdvanced(34) ? 3 : 1;

                // 创建第一个子弹
                var bullet1 = CreateBullet.Instance.SetBullet(spawnPosition.x, spawnPosition.y, __instance.theRow,
                    (BulletType)904, 10);

                if (bullet1 != null)
                {
                    bullet1.GetComponent<Bullet_lourCactus>().lour = __instance.lour;
                    bullet1.GetComponent<Rigidbody2D>().velocity = new Vector2(-1f, 1f);
                    bullet1.Damage = __instance.lour.attackDamage * damageMultiplier;
                }

                // 创建第二个子弹
                var bullet2 = CreateBullet.Instance.SetBullet(spawnPosition.x, spawnPosition.y, __instance.theRow,
                    (BulletType)904, 10);

                if (bullet2 != null)
                {
                    bullet2.GetComponent<Bullet_lourCactus>().lour = __instance.lour;
                    bullet2.GetComponent<Rigidbody2D>().velocity = new Vector2(-1f, -1f);
                    bullet2.Damage = __instance.lour.attackDamage * damageMultiplier;
                }

                // 创建第三个子弹
                var bullet3 = CreateBullet.Instance.SetBullet(spawnPosition.x, spawnPosition.y, __instance.theRow,
                    (BulletType)904, 10);

                if (bullet3 != null)
                {
                    bullet3.GetComponent<Bullet_lourCactus>().lour = __instance.lour;
                    bullet3.GetComponent<Rigidbody2D>().velocity = new Vector2(1.4f, 0);
                    bullet3.Damage = __instance.lour.attackDamage * damageMultiplier;
                }

                __instance.shootCount += 3;
            }
        }

        if (__instance.shootCount >= 60) Object.Destroy(__instance.gameObject);
        return false;
    }
}

[HarmonyPatch(typeof(Plant))]
public static class PlantPatch
{
    [HarmonyPatch(nameof(Plant.Die))]
    [HarmonyPatch(nameof(Plant.Crashed))]
    [HarmonyPrefix]
    public static void PreDie(CattailGirl __instance)
    {
        if (__instance.thePlantType is not (PlantType)178) return;
        for (var i = Board.Instance.zombieArray.Count - 1; i >= 0; i--)
        {
            var z = Board.Instance.zombieArray[i];
            if (z is not null && !z.isMindControlled)
            {
                if (Lawnf.TravelAdvanced(SuperDoomSqualour.Buff)) //难度开了
                {
                    if (z is not null && !z.IsDestroyed())
                    {
                        //生成爆炸效果
                        Board.Instance.SetDoom(Mouse.Instance.GetColumnFromX(z.GameObject().transform.position.x),
                            z.theZombieRow, false, default, default, 1);
                        //直接抹除
                        z.Die(2);
                    }
                }
                else
                {
                    if (z is not null && !z.IsDestroyed())
                        z.TakeDamage(DmgType.Explode, 3600); //3600伤害
                    if (z is not null && !z.IsDestroyed()) //附加死亡爆炸效果
                    {
                        z.isDoom = true;
                        z.doomWithPit = false;
                        z.SetColor(Zombie.ZombieColor.Doom);
                    }
                }
            }
        }

        Board.Instance.SetDoom(__instance.thePlantColumn, __instance.thePlantRow, false, damage: 0);
    }
}

public class SuperHypnoDoomCattailLour : MonoBehaviour
{
    public SuperHypnoDoomCattailLour() : base(ClassInjector.DerivedConstructorPointer<SuperHypnoDoomCattailLour>())
    {
        ClassInjector.DerivedConstructorBody(this);
    }

    public SuperHypnoDoomCattailLour(IntPtr i) : base(i)
    {
    }

    public static GameObject? flyPrefab { get; set; }
    public Transform? flyPos { get; set; }
    public Stack<SuperHypnoDoomCattailLour_fly> LourFlies { [HideFromIl2Cpp] get; [HideFromIl2Cpp] set; } = [];
    public CattailLour plant => gameObject.GetComponent<CattailLour>();

    public void Start()
    {
        flyPos = transform.FindChild("FlyPos");
    }

    public Bullet Shoot()
    {
        // 获取发射点位置
        var spawnPosition = transform.Find("Shoot").position;

        // 通过子弹管理器创建子弹
        var newBullet =
            CreateBullet.Instance.SetBullet(spawnPosition.x, spawnPosition.y, plant.thePlantRow, (BulletType)904, 6);

        // 词条伤害处理
        var finalDamage = Lawnf.TravelAdvanced(35) ? 5 * plant.attackDamage : plant.attackDamage;
        newBullet.Damage = finalDamage;

        // 关联子弹和发射器
        if (newBullet is Bullet_lourCactus cactusBullet)
        {
            cactusBullet.lour = plant;
            cactusBullet.targetZombie = plant.targetZombie; // 自定义初始化方法
        }

        // 播放随机音效
        GameAPP.PlaySound(Random.Range(3, 5));

        return newBullet;
    }

    public void Supply()
    {
        if (LourFlies.Count > 6) return;
        // 实例化飞行物体并设置初始属性
        var spawnPosition = flyPos!.position;
        spawnPosition.z = 0f; // 确保Z轴坐标为0（2D场景）
        spawnPosition.y = 6f;
        // 实例化预制体并设置父对象
        var flyInstance = Instantiate(
            flyPrefab!,
            spawnPosition,
            Quaternion.identity,
            plant.board.transform
        );

        // 获取LourFly组件并初始化
        var lourFly = flyInstance.GetComponent<LourFly>();
        if (lourFly is not null)
        {
            lourFly.theRow = plant.thePlantRow;
            lourFly.targetPosition = flyPos.position;
            lourFly.lour = plant;
            lourFly.shootPos = lourFly.transform.Find("Pos");
            lourFly.arrived = false;
        }

        LourFlies.Push(flyInstance.GetComponent<SuperHypnoDoomCattailLour_fly>());

        // 设置渲染器排序图层
        var renderer = flyInstance.GetComponent<Renderer>();
        if (renderer is not null) renderer.sortingLayerName = string.Format("bullet{0}", plant.thePlantRow);
    }
}

public class SuperHypnoDoomCattailLour_fly : MonoBehaviour
{
    public SuperHypnoDoomCattailLour_fly() : base(
        ClassInjector.DerivedConstructorPointer<SuperHypnoDoomCattailLour_fly>())
    {
        ClassInjector.DerivedConstructorBody(this);
    }

    public SuperHypnoDoomCattailLour_fly(IntPtr i) : base(i)
    {
    }

    public void OnDestroy()
    {
        if (gameObject is not null && !gameObject.IsDestroyed()
                                   && gameObject.TryGetComponent<LourFly>(out var fly)
                                   && fly is not null && fly.lour is not null && !fly.lour.IsDestroyed()
                                   && fly.lour.TryGetComponent<SuperHypnoDoomCattailLour>(out var shdcl))
            shdcl.LourFlies.Pop();
    }
}