using System;
using System.Collections.Generic;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UltimateGoldJackBoxZombieMod
{
	public class ZombieSpawner : MonoBehaviour
	{
		public static ZombieSpawner Instance
		{
			get
			{
				if (ZombieSpawner._instance == null)
				{
					GameObject gameObject = new GameObject("ZombieSpawner");
					ZombieSpawner._instance = gameObject.AddComponent<ZombieSpawner>();
					UnityEngine.Object.DontDestroyOnLoad(gameObject);
				}
				return ZombieSpawner._instance;
			}
		}

		private void Update()
		{
			float deltaTime = Time.deltaTime;
			if (this.activeTasks.Count > 0)
			{
				for (int i = this.activeTasks.Count - 1; i >= 0; i--)
				{
					ZombieSpawner.SpawnTask spawnTask = this.activeTasks[i];
					if (spawnTask.zombieObj == null)
					{
						this.activeTasks.RemoveAt(i);
					}
					else
					{
						spawnTask.elapsedTime += deltaTime;
						float num = Mathf.Clamp01(spawnTask.elapsedTime / spawnTask.duration);
						float num2 = this.EaseInOutCubic(num);
						spawnTask.zombieObj.transform.localScale = spawnTask.targetScale * num2;
						if (num >= 1f)
						{
							spawnTask.zombieObj.transform.localScale = spawnTask.targetScale;
							if (!spawnTask.soundPlayed)
							{
								this.PlaySpawnSound();
								spawnTask.soundPlayed = true;
							}
							this.activeTasks.RemoveAt(i);
						}
					}
				}
			}
			if (this.activeDespawnTasks.Count > 0)
			{
				this.UpdateDespawnTasks(deltaTime);
			}
		}

		public void DespawnZombieWithEffect(GameObject zombieObj, int row, int particleType = 12)
		{
			if (zombieObj == null)
			{
				return;
			}
			try
			{
				ZombieSpawner.DespawnTask item = new ZombieSpawner.DespawnTask
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
				this.activeDespawnTasks.Add(item);
				this.PlayDespawnStartSound();
				ManualLogSource log = Plugin.Log;
				if (log != null)
				{
					log.LogInfo("开始僵尸消失动画");
				}
			}
			catch (Exception ex)
			{
				ManualLogSource log2 = Plugin.Log;
				if (log2 != null)
				{
					bool flag;
					BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler = new BepInExErrorLogInterpolatedStringHandler(10, 1, out flag);
					if (flag)
					{
						bepInExErrorLogInterpolatedStringHandler.AppendLiteral("开始消失动画失败: ");
						bepInExErrorLogInterpolatedStringHandler.AppendFormatted<string>(ex.Message);
					}
					log2.LogError(bepInExErrorLogInterpolatedStringHandler);
				}
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
				ManualLogSource log = Plugin.Log;
				if (log != null)
				{
					bool flag;
					BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler = new BepInExErrorLogInterpolatedStringHandler(12, 1, out flag);
					if (flag)
					{
						bepInExErrorLogInterpolatedStringHandler.AppendLiteral("播放消失开始音效失败: ");
						bepInExErrorLogInterpolatedStringHandler.AppendFormatted<string>(ex.Message);
					}
					log.LogError(bepInExErrorLogInterpolatedStringHandler);
				}
			}
		}

		private float EaseInOutCubic(float t)
		{
			if (t < 0.5f)
			{
				return 4f * t * t * t;
			}
			float num = 2f * t - 2f;
			return 0.5f * num * num * num + 1f;
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
			for (int i = this.activeDespawnTasks.Count - 1; i >= 0; i--)
			{
				ZombieSpawner.DespawnTask despawnTask = this.activeDespawnTasks[i];
				despawnTask.elapsedTime += deltaTime;
				despawnTask.particleTimer += deltaTime;
				float num = Mathf.Clamp01(despawnTask.elapsedTime / despawnTask.duration);
				if (despawnTask.zombieObj != null)
				{
					despawnTask.zombiePosition = despawnTask.zombieObj.transform.position;
					float num2 = Mathf.Lerp(1f, 0.3f, this.EaseInCubic(num));
					despawnTask.zombieObj.transform.localScale = despawnTask.initialScale * num2;
				}
				if (despawnTask.particleTimer >= despawnTask.particleInterval && despawnTask.particlesSpawned < despawnTask.particleCount)
				{
					this.SpawnDespawnParticle(despawnTask, false);
					despawnTask.particlesSpawned++;
					despawnTask.particleTimer = 0f;
				}
				if (num >= 1f)
				{
					for (int j = 0; j < 5; j++)
					{
						this.SpawnDespawnParticle(despawnTask, true);
					}
					if (despawnTask.zombieObj != null)
					{
						UnityEngine.Object.Destroy(despawnTask.zombieObj);
					}
					this.PlayDespawnSound();
					this.activeDespawnTasks.RemoveAt(i);
					ManualLogSource log = Plugin.Log;
					if (log != null)
					{
						log.LogInfo("僵尸消失动画完成");
					}
				}
			}
		}

		private void PlayDespawnSound()
		{
		}

		private void SpawnDespawnParticle(ZombieSpawner.DespawnTask task, bool isFinalBurst = false)
		{
			try
			{
				ParticleManager instance = ParticleManager.Instance;
				if (!(instance == null))
				{
					Vector3 zombiePosition = task.zombiePosition;
					float num = Random.Range(-0.5f, 0.5f);
					float num2 = Random.Range(-0.3f, 0.8f);
					if (isFinalBurst)
					{
						num *= 2f;
						num2 *= 2f;
					}
					Vector2 vector = new Vector2(zombiePosition.x + num, zombiePosition.y + num2);
					int[] array = new int[]
					{
						task.particleId,
						13,
						14
					};
					int num3 = array[Random.Range(0, array.Length)];
					instance.SetParticle((ParticleType)num3, vector, task.row);
				}
			}
			catch (Exception ex)
			{
				ManualLogSource log = Plugin.Log;
				if (log != null)
				{
					bool flag;
					BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler = new BepInExErrorLogInterpolatedStringHandler(10, 1, out flag);
					if (flag)
					{
						bepInExErrorLogInterpolatedStringHandler.AppendLiteral("生成消失粒子失败: ");
						bepInExErrorLogInterpolatedStringHandler.AppendFormatted<string>(ex.Message);
					}
					log.LogError(bepInExErrorLogInterpolatedStringHandler);
				}
			}
		}

		public void SpawnZombieAt(Vector3 position, int lastCount, int zombieType, bool isGoldZombie = true, float x = 9f, int row = 0)
		{
			try
			{
				this.SpawnParticleEffect(position, row);
				GameObject gameObject = CreateZombie.Instance.SetZombie(row, (ZombieType)zombieType, x, false);
				Animator component = gameObject.GetComponent<Animator>();
				Vector3 vector = Vector3.zero;
				ManualLogSource log;
				bool flag;
				if (isGoldZombie)
				{
					Jackbox_c component2 = gameObject.GetComponent<Jackbox_c>();
					if (component2 != null)
					{
						component2.Start();
						UltimateGoldJackBox component3 = gameObject.GetComponent<UltimateGoldJackBox>();
						if (component3 != null)
						{
							component3.lastCount = lastCount;
							component3.hasUsedNoJumperRevive = (lastCount < 0);
							log = Plugin.Log;
							if (log != null)
							{
								BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(41, 2, out flag);
								if (flag)
								{
									bepInExInfoLogInterpolatedStringHandler.AppendLiteral("初始化新僵尸 lastCount=");
									bepInExInfoLogInterpolatedStringHandler.AppendFormatted<int>(lastCount);
									bepInExInfoLogInterpolatedStringHandler.AppendLiteral(", hasUsedNoJumperRevive=");
									bepInExInfoLogInterpolatedStringHandler.AppendFormatted<bool>(component3.hasUsedNoJumperRevive);
								}
								log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
							}
						}
					}
					ManualLogSource log2 = Plugin.Log;
					if (log2 != null)
					{
						log2.LogInfo("获取组件");
					}
					vector = new Vector3(0.4f, 0.4f, 0.4f);
					gameObject.transform.localScale = Vector3.zero;
				}
				else
				{
					vector = gameObject.transform.localScale;
					gameObject.transform.localScale = Vector3.zero;
				}
				ZombieSpawner.SpawnTask item = new ZombieSpawner.SpawnTask
				{
					zombieObj = gameObject,
					elapsedTime = 0f,
					duration = this.spawnAnimationDuration,
					animator = component,
					particleSpawned = true,
					soundPlayed = false,
					targetScale = (isGoldZombie ? (vector * 1.0f) : vector),
					lastCount = lastCount
				};
				this.activeTasks.Add(item);
				log = Plugin.Log;
				if (log != null)
				{
					BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(14, 2, out flag);
					if (flag)
					{
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral("生成僵尸在位置: ");
						bepInExInfoLogInterpolatedStringHandler.AppendFormatted<Vector3>(position);
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral(", 行: ");
						bepInExInfoLogInterpolatedStringHandler.AppendFormatted<int>(row);
					}
					log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
				}
			}
			catch (Exception ex)
			{
				ManualLogSource log = Plugin.Log;
				if (log != null)
				{
					bool flag;
					BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler = new BepInExErrorLogInterpolatedStringHandler(8, 1, out flag);
					if (flag)
					{
						bepInExErrorLogInterpolatedStringHandler.AppendLiteral("生成僵尸失败: ");
						bepInExErrorLogInterpolatedStringHandler.AppendFormatted<string>(ex.Message);
					}
					log.LogError(bepInExErrorLogInterpolatedStringHandler);
				}
			}
		}

		private void SpawnParticleEffect(Vector3 position, int row)
		{
			try
			{
				if (ParticleManager.Instance != null)
				{
					Vector2 vector = new Vector2(position.x + this.particleOffset.x, position.y + this.particleOffset.y);
					ParticleManager.Instance.SetParticle(this.spawnParticleType, vector, row);
				}
			}
			catch (Exception ex)
			{
				ManualLogSource log = Plugin.Log;
				if (log != null)
				{
					bool flag;
					BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler = new BepInExErrorLogInterpolatedStringHandler(10, 1, out flag);
					if (flag)
					{
						bepInExErrorLogInterpolatedStringHandler.AppendLiteral("生成粒子效果失败: ");
						bepInExErrorLogInterpolatedStringHandler.AppendFormatted<string>(ex.Message);
					}
					log.LogError(bepInExErrorLogInterpolatedStringHandler);
				}
			}
		}

		private void PlaySpawnSound()
		{
			try
			{
				GameAPP.PlaySound(Random.Range(100, 103), 0.7f, 1f);
			}
			catch (Exception ex)
			{
				ManualLogSource log = Plugin.Log;
				if (log != null)
				{
					bool flag;
					BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler = new BepInExErrorLogInterpolatedStringHandler(8, 1, out flag);
					if (flag)
					{
						bepInExErrorLogInterpolatedStringHandler.AppendLiteral("播放音效失败: ");
						bepInExErrorLogInterpolatedStringHandler.AppendFormatted<string>(ex.Message);
					}
					log.LogError(bepInExErrorLogInterpolatedStringHandler);
				}
			}
		}

		public void SpawnZombieAtWithFancyEffect(Vector3 position, int row = 0, UnityEngine.Object zombiePrefab = null)
		{
		}

		private void SpawnMultipleParticleEffects(Vector3 position, int row, int count = 3)
		{
			try
			{
				if (!(ParticleManager.Instance == null))
				{
					for (int i = 0; i < count; i++)
					{
						float num = Random.Range(-0.3f, 0.3f);
						float num2 = Random.Range(-0.2f, 0.5f);
						Vector2 vector = new Vector2(position.x + this.particleOffset.x + num, position.y + this.particleOffset.y + num2);
						int num3 = (int)this.spawnParticleType + i % 2;
						ParticleManager.Instance.SetParticle((ParticleType)num3, vector, row);
					}
					ManualLogSource log = Plugin.Log;
					if (log != null)
					{
						bool flag;
						BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(10, 1, out flag);
						if (flag)
						{
							bepInExInfoLogInterpolatedStringHandler.AppendLiteral("生成了 ");
							bepInExInfoLogInterpolatedStringHandler.AppendFormatted<int>(count);
							bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" 个生成粒子");
						}
						log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
					}
				}
			}
			catch (Exception ex)
			{
				ManualLogSource log = Plugin.Log;
				if (log != null)
				{
					bool flag;
					BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler = new BepInExErrorLogInterpolatedStringHandler(12, 1, out flag);
					if (flag)
					{
						bepInExErrorLogInterpolatedStringHandler.AppendLiteral("生成多重粒子效果异常: ");
						bepInExErrorLogInterpolatedStringHandler.AppendFormatted<string>(ex.Message);
					}
					log.LogError(bepInExErrorLogInterpolatedStringHandler);
				}
			}
		}

		public float spawnAnimationDuration = 1f;
		public float animationSpeedMultiplier = 0.6f;
		public ParticleType spawnParticleType = (ParticleType)11;
		public Vector2 particleOffset = new Vector2(0f, 0.7f);
		private static ZombieSpawner _instance;
		private List<ZombieSpawner.SpawnTask> activeTasks = new List<ZombieSpawner.SpawnTask>();
		private List<ZombieSpawner.DespawnTask> activeDespawnTasks = new List<ZombieSpawner.DespawnTask>();

		private class SpawnTask
		{
			public GameObject zombieObj;
			public float elapsedTime;
			public float duration;
			public Vector3 targetScale = Vector3.one;
			public Animator animator;
			public bool particleSpawned;
			public bool soundPlayed;
			public int lastCount;
		}

		private class DespawnTask
		{
			public GameObject zombieObj;
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
