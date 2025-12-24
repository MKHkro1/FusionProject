using System;
using System.Collections.Generic;
using UnityEngine;

namespace PluginTemplate.BepInEx
{
    /// <summary>
    /// 【模板】自定义卡片注册表
    /// 用于管理需要添加到彩卡库的植物
    /// 
    /// 通常不需要修改此文件，直接复制使用即可
    /// </summary>
    internal static class CustomCardRegistry
    {
        // 存储自定义卡片：PlantType -> 父节点获取函数列表
        internal static readonly Dictionary<PlantType, List<Func<Transform?>>> CustomCards
            = new Dictionary<PlantType, List<Func<Transform?>>>();

        /// <summary>
        /// 注册植物到彩卡库
        /// </summary>
        public static void RegisterToColorfulCards(PlantType plantType)
        {
            var parentGetters = new List<Func<Transform?>> { GetColorfulCardParent };

            if (!CustomCards.ContainsKey(plantType))
            {
                CustomCards.Add(plantType, parentGetters);
            }
            else
            {
                CustomCards[plantType].AddRange(parentGetters);
            }
        }

        /// <summary>
        /// 获取彩卡父节点
        /// 根据游戏模式（普通/IZ）返回不同的父节点
        /// </summary>
        internal static Transform? GetColorfulCardParent()
        {
            try
            {
                if (Board.Instance == null)
                    return null;

                // 普通模式
                if (!Board.Instance.isIZ)
                {
                    if (InGameUI.Instance != null)
                    {
                        var seedBank = InGameUI.Instance.SeedBank;
                        if (seedBank != null)
                        {
                            var parent = seedBank.transform.parent;
                            if (parent != null)
                            {
                                var colorfulCards = parent.FindChild("Bottom/SeedLibrary/Grid/ColorfulCards/Page1");
                                if (colorfulCards != null)
                                    return colorfulCards;
                            }
                        }
                    }
                }
                // IZ 模式
                else
                {
                    if (IZBottomMenu.Instance != null)
                    {
                        var plantLibrary = IZBottomMenu.Instance.plantLibrary;
                        if (plantLibrary != null)
                        {
                            var colorfulCards = plantLibrary.transform.FindChild("Grid/ColorfulCards/Page1");
                            if (colorfulCards != null)
                                return colorfulCards;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Core.Logger?.LogWarning($"[{Core.PLUGIN_NAME}] 获取彩卡父节点失败：{ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 获取彩卡模板 GameObject（使用 CattailGirl 作为模板）
        /// </summary>
        internal static GameObject? GetColorfulCardTemplate()
        {
            try
            {
                if (Board.Instance == null)
                    return null;

                if (!Board.Instance.isIZ)
                {
                    if (InGameUI.Instance != null)
                    {
                        var seedBank = InGameUI.Instance.SeedBank;
                        if (seedBank != null)
                        {
                            var parent = seedBank.transform.parent;
                            if (parent != null)
                            {
                                var cattailGirl = parent.FindChild("Bottom/SeedLibrary/Grid/ColorfulCards/Page1/CattailGirl");
                                if (cattailGirl != null)
                                    return cattailGirl.gameObject;
                            }
                        }
                    }
                }
                else
                {
                    if (IZBottomMenu.Instance != null)
                    {
                        var plantLibrary = IZBottomMenu.Instance.plantLibrary;
                        if (plantLibrary != null)
                        {
                            var cattailGirl = plantLibrary.transform.FindChild("Grid/ColorfulCards/Page1/CattailGirl");
                            if (cattailGirl != null)
                                return cattailGirl.gameObject;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Core.Logger?.LogWarning($"[{Core.PLUGIN_NAME}] 获取彩卡模板失败：{ex.Message}");
            }
            return null;
        }
    }
}
