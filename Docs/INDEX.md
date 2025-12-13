# HeistNSeek Documentation Index

## Quick Links

| Document | Purpose | Lines | Read Time |
|----------|---------|-------|-----------|
| **[README.md](README.md)** | Start here - Navigation & overview | 248 | 5 min |
| **[00_PROJECT_STRUCTURE.md](00_PROJECT_STRUCTURE.md)** | Folder organization & file structure | 219 | 10 min |
| **[01_ARCHITECTURE_OVERVIEW.md](01_ARCHITECTURE_OVERVIEW.md)** | Core patterns & architecture | 455 | 15 min |
| **[02_KEY_SYSTEMS.md](02_KEY_SYSTEMS.md)** | System details & interactions | 624 | 20 min |
| **[03_DEVELOPER_GUIDE.md](03_DEVELOPER_GUIDE.md)** | Practical tasks & code examples | 787 | 25 min |
| **[04_FILE_REFERENCE.md](04_FILE_REFERENCE.md)** | File-by-file reference | 457 | 15 min |
| **[DOCUMENTATION_SUMMARY.md](DOCUMENTATION_SUMMARY.md)** | What was documented | 412 | 10 min |

**Total Documentation:** 7 files, 3,202 lines, ~100 KB

---

## By Role

### 🚀 First-Time Developer
1. Read **[README.md](README.md)** (5 min)
2. Read **[00_PROJECT_STRUCTURE.md](00_PROJECT_STRUCTURE.md)** (10 min)
3. Read **[01_ARCHITECTURE_OVERVIEW.md](01_ARCHITECTURE_OVERVIEW.md)** (15 min)
4. Skim **[03_DEVELOPER_GUIDE.md](03_DEVELOPER_GUIDE.md)** patterns (10 min)

**Total Onboarding:** 40 minutes

### 👨‍💼 Implementing Features
1. **[03_DEVELOPER_GUIDE.md](03_DEVELOPER_GUIDE.md)** - Common tasks
2. **[02_KEY_SYSTEMS.md](02_KEY_SYSTEMS.md)** - System details
3. **[04_FILE_REFERENCE.md](04_FILE_REFERENCE.md)** - Find files

### 🔧 Debugging Issues
1. **[02_KEY_SYSTEMS.md](02_KEY_SYSTEMS.md)** - System interactions
2. **[03_DEVELOPER_GUIDE.md](03_DEVELOPER_GUIDE.md)** - Common mistakes
3. **[04_FILE_REFERENCE.md](04_FILE_REFERENCE.md)** - Locate code

### 📚 Learning Architecture
1. **[01_ARCHITECTURE_OVERVIEW.md](01_ARCHITECTURE_OVERVIEW.md)** - Patterns
2. **[02_KEY_SYSTEMS.md](02_KEY_SYSTEMS.md)** - System details
3. **[00_PROJECT_STRUCTURE.md](00_PROJECT_STRUCTURE.md)** - Organization

### 🔍 Finding Specific Information
- **Where's the file?** → **[04_FILE_REFERENCE.md](04_FILE_REFERENCE.md)**
- **How do I...?** → **[03_DEVELOPER_GUIDE.md](03_DEVELOPER_GUIDE.md)**
- **How does X work?** → **[02_KEY_SYSTEMS.md](02_KEY_SYSTEMS.md)**
- **Why is it like this?** → **[01_ARCHITECTURE_OVERVIEW.md](01_ARCHITECTURE_OVERVIEW.md)**
- **Where should my code go?** → **[00_PROJECT_STRUCTURE.md](00_PROJECT_STRUCTURE.md)**

---

## By Topic

