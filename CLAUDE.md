# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

HeistNSeek is a Unity 6 (6000.2.10f1) multiplayer game project using VContainer for dependency injection. The architecture emphasizes memory-safety through DI over singletons, thin MonoBehaviours with logic in plain C# services, and testability through constructor injection.

## Core Architecture

### Dependency Injection with VContainer

The project uses VContainer (v1.17.0) with a two-scope architecture:

1. **BootstrapLifetimeScope** (`Assets/Scripts/Core/InjectionBase/BootstrapLifetimeScope.cs`)
   - Root scope that persists across scenes (DontDestroyOnLoad)
   - Registers global singleton services (e.g., `IMessageHub`, `IGlobalTestService`, `IMonoGlobalService`)
   - Registers `BootstrapEntryPoint` for initialization logic
   - Uses `[SceneDropdown]` attribute for editor scene selection
   - Located in Bootstrap scene

2. **GameplayLifetimeScope** (`Assets/Scripts/Core/InjectionBase/GameplayLifetimeScope.cs`)
   - Scene-specific scope for gameplay services
   - Registers scene-bound services (e.g., `IPlainService`)
   - Registers `GameplayEntryPoint` for scene initialization
   - Registers MonoBehaviour components in hierarchy
   - Located in Main scene

**Important DI Conventions:**
- Plain C# classes: Use constructor injection with `Init` method
- MonoBehaviours: Use `[Inject]` attribute on fields/properties or `Init` method with `[Inject]` attribute
- Prefer interfaces at DI boundaries
- Register global services as `Singleton` in BootstrapLifetimeScope
- Register scene services as `Singleton` or `Scoped` in GameplayLifetimeScope
- Avoid manual `new` for managed dependencies - let VContainer control lifetimes

**VContainer Documentation:** https://vcontainer.hadashikick.jp/scoping/project-root-lifetimescope

### Scene Architecture & EntryPoints

The project uses VContainer's EntryPoint pattern for scene initialization:

- **Bootstrap.unity**: Entry point scene containing BootstrapLifetimeScope
  - **BootstrapEntryPoint** (`Assets/Scripts/Core/LevelInitializers/BootstrapEntryPoint.cs`): Handles Bootstrap initialization and scene loading

- **Main.unity**: Gameplay scene containing GameplayLifetimeScope
  - **GameplayEntryPoint** (`Assets/Scripts/Core/LevelInitializers/GameplayEntryPoint.cs`): Handles Gameplay initialization

- **Build flow**: Bootstrap → BootstrapEntryPoint loads Main scene additively → GameplayEntryPoint initializes gameplay

**EntryPoint Pattern:**
EntryPoints implement `IStartable` and run after all dependencies are resolved. Use them for:
- Scene initialization logic
- Loading other scenes
- Starting game systems
- Any logic that needs all dependencies ready

Example:
```csharp
public class MySceneEntryPoint : IStartable
{
    private readonly IMyService _myService;

    public MySceneEntryPoint(IMyService myService)
    {
        _myService = myService;
    }

    public void Start()
    {
        // Initialization logic here
        _myService.Initialize();
    }
}
```

### Service Patterns

**Plain Service Pattern** (preferred for logic):
```csharp
namespace HeistNSeek.Core
{
    public interface IMyService
    {
        void DoSomething();
    }

    public class MyService : IMyService
    {
        public void DoSomething() { }
    }
}
```

**MonoBehaviour Service Pattern** (when Unity lifecycle needed):
```csharp
namespace HeistNSeek.Core
{
    public class MyMonoService : MonoBehaviour
    {
        [SerializeField] private string config;

        public void DoSomething() { }
    }
}
```

## Project Structure

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── InjectionBase/      # VContainer LifetimeScopes
│   │   ├── LevelInitializers/  # Scene EntryPoints (initialization logic)
│   │   ├── ExampleUsage/       # Example code for testing
│   │   └── DI/                 # Other DI-related utilities
│   ├── Helpers/                # Utility classes (DO NOT modify unless specified)
│   └── Editor/                 # Unity Editor scripts (PropertyDrawers, etc.)
├── Scenes/
│   ├── Bootstrap.unity         # Entry scene with root DI scope
│   └── Main.unity              # Gameplay scene
├── Prefabs/                    # Prefab assets
├── Settings/                   # Project configuration
└── Multiplayer Widgets/        # Unity multiplayer UI components
```

**Key Folders:**
- `Core/InjectionBase/`: Contains LifetimeScope configurations for each scene
- `Core/LevelInitializers/`: Contains EntryPoint classes that run scene initialization logic
- `Helpers/`: Reusable utilities (CoroutineHelper, SceneLoader, Extensions, etc.)

## Key Dependencies

**Unity Packages:**
- VContainer 1.17.0 (DI framework)
- Unity Input System 1.14.2
- Unity Multiplayer packages (Center, Widgets, Playmode, Services)
- Unity Netcode
- Vivox (voice chat)
- Universal Render Pipeline 17.2.0
- Newtonsoft.Json 3.2.2

**NuGet Packages:**
- Easy.MessageHub 5.1.0 (registered globally as `IMessageHub` singleton)

## Global Services

### MessageHub (Easy.MessageHub)
Registered globally in `BootstrapLifetimeScope` as a singleton. Available for injection anywhere in the project.

**Usage:**
```csharp
// In a service (constructor injection)
public class MyService
{
    private readonly IMessageHub _messageHub;

