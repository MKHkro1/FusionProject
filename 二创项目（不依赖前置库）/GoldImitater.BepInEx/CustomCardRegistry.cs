using System;
using System.Collections.Generic;
using UnityEngine;

namespace GoldImitater.BepInEx
{
    /// <summary>
    /// 自定义卡片注册表（用于彩卡注册）
    /// </summary>
    internal static class CustomCardRegistry
    {
        // 存储自定义卡片：PlantType -> 父Transform获取函数列表
        internal static readonly Dictionary<PlantType, List<Func<Transform?>>> CustomCards 
            = new Dictionary<PlantType, List<Func<Transform?>>>();

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
        /// </summary>
        internal static Transform? GetColorfulCardParent()
        {
            try
            {
                // 检查是否在游戏中
                if (Board.Instance == null)
                    return null;

                // 非IZ模式
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
                else
                {
                    // IZ模式
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
                Core.Logger?.LogWarning($"[GoldImitater] 获取彩卡父节点失败：{ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 获取彩卡模板GameObject（CattailGirl）
        /// </summary>
        internal static GameObject? GetColorfulCardGameObject()
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
                Core.Logger?.LogWarning($"[GoldImitater] 获取彩卡模板失败：{ex.Message}");
            }
            return null;
        }
    }
}
