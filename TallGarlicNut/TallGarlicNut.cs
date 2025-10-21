using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace TallGarlicNut.BepInEx
{
    /// 内鬼-蒜毒高坚果植物组件
    public class TallGarlicNut : MonoBehaviour
    {
        #region 常量定义
        ///攻击间隔时间（秒）
        private const float ATTACK_INTERVAL = 0.5f;
        
        ///大蒜中毒持续时间（秒）
        private const float GARLIC_POISON_DURATION = 10f;
        
        /// 大蒜值
        private const float GARLIC_VALUE = 2f;
        
        ///感同身受伤害值
        private const int SYMPATHY_DAMAGE = 15;
        
        ///随机召唤概率（百分比）
        private const int SUMMON_CHANCE = 10;
        
        /// 植物最大血量
        private const int MAX_HEALTH = 8000;
        #endregion

        #region 血量阈值定义
        ///血量阈值配置
        private static readonly HealthThreshold[] HealthThresholds = new HealthThreshold[]
        {
            new HealthThreshold(7000, 8000, 0, 43),      // 7000-8000: 召唤编号0~43的僵尸
            new HealthThreshold(6000, 7000, 47, 72),     // 6000-7000: 召唤编号47~72的僵尸
            new HealthThreshold(5000, 6000, 100, 125),   // 5000-6000: 召唤编号100~125的僵尸
            new HealthThreshold(4000, 5000, 200, 217),   // 4000-5000: 召唤编号200~217的僵尸
            new HealthThreshold(3000, 4000, 218, 230),   // 3000-4000: 召唤领袖僵尸
            new HealthThreshold(2000, 3000, 300, 335),   // 2000-3000: 召唤1~3级僵尸
            new HealthThreshold(1000, 2000, 0, 999),     // 1000-2000: 召唤所有僵尸
            new HealthThreshold(0, 1000, -1, -1)         // 1000以下: 死亡触发片甲不留
        };
        #endregion

        #region 属性
        /// 获取关联的大蒜植物组件
        public Garlic Plant
        {
            get
            {
                return gameObject.GetComponent<Garlic>();
            }
        }
        #endregion

        #region 私有字段
        ///攻击计时器
        private float attackTimer = 0f;
        
        ///是否已触发片甲不留技能
        private bool hasTriggeredAnnihilation = false;
        #endregion

        #region 构造函数
        /// 构造函数
        /// <param name="i">IntPtr参数</param>
        public TallGarlicNut(IntPtr i) : base(i)
        {
        }
        #endregion

        #region Unity生命周期
        /// 每帧更新
        /// 处理攻击计时和技能触发
        private void Update()
        {
            // 检查植物是否存在
            if (Plant == null) return;

            // 更新攻击计时器
            attackTimer += Time.deltaTime;
            
            // 检查是否到达攻击间隔
            if (attackTimer >= ATTACK_INTERVAL)
            {
                attackTimer = 0f;
                ExecuteGarlicFumeAttack();
            }
        }
        #endregion

        #region 数据结构
        /// 血量阈值配置结构
        private struct HealthThreshold
        {
            public int MinHealth;
            public int MaxHealth;
            public int MinZombieId;
            public int MaxZombieId;

            public HealthThreshold(int minHealth, int maxHealth, int minZombieId, int maxZombieId)
            {
                MinHealth = minHealth;
                MaxHealth = maxHealth;
                MinZombieId = minZombieId;
                MaxZombieId = maxZombieId;
            }
        }
        #endregion

        #region 核心攻击逻辑
        /// 执行大蒜毒气攻击
        /// 包含所有技能效果：真·父爱如山、感同身受、通敌叛国等
        public void ExecuteGarlicFumeAttack()
        {
            // 检查前置条件
            if (Plant == null || Board.Instance == null)
            {
                Debug.LogWarning("TallGarlicNut: 植物或游戏板实例为空，无法执行攻击");
                return;
            }

            try
            {
                // 处理所有僵尸
                ProcessZombies();
                
                // 处理血量阈值技能
                ProcessHealthThresholdSkills();
            }
            catch (Exception ex)
            {
                Debug.LogError($"TallGarlicNut: 执行攻击时发生错误: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        /// 处理僵尸相关效果
        /// 包含：真·父爱如山、感同身受等技能
        private void ProcessZombies()
        {
            if (Board.Instance?.zombieArray == null) return;

            foreach (Zombie zombie in Board.Instance.zombieArray)
            {
                if (zombie == null) continue;

                // 真·父爱如山：对正在攻击的僵尸施加大蒜效果
                if (zombie.isAttacking)
                {
                    ApplyGarlicEffect(zombie);
                }
            }
        }
        
        /// 对僵尸施加大蒜效果
        /// <param name="zombie">目标僵尸</param>
        private void ApplyGarlicEffect(Zombie zombie)
        {
            try
            {
                // 让僵尸吃大蒜（换行效果）
                zombie.EatGarlic(Plant, 0.5f, true);
                
                // 设置中毒效果
                zombie.SetPoison(GARLIC_POISON_DURATION);
                
                // 应用大蒜状态
                zombie.Garliced(false, false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"TallGarlicNut: 应用大蒜效果时发生错误: {ex.Message}");
            }
        }
        
        /// 处理血量阈值技能
        /// 包含：通敌叛国、片甲不留等技能
        private void ProcessHealthThresholdSkills()
        {
            int currentHealth = Plant.thePlantHealth;
            
            // 遍历所有血量阈值配置
            foreach (var threshold in HealthThresholds)
            {
                if (currentHealth <= threshold.MaxHealth && currentHealth > threshold.MinHealth)
                {
                    // 检查是否应该触发技能
                    if (ShouldTriggerSkill())
                    {
                        ExecuteThresholdSkill(threshold, currentHealth);
                    }
                    break; // 找到匹配的阈值后退出
                }
            }
        }
        
        /// 判断是否应该触发技能
        /// <returns>是否触发</returns>
        private bool ShouldTriggerSkill()
        {
            return UnityEngine.Random.Range(1, 101) <= SUMMON_CHANCE;
        }
        
        /// 执行阈值技能
        /// <param name="threshold">血量阈值配置</param>
        /// <param name="currentHealth">当前血量</param>
        private void ExecuteThresholdSkill(HealthThreshold threshold, int currentHealth)
        {
            // 特殊处理：血量低于1000时触发片甲不留
            if (threshold.MinHealth == 0 && threshold.MaxHealth == 1000)
            {
                TriggerAnnihilationSkill();
                return;
            }

            // 召唤僵尸
            if (threshold.MinZombieId >= 0 && threshold.MaxZombieId >= 0)
            {
                SummonRandomZombie(threshold.MinZombieId, threshold.MaxZombieId);
            }
        }
        
        /// 召唤随机僵尸
        /// <param name="minId">最小僵尸ID</param>
        /// <param name="maxId">最大僵尸ID</param>
        private void SummonRandomZombie(int minId, int maxId)
        {
            try
            {
                int randomRow = UnityEngine.Random.Range(0, Board.Instance.rowNum);
                int randomZombieId = UnityEngine.Random.Range(minId, maxId + 1);
                
                CreateZombie.Instance.SetZombie(randomRow, (ZombieType)randomZombieId, 9.9f, false);
                
                Debug.Log($"TallGarlicNut: 召唤了僵尸 ID:{randomZombieId} 在行:{randomRow}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"TallGarlicNut: 召唤僵尸时发生错误: {ex.Message}");
            }
        }
        
        /// 触发片甲不留技能
        /// 当血量低于1000时，植物死亡并召唤一列究极黑橄榄大帅
        private void TriggerAnnihilationSkill()
        {
            if (hasTriggeredAnnihilation) return;
            
            hasTriggeredAnnihilation = true;
            
            try
            {
                // 植物死亡
                Plant.Die(0);
                
                // 召唤一列究极黑橄榄大帅（僵尸ID: 229）
                for (int row = 0; row < Board.Instance.rowNum; row++)
                {
                    CreateZombie.Instance.SetZombie(row, (ZombieType)229, 9.9f, false);
                }
                
                Debug.Log("TallGarlicNut: 触发片甲不留技能，召唤了一列究极黑橄榄大帅");
            }
            catch (Exception ex)
            {
                Debug.LogError($"TallGarlicNut: 触发片甲不留技能时发生错误: {ex.Message}");
            }
        }
        #endregion

        #region 公共属性
        /// 植物ID，用于游戏内识别
        public static int PlantID = 2032;
        #endregion
    }
}
