using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateGoldJackBoxZombieMod
{
	public static class JumpDataStore
	{
		public static JumpDataStore.JumpData GetOrCreate(Jackbox_c instance)
		{
			int instanceID = instance.GetInstanceID();
			if (!JumpDataStore._dataDict.ContainsKey(instanceID))
			{
				JumpDataStore._dataDict[instanceID] = new JumpDataStore.JumpData();
			}
			return JumpDataStore._dataDict[instanceID];
		}

		public static void Remove(Jackbox_c instance)
		{
			JumpDataStore._dataDict.Remove(instance.GetInstanceID());
		}

		public static void Clear()
		{
			JumpDataStore._dataDict.Clear();
		}

		private static Dictionary<int, JumpDataStore.JumpData> _dataDict = new Dictionary<int, JumpDataStore.JumpData>();

		public class JumpData
		{
			public bool HasBigJumped;
			public bool IsInBigJump;
			public float SmallJumpTimer;
			public float NextSmallJumpTime;
			public bool IsInSmallJump;
			public float SmallJumpProgress;
			public float SmallJumpStartX;
			public float SmallJumpTargetX;
			public float OriginalWaitTime;
			public float OriginalJumpX;
			public Vector3 SavedPosition;
		}
	}
}
