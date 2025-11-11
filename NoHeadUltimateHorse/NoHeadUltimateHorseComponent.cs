using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using CustomizeLib.BepInEx;

namespace NoHeadUltimateHorse.BepInEx
{

    public class NoHeadUltimateHorseComponent : MonoBehaviour
    {

        public static int ZombieID = 450;

        public UltimateHorse zombie
        {
            get
            {
                UltimateHorse ultimateHorse;
                return base.gameObject.TryGetComponent<UltimateHorse>(out ultimateHorse) ? ultimateHorse : null!;
            }
        }

		public void OnDisable()
		{
			try
			{
				this.CancelInvoke();
			}
			catch { }
		}

		public void OnDestroy()
		{
			try
			{
				this.CancelInvoke();
			}
			catch { }
		}

        public bool hasEntered = false;


        public bool isInvincible = false;

        public float slashCooldown = 5f;

        public float chargeCooldown = 15f;


        public float chargeStunTimer = 0f;

        public bool isCharging = false;

        public float healTimer = 0f;

        private Transform? shootTransform = null;

        public void Awake()
        {
            try
            {
                // 初始化基本状态
                this.isInvincible = true; // 入场时无敌
                this.hasEntered = false;
                this.slashCooldown = 0f; // 可以立即挥砍
                this.chargeCooldown = 0f; // 可以立即冲撞
                this.healTimer = 0f;
                this.chargeStunTimer = 0f;
                this.isCharging = false;

                // 检查zombie是否可用，只在游戏进行中时初始化
                if (GameAPP.theGameStatus == GameStatus.InGame && this.zombie != null)
                {
                    // 初始化僵尸状态
                    this.zombie.theStatus = (ZombieStatus)36; // UltimateHorse特殊状态
                    
                    // 查找射击点
                    this.FindShootTransform();
                }
                else if (this.zombie == null)
                {
                    Core.Instance?.Logger.LogWarning("无头终极马僵尸插件: Awake时zombie为null");
                }
            }
            catch (Exception ex)
            {
                Core.Instance?.Logger.LogError($"无头终极马僵尸插件: Awake失败: {ex.Message}");
                Core.Instance?.Logger.LogError($"无头终极马僵尸插件: 堆栈跟踪: {ex.StackTrace}");
            }
        }

        public void Start()
        {
            try
            {
                // 确保游戏进行中且僵尸存在
                if (this.zombie != null)
                {
                    // 显式设置僵尸类型
                    this.zombie.theZombieType = (ZombieType)NoHeadUltimateHorseComponent.ZombieID;
                    
                    // 确保GameObject激活
                    if (!base.gameObject.activeSelf)
                    {
                        Core.Instance?.Logger.LogWarning("无头终极马僵尸插件: GameObject未激活，尝试激活");
                        base.gameObject.SetActive(true);
                    }
                    
                    // 确保血量正确设置
                    if (this.zombie.theMaxHealth != 54000)
                    {
                        this.zombie.theMaxHealth = 54000;
                    }
                    
                    // 确保当前血量至少等于最大血量（防止死亡）
                    if (this.zombie.theHealth < this.zombie.theMaxHealth)
                    {
                        this.zombie.theHealth = this.zombie.theMaxHealth;
                    }
                    
                    // 更新血量显示
                    this.zombie.UpdateHealthText();
                    this.ConfigureLeaderHealthDisplay();
                    
                    // 确保无敌状态已设置
                    if (!this.isInvincible)
                    {
                        Core.Instance?.Logger.LogWarning("无头终极马僵尸插件: Start时无敌状态未设置，重新设置");
                        this.isInvincible = true;
                    }
                    
                    // 确保僵尸可见性 - 检查所有渲染器
                    this.EnsureZombieVisibility();
                    
                    // 只在游戏进行中时执行游戏逻辑
                    if (GameAPP.theGameStatus == GameStatus.InGame)
                    {
                        // 设置领袖僵尸属性
                        this.SetLeaderZombie();
                        
                        // 处理入场逻辑
                        if (!this.hasEntered)
                        {
                            this.hasEntered = true;
                            
                            // 延迟执行投掷长斧，确保所有对象完全初始化
                            this.Invoke("ThrowAxeOnEnter", 0.3f);
                            
                            // 3秒后取消无敌
                            this.Invoke("DisableInvincibility", 3f);
                        }
                    }
                    else
                    {
                    }
                }
                else
                {
                    Core.Instance?.Logger.LogWarning("无头终极马僵尸插件: Start时zombie为null");
                    Core.Instance?.Logger.LogWarning($"无头终极马僵尸插件: GameObject: {base.gameObject.name}, 组件列表: {string.Join(", ", base.gameObject.GetComponents<Component>().Select(c => c.GetType().Name))}");
                }
            }
            catch (Exception ex)
            {
                Core.Instance?.Logger.LogError($"无头终极马僵尸插件: Start失败: {ex.Message}");
                Core.Instance?.Logger.LogError($"无头终极马僵尸插件: 堆栈跟踪: {ex.StackTrace}");
            }
        }

