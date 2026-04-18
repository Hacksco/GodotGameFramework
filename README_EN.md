# GGF (Godot Game Framework)

English | [中文](README.md)

> A game framework for Godot 4.x C#, ported from the [Unity Game Framework (UGF)](https://github.com/EllanJiang/GameFramework) architecture.

---

## Introduction

GGF (Godot Game Framework) is a game framework that ports the architecture and design philosophy of [Unity Game Framework](https://gameframework.cn/) to the Godot 4.x engine (C#).

GGF follows the exact same **three-layer separation architecture** as UGF:

```
┌─────────────────────────────────────────────────┐
│            Game Code (AAAGame)                  │  ← Your game logic
├─────────────────────────────────────────────────┤
│       Godot Wrapper Layer (GodotGameFramework)  │  ← Godot API calls
├─────────────────────────────────────────────────┤
│           Core Framework (GameFramework)        │  ← Engine-agnostic
└─────────────────────────────────────────────────┘
```

- **Core Framework Layer** (`Framework/GameFramework/`) — Pure C# code directly reused from UGF, with zero engine dependencies, containing 20+ manager modules, reference pools, event pools, task pools, and more
- **Godot Wrapper Layer** (`Framework/GodotGameFramework/`) — Engine adapter layer rewritten with Godot APIs, mapping Unity MonoBehaviour to Godot Node, and Unity APIs to Godot equivalents
- **Game Code Layer** (`AAAGame/`) — Game logic written using the framework API, fully decoupled from the engine

---

## Features

- **14 Built-in Modules** — Covering common game development needs: Event, FSM, Procedure, Entity, UI, Sound, Resource, Config, DataTable, Localization, Setting, Object Pool, Data Node, Variable
- **Three-Layer Architecture** — Engine-agnostic core, replaceable adapter layer, interface-driven business logic
- **Global Static Facade `GF`** — Access all framework components from one class: `GF.Entity.ShowEntity<T>()`, `GF.UI.OpenUIForm<T>()`, `GF.Sound.PlayBGM()`
- **Procedure-Driven** — Manage game lifecycle through Procedure (FSM), keeping logic structured and maintainable
- **Object Pool System** — Automatic recycling of entities and UI forms, reducing GC pressure and improving performance
- **Reference Pool System** — Built-in `ReferencePool` with `IReference` interface for zero-GC object allocation
- **Event System** — Decoupled inter-module communication with custom event arguments
- **Simplified Loading** — Adapted to Godot's resource loading model, simplifying UGF's async pipeline
- **2D/3D Compatible** — Entities and UI support both 2D (Node2D/Control) and 3D (Node3D) scenes
- **Complete Demo Game** — Built-in "Click The Blocks" demo demonstrating all framework features

---

## Module Descriptions

| Module | Description |
|--------|-------------|
| **Event** | Mechanism for firing and observing events, decoupling game logic modules |
| **FSM** | Create, use and destroy finite state machines for state-machine-based game logic |
| **Procedure** | FSM spanning the entire game lifecycle, decoupling different game states |
| **Entity** | Dynamically create/destroy scene objects with entity groups, parent-child attachment, and object pool recycling |
| **UI** | Manage UI forms and groups with show/hide, pause/cover, depth sorting, and object pool recycling |
| **Sound** | Manage sounds and sound groups with priority preemption, volume/speed/loop control, 2D/3D audio |
| **Resource** | Load Godot resources (PackedScene, Texture2D, AudioStream, etc.) with sync/async support |
| **Config** | Load and manage global read-only key-value configurations |
| **DataTable** | Load and manage game data from tab-separated text tables |
| **Localization** | Multi-language support with text dictionaries, integrated with Godot TranslationServer |
| **Setting** | Persist player data as key-value pairs using Godot ConfigFile |
| **Object Pool** | Object caching pool avoiding frequent creation/destruction, with SingleSpawn/MultiSpawn modes |
| **Data Node** | Manage arbitrary runtime data in tree structures |
| **Variable** | Type-safe variable wrappers (VarBoolean, VarInt32, VarSingle, VarString) |

> **Note:** Debugger, Download, FileSystem, Network, and WebRequest modules from UGF are not yet wrapped in GGF (core layer code is retained). These modules will be adapted in future releases as development progresses.

---

## Quick Start

### Requirements

- Godot 4.6 or later
- .NET 8.0 SDK
- C# IDE (VS Code + C# Dev Kit, Visual Studio, or Rider recommended)

### Installation

1. **Clone the project**

   ```bash
   git clone https://github.com/Hacksco/GodotGameFramework.git
   cd GodotGameFramework
   ```

2. **Open in Godot**

   Open `project.godot` with the Godot editor.

3. **Configure Autoload**

   Go to **Project → Project Settings → Autoload** and add:

   | Name | Path |
   |------|------|
   | `GGFEntry` | `res://Framework/GodotGameFramework/Base/GGFEntry.cs` |

   GGFEntry automatically registers all `GGFComponent` instances found in its child nodes.

4. **Set up component scene tree**

   Add framework components as children of GGFEntry:

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

5. **Run the demo**

   Open scenes under `AAAGame/Scene/` to run the "Click The Blocks" demo.

---

## Usage

### Core Concepts

GGF categorizes visible game elements into two types: **Entities** and **UI**, managed through Procedure (FSM) state machines.

- **Entity** — All dynamically created objects in the scene (characters, enemies, effects, items, etc.)
- **UI** — All interface windows (main menu, HUD, dialogs, settings panel, etc.)
- **Procedure** — State machine for game flow (launch, menu, gameplay, results, etc.)

### Global Facade GF

`GF` is a pure static class providing convenient access to all framework components:

```csharp
// Entity management
GF.Entity.ShowEntity<EnemyLogic>(1, "res://Scenes/Enemy.tscn", "Enemy");
GF.Entity.HideEntity(1);
GF.Entity.AttachEntity(weaponId, playerId);

// UI management
GF.UI.OpenUIForm<MainMenuForm>("res://UI/MainMenu.tscn", "Normal");
GF.UI.CloseUIForm(serialId);
GF.UI.GetUIForm<GameHUDForm>(serialId);

// Audio
GF.Sound.PlayBGM("res://Audio/bgm.mp3");
GF.Sound.PlaySFX("res://Audio/click.wav");
GF.Sound.StopBGM(1.0f);  // Fade out

// Events
GF.Event.Fire(this, new ScoreChangedEventArgs { Score = 100 });
GF.Event.Subscribe<ScoreChangedEventArgs>(OnScoreChanged);
GF.Event.Unsubscribe<ScoreChangedEventArgs>(OnScoreChanged);

// DataTable
GF.DataTable.CreateDataTable<TestItemData>("TestItemData");
GF.DataTable.GetDataTable<TestItemData>();

// Config
GF.Config.GetBool("IsDebug");
GF.Config.GetInt("MaxBlocks");

// Localization
GF.Localization.GetString("GameTitle");

// Settings (persistent)
GF.Setting.SetBool("MusicMuted", true);
GF.Setting.Save();

// Object Pool
GF.ObjectPool.CreateSingleSpawnObjectPool<BulletPoolObject>("Bullet", 100);

// Data Node
GF.DataNode.SetData("Game/Score", new VarInt32(100));
GF.DataNode.GetData<VarInt32>("Game/Score");

// Base utilities
GF.Base.PauseGame();
GF.Base.ResumeGame();
GF.Base.GameSpeed = 2.0f;
```

### Procedure System

Procedure is an FSM spanning the entire game runtime, managing game flow through state transitions:

```csharp
public class MenuProcedure : ProcedureBase
{
    protected override void OnEnter(IFsm<IProcedureManager> procedureFsm)
    {
        base.OnEnter(procedureFsm);
        // Enter: show main menu UI
        GF.UI.OpenUIForm<MainMenuForm>("res://UI/MainMenu.tscn", "Normal");
    }

    protected override void OnUpdate(IFsm<IProcedureManager> procedureFsm, float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(procedureFsm, elapseSeconds, realElapseSeconds);
        // Per-frame update
    }

    protected override void OnLeave(IFsm<IProcedureManager> procedureFsm, bool isShutdown)
    {
        base.OnLeave(procedureFsm, isShutdown);
        // Leave: close main menu UI
        GF.UI.CloseUIForms("Normal");
    }

    // Transition to another procedure
    private void OnStartGame()
    {
        ChangeState<GameProcedure>(procedureFsm);
    }
}
```

Typical game flow:

```
LaunchProcedure → MenuProcedure → GameProcedure → GameOverProcedure → MenuProcedure
     (Launch)        (Menu)        (Gameplay)       (Results)        (Back to Menu)
```

### Entity System

Entities are all dynamically created objects in the scene, with automatic object pool recycling:

```csharp
// 1. Define entity logic
public partial class EnemyLogic : EntityLogic
{
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        // Initialize
    }

    protected override void OnShow(object userData)
    {
        base.OnShow(userData);
        // Show (called when retrieved from pool or newly created)
    }

    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        // Per-frame update
    }

    protected override void OnHide(bool isShutdown, object userData)
    {
        base.OnHide(isShutdown, userData);
        // Hide (called when recycled to pool)
    }
}

// 2. Create entity
GF.Entity.ShowEntity<EnemyLogic>(entityId, "res://Scenes/Enemy.tscn", "Enemy");

// 3. Hide entity (automatically recycled to pool)
GF.Entity.HideEntity(entityId);
```

### UI System

The UI system manages the full lifecycle of UI forms:

```csharp
// 1. Define UI logic
public partial class MainMenuForm : UIFormLogic
{
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        // Initialize UI control references
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        // When UI opens
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);
        // When UI closes
    }

    protected override void OnRecycle()
    {
        base.OnRecycle();
        // When recycled to pool
    }
}

// 2. Open UI form
GF.UI.OpenUIForm<MainMenuForm>("res://UI/MainMenu.tscn", "Normal");

// 3. Close UI form (automatically recycled to pool)
GF.UI.CloseUIForm(serialId);

// 4. Get UI form instance
MainMenuForm form = GF.UI.GetUIForm<MainMenuForm>(serialId);
```

### Event System

Decouple modules through events:

```csharp
// 1. Define event arguments
public class ScoreChangedEventArgs : GameEventArgs
{
    public int Score { get; set; }

    public override int Id => 1001;  // Unique event ID
    public override void Clear() { Score = 0; }
}

// 2. Subscribe to event
GF.Event.Subscribe<ScoreChangedEventArgs>(OnScoreChanged);

// 3. Fire event
GF.Event.Fire(this, new ScoreChangedEventArgs { Score = 100 });

// 4. Handle event
private void OnScoreChanged(object sender, ScoreChangedEventArgs e)
{
    GD.Print($"Score changed: {e.Score}");
}

// 5. Unsubscribe
GF.Event.Unsubscribe<ScoreChangedEventArgs>(OnScoreChanged);
```

---

## Project Structure

```
GGF/
├── Framework/
│   ├── GameFramework/              # Core framework layer (reused from UGF, 331 .cs files)
│   │   ├── Base/                   #   Entry, module base classes, reference/event/task pools, variables
│   │   ├── Event/                  #   Event manager
│   │   ├── Fsm/                    #   Finite state machine manager
│   │   ├── Procedure/              #   Procedure manager
│   │   ├── ObjectPool/             #   Object pool manager
│   │   ├── DataNode/               #   Data node manager
│   │   ├── Entity/                 #   Entity manager
│   │   ├── UI/                     #   UI manager
│   │   ├── Sound/                  #   Sound manager
│   │   ├── Resource/               #   Resource manager
│   │   ├── Config/                 #   Config manager
│   │   ├── DataTable/              #   Data table manager
│   │   ├── Localization/           #   Localization manager
│   │   ├── Setting/                #   Setting manager
│   │   ├── Scene/                  #   Scene manager
│   │   ├── Utility/                #   Utility classes
│   │   └── ...                     #   More modules (Debugger/Download/FileSystem/Network/WebRequest)
│   │
│   └── GodotGameFramework/         # Godot engine adapter layer (~61 new files)
│       ├── Base/                   #   GGFEntry (entry), GGFComponent (base), GF (static facade), Log
│       ├── Event/                  #   Event component
│       ├── Fsm/                    #   FSM component
│       ├── Procedure/              #   Procedure component
│       ├── ObjectPool/             #   Object pool component
│       ├── DataNode/               #   Data node component
│       ├── Entity/                 #   Entity component + EntityLogic + extension methods
│       ├── UI/                     #   UI component + UIFormLogic + UIItemBase + extension methods
│       ├── Sound/                  #   Audio component + extension methods
│       ├── Resource/               #   Resource component (dual mode: direct load + resource pipeline)
│       ├── Config/                 #   Config component
│       ├── DataTable/              #   Data table component
│       ├── Localization/           #   Localization component
│       ├── Setting/                #   Setting component
│       ├── Variable/               #   Variable types (VarBoolean/Int32/Single/String)
│       └── Utility/                #   Utility helpers
│
├── AAAGame/                        # Demo game "Click The Blocks"
│   ├── Procedure/                  #   Procedures: launch, menu, gameplay
│   ├── Entity/                     #   Entities: block, score block, red block
│   ├── UI/                         #   UI: main menu, HUD, results, pause, score popup
│   ├── Event/                      #   Custom events
│   ├── DataTable/                  #   Data table definitions
│   ├── ObjectPool/                 #   Object pool example
│   ├── Audio/                      #   Audio resources
│   └── Scene/                      #   Scene files
│
├── Data/                           # Data files
│   ├── TestConfig.txt              #   Config file (tab-separated key-value pairs)
│   ├── TestItemData.txt            #   Data table
│   ├── GameConfig.txt              #   Game configuration
│   ├── BlockTypeData.txt           #   Block type data
│   └── Localization/               #   Localization dictionaries
│       ├── ChineseSimplified.txt
│       └── English.txt
│
├── Docs/                           # Documentation
│   └── UGF_Analysis/               #   UGF architecture analysis and porting docs
│
├── project.godot                    # Godot project configuration
├── GGF.csproj                       # C# project configuration (.NET 8.0, Godot 4.6)
└── icon.svg                         # Project icon
```

---

## UGF to GGF Mapping

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
| `Application.runInBackground` | Godot default behavior |
| `DontDestroyOnLoad` | Autoload |
| `Instantiate()` | `Node.Instantiate()` |
| `Destroy()` | `Node.QueueFree()` |
| `GameEntry` | `GGFEntry` (Autoload) |
| `GameFrameworkComponent` | `GGFComponent` |
| `GFBuiltin` / Extension | `GF` (static facade) |

---

## Acknowledgments

- **[Unity Game Framework (UGF)](https://github.com/EllanJiang/GameFramework)** — Original author of the core framework layer: [Jiang Yin](https://gameframework.cn/)
- **[Godot Engine](https://godotengine.org/)** — Open-source game engine
- **[GF_X](https://github.com/sunsvip/GF_X)** — Reference implementation providing extended UGF usage patterns

---

## License

This project is licensed under the [MIT License](LICENSE).

The core framework layer (`Framework/GameFramework/`) is copyrighted by the original UGF author Jiang Yin.