### Architecture & Design
- VContainer DI → **[01_ARCHITECTURE_OVERVIEW.md](01_ARCHITECTURE_OVERVIEW.md#1-dependency-injection-vcontainer)**
- State Machine → **[01_ARCHITECTURE_OVERVIEW.md](01_ARCHITECTURE_OVERVIEW.md#2-state-machine)**
- MessageHub Events → **[01_ARCHITECTURE_OVERVIEW.md](01_ARCHITECTURE_OVERVIEW.md#3-event-driven-communication-messagehub)**
- Scene Loading → **[01_ARCHITECTURE_OVERVIEW.md](01_ARCHITECTURE_OVERVIEW.md#4-scene-loading--entrypoints)**
- Networking → **[01_ARCHITECTURE_OVERVIEW.md](01_ARCHITECTURE_OVERVIEW.md#5-networking-unity-netcode)**
- Design Patterns → **[01_ARCHITECTURE_OVERVIEW.md](01_ARCHITECTURE_OVERVIEW.md#common-patterns)**

### Systems & Features
- Player System → **[02_KEY_SYSTEMS.md](02_KEY_SYSTEMS.md#1-player-system)**
- Inventory System → **[02_KEY_SYSTEMS.md](02_KEY_SYSTEMS.md#2-inventory-system)**
- Network System → **[02_KEY_SYSTEMS.md](02_KEY_SYSTEMS.md#3-network-system)**
- Event System → **[02_KEY_SYSTEMS.md](02_KEY_SYSTEMS.md#4-event-system)**
- State Machine → **[02_KEY_SYSTEMS.md](02_KEY_SYSTEMS.md#5-game-state-machine)**

### Development Tasks
- Add Player Stat → **[03_DEVELOPER_GUIDE.md](03_DEVELOPER_GUIDE.md#task-1-add-a-new-player-statproperty)**
- Create Game State → **[03_DEVELOPER_GUIDE.md](03_DEVELOPER_GUIDE.md#task-2-create-a-new-game-state)**
- Create Service → **[03_DEVELOPER_GUIDE.md](03_DEVELOPER_GUIDE.md#task-3-create-a-new-service)**
- Handle Input → **[03_DEVELOPER_GUIDE.md](03_DEVELOPER_GUIDE.md#task-4-handle-player-input)**
- Add UI → **[03_DEVELOPER_GUIDE.md](03_DEVELOPER_GUIDE.md#task-5-add-ui-system)**
- Network Behavior → **[03_DEVELOPER_GUIDE.md](03_DEVELOPER_GUIDE.md#task-6-network-behavior-multiplayer)**
- Subscribe Events → **[03_DEVELOPER_GUIDE.md](03_DEVELOPER_GUIDE.md#task-7-subscribe-to-game-events)**
- Inventory Items → **[03_DEVELOPER_GUIDE.md](03_DEVELOPER_GUIDE.md#task-8-add-to-inventory-system)**

### File Organization
- Folder Structure → **[00_PROJECT_STRUCTURE.md](00_PROJECT_STRUCTURE.md#scripts-folder-structure)**
- File Locations → **[04_FILE_REFERENCE.md](04_FILE_REFERENCE.md#core-systems)**
- Naming Conventions → **[00_PROJECT_STRUCTURE.md](00_PROJECT_STRUCTURE.md#naming-conventions)**
- File Categories → **[00_PROJECT_STRUCTURE.md](00_PROJECT_STRUCTURE.md#key-file-categories)**

### Code Examples
- DI Registration → **[01_ARCHITECTURE_OVERVIEW.md](01_ARCHITECTURE_OVERVIEW.md#registration-pattern)**, **[03_DEVELOPER_GUIDE.md](03_DEVELOPER_GUIDE.md#pattern-3-dependency-injection)**
- Event Publishing → **[01_ARCHITECTURE_OVERVIEW.md](01_ARCHITECTURE_OVERVIEW.md#publishing-events)**, **[03_DEVELOPER_GUIDE.md](03_DEVELOPER_GUIDE.md#pattern-1-message-publishing)**
- Event Subscription → **[01_ARCHITECTURE_OVERVIEW.md](01_ARCHITECTURE_OVERVIEW.md#subscribing-to-events)**, **[03_DEVELOPER_GUIDE.md](03_DEVELOPER_GUIDE.md#pattern-2-event-subscription)**
- State Transitions → **[02_KEY_SYSTEMS.md](02_KEY_SYSTEMS.md#state-transitions)**, **[03_DEVELOPER_GUIDE.md](03_DEVELOPER_GUIDE.md#pattern-4-state-machine-transitions)**
- NetworkBehaviour → **[02_KEY_SYSTEMS.md](02_KEY_SYSTEMS.md#network-behavior-pattern)**, **[03_DEVELOPER_GUIDE.md](03_DEVELOPER_GUIDE.md#pattern-5-networkbehaviour)**

---

## Documentation Content Summary

### README.md
- Quick navigation guide
- Key concepts summary
- Project structure overview
- Common workflows
- DO/DON'T guidelines
- Getting help resources

### 00_PROJECT_STRUCTURE.md
- High-level organization
- Scripts folder structure (detailed)
- Core systems locations
- Naming conventions
- File guidelines
- Module dependencies
- Scene structure

### 01_ARCHITECTURE_OVERVIEW.md
- Dependency Injection (VContainer)
- State Machine pattern
- Event-Driven Communication
- Scene Loading & EntryPoints
- Networking integration
- Architectural decisions
- Common patterns

### 02_KEY_SYSTEMS.md
- Player System (movement, input, ragdoll, push)
- Inventory System (items, stacking, dropping)
- Network System (spawning, syncing)
- Event System (publishing, subscribing)
- State Machine (states, transitions)
- DI Scopes
- System interactions

### 03_DEVELOPER_GUIDE.md
- 8 Common tasks with complete examples
- 5 Essential patterns
- Testing & debugging
- Code organization
- Performance tips
- Common mistakes
- Feature checklist

### 04_FILE_REFERENCE.md
- Complete file listing
- File purposes & methods
- Namespace hierarchy
- Asset organization
- File relationships
- Finding code guide

### DOCUMENTATION_SUMMARY.md
- What was documented
- File descriptions
- Statistics
- Coverage details
- How to use docs

---

## Key Sections by Document

| Section | Document |
|---------|----------|
| Where should code go? | [00_PROJECT_STRUCTURE.md](00_PROJECT_STRUCTURE.md) |
| How does DI work? | [01_ARCHITECTURE_OVERVIEW.md](01_ARCHITECTURE_OVERVIEW.md) |
| What's a State Machine? | [01_ARCHITECTURE_OVERVIEW.md](01_ARCHITECTURE_OVERVIEW.md) / [02_KEY_SYSTEMS.md](02_KEY_SYSTEMS.md) |
| How do events work? | [01_ARCHITECTURE_OVERVIEW.md](01_ARCHITECTURE_OVERVIEW.md) / [02_KEY_SYSTEMS.md](02_KEY_SYSTEMS.md) |
| What file does X? | [04_FILE_REFERENCE.md](04_FILE_REFERENCE.md) |
| How do I add Y? | [03_DEVELOPER_GUIDE.md](03_DEVELOPER_GUIDE.md) |
| Common patterns? | [01_ARCHITECTURE_OVERVIEW.md](01_ARCHITECTURE_OVERVIEW.md) / [03_DEVELOPER_GUIDE.md](03_DEVELOPER_GUIDE.md) |
| Best practices? | [03_DEVELOPER_GUIDE.md](03_DEVELOPER_GUIDE.md) |
| How do systems interact? | [02_KEY_SYSTEMS.md](02_KEY_SYSTEMS.md) |
| Debugging tips? | [03_DEVELOPER_GUIDE.md](03_DEVELOPER_GUIDE.md) |

---

## Reading Paths

### Path 1: Quick Understanding (30 min)
1. **[README.md](README.md)** - Overview (5 min)
2. **[00_PROJECT_STRUCTURE.md](00_PROJECT_STRUCTURE.md)** - Organization (10 min)
3. **[01_ARCHITECTURE_OVERVIEW.md](01_ARCHITECTURE_OVERVIEW.md)** - Key patterns (15 min)

### Path 2: Complete Onboarding (90 min)
1. **[README.md](README.md)** (5 min)
2. **[00_PROJECT_STRUCTURE.md](00_PROJECT_STRUCTURE.md)** (15 min)
3. **[01_ARCHITECTURE_OVERVIEW.md](01_ARCHITECTURE_OVERVIEW.md)** (20 min)
4. **[02_KEY_SYSTEMS.md](02_KEY_SYSTEMS.md)** (30 min)
5. **[03_DEVELOPER_GUIDE.md](03_DEVELOPER_GUIDE.md)** - Patterns section (20 min)

### Path 3: Feature Implementation (Variable)
1. **[03_DEVELOPER_GUIDE.md](03_DEVELOPER_GUIDE.md)** - Find similar task
2. **[02_KEY_SYSTEMS.md](02_KEY_SYSTEMS.md)** - Understand system
3. **[04_FILE_REFERENCE.md](04_FILE_REFERENCE.md)** - Find files
4. Code implementation

### Path 4: Debugging (Variable)
1. **[02_KEY_SYSTEMS.md](02_KEY_SYSTEMS.md)** - System details
2. **[03_DEVELOPER_GUIDE.md](03_DEVELOPER_GUIDE.md)** - Common mistakes
3. **[04_FILE_REFERENCE.md](04_FILE_REFERENCE.md)** - Find relevant files

---

## External Resources

### Official Documentation
- **VContainer:** https://vcontainer.hadashikick.jp/
- **Unity Netcode:** https://docs-multiplayer.unity3d.com/
- **Easy.MessageHub:** GitHub repository
- **FPS Engine (Cowsins):** Included in ThirdParty/

### Project Guidelines
- **CLAUDE.md** - In repo root
- Source code - Always the source of truth

---

## Document Attributes

| Attribute | Value |
|-----------|-------|
| Total Files | 7 |
| Total Lines | 3,202 |
| Total Size | ~100 KB |
| Code Examples | 40+ |
| Tables | 15+ |
| Diagrams | 10+ |
| Coverage | Comprehensive |
| Last Updated | 2025-12-13 |

---

## How to Maintain Documentation

When changes occur:
1. **New system added?** → Add to [02_KEY_SYSTEMS.md](02_KEY_SYSTEMS.md)
2. **New files created?** → Update [04_FILE_REFERENCE.md](04_FILE_REFERENCE.md)
3. **New patterns discovered?** → Add to [03_DEVELOPER_GUIDE.md](03_DEVELOPER_GUIDE.md)
4. **Organization changes?** → Update [00_PROJECT_STRUCTURE.md](00_PROJECT_STRUCTURE.md)
5. **Architecture changes?** → Update [01_ARCHITECTURE_OVERVIEW.md](01_ARCHITECTURE_OVERVIEW.md)

---

## Quick Reference

**Need help?**
1. **What/Where:** [04_FILE_REFERENCE.md](04_FILE_REFERENCE.md)
2. **How-to:** [03_DEVELOPER_GUIDE.md](03_DEVELOPER_GUIDE.md)
3. **Why:** [01_ARCHITECTURE_OVERVIEW.md](01_ARCHITECTURE_OVERVIEW.md)
4. **System interaction:** [02_KEY_SYSTEMS.md](02_KEY_SYSTEMS.md)
5. **Organization:** [00_PROJECT_STRUCTURE.md](00_PROJECT_STRUCTURE.md)
6. **All of above:** [README.md](README.md)

---

**Start with [README.md](README.md) if you're new here! 👋**