        private void SetLeaderZombie()
        {
            try
            {
            }
            catch (Exception ex)
            {
                Core.Instance?.Logger.LogError($"无头终极马僵尸插件: 设置领袖僵尸失败: {ex.Message}");
            }
        }

        public void SetWalkAnimation()
        {
            try
            {
                if (this.zombie != null && this.zombie.anim != null)
                {
                    // 只在游戏状态下设置walk动画
                    if (GameAPP.theGameStatus == GameStatus.InGame)
                    {
                        this.zombie.anim.SetTrigger("walk");
                        this.walkAnimationSet = true;
                        this.walkCheckTimer = 0f; // 重置检查计时器，开始持续检查
                        
                        // 确保僵尸状态正确
                        if (this.zombie.theSpeed == 0f)
                        {
                            // 如果速度为0，可能需要设置一个初始速度
                            // 但这取决于UltimateHorse的实现，暂时不修改
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Core.Instance?.Logger.LogError($"无头终极马僵尸插件: 设置walk动画失败: {ex.Message}");
            }
        }

        private void EnsureZombieVisibility()
        {
            try
            {
                if (base.gameObject == null)
                {
                    Core.Instance?.Logger.LogWarning("无头终极马僵尸插件: EnsureZombieVisibility: gameObject为null");
                    return;
                }

                // 确保GameObject激活
                if (!base.gameObject.activeSelf)
                {
                    Core.Instance?.Logger.LogWarning("无头终极马僵尸插件: GameObject未激活，尝试激活");
                    base.gameObject.SetActive(true);
                }

                // 检查并启用所有SpriteRenderer
                SpriteRenderer[] renderers = base.gameObject.GetComponentsInChildren<SpriteRenderer>(true);
                foreach (SpriteRenderer renderer in renderers)
                {
                    if (renderer != null)
                    {
                        if (!renderer.enabled)
                        {
                            renderer.enabled = true;
                        }
                        if (renderer.gameObject != null && !renderer.gameObject.activeSelf)
                        {
                            renderer.gameObject.SetActive(true);
                        }
                    }
                }

                // 检查僵尸组件状态
                if (this.zombie != null)
                {
                    if (!this.zombie.enabled)
                    {
                        this.zombie.enabled = true;
                    }
                    if (!this.zombie.gameObject.activeSelf)
                    {
                        this.zombie.gameObject.SetActive(true);
                    }
                }

                // 检查UltimateHorse组件状态
                UltimateHorse? ultimateHorse = base.gameObject.GetComponent<UltimateHorse>();
                if (ultimateHorse != null)
                {
                    if (!ultimateHorse.enabled)
                    {
                        ultimateHorse.enabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Core.Instance?.Logger.LogError($"无头终极马僵尸插件: EnsureZombieVisibility失败: {ex.Message}");
                Core.Instance?.Logger.LogError($"无头终极马僵尸插件: 堆栈跟踪: {ex.StackTrace}");
            }
        }

        private void DisableInvincibility()
        {
            this.isInvincible = false;
        }

        private void FindShootTransform()
        {
            try
            {
                // 首先尝试通过反射获取shoot字段
                var shootField = typeof(UltimateHorse).GetField("shoot");
                if (shootField != null)
                {
                    var shootValue = shootField.GetValue(this.zombie);
                    if (shootValue is Transform shoot)
                    {
                        this.shootTransform = shoot;
                        return;
                    }
                }

                // 如果反射失败，尝试查找子对象
                string[] possibleNames = { "Shoot", "shoot", "ShootPoint", "shootPoint" };
                foreach (string name in possibleNames)
                {
                    Transform found = this.zombie.transform.FindChild(name);
                    if (found != null)
                    {
                        this.shootTransform = found;
                        return;
                    }
                }

                // 如果还是找不到，使用axis作为备用
                if (this.zombie.axis != null)
                {
                    this.shootTransform = this.zombie.axis;
                }
            }
            catch (Exception ex)
            {
                Core.Instance?.Logger.LogError($"无头终极马僵尸插件: 查找射击点失败: {ex.Message}");
            }
        }

        private bool walkAnimationSet = false;

        private float walkCheckTimer = 0f;

        public void Update()
        {
			if (this.zombie == null)
			{
				return;
			}

			// 僵尸死亡或临死流程中，彻底停止一切逻辑，避免残留伤害
			if (this.zombie.beforeDying || this.zombie.theHealth <= 0 || !this.zombie.gameObject.activeInHierarchy)
			{
				return;
			}

			if (GameAPP.theGameStatus == GameStatus.InGame)
            {
                // 强制walk状态检查（在入场后的前几秒持续检查，确保walk状态不会被tohorse覆盖）
                if (!this.walkAnimationSet || this.walkCheckTimer < 2f)
                {
                    this.walkCheckTimer += Time.deltaTime;
                    if (this.zombie.anim != null)
                    {
                        // 检查当前动画状态，如果可能播放tohorse，强制切换到walk
                        try
                        {
                            var animator = this.zombie.anim;
                            if (animator != null && animator.isActiveAndEnabled)
                            {
                                // 获取当前动画状态信息
                                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                                var stateName = stateInfo.IsName("tohorse") || stateInfo.IsName("ToHorse");
                                
                                // 如果当前是tohorse动画或类似状态，立即切换到walk
                                if (stateName || !this.walkAnimationSet)
                                {
                                    this.zombie.anim.SetTrigger("walk");
                                    this.walkAnimationSet = true;
                                    // 如果检测到tohorse动画，直接切换到walk
                                }
                            }
                        }
                        catch
                        {
                            // 如果检查动画状态失败，直接设置walk
                            try
                            {
                                this.zombie.anim.SetTrigger("walk");
                                this.walkAnimationSet = true;
                            }
                            catch { }
                        }
                    }
                }

                // 回血机制：每0.1秒回血300
                this.healTimer += Time.deltaTime;
                if (this.healTimer >= 0.1f)
                {
                    this.healTimer = 0f;
                    if (this.zombie.theHealth < this.zombie.theMaxHealth)
                    {
                        int newHealth = Math.Min(this.zombie.theHealth + 300, this.zombie.theMaxHealth);
                        this.zombie.theHealth = newHealth;
                        this.zombie.UpdateHealthText();
                        this.UpdateLeaderHealthDisplay();
                    }
                }

                // 冲撞后停顿处理
                if (this.chargeStunTimer > 0f)
                {
                    this.chargeStunTimer -= Time.deltaTime;
                    if (this.chargeStunTimer <= 0f)
                    {
                        this.chargeStunTimer = 0f;
                        this.isCharging = false;
                        // 恢复正常移动
                        if (this.zombie.anim != null)
                        {
                            this.zombie.anim.SetTrigger("walk");
                        }
                    }
                }

                // 挥砍冷却计时
                if (this.slashCooldown > 0f)
                {
                    this.slashCooldown -= Time.deltaTime;
                }

                // 冲撞冷却计时
                if (this.chargeCooldown > 0f && !this.isCharging)
                {
                    this.chargeCooldown -= Time.deltaTime;
                }

                // 自动挥砍（每5秒）- 只在非冲撞状态下执行
                if (this.slashCooldown <= 0f && this.chargeStunTimer <= 0f && !this.isCharging && !this.isInvincible)
                {
                    if (this.zombie.anim != null && this.zombie.theStatus != (ZombieStatus)37) // 不在冲撞状态
                    {
                        this.zombie.anim.SetTrigger("shoot");
                        this.slashCooldown = 5f;
                    }
                }

                // 自动冲撞（每15秒）- 只在非冲撞状态下执行
                if (this.chargeCooldown <= 0f && this.chargeStunTimer <= 0f && !this.isCharging && !this.isInvincible)
                {
                    if (this.zombie.anim != null && this.zombie.theStatus != (ZombieStatus)37) // 不在冲撞状态
                    {
                        this.zombie.anim.SetTrigger("hit2");
                        this.isCharging = true;
                        this.chargeCooldown = 15f;
                    }
                }
            }
        }

        private void ThrowAxeOnEnter()
        {
            try
            {
                // 检查必要的对象是否已初始化
                if (this.zombie == null)
                {
                    Core.Instance?.Logger.LogWarning("无头终极马僵尸插件: 投掷长斧失败: zombie为null");
                    return;
                }

                if (this.zombie.board == null)
                {
                    Core.Instance?.Logger.LogWarning("无头终极马僵尸插件: 投掷长斧失败: zombie.board为null");
                    return;
                }

                if (Board.Instance == null)
                {
                    Core.Instance?.Logger.LogWarning("无头终极马僵尸插件: 投掷长斧失败: Board.Instance为null");
                    return;
                }

                if (Mouse.Instance == null)
                {
                    Core.Instance?.Logger.LogWarning("无头终极马僵尸插件: 投掷长斧失败: Mouse.Instance为null");
                    return;
                }

                Board board = this.zombie.board;
                if (board == null)
                {
                    Core.Instance?.Logger.LogWarning("无头终极马僵尸插件: 投掷长斧失败: board为null");
                    return;
                }

                int zombieRow = this.zombie.theZombieRow;
                if (zombieRow < 0 || zombieRow >= board.rowNum)
                {
                    Core.Instance?.Logger.LogWarning($"无头终极马僵尸插件: 投掷长斧失败: 无效的僵尸行 {zombieRow}");
                    return;
                }

                // 从第5列开始，向前投掷长斧
                // 如果没有植物阻挡，最多能从第5列右侧出场
                int startColumn = 5;
                int maxColumns = board.columnNum;
                if (maxColumns <= 0)
                {
                    Core.Instance?.Logger.LogWarning("无头终极马僵尸插件: 投掷长斧失败: 无效的列数");
                    return;
                }

                bool foundPlant = false;

                // 对每一列进行4次攻击
                for (int col = startColumn; col < maxColumns; col++)
                {
                    // 获取该列的植物
                    var plantsInColumnRaw = Lawnf.Get1x1Plants(col, zombieRow);
                    if (plantsInColumnRaw == null)
                        continue;

                    List<Plant> plantsInColumn = plantsInColumnRaw.ToArray().ToList();
                    if (plantsInColumn != null && plantsInColumn.Count > 0)
                    {
                        foundPlant = true;
                        // 有植物阻挡，对植物造成4次100点伤害
                        foreach (Plant plant in plantsInColumn)
                        {
                            if (plant != null && plant.gameObject != null)
                            {
                                // 对植物造成4次100点伤害
                                for (int i = 0; i < 4; i++)
                                {
                                    if (plant.thePlantHealth > 0)
                                    {
                                        plant.thePlantHealth -= 100;
                                        plant.FlashOnce();
                                        plant.UpdateText();
                                        if (plant.thePlantHealth <= 0)
                                        {
                                            plant.thePlantHealth = 0;
                                            plant.Broken();
                                            break; // 植物已死亡，跳出循环
                                        }
                                    }
                                    else
                                    {
                                        break; // 植物已死亡
                                    }
                                }
                            }
                        }
                        break; // 遇到植物阻挡，停止投掷
                    }
                }

                // 如果没有植物阻挡，将僵尸移动到第5列右侧
                if (!foundPlant && this.zombie.axis != null && Mouse.Instance != null)
                {
                    try
                    {
                        // 获取第5列右侧的位置
                        float targetX = Mouse.Instance.GetBoxXFromColumn(startColumn);
                        Vector3 currentPos = this.zombie.axis.position;
                        this.zombie.axis.position = new Vector3(targetX, currentPos.y, currentPos.z);
                        
                        // 更新僵尸位置
                        if (this.zombie.transform != null)
                        {
                            this.zombie.transform.position = this.zombie.axis.position;
                        }
                    }
                    catch (Exception ex)
                    {
                        Core.Instance?.Logger.LogWarning($"无头终极马僵尸插件: 移动僵尸位置失败: {ex.Message}");
                    }
                }

                // 播放音效（樱桃啵啵啵~）
                try
                {
                    GameAPP.PlaySound(40);
                    if (this.zombie.axis != null)
                    {
                        Vector3 position = this.zombie.axis.position;
                        CreateParticle.SetParticle(11, new Vector3(position.x, position.y, 0f), zombieRow);
                    }
                }
                catch (Exception ex)
                {
                    Core.Instance?.Logger.LogWarning($"无头终极马僵尸插件: 播放音效和特效失败: {ex.Message}");
                }

                // 投掷斧头后，确保僵尸继续存在并设置walk动画
                // 这很重要，因为投掷斧头可能会触发某些动画状态导致僵尸消失
                try
                {
                    if (this.zombie != null && this.zombie.gameObject != null)
                    {
                        // 确保GameObject激活
                        if (!this.zombie.gameObject.activeSelf)
                        {
                            Core.Instance?.Logger.LogWarning("无头终极马僵尸插件: 投掷斧头后GameObject未激活，尝试激活");
                            this.zombie.gameObject.SetActive(true);
                        }

                        // 确保Zombie组件启用
                        if (!this.zombie.enabled)
                        {
                            Core.Instance?.Logger.LogWarning("无头终极马僵尸插件: 投掷斧头后Zombie组件未启用，尝试启用");
                            this.zombie.enabled = true;
                        }

                        // 延迟设置walk动画，确保动画状态正确切换
                        // 立即设置一次
                        if (this.zombie.anim != null)
                        {
                            this.zombie.anim.SetTrigger("walk");
                            this.walkAnimationSet = true;
                            this.walkCheckTimer = 0f; // 重置计时器，继续监控
                        this.UpdateLeaderHealthDisplay();
                        }

                        // 延迟再次设置，确保walk状态持续
                        this.Invoke("SetWalkAnimation", 0.1f);
                        this.Invoke("SetWalkAnimation", 0.2f);
                        this.Invoke("SetWalkAnimation", 0.5f);

                        // 确保僵尸可见
                        this.EnsureZombieVisibility();
                    }
                    else
                    {
                        Core.Instance?.Logger.LogError("无头终极马僵尸插件: 投掷斧头后zombie或gameObject为null！");
                    }
                }
                catch (Exception ex)
                {
                    Core.Instance?.Logger.LogError($"无头终极马僵尸插件: 投掷斧头后设置walk动画失败: {ex.Message}");
                    Core.Instance?.Logger.LogError($"无头终极马僵尸插件: 堆栈跟踪: {ex.StackTrace}");
                }
            }
            catch (Exception ex)
            {
                Core.Instance?.Logger.LogError($"无头终极马僵尸插件: 投掷长斧失败: {ex.Message}");
                Core.Instance?.Logger.LogError($"无头终极马僵尸插件: 堆栈跟踪: {ex.StackTrace}");
            }
        }

        public void AnimShoot()
        {
            try
            {
				// 僵尸已进入死亡流程或已失活，终止
				if (this.zombie == null || this.zombie.beforeDying || this.zombie.theHealth <= 0 || !this.zombie.gameObject.activeInHierarchy)
				{
					return;
				}

                // 确保僵尸存在且激活
                if (this.zombie == null || this.zombie.gameObject == null)
                {
                    Core.Instance?.Logger.LogWarning("无头终极马僵尸插件: AnimShoot时zombie为null");
                    return;
                }

                // 确保GameObject激活
                if (!this.zombie.gameObject.activeSelf)
                {
                    Core.Instance?.Logger.LogWarning("无头终极马僵尸插件: AnimShoot时GameObject未激活，尝试激活");
                    this.zombie.gameObject.SetActive(true);
                }

                if (Board.Instance == null)
                    return;

                int zombieRow = this.zombie.theZombieRow;
                int zombieColumn = this.GetZombieColumn();

                // 获取前方两格的植物
                for (int colOffset = 0; colOffset < 2; colOffset++)
                {
                    int targetColumn = zombieColumn - colOffset - 1;
                    if (targetColumn >= 0)
                    {
                        var plantsRaw = Lawnf.Get1x1Plants(targetColumn, zombieRow);
                        List<Plant> plants = plantsRaw != null ? plantsRaw.ToArray().ToList() : new List<Plant>();
                        if (plants != null && plants.Count > 0)
                        {
                            foreach (Plant plant in plants)
                            {
                                if (plant != null && plant.gameObject != null)
                                {
									// 被击中植物：添加取消限伤标记
									if (plant.gameObject.GetComponent<UncappedPlantDamageComponent>() == null)
									{
										plant.gameObject.AddComponent<UncappedPlantDamageComponent>();
									}
                                    // 对植物造成4次100点伤害
                                    for (int i = 0; i < 4; i++)
                                    {
                                        if (plant.thePlantHealth > 0)
                                        {
                                            plant.thePlantHealth -= 100;
                                            plant.FlashOnce();
                                            plant.UpdateText();
                                            if (plant.thePlantHealth <= 0)
                                            {
                                                plant.thePlantHealth = 0;
                                                plant.Broken();
                                                break; // 植物已死亡，跳出循环
                                            }
                                        }
                                        else
                                        {
                                            break; // 植物已死亡
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // 播放音效
                GameAPP.PlaySound(40);

                // 挥砍后确保僵尸继续存在并设置walk动画
                if (this.zombie != null && this.zombie.anim != null)
                {
                    // 延迟设置walk动画，确保动画状态正确切换
                    this.Invoke("SetWalkAnimation", 0.1f);
                    this.Invoke("SetWalkAnimation", 0.3f);
                }
            }
            catch (Exception ex)
            {
                Core.Instance?.Logger.LogError($"无头终极马僵尸插件: 挥砍攻击失败: {ex.Message}");
            }
        }

        public void AnimHit2()
        {
            try
            {
				if (this.zombie == null || Board.Instance == null)
					return;

				// 僵尸已进入死亡流程或已失活，终止
				if (this.zombie.beforeDying || this.zombie.theHealth <= 0 || !this.zombie.gameObject.activeInHierarchy)
                    return;

                // 基于距离的“附近”检测（使用物理重叠圆）
                Vector3 center = (this.zombie.axis != null) ? this.zombie.axis.position : this.zombie.transform.position;
                float radius = 2.0f; // 半径可按需要微调
                int row = this.zombie.theZombieRow;

                foreach (Collider2D collider in Physics2D.OverlapCircleAll(center, radius, this.zombie.zombieLayer))
                {
                    if (collider == null) continue;
                    Zombie target = collider.gameObject.GetComponent<Zombie>();
                    if (target == null || target == this.zombie) continue;
                    if (this.IsLeaderZombie(target)) continue; // 不对领袖生效
                    if (target.beforeDying) continue;

                    this.ApplyUndyingBuff(target, 10f);
                }

                // 设置冲撞后停顿
                this.chargeStunTimer = 1.5f;
                this.isCharging = true;
                this.chargeCooldown = 15f; // 重置冲撞冷却

                // 播放音效和特效
                GameAPP.PlaySound(141);
                if (this.zombie.axis != null)
                {
                    Vector3 position = this.zombie.axis.position;
                    CreateParticle.SetParticle(121, new Vector3(position.x, position.y, 0f), this.zombie.theZombieRow);
                }
            }
            catch (Exception ex)
            {
                Core.Instance?.Logger.LogError($"无头终极马僵尸插件: 冲撞攻击失败: {ex.Message}");
            }
        }

        private int GetZombieColumn(Zombie? targetZombie = null)
        {
            try
            {
                Zombie? zombieToCheck = targetZombie ?? this.zombie;
                if (zombieToCheck == null)
                    return -1;
                
                Transform? axis = zombieToCheck.axis ?? zombieToCheck.transform;
                if (axis == null)
                    return -1;

                // 通过Mouse获取列位置
                if (Mouse.Instance != null)
                {
                    int col = Mouse.Instance.GetColumnFromX(axis.position.x);
                    if (col >= 0)
                    {
                        return col;
                    }
                }

                // 备用方法：通过board计算
                if (zombieToCheck.board != null)
                {
                    float x = axis.position.x;
                    // 简单计算列位置（可能需要根据实际游戏调整）
                    int approx = (int)Math.Floor(x);
                    if (approx >= 0)
                    {
                        return approx;
                    }
                }

                return -1;
            }
            catch (Exception ex)
            {
                Core.Instance?.Logger.LogError($"无头终极马僵尸插件: 获取僵尸列位置失败: {ex.Message}");
                return -1;
            }
        }


        private bool IsLeaderZombie(Zombie zombie)
        {
            try
            {
                if (zombie == null)
                    return false;

                // 通过TypeMgr检查是否为领袖僵尸
                try
                {
                    if (TypeMgr.IsLeaderZombie != null)
                    {
                        return TypeMgr.IsLeaderZombie(zombie.theZombieType);
                    }
                }
                catch { }

                return false;
            }
            catch (Exception ex)
            {
                Core.Instance?.Logger.LogError($"无头终极马僵尸插件: 检查领袖僵尸失败: {ex.Message}");
                return false;
            }
        }

        private void ApplyUndyingBuff(Zombie zombie, float duration)
        {
            try
            {
                if (zombie == null)
                    return;

                // 获取或创建UndyingBuff组件
                UndyingBuffComponent buff = zombie.gameObject.GetComponent<UndyingBuffComponent>();
                if (buff == null)
                {
                    buff = zombie.gameObject.AddComponent<UndyingBuffComponent>();
                }

                buff.targetZombie = zombie;
                buff.SetUndying(duration);
                buff.ApplyGreenTint();
            }
            catch (Exception ex)
            {
                Core.Instance?.Logger.LogError($"无头终极马僵尸插件: 施加不死状态失败: {ex.Message}");
            }
        }

        private void ConfigureLeaderHealthDisplay()
        {
            try
            {
                if (this.zombie == null)
                {
                    return;
                }

                if (this.zombie.healthText != null)
                {
                    this.zombie.healthText.gameObject.SetActive(true);
                    this.zombie.healthText.fontSize = Mathf.Max(this.zombie.healthText.fontSize, 4f);
                    this.zombie.healthText.color = new Color(0.92f, 0.15f, 0.15f, 1f);
                }

                if (this.zombie.healthTextShadow != null)
                {
                    this.zombie.healthTextShadow.gameObject.SetActive(true);
                    this.zombie.healthTextShadow.fontSize = Mathf.Max(this.zombie.healthTextShadow.fontSize, 4f);
                }

            }
            catch (Exception ex)
            {
                Core.Instance?.Logger.LogError($"无头终极马僵尸插件: 配置领袖血量显示失败: {ex.Message}");
            }
        }

        private void UpdateLeaderHealthDisplay()
        {
            try
            {
                if (this.zombie == null)
                {
                    return;
                }

                if (this.zombie.healthText != null && this.zombie.healthText.gameObject.activeSelf)
                {
                    this.zombie.UpdateHealthText();
                }
            }
            catch (Exception ex)
            {
                Core.Instance?.Logger.LogError($"无头终极马僵尸插件: 更新领袖血量显示失败: {ex.Message}");
            }
        }

        public void AnimFlagUp()
        {
        }

        public void AnimRevive()
        {
        }

        public void AnimDestoryHorse()
        {
        }
    }
}
