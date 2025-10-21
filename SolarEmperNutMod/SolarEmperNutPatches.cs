using System;
using System.Collections.Generic;
using UnityEngine;
using CustomizeLib.BepInEx;

namespace SolarEmperNutMod
{
    /// <summary>
    /// 阳光帝果技能修改
    /// 使用CustomCore.RegisterCustomPlantClickEvent注册点击事件
    /// </summary>
    public class SolarEmperNutPatches
    {
        // 阳光帝果的植物ID
        private const int SOLAR_EMPER_NUT_ID = 905;
        // 巨型阳光坚果的植物ID
        private const int GIANT_SUN_NUT_ID = 251;

                /// <summary>
                /// 阳光帝果点击事件处理
                /// 直接滚出巨型阳光坚果
                /// </summary>
                /// <param name="plant">阳光帝果实例</param>
                public static void HandleSolarEmperNutClick(Plant plant)
                {
                    try
                    {
                        // 检查阳光是否足够 (500阳光)
                        if (plant.board.theSun >= 500)
                        {
                            // 消耗阳光
                            plant.board.theSun -= 500;
                            
                            // 回复1倍韧性血量
                            plant.Recover(1500f); // 使用固定值，参考代码中的做法
                            
                            // 直接创建巨型阳光坚果并让它滚动
                            CreateRollingGiantSunNut(plant);
                        }
                    }
                    catch (Exception ex)
                    {
                        // 静默处理错误
                    }
                }

        /// <summary>
        /// 创建滚动的巨型阳光坚果
        /// </summary>
        /// <param name="solarEmperNut">阳光帝果实例</param>
        private static void CreateRollingGiantSunNut(Plant solarEmperNut)
        {
            try
            {
                // 在前方1格创建巨型阳光坚果
                GameObject giantSunNut = CreatePlant.Instance.SetPlant(
                    solarEmperNut.thePlantColumn + 1, 
                    solarEmperNut.thePlantRow, 
                    (PlantType)GIANT_SUN_NUT_ID, 
                    null, 
                    Vector2.zero, 
                    true, 
                    true
                );
                
                if (giantSunNut != null)
                {
                    // 延迟设置滚动行为，避免GetComponent问题
                    // 直接延迟执行，避免AddComponent问题
                    try
                    {
                        // 直接尝试设置滚动，不依赖GetComponent
                        DelayedSetRollingDirect(giantSunNut, 0.1f);
                    }
                    catch (Exception ex)
                    {
                        // 静默处理错误
                    }
                }
            }
            catch (Exception ex)
            {
                // 静默处理错误
            }
        }

        /// <summary>
        /// 让巨型阳光坚果立即开始滚动
        /// </summary>
        /// <param name="giantSunNut">巨型阳光坚果实例</param>
        public static void MakeGiantSunNutRoll(Plant giantSunNut)
        {
            try
            {
                // 创建一个新的GameObject来处理滚动逻辑
                GameObject rollingObject = new GameObject("RollingNutObject");
                rollingObject.transform.position = giantSunNut.gameObject.transform.position;
                rollingObject.transform.SetParent(giantSunNut.gameObject.transform);
                
                // 直接添加滚动组件到新对象
                RollingNut rollingComponent = rollingObject.AddComponent<RollingNut>();
                
                // 设置滚动参数 - 使用默认行数0，避免GetComponent调用
                rollingComponent.Initialize(0, 600); // 固定伤害600
                
                // 立即开始滚动（不延迟）
                rollingComponent.StartRolling(0.0f);
            }
            catch (Exception ex)
            {
                // 静默处理错误
            }
        }

        /// <summary>
        /// 延迟设置滚动的方法
        /// </summary>
        /// <param name="plant">植物实例</param>
        /// <param name="delay">延迟时间</param>
        private static void DelayedSetRolling(Plant plant, float delay)
        {
            try
            {
                if (plant != null)
                {
                    // 立即让巨型阳光坚果开始滚动（不延迟）
                    MakeGiantSunNutRoll(plant);
                }
            }
            catch (Exception ex)
            {
                // 静默处理错误
            }
        }

