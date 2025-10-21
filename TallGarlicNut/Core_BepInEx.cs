using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using CustomizeLib.BepInEx;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace TallGarlicNut.BepInEx
{
    /// TallGarlicNut插件的主入口类
    /// 负责插件的初始化、植物注册和配置
    [BepInPlugin("TallGarlicNut.BepInEx", "TallGarlicNut", "1.0.0")]
    public class Core : BasePlugin
    {
        #region 常量定义
        ///高坚果植物ID
        private const int TALL_NUT_PLANT_ID = 1027;
        
        ///大蒜植物ID
        private const int GARLIC_PLANT_ID = 29;
        
        ///大蒜坚果植物ID
        private const int GARLIC_NUT_PLANT_ID = 1278;
        
        ///肥料植物ID
        private const int FERTILIZER_PLANT_ID = 1278;
        
        ///植物韧性值
        private const int PLANT_TOUGHNESS = 8000;
        
        ///植物冷却时间
        private const float PLANT_COOLDOWN = 20f;
        
        ///植物阳光消耗
        private const int PLANT_SUN_COST = 250;
        #endregion

        #region 插件加载
        /// 插件加载入口点
        /// 执行所有必要初始化操作
        public override void Load()
        {
            try
            {
                // 设置控制台编码为UTF-8，确保中文显示正常
                Console.OutputEncoding = Encoding.UTF8;
                
                Log.LogInfo("TallGarlicNut: 开始加载插件...");
                
                // 注册Harmony补丁
                RegisterHarmonyPatches();
                
                // 注册自定义类型到IL2CPP
                RegisterCustomTypes();
                
                // 注册自定义植物
                RegisterCustomPlant();
                
                // 注册植物图鉴信息
                RegisterPlantAlmanac();
                
                // 注册肥料使用事件
                RegisterFertilizerEvent();
                
                Log.LogInfo($"TallGarlicNut: 插件加载完成，植物ID: {TallGarlicNut.PlantID}");
            }
            catch (Exception ex)
            {
                Log.LogError($"TallGarlicNut: 插件加载失败: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }
        #endregion

        #region 私有方法
        /// 注册Harmony补丁
        /// 用于拦截和修改游戏原有逻辑
        private void RegisterHarmonyPatches()
        {
            try
            {
                Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
                Log.LogInfo("TallGarlicNut: Harmony补丁已注册");
            }
            catch (Exception ex)
            {
                Log.LogError($"TallGarlicNut: 注册Harmony补丁失败: {ex.Message}");
                throw;
            }
        }
        
        /// 注册自定义类型到IL2CPP
        /// 使自定义组件能够被Unity识别和使用
        private void RegisterCustomTypes()
        {
            try
            {
                ClassInjector.RegisterTypeInIl2Cpp<TallGarlicNut>();
                Log.LogInfo("TallGarlicNut: 类型已注册到Il2Cpp");
            }
            catch (Exception ex)
            {
                Log.LogError($"TallGarlicNut: 注册自定义类型失败: {ex.Message}");
                throw;
            }
        }
        
        /// 注册自定义植物
        /// 将植物添加到游戏中，包括模型、预览图和属性配置
        private void RegisterCustomPlant()
        {
            try
            {
                // 获取植物资源包
                AssetBundle assetBundle = CustomCore.GetAssetBundle(Assembly.GetExecutingAssembly(), "tallgarlicnut");
                
                // 定义融合配方：高坚果+大蒜 或 大蒜坚果+肥料
                var fusionRecipes = new List<ValueTuple<int, int>>
                {
                    new ValueTuple<int, int>(TALL_NUT_PLANT_ID, GARLIC_PLANT_ID),  // 高坚果+大蒜
                    new ValueTuple<int, int>(GARLIC_PLANT_ID, TALL_NUT_PLANT_ID)   // 大蒜+高坚果
                };

                // 注册植物到游戏系统
                CustomCore.RegisterCustomPlant<Garlic, TallGarlicNut>(
                    TallGarlicNut.PlantID,                                    // 植物ID
                    assetBundle.GetAsset<GameObject>("TallGarlicNutPrefab"),  // 植物预制体
                    assetBundle.GetAsset<GameObject>("TallGarlicNutPreview"), // 植物预览图
                    fusionRecipes,                                            // 融合配方
                    0f,                                                       // 种植延迟
                    1f,                                                       // 生长时间
                    0,                                                        // 特殊属性
                    PLANT_TOUGHNESS,                                          // 韧性值
                    PLANT_COOLDOWN,                                           // 冷却时间
                    PLANT_SUN_COST                                            // 阳光消耗
                );

                // 将植物标记为高坚果类型，享受相关属性加成
                CustomCore.TypeMgrExtra.IsTallNut.Add((PlantType)TallGarlicNut.PlantID);
                
                Log.LogInfo($"TallGarlicNut: 植物已注册，PlantID: {TallGarlicNut.PlantID}");
            }
            catch (Exception ex)
            {
                Log.LogError($"TallGarlicNut: 注册自定义植物失败: {ex.Message}");
                throw;
            }
        }
        
        /// 注册植物图鉴信息
        /// 为植物添加详细的描述和技能说明
        private void RegisterPlantAlmanac()
        {
            try
            {
                string plantName = $"内鬼-蒜毒高坚果({TallGarlicNut.PlantID})";
                string plantDescription = BuildPlantDescription();
                
                CustomCore.AddPlantAlmanacStrings(TallGarlicNut.PlantID, plantName, plantDescription);
                Log.LogInfo("TallGarlicNut: 植物图鉴信息已注册");
            }
            catch (Exception ex)
            {
                Log.LogError($"TallGarlicNut: 注册植物图鉴失败: {ex.Message}");
                throw;
            }
        }
        
        /// 构建植物描述文本
        /// <returns>格式化的植物描述</returns>
        private string BuildPlantDescription()
        {
            return @"护植神器？内鬼？谁说的清楚呢。

<color=#3D1400>作者:</color><color=red>慕容孤晴、梧萱梦汐X</color>
<color=#3D1400>韧性:</color><color=red>8000</color>
<color=#3D1400>特点:</color><color=red>无法被搭梯子，免疫碾压与砸击(不防爆)，被啃咬时使啃咬者中毒10秒并赋予2点蒜值，随后换行。</color>
<color=#3D1400>融合配方:</color><color=red>高坚果+大蒜/大蒜坚果+肥料</color>
<color=#3D1400>专属技能:</color><color=red>①伪·父爱如山 ②真·父爱如山 ③感同身受 ④通敌叛国 ⑤片甲不留</color>

<color=blue>伪·父爱如山:</color><color=red>概率使大蒜坚果失去换行效果，同时技能感同身受与真·父爱如山失效。(仅对大蒜坚果第一口啃咬生效，第二口啃咬时本技能失效)</color>

<color=blue>真·父爱如山:</color><color=red>场上所有植物被啃咬时使啃咬者中毒10秒并赋予2点蒜值。</color>

<color=blue>感同身受:</color><color=red>场上植物被啃咬时，内鬼-蒜毒高坚果将会受到15点伤害。</color>

<color=blue>通敌叛国:</color><color=red>当自身血量处于不同阈值时触发不同效果。</color>
<color=purple>7000-8000:</color><color=red>不定时随机召唤编号0~43的僵尸，无上限。</color>
<color=purple>6000-7000:</color><color=red>不定时随机召唤编号47~72的僵尸，无上限。</color>
<color=purple>5000-6000:</color><color=red>不定时随机召唤编号100~125的僵尸，无上限。</color>
<color=purple>4000-5000:</color><color=red>不定时随机召唤编号200~217的僵尸，无上限。</color>
<color=purple>3000-4000:</color><color=red>不定时随机召唤领袖僵尸，无上限。</color>
<color=purple>2000-3000:</color><color=red>不定时随机召唤1~3级僵尸，无上限。</color>
<color=purple>1000-2000:</color><color=red>不定时随机所有僵尸(含僵王及二创僵尸)，无上限。</color>
<color=purple>1000血量以下:</color><color=red>概率献祭自身触发技能片甲不留。</color>

<color=blue>片甲不留:</color><color=red>当自身血量归0(死亡/偷走/铲除)时触发，召唤一列究极黑橄榄大帅。</color>

<color=#3D1400>这是究极云杉博士做出的第一个成品，它既能让植物提升生存空间，又能使防御系统一团乱麻。</color>";
        }
        
        /// 注册肥料使用事件
        /// 允许肥料对植物产生特殊效果
        /// 实现大蒜坚果+肥料的融合配方
        private void RegisterFertilizerEvent()
        {
            try
            {
                // 注册肥料使用事件：当对大蒜坚果使用肥料时，转换为内鬼-蒜毒高坚果
                CustomCore.RegisterCustomUseFertilizeOnPlantEvent(
                    (PlantType)GARLIC_NUT_PLANT_ID,  // 目标植物：大蒜坚果 (ID: 1278)
                    (PlantType)TallGarlicNut.PlantID  // 转换结果：内鬼-蒜毒高坚果 (ID: 2032)
                );
                Log.LogInfo($"TallGarlicNut: 肥料使用事件已注册 - 大蒜坚果(ID:{GARLIC_NUT_PLANT_ID})+肥料→内鬼-蒜毒高坚果(ID:{TallGarlicNut.PlantID})");
            }
            catch (Exception ex)
            {
                Log.LogError($"TallGarlicNut: 注册肥料事件失败: {ex.Message}");
                throw;
            }
        }
        #endregion
    }
}
