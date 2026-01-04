using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UltimateGoldJackBoxZombieMod
{
	public class ZombieSmokeEffect : MonoBehaviour
	{
		private void Awake()
		{
			if (ZombieSmokeEffect.Instance == null)
			{
				ZombieSmokeEffect.Instance = this;
				UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
			}
		}

		private void PlaySmokeSound()
		{
			try
			{
				GameAPP.PlaySound(120, 0.8f, Random.Range(0.9f, 1.1f));
			}
			catch
			{
			}
		}

		private void PlaySubstitutionSound()
		{
			try
			{
				GameAPP.PlaySound(121, 1f, Random.Range(0.95f, 1.05f));
			}
			catch
			{
			}
		}

		public void PlayAppearSmoke(Vector3 worldPos, int row)
		{
			Vector2 vector;
			vector = new Vector2(worldPos.x, worldPos.y + 0.6f);
			this.PlaySmokeSound();
			this.SpawnSmoke(vector, row);
			for (int i = 0; i < 6; i++)
			{
				Vector2 vector2 = Random.insideUnitCircle * 0.5f;
				this.SpawnSmoke(vector + vector2, row);
			}
		}

		public void PlaySubstitutionDisappear(GameObject target, int row)
		{
			if (target == null)
			{
				return;
			}
			Vector3 position = target.transform.position;
			Vector2 vector;
			vector = new Vector2(position.x, position.y + 0.5f);
			this.PlaySubstitutionSound();
			for (int i = 0; i < 8; i++)
			{
				Vector2 vector2 = Random.insideUnitCircle * 0.6f;
				this.SpawnSmoke(vector + vector2, row);
			}
			for (int j = 0; j < 5; j++)
			{
				Vector2 vector3;
				vector3 = new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(0.3f, 1f));
				this.SpawnSmoke(vector + vector3, row);
			}
			target.SetActive(false);
			UnityEngine.Object.Destroy(target, 0.05f);
		}

		private void SpawnSmoke(Vector2 pos, int row)
		{
			ParticleManager instance = ParticleManager.Instance;
			if (instance == null)
			{
				return;
			}
			instance.SetParticle(this.smokeParticle, pos, row);
		}

		public static ZombieSmokeEffect Instance;
		private const int SMOKE_POP_SOUND = 120;
		private const int SUBSTITUTION_SOUND = 121;
		public ParticleType smokeParticle = (ParticleType)11;
	}
}
