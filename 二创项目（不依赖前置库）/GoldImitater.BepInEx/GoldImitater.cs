using System;
using System.Collections.Generic;
using UnityEngine;

namespace GoldImitater.BepInEx
{
    /// <summary>
    /// 黄金模仿者核心逻辑组件
    /// 类名必须是 GoldImitater，因为动画事件会调用 GoldImitater.AnimSpawn()
    /// </summary>
    public class GoldImitater : MonoBehaviour
    {
        /// <summary>
        /// 游戏内植物 ID：黄金模仿者 = 1931
        /// </summary>
        public static int PlantID = 1931;

        public GoldImitater(IntPtr ptr) : base(ptr)
        {
        }

        /// <summary>
        /// 关联的模仿者组件（黄金模仿者基于 Imitater）
        /// </summary>
        public Imitater? plant => gameObject.GetComponent<Imitater>();

        /// <summary>
        /// 黄金模仿者变身逻辑 - 由动画事件调用
        /// </summary>
        public void AnimSpawn()
        {
            Core.Logger?.LogInfo("[GoldImitater] AnimSpawn 被动画事件调用");
            
            try
            {
                var imitater = plant;
                if (imitater == null)
                {
                    Core.Logger?.LogError("[GoldImitater] Imitater 组件为空");
                    return;
                }

                // 在调用 Die 之前保存所有需要的数据
                int thePlantRow = imitater.thePlantRow;
                int thePlantColumn = imitater.thePlantColumn;
                Vector3 axisPosition = imitater.axis != null ? imitater.axis.transform.position : Vector3.zero;
                float axisX = axisPosition.x;

                // 播放粒子效果
                ParticleManager.Instance?.SetParticle((ParticleType)11, axisPosition, thePlantRow);
                
                // 让模仿者死亡
                imitater.Die((Plant.DieReason)2);
                
                // 执行变身逻辑
                int num = UnityEngine.Random.Range(1, 101);
                Core.Logger?.LogInfo($"[GoldImitater] 随机数: {num}");
                
                if (num <= 44)
                {
                    SpawnNormalPlant(thePlantColumn, thePlantRow);
                }
                else if (num <= 64)
                {
                    SpawnUltimatePlant(thePlantColumn, thePlantRow);
                }
                else if (num <= 84)
                {
                    SpawnNormalZombie(thePlantRow, axisX);
                }
                else if (num <= 94)
                {
                    SpawnUltimateZombie(thePlantRow, axisX);
                }
                else if (num <= 99)
                {
                    SpawnBossZombie(thePlantRow, axisX);
                }
                else
                {
                    TryDrawTravelEntry(thePlantColumn, thePlantRow);
                }
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[GoldImitater] AnimSpawn 执行失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void SpawnNormalPlant(int column, int row)
        {
            try
            {
                var allPlants = GameAPP.resourcesManager.allPlants;
                var validPlants = new List<PlantType>();
                
                foreach (var pt in allPlants)
                {
                    if (pt != (PlantType)(-1) && pt != (PlantType)250 && pt != (PlantType)938 && 
                        pt != (PlantType)246 && pt != (PlantType)247 && pt != (PlantType)257 && 
                        pt != (PlantType)258 && pt != (PlantType)259 && pt != (PlantType)260 && 
                        !Lawnf.IsUltiPlant(pt))
                    {
                        validPlants.Add(pt);
                    }
                }
                
                if (validPlants.Count > 0)
                {
                    PlantType plantType = validPlants[UnityEngine.Random.Range(0, validPlants.Count)];
                    Core.Logger?.LogInfo($"[GoldImitater] 召唤普通植物: {plantType}");
                    CreatePlant.Instance?.SetPlant(column, row, plantType, null, default, true, true, null);
                }
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[GoldImitater] 召唤普通植物失败: {ex.Message}");
            }
        }

        private void SpawnUltimatePlant(int column, int row)
        {
            try
            {
                var allPlants = GameAPP.resourcesManager.allPlants;
                var ultiPlants = new List<PlantType>();
                
                foreach (var pt in allPlants)
                {
                    if (Lawnf.IsUltiPlant(pt) && pt != (PlantType)938 && pt != (PlantType)(-1) && 
                        pt != (PlantType)250 && pt != (PlantType)246 && pt != (PlantType)247 && 
                        pt != (PlantType)257 && pt != (PlantType)258 && pt != (PlantType)259 && 
                        pt != (PlantType)260)
                    {
                        ultiPlants.Add(pt);
                    }
                }
                
                if (ultiPlants.Count > 0)
                {
                    PlantType plantType = ultiPlants[UnityEngine.Random.Range(0, ultiPlants.Count)];
                    Core.Logger?.LogInfo($"[GoldImitater] 召唤究极植物: {plantType}");
                    CreatePlant.Instance?.SetPlant(column, row, plantType, null, default, true, true, null);
                }
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[GoldImitater] 召唤究极植物失败: {ex.Message}");
            }
        }

        private void SpawnNormalZombie(int row, float axisX)
        {
            try
            {
                var allZombies = GameAPP.resourcesManager.allZombieTypes;
                var validZombies = new List<ZombieType>();
                
                foreach (var zt in allZombies)
                {
                    if (zt != (ZombieType)(-1) && zt != (ZombieType)46 && zt != (ZombieType)228 && 
                        zt != (ZombieType)54 && !TypeMgr.IsBossZombie(zt))
                    {
                        validZombies.Add(zt);
                    }
                }
                
                if (validZombies.Count > 0)
                {
                    ZombieType zombieType = validZombies[UnityEngine.Random.Range(0, validZombies.Count)];
                    Core.Logger?.LogInfo($"[GoldImitater] 召唤普通僵尸: {zombieType}");
                    SpawnZombieInternal(zombieType, row, axisX);
                }
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[GoldImitater] 召唤普通僵尸失败: {ex.Message}");
            }
        }

        private void SpawnUltimateZombie(int row, float axisX)
        {
            try
            {
                var allZombies = GameAPP.resourcesManager.allZombieTypes;
                var ultiZombies = new List<ZombieType>();
                
                foreach (var zt in allZombies)
                {
                    if (TypeMgr.UltimateZombie(zt) && !TypeMgr.IsBossZombie(zt) && 
                        zt != (ZombieType)(-1) && zt != (ZombieType)46 && zt != (ZombieType)228 && 
                        zt != (ZombieType)54)
                    {
                        ultiZombies.Add(zt);
                    }
                }
                
                if (ultiZombies.Count > 0)
                {
                    ZombieType zombieType = ultiZombies[UnityEngine.Random.Range(0, ultiZombies.Count)];
                    Core.Logger?.LogInfo($"[GoldImitater] 召唤究极僵尸: {zombieType}");
                    SpawnZombieInternal(zombieType, row, axisX);
                }
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[GoldImitater] 召唤究极僵尸失败: {ex.Message}");
            }
        }

        private void SpawnBossZombie(int row, float axisX)
        {
            try
            {
                var allZombies = GameAPP.resourcesManager.allZombieTypes;
                var bossZombies = new List<ZombieType>();
                
                foreach (var zt in allZombies)
                {
                    if (TypeMgr.IsBossZombie(zt) && zt != (ZombieType)(-1) && 
                        zt != (ZombieType)46 && zt != (ZombieType)228 && zt != (ZombieType)54)
                    {
                        bossZombies.Add(zt);
                    }
                }
                
                if (bossZombies.Count > 0)
                {
                    ZombieType zombieType = bossZombies[UnityEngine.Random.Range(0, bossZombies.Count)];
                    Core.Logger?.LogInfo($"[GoldImitater] 召唤Boss僵尸: {zombieType}");
                    SpawnZombieInternal(zombieType, row, axisX);
                }
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[GoldImitater] 召唤Boss僵尸失败: {ex.Message}");
            }
        }

        private void SpawnZombieInternal(ZombieType zombieType, int row, float axisX)
        {
            try
            {
                if (zombieType == (ZombieType)44)
                {
                    GameObject? gameObject = CreateZombie.Instance?.SetZombie(0, zombieType, axisX, false);
                    if (gameObject != null)
                    {
                        var zombie = gameObject.GetComponent<Zombie>();
                        if (zombie != null)
                        {
                            zombie.theHealth *= 10;
                            zombie.theMaxHealth *= 10;
                            zombie.UpdateHealthText();
                        }
                    }
                    GameAPP.Instance?.PlayMusic((MusicType)18);
                }
                else
                {
                    CreateZombie.Instance?.SetZombie(row, zombieType, axisX, false);
                }
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[GoldImitater] 召唤僵尸失败: {ex.Message}");
            }
        }

        private void TryDrawTravelEntry(int column, int row)
        {
            try
            {
                TravelMgr? travel = TravelMgr.Instance;
                
                if (travel == null)
                {
                    Core.Logger?.LogInfo("[GoldImitater] 没有旅行模式，重新生成黄金模仿者");
                    CreatePlant.Instance?.SetPlant(column, row, (PlantType)PlantID, null, default, true, true, null);
                    return;
                }

                var candidates = new List<int>();
                if (travel.advancedUpgrades != null)
                {
                    for (int i = 0; i < travel.advancedUpgrades.Count; i++)
                    {
                        if (!travel.advancedUpgrades[i])
                        {
                            candidates.Add(i);
                        }
                    }
                }

                if (candidates.Count > 0)
                {
                    CreatePlant.Instance?.SetPlant(column, row, (PlantType)PlantID, null, default, true, true, null);
                    if (travel.advancedUpgrades != null)
                    {
                        travel.advancedUpgrades[candidates[UnityEngine.Random.Range(0, candidates.Count)]] = true;
                    }
                    InGameText.Instance?.ShowText("窝给你抽个词条", 3f, false);
                    Core.Logger?.LogInfo("[GoldImitater] 成功抽取旅行词条");
                }
                else
                {
                    Core.Logger?.LogInfo("[GoldImitater] 没有可抽的词条，重新生成黄金模仿者");
                    CreatePlant.Instance?.SetPlant(column, row, (PlantType)PlantID, null, default, true, true, null);
                }
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[GoldImitater] 抽取词条失败: {ex.Message}");
            }
        }
    }
}
