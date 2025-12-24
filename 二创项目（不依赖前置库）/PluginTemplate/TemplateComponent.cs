using System;
using System.Collections.Generic;
using UnityEngine;

namespace PluginTemplate.BepInEx
{
    /// <summary>
    /// 【模板】植物核心逻辑组件
    /// 
    /// 使用说明：
    /// 1. 修改类名为你的植物名
    /// 2. 修改 PlantID 为你的植物ID（避免与游戏内ID冲突，建议1900+）
    /// 3. 根据植物类型修改基类组件获取方式
    /// 4. 实现你的植物逻辑
    /// </summary>
    public class TemplateComponent : MonoBehaviour
    {
        // ==================== 植物配置 ====================
        /// <summary>
        /// 植物ID（必须唯一，建议使用1900+的ID）
        /// </summary>
        public static int PlantID = 1999;

        /// <summary>
        /// 植物名称（用于日志）
        /// </summary>
        public static string PlantName = "模板植物";

        // ==================== IL2CPP 必须的构造函数 ====================
        /// <summary>
        /// IL2CPP 构造函数（必须！否则 AddComponent 会失败）
        /// </summary>
        public TemplateComponent(IntPtr ptr) : base(ptr) { }

        // ==================== 组件引用 ====================
        /// <summary>
        /// 获取关联的基类组件
        /// 根据你的植物类型修改：
        /// - PeaShooter: 射击类
        /// - Imitater: 模仿者类
        /// - Chomper/UltimateChomper: 食人花类
        /// - WallNut/TallNut: 坚果类
        /// - Sunflower: 向日葵类
        /// </summary>
        public Plant? BasePlant => gameObject.GetComponent<Plant>();
        
        // 如果是特定类型，可以添加更具体的引用
        // public PeaShooter? Shooter => gameObject.GetComponent<PeaShooter>();
        // public Imitater? Imitater => gameObject.GetComponent<Imitater>();

        // ==================== 私有变量 ====================
        private float _timer = 0f;
        private int _attackCount = 0;
        private bool _initialized = false;

        // ==================== Unity 生命周期 ====================
        
        void Start()
        {
            Core.Logger?.LogInfo($"[{PlantName}] 植物已生成");
            _initialized = true;
            OnPlantSpawned();
        }

        void Update()
        {
            if (!_initialized) return;

            // 每秒执行一次逻辑
            _timer += Time.deltaTime;
            if (_timer >= 1f)
            {
                _timer = 0f;
                OnSecondTick();
            }

            // 自定义更新逻辑
            OnCustomUpdate();
        }

        void OnDestroy()
        {
            Core.Logger?.LogInfo($"[{PlantName}] 植物已销毁");
            OnPlantDestroyed();
        }

        // ==================== 自定义生命周期方法 ====================

        /// <summary>
        /// 植物生成时调用
        /// </summary>
        protected virtual void OnPlantSpawned()
        {
            // 在这里初始化植物状态
        }

        /// <summary>
        /// 每秒调用一次
        /// </summary>
        protected virtual void OnSecondTick()
        {
            // 在这里实现每秒逻辑，例如：
            // - 回血
            // - 充能
            // - 状态检查
        }

        /// <summary>
        /// 每帧调用
        /// </summary>
        protected virtual void OnCustomUpdate()
        {
            // 在这里实现每帧逻辑
        }

        /// <summary>
        /// 植物销毁时调用
        /// </summary>
        protected virtual void OnPlantDestroyed()
        {
            // 在这里实现死亡逻辑，例如：
            // - 爆炸效果
            // - 掉落物品
        }

        // ==================== 动画事件回调 ====================
        // 这些方法名必须与 AssetBundle 中动画事件设置的名称一致

        /// <summary>
        /// 动画事件：射击
        /// </summary>
        public void AnimShoot()
        {
            _attackCount++;
            Core.Logger?.LogInfo($"[{PlantName}] AnimShoot 被调用，第 {_attackCount} 次攻击");
            OnAnimShoot();
        }

        /// <summary>
        /// 动画事件：变身/生成
        /// </summary>
        public void AnimSpawn()
        {
            Core.Logger?.LogInfo($"[{PlantName}] AnimSpawn 被调用");
            OnAnimSpawn();
        }

