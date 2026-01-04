using System;
using System.Collections;
using BepInEx.Unity.IL2CPP.Utils;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;

namespace UltimateGoldJackBoxZombieMod
{
	public class DelayRunner : MonoBehaviour
	{
		public static DelayRunner Instance
		{
			get
			{
				if (DelayRunner._instance == null)
				{
					GameObject gameObject = new GameObject("DelayRunner");
					UnityEngine.Object.DontDestroyOnLoad(gameObject);
					DelayRunner._instance = gameObject.AddComponent<DelayRunner>();
				}
				return DelayRunner._instance;
			}
		}

		public void Delay(float seconds, Action action)
		{
			MonoBehaviourExtensions.StartCoroutine(this, this.DelayCoroutineImpl(seconds, action));
		}

		[HideFromIl2Cpp]
		private IEnumerator DelayCoroutineImpl(float seconds, Action action)
		{
			yield return new WaitForSeconds(seconds);
			action?.Invoke();
		}

		private static DelayRunner _instance;
	}
}
