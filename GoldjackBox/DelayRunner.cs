using System;
using System.Collections;
using BepInEx.Unity.IL2CPP.Utils;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;

namespace UltimateGoldJackBoxZombieMod
{
    public class DelayRunner : MonoBehaviour
    {
        private static DelayRunner? _instance;

        public static DelayRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject gameObject = new GameObject("DelayRunner");
                    UnityEngine.Object.DontDestroyOnLoad(gameObject);
                    _instance = gameObject.AddComponent<DelayRunner>();
                }
                return _instance;
            }
        }

        public void Delay(float seconds, Action action)
        {
            this.StartCoroutine(DelayCoroutineImpl(seconds, action));
        }

        [HideFromIl2Cpp]
        private IEnumerator DelayCoroutineImpl(float seconds, Action action)
        {
            yield return new WaitForSeconds(seconds);
            action?.Invoke();
        }
    }
}
