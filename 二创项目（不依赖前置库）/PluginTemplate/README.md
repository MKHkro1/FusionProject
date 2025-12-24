# 二创植物插件模板

这是一个不依赖 CustomizeLib 前置库的 PVZ 杂交版二创植物插件模板。

## 快速开始

### 1. 复制模板

将整个 `PluginTemplate` 文件夹复制并重命名为你的插件名，例如 `MyAwesomePlant`。

### 2. 修改文件

#### Core.cs
```csharp
// 修改插件信息
public const string PLUGIN_GUID = "yourname.myawesomeplant";
public const string PLUGIN_NAME = "MyAwesomePlant";
public const string PLUGIN_VERSION = "1.0.0";
public const string BUNDLE_NAME = "myawesomeplant";

// 修改命名空间
namespace MyAwesomePlant.BepInEx

// 修改类型注册
ClassInjector.RegisterTypeInIl2Cpp<MyAwesomePlantComponent>();
```

#### TemplateComponent.cs
```csharp
// 修改命名空间
namespace MyAwesomePlant.BepInEx

// 修改类名
public class MyAwesomePlantComponent : MonoBehaviour

// 修改植物ID（避免冲突，建议1900+）
public static int PlantID = 2000;

// 修改植物名称
public static string PlantName = "我的超棒植物";

// 实现你的植物逻辑
protected override void OnAnimShoot() { ... }
```

#### Patches.cs
```csharp
// 修改命名空间
namespace MyAwesomePlant.BepInEx

// 修改资源名称
GameObject? prefab = assetBundle.LoadAsset("MyAwesomePlantPrefab")?.TryCast<GameObject>();
GameObject? preview = assetBundle.LoadAsset("MyAwesomePlantPreview")?.TryCast<GameObject>();

// 修改基类组件（根据植物类型）
var baseComponent = prefab.GetComponent<PeaShooter>();  // 或 Imitater, Chomper 等

// 修改 PlantData 属性
data.field_Public_Int32_0 = 300;   // 血量
data.field_Public_Int32_1 = 100;   // 阳光花费
data.field_Public_Single_2 = 7.5f; // 冷却时间
data.attackDamage = 20;            // 攻击伤害

// 修改图鉴文本
private static readonly string AlmanacTitle = "我的超棒植物 (2000)";
private static readonly string AlmanacDescription = "...";
```

#### PluginTemplate.BepInEx.csproj
```xml
<!-- 修改程序集名称 -->
<AssemblyName>MyAwesomePlant.BepInEx</AssemblyName>

<!-- 修改嵌入资源 -->
<EmbeddedResource Include="myawesomeplant">
  <LogicalName>myawesomeplant</LogicalName>
</EmbeddedResource>
```

### 3. 准备 AssetBundle

1. 在 Unity 中创建预制体：
   - `MyAwesomePlantPrefab` - 植物预制体
   - `MyAwesomePlantPreview` - 预览图

2. 预制体要求：
   - 必须有 `axis` 子对象
   - 包含 Animator 和动画
   - 设置动画事件（AnimShoot、AnimSpawn 等）

3. 打包 AssetBundle 并放入项目目录

### 4. 构建

```powershell
dotnet build -c Release
```

### 5. 部署

将 `bin/Release/net6.0/MyAwesomePlant.BepInEx.dll` 复制到：
```
游戏目录/BepInEx/plugins/
```

## 文件说明

| 文件 | 说明 |
|-----|------|
| `Core.cs` | 插件入口，初始化和资源加载 |
| `TemplateComponent.cs` | 植物逻辑组件 |
| `Patches.cs` | Harmony 补丁，植物注册 |
| `CustomCardRegistry.cs` | 彩卡注册（通常不需修改） |
| `.csproj` | 项目配置 |

## 常见基类

| 基类 | 适用场景 |
|-----|---------|
| `PeaShooter` | 射击类 |
| `Imitater` | 模仿者类 |
| `Chomper` | 食人花类 |
| `WallNut` | 坚果类 |
| `Sunflower` | 向日葵类 |
| `Fume` | 大喷菇类 |

## 注意事项

1. 植物ID必须唯一，建议使用1900+
2. 所有 MonoBehaviour 子类必须有 `(IntPtr ptr)` 构造函数
3. 自定义组件必须通过 `ClassInjector.RegisterTypeInIl2Cpp<T>()` 注册
4. 预制体必须设置 `tag = "Plant"`
5. 预览图必须设置 `tag = "Preview"`
