# 不依赖 CustomizeLib 前置库的 PVZ 杂交版二创插件开发教程

本教程将详细介绍如何在不依赖 CustomizeLib 前置库的情况下，手动注册二创植物/僵尸插件到游戏中。

## 目录

1. [项目结构概述](#1-项目结构概述)
2. [必要依赖](#2-必要依赖)
3. [核心文件说明](#3-核心文件说明)
4. [插件入口类 (Core.cs)](#4-插件入口类-corecs)
5. [植物注册 (Patches.cs)](#5-植物注册-patchescs)
6. [植物逻辑实现](#6-植物逻辑实现)
7. [彩卡注册](#7-彩卡注册)
8. [图鉴文本注册](#8-图鉴文本注册)
9. [AssetBundle 资源嵌入](#9-assetbundle-资源嵌入)
10. [项目配置文件 (.csproj)](#10-项目配置文件-csproj)
11. [构建与部署](#11-构建与部署)

---

## 1. 项目结构概述

一个典型的不依赖前置库的二创插件项目结构如下：

```
MyPlant.BepInEx/
├── Core.cs                    # 插件入口，负责初始化和加载
├── MyPlant.cs                 # 植物核心逻辑组件
├── Patches.cs                 # Harmony 补丁，负责注册植物
├── CustomCardRegistry.cs      # 彩卡注册表（可选）
├── myplant                    # AssetBundle 资源文件（无扩展名）
└── MyPlant.BepInEx.csproj     # 项目配置文件
```

### 1.1 各文件职责详解

| 文件 | 职责 | 必须 |
|-----|------|-----|
| `Core.cs` | BepInEx 插件入口点，负责初始化 Harmony、注册 IL2CPP 类型、加载 AssetBundle | ✅ |
| `MyPlant.cs` | 植物的自定义逻辑组件，处理动画事件、技能效果等 | ✅ |
| `Patches.cs` | 包含所有 Harmony 补丁，负责植物注册、图鉴注册、彩卡创建等 | ✅ |
| `CustomCardRegistry.cs` | 彩卡注册辅助类，管理自定义植物在彩卡库中的显示 | ❌ |
| `myplant` | Unity AssetBundle 文件，包含预制体和预览图 | ✅ |
| `.csproj` | 项目配置，定义依赖引用和嵌入资源 | ✅ |

### 1.2 开发流程概览

```
1. 制作 AssetBundle（Unity）
   ↓
2. 创建项目结构和 .csproj
   ↓
3. 编写 Core.cs（插件入口）
   ↓
4. 编写 Patches.cs（植物注册）
   ↓
5. 编写 MyPlant.cs（植物逻辑）
   ↓
6. 构建 DLL 并部署到游戏
```

---

## 2. 必要依赖

需要引用以下 DLL 文件（位于 `libs插件依赖` 文件夹）：

### 2.1 核心依赖

| 依赖名称 | 说明 | 用途 |
|---------|------|------|
| `0Harmony.dll` | Harmony 补丁框架 | 用于 Hook 游戏方法，实现植物注册 |
| `Assembly-CSharp.dll` | 游戏主程序集 | 包含所有游戏类型定义（Plant、Zombie、Board 等） |
| `BepInEx.Core.dll` | BepInEx 核心 | 插件加载框架基础 |
| `BepInEx.Unity.IL2CPP.dll` | BepInEx IL2CPP 支持 | IL2CPP 游戏的插件支持 |

### 2.2 IL2CPP 互操作依赖

| 依赖名称 | 说明 | 用途 |
|---------|------|------|
| `Il2CppInterop.Runtime.dll` | IL2CPP 互操作运行时 | 托管代码与 IL2CPP 代码交互 |
| `Il2CppInterop.Common.dll` | IL2CPP 互操作公共库 | 基础互操作功能 |
| `Il2Cppmscorlib.dll` | IL2CPP 基础类库 | IL2CPP 版本的 .NET 基础类型 |
| `Il2CppSystem.dll` | IL2CPP System 命名空间 | IL2CPP 集合类型等 |

### 2.3 Unity 引擎依赖

| 依赖名称 | 说明 | 用途 |
|---------|------|------|
| `UnityEngine.dll` | Unity 引擎核心 | GameObject、Transform 等基础类型 |
| `UnityEngine.CoreModule.dll` | Unity 核心模块 | MonoBehaviour、Component 等 |
| `UnityEngine.AssetBundleModule.dll` | AssetBundle 支持 | 加载 AssetBundle 资源 |
| `UnityEngine.UI.dll` | Unity UI 系统 | Image、Button 等 UI 组件 |
| `Unity.TextMeshPro.dll` | TextMeshPro 文本组件 | 卡片花费文本显示 |

### 2.4 可选依赖（根据功能需要）

| 依赖名称 | 说明 | 何时需要 |
|---------|------|---------|
| `UnityEngine.Physics2DModule.dll` | 2D 物理模块 | 需要碰撞检测时 |
| `UnityEngine.AnimationModule.dll` | 动画模块 | 需要控制动画时 |
| `__Generated.dll` | 生成的互操作代码 | 某些游戏版本需要 |

---

## 3. 核心文件说明

### 3.1 关键命名空间

```csharp
// BepInEx 插件框架
using BepInEx;                          // BasePlugin、BepInPlugin 特性
using BepInEx.Logging;                  // ManualLogSource 日志
using BepInEx.Unity.IL2CPP;             // IL2CPP 插件支持

// Harmony 补丁框架
using HarmonyLib;                       // Harmony、HarmonyPatch 特性

// IL2CPP 互操作
using Il2CppInterop.Runtime.Injection;  // ClassInjector 类型注册
using Il2CppInterop.Runtime.InteropTypes.Arrays;  // Il2CppReferenceArray

// Unity 引擎
using UnityEngine;                      // GameObject、MonoBehaviour 等
using UnityEngine.UI;                   // Image、Button 等 UI 组件
using TMPro;                            // TextMeshProUGUI 文本组件

// .NET 基础
using System;                           // Exception、IntPtr 等
using System.Reflection;                // Assembly 反射
using System.Collections.Generic;       // Dictionary、List 等
```

### 3.2 IL2CPP 类型别名

由于 IL2CPP 的特殊性，需要使用别名来区分托管类型和 IL2CPP 类型：

```csharp
// IL2CPP 集合类型别名
using Il2CppGameObjectList = Il2CppSystem.Collections.Generic.List<UnityEngine.GameObject>;

// 使用示例
var list = new Il2CppGameObjectList(1);  // 创建 IL2CPP List
list.Add(prefab);                         // 添加元素
```

### 3.3 游戏核心类型说明

| 类型 | 说明 | 常用成员 |
|-----|------|---------|
| `GameAPP` | 游戏主控制器 | `resourcesManager`、`gameAPP` |
| `ResourcesManager` | 资源管理器 | `plantPrefabs`、`plantPreviews`、`allPlants` |
| `PlantDataLoader` | 植物数据加载器 | `plantData`、`plantDatas` |
| `Board` | 游戏棋盘 | `Instance`、`isIZ` |
| `CreatePlant` | 植物创建器 | `Instance`、`SetPlant()` |
| `CreateZombie` | 僵尸创建器 | `Instance`、`SetZombie()` |
| `AlmanacPlantMenu` | 植物图鉴菜单 | `PlantAlmanacData` |

---

## 4. 插件入口类 (Core.cs)

插件入口类是 BepInEx 加载插件的起点，负责：
- 注册 Harmony 补丁
- 注册自定义组件到 IL2CPP
- 加载嵌入的 AssetBundle 资源

### 4.1 BepInPlugin 特性说明

```csharp
[BepInPlugin("author.myplant", "MyPlant", "1.0.0")]
//            ^GUID            ^名称      ^版本
```

| 参数 | 说明 | 示例 |
|-----|------|------|
| GUID | 插件唯一标识符，建议格式：`作者.插件名` | `"salmon.goldimitater"` |
| Name | 插件显示名称 | `"GoldImitater"` |
| Version | 插件版本号 | `"2.0.0"` |

### 4.2 Load() 方法详解

```csharp
public override void Load()
{
    // ========== 步骤1：初始化日志 ==========
    // 设置控制台编码为 UTF8，支持中文输出
    Console.OutputEncoding = Encoding.UTF8;
    // 保存日志实例供其他类使用
    Logger = Log;

    // ========== 步骤2：注册 Harmony 补丁 ==========
    // 扫描当前程序集中所有带 [HarmonyPatch] 特性的类并应用补丁
    var harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
    // 可以通过 harmony.GetPatchedMethods() 查看补丁了哪些方法

    // ========== 步骤3：注册自定义组件到 IL2CPP ==========
    // 重要！所有继承 MonoBehaviour 的自定义类都必须注册
    // 否则 AddComponent<T>() 会失败
    ClassInjector.RegisterTypeInIl2Cpp<MyPlant>();

    // ========== 步骤4：检查游戏状态 ==========
    // 如果插件加载时游戏已经初始化，直接注册植物
    // 否则等待 GameAPP.Awake 补丁触发
    if (GameAPP.gameAPP != null)
    {
        GameAppAwakePatch.TryRegisterPlant();
    }
}
```

### 4.3 AssetBundle 加载详解

```csharp
internal static AssetBundle? LoadEmbeddedAssetBundle(string bundleName)
{
    // 使用缓存避免重复加载
    if (CachedBundle != null)
        return CachedBundle;

    // ========== 步骤1：获取嵌入资源名称 ==========
    var assembly = Assembly.GetExecutingAssembly();
    var resourceNames = assembly.GetManifestResourceNames();
    // resourceNames 格式通常为：命名空间.文件名
    // 例如：MyPlant.BepInEx.myplant

    // ========== 步骤2：查找匹配的资源 ==========
    string? matchedName = null;
    foreach (var name in resourceNames)
    {
        // 支持完全匹配或后缀匹配
        if (name.EndsWith(bundleName, StringComparison.OrdinalIgnoreCase))
        {
            matchedName = name;
            break;
        }
    }

    // ========== 步骤3：读取资源流 ==========
    using var stream = assembly.GetManifestResourceStream(matchedName);
    var bytes = new byte[stream.Length];
    stream.Read(bytes, 0, bytes.Length);

    // ========== 步骤4：从内存加载 AssetBundle ==========
    CachedBundle = AssetBundle.LoadFromMemory(bytes);
    return CachedBundle;
}
```

### 4.4 完整 Core.cs 示例

```csharp
using System;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace MyPlant.BepInEx
{
    [BepInPlugin("author.myplant", "MyPlant", "1.0.0")]
    public class Core : BasePlugin
    {
        internal static ManualLogSource? Logger;
        internal static AssetBundle? CachedBundle;

        public override void Load()
        {
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
                Logger = Log;

                Log.LogInfo("[MyPlant] 开始加载插件...");

                // 1. 注册 Harmony 补丁
                var harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
                Log.LogInfo($"[MyPlant] Harmony 补丁已注册");

                // 2. 注册自定义组件类型到 IL2CPP（重要！）
                ClassInjector.RegisterTypeInIl2Cpp<MyPlant>();

                Log.LogInfo("[MyPlant] 插件加载完成");
                
                // 3. 检查 GameAPP 是否已经初始化
                if (GameAPP.gameAPP != null)
                {
                    GameAppAwakePatch.TryRegisterPlant();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[MyPlant] 插件加载失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 从嵌入资源加载 AssetBundle
        /// </summary>
        internal static AssetBundle? LoadEmbeddedAssetBundle(string bundleName)
        {
            if (CachedBundle != null)
                return CachedBundle;

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceNames = assembly.GetManifestResourceNames();
                string? matchedName = null;

                foreach (var name in resourceNames)
                {
                    if (name.EndsWith(bundleName, StringComparison.OrdinalIgnoreCase) ||
                        name.Equals(bundleName, StringComparison.OrdinalIgnoreCase))
                    {
                        matchedName = name;
                        break;
                    }
                }

                if (matchedName == null)
                {
                    Logger?.LogError($"[MyPlant] 未找到嵌入资源: {bundleName}");
                    return null;
                }

                using var stream = assembly.GetManifestResourceStream(matchedName);
                if (stream == null)
                    return null;

                var bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);
                CachedBundle = AssetBundle.LoadFromMemory(bytes);
                return CachedBundle;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"[MyPlant] 加载嵌入资源失败: {ex.Message}");
                return null;
            }
        }
    }
}
```

---

## 5. 植物注册 (Patches.cs)

植物注册是核心步骤，需要在 `GameAPP.Awake` 之后执行。

### 5.1 注册时机

必须在 `GameAPP.Awake` 之后执行，通过 Harmony Postfix 补丁：

```csharp
[HarmonyPatch(typeof(GameAPP), "Awake")]
internal static class GameAppAwakePatch
{
    private static bool _registered = false;

    [HarmonyPostfix]
    private static void Postfix()
    {
        // 此时 GameAPP.resourcesManager 已初始化
        TryRegisterPlant();
    }

    internal static void TryRegisterPlant()
    {
        if (_registered) return;

        try
        {
            // 1. 加载 AssetBundle
            AssetBundle? assetBundle = Core.LoadEmbeddedAssetBundle("myplant");
            if (assetBundle == null) return;

            // 2. 加载预制体和预览图
            GameObject? prefab = assetBundle.LoadAsset("MyPlantPrefab")?.TryCast<GameObject>();
            GameObject? preview = assetBundle.LoadAsset("MyPlantPreview")?.TryCast<GameObject>();

            if (prefab == null || preview == null) return;

            // 3. 注册植物预制体
            ManualRegisterPlant(prefab, preview);
            
            // 4. 注册彩卡（可选）
            CustomCardRegistry.RegisterToColorfulCards((PlantType)MyPlant.PlantID);

            _registered = true;
        }
        catch (Exception ex)
        {
            Core.Logger?.LogError($"[MyPlant] 注册植物失败: {ex.Message}");
        }
    }
}
```

### 5.2 手动植物预制体注册

植物预制体注册需要将预制体添加到游戏的 `ResourcesManager` 中：

```csharp
using System;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using Il2CppGameObjectList = Il2CppSystem.Collections.Generic.List<UnityEngine.GameObject>;

private static void ManualRegisterPlant(GameObject prefab, GameObject preview)
{
    var plantType = (PlantType)MyPlant.PlantID;  // 例如 1931
    var res = GameAPP.resourcesManager;

    // ========== 第一步：设置标签 ==========
    // 游戏通过标签识别植物预制体，必须设置！
    prefab.tag = "Plant";
    preview.tag = "Preview";

    // ========== 第二步：添加自定义组件 ==========
    var myPlant = prefab.GetComponent<MyPlant>();
    if (myPlant == null)
    {
        myPlant = prefab.AddComponent<MyPlant>();
    }

    // ========== 第三步：添加基类组件 ==========
    // 根据植物类型选择合适的基类（PeaShooter、Imitater、Chomper等）
    var baseComponent = prefab.GetComponent<Imitater>();
    if (baseComponent == null)
    {
        baseComponent = prefab.AddComponent<Imitater>();
    }
    baseComponent.thePlantType = plantType;

    // ========== 第四步：设置 axis 引用 ==========
    // axis 是植物的定位点，游戏用它来确定植物位置
    var axisTransform = prefab.transform.Find("axis") ?? prefab.transform.Find("Axis");
    if (axisTransform == null)
    {
        var axisObj = new GameObject("axis");
        axisObj.transform.SetParent(prefab.transform);
        axisObj.transform.localPosition = Vector3.zero;
        axisTransform = axisObj.transform;
    }
    baseComponent.axis = axisTransform;

    // ========== 第五步：注册预制体到 plantPrefabs ==========
    res.plantPrefabs[plantType] = prefab;

    // ========== 第六步：添加到 allPlants 列表 ==========
    if (!res.allPlants.Contains(plantType))
        res.allPlants.Add(plantType);

    // ========== 第七步：注册到 _plantPrefabs 字典 ==========
    if (!res._plantPrefabs.ContainsKey(plantType))
    {
        var list = new Il2CppGameObjectList(1);
        list.Add(prefab);
        res._plantPrefabs.Add(plantType, list);
    }

    // ========== 第八步：注册预览图 ==========
    res.plantPreviews[plantType] = preview;

    if (!res._plantPreviews.ContainsKey(plantType))
    {
        var list = new Il2CppGameObjectList(1);
        list.Add(preview);
        res._plantPreviews.Add(plantType, list);
    }

    // ========== 第九步：注册 PlantData ==========
    RegisterPlantData(plantType);
}
```

### 5.3 手动 PlantData 注册

PlantData 包含植物的基础属性（血量、花费、冷却等），需要注册到 `PlantDataLoader`：

```csharp
private static void RegisterPlantData(PlantType plantType)
{
    int plantId = (int)plantType;
    
    // ========== 第一步：确保数组容量足够 ==========
    EnsurePlantDataCapacity(plantId);

    // ========== 第二步：创建 PlantData 对象 ==========
    var data = new PlantDataLoader.PlantData_();
    
    // ========== 第三步：设置植物属性 ==========
    data.field_Public_PlantType_0 = plantType;      // 植物类型
    data.field_Public_Int32_0 = 300;                // 血量/韧性
    data.field_Public_Int32_1 = 50;                 // 阳光花费
    data.field_Public_Single_0 = 0f;                // 浮点参数1
    data.field_Public_Single_1 = 0f;                // 浮点参数2
    data.field_Public_Single_2 = 15f;               // 冷却时间（秒）
    data.attackDamage = 20;                         // 攻击伤害

    // ========== 第四步：写入 plantData 数组 ==========
    if (PlantDataLoader.plantData != null && PlantDataLoader.plantData.Length > plantId)
        PlantDataLoader.plantData[plantId] = data;

    // ========== 第五步：写入 plantDatas 字典 ==========
    PlantDataLoader.plantDatas[plantType] = data;
}

// 扩容 plantData 数组（因为自定义ID可能超出原数组长度）
private static void EnsurePlantDataCapacity(int plantId)
{
    var oldArr = PlantDataLoader.plantData;
    var needed = plantId + 1;
    
    // 如果容量足够，直接返回
    if (oldArr != null && oldArr.Length > plantId)
        return;

    // 创建新数组（容量翻倍或满足需求）
    var newLen = oldArr == null ? needed : Math.Max(needed, oldArr.Length * 2);
    var newArr = new Il2CppReferenceArray<PlantDataLoader.PlantData_>(newLen);

    // 复制旧数据
    if (oldArr != null)
    {
        for (int i = 0; i < oldArr.Length; i++)
            newArr[i] = oldArr[i];
    }

    // 替换原数组
    PlantDataLoader.plantData = newArr;
}
```

### 5.4 PlantData 字段说明

| 字段 | 类型 | 说明 |
|-----|------|------|
| `field_Public_PlantType_0` | PlantType | 植物类型枚举 |
| `field_Public_Int32_0` | int | 血量/韧性 |
| `field_Public_Int32_1` | int | 阳光花费 |
| `field_Public_Single_0` | float | 浮点参数1（根据植物类型不同含义不同） |
| `field_Public_Single_1` | float | 浮点参数2 |
| `field_Public_Single_2` | float | 冷却时间（秒） |
| `attackDamage` | int | 攻击伤害 |

### 5.5 完整 Patches.cs 示例

```csharp
using System;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using Il2CppGameObjectList = Il2CppSystem.Collections.Generic.List<UnityEngine.GameObject>;

namespace MyPlant.BepInEx
{
    [HarmonyPatch(typeof(GameAPP), "Awake")]
    internal static class GameAppAwakePatch
    {
        private static bool _registered = false;

        [HarmonyPostfix]
        private static void Postfix()
        {
            TryRegisterPlant();
        }

        internal static void TryRegisterPlant()
        {
            if (_registered) return;

            try
            {
                AssetBundle? assetBundle = Core.LoadEmbeddedAssetBundle("myplant");
                if (assetBundle == null) return;

                GameObject? prefab = assetBundle.LoadAsset("MyPlantPrefab")?.TryCast<GameObject>();
                GameObject? preview = assetBundle.LoadAsset("MyPlantPreview")?.TryCast<GameObject>();

                if (prefab == null || preview == null) return;

                ManualRegisterPlant(prefab, preview);
                CustomCardRegistry.RegisterToColorfulCards((PlantType)MyPlant.PlantID);

                _registered = true;
                Core.Logger?.LogInfo($"[MyPlant] 植物注册完成，ID: {MyPlant.PlantID}");
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[MyPlant] 注册植物失败: {ex.Message}");
            }
        }

        private static void ManualRegisterPlant(GameObject prefab, GameObject preview)
        {
            var plantType = (PlantType)MyPlant.PlantID;
            var res = GameAPP.resourcesManager;

            // 设置标签
            prefab.tag = "Plant";
            preview.tag = "Preview";

            // 添加自定义组件
            if (prefab.GetComponent<MyPlant>() == null)
                prefab.AddComponent<MyPlant>();

            // 添加基类组件
            var baseComponent = prefab.GetComponent<Imitater>();
            if (baseComponent == null)
                baseComponent = prefab.AddComponent<Imitater>();
            baseComponent.thePlantType = plantType;

            // 设置 axis
            var axisTransform = prefab.transform.Find("axis") ?? prefab.transform.Find("Axis");
            if (axisTransform == null)
            {
                var axisObj = new GameObject("axis");
                axisObj.transform.SetParent(prefab.transform);
                axisObj.transform.localPosition = Vector3.zero;
                axisTransform = axisObj.transform;
            }
            baseComponent.axis = axisTransform;

            // 注册预制体
            res.plantPrefabs[plantType] = prefab;
            if (!res.allPlants.Contains(plantType))
                res.allPlants.Add(plantType);

            if (!res._plantPrefabs.ContainsKey(plantType))
            {
                var list = new Il2CppGameObjectList(1);
                list.Add(prefab);
                res._plantPrefabs.Add(plantType, list);
            }

            // 注册预览图
            res.plantPreviews[plantType] = preview;
            if (!res._plantPreviews.ContainsKey(plantType))
            {
                var list = new Il2CppGameObjectList(1);
                list.Add(preview);
                res._plantPreviews.Add(plantType, list);
            }

            // 注册 PlantData
            RegisterPlantData(plantType);
        }

        private static void RegisterPlantData(PlantType plantType)
        {
            int plantId = (int)plantType;
            EnsurePlantDataCapacity(plantId);

            var data = new PlantDataLoader.PlantData_();
            data.field_Public_PlantType_0 = plantType;
            data.field_Public_Int32_0 = 300;      // 血量
            data.field_Public_Int32_1 = 50;       // 阳光花费
            data.field_Public_Single_0 = 0f;
            data.field_Public_Single_1 = 0f;
            data.field_Public_Single_2 = 15f;     // 冷却时间
            data.attackDamage = 0;

            if (PlantDataLoader.plantData != null && PlantDataLoader.plantData.Length > plantId)
                PlantDataLoader.plantData[plantId] = data;

            PlantDataLoader.plantDatas[plantType] = data;
        }

        private static void EnsurePlantDataCapacity(int plantId)
        {
            var oldArr = PlantDataLoader.plantData;
            var needed = plantId + 1;
            if (oldArr != null && oldArr.Length > plantId)
                return;

            var newLen = oldArr == null ? needed : Math.Max(needed, oldArr.Length * 2);
            var newArr = new Il2CppReferenceArray<PlantDataLoader.PlantData_>(newLen);

            if (oldArr != null)
            {
                for (int i = 0; i < oldArr.Length; i++)
                    newArr[i] = oldArr[i];
            }

            PlantDataLoader.plantData = newArr;
        }
    }
}
```

---

## 6. 植物逻辑实现

自定义植物组件需要继承 `MonoBehaviour` 并实现 IL2CPP 构造函数。

### 6.1 IL2CPP 构造函数（必须）

```csharp
// 所有继承 MonoBehaviour 的 IL2CPP 类都必须有这个构造函数
public MyPlant(IntPtr ptr) : base(ptr) { }
```

**为什么需要这个构造函数？**
- IL2CPP 运行时创建对象时会传入一个指向原生对象的指针
- 没有这个构造函数，`AddComponent<T>()` 会抛出异常

### 6.2 获取基类组件

```csharp
// 通过属性获取关联的基类组件
public PeaShooter? plant => gameObject.GetComponent<PeaShooter>();
public Imitater? imitater => gameObject.GetComponent<Imitater>();

// 使用示例
void Update()
{
    if (plant != null)
    {
        int row = plant.thePlantRow;      // 获取植物所在行
        int col = plant.thePlantColumn;   // 获取植物所在列
        var axis = plant.axis;            // 获取定位点
    }
}
```

### 6.3 动画事件回调

动画事件是 Unity 动画系统的功能，可以在动画特定帧触发方法调用：

```csharp
// 动画事件方法名必须与 AssetBundle 中动画设置的事件名一致
public void AnimShoot()
{
    // 发射子弹逻辑
    Core.Logger?.LogInfo("[MyPlant] AnimShoot 被调用");
}

public void AnimSpawn()
{
    // 变身/生成逻辑
}

public void AnimExplode()
{
    // 爆炸逻辑
}
```

### 6.4 常用游戏 API

```csharp
// ========== 创建植物 ==========
CreatePlant.Instance?.SetPlant(
    column,           // 列（0-8）
    row,              // 行（0-4）
    plantType,        // 植物类型
    null,             // 父对象
    default,          // 颜色
    true,             // 是否播放音效
    true,             // 是否消耗阳光
    null              // 额外参数
);

// ========== 创建僵尸 ==========
CreateZombie.Instance?.SetZombie(
    row,              // 行
    zombieType,       // 僵尸类型
    xPosition,        // X 坐标
    false             // 是否为魅惑僵尸
);

// ========== 播放粒子效果 ==========
ParticleManager.Instance?.SetParticle(
    (ParticleType)11, // 粒子类型
    position,         // 位置
    row               // 行
);

// ========== 显示文本 ==========
InGameText.Instance?.ShowText("显示的文本", 3f, false);

// ========== 播放音乐 ==========
GameAPP.Instance?.PlayMusic((MusicType)18);
```

### 6.5 完整植物逻辑示例

```csharp
using System;
using UnityEngine;

namespace MyPlant.BepInEx
{
    public class MyPlant : MonoBehaviour
    {
        public static int PlantID = 1931;  // 自定义植物ID

        // IL2CPP 必须的构造函数
        public MyPlant(IntPtr ptr) : base(ptr) { }

        // 获取关联的基类组件
        public Imitater? plant => gameObject.GetComponent<Imitater>();

        private float _timer = 0f;
        private int _attackCount = 0;

        void Start()
        {
            Core.Logger?.LogInfo("[MyPlant] 植物已生成");
            _timer = 0f;
        }

        void Update()
        {
            // 每秒执行一次逻辑
            _timer += Time.deltaTime;
            if (_timer >= 1f)
            {
                _timer = 0f;
                OnSecondTick();
            }
        }

        private void OnSecondTick()
        {
            // 自定义每秒逻辑
            if (plant != null)
            {
                // 例如：回血
                plant.theHealth = Math.Min(plant.theHealth + 10, plant.theMaxHealth);
            }
        }

        // 动画事件：发射
        public void AnimShoot()
        {
            _attackCount++;
            Core.Logger?.LogInfo($"[MyPlant] 第 {_attackCount} 次攻击");
        }

        // 动画事件：变身
        public void AnimSpawn()
        {
            try
            {
                var imitater = plant;
                if (imitater == null) return;

                // 保存位置信息
                int row = imitater.thePlantRow;
                int col = imitater.thePlantColumn;

                // 让当前植物死亡
                imitater.Die((Plant.DieReason)2);

                // 生成新植物
                int random = UnityEngine.Random.Range(0, 100);
                if (random < 50)
                {
                    // 50% 概率生成豌豆射手
                    CreatePlant.Instance?.SetPlant(col, row, PlantType.Peashooter, null, default, true, true, null);
                }
                else
                {
                    // 50% 概率生成向日葵
                    CreatePlant.Instance?.SetPlant(col, row, PlantType.Sunflower, null, default, true, true, null);
                }
            }
            catch (Exception ex)
            {
                Core.Logger?.LogError($"[MyPlant] AnimSpawn 失败: {ex.Message}");
            }
        }
    }
}
```

---

## 7. 彩卡注册

如果需要将植物添加到彩卡库中，需要实现彩卡注册和 UI 创建。

### 7.1 彩卡注册表

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyPlant.BepInEx
{
    /// <summary>
    /// 自定义卡片注册表
    /// 用于管理需要添加到彩卡库的植物
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
                CustomCards.Add(plantType, parentGetters);
            else
                CustomCards[plantType].AddRange(parentGetters);
        }

        /// <summary>
        /// 获取彩卡父节点
        /// 根据游戏模式（普通/IZ）返回不同的父节点
        /// </summary>
        internal static Transform? GetColorfulCardParent()
        {
            try
            {
                if (Board.Instance == null) return null;

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
                                // 路径：Bottom/SeedLibrary/Grid/ColorfulCards/Page1
                                return parent.FindChild("Bottom/SeedLibrary/Grid/ColorfulCards/Page1");
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
                            // 路径：Grid/ColorfulCards/Page1
                            return plantLibrary.transform.FindChild("Grid/ColorfulCards/Page1");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Core.Logger?.LogWarning($"获取彩卡父节点失败：{ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 获取彩卡模板（用于克隆）
        /// 使用 CattailGirl 作为模板
        /// </summary>
        internal static GameObject? GetColorfulCardTemplate()
        {
            try
            {
                if (Board.Instance == null) return null;

                if (!Board.Instance.isIZ)
                {
                    if (InGameUI.Instance?.SeedBank != null)
                    {
                        var parent = InGameUI.Instance.SeedBank.transform.parent;
                        return parent?.FindChild("Bottom/SeedLibrary/Grid/ColorfulCards/Page1/CattailGirl")?.gameObject;
                    }
                }
                else
                {
                    if (IZBottomMenu.Instance?.plantLibrary != null)
                    {
                        return IZBottomMenu.Instance.plantLibrary.transform
                            .FindChild("Grid/ColorfulCards/Page1/CattailGirl")?.gameObject;
                    }
                }
            }
            catch (Exception ex)
            {
                Core.Logger?.LogWarning($"获取彩卡模板失败：{ex.Message}");
            }
            return null;
        }
    }
}
```

### 7.2 彩卡 UI 创建补丁

在 `SeedLibrary.Start` 时创建彩卡 UI：

```csharp
[HarmonyPatch(typeof(SeedLibrary), "Start")]
internal static class SeedLibraryStartPatch
{
    [HarmonyPostfix]
    private static void Postfix(SeedLibrary __instance)
    {
        try
        {
            // 获取彩卡模板
            var template = CustomCardRegistry.GetColorfulCardTemplate();
            if (template == null) return;

            // 遍历所有注册的自定义卡片
            foreach (var kvp in CustomCardRegistry.CustomCards)
            {
                var plantType = kvp.Key;
                var parentGetters = kvp.Value;

                foreach (var getParent in parentGetters)
                {
                    var parent = getParent();
                    if (parent == null) continue;

                    // 克隆模板
                    var cardGO = UnityEngine.Object.Instantiate(template, parent);
                    cardGO.SetActive(true);

                    // 设置卡片图标
                    var iconImage = cardGO.transform.GetChild(0)?.GetChild(0)?.GetComponent<Image>();
                    if (iconImage != null && GameAPP.resourcesManager.plantPreviews.ContainsKey(plantType))
                    {
                        var previewObj = GameAPP.resourcesManager.plantPreviews[plantType];
                        var spriteRenderer = previewObj?.GetComponent<SpriteRenderer>();
                        if (spriteRenderer != null)
                        {
                            iconImage.sprite = spriteRenderer.sprite;
                            iconImage.SetNativeSize();
                        }
                    }

                    // 设置花费文本
                    var costText = cardGO.transform.GetChild(0)?.GetChild(1)?.GetComponent<TextMeshProUGUI>();
                    if (costText != null && PlantDataLoader.plantDatas.ContainsKey(plantType))
                    {
                        var plantData = PlantDataLoader.plantDatas[plantType];
                        costText.text = plantData?.field_Public_Int32_1.ToString() ?? "0";
                    }

                    // 设置 CardUI 组件
                    var cardUI = cardGO.transform.GetChild(1)?.GetComponent<CardUI>();
                    if (cardUI != null)
                    {
                        cardUI.thePlantType = plantType;
                        cardUI.theSeedType = (int)plantType;

                        if (PlantDataLoader.plantDatas.ContainsKey(plantType))
                        {
                            var plantData = PlantDataLoader.plantDatas[plantType];
                            cardUI.theSeedCost = plantData?.field_Public_Int32_1 ?? 0;
                            cardUI.fullCD = plantData?.field_Public_Single_2 ?? 7.5f;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Core.Logger?.LogError($"创建彩卡UI失败: {ex.Message}");
        }
    }
}
```

### 7.3 彩卡路径说明

```
游戏 UI 层级结构：
├── InGameUI
│   └── SeedBank
│       └── parent
│           └── Bottom
│               └── SeedLibrary
│                   └── Grid
│                       ├── NormalCards/     # 普通卡片
│                       └── ColorfulCards/   # 彩卡
│                           ├── Page1/       # 第一页
│                           │   ├── CattailGirl  # 模板卡片
│                           │   └── ...
│                           └── Page2/       # 第二页
```

---

## 8. 图鉴文本注册

通过 Harmony 补丁注册图鉴文本，让植物在图鉴中显示名称和描述。

### 8.1 图鉴补丁实现

```csharp
[HarmonyPatch(typeof(AlmanacPlantMenu))]
internal static class AlmanacPlantMenuPatch
{
    private static readonly PlantType TargetType = (PlantType)MyPlant.PlantID;

    /// <summary>
    /// 在图鉴初始化后注册自定义植物的图鉴信息
    /// </summary>
    [HarmonyPatch(nameof(AlmanacPlantMenu.InitNameAndInfoFromJson))]
    [HarmonyPostfix]
    public static void PostInitNameAndInfoFromJson()
    {
        try
        {
            var plantInfo = new AlmanacPlantBank.PlantInfo
            {
                name = "我的植物 (1931)",
                info = "这是植物描述\n\n" +
                       "<color=#3D1400>作者：</color><color=red>@作者名</color>\n" +
                       "<color=#3D1400>特点：</color><color=red>植物特点描述</color>\n\n" +
                       "花费：<color=red>100</color>\n" +
                       "冷却时间：<color=red>7.5秒</color>"
            };
            AlmanacPlantMenu.PlantAlmanacData[TargetType] = plantInfo;
        }
        catch (Exception ex)
        {
            Core.Logger?.LogWarning($"注册图鉴文本失败: {ex.Message}");
        }
    }
}
```

### 8.2 图鉴文本格式

游戏支持 Unity 富文本标签：

| 标签 | 效果 | 示例 |
|-----|------|------|
| `<color=#RRGGBB>` | 设置颜色（十六进制） | `<color=#3D1400>标题</color>` |
| `<color=red>` | 设置颜色（颜色名） | `<color=red>重要内容</color>` |
| `<b>` | 粗体 | `<b>粗体文字</b>` |
| `<i>` | 斜体 | `<i>斜体文字</i>` |
| `<size=N>` | 字体大小 | `<size=20>大字</size>` |
| `\n` | 换行 | `第一行\n第二行` |

### 8.3 推荐的图鉴格式模板

```csharp
var info = 
    "植物的宝开语/描述文字\n\n" +
    "<color=#3D1400>画师：</color><color=red>@画师名</color>\n" +
    "<color=#3D1400>作者：</color><color=red>@作者名</color>\n\n" +
    "<color=#3D1400>韧性：</color><color=red>300</color>\n" +
    "<color=#3D1400>攻击：</color><color=red>100/2秒</color>\n" +
    "<color=#3D1400>花费：</color><color=red>50阳光</color>\n" +
    "<color=#3D1400>冷却：</color><color=red>15秒</color>\n\n" +
    "<color=#3D1400>特点：</color><color=red>详细的特点描述...</color>\n\n" +
    "<color=#3D1400>融合配方：</color><color=red>植物A + 植物B</color>";
```

### 8.4 完整图鉴示例

```csharp
var plantInfo = new AlmanacPlantBank.PlantInfo
{
    name = "黄金模仿者 (1931)",
    info = "或许是宝藏呢？\n\n" +
           "<color=#3D1400>贴图作者：</color><color=red>@林秋-AutumnLin</color>\n" +
           "<color=#3D1400>特点：</color><color=red>短时间内变身随机召唤植物或僵尸。</color>\n\n" +
           "花费：<color=red>50</color>\n" +
           "冷却时间：<color=red>15秒</color>"
};
```

---

## 9. AssetBundle 资源嵌入

### 9.1 AssetBundle 制作要求

AssetBundle 中需要包含：
- `{PlantName}Prefab` - 植物预制体（包含动画、碰撞体等）
- `{PlantName}Preview` - 植物预览图（用于卡片显示）

### 9.2 预制体结构要求

```
MyPlantPrefab (GameObject)
├── axis (GameObject)           # 必须！定位点
├── shadow (GameObject)         # 可选，阴影
├── Sprite (SpriteRenderer)     # 植物贴图
└── Animator                    # 动画控制器
```

### 9.3 预制体组件要求

| 组件 | 必须 | 说明 |
|-----|-----|------|
| `Transform` | ✅ | 位置、旋转、缩放 |
| `SpriteRenderer` | ✅ | 显示植物贴图 |
| `Animator` | ✅ | 播放动画 |
| `BoxCollider2D` | ❌ | 碰撞检测（某些植物需要） |

### 9.4 动画控制器要求

动画控制器需要包含以下状态（根据植物类型）：

| 状态名 | 说明 | 适用类型 |
|-------|------|---------|
| `idle` | 待机动画 | 所有植物 |
| `shoot` | 射击动画 | 射击类植物 |
| `chomp` / `bite` | 啃咬动画 | 食人花类 |
| `explode` | 爆炸动画 | 爆炸类植物 |
| `die` | 死亡动画 | 可选 |

### 9.5 动画事件设置

在 Unity 中为动画添加事件：

1. 打开动画窗口
2. 选择需要添加事件的帧
3. 右键 → Add Animation Event
4. 设置 Function 为代码中的方法名（如 `AnimShoot`、`AnimSpawn`）

### 9.6 预览图要求

```
MyPlantPreview (GameObject)
└── SpriteRenderer
    └── Sprite (植物卡片图标)
```

- 尺寸建议：100x100 像素
- 格式：PNG（支持透明）
- Tag：设置为 `Preview`

### 9.7 AssetBundle 打包步骤（Unity）

```csharp
// 1. 选择预制体，在 Inspector 底部设置 AssetBundle 名称
// 例如：myplant

// 2. 使用脚本打包
using UnityEditor;

public class BuildAssetBundles
{
    [MenuItem("Assets/Build AssetBundles")]
    static void BuildAllAssetBundles()
    {
        BuildPipeline.BuildAssetBundles(
            "Assets/AssetBundles",
            BuildAssetBundleOptions.None,
            BuildTarget.StandaloneWindows64
        );
    }
}
```

### 9.8 在项目中嵌入 AssetBundle

将打包好的 AssetBundle 文件（无扩展名）放入项目目录，并在 `.csproj` 中配置：

```xml
<ItemGroup>
  <EmbeddedResource Include="myplant">
    <LogicalName>myplant</LogicalName>
  </EmbeddedResource>
</ItemGroup>
```

---

## 10. 项目配置文件 (.csproj)

### 10.1 基本配置

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <!-- 目标框架：.NET 6.0 -->
    <TargetFramework>net6.0</TargetFramework>
    <!-- 启用隐式 using -->
    <ImplicitUsings>enable</ImplicitUsings>
    <!-- 启用可空引用类型 -->
    <Nullable>enable</Nullable>
    <!-- 使用最新 C# 语言版本 -->
    <LangVersion>latest</LangVersion>
    <!-- 输出路径 -->
    <OutputPath>bin\Release\</OutputPath>
    <!-- 程序集名称 -->
    <AssemblyName>MyPlant.BepInEx</AssemblyName>
  </PropertyGroup>
```

### 10.2 依赖引用配置

```xml
  <ItemGroup>
    <!-- ========== BepInEx 核心 ========== -->
    <Reference Include="BepInEx.Core">
      <HintPath>..\..\libs插件依赖\BepInEx.Core.dll</HintPath>
    </Reference>
    <Reference Include="BepInEx.Unity.IL2CPP">
      <HintPath>..\..\libs插件依赖\BepInEx.Unity.IL2CPP.dll</HintPath>
    </Reference>

    <!-- ========== Harmony 补丁框架 ========== -->
    <Reference Include="0Harmony">
      <HintPath>..\..\libs插件依赖\0Harmony.dll</HintPath>
    </Reference>

    <!-- ========== IL2CPP 互操作 ========== -->
    <Reference Include="Il2CppInterop.Runtime">
      <HintPath>..\..\libs插件依赖\Il2CppInterop.Runtime.dll</HintPath>
    </Reference>
    <Reference Include="Il2CppInterop.Common">
      <HintPath>..\..\libs插件依赖\Il2CppInterop.Common.dll</HintPath>
    </Reference>
    <Reference Include="Il2Cppmscorlib">
      <HintPath>..\..\libs插件依赖\Il2Cppmscorlib.dll</HintPath>
    </Reference>
    <Reference Include="Il2CppSystem">
      <HintPath>..\..\libs插件依赖\Il2CppSystem.dll</HintPath>
    </Reference>

    <!-- ========== 游戏程序集 ========== -->
    <Reference Include="Assembly-CSharp">
      <HintPath>..\..\libs插件依赖\Assembly-CSharp.dll</HintPath>
    </Reference>

    <!-- ========== Unity 引擎 ========== -->
    <Reference Include="UnityEngine">
      <HintPath>..\..\libs插件依赖\UnityEngine.dll</HintPath>
    </Reference>
    <Reference Include="UnityEngine.CoreModule">
      <HintPath>..\..\libs插件依赖\UnityEngine.CoreModule.dll</HintPath>
    </Reference>
    <Reference Include="UnityEngine.AssetBundleModule">
      <HintPath>..\..\libs插件依赖\UnityEngine.AssetBundleModule.dll</HintPath>
    </Reference>
    <Reference Include="UnityEngine.UI">
      <HintPath>..\..\libs插件依赖\UnityEngine.UI.dll</HintPath>
    </Reference>
    <Reference Include="UnityEngine.UIModule">
      <HintPath>..\..\libs插件依赖\UnityEngine.UIModule.dll</HintPath>
    </Reference>

    <!-- ========== TextMeshPro ========== -->
    <Reference Include="Unity.TextMeshPro">
      <HintPath>..\..\libs插件依赖\Unity.TextMeshPro.dll</HintPath>
    </Reference>

    <!-- ========== 可选：物理模块 ========== -->
    <Reference Include="UnityEngine.Physics2DModule">
      <HintPath>..\..\libs插件依赖\UnityEngine.Physics2DModule.dll</HintPath>
    </Reference>
  </ItemGroup>
```

### 10.3 嵌入资源配置

```xml
  <!-- 嵌入 AssetBundle 资源 -->
  <ItemGroup>
    <EmbeddedResource Include="myplant">
      <!-- LogicalName 是代码中加载时使用的名称 -->
      <LogicalName>myplant</LogicalName>
    </EmbeddedResource>
  </ItemGroup>

</Project>
```

### 10.4 HintPath 路径说明

`HintPath` 是相对于 `.csproj` 文件的路径：

```
项目结构示例：
插件项目Code/
├── libs插件依赖/           # 依赖 DLL 文件夹
│   ├── 0Harmony.dll
│   ├── Assembly-CSharp.dll
│   └── ...
└── 二创项目/
    └── MyPlant.BepInEx/
        ├── MyPlant.BepInEx.csproj  # 当前文件
        └── ...

HintPath 计算：
从 MyPlant.BepInEx.csproj 到 libs插件依赖：
..\..\libs插件依赖\xxx.dll
```

---

## 11. 构建与部署

### 11.1 构建命令

```powershell
# 在项目目录下执行
dotnet build -c Release

# 或者指定项目文件
dotnet build MyPlant.BepInEx.csproj -c Release
```

### 11.2 构建输出

构建成功后，DLL 文件位于：
```
MyPlant.BepInEx/bin/Release/net6.0/MyPlant.BepInEx.dll
```

### 11.3 部署步骤

1. **找到游戏目录**
   ```
   Steam: Steam\steamapps\common\PlantsVsZombies杂交版\
   ```

2. **复制 DLL 到插件目录**
   ```
   游戏目录/BepInEx/plugins/MyPlant.BepInEx.dll
   ```

3. **启动游戏测试**

### 11.4 调试技巧

**查看日志：**
```
游戏目录/BepInEx/LogOutput.log
```

**实时日志（控制台）：**
修改 `BepInEx/config/BepInEx.cfg`：
```ini
[Logging.Console]
Enabled = true
```

### 11.5 常见构建错误

| 错误 | 原因 | 解决方案 |
|-----|------|---------|
| `CS0246: 找不到类型` | 缺少依赖引用 | 检查 .csproj 中的 Reference |
| `CS0012: 类型定义在未引用的程序集中` | 依赖链不完整 | 添加缺少的依赖 DLL |
| `找不到嵌入资源` | AssetBundle 未正确嵌入 | 检查 EmbeddedResource 配置 |
| `IL2CPP 类型未注册` | 忘记调用 ClassInjector | 在 Load() 中注册类型 |

### 11.6 批量构建脚本

```powershell
# build_all.ps1
$projects = @(
    "MyPlant.BepInEx",
    "AnotherPlant.BepInEx"
)

foreach ($project in $projects) {
    Write-Host "Building $project..." -ForegroundColor Cyan
    dotnet build "$project/$project.csproj" -c Release
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed for $project" -ForegroundColor Red
        exit 1
    }
}

Write-Host "All builds completed!" -ForegroundColor Green
```

---

## 常见植物基类选择

根据植物功能选择合适的基类组件：

| 基类 | 适用场景 | 关键方法/属性 |
|-----|---------|--------------|
| `PeaShooter` | 射击类植物 | `AnimShoot()`、发射子弹 |
| `Imitater` | 模仿者类植物 | `AnimSpawn()`、变身逻辑 |
| `Chomper` | 普通食人花 | `AnimChomp()`、吞噬僵尸 |
| `UltimateChomper` | 究极食人花 | 增强版吞噬、群体攻击 |
| `WallNut` | 普通坚果 | 防御、血量管理 |
| `TallNut` | 高坚果 | 防跳跃、高大植物 |
| `Sunflower` | 向日葵类 | 生产阳光 |
| `UltimateSunflower` | 究极向日葵 | 增强生产 |
| `Fume` | 大喷菇 | 穿透攻击 |
| `UltimateFume` | 究极大喷菇 | 增强穿透 |
| `Cactus` | 仙人掌 | 对空攻击 |
| `Melon` | 投手类 | 抛物线攻击、溅射 |
| `Present` | 礼盒类 | 随机生成 |
| `DiamondImitater` | 钻石模仿者 | 随机事件 |
| `GoldDoom` | 金毁灭菇 | 爆炸效果 |
| `LaserUmbrella` | 激光伞 | 激光攻击、链接 |
| `SuperMachineNut` | 超级机械坚果 | 血量突破、反弹 |

### 基类选择建议

1. **射击类植物**：继承 `PeaShooter` 或对应的射手基类
2. **防御类植物**：继承 `WallNut`、`TallNut` 或 `SuperMachineNut`
3. **变身类植物**：继承 `Imitater` 或 `DiamondImitater`
4. **爆炸类植物**：继承 `GoldDoom` 或 `CherryJalapeno`
5. **生产类植物**：继承 `Sunflower` 或 `UltimateSunflower`

---

## 注意事项

### 关键注意点

1. **植物ID选择**
   - 避免与游戏内已有ID冲突
   - 建议使用 1900+ 的ID
   - 可以在 `植物僵尸ID集合` 文件中查看已使用的ID

2. **IL2CPP构造函数**
   - 所有继承 `MonoBehaviour` 的类必须有 `(IntPtr ptr)` 构造函数
   - 否则 `AddComponent<T>()` 会失败
   ```csharp
   public MyPlant(IntPtr ptr) : base(ptr) { }
   ```

3. **组件注册**
   - 自定义组件必须通过 `ClassInjector.RegisterTypeInIl2Cpp<T>()` 注册
   - 必须在 `Load()` 方法中注册，且在使用前注册

4. **标签设置**
   - 预制体必须设置 `tag = "Plant"`
   - 预览图必须设置 `tag = "Preview"`
   - 游戏通过标签识别对象类型

5. **axis对象**
   - 植物预制体必须有 `axis` 子对象
   - 游戏用它来确定植物的精确位置
   - 如果没有，代码中需要创建

### 常见问题排查

| 问题 | 可能原因 | 解决方案 |
|-----|---------|---------|
| 植物不显示 | 预制体未正确注册 | 检查 `plantPrefabs` 注册 |
| 卡片无图标 | 预览图未注册 | 检查 `plantPreviews` 注册 |
| 无法种植 | PlantData 未注册 | 检查 `plantDatas` 注册 |
| 图鉴无信息 | 图鉴补丁未生效 | 检查 Harmony 补丁 |
| 动画不播放 | Animator 未配置 | 检查 AssetBundle 中的动画 |
| 游戏崩溃 | IL2CPP 类型未注册 | 检查 ClassInjector 调用 |

### 版本兼容性

- 游戏版本更新可能导致 API 变化
- 建议保留旧版本依赖文件备份
- 关注 `Assembly-CSharp.dll` 的变化

---

## 参考项目

以下项目都位于 `插件项目Code/二创项目/` 目录下，可作为开发参考：

| 项目 | 说明 | 特点 |
|-----|------|------|
| `GoldImitater.BepInEx` | 黄金模仿者 | 完整示例，变身逻辑 |
| `SuperIceMachineNut` | 寒冰机械坚果 | 坚果类，反弹子弹 |
| `ObsidianFortress` | 黑曜石堡垒坚果 | 巨型植物，高防御 |
| `UltimateCherrySpruce` | 终极爆破云杉 | 射击类，追踪子弹 |
| `SuperHypnoObsidianWallNut` | 幻灭黑曜石坚果 | 爆炸效果，魅惑 |
| `OlivionLunarCabbage` | 终极寒烬月神卷心菜 | 投手类，自定义子弹 |
| `HeiTa` | 黑塔 | 模仿者类，简单示例 |
| `WaterPot` | 水盆 | 花盆类，低矮植物 |

### 推荐学习顺序

1. **入门**：`GoldImitater.BepInEx` - 了解完整项目结构
2. **射击类**：`UltimateCherrySpruce` - 学习子弹发射
3. **防御类**：`SuperIceMachineNut` - 学习坚果机制
4. **特效类**：`SuperHypnoObsidianWallNut` - 学习爆炸效果
5. **进阶**：`OlivionLunarCabbage` - 学习复杂机制

---

## 附录：快速开始模板

创建新项目时，可以复制以下文件结构：

```
MyNewPlant.BepInEx/
├── Core.cs                 # 复制并修改插件信息
├── MyNewPlant.cs           # 复制并修改植物逻辑
├── Patches.cs              # 复制并修改注册代码
├── CustomCardRegistry.cs   # 直接复制
├── mynewplant              # 你的 AssetBundle
└── MyNewPlant.BepInEx.csproj  # 复制并修改项目名和资源名
```

**修改清单：**
1. `Core.cs`：修改 `BepInPlugin` 特性、类名、日志前缀
2. `MyNewPlant.cs`：修改类名、`PlantID`、植物逻辑
3. `Patches.cs`：修改 AssetBundle 名称、预制体名称
4. `.csproj`：修改 `AssemblyName`、`EmbeddedResource`
