using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateGoldJackBoxZombieMod
{
    public class ZombieSpawner : MonoBehaviour
    {
        public float spawnAnimationDuration = 1f;
        public float animationSpeedMultiplier = 0.6f;
        public ParticleType spawnParticleType = (ParticleType)11;
        public Vector2 particleOffset = new Vector2(0f, 0.7f);
        private static ZombieSpawner? _instance;
        private List<SpawnTask> activeTasks = new List<SpawnTask>();
        private List<DespawnTask> activeDespawnTasks = new List<DespawnTask>();

        public static ZombieSpawner Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject obj = new GameObject("ZombieSpawner");
                    _instance = obj.AddComponent<ZombieSpawner>();
                    UnityEngine.Object.DontDestroyOnLoad(obj);
                }
                return _instance;
            }
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (activeTasks.Count > 0)
            {
                for (int i = activeTasks.Count - 1; i >= 0; i--)
                {
                    SpawnTask task = activeTasks[i];
                    if (task.zombieObj == null)
                    {
                        activeTasks.RemoveAt(i);
                    }
                    else
                    {
                        task.elapsedTime += deltaTime;
                        float t = Mathf.Clamp01(task.elapsedTime / task.duration);
                        float eased = EaseInOutCubic(t);
                        task.zombieObj.transform.localScale = task.targetScale * eased;
                        if (t >= 1f)
                        {
                            task.zombieObj.transform.localScale = task.targetScale;
                            if (!task.soundPlayed)
                            {
                                PlaySpawnSound();
                                task.soundPlayed = true;
                            }
                            activeTasks.RemoveAt(i);
                        }
                    }
                }
            }
            if (activeDespawnTasks.Count > 0)
            {
                UpdateDespawnTasks(deltaTime);
            }
        }

        public void DespawnZombieWithEffect(GameObject zombieObj, int row, int particleType = 12)
        {
            if (zombieObj == null) return;
            try
            {
                DespawnTask task = new DespawnTask
                {
                    zombieObj = zombieObj,
                    elapsedTime = 0f,
                    duration = 1.8f,
                    initialScale = zombieObj.transform.localScale,
                    particleId = particleType,
                    particleTimer = 0f,
                    particlesSpawned = 0,
                    zombiePosition = zombieObj.transform.position,
                    row = row
                };
                activeDespawnTasks.Add(task);
                PlayDespawnStartSound();
                Plugin.Log?.LogInfo("开始僵尸消失动画");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"开始消失动画失败: {ex.Message}");
            }
        }

        private void PlayDespawnStartSound()
        {
            try
            {
                GameAPP.PlaySound(110, 0.5f, 0.8f);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"播放消失开始音效失败: {ex.Message}");
            }
        }

        private float EaseInOutCubic(float t)
        {
            if (t < 0.5f)
            {
                return 4f * t * t * t;
            }
            float f = 2f * t - 2f;
            return 0.5f * f * f * f + 1f;
        }

        private float EaseInCubic(float t)
        {
            return t * t * t;
        }

        private float EaseOutQuad(float t)
        {
            return t * (2f - t);
        }

        private void UpdateDespawnTasks(float deltaTime)
        {
            for (int i = activeDespawnTasks.Count - 1; i >= 0; i--)
            {
                DespawnTask task = activeDespawnTasks[i];
                task.elapsedTime += deltaTime;
                task.particleTimer += deltaTime;
                float t = Mathf.Clamp01(task.elapsedTime / task.duration);
                if (task.zombieObj != null)
                {
                    task.zombiePosition = task.zombieObj.transform.position;
                    float scale = Mathf.Lerp(1f, 0.3f, EaseInCubic(t));
                    task.zombieObj.transform.localScale = task.initialScale * scale;
                }
                if (task.particleTimer >= task.particleInterval && task.particlesSpawned < task.particleCount)
                {
                    SpawnDespawnParticle(task, false);
                    task.particlesSpawned++;
                    task.particleTimer = 0f;
                }
                if (t >= 1f)
                {
                    for (int j = 0; j < 5; j++)
                    {
                        SpawnDespawnParticle(task, true);
                    }
                    if (task.zombieObj != null)
                    {
                        UnityEngine.Object.Destroy(task.zombieObj);
                    }
                    PlayDespawnSound();
                    activeDespawnTasks.RemoveAt(i);
                    Plugin.Log?.LogInfo("僵尸消失动画完成");
                }
            }
        }

        private void PlayDespawnSound()
        {
        }

        private void SpawnDespawnParticle(DespawnTask task, bool isFinalBurst = false)
        {
            try
            {
                ParticleManager? instance = ParticleManager.Instance;
                if (instance == null) return;
                Vector3 pos = task.zombiePosition;
                float offsetX = UnityEngine.Random.Range(-0.5f, 0.5f);
                float offsetY = UnityEngine.Random.Range(-0.3f, 0.8f);
                if (isFinalBurst)
                {
                    offsetX *= 2f;
                    offsetY *= 2f;
                }
                Vector2 particlePos = new Vector2(pos.x + offsetX, pos.y + offsetY);
                int[] particleTypes = new int[] { task.particleId, 13, 14 };
                int particleType = particleTypes[UnityEngine.Random.Range(0, particleTypes.Length)];
                instance.SetParticle((ParticleType)particleType, particlePos, task.row);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"生成消失粒子失败: {ex.Message}");
            }
        }

        public void SpawnZombieAt(Vector3 position, int lastCount, int zombieType, bool isGoldZombie = true, float x = 9f, int row = 0)
        {
            try
            {
                SpawnParticleEffect(position, row);
                GameObject zombieObj = CreateZombie.Instance.SetZombie(row, (ZombieType)zombieType, x, false);
                Animator? animator = zombieObj.GetComponent<Animator>();
                Vector3 targetScale = Vector3.zero;
                if (isGoldZombie)
                {
                    Jackbox_c? jackbox = zombieObj.GetComponent<Jackbox_c>();
                    if (jackbox != null)
                    {
                        jackbox.Start();
                        UltimateGoldJackBox? goldJackBox = zombieObj.GetComponent<UltimateGoldJackBox>();
                        if (goldJackBox != null)
                        {
                            // 设置复活次数，-1表示不能再复活
                            goldJackBox.lastCount = lastCount;
                            goldJackBox.hasUsedNoJumperRevive = lastCount < 0;
                            Plugin.Log?.LogInfo($"初始化新僵尸 lastCount={lastCount}, hasUsedNoJumperRevive={goldJackBox.hasUsedNoJumperRevive}");
                        }
                    }
                    Plugin.Log?.LogInfo("获取组件");
                    targetScale = new Vector3(0.4f, 0.4f, 0.4f);
                    zombieObj.transform.localScale = Vector3.zero;
                }
                else
                {
                    targetScale = zombieObj.transform.localScale;
                    zombieObj.transform.localScale = Vector3.zero;
                }
                SpawnTask task = new SpawnTask
                {
                    zombieObj = zombieObj,
                    elapsedTime = 0f,
                    duration = spawnAnimationDuration,
                    animator = animator,
                    particleSpawned = true,
                    soundPlayed = false,
                    targetScale = isGoldZombie ? (targetScale * 1.15f) : targetScale,
                    lastCount = lastCount
                };
                activeTasks.Add(task);
                Plugin.Log?.LogInfo($"生成僵尸在位置: {position}, 行: {row}");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"生成僵尸失败: {ex.Message}");
            }
        }

        private void SpawnParticleEffect(Vector3 position, int row)
        {
            try
            {
                if (ParticleManager.Instance != null)
                {
                    Vector2 particlePos = new Vector2(position.x + particleOffset.x, position.y + particleOffset.y);
                    ParticleManager.Instance.SetParticle(spawnParticleType, particlePos, row);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"生成粒子效果失败: {ex.Message}");
            }
        }

        private void PlaySpawnSound()
        {
            try
            {
                GameAPP.PlaySound(UnityEngine.Random.Range(100, 103), 0.7f, 1f);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"播放音效失败: {ex.Message}");
            }
        }

        public void SpawnZombieAtWithFancyEffect(Vector3 position, int row = 0, UnityEngine.Object? zombiePrefab = null)
        {
        }

        private void SpawnMultipleParticleEffects(Vector3 position, int row, int count = 3)
        {
            try
            {
                if (ParticleManager.Instance == null) return;
                for (int i = 0; i < count; i++)
                {
                    float offsetX = UnityEngine.Random.Range(-0.3f, 0.3f);
                    float offsetY = UnityEngine.Random.Range(-0.2f, 0.5f);
                    Vector2 particlePos = new Vector2(position.x + particleOffset.x + offsetX, position.y + particleOffset.y + offsetY);
                    int particleType = (int)spawnParticleType + i % 2;
                    ParticleManager.Instance.SetParticle((ParticleType)particleType, particlePos, row);
                }
                Plugin.Log?.LogInfo($"生成了 {count} 个生成粒子");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"生成多重粒子效果异常: {ex.Message}");
            }
        }

        private class SpawnTask
        {
            public GameObject? zombieObj;
            public float elapsedTime;
            public float duration;
            public Vector3 targetScale = Vector3.one;
            public Animator? animator;
            public bool particleSpawned;
            public bool soundPlayed;
            public int lastCount;
        }

        private class DespawnTask
        {
            public GameObject? zombieObj;
            public float elapsedTime;
            public float duration = 2f;
            public Vector3 initialScale;
            public int particleId;
            public float particleTimer;
            public float particleInterval = 0.1f;
            public int particleCount = 15;
            public int particlesSpawned;
            public Vector3 zombiePosition;
            public int row;
        }
    }
}
