using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateGoldJackBoxZombieMod
{
    public static class CoinFlyAnimationManager
    {
        private static List<FlyingCoin> flyingCoins = new List<FlyingCoin>();

        public static void AddFlyingCoin(GameObject coin, Vector3 targetPosition, float duration = 0.8f)
        {
            if (coin == null) return;
            FlyingCoin item = new FlyingCoin(coin, coin.transform.position, targetPosition, duration);
            flyingCoins.Add(item);
        }

        public static void UpdateFlyingCoins(Action finshCallback)
        {
            for (int i = flyingCoins.Count - 1; i >= 0; i--)
            {
                FlyingCoin flyingCoin = flyingCoins[i];
                if (flyingCoin.coin == null)
                {
                    flyingCoins.RemoveAt(i);
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
                        finshCallback?.Invoke();
                        UnityEngine.Object.Destroy(flyingCoin.coin);
                        flyingCoins.RemoveAt(i);
                    }
                }
            }
        }

        public static void Clear()
        {
            flyingCoins.Clear();
        }

        private class FlyingCoin
        {
            public GameObject coin;
            public Vector3 startPosition;
            public Vector3 targetPosition;
            public float duration;
            public float elapsedTime;

            public FlyingCoin(GameObject coin, Vector3 start, Vector3 target, float duration)
            {
                this.coin = coin;
                this.startPosition = start;
                this.targetPosition = target;
                this.duration = duration;
                this.elapsedTime = 0f;
            }
        }
    }
}
