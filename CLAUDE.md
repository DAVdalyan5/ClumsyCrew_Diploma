# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

HeistNSeek is a Unity 6 (6000.2.10f1) multiplayer game project using VContainer for dependency injection. The architecture emphasizes memory-safety through DI over singletons, thin MonoBehaviours with logic in plain C# services, and testability through constructor injection.

## Core Architecture

### Dependency Injection with VContainer

The project uses VContainer (v1.17.0) with a two-scope architecture:

1. **BootstrapLifetimeScope** (`Assets/Scripts/DI/BootstrapLifetimeScope.cs`)
   - Root scope that persists across scenes (DontDestroyOnLoad)
   - Registers global singleton services (e.g., `IGlobalTestService`, `IMonoGlobalService`)
   - Automatically loads the Main scene on startup
   - Located in Bootstrap scene

2. **GameplayLifetimeScope** (`Assets/Scripts/DI/GameplayLifetimeScope.cs`)
   - Scene-specific scope for gameplay services
   - Registers scene-bound services (e.g., `IPlainService`)
   - Registers MonoBehaviour components in hierarchy
   - Located in Main scene

**Important DI Conventions:**
- Plain C# classes: Use constructor injection with `Init` method
- MonoBehaviours: Use `[Inject]` attribute on fields/properties
- Prefer interfaces at DI boundaries
- Register global services as `Singleton` in BootstrapLifetimeScope
- Register scene services as `Singleton` or `Scoped` in GameplayLifetimeScope
- Avoid manual `new` for managed dependencies - let VContainer control lifetimes

**VContainer Documentation:** https://vcontainer.hadashikick.jp/scoping/project-root-lifetimescope

### Scene Architecture

- **Bootstrap.unity**: Entry point scene containing BootstrapLifetimeScope
- **Main.unity**: Gameplay scene containing GameplayLifetimeScope
- Build flow: Bootstrap → auto-loads Main scene

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
│   ├── Core/              # Core service interfaces and implementations
│   ├── DI/                # VContainer LifetimeScopes
│   └── Helpers/           # Utility classes (DO NOT modify unless specified)
├── Scenes/
│   ├── Bootstrap.unity    # Entry scene with root DI scope
│   └── Main.unity         # Gameplay scene
├── Prefabs/               # Prefab assets
├── Settings/              # Project configuration
└── Multiplayer Widgets/   # Unity multiplayer UI components
```

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
- Easy.MessageHub 5.1.0

## Helpers (DO NOT MODIFY UNLESS SPECIFIED OTHERWISE)

Located in `Assets/Scripts/Helpers/`:

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

**3. Inject into MonoBehaviour:**
- Create `Init` method and Add `[Inject]` attribute for injection. Add parameters all the stuff that need to be injected. 
- Ensure object is under a LifetimeScope in scene hierarchy

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

- The project uses a Bootstrap → Main scene flow. Always test from Bootstrap scene
- Services are split between global (Bootstrap) and scene-specific (Gameplay) scopes
- Multiplayer functionality is integrated via Unity's multiplayer packages
- Follow the ModelInstruction.md file for detailed architectural guidance