        /// <summary>
        /// 动画事件：爆炸
        /// </summary>
        public void AnimExplode()
        {
            Core.Logger?.LogInfo($"[{PlantName}] AnimExplode 被调用");
            OnAnimExplode();
        }

        /// <summary>
        /// 动画事件：啃咬
        /// </summary>
        public void AnimChomp()
        {
            Core.Logger?.LogInfo($"[{PlantName}] AnimChomp 被调用");
            OnAnimChomp();
        }

        // ==================== 可重写的动画事件处理 ====================

        protected virtual void OnAnimShoot()
        {
            // 实现射击逻辑
            // 示例：发射子弹
            /*
            var plant = BasePlant;
            if (plant == null) return;
            
            // 获取发射位置
            var shootPos = plant.axis?.position ?? transform.position;
            
            // 创建子弹（需要根据实际情况修改）
            // Bullet.CreateBullet(...);
            */
        }

        protected virtual void OnAnimSpawn()
        {
            // 实现变身逻辑
            // 示例：模仿者变身
            /*
            var plant = BasePlant;
            if (plant == null) return;
            
            int row = plant.thePlantRow;
            int col = plant.thePlantColumn;
            
            // 让当前植物死亡
            plant.Die((Plant.DieReason)2);
            
            // 生成新植物
            CreatePlant.Instance?.SetPlant(col, row, PlantType.Peashooter, null, default, true, true, null);
            */
        }

        protected virtual void OnAnimExplode()
        {
            // 实现爆炸逻辑
            // 示例：毁灭菇爆炸
            /*
            var plant = BasePlant;
            if (plant == null) return;
            
            var pos = plant.axis?.position ?? transform.position;
            
            // 播放爆炸粒子
            ParticleManager.Instance?.SetParticle((ParticleType)11, pos, plant.thePlantRow);
            
            // 对范围内僵尸造成伤害
            // ...
            */
        }

        protected virtual void OnAnimChomp()
        {
            // 实现啃咬逻辑
        }

        // ==================== 工具方法 ====================

        /// <summary>
        /// 获取植物所在行
        /// </summary>
        protected int GetRow() => BasePlant?.thePlantRow ?? 0;

        /// <summary>
        /// 获取植物所在列
        /// </summary>
        protected int GetColumn() => BasePlant?.thePlantColumn ?? 0;

        /// <summary>
        /// 获取植物位置
        /// </summary>
        protected Vector3 GetPosition() => BasePlant?.axis?.position ?? transform.position;

        /// <summary>
        /// 设置植物血量
        /// </summary>
        protected void SetHealth(int health)
        {
            var plant = BasePlant;
            if (plant != null)
            {
                plant.theHealth = health;
            }
        }

        /// <summary>
        /// 获取植物血量
        /// </summary>
        protected int GetHealth() => BasePlant?.theHealth ?? 0;

        /// <summary>
        /// 获取植物最大血量
        /// </summary>
        protected int GetMaxHealth() => BasePlant?.theMaxHealth ?? 0;

        /// <summary>
        /// 回复血量
        /// </summary>
        protected void Heal(int amount)
        {
            var plant = BasePlant;
            if (plant != null)
            {
                plant.theHealth = Math.Min(plant.theHealth + amount, plant.theMaxHealth);
            }
        }

        /// <summary>
        /// 让植物死亡
        /// </summary>
        protected void Die(Plant.DieReason reason = Plant.DieReason.Default)
        {
            BasePlant?.Die(reason);
        }

        /// <summary>
        /// 在指定位置创建植物
        /// </summary>
        protected void CreatePlantAt(int column, int row, PlantType plantType)
        {
            CreatePlant.Instance?.SetPlant(column, row, plantType, null, default, true, true, null);
        }

        /// <summary>
        /// 在指定位置创建僵尸
        /// </summary>
        protected void CreateZombieAt(int row, ZombieType zombieType, float x)
        {
            CreateZombie.Instance?.SetZombie(row, zombieType, x, false);
        }

        /// <summary>
        /// 播放粒子效果
        /// </summary>
        protected void PlayParticle(ParticleType type, Vector3 position, int row)
        {
            ParticleManager.Instance?.SetParticle(type, position, row);
        }

        /// <summary>
        /// 显示游戏内文本
        /// </summary>
        protected void ShowText(string text, float duration = 3f)
        {
            InGameText.Instance?.ShowText(text, duration, false);
        }
    }
}
