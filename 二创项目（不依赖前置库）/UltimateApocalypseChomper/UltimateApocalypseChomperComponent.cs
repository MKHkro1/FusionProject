using System;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace UltimateApocalypseChomper.BepInEx
{
    public class UltimateApocalypseChomperComponent : MonoBehaviour
    {
        public int SwallowStacks { get; private set; }

        public UltimateApocalypseChomperComponent() : base(ClassInjector.DerivedConstructorPointer<UltimateApocalypseChomperComponent>())
        {
            ClassInjector.DerivedConstructorBody(this);
        }

        public UltimateApocalypseChomperComponent(IntPtr ptr) : base(ptr) { }

        private UltimateChomper? Plant
        {
            get
            {
                try
                {
                    if (this == null || gameObject == null) return null;
                    return gameObject.GetComponent<UltimateChomper>();
                }
                catch { return null; }
            }
        }

        private void Awake() => TryInitialize();
        private void Start() => TryInitialize();

        private void Update()
        {
            try
            {
                // 先检查游戏状态，避免在非游戏状态下执行
                if (GameAPP.theGameStatus != GameStatus.InGame) return;
                
                var plant = Plant;
                if (plant == null || ((UnityEngine.Object)plant) == null) return;
                
                // 检查植物是否已被销毁
                if (plant.gameObject == null) return;
                
                UpdateDigestState(plant);
                UpdateSwallowBurst(plant);
                EnforceHealthLimits(plant);
                UpdateUndyingState(plant);
                EnsureAntiCrashFlags(plant);
                ValidateCurrentTarget(plant);
                EnsureActiveTarget(plant);
            }
            catch (Exception ex)
            {
                // 记录异常但不崩溃
                Core.PluginLog?.LogWarning($"[究极天启樱龙] Update 异常：{ex.Message}");
            }
        }

        private void TryInitialize()
        {
            if (_initialized) return;
            try
            {
                var plant = Plant;
                if (plant == null) return;

                plant.thePlantType = (PlantType)Core.PlantId;
                if (plant.thePlantMaxHealth < Core.PlantToughness)
                    plant.thePlantMaxHealth = Core.PlantToughness;
                if (plant.thePlantHealth < Core.PlantToughness)
                    plant.thePlantHealth = Core.PlantToughness;
                plant.attackDamage = Core.BaseAttackDamage;
                EnsureShootAndAxis(plant);
                EnsureAntiCrashFlags(plant);
                _swallowCooldown = 5f;
                _swallowCooldownInitialized = true;
                _initialized = true;
            }
            catch { }
        }

        internal bool PerformGroupAttack()
        {
            try
            {
                var plant = Plant;
                if (plant == null) return false;
                if (_digestTimer > 0f)
                {
                    try { plant.targetZombie = null; } catch { }
                    return true;
                }
                EnsureShootAndAxis(plant);
                CollectTargets(plant);
                if (_targetCache.Count == 0)
                {
                    try { if (plant.targetZombie == null) EnsureActiveTarget(plant); } catch { }
                    return false;
                }
                bool flag = false;
                Zombie[] targetArray = _targetCache.ToArray();
                foreach (var zombie in targetArray)
                {
                    if (zombie == null || IsZombieInvalid(zombie)) continue;
                    try
                    {
                        int num = CalculateBiteDamage(zombie);
                        num = ApplyDamageModifiers(num);
                        int burstDamage = Mathf.Max(1, Mathf.RoundToInt(num * 0.3f));  // 30% 爆裂子弹伤害
                        int splashDamage = Mathf.Max(1, Mathf.RoundToInt(num * 0.03f)); // 3% 裂片溅射伤害
                        ApplyDirectDamage(zombie, num);
                        HealPlant(plant, num);
                        DealSplashDamage(zombie, splashDamage, 1.5f);  // 裂片溅射 3%
                        SpawnCherryProjectiles(plant, zombie, burstDamage);  // 爆裂子弹 30%
                        flag = true;
                    }
                    catch { }
                }
                if (!flag)
                {
                    try { plant.theStatus = 0; plant.attributeCountdown = 0f; } catch { }
                    _targetCache.Clear();
                    EnsureActiveTarget(plant);
                    return false;
                }
                TrySwallow(plant);
                try { GameAPP.PlaySound(UnityEngine.Random.Range(3, 5), 0.5f, 1f); } catch { }
                _targetCache.Clear();
                try { plant.targetZombie = null; plant.theStatus = 0; } catch { }
                return true;
            }
            catch (Exception ex)
            {
                Core.PluginLog?.LogError($"[究极天启樱龙] PerformGroupAttack 异常：{ex}");
                return false;
            }
        }

        private static System.Collections.Generic.List<Zombie> SafeGetAllZombies()
        {
            // 避免使用 ThreadLocal，直接使用静态缓存（Unity 主线程单线程执行）
            if (_zombieSnapshotCache == null)
                _zombieSnapshotCache = new System.Collections.Generic.List<Zombie>(64);
            
            int frameCount;
            try { frameCount = Time.frameCount; } catch { return new System.Collections.Generic.List<Zombie>(); }
            if (_lastSnapshotFrame == frameCount) return _zombieSnapshotCache;

            _zombieSnapshotCache.Clear();
            try
            {
                if (GameAPP.theGameStatus != GameStatus.InGame) { _lastSnapshotFrame = frameCount; return _zombieSnapshotCache; }
                var allZombies = Lawnf.GetAllZombies();
                if (allZombies == null) { _lastSnapshotFrame = frameCount; return _zombieSnapshotCache; }
                
                // 先获取数量，避免遍历时列表被修改
                int count = allZombies.Count;
                for (int i = 0; i < count; i++)
                {
                    try 
                    { 
                        if (i >= allZombies.Count) break;  // 防止遍历时列表缩小
                        var z = allZombies[i]; 
                        if (z != null && ((UnityEngine.Object)z) != null) 
                            _zombieSnapshotCache.Add(z); 
                    } 
                    catch { }
                }
            }
            catch { _zombieSnapshotCache.Clear(); }
            _lastSnapshotFrame = frameCount;
            return _zombieSnapshotCache;
        }
        
        // 静态缓存替代 ThreadLocal
        private static System.Collections.Generic.List<Zombie>? _zombieSnapshotCache;
        private static int _lastSnapshotFrame = -1;

        private static bool IsZombieInvalid(Zombie? zombie)
        {
            if (zombie == null || ((UnityEngine.Object?)zombie) == null) return true;
            try { return zombie.beforeDying || zombie.isMindControlled; } catch { return true; }
        }

        private void UpdateDigestState(UltimateChomper plant)
        {
            if (plant == null) return;
            try
            {
                float dt = Time.deltaTime;
                if (_digestTimer > 0f)
                {
                    _digestTimer -= dt;
                    plant.attributeCountdown = Mathf.Max(_digestTimer, 0f);
                    if (plant.thePlantHealth < 1) plant.thePlantHealth = 1;
                    return;
                }
                if (plant.attributeCountdown > 0f)
                    plant.attributeCountdown = Mathf.Max(0f, plant.attributeCountdown - dt);
            }
            catch { }
        }

        private void UpdateSwallowBurst(UltimateChomper plant)
        {
            if (plant == null) return;
            try
            {
                float dt = Time.deltaTime;
                if (!_swallowCooldownInitialized) { _swallowCooldown = 5f; _swallowCooldownInitialized = true; }
                if (_swallowCooldown > 0f) { _swallowCooldown = Mathf.Max(0f, _swallowCooldown - dt); return; }
                if (_initialBurstTriggered)
                {
                    bool hasTarget = false;
                    var list = SafeGetAllZombies();
                    for (int i = 0; i < list.Count; i++)
                        if (IsValidTarget(plant, list[i]) && CanSwallow(list[i])) { hasTarget = true; break; }
                    if (!hasTarget) { _swallowCooldown = 5f; return; }
                }
                TryTriggerCherryBurst(plant);
            }
            catch { }
        }

        private void EnforceHealthLimits(UltimateChomper plant)
        {
            if (plant == null) return;
            try
            {
                if (plant.thePlantMaxHealth != Core.PlantToughness) plant.thePlantMaxHealth = Core.PlantToughness;
                if (plant.thePlantHealth > 160000) plant.thePlantHealth = 160000;
                
                // 只有在不死状态中才保持血量为1，否则让植物正常死亡
                if (plant.thePlantHealth < 1)
                {
                    if (_undyingTimer > 0f)
                    {
                        plant.thePlantHealth = 1;  // 不死状态中保持1点血
                    }
                    // 否则不干预，让游戏正常处理死亡
                }
                
                if (plant.thePlantHealth > Core.PlantToughness)
                {
                    float drain = 1600f * Time.deltaTime;
                    plant.thePlantHealth = Mathf.Max(Core.PlantToughness, plant.thePlantHealth - (int)drain);
                }
            }
            catch { }
        }

        private void UpdateUndyingState(UltimateChomper plant)
        {
            if (plant == null) return;
            try
            {
                float dt = Time.deltaTime;
                if (_undyingTimer > 0f)
                {
                    _undyingTimer -= dt;
                    if (_undyingTimer <= 0f) { _undyingTimer = 0f; _undyingCooldown = 5f; }
                }
                else if (_undyingCooldown > 0f)
                {
                    _undyingCooldown -= dt;
                    if (_undyingCooldown < 0f) _undyingCooldown = 0f;
                }
                if (_undyingTimer > 0f && plant.thePlantHealth < 1) plant.thePlantHealth = 1;
            }
            catch { }
        }

        private static void EnsureAntiCrashFlags(UltimateChomper plant)
        {
            if (plant == null) return;
            try
            {
                if (plant.jigsawType == null) plant.jigsawType = new Il2CppSystem.Collections.Generic.List<JigsawType>();
                // 只添加防碾压(7)，不添加承伤(6)
                if (!plant.jigsawType.Contains((JigsawType)7)) plant.jigsawType.Add((JigsawType)7);
            }
            catch { }
        }

        private void EnsureActiveTarget(UltimateChomper plant)
        {
            if (plant == null) return;
            try
            {
                if (plant.targetZombie != null && IsValidTarget(plant, plant.targetZombie)) return;
                plant.targetZombie = null;
                try { plant.ChomperSearchZombie(null); if (plant.targetZombie != null && IsValidTarget(plant, plant.targetZombie)) return; } catch { }
                Zombie? best = null; float minX = float.MaxValue;
                var list = SafeGetAllZombies();
                for (int i = 0; i < list.Count; i++)
                {
                    var z = list[i];
                    if (IsValidTarget(plant, z))
                    {
                        try
                        {
                            var pos = z.axis != null ? z.axis.position : z.transform.position;
                            if (pos.x < minX) { best = z; minX = pos.x; }
                        }
                        catch { }
                    }
                }
                if (best != null) plant.targetZombie = best;
            }
            catch { }
        }

        private void ValidateCurrentTarget(UltimateChomper plant)
        {
            if (plant == null || plant.targetZombie == null) return;
            try { if (!IsValidTarget(plant, plant.targetZombie)) plant.targetZombie = null; } catch { }
        }

        private void CollectTargets(UltimateChomper plant)
        {
            if (plant == null) return;
            try
            {
                _targetCache.Clear();
                var list = SafeGetAllZombies();
                for (int i = 0; i < list.Count; i++) if (IsValidTarget(plant, list[i])) _targetCache.Add(list[i]);
                _targetCache.Sort((a, b) =>
                {
                    if (a == null || b == null) return 0;
                    try
                    {
                        float ax = a.axis != null ? a.axis.position.x : a.transform.position.x;
                        float bx = b.axis != null ? b.axis.position.x : b.transform.position.x;
                        return ax.CompareTo(bx);
                    }
                    catch { return 0; }
                });
                if (_targetCache.Count > 0) try { plant.targetZombie = _targetCache[0]; } catch { }
            }
            catch { }
        }


        private bool IsValidTarget(UltimateChomper plant, Zombie? zombie)
        {
            if (IsZombieInvalid(zombie)) return false;
            if (plant == null || plant.transform == null) return false;
            if (zombie!.col == null || !zombie.col.enabled) return false;
            if (zombie.theZombieRow != plant.thePlantRow) return false;
            try
            {
                var zpos = zombie.axis != null ? zombie.axis.position : zombie.transform.position;
                float dx = zpos.x - plant.transform.position.x;
                if (dx < 0f) return false;
                float range = EffectiveAttackRange;
                if (dx > range * 1.5f) return false;
                return true;
            }
            catch { return false; }
        }

        private static void EnsureShootAndAxis(UltimateChomper plant)
        {
            if (plant == null) return;
            try
            {
                if (plant.shoot == null && plant.transform != null)
                {
                    var t = plant.transform.Find("Shoot") ?? plant.transform.Find("shoot");
                    if (t != null) plant.shoot = t;
                }
                if (plant.axis == null && plant.transform != null)
                    plant.axis = plant.shoot ?? plant.transform;
            }
            catch { }
        }

        private int CalculateBiteDamage(Zombie zombie)
        {
            try
            {
                // 僵尸总最大血量 = 本体 + 一层护甲 + 二层护甲
                int maxHealth = Math.Max(1, zombie.theMaxHealth + zombie.theFirstArmorMaxHealth + zombie.theSecondArmorMaxHealth);
                int percent = Mathf.RoundToInt((6f + 0.4f * SwallowStacks) / 100f * maxHealth);
                return Core.BaseAttackDamage + percent;
            }
            catch { return Core.BaseAttackDamage; }
        }

        /// <summary>
        /// 直接扣除僵尸血量（绕过护甲和减伤机制）
        /// 按顺序扣除：二层护甲 -> 一层护甲 -> 本体血量
        /// </summary>
        private static void ApplyDirectDamage(Zombie zombie, int damage)
        {
            if (zombie == null || damage <= 0) return;
            try
            {
                int remaining = damage;

                // 先扣二层护甲
                if (remaining > 0 && zombie.theSecondArmorHealth > 0)
                {
                    int armorDmg = Math.Min(remaining, zombie.theSecondArmorHealth);
                    zombie.theSecondArmorHealth -= armorDmg;
                    remaining -= armorDmg;
                }

                // 再扣一层护甲
                if (remaining > 0 && zombie.theFirstArmorHealth > 0)
                {
                    int armorDmg = Math.Min(remaining, zombie.theFirstArmorHealth);
                    zombie.theFirstArmorHealth -= armorDmg;
                    remaining -= armorDmg;
                }

                // 最后扣本体血量
                if (remaining > 0)
                {
                    zombie.theHealth -= remaining;
                    if (zombie.theHealth <= 0)
                    {
                        zombie.theHealth = 0;
                        try { zombie.Die(); } catch { }
                    }
                }
            }
            catch { }
        }

        private static void DealSplashDamage(Zombie origin, int damage, float radius)
        {
            if (damage <= 0 || IsZombieInvalid(origin)) return;
            try
            {
                var pos = origin.axis != null ? origin.axis.position : origin.transform.position;
                var list = SafeGetAllZombies();
                float rSq = radius * radius;
                for (int i = 0; i < list.Count; i++)
                {
                    var z = list[i];
                    if (!IsZombieInvalid(z))
                    {
                        try
                        {
                            var zp = z.axis != null ? z.axis.position : z.transform.position;
                            float dx = zp.x - pos.x, dy = zp.y - pos.y;
                            if (dx * dx + dy * dy <= rSq)
                                try { z.TakeDamage((DmgType)0, damage); } catch { }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        private void TrySwallow(UltimateChomper plant)
        {
            if (plant == null) return;
            try
            {
                _swallowCache.Clear();
                float totalHp = 0f, totalDmg = 0f;
                var list = SafeGetAllZombies();
                for (int i = 0; i < list.Count; i++)
                {
                    var z = list[i];
                    if (IsValidTarget(plant, z) && CanSwallow(z))
                    {
                        _swallowCache.Add(z);
                        totalHp += GetZombieTotalHealth(z);
                    }
                }
                if (_swallowCache.Count == 0) return;
                bool swallowed = false;
                foreach (var z in _swallowCache)
                {
                    if (!IsZombieInvalid(z))
                    {
                        totalDmg += CalculateBiteDamage(z);
                        if (ExecuteSwallow(plant, z)) swallowed = true;
                    }
                }
                if (swallowed)
                {
                    float digestTime = CalculateDigestTimer(Mathf.Max(1f, totalHp));
                    SwallowStacks += _swallowCache.Count;
                    _digestTimer = digestTime;
                    try { plant.attributeCountdown = _digestTimer; plant.theStatus = 0; } catch { }
                    HealPlant(plant, Core.PlantToughness + totalHp);
                    CreateFrontExplosion(plant, Mathf.Max(1, Mathf.RoundToInt(totalDmg * 0.5f)));
                    SetSwallowCooldown(totalHp);
                }
            }
            catch (Exception ex) { Core.PluginLog?.LogError($"[究极天启樱龙] TrySwallow 异常：{ex}"); }
            finally { _swallowCache.Clear(); }
        }

        private static bool CanSwallow(Zombie? zombie)
        {
            if (IsZombieInvalid(zombie)) return false;
            try { return !TypeMgr.IsBossZombie(zombie!.theZombieType) && !TypeMgr.IsDriverZombie(zombie.theZombieType); }
            catch { return false; }
        }

        private bool ExecuteSwallow(UltimateChomper plant, Zombie zombie)
        {
            if (plant == null) return false;
            try { plant.Chomp(zombie); return true; }
            catch { ForceKill(zombie); return false; }
        }

        private static void ForceKill(Zombie zombie)
        {
            if (IsZombieInvalid(zombie)) return;
            try
            {
                int dmg = zombie.theHealth + zombie.theFirstArmorHealth + zombie.theSecondArmorHealth + 100000;
                try { zombie.TakeDamage((DmgType)1, dmg); } catch { }
            }
            catch { }
        }

        private static float GetZombieTotalHealth(Zombie? zombie)
        {
            if (zombie == null) return 0f;
            try { return zombie.theHealth + zombie.theFirstArmorHealth + zombie.theSecondArmorHealth; }
            catch { return 0f; }
        }

        private bool TryTriggerCherryBurst(UltimateChomper plant)
        {
            if (plant == null) return false;
            try
            {
                _burstCache.Clear();
                float totalHp = 0f, totalDmg = 0f;
                var list = SafeGetAllZombies();
                for (int i = 0; i < list.Count; i++)
                {
                    var z = list[i];
                    if (IsValidTarget(plant, z) && CanSwallow(z))
                    {
                        _burstCache.Add(z);
                        totalHp += GetZombieTotalHealth(z);
                        totalDmg += CalculateBiteDamage(z);
                    }
                }
                foreach (var z in _burstCache) if (z != null) ForceKill(z);
                if (_burstCache.Count == 0)
                {
                    var list2 = SafeGetAllZombies();
                    for (int k = 0; k < list2.Count; k++)
                        if (IsValidTarget(plant, list2[k])) totalDmg += CalculateBiteDamage(list2[k]);
                }
                CreateFrontExplosion(plant, Mathf.Max(1, Mathf.RoundToInt(totalDmg * 0.5f)));
                _initialBurstTriggered = true;
                if (_burstCache.Count > 0)
                {
                    HealPlant(plant, Core.PlantToughness + totalHp);
                    SwallowStacks += _burstCache.Count;  // 按吞噬数量增加层数
                    SetSwallowCooldown(totalHp);
                }
                else _swallowCooldown = 40f;
                return true;
            }
            catch (Exception ex) { Core.PluginLog?.LogError($"[究极天启樱龙] TryTriggerCherryBurst 异常：{ex}"); return false; }
            finally { _burstCache.Clear(); }
        }

        private void SetSwallowCooldown(float totalHealth)
        {
            try
            {
                float hp = Mathf.Max(1f, totalHealth);
                float cd = HasGluttonyBuff ? Mathf.Min(15f, 5f + Mathf.Log(hp)) : Mathf.Min(40f, 8f + 3f * Mathf.Log(hp));
                _swallowCooldown = Mathf.Max(0f, cd);
            }
            catch { _swallowCooldown = HasGluttonyBuff ? 5f : 8f; }
        }

        private void CreateFrontExplosion(UltimateChomper plant, int damage)
        {
            if (damage <= 0 || plant == null || Board.Instance == null) return;
            try
            {
                var t = plant.axis ?? plant.transform;
                if (t == null) return;
                var pos = t.position;
                int row = plant.thePlantRow;
                Board.Instance.CreateCherryExplode(new Vector2(pos.x + 1.5f, pos.y), row, 0, damage);
            }
            catch (Exception ex) { Core.PluginLog?.LogWarning($"[究极天启樱龙] CreateCherryExplode 失败：{ex.Message}"); }
        }

        private void SpawnCherryProjectiles(UltimateChomper plant, Zombie target, int burstDamage)
        {
            if (plant == null || CreateBullet.Instance == null || plant.transform == null) return;
            try
            {
                Vector3 pos = plant.shoot != null ? plant.shoot.position : (plant.axis != null ? plant.axis.position : plant.transform.position);
                var bullet = CreateBullet.Instance.SetBullet(pos.x, pos.y, plant.thePlantRow, (BulletType)3, 0);
                if (bullet != null)
                {
                    bullet.Damage = Mathf.Max(1, burstDamage);
                    bullet.targetZombie = target;
                    bullet.from = plant;
                    bullet.melonSputter = true;
                    bullet.Vx = 4.5f;
                    bullet.Vy = 0f;
                }
            }
            catch (Exception ex) { Core.PluginLog?.LogWarning($"[究极天启樱龙] 生成樱桃子弹异常：{ex.Message}"); }
        }

        internal void HealPlant(UltimateChomper plant, float amount)
        {
            if (plant == null) return;
            try
            {
                if (HasJudgementBuff) amount *= 3f;
                plant.thePlantHealth = Mathf.Min(160000, plant.thePlantHealth + Mathf.RoundToInt(amount));
            }
            catch { }
        }

        public bool HandleIncomingDamage(Plant plant, ref int damage)
        {
            try
            {
                if (plant == null) return true;
                int hp = plant.thePlantHealth;
                
                // 不死状态中：血量最低保持1点
                if (_undyingTimer > 0f)
                {
                    if (hp - damage < 1) damage = Mathf.Max(0, hp - 1);
                    return true;
                }
                
                // 即将死亡且不死冷却已结束：触发不死状态
                if (hp - damage <= 0 && _undyingCooldown <= 0f)
                {
                    _undyingTimer = 5f;  // 不死状态持续5秒
                    _undyingCooldown = 0f;
                    try { plant.thePlantHealth = 1; } catch { return true; }
                    damage = 0;
                    return false;
                }
                
                // 即将死亡但不死冷却中：正常死亡（让原方法执行）
                // 不做任何处理，返回 true 让游戏正常处理死亡
                
                return true;
            }
            catch (Exception ex) { Core.PluginLog?.LogError($"[究极天启樱龙] HandleIncomingDamage 异常：{ex}"); return true; }
        }

        private float CalculateDigestTimer(float totalHealth)
        {
            float hp = Mathf.Max(1f, totalHealth);
            try { return HasGluttonyBuff ? Mathf.Clamp(5f + Mathf.Log(hp), 5f, 15f) : Mathf.Clamp(8f + 3f * Mathf.Log(hp), 8f, 40f); }
            catch { return HasGluttonyBuff ? 5f : 8f; }
        }

        private int ApplyDamageModifiers(int damage)
        {
            try { if (HasJudgementBuff && _undyingTimer > 0f) damage = Mathf.RoundToInt(damage * 4f); }
            catch { }
            return damage;
        }

        private float EffectiveAttackRange => HasGluttonyBuff ? 3f : 1.5f;
        private bool HasGluttonyBuff => SafeCheckTravelBuff(Core.TravelBuffGluttonyId);
        private bool HasJudgementBuff => SafeCheckTravelBuff(Core.TravelBuffJudgementId);

        private static bool SafeCheckTravelBuff(int buffId)
        {
            if (buffId < 0) return false;
            try { return Lawnf.TravelUltimate(buffId); }
            catch { return false; }
        }

        // 缓存列表
        private readonly System.Collections.Generic.List<Zombie> _targetCache = new();
        private readonly System.Collections.Generic.List<Zombie> _burstCache = new();
        private readonly System.Collections.Generic.List<Zombie> _swallowCache = new();

        // 状态变量
        private bool _initialized;
        private float _digestTimer;
        private float _swallowCooldown = 5f;
        private bool _swallowCooldownInitialized;
        private bool _initialBurstTriggered;
        private float _undyingTimer;
        private float _undyingCooldown;
    }
}
