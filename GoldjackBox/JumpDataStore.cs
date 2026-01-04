using System.Collections.Generic;
using UnityEngine;

namespace UltimateGoldJackBoxZombieMod
{
    public static class JumpDataStore
    {
        private static Dictionary<int, JumpData> _dataDict = new Dictionary<int, JumpData>();

        public static JumpData GetOrCreate(Jackbox_c instance)
        {
            int instanceID = instance.GetInstanceID();
            if (!_dataDict.ContainsKey(instanceID))
            {
                _dataDict[instanceID] = new JumpData();
            }
            return _dataDict[instanceID];
        }

        public static void Remove(Jackbox_c instance)
        {
            _dataDict.Remove(instance.GetInstanceID());
        }

        public static void Clear()
        {
            _dataDict.Clear();
        }

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