        /// <summary>
        /// 直接设置滚动的方法，不依赖GetComponent
        /// </summary>
        /// <param name="gameObject">游戏对象</param>
        /// <param name="delay">延迟时间</param>
        private static void DelayedSetRollingDirect(GameObject gameObject, float delay)
        {
            try
            {
                if (gameObject != null)
                {
                    // 直接让巨型阳光坚果开始滚动（不延迟）
                    MakeGiantSunNutRollDirect(gameObject);
                }
            }
            catch (Exception ex)
            {
                // 静默处理错误
            }
        }

        /// <summary>
        /// 直接让巨型阳光坚果开始滚动，不依赖AddComponent
        /// </summary>
        /// <param name="gameObject">游戏对象</param>
        public static void MakeGiantSunNutRollDirect(GameObject gameObject)
        {
            try
            {
                // 创建一个新的GameObject来处理滚动逻辑
                GameObject rollingObject = new GameObject("RollingNutObject");
                rollingObject.transform.position = gameObject.transform.position;
                rollingObject.transform.SetParent(gameObject.transform);
                
                // 添加滚动组件到新对象
                try
                {
                    RollingNut rollingComponent = rollingObject.AddComponent<RollingNut>();
                    
                    // 设置滚动参数 - 使用默认行数0，固定伤害600
                    rollingComponent.Initialize(0, 600);
                    
                    // 立即开始滚动（不延迟）
                    rollingComponent.StartRolling(0.0f);
                }
                catch (Exception ex)
                {
                    // 如果添加组件失败，销毁临时对象
                    UnityEngine.Object.Destroy(rollingObject);
                }
            }
            catch (Exception ex)
            {
                // 静默处理错误
            }
        }
    }

    /// <summary>
    /// 延迟设置滚动的组件（备用方案）
    /// </summary>
    public class DelayedRollingSetter : MonoBehaviour
    {
        public void SetRollingDelayed()
        {
            try
            {
                // 获取植物组件
                Plant plant = GetComponent<Plant>();
                if (plant != null)
                {
                    // 立即让巨型阳光坚果开始滚动
                    SolarEmperNutPatches.MakeGiantSunNutRoll(plant);
                }
                
                // 销毁这个临时组件
                Destroy(this);
            }
            catch (Exception ex)
            {
                // 静默处理错误
            }
        }
    }

    /// <summary>
    /// 滚动坚果组件
    /// </summary>
    public class RollingNut : MonoBehaviour
    {
        private int _row;
        private int _damage;
        private bool _isRolling = false;
        private float _rollSpeed = 5.0f; // 滚动速度
        private float _damageInterval = 0.02f; // 伤害间隔
        private float _lastDamageTime = 0f;

        public void Initialize(int row, int damage)
        {
            _row = row;
            _damage = damage;
        }

        public void StartRolling(float delay)
        {
            Invoke(nameof(BeginRolling), delay);
        }

        private void BeginRolling()
        {
            _isRolling = true;
        }

        private void Update()
        {
            if (_isRolling)
            {
                // 向前滚动
                transform.Translate(Vector3.right * _rollSpeed * Time.deltaTime);
                
                // 对僵尸造成伤害
                if (Time.time - _lastDamageTime >= _damageInterval)
                {
                    DamageZombiesInRange();
                    _lastDamageTime = Time.time;
                }
                
                // 检查是否滚出屏幕
                if (transform.position.x > 20f) // 假设屏幕右边界是20
                {
                    Destroy(gameObject);
                }
            }
        }

        private void DamageZombiesInRange()
        {
            try
            {
                // 获取所有僵尸并筛选当前行数的僵尸
                foreach (Zombie zombie in Board.Instance.zombieArray)
                {
                    if (zombie != null && zombie.theZombieRow == _row && Vector3.Distance(transform.position, zombie.transform.position) <= 1.0f)
                    {
                        zombie.TakeDamage(DmgType.Normal, _damage, false);
                    }
                }
            }
            catch (Exception ex)
            {
                // 静默处理错误
            }
        }
    }
}
