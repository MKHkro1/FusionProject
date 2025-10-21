using System;
using System.Runtime.CompilerServices;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace SuperSunSniperPea
{
    public class SuperSunSniperPea : MonoBehaviour
    {
        public SuperSunSniperPea(IntPtr i) : base(i)
        {
        }

        /// <summary>
        /// 伤害提升计时器
        /// </summary>
        private float damageBoostTimer = 0f;

        /// <summary>
        /// 伤害提升持续时间计时器
        /// </summary>
        private float damageBoostDuration = 0f;

        /// <summary>
        /// 是否处于伤害提升状态
        /// </summary>
        private bool isDamageBoosted = false;

        /// <summary>
        /// 伤害提升倍数
        /// </summary>
        private const float DAMAGE_BOOST_MULTIPLIER = 50f;

        /// <summary>
        /// 伤害提升持续时间（秒）
        /// </summary>
        private const float DAMAGE_BOOST_DURATION = 5f;

        /// <summary>
        /// 伤害提升触发间隔（秒）
        /// </summary>
        private const float DAMAGE_BOOST_INTERVAL = 3f;

        /// <summary>
        /// 伤害提升触发概率
        /// </summary>
        private const float DAMAGE_BOOST_CHANCE = 0.5f;

        /// <summary>
        /// 更新伤害提升逻辑
        /// </summary>
        public void UpdateDamageBoost()
        {
            // 如果当前处于伤害提升状态
            if (isDamageBoosted)
            {
                damageBoostDuration -= Time.deltaTime;
                if (damageBoostDuration <= 0f)
                {
                    // 伤害提升结束
                    isDamageBoosted = false;
                    UnityEngine.Debug.Log("SuperSunSniperPea: 伤害提升效果结束");
                }
            }
            else
            {
                // 更新计时器
                damageBoostTimer += Time.deltaTime;
                
                // 每3秒检查一次是否触发伤害提升
                if (damageBoostTimer >= DAMAGE_BOOST_INTERVAL)
                {
                    damageBoostTimer = 0f; // 重置计时器
                    
                    // 50%概率触发伤害提升
                    if (UnityEngine.Random.Range(0f, 1f) < DAMAGE_BOOST_CHANCE)
                    {
                        TriggerDamageBoost();
                    }
                }
            }
        }

        /// <summary>
        /// 触发伤害提升效果
        /// </summary>
        private void TriggerDamageBoost()
        {
            isDamageBoosted = true;
            damageBoostDuration = DAMAGE_BOOST_DURATION;
            
            // 播放特效和音效
            GameAPP.PlaySound(40, 0.5f, 1f); // 播放射击音效
            CreateParticle.SetParticle(28, plant.ac.transform.position, plant.thePlantRow, true);
            
            UnityEngine.Debug.Log("SuperSunSniperPea: 伤害提升效果触发！持续5秒");
        }

        /// <summary>
        /// 检查是否处于伤害提升状态
        /// </summary>
        public bool IsDamageBoosted()
        {
            return isDamageBoosted;
        }

        // Token: 0x0600000C RID: 12 RVA: 0x00002264 File Offset: 0x00000464
        public void AttackZombie(Zombie zombie, int damage)
        {
            if (zombie)
            {
                zombie.TakeDamage(0, damage, false);
                bool flag = this.plant.attackCount % 12 == 0;
                UnityEngine.Random.Range(0, 3);
                Vector3 position = this.plant.ac.transform.position;
                Transform transform = this.plant.board.transform;
                CreateParticle.SetParticle(28, position, this.plant.targetZombie.theZombieRow, true);
            }
        }

        // Token: 0x0600000D RID: 13 RVA: 0x000022E0 File Offset: 0x000004E0
        public void AnimShoot_IceDoom()
        {
            GameAPP.PlaySound(40, 0.2f, 1f);
            Zombie targetZombie = this.plant.targetZombie;
            if (!(targetZombie == null) && this.SearchUniqueZombie(targetZombie))
            {
                SniperPea plant = this.plant;
                int attackCount = plant.attackCount;
                plant.attackCount = attackCount + 1;
                int attackDamage = this.plant.attackDamage;
                
                // 如果处于伤害提升状态，增加伤害
                if (isDamageBoosted)
                {
                    attackDamage = (int)(attackDamage * DAMAGE_BOOST_MULTIPLIER);
                }
                Zombie targetZombie2 = this.plant.targetZombie;
                float num = targetZombie2.theHealth + (float)targetZombie2.theFirstArmorHealth +
                            (float)targetZombie2.theSecondArmorHealth;
                int num2 = targetZombie2.theMaxHealth + targetZombie2.theFirstArmorMaxHealth +
                           targetZombie2.theSecondArmorMaxHealth;
                CreateItem.Instance.SetCoin(0, 0, 13, 0, plant.targetZombie.transform.position, false);
                if (num <= (float)attackDamage)
                {
                    int num3 = (int)((double)num2 * 0.1 / 25.0);
                    for (int i = 0; i < num3 / 2; i++)
                    {
                        Vector3 position = plant.targetZombie.transform.position;
                        position.y += 2f;
                        position.x += UnityEngine.Random.Range(-0.15f, 0.15f);
                        position.y += UnityEngine.Random.Range(-0.15f, 0.15f);
                        CreateItem.Instance.SetCoin(0, 0, 0, 0, position, false);
                    }

                    if (num3 % 2 == 1)
                    {
                        CreateItem.Instance.SetCoin(0, 0, 13, 0, plant.targetZombie.transform.position, false);
                    }
                }

                if (targetZombie != null)
                {
                    this.AttackZombie(targetZombie, attackDamage);
                    if (targetZombie.theStatus == ZombieStatus.Dying || targetZombie.beforeDying)
                    {
                        this.plant.targetZombie = null;
                    }
                }
            }
        }

        // Token: 0x0600000E RID: 14 RVA: 0x000024A0 File Offset: 0x000006A0
        public bool SearchUniqueZombie(Zombie zombie)
        {
            bool result;
            if (zombie == null)
            {
                result = false;
            }
            else if (zombie.isMindControlled || zombie.beforeDying)
            {
                result = false;
            }
            else
            {
                ZombieStatus theStatus = zombie.theStatus;
                if (theStatus <= ZombieStatus.Dying)
                {
                    if (theStatus == ZombieStatus.Dying || theStatus == (ZombieStatus)7)
                    {
                        return false;
                    }
                }
                else if (theStatus == (ZombieStatus)12 ||
                         (theStatus >= (ZombieStatus)20 && theStatus <= (ZombieStatus)24))
                {
                    return false;
                }

                result = true;
            }

            return result;
        }

        // Token: 0x0600000F RID: 15 RVA: 0x00002510 File Offset: 0x00000710
        public GameObject SearchZombie()
        {
            this.plant.zombieList.Clear();
            float num = float.MaxValue;
            GameObject gameObject = null;
            if (this.plant.board != null)
            {
                foreach (Zombie zombie in this.plant.board.zombieArray)
                {
                    if (!(zombie == null))
                    {
                        Transform transform = zombie.transform;
                        if (!(transform == null) && this.plant.vision >= transform.position.x)
                        {
                            Transform axis = this.plant.axis;
                            if (!(axis == null) && transform.position.x > axis.position.x &&
                                this.SearchUniqueZombie(zombie))
                            {
                                float num2 = Vector3.Distance(transform.position, axis.position);
                                if (num2 < num)
                                {
                                    num = num2;
                                    gameObject = zombie.gameObject;
                                }
                            }
                        }
                    }
                }
            }

            GameObject result;
            if (gameObject != null)
            {
                this.plant.targetZombie = gameObject.GetComponent<Zombie>();
                result = gameObject;
            }
            else
            {
                result = null;
            }

            return result;
        }

        // Token: 0x17000001 RID: 1
        // (get) Token: 0x06000010 RID: 16 RVA: 0x00002639 File Offset: 0x00000839
        public SniperPea plant
        {
            get { return base.gameObject.GetComponent<SniperPea>(); }
        }

        // Token: 0x04000003 RID: 3
        public static int PlantID = 1651;
    }
}