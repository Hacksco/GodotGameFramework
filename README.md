# GGF (Godot Game Framework)

[English](README_EN.md) | **中文**

> 基于 [Unity Game Framework (UGF)](https://github.com/EllanJiang/GameFramework) 架构，为 Godot 4.x C# 重写的游戏框架。

---

## 简介

GGF（Godot Game Framework）是将 [Unity Game Framework](https://gameframework.cn/) 的核心架构和设计理念移植到 Godot 4.x 引擎（C#）的游戏框架。

GGF 采用了与 UGF 完全相同的**三层分离架构**：

```
┌─────────────────────────────────────────────────┐
│            游戏业务代码 (AAAGame)                │  ← 你的游戏逻辑
├─────────────────────────────────────────────────┤
│       Godot 引擎适配层 (GodotGameFramework)      │  ← Godot API 调用
├─────────────────────────────────────────────────┤
│           核心框架层 (GameFramework)             │  ← 引擎无关，可直接复用
└─────────────────────────────────────────────────┘
```

- **核心框架层** (`Framework/GameFramework/`) — 从 UGF 直接复用的纯 C# 代码，零引擎依赖，包含 20+ 管理器模块、引用池、事件池、任务池等基础设施
- **Godot 适配层** (`Framework/GodotGameFramework/`) — 使用 Godot API 重写的引擎适配层，将 Unity MonoBehaviour 映射为 Godot Node，将 Unity API 映射为 Godot 等价 API
- **游戏业务层** (`AAAGame/`) — 使用框架 API 编写的游戏逻辑，与引擎完全解耦

---

## 功能特性

- **14 个内置模块** — 覆盖游戏开发常用功能：事件、状态机、流程、实体、UI、音频、资源、配置、数据表、本地化、设置、对象池、数据节点、变量
- **三层分离架构** — 核心层引擎无关，适配层可替换，业务层只依赖接口
- **全局静态门面 `GF`** — 一个类访问所有框架组件，简洁高效：`GF.Entity.ShowEntity<T>()`、`GF.UI.OpenUIForm<T>()`、`GF.Sound.PlayBGM()`
- **流程驱动** — 通过 Procedure（有限状态机）管理游戏生命周期，结构清晰，易于维护
- **对象池系统** — 实体和 UI 自动回收复用，减少 GC 压力，提升性能
- **引用池系统** — 内置 `ReferencePool`，通过 `IReference` 接口实现对象零 GC 分配
- **事件系统** — 模块间解耦通信，支持自定义事件参数
- **同步加载简化** — 适配 Godot 的资源加载模型，简化了 UGF 的异步管道
- **2D/3D 兼容** — 实体和 UI 同时支持 2D（Node2D/Control）和 3D（Node3D）场景
- **完整示例游戏** — 内置 "Click The Blocks" 示例，演示所有框架功能

---

## 模块说明

| 模块 | 说明 |
|------|------|
| **事件 (Event)** | 游戏逻辑监听、抛出事件的机制，用于模块间解耦通信 |
| **有限状态机 (FSM)** | 创建、使用和销毁有限状态机的功能，适用于状态机模式的游戏逻辑 |
| **流程 (Procedure)** | 贯穿游戏运行时整个生命周期的有限状态机，将不同游戏状态解耦 |
| **实体 (Entity)** | 动态创建/销毁场景中的物体，支持实体组管理、父子挂载、对象池复用 |
| **界面 (UI)** | 管理 UI 窗体和界面组，支持显示/隐藏、暂停/覆盖、深度排序、对象池复用 |
| **声音 (Sound)** | 管理声音和声音组，支持优先级抢占、音量/速率/循环控制、2D/3D 音频 |
| **资源 (Resource)** | 加载 Godot 资源（PackedScene、Texture2D、AudioStream 等），支持同步/异步加载 |
| **配置 (Config)** | 加载和管理全局只读的键值对配置 |
| **数据表 (DataTable)** | 以表格形式（Tab 分隔的文本文件）加载和管理游戏数据 |
| **本地化 (Localization)** | 多语言支持，文本本地化字典，集成 Godot TranslationServer |
| **设置 (Setting)** | 以键值对形式持久化存储玩家数据，基于 Godot ConfigFile |
| **对象池 (Object Pool)** | 对象缓存池，避免频繁创建和销毁，支持单生成/多生成模式 |
| **数据节点 (Data Node)** | 以树状结构保存和管理任意类型的运行时数据 |
| **变量 (Variable)** | 类型安全的变量包装（VarBoolean、VarInt32、VarSingle、VarString） |

> **注意：** UGF 中的 Debugger、Download、FileSystem、Network、WebRequest 模块在 GGF 中暂未实现适配层（核心层代码保留）。这些模块将在后续版本中视进度逐步补充适配。

---

## 快速开始

### 环境要求

- Godot 4.6 或更高版本
- .NET 8.0 SDK
- C# 开发环境（推荐 VS Code + C# Dev Kit 或 Visual Studio / Rider）

### 安装步骤

1. **克隆项目**

   ```bash
   git clone https://github.com/Hacksco/GodotGameFramework.git
   cd GodotGameFramework
   ```

2. **打开项目**

   用 Godot 编辑器打开 `project.godot`

3. **配置 Autoload**

   在 Godot 编辑器中，进入 **项目 → 项目设置 → Autoload**，添加以下自动加载节点：

   | 名称 | 路径 |
   |------|------|
   | `GGFEntry` | `res://Framework/GodotGameFramework/Base/GGFEntry.cs` |

   GGFEntry 会自动注册所有子节点中的 `GGFComponent` 组件。

4. **配置组件场景树**

   将框架组件作为 GGFEntry 的子节点添加到场景中，典型的场景树结构：

   ```
   GGFEntry (Autoload)
     ├── GGFBaseComponent
     ├── EventComponent
     ├── FsmComponent
     ├── ProcedureComponent
     ├── ObjectPoolComponent
     ├── DataNodeComponent
     ├── ConfigComponent
     ├── DataTableComponent
     ├── LocalizationComponent
     ├── SettingComponent
     ├── ResourceComponent
     ├── EntityComponent
     ├── SoundComponent
     └── UIComponent
   ```

5. **运行示例游戏**

   打开 `AAAGame/Scene/` 下的场景即可运行 "Click The Blocks" 示例。

---

## 使用说明

### 核心理念

GGF 将游戏中的可见元素分为两类：**实体 (Entity)** 和 **界面 (UI)**，通过流程 (Procedure) 有限状态机管理游戏逻辑。

- **Entity** — 场景中动态创建的一切物体（角色、敌人、特效、道具等）
- **UI** — 所有界面窗口（主菜单、HUD、弹窗、设置面板等）
- **Procedure** — 游戏流程的状态机（启动、菜单、游戏、结算等）

### 全局门面 GF

`GF` 是一个纯静态类，提供所有框架组件的便捷访问：

```csharp
// 实体管理
GF.Entity.ShowEntity<EnemyLogic>(1, "res://Scenes/Enemy.tscn", "Enemy");
GF.Entity.HideEntity(1);
GF.Entity.AttachEntity(weaponId, playerId);

// UI 管理
GF.UI.OpenUIForm<MainMenuForm>("res://UI/MainMenu.tscn", "Normal");
GF.UI.CloseUIForm(serialId);
GF.UI.GetUIForm<GameHUDForm>(serialId);

// 音频
GF.Sound.PlayBGM("res://Audio/bgm.mp3");
GF.Sound.PlaySFX("res://Audio/click.wav");
GF.Sound.StopBGM(1.0f);  // 淡出停止

// 事件
GF.Event.Fire(this, new ScoreChangedEventArgs { Score = 100 });
GF.Event.Subscribe<ScoreChangedEventArgs>(OnScoreChanged);
GF.Event.Unsubscribe<ScoreChangedEventArgs>(OnScoreChanged);

// 数据表
GF.DataTable.CreateDataTable<TestItemData>("TestItemData");
GF.DataTable.GetDataTable<TestItemData>();

// 配置
GF.Config.GetBool("IsDebug");
GF.Config.GetInt("MaxBlocks");

// 本地化
GF.Localization.GetString("GameTitle");

// 设置（持久化）
GF.Setting.SetBool("MusicMuted", true);
GF.Setting.Save();

// 对象池
GF.ObjectPool.CreateSingleSpawnObjectPool<BulletPoolObject>("Bullet", 100);

// 数据节点
GF.DataNode.SetData("Game/Score", new VarInt32(100));
GF.DataNode.GetData<VarInt32>("Game/Score");

// 基础功能
GF.Base.PauseGame();
GF.Base.ResumeGame();
GF.Base.GameSpeed = 2.0f;
```

### Procedure（流程）系统

Procedure 是贯穿游戏运行时的有限状态机，通过状态切换管理游戏流程：

```csharp
public class MenuProcedure : ProcedureBase
{
    protected override void OnEnter(IFsm<IProcedureManager> procedureFsm)
    {
        base.OnEnter(procedureFsm);
        // 进入流程：显示主菜单 UI
        GF.UI.OpenUIForm<MainMenuForm>("res://UI/MainMenu.tscn", "Normal");
    }

    protected override void OnUpdate(IFsm<IProcedureManager> procedureFsm, float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(procedureFsm, elapseSeconds, realElapseSeconds);
        // 每帧更新
    }

    protected override void OnLeave(IFsm<IProcedureManager> procedureFsm, bool isShutdown)
    {
        base.OnLeave(procedureFsm, isShutdown);
        // 离开流程：关闭主菜单 UI
        GF.UI.CloseUIForms("Normal");
    }

    // 切换到其他流程
    private void OnStartGame()
    {
        ChangeState<GameProcedure>(procedureFsm);
    }
}
```

典型的游戏流程：

```
LaunchProcedure → MenuProcedure → GameProcedure → GameOverProcedure → MenuProcedure
    (启动)           (主菜单)       (游戏中)        (结算)            (返回主菜单)
```

### Entity（实体）系统

实体是场景中动态创建的一切物体，支持对象池自动复用：

```csharp
// 1. 定义实体逻辑
public partial class EnemyLogic : EntityLogic
{
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        // 初始化
    }

    protected override void OnShow(object userData)
    {
        base.OnShow(userData);
        // 显示（从对象池取出或新建时调用）
    }

    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        // 每帧更新
    }

    protected override void OnHide(bool isShutdown, object userData)
    {
        base.OnHide(isShutdown, userData);
        // 隐藏（回收到对象池时调用）
    }
}

// 2. 创建实体
GF.Entity.ShowEntity<EnemyLogic>(entityId, "res://Scenes/Enemy.tscn", "Enemy");

// 3. 隐藏实体（自动回收到对象池）
GF.Entity.HideEntity(entityId);
```

### UI（界面）系统

UI 系统管理界面窗体的完整生命周期：

```csharp
// 1. 定义界面逻辑
public partial class MainMenuForm : UIFormLogic
{
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        // 初始化 UI 控件引用
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        // 界面打开时
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);
        // 界面关闭时
    }

    protected override void OnRecycle()
    {
        base.OnRecycle();
        // 回收到对象池时
    }
}

// 2. 打开界面
GF.UI.OpenUIForm<MainMenuForm>("res://UI/MainMenu.tscn", "Normal");

// 3. 关闭界面（自动回收到对象池）
GF.UI.CloseUIForm(serialId);

// 4. 获取界面实例
MainMenuForm form = GF.UI.GetUIForm<MainMenuForm>(serialId);
```

### 事件系统

通过事件实现模块间解耦：

```csharp
// 1. 定义事件参数
public class ScoreChangedEventArgs : GameEventArgs
{
    public int Score { get; set; }

    public override int Id => 1001;  // 事件唯一 ID
    public override void Clear() { Score = 0; }
}

// 2. 订阅事件
GF.Event.Subscribe<ScoreChangedEventArgs>(OnScoreChanged);

// 3. 触发事件
GF.Event.Fire(this, new ScoreChangedEventArgs { Score = 100 });

// 4. 事件处理
private void OnScoreChanged(object sender, ScoreChangedEventArgs e)
{
    GD.Print($"Score changed: {e.Score}");
}

// 5. 取消订阅
GF.Event.Unsubscribe<ScoreChangedEventArgs>(OnScoreChanged);
```

---

## 项目结构

```
GGF/
├── Framework/
│   ├── GameFramework/              # 核心框架层（从 UGF 直接复用，331 个 .cs 文件）
│   │   ├── Base/                   #   入口、模块基类、引用池、事件池、任务池、变量
│   │   ├── Event/                  #   事件管理器
│   │   ├── Fsm/                    #   有限状态机管理器
│   │   ├── Procedure/              #   流程管理器
│   │   ├── ObjectPool/             #   对象池管理器
│   │   ├── DataNode/               #   数据节点管理器
│   │   ├── Entity/                 #   实体管理器
│   │   ├── UI/                     #   UI 管理器
│   │   ├── Sound/                  #   声音管理器
│   │   ├── Resource/               #   资源管理器
│   │   ├── Config/                 #   配置管理器
│   │   ├── DataTable/              #   数据表管理器
│   │   ├── Localization/           #   本地化管理器
│   │   ├── Setting/                #   设置管理器
│   │   ├── Scene/                  #   场景管理器
│   │   ├── Utility/                #   工具类
│   │   └── ...                     #   更多模块（Debugger/Download/FileSystem/Network/WebRequest）
│   │
│   └── GodotGameFramework/         # Godot 引擎适配层（~61 个新文件）
│       ├── Base/                   #   GGFEntry（入口）、GGFComponent（组件基类）、GF（静态门面）、Log
│       ├── Event/                  #   事件组件
│       ├── Fsm/                    #   状态机组件
│       ├── Procedure/              #   流程组件
│       ├── ObjectPool/             #   对象池组件
│       ├── DataNode/               #   数据节点组件
│       ├── Entity/                 #   实体组件 + EntityLogic + 扩展方法
│       ├── UI/                     #   UI 组件 + UIFormLogic + UIItemBase + 扩展方法
│       ├── Sound/                  #   音频组件 + 扩展方法
│       ├── Resource/               #   资源组件（双模式：直接加载 + 资源管线）
│       ├── Config/                 #   配置组件
│       ├── DataTable/              #   数据表组件
│       ├── Localization/           #   本地化组件
│       ├── Setting/                #   设置组件
│       ├── Variable/               #   变量类型（VarBoolean/Int32/Single/String）
│       └── Utility/                #   工具 Helper
│
├── AAAGame/                        # 示例游戏 "Click The Blocks"
│   ├── Procedure/                  #   流程：启动、菜单、游戏
│   ├── Entity/                     #   实体：方块、得分方块、红色方块
│   ├── UI/                         #   UI：主菜单、HUD、结算、暂停、得分浮字
│   ├── Event/                      #   自定义事件
│   ├── DataTable/                  #   数据表定义
│   ├── ObjectPool/                 #   对象池示例
│   ├── Audio/                      #   音频资源
│   └── Scene/                      #   场景文件
│
├── Data/                           # 数据文件
│   ├── TestConfig.txt              #   配置文件（Tab 分隔键值对）
│   ├── TestItemData.txt            #   数据表
│   ├── GameConfig.txt              #   游戏配置
│   ├── BlockTypeData.txt           #   方块类型数据
│   └── Localization/               #   多语言字典
│       ├── ChineseSimplified.txt
│       └── English.txt
│
├── Docs/                           # 文档
│   └── UGF_Analysis/               #   UGF 架构分析和移植文档
│
├── project.godot                    # Godot 项目配置
├── GGF.csproj                       # C# 项目配置（.NET 8.0, Godot 4.6）
└── icon.svg                         # 项目图标
```

---

## 与 UGF 的对应关系

| Unity (UGF) | Godot (GGF) |
|-------------|-------------|
| `MonoBehaviour` | `Node` |
| `GameObject` | `Node` |
| `Transform` | `Node3D.Transform` / `Node2D` |
| `AudioSource` | `AudioStreamPlayer` |
| `Canvas` / `CanvasGroup` | `CanvasLayer` / `Control` |
| `AssetBundle` | `PackedScene` / `PCK` |
| `Resources.Load()` | `ResourceLoader.Load<T>()` |
| `PlayerPrefs` | `ConfigFile` |
| `Time.deltaTime` | `_Process(double delta)` |
| `Application.runInBackground` | Godot 默认支持 |
| `DontDestroyOnLoad` | Autoload |
| `Instantiate()` | `Node.Instantiate()` |
| `Destroy()` | `Node.QueueFree()` |
| `GameEntry` | `GGFEntry` (Autoload) |
| `GameFrameworkComponent` | `GGFComponent` |
| `GFBuiltin` / Extension | `GF` (静态门面) |

---

## 致谢

- **[Unity Game Framework (UGF)](https://github.com/EllanJiang/GameFramework)** — 核心框架层代码的原始作者：[Jiang Yin](https://gameframework.cn/)
- **[Godot Engine](https://godotengine.org/)** — 开源游戏引擎
- **[GF_X](https://github.com/sunsvip/GF_X)** — 参考实现，提供了 UGF 的扩展使用模式

---

## 许可证

本项目采用 [MIT License](LICENSE) 开源。

核心框架层 (`Framework/GameFramework/`) 的版权归属于 UGF 原始作者 Jiang Yin。
