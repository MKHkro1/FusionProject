using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace NoHeadUltimateHorse.BepInEx
{
		/// 被究极无头骑士攻击过的植物：移除限伤逻辑
		[HarmonyPatch(typeof(Plant), "TakeDamage")]
		public class PlantTakeDamage_UncapPatch
		{
			[HarmonyPrefix]
			public static bool Prefix(Plant __instance, ref int damage, ref int damageType)
			{
				try
				{
					if (__instance != null && __instance.gameObject != null &&
						__instance.gameObject.GetComponent<UncappedPlantDamageComponent>() != null)
					{
						// 直接按真实伤害结算
						if (__instance.thePlantHealth > 0)
						{
							__instance.thePlantHealth -= damage;
							__instance.FlashOnce();
							__instance.UpdateText();
							if (__instance.thePlantHealth <= 0)
							{
								__instance.thePlantHealth = 0;
								__instance.Broken();
							}
						}
						return false;
					}
				}
				catch (Exception ex)
				{
					Core.Instance?.Logger.LogError($"究极无头骑士插件: 取消植物限伤补丁失败: {ex.Message}");
				}
				return true;
			}
		}

		/// 标记为领袖僵尸（Boss）
		[HarmonyPatch(typeof(TypeMgr), "IsBossZombie")]
		public class TypeMgrIsBossZombiePatch
		{
			[HarmonyPostfix]
			public static void Postfix(ref ZombieType theZombieType, ref bool __result)
			{
				if (theZombieType == (ZombieType)NoHeadUltimateHorseComponent.ZombieID)
				{
					__result = true;
				}
			}
		}

    [HarmonyPatch]
    public class NoHeadUltimateHorsePatches
    {
        /// 拦截Zombie的TakeDamage方法，实现无敌状态
        [HarmonyPatch(typeof(Zombie), "TakeDamage")]
        [HarmonyPostfix]
        public static void TakeDamage_Postfix(Zombie __instance, DmgType theDamageType, int theDamage, bool fix)
        {
            try
            {
                // 检查是否为究极无头骑士
                if (__instance.theZombieType == (ZombieType)NoHeadUltimateHorseComponent.ZombieID)
                {
                    NoHeadUltimateHorseComponent component = __instance.gameObject.GetComponent<NoHeadUltimateHorseComponent>();
                    if (component != null && component.isInvincible)
                    {
                        // 无敌状态：直接恢复最大血量
                        int maxHealth = __instance.theMaxHealth;
                        if (__instance.theHealth < maxHealth)
                        {
                            __instance.theHealth = maxHealth;
                            __instance.UpdateHealthText();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Core.Instance?.Logger.LogError($"究极无头骑士插件: TakeDamage补丁执行失败: {ex.Message}");
            }
        }

        /// 拦截Zombie的GetDamage方法，实现免疫
        [HarmonyPatch(typeof(UltimateHorse), "GetDamage")]
        [HarmonyPrefix]
        public static bool GetDamage_Prefix(UltimateHorse __instance, int theDamage, int theDamageType, bool fix, ref int __result)
        {
            try
            {
                // 检查是否为究极无头骑士
                if (__instance.theZombieType == (ZombieType)NoHeadUltimateHorseComponent.ZombieID)
                {
                    // UltimateHorse基类已经处理了伤害转移等逻辑
                    // 这个地方还可以添加额外的处理
                    return true; // 使用原始逻辑
                }

                return true; // 使用原始逻辑
            }
            catch (Exception ex)
            {
                Core.Instance?.Logger.LogError($"究极无头骑士插件: GetDamage补丁执行失败: {ex.Message}");
                return true;
            }
        }

        /// 拦截Zombie的KnockBack方法，实现免疫击退
        [HarmonyPatch(typeof(Zombie), "KnockBack")]
        [HarmonyPrefix]
        public static bool KnockBack_Prefix(Zombie __instance, float x, Zombie.KnockBackReason reason)
        {
            try
            {
                // 检查是否为究极无头骑士
                if (__instance.theZombieType == (ZombieType)NoHeadUltimateHorseComponent.ZombieID)
                {
                    // 免疫击退
                    return false; // 阻止原始方法执行
                }

                return true; // 使用原始逻辑
            }
            catch (Exception ex)
            {
                Core.Instance?.Logger.LogError($"究极无头骑士插件: KnockBack补丁执行失败: {ex.Message}");
                return true;
            }
        }

        /// 拦截Zombie的SetFreeze方法，实现免疫冻结
        [HarmonyPatch(typeof(Zombie), "SetFreeze")]
        [HarmonyPrefix]
        public static bool SetFreeze_Prefix(Zombie __instance, float time, int theFreezeLevel)
        {
            try
            {
                // 检查是否为究极无头骑士
                if (__instance.theZombieType == (ZombieType)NoHeadUltimateHorseComponent.ZombieID)
                {
                    // 免疫冻结
                    return false; // 阻止原始方法执行
                }

                return true; // 使用原始逻辑
            }
            catch (Exception ex)
            {
                Core.Instance?.Logger.LogError($"究极无头骑士插件: SetFreeze补丁执行失败: {ex.Message}");
                return true;
            }
        }

        /// 拦截Zombie的AddfreezeLevel方法，实现免疫冻结
        [HarmonyPatch(typeof(Zombie), "AddfreezeLevel")]
        [HarmonyPrefix]
        public static bool AddfreezeLevel_Prefix(Zombie __instance, int level)
        {
            try
            {
                // 检查是否为究极无头骑士
                if (__instance.theZombieType == (ZombieType)NoHeadUltimateHorseComponent.ZombieID)
                {
                    // 免疫冻结
                    return false; // 阻止原始方法执行
                }

                return true; // 使用原始逻辑
            }
            catch (Exception ex)
            {
                Core.Instance?.Logger.LogError($"究极无头骑士插件: AddfreezeLevel补丁执行失败: {ex.Message}");
                return true;
            }
        }

        /// 拦截Zombie的SetCold方法，实现免疫寒冷
        [HarmonyPatch(typeof(Zombie), "SetCold")]
        [HarmonyPrefix]
        public static bool SetCold_Prefix(Zombie __instance, float time)
        {
            try
            {
                // 检查是否为究极无头骑士
                if (__instance.theZombieType == (ZombieType)NoHeadUltimateHorseComponent.ZombieID)
                {
                    // 免疫寒冷
                    return false; // 阻止原始方法执行
                }

                return true; // 使用原始逻辑
            }
            catch (Exception ex)
            {
                Core.Instance?.Logger.LogError($"究极无头骑士插件: SetCold补丁执行失败: {ex.Message}");
                return true;
            }
        }

        /// 拦截Zombie的SetPoison方法，实现免疫蒜毒
        [HarmonyPatch(typeof(Zombie), "SetPoison")]
        [HarmonyPrefix]
        public static bool SetPoison_Prefix(Zombie __instance)
        {
            try
            {
                // 检查是否为究极无头骑士
                if (__instance.theZombieType == (ZombieType)NoHeadUltimateHorseComponent.ZombieID)
                {
                    // 免疫蒜毒
                    return false; // 阻止原始方法执行
                }

                return true; // 使用原始逻辑
            }
            catch (Exception ex)
            {
                Core.Instance?.Logger.LogError($"究极无头骑士插件: SetPoison补丁执行失败: {ex.Message}");
                return true;
            }
        }

        /// 拦截Zombie的AddPoisonLevel方法，实现免疫蒜毒
        [HarmonyPatch(typeof(Zombie), "AddPoisonLevel")]
        [HarmonyPrefix]
        public static bool AddPoisonLevel_Prefix(Zombie __instance)
        {
            try
            {
                // 检查是否为究极无头骑士
                if (__instance.theZombieType == (ZombieType)NoHeadUltimateHorseComponent.ZombieID)
                {
                    // 免疫蒜毒
                    return false; // 阻止原始方法执行
                }

                return true; // 使用原始逻辑
            }
            catch (Exception ex)
            {
                Core.Instance?.Logger.LogError($"究极无头骑士插件: AddPoisonLevel补丁执行失败: {ex.Message}");
                return true;
            }
        }

        /// 拦截Zombie的Die方法，实现不死状态复活和无敌状态阻止死亡
        [HarmonyPatch(typeof(Zombie), "Die")]
        [HarmonyPrefix]
        public static bool Die_Prefix(Zombie __instance, int reason)
        {
            try
            {
                // 首先检查是否为究极无头骑士
                NoHeadUltimateHorseComponent component = __instance.gameObject.GetComponent<NoHeadUltimateHorseComponent>();
                if (component != null)
                {
                    // 检查无敌状态
                    if (component.isInvincible)
                    {
                        // 无敌状态下阻止死亡，恢复最大血量
                        __instance.theHealth = __instance.theMaxHealth;
                        __instance.UpdateHealthText();
                        return false; // 阻止死亡
                    }
                    
                    // 不是无敌状态，允许死亡（但记录日志）
                    Core.Instance?.Logger.LogWarning($"究极无头骑士插件: 究极无头骑士死亡，原因: {reason}, 血量: {__instance.theHealth}/{__instance.theMaxHealth}");
                    return true; // 使用原始逻辑
                }

                // 检查是否有不死状态（其他僵尸）
                UndyingBuffComponent buff = __instance.gameObject.GetComponent<UndyingBuffComponent>();
                if (buff != null && buff.isUndying)
                {
                    // 立即复活
                    __instance.theHealth = __instance.theMaxHealth;
                    __instance.UpdateHealthText();
                    
                    // 复活特效
                    if (__instance.axis != null)
                    {
                        Vector3 position = __instance.axis.position;
                        CreateParticle.SetParticle(121, new Vector3(position.x, position.y, 0f), __instance.theZombieRow);
                    }

                    Core.Instance?.Logger.LogInfo($"究极无头骑士插件: 僵尸 {__instance.theZombieType} 因不死状态复活");
                    
                    return false; // 阻止死亡
                }

                return true; // 使用原始逻辑
            }
            catch (Exception ex)
            {
                Core.Instance?.Logger.LogError($"究极无头骑士插件: Die补丁执行失败: {ex.Message}");
                return true;
            }
        }

        /// 拦截UltimateHorse的Start方法，防止在非游戏状态下出现空引用异常
        /// 使用Prefix来检查必要对象是否存在，如果不存在则跳过原始方法
        /// 提前设置walk动画，跳过tohorse
        /// </summary>
        [HarmonyPatch(typeof(UltimateHorse), "Start")]
        [HarmonyPrefix]
        public static bool Start_Prefix(UltimateHorse __instance)
        {
            try
            {
                // 检查是否为究极无头骑士
                if (__instance.theZombieType == (ZombieType)NoHeadUltimateHorseComponent.ZombieID)
                {
                    if (GameAPP.theGameStatus != GameStatus.InGame)
                    {
                        
                        try
                        {
                            if (__instance.theMaxHealth != 54000)
                            {
                                __instance.theMaxHealth = 54000;
                                __instance.theHealth = 54000;
                            }
                        }
                        catch (Exception ex)
                        {
                            Core.Instance?.Logger.LogWarning($"究极无头骑士插件: 设置血量失败: {ex.Message}");
                        }
                        
                        // 返回false以跳过原始Start方法
                        return false;
                    }
                    
                    // 在游戏状态下，检查必要对象是否存在
                    if (Board.Instance == null || Mouse.Instance == null)
                    {
                        Core.Instance?.Logger.LogWarning("究极无头骑士插件: Board或Mouse为null，但仍尝试执行Start方法");
                        // 不返回false，让Start方法执行，可能会出错但至少会尝试初始化
                    }
                    
                    // 在游戏状态下，提前设置walk动画（在Start执行前）
                    if (__instance.anim != null)
                    {
                        try
                        {
                            NoHeadUltimateHorseComponent component = __instance.gameObject.GetComponent<NoHeadUltimateHorseComponent>();
                            if (component != null)
                            {
                                // 立即设置walk动画，避免tohorse动画触发
                                __instance.anim.SetTrigger("walk");
                            }
                        }
                        catch (Exception ex)
                        {
                            Core.Instance?.Logger.LogWarning($"究极无头骑士插件: 在Prefix中设置walk动画失败: {ex.Message}");
                        }
                    }
                    
                    // 始终返回true，让原始的Start方法执行
                    // 确保僵尸正常初始化
                }
                
                // 对于其他情况，执行原始方法
                return true;
            }
            catch (Exception ex)
            {
                Core.Instance?.Logger.LogError($"究极无头骑士插件: Start Prefix补丁执行失败: {ex.Message}");
                Core.Instance?.Logger.LogError($"究极无头骑士插件: 堆栈跟踪: {ex.StackTrace}");
                // 发生异常时，尝试执行原始方法
                return true;
            }
        }

        /// 拦截UltimateHorse的Start方法，在Postfix中验证组件并立即设置walk动画
        [HarmonyPatch(typeof(UltimateHorse), "Start")]
        [HarmonyPostfix]
        public static void Start_Postfix(UltimateHorse __instance)
        {
            try
            {
                // 检查是否为究极无头骑士
                if (__instance.theZombieType == (ZombieType)NoHeadUltimateHorseComponent.ZombieID)
                {
                    NoHeadUltimateHorseComponent component = __instance.gameObject.GetComponent<NoHeadUltimateHorseComponent>();
                    if (component != null)
                    {
                        // 在游戏状态下，立即设置walk动画，跳过tohorse
                        // 使用延迟确保在UltimateHorse.Start()完成后再设置动画
                        if (GameAPP.theGameStatus == GameStatus.InGame && __instance.anim != null)
                        {
                            // 立即调用，不延迟，以确保walk动画尽快设置
                            component.SetWalkAnimation();
                            
                            // 延迟再次设置，确保walk状态持续
                            component.Invoke("SetWalkAnimation", 0.1f);
                            component.Invoke("SetWalkAnimation", 0.2f);
                            component.Invoke("SetWalkAnimation", 0.5f);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Core.Instance?.Logger.LogError($"究极无头骑士插件: Start Postfix补丁执行失败: {ex.Message}");
            }
        }

        /// 在黑大帅死亡后生成究极无头骑士
        /// 使用Die的Postfix，只在实际死亡后触发一次
        private static readonly System.Collections.Generic.HashSet<int> s_spawnedFromBlackHandsome = new System.Collections.Generic.HashSet<int>();
        [HarmonyPatch(typeof(Zombie), "Die")]
        [HarmonyPostfix]
        public static void ZombieDie_Postfix(Zombie __instance, int reason)
        {
            try
            {
                if (GameAPP.theGameStatus != GameStatus.InGame)
                    return;

                // 仅当黑大帅死亡时触发
                if (__instance.theZombieType != (ZombieType)230)
                    return;

                // 防重复：同一只黑大帅只生成一次
                int key = __instance.GetInstanceID();
                if (s_spawnedFromBlackHandsome.Contains(key))
                    return;
                s_spawnedFromBlackHandsome.Add(key);

                if (Board.Instance == null || CreateZombie.Instance == null)
                    return;

                int zombieRow = __instance.theZombieRow;
                float zombieX = __instance.transform != null ? __instance.transform.position.x :
                                (__instance.axis != null ? __instance.axis.position.x : 0f);

                GameObject? spawned = null;
                if (!__instance.isMindControlled)
                {
                    spawned = CreateZombie.Instance.SetZombie(zombieRow, (ZombieType)NoHeadUltimateHorseComponent.ZombieID, zombieX);
                }
                else
                {
                    spawned = CreateZombie.Instance.SetZombieWithMindControl(zombieRow, (ZombieType)NoHeadUltimateHorseComponent.ZombieID, zombieX);
                }

            }
            catch
            {
            }
        }

        /// 拦截Animator.SetTrigger方法，阻止tohorse动画触发
        /// 使用ref参数来修改name参数，将tohorse改为walk
        [HarmonyPatch(typeof(UnityEngine.Animator), "SetTrigger", new Type[] { typeof(string) })]
        [HarmonyPrefix]
        public static bool AnimatorSetTrigger_Prefix(UnityEngine.Animator __instance, ref string name)
        {
            try
            {
                // 检查是否为究极无头骑士的Animator
                if (__instance != null && __instance.gameObject != null)
                {
                    UltimateHorse zombie = __instance.gameObject.GetComponent<UltimateHorse>();
                    if (zombie != null && zombie.theZombieType == (ZombieType)NoHeadUltimateHorseComponent.ZombieID)
                    {
                        // 如果尝试触发tohorse动画，改为walk
                        if (name != null && (name.Equals("tohorse", StringComparison.OrdinalIgnoreCase) || 
                                            name.Equals("ToHorse", StringComparison.OrdinalIgnoreCase)))
                        {
                            Core.Instance?.Logger.LogInfo($"究极无头骑士插件: 拦截tohorse动画触发，改为walk动画");
                            name = "walk"; // 修改参数为walk，让原始方法执行walk动画
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Core.Instance?.Logger.LogError($"究极无头骑士插件: AnimatorSetTrigger补丁执行失败: {ex.Message}");
            }
            
            // 继续执行原始方法（参数已被修改）
            return true;
        }
    }
}
