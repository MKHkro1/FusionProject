using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using CustomizeLib.BepInEx;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace UltimateGoldJackBoxZombieMod
{
    [BepInPlugin("com.deqing.UltimateGoldJackBoxZombieMod", "UltimateGoldJackBoxZombieMod", "1.0.0")]
    public class Plugin : BasePlugin
    {
        public static new ManualLogSource? Log;
        public static ZombieRecordManager zombieRecordManager = new ZombieRecordManager(5);
        public static GoldRecordManager goldManager = new GoldRecordManager();
        public static ZombieType theNewZombieType;
        public static int buff0 = -1;
        public static int theNewTravelId = -1;
        public Harmony? _harmony;

        // 预览图相关
        private const int PreviewSpriteId = 801;
        internal static Sprite? _pendingZombieSprite;

        public override void Load()
        {
            // 不要调用 Harmony.UnpatchAll()，这会移除其他插件的Patch
            if (Log == null)
            {
                Log = new ManualLogSource("UltimateGoldJackBoxZombieMod");
                BepInEx.Logging.Logger.Sources.Add(Log);
            }
            _harmony = new Harmony("com.jackboxoverride.mod");
            _harmony.PatchAll();
            ClassInjector.RegisterTypeInIl2Cpp<UltimateGoldJackBox>();
            ClassInjector.RegisterTypeInIl2Cpp<ZombieSpawner>();
            RegisterNewZombieType();
            LoadAssetBundle();
            Log.LogInfo("UltimateGoldJackBoxZombieMod loaded");
            // 词条已取消
        }

        public static void LoadAssetBundle()
        {
            AssetBundle? assetBundle = CustomizeLib.BepInEx.CustomCore.GetAssetBundle(Assembly.GetExecutingAssembly(), "goldjackbox");
            if (assetBundle == null)
            {
                Log?.LogError("究极金丑插件: 无法加载金丑资源包");
                return;
            }
            foreach (UnityEngine.Object obj in assetBundle.LoadAllAssets())
            {
                Log?.LogInfo($"asset: {obj.name}, type: {obj.GetType().FullName}");
            }
            GameObject? asset = assetBundle.GetAsset<GameObject>("JackboxJumpZombie");
            if (asset == null)
            {
                Log?.LogError("究极金丑插件: 无法从AssetBundle中获取UltimateGoldJackBox预制体");
                return;
            }
            Log?.LogInfo("究极金丑插件: 成功获取UltimateGoldJackBox预制体");

            // 加载预览图AssetBundle
            AssetBundle? previewBundle = CustomizeLib.BepInEx.CustomCore.GetAssetBundle(Assembly.GetExecutingAssembly(), "jackboxjumppreview");
            if (previewBundle != null)
            {
                // 先列出所有资源名称用于调试
                foreach (UnityEngine.Object obj in previewBundle.LoadAllAssets())
                {
                    Log?.LogInfo($"preview asset: {obj.name}, type: {obj.GetType().FullName}");

                    // IL2CPP环境下使用TryCast进行类型转换
                    GameObject? go = obj.TryCast<GameObject>();
                    if (go != null)
                    {
                        SpriteRenderer? sr = go.GetComponent<SpriteRenderer>();
                        if (sr != null && sr.sprite != null)
                        {
                            _pendingZombieSprite = sr.sprite;
                            CustomizeLib.BepInEx.CustomCore.RegisterCustomSprite(PreviewSpriteId, _pendingZombieSprite);
                            Log?.LogInfo($"究极金丑插件: 成功从预制体 {obj.name} 加载预览图Sprite");
                            break;
                        }
                    }

                    // 尝试直接转换为Sprite
                    Sprite? sprite = obj.TryCast<Sprite>();
                    if (sprite != null)
                    {
                        _pendingZombieSprite = sprite;
                        CustomizeLib.BepInEx.CustomCore.RegisterCustomSprite(PreviewSpriteId, _pendingZombieSprite);
                        Log?.LogInfo($"究极金丑插件: 成功直接加载预览图Sprite {obj.name}");
                        break;
                    }
                }

                if (_pendingZombieSprite == null)
                {
                    Log?.LogWarning("究极金丑插件: 无法从预览图AssetBundle中获取Sprite");
                }
            }
            else
            {
                Log?.LogWarning("究极金丑插件: 无法加载预览图资源包jackboxjumppreview");
            }

            // 注册僵尸，使用预览图SpriteId（如果加载成功）
            int spriteId = _pendingZombieSprite != null ? PreviewSpriteId : -1;
            CustomizeLib.BepInEx.CustomCore.RegisterCustomZombie<Jackbox_c, UltimateGoldJackBox>(theNewZombieType, asset, spriteId, 100, 72003, 0, 0);
            string name = $"究极黄金玩偶匣跳跳王({(int)theNewZombieType})";
            string description = "<color=#3D1400>作者：</color><color=#FF0000>不爱染发的赛亚人、文枭S、铁甲机鱼、梧萱梦汐X</color>\n<color=#3D1400>韧性：</color><color=red>54000</color>\n<color=#3D1400>伤害：</color><color=red>1000</color>\n<color=#3D1400>特点：</color><color=red>领袖僵尸。免疫击退、定身、冻结、寒冷、蒜毒、黄油、啃咬。未下跳杆时减伤40%，限伤5000；下跳杆后减伤70%，限伤3000。每次爆炸/下跳杆/死亡时，复活周围3x3血量最高的最多5个僵尸，不包含自身类型，包括究极僵尸。爆炸时同时吸取复活僵尸血量的金币数并随机传送到最右侧随机一行。下跳杆时也会爆炸，不会传送。小跳间隔时间为2～5秒。</color>\n<color=red>只有周围死亡僵尸超过随机3～5个且僵尸总血量（包括一级防具）超过随机15000～54000血量（血量会因不同模式有调整）时才下跳杆。下跳杆后，每5秒吸取3000～8000金币，并恢复金币数/10的血量，到达100%时不再回血。下跳杆后周围的僵尸减伤50%，限伤3000，并免疫定身、击退和黄油。当吸取金币数到达60000时，获得一次复活机会。（复活：从最右侧随机一行复活）\n当全程没有下跳杆死亡时，执行复活机制（一次）。在场时，不能魅惑跳跳类僵尸。\n与玩偶匣跳跳王有5%伴生。</color>\n";
            CustomizeLib.BepInEx.CustomCore.AddZombieAlmanacStrings((int)theNewZombieType, name, description);
        }

        private void RegisterNewZombieType()
        {
            Array values = Enum.GetValues(typeof(ZombieType));
            int num = 0;
            foreach (object obj in values)
            {
                ZombieType zombieType = (ZombieType)obj;
                if ((int)zombieType > num)
                {
                    num = (int)zombieType;
                }
            }
            theNewZombieType = (ZombieType)(num + 1);
            Log?.LogInfo($"RegisterNewZombieType theNewZombieType:{theNewZombieType}");
        }

        public static void TeleportPosition(Jackbox_c zombie, int lastCount = 0)
        {
            int rowNum = Board.Instance.rowNum;
            int row = UnityEngine.Random.Range(0, rowNum);
            Vector3 position = zombie.axis.position;
            // 传递lastCount-1，表示已经使用了一次复活机会
            // 如果lastCount为0，则传递-1表示不能再复活
            int newLastCount = lastCount > 0 ? lastCount - 1 : -1;
            ZombieSpawner.Instance.SpawnZombieAt(position, newLastCount, (int)theNewZombieType, true, 9f, row);
        }

        public static void CallBase<TBase>(object instance, string methodName, params object[] args)
        {
            MethodInfo? methodInfo = AccessTools.Method(typeof(TBase), methodName, null, null);
            methodInfo?.Invoke(instance, args);
        }

        public static void HandleSmallJumpCycle(Jackbox_c instance, JumpDataStore.JumpData data, Vector3 currentPos)
        {
            if (data.IsInSmallJump) return;
            if (instance.rb != null)
            {
                instance.rb.velocity = Vector2.zero;
            }
            data.SmallJumpTimer += Time.deltaTime;
            if (data.SmallJumpTimer >= data.NextSmallJumpTime)
            {
                TriggerSmallJump(instance, data, currentPos);
                data.SmallJumpTimer = 0f;
                data.NextSmallJumpTime = UnityEngine.Random.Range(2f, 6f);
            }
        }

        public static float GetLandY(Jackbox_c instance, float x)
        {
            try
            {
                Mouse? mouse = Mouse.Instance;
                if (mouse == null) return 0f;
                float num = mouse.GetLandY(x, instance.theZombieRow);
                if (instance.board != null && instance.board.boardTag.isRoof)
                {
                    num += 0.3f;
                }
                return num;
            }
            catch
            {
                return 0f;
            }
        }

        public static float getHealthInTravel()
        {
            int theCurrentSurvivalRound = Board.Instance.theCurrentSurvivalRound;
            float num = (float)theCurrentSurvivalRound * 0.2f;
            if (Board.Instance.boardTag.isRogue)
            {
                if (theCurrentSurvivalRound >= 1)
                {
                    int num2 = (theCurrentSurvivalRound < 4) ? theCurrentSurvivalRound : 4;
                    num += (float)num2 * 0.2f;
                }
                if (theCurrentSurvivalRound >= 5)
                {
                    int num3 = (theCurrentSurvivalRound - 4 < 4) ? (theCurrentSurvivalRound - 4) : 4;
                    num += (float)num3 * 0.4f;
                }
                if (theCurrentSurvivalRound >= 9)
                {
                    int num4 = (theCurrentSurvivalRound - 8 < 4) ? (theCurrentSurvivalRound - 8) : 4;
                    num += (float)num4 * 0.6f;
                }
                if (theCurrentSurvivalRound >= 13)
                {
                    int num5 = (theCurrentSurvivalRound - 12 < 4) ? (theCurrentSurvivalRound - 12) : 4;
                    num += (float)num5;
                }
            }
            if (GameAPP.difficulty == 5)
            {
                num *= 1.5f;
            }
            if (Board.Instance.boardTag.isEndless)
            {
                num = (float)theCurrentSurvivalRound * 0.2f;
            }
            if (Lawnf.TravelCurse())
            {
                num += num;
            }
            return Mathf.Min(100f, num + 1f);
        }

        public static void zombieEvent(Zombie __instance, List<ZombieDieRecord> recodes, string msg)
        {
            foreach (ZombieDieRecord record in recodes)
            {
                float boxXFromColumn = Mouse.Instance.GetBoxXFromColumn(record.col);
                CreateZombie.Instance.SetZombie(record.row, record.zombieType, boxXFromColumn, false).GetComponent<Zombie>();
                float boxYFromRow = Mouse.Instance.GetBoxYFromRow(record.row);
                Vector3 position = new Vector3(boxXFromColumn, boxYFromRow, 0f);
                ZombieSpawner.Instance.SpawnZombieAt(position, 0, (int)record.zombieType, false, boxXFromColumn, record.row);
            }
        }

        public static void TriggerSmallJump(Jackbox_c instance, JumpDataStore.JumpData data, Vector3 currentPos)
        {
            data.IsInSmallJump = true;
            data.SmallJumpProgress = 0f;
            data.SmallJumpStartX = currentPos.x;
            data.SmallJumpTargetX = currentPos.x - 1f;
            if (instance.anim != null)
            {
                instance.anim.SetTrigger("jump");
            }
            try
            {
                GameAPP.PlaySound(109, 0.5f, 1f);
            }
            catch { }
        }
    }
}