    public MyService(IMessageHub messageHub)
    {
        _messageHub = messageHub;
        _messageHub.Subscribe<MyMessage>(OnMyMessage);
    }

    private void OnMyMessage(MyMessage msg) { /* handle */ }
}

// In a MonoBehaviour (field injection)
public class MyBehaviour : MonoBehaviour
{
    [Inject] private IMessageHub _messageHub;

    [Inject]
    public void Init(IMessageHub messageHub)
    {
        _messageHub = messageHub;
        _messageHub.Subscribe<GameEvent>(OnGameEvent);
    }
}
```

## Helpers (DO NOT MODIFY UNLESS SPECIFIED OTHERWISE)

Located in `Assets/Scripts/Helpers/`:

### SceneLoader
Static utility for async scene loading with callbacks:
```csharp
SceneLoader.Load(sceneIndex, LoadSceneMode.Additive, onLoaded: () =>
{
    Debug.Log("Scene loaded!");
});

// Or by name
SceneLoader.Load("MainScene", LoadSceneMode.Additive);
```
- Uses CoroutineHelper internally
- Sets loaded scene as active automatically
- Supports callbacks via `onLoaded` parameter

### CoroutineHelper
Run coroutines without an existing MonoBehaviour runner:
```csharp
CoroutineHelper.RunCoroutine(MyCoroutine());
```
- Creates temporary GameObject with CoroutineRunner
- Destroys on completion by default
- Avoid in high-frequency paths (creates objects)
- For long-running coroutines, use `destroyOnComplete: false`

### FpsCounter
Attach to scene object to display FPS metrics. Configure via inspector.

### Extensions & EnumerableExtensions
- **Extensions**: String helpers, int/bool conversion, range checks, normalization, logarithmic dB mapping
- **EnumerableExtensions**: Collection utilities, safe add-if-missing, null checks, shuffle, random selection

## Development Guidelines

### Memory and Lifetime Management
- Prefer service registration as `Singleton` in appropriate scope
- Do not cache references longer than their scope
- Avoid manual `new` for managed dependencies
- For Unity components, use standard instantiation/pooling
- Be cautious with `CoroutineHelper` in high-frequency code

### Adding New Features

**1. Add a new service:**
- Create `IMyService` interface and `MyService` class in `Assets/Scripts/Core/`
- Register in appropriate LifetimeScope as singleton
- Inject via constructor (plain C#) or `[Inject]` (MonoBehaviour)

**2. Add a new scene model/system:**
- Create plain C# class (avoid MonoBehaviour if possible)
- Register in GameplayLifetimeScope as `Lifetime.Scoped`
- Start via EntryPoint or existing system

**3. Add a new scene with initialization logic:**
- Create a new scene in `Assets/Scenes/`
- Add the scene to Build Settings (File → Build Settings)
- Create a LifetimeScope script in `Assets/Scripts/Core/InjectionBase/` (e.g., `MySceneLifetimeScope`)
- Create an EntryPoint in `Assets/Scripts/Core/LevelInitializers/` (e.g., `MySceneEntryPoint`)
- Register the EntryPoint in the LifetimeScope: `builder.RegisterEntryPoint<MySceneEntryPoint>(Lifetime.Singleton);`
- Add the LifetimeScope component to a GameObject in your scene

**4. Inject into MonoBehaviour:**
- Create `Init` method and Add `[Inject]` attribute for injection. Add parameters all the stuff that need to be injected.
- Ensure object is under a LifetimeScope in scene hierarchy

**5. Use SceneDropdown for scene selection in editor:**
- For int fields that store scene build indices, add `[SceneDropdown]` attribute
- This will display a dropdown in the Inspector with all scenes from Build Settings
- Example: `[SceneDropdown] [SerializeField] private int targetSceneIndex;`

### Code Conventions
- Namespace: `HeistNSeek.Core`, `HeistNSeek.Helpers`, etc.
- Keep MonoBehaviours minimal - push logic to services/models
- Prefer interfaces for DI boundaries
- Use constructor injection for testability
- Avoid unnecessary changes that can affect stability

## Build and Development

**Unity Version:** 6000.2.10f1

**Opening the project:**
- Open in Unity Hub with Unity 6000.2.10f1
- Bootstrap scene is the entry point

**Testing:**
- Use Unity Test Framework (Test Runner window)
- Navigate to Window → General → Test Runner

**Building:**
- Ensure Bootstrap scene is first in Build Settings
- File → Build Settings → Build

**NuGet package management:**
- Configure via `Assets/NuGet.config` and `Assets/packages.config`
- Packages stored in `Assets/Packages/`

## Important Notes

- **Always test from Bootstrap scene**: The project uses Bootstrap → Main scene flow with additive loading
- **EntryPoint pattern**: Scene initialization logic should be in EntryPoint classes (in `LevelInitializers/`), not in MonoBehaviour Start/Awake methods
- **Services are scoped**: Global services in BootstrapLifetimeScope, scene-specific in GameplayLifetimeScope
- **MessageHub is global**: `IMessageHub` is registered in Bootstrap scope and available everywhere
- **Scripts location**: All scripts must be created under `Assets/Scripts/` folder (see ModelInstructions.md)
- **Scene management**: Use `SceneLoader` utility for async scene loading with proper scene activation
- **Multiplayer functionality**: Integrated via Unity's multiplayer packages (Netcode, Widgets, Services)
- **Editor utilities**: Use `[SceneDropdown]` attribute for scene index fields to get inspector dropdowns
