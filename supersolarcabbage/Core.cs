using System.Reflection;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using CustomizeLib.BepInEx;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace SuperSolarCabbageBepInEx
{
	[BepInPlugin("supersolarcabbage.bepinex", "SuperSolarCabbage", "1.0.0")]
	public class Core : BasePlugin
	{
		public const int PlantID = 2024;
		public const int BulletID = 280;

		public override void Load()
		{
			Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
			ClassInjector.RegisterTypeInIl2Cpp<SuperSolarCabbageBehaviour>();

			var ab = CustomCore.GetAssetBundle(Assembly.GetExecutingAssembly(), "supersolarcabbage");

			// 列出AssetBundle中的所有资源
			string[] assetNames = ab.GetAllAssetNames();
			Console.WriteLine($"AssetBundle包含 {assetNames.Length} 个资源:");
			foreach (string name in assetNames)
			{
				Console.WriteLine($"  - {name}");
			}

			// 使用正确的资源名称
			var prefab = ab.GetAsset<GameObject>("SuperSolarCabbagePrefab");
			var preview = ab.GetAsset<GameObject>("SuperSolarCabbagePreview");
			
			// 注册自定义子弹（从AssetBundle加载预制体）
			try
			{
				// 尝试加载子弹预制体
				GameObject bulletPrefab = ab.GetAsset<GameObject>("cabbage_ziddan");
				if (bulletPrefab != null)
				{
					// 注册自定义子弹
					CustomCore.RegisterCustomBullet<Bullet>((BulletType)BulletID, bulletPrefab);
				}
				else
				{
					Console.WriteLine($"无法加载子弹预制体 cabbage_zidan，将使用默认子弹外观，子弹ID仍为: {BulletID}");
				}
			}
			catch (System.Exception)
			{
				Console.WriteLine($"加载子弹预制体失败");
			}

			CustomCore.RegisterCustomPlant<SolarCabbage, SuperSolarCabbageBehaviour>(
				PlantID,
				prefab,
				preview,
				new System.Collections.Generic.List<(int, int)> { (934, 16), (16, 934) }, // 究极太阳神卷心菜 + 火爆辣椒（正反无顺序）
				attackInterval: 0f,
				produceInterval: 0f,
				attackDamage: 60,
				maxHealth: 300,
				cd: 7.5f,
				sun: 550
			);

			CustomCore.AddPlantAlmanacStrings(
				PlantID,
				$"究极普罗米修斯太阳神卷心菜({PlantID})",
				"融合究极太阳神卷心菜与火爆辣椒的强力植物。\n\n" +
				"<color=#3D1400>作者:</color><color=red>梧萱梦汐X、寒冰豌豆黄bdh</color>\n" +
				"<color=#3D1400>伤害:</color><color=red>（光核）当前阳光数/50×5颗/2秒\n（阳光卷心菜）300×5颗/2秒</color>\n" +
				"<color=#3D1400>特点:</color><color=red>投掷攻击，子弹命中僵尸施加红温效果，超级技能发射1颗20000伤害的子弹</color>\n" +
				"<color=#3D1400>融合配方:</color><color=red>究极太阳神卷心菜+火爆辣椒</color>\n" +
				"<color=#3D1400>升级消耗:</color><color=red>1000金钱，回复满血并延长太阳存在时间</color>\n" +
				"<color=#3D1400>超级技能:</color><color=red>消耗1000金钱，回复满血，延长太阳存在时间，为全场僵尸施加红温效果，发射1颗20000伤害的超级子弹</color>\n" +
				"<color=#3D1400>其它特点:</color><color=red>特点：1.拥有阳光卷心菜的特点\n太阳：持续30秒。每轮存在时间内，召唤太阳会增加其30秒特续时间\n每0.5秒对全场僵尸造成(60+40×太阳神数量)的伤害，阳光高于15000时，消耗200阳光使本次伤害×3\n使阳光自然掉落变为0.75秒一次</color>\n" +
				"<color=#3D1400>词条（太阳神同款）:\n</color><color=red>词条1：金光闪闪：太阳神的子弹会消耗超过15000阳光部分0.5%阳光，使该子弹增加(20×消耗阳光)的伤害；亚种月亮神的子弹的光照等级增伤×3\n词条2：人造太阳：太阳伤害×3月亮回血×3\n连携词条：究极杨桃大帝（及其亚种)与究极太阳神卷心菜的数量均不小于10时：太阳持续时间无限且伤害×5,固定每3秒召唤太阳神流星，伤害为1800×(1+大帝数量)×(1+0.5×太阳神数量)，分裂出180个伤害400的子弹，并掉落1250阳光</color>\n\n" +
				"<color=#3D1400>超级太阳卷心菜是究极太阳神卷心菜与火爆辣椒的完美融合体，继承了太阳的能量与火焰的爆发力。它的攻击方式独特，通过投掷太阳能量球对敌人造成伤害，每次命中都会让僵尸陷入红温状态。当使用超级技能时，它会释放太阳风暴，为全场僵尸施加红温效果，然后凝聚所有太阳能量，发射一颗威力巨大的超级子弹，足以一击秒杀大部分敌人。</color>\n"
			);

			CustomCore.RegisterSuperSkill(PlantID, (p) => 1000, (p) =>
			{
				p.Recover(p.thePlantMaxHealth);
				if (Solar.Instance == null)
				{
					GameObject solarPrefab = GameAPP.itemPrefab[46];
					var solarObj = UnityEngine.Object.Instantiate(solarPrefab, new Vector3(-25f, 35f, 0f), Quaternion.identity, Board.Instance.transform);
					Solar solar = solarObj.GetComponent<Solar>();
					solar?.SetDamage();
					GameAPP.PlaySound(95, 0.5f, 1f);
					Solar.Instance = solar;
					solar.deathTime = 30f;
				}
				else
				{
					Solar.Instance.deathTime += 15f;
				}
				
				// 为全场僵尸赋予红温效果
				if (Board.Instance?.zombieArray != null)
				{
					int affectedZombies = 0;
					foreach (Zombie zombie in Board.Instance.zombieArray)
					{
						if (zombie != null)
						{
							zombie.SetJalaed();
							affectedZombies++;
						}
					}
				}
				
				// 发射1颗高伤害子弹
				SuperSolarCabbageBehaviour behaviour = p.GetComponent<SuperSolarCabbageBehaviour>();
				if (behaviour != null)
				{
					behaviour.SuperSkillShoot();
				}
			});

			// 不使用 Harmony 打补丁，改为在挂载脚本中实现与动画事件同名的方法（ SuperSolarCabbageBehaviour）
		}
	}

	public class SuperSolarCabbageBehaviour : MonoBehaviour
	{
		public SuperSolarCabbageBehaviour() : base(ClassInjector.DerivedConstructorPointer<SuperSolarCabbageBehaviour>()) => ClassInjector.DerivedConstructorBody(this);
		public SuperSolarCabbageBehaviour(System.IntPtr i) : base(i) {}

		public SolarCabbage plant => gameObject.GetComponent<SolarCabbage>();

		void Start()
		{
			// 若 AB 里存在固定的发射点路径，可在此设置，避免空引用
			// plant.shoot = transform.Find("body/zi_dan/Shoot");
		}

		// 超级技能发射高伤害子弹方法
		public void SuperSkillShoot()
		{
			try
			{
				if (plant == null || plant.thePlantType != (PlantType)Core.PlantID)
					return;
				if (Board.Instance == null || CreateBullet.Instance == null)
					return;
				Vector3 pos = Vector3.zero;
				if (plant.shoot != null) pos = plant.shoot.position;
				else if (plant.axis != null) pos = plant.axis.position;
				else if (transform != null) pos = transform.position;

				// 超级技能固定伤害：20000
				int superSkillDamage = 20000;
				
				
				// 发射1颗高伤害子弹
				var bullet = CreateBullet.Instance.SetBullet(
					pos.x,
					pos.y,
					plant.thePlantRow,
					(BulletType)Core.BulletID,
					13, // 投掷移动方式 (Throw)
					false
				);
				
				// 设置子弹属性
				bullet.Damage = superSkillDamage;
				bullet.from = plant;
				bullet.targetPlant = plant;
				
				// 寻找同一行的僵尸作为目标
				Zombie targetZombie = SearchZombieInSameRow(plant);
				if (targetZombie != null)
				{
					bullet.targetZombie = targetZombie;
					
					// 计算抛物线轨迹，落地点向右偏移一格
					Vector2 startPosition = new Vector2(plant.transform.position.x, plant.transform.position.y);
					float t1 = Time.time - 0.5f;
					Vector2 firstPlace = new Vector2(targetZombie.transform.position.x + 1f, targetZombie.transform.position.y); // 向右偏移1格
					float t2 = Time.time;
					Vector2 secondPlace = firstPlace;
					float flightTime = 1.5f;
					
					// 计算抛物线参数
					float[] calculate = Lawnf.CalculateProjectileParameters(startPosition, t1, firstPlace, t2, secondPlace, flightTime);
					try
					{
						bullet.Vx = calculate[1];
						bullet.Vy = calculate[2];
						bullet.detaVy = -calculate[3];
					}
						catch (System.Exception)
						{
							// 如果计算失败，使用简单的投掷轨迹，向右偏移
							bullet.Vx = 2.5f; // 增加水平速度
							bullet.Vy = 3f;
							bullet.detaVy = -2f;
						}
				}
				else
				{
					// 没有目标僵尸，使用默认投掷轨迹，向右偏移
					bullet.Vx = 2.5f; // 增加水平速度
					bullet.Vy = 3f;
					bullet.detaVy = -2f;
				}
				
				try { GameAPP.PlaySound(UnityEngine.Random.Range(3, 5), 0.5f, 1f); } catch {}
			}
			catch (System.Exception)
			{
				// 超级技能发射失败，静默处理
			}
		}

		//  Unity 动画事件调用的方法（将动画事件指向这个方法名）
		public void AnimShoot()
		{
			try
			{
				if (plant == null || plant.thePlantType != (PlantType)Core.PlantID)
					return;
				if (Board.Instance == null || CreateBullet.Instance == null)
					return;
				Vector3 pos = Vector3.zero;
				if (plant.shoot != null) pos = plant.shoot.position;
				else if (plant.axis != null) pos = plant.axis.position;
				else if (transform != null) pos = transform.position;

				// 获取当前阳光数并计算伤害
				int currentSun = plant.board.theSun;
				int bulletDamage = currentSun / 50;
				
				// 确保伤害至少为1
				if (bulletDamage <= 0) bulletDamage = 1;
				
				
				// 创建投掷子弹（抛物线轨迹）- 射速加快1倍，子弹数量翻倍
				for (int i = 0; i < 20; i++)
				{
					var bullet = CreateBullet.Instance.SetBullet(
						pos.x,
						pos.y,
						plant.thePlantRow,
						(BulletType)Core.BulletID,
						13, // 投掷移动方式 (Throw)
						false
					);
					
					// 设置子弹属性
					bullet.Damage = bulletDamage;
					bullet.from = plant;
					bullet.targetPlant = plant;
					
					// 寻找同一行的僵尸作为目标
					Zombie targetZombie = SearchZombieInSameRow(plant);
					if (targetZombie != null)
					{
						bullet.targetZombie = targetZombie;
						
						// 计算抛物线轨迹，落地点向右偏移一格
						Vector2 startPosition = new Vector2(plant.transform.position.x, plant.transform.position.y);
						float t1 = Time.time - 0.5f;
						Vector2 firstPlace = new Vector2(targetZombie.transform.position.x + 1f, targetZombie.transform.position.y); // 向右偏移1格
						float t2 = Time.time;
						Vector2 secondPlace = firstPlace;
						float flightTime = 1.5f;
						
						// 计算抛物线参数
						float[] calculate = Lawnf.CalculateProjectileParameters(startPosition, t1, firstPlace, t2, secondPlace, flightTime);
						try
						{
							bullet.Vx = calculate[1];
							bullet.Vy = calculate[2];
							bullet.detaVy = -calculate[3];
						}
						catch (System.Exception)
						{
							// 如果计算失败，使用简单的投掷轨迹，向右偏移
							bullet.Vx = 2.5f; // 增加水平速度
							bullet.Vy = 3f;
							bullet.detaVy = -2f;
						}
					}
					else
					{
						// 没有目标僵尸，使用默认投掷轨迹，向右偏移
						bullet.Vx = 2.5f; // 增加水平速度
						bullet.Vy = 3f;
						bullet.detaVy = -2f;
					}
				}
				try { GameAPP.PlaySound(UnityEngine.Random.Range(3, 5), 0.5f, 1f); } catch {}
			}
			catch { }
		}
		
		// 搜索同一行的僵尸
		private Zombie SearchZombieInSameRow(Plant plant)
		{
			if (Board.Instance?.zombieArray == null) return null;
			
			foreach (Zombie zombie in Board.Instance.zombieArray)
			{
				if (zombie != null && zombie.theZombieRow == plant.thePlantRow)
				{
					return zombie;
				}
			}
			return null;
		}
	}

	[HarmonyPatch(typeof(Lawnf), nameof(Lawnf.GetPlantCount), new System.Type[] { typeof(PlantType), typeof(Board) })]
	public static class Lawnf_GetPlantCount_Patch
	{
		[HarmonyPostfix]
		public static void Postfix(ref PlantType theSeedType, ref Board board, ref int __result)
		{
			if (theSeedType == PlantType.UltimateCabbage)
			{
				var extra = Lawnf.GetPlantCount((PlantType)Core.PlantID, board);
				if (extra > 0) __result += extra;
			}
		}
	}

	// 自定义子弹补丁（如果需要特殊行为）
	[HarmonyPatch(typeof(Bullet))]
	public static class SuperSolarCabbageBulletPatch
	{
		[HarmonyPrefix]
		[HarmonyPatch("HitZombie")]
		public static bool PreHitZombie(Bullet __instance, Zombie zombie)
		{
			// 只处理自定义子弹
			if (__instance.theBulletType == (BulletType)Core.BulletID)
			{
				// 对命中的僵尸施加红温效果
				if (zombie != null)
				{
					zombie.SetJalaed();
				}
				
				// 播放特殊音效
				try { GameAPP.PlaySound(UnityEngine.Random.Range(3, 5), 0.5f, 1f); } catch { }
				
				// 正常伤害处理
				zombie.TakeDamage(0, __instance.Damage, false);
				__instance.Die();
				return false; // 阻止原始方法执行
			}
			return true; // 其他子弹正常处理
		}
	}
}


