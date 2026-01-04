using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateGoldJackBoxZombieMod
{
	public static class CoinFlyAnimationManager
	{
		public static void AddFlyingCoin(GameObject coin, Vector3 targetPosition, float duration = 0.8f)
		{
			if (coin == null)
			{
				return;
			}
			CoinFlyAnimationManager.FlyingCoin item = new CoinFlyAnimationManager.FlyingCoin(coin, coin.transform.position, targetPosition, duration);
			CoinFlyAnimationManager.flyingCoins.Add(item);
		}

		public static void UpdateFlyingCoins(Action finshCallback)
		{
			for (int i = CoinFlyAnimationManager.flyingCoins.Count - 1; i >= 0; i--)
			{
				CoinFlyAnimationManager.FlyingCoin flyingCoin = CoinFlyAnimationManager.flyingCoins[i];
				if (flyingCoin.coin == null)
				{
					CoinFlyAnimationManager.flyingCoins.RemoveAt(i);
				}
				else
				{
					flyingCoin.elapsedTime += Time.deltaTime;
					float num = Mathf.Clamp01(flyingCoin.elapsedTime / flyingCoin.duration);
					flyingCoin.coin.transform.position = Vector3.Lerp(flyingCoin.startPosition, flyingCoin.targetPosition, num);
					float num2 = Mathf.Lerp(3f, 0.5f, num);
					flyingCoin.coin.transform.localScale = Vector3.one * num2;
					if (num >= 1f)
					{
						if (finshCallback != null)
						{
							finshCallback();
						}
						UnityEngine.Object.Destroy(flyingCoin.coin);
						CoinFlyAnimationManager.flyingCoins.RemoveAt(i);
					}
				}
			}
		}

		public static void Clear()
		{
			// 销毁所有飞行中的金币对象
			for (int i = CoinFlyAnimationManager.flyingCoins.Count - 1; i >= 0; i--)
			{
				CoinFlyAnimationManager.FlyingCoin flyingCoin = CoinFlyAnimationManager.flyingCoins[i];
				if (flyingCoin.coin != null)
				{
					UnityEngine.Object.Destroy(flyingCoin.coin);
				}
			}
			CoinFlyAnimationManager.flyingCoins.Clear();
		}

		private static List<CoinFlyAnimationManager.FlyingCoin> flyingCoins = new List<CoinFlyAnimationManager.FlyingCoin>();

		private class FlyingCoin
		{
			public FlyingCoin(GameObject coin, Vector3 start, Vector3 target, float duration)
			{
				this.coin = coin;
				this.startPosition = start;
				this.targetPosition = target;
				this.duration = duration;
				this.elapsedTime = 0f;
			}

			public GameObject coin;
			public Vector3 startPosition;
			public Vector3 targetPosition;
			public float duration;
			public float elapsedTime;
		}
	}
}
