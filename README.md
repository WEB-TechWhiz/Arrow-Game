# Arrow-Game

> Classic arcade archery game built with HTML5 Canvas and JavaScript, featuring physics-based projectile trajectories, moving targets, and scoring.

---

## 📋 Table of Contents
- [Overview](#-overview)
- [Key Features](#-key-features)
- [Tech Stack](#-tech-stack)
- [Project Structure](#-project-structure)
- [Getting Started](#-getting-started)
- [Contributing](#-contributing)
- [Author & License](#-author--license)

---

## 📌 Overview
**Arrow-Game** is designed to provide a comprehensive, maintainable, and scalable solution in the **Game Development / HTML5 Canvas** domain. Engineered with modern industry standards and clean architecture.

---

## ✨ Key Features
- **Trajectory Physics**: Realistic gravity, launch velocity, and angle calculations
- **Moving Targets & Obstacles**: Dynamic gameplay with increasing difficulty levels
- **Score Tracking**: High score persistence and visual hit animations

---

## 🛠️ Tech Stack
- **Clean Architecture**

---

## 📂 Project Structure
```text
Arrow-Game/
├── Assets/
│   ├── Scripts/
│   │   ├── Audio/
│   │   ├── Core/
│   │   ├── Data/
│   │   ├── Difficulty/
│   │   ├── FX/
│   │   ├── Maze/
│   │   ├── Mechanics/
│   │   ├── Meta/
│   │   ├── UI/
│   │   ├── Audio.meta
│   │   ├── Core.meta
│   │   ├── Data.meta
│   │   ├── Difficulty.meta
│   │   ├── FX.meta
│   │   ├── Maze.meta
│   │   ├── Mechanics.meta
│   │   ├── Meta.meta
│   │   └── UI.meta
│   └── Scripts.meta
├── Library/
│   ├── Artifacts/
│   │   ├── 00/
│   │   ├── 01/
│   │   ├── 02/
│   │   ├── 04/
│   │   ├── 05/
│   │   ├── 06/
│   │   ├── 07/
│   │   ├── 08/
│   │   ├── 09/
│   │   ├── 0a/
│   │   ├── 0b/
│   │   ├── 0c/
│   │   ├── 0d/
│   │   ├── 0e/
│   │   ├── 0f/
│   │   ├── 10/
│   │   ├── 11/
│   │   ├── 13/
│   │   ├── 15/
│   │   ├── 16/
│   │   ├── 17/
│   │   ├── 18/
│   │   ├── 19/
│   │   ├── 1a/
│   │   ├── 1b/
│   │   ├── 1c/
│   │   ├── 1d/
│   │   ├── 1e/
│   │   ├── 1f/
│   │   ├── 20/
│   │   ├── 21/
│   │   ├── 22/
│   │   ├── 23/
│   │   ├── 24/
│   │   ├── 25/
│   │   ├── 26/
│   │   ├── 27/
│   │   ├── 28/
│   │   ├── 29/
│   │   ├── 2a/
│   │   ├── 2b/
│   │   ├── 2c/
│   │   ├── 2d/
│   │   ├── 2e/
│   │   ├── 30/
│   │   ├── 31/
│   │   ├── 33/
│   │   ├── 34/
│   │   ├── 35/
│   │   ├── 36/
│   │   ├── 37/
│   │   ├── 38/
│   │   ├── 39/
│   │   ├── 3a/
│   │   ├── 3b/
│   │   ├── 3c/
│   │   ├── 3d/
│   │   ├── 3f/
│   │   ├── 40/
│   │   ├── 41/
│   │   ├── 42/
│   │   ├── 43/
│   │   ├── 44/
│   │   ├── 45/
│   │   ├── 46/
│   │   ├── 47/
│   │   ├── 48/
│   │   ├── 49/
│   │   ├── 4a/
│   │   ├── 4b/
│   │   ├── 4c/
│   │   ├── 4d/
│   │   ├── 4e/
│   │   ├── 4f/
│   │   ├── 50/
│   │   ├── 51/
│   │   ├── 52/
│   │   ├── 53/
│   │   ├── 54/
│   │   ├── 55/
│   │   ├── 56/
│   │   ├── 57/
│   │   ├── 58/
│   │   ├── 59/
│   │   ├── 5a/
│   │   ├── 5b/
│   │   ├── 5c/
│   │   ├── 5d/
│   │   ├── 5e/
│   │   ├── 5f/
│   │   ├── 60/
│   │   ├── 61/
│   │   ├── 62/
│   │   ├── 63/
│   │   ├── 64/
│   │   ├── 65/
│   │   ├── 66/
│   │   ├── 67/
│   │   ├── 68/
│   │   ├── 69/
│   │   ├── 6a/
│   │   ├── 6b/
│   │   ├── 6c/
│   │   ├── 6e/
│   │   ├── 6f/
│   │   ├── 70/
│   │   ├── 71/
│   │   ├── 72/
│   │   ├── 74/
│   │   ├── 75/
│   │   ├── 76/
│   │   ├── 77/
│   │   ├── 78/
│   │   ├── 7a/
│   │   ├── 7b/
│   │   ├── 7c/
│   │   ├── 7d/
│   │   ├── 7e/
│   │   ├── 7f/
│   │   ├── 81/
│   │   ├── 82/
│   │   ├── 83/
│   │   ├── 84/
│   │   ├── 85/
│   │   ├── 86/
│   │   ├── 87/
│   │   ├── 88/
│   │   ├── 89/
│   │   ├── 8a/
│   │   ├── 8b/
│   │   ├── 8c/
│   │   ├── 8d/
│   │   ├── 8e/
│   │   ├── 8f/
│   │   ├── 90/
│   │   ├── 91/
│   │   ├── 92/
│   │   ├── 94/
│   │   ├── 95/
│   │   ├── 97/
│   │   ├── 98/
│   │   ├── 99/
│   │   ├── 9a/
│   │   ├── 9c/
│   │   ├── 9d/
│   │   ├── 9e/
│   │   ├── 9f/
│   │   ├── a1/
│   │   ├── a2/
│   │   ├── a3/
│   │   ├── a4/
│   │   ├── a5/
│   │   ├── a6/
│   │   ├── a7/
│   │   ├── a8/
│   │   ├── a9/
│   │   ├── aa/
│   │   ├── ab/
│   │   ├── ac/
│   │   ├── ad/
│   │   ├── ae/
│   │   ├── af/
│   │   ├── b0/
│   │   ├── b1/
│   │   ├── b2/
│   │   ├── b4/
│   │   ├── b5/
│   │   ├── b6/
│   │   ├── b7/
│   │   ├── b8/
│   │   ├── b9/
│   │   ├── bb/
│   │   ├── bc/
│   │   ├── bd/
│   │   ├── be/
│   │   ├── bf/
│   │   ├── c0/
│   │   ├── c1/
│   │   ├── c2/
│   │   ├── c3/
│   │   ├── c5/
│   │   ├── c6/
│   │   ├── c7/
│   │   ├── c9/
│   │   ├── ca/
│   │   ├── cb/
│   │   ├── cc/
│   │   ├── cd/
│   │   ├── ce/
│   │   ├── cf/
│   │   ├── d0/
│   │   ├── d2/
│   │   ├── d3/
│   │   ├── d4/
│   │   ├── d5/
│   │   ├── d6/
│   │   ├── d7/
│   │   ├── d8/
│   │   ├── da/
│   │   ├── db/
│   │   ├── dc/
│   │   ├── dd/
│   │   ├── de/
│   │   ├── df/
│   │   ├── e0/
│   │   ├── e1/
│   │   ├── e3/
│   │   ├── e5/
│   │   ├── e6/
│   │   ├── e7/
│   │   ├── e8/
│   │   ├── e9/
│   │   ├── ea/
│   │   ├── eb/
│   │   ├── ec/
│   │   ├── ed/
│   │   ├── ee/
│   │   ├── f0/
│   │   ├── f1/
│   │   ├── f2/
│   │   ├── f3/
│   │   ├── f4/
│   │   ├── f5/
│   │   ├── f6/
│   │   ├── f7/
│   │   ├── f8/
│   │   ├── f9/
│   │   ├── fa/
│   │   ├── fb/
│   │   ├── fc/
│   │   ├── fe/
│   │   └── ff/
│   ├── Bee/
│   │   ├── artifacts/
│   │   ├── CachedNodeOutput/
│   │   ├── 1900b0aE-inputdata.json
│   │   ├── 1900b0aE.dag
│   │   ├── 1900b0aE.dag_derived
│   │   ├── 1900b0aE.dag_fsmtime
│   │   ├── 1900b0aE.dag.json
│   │   ├── 1900b0aE.dag.outputdata
│   │   ├── 1900b0aE.dag.payloads
│   │   ├── backend1.traceevents
│   │   ├── backend2.traceevents
│   │   ├── bee_backend.info
│   │   ├── buildprogram0.traceevents
│   │   ├── fullprofile.json
│   │   ├── tundra.digestcache
│   │   ├── tundra.log.json
│   │   ├── TundraBuildState.state
│   │   └── TundraBuildState.state.map
│   ├── BuildProfiles/
│   │   ├── PlatformProfile.4e3c793746204150860bf175a9a41a05.asset
│   │   ├── PlatformProfile.84a3bb9e7420477f885e98145999eb20.asset
│   │   └── SharedProfile.asset
│   ├── PackageCache/
│   │   ├── com.unity.modules.audio@1.0.0/
│   │   ├── com.unity.modules.imgui@1.0.0/
│   │   ├── com.unity.modules.particlesystem@1.0.0/
│   │   ├── com.unity.modules.physics2d@1.0.0/
│   │   ├── com.unity.modules.tilemap@1.0.0/
│   │   ├── com.unity.modules.ui@1.0.0/
│   │   ├── com.unity.textmeshpro@3.0.6/
│   │   └── com.unity.ugui@1.0.0/
│   ├── PackageManager/
│   │   ├── ProjectCache
│   │   ├── ProjectCache.md5
│   │   └── projectResolution.json
│   ├── PlayModeViewStates/
│   │   ├── 5291385de4fea2e43aaa31f0d4409d6e
│   │   └── fd245bf971e20c94d8a3a8ffc5d9bf2a
│   ├── ScriptAssemblies/
│   │   ├── Assembly-CSharp.dll
│   │   ├── Assembly-CSharp.pdb
│   │   ├── Unity.TextMeshPro.dll
│   │   ├── Unity.TextMeshPro.Editor.dll
│   │   ├── Unity.TextMeshPro.Editor.pdb
│   │   ├── Unity.TextMeshPro.pdb
│   │   ├── UnityEditor.UI.dll
│   │   ├── UnityEditor.UI.pdb
│   │   ├── UnityEngine.UI.dll
│   │   └── UnityEngine.UI.pdb
│   ├── Search/
│   │   ├── .SearchIndexArtifactImporter.262146.b.index
│   │   ├── .SearchIndexArtifactImporter.262146.b.index-lock
│   │   ├── 98957a664bd18c47a3e41b2a0189ef53.SearchIndexArtifactImporter.262146.b.index
│   │   ├── 98957a664bd18c47a3e41b2a0189ef53.SearchIndexArtifactImporter.262146.b.index-lock
│   │   ├── propertyAliases.db
│   │   ├── propertyAliases.db.st
│   │   ├── propertyDatabase.db
│   │   ├── propertyDatabase.db.st
│   │   └── transactions.db
│   ├── ShaderCache/
│   │   ├── builtin/
│   │   ├── shader/
│   │   └── EditorEncounteredVariants
│   ├── StateCache/
│   │   ├── LayerSettings/
│   │   ├── MainStageHierarchy/
│   │   └── SceneView/
│   ├── AnnotationManager
│   ├── ArtifactDB
│   ├── ArtifactDB-lock
│   ├── BuildPlayer.prefs
│   ├── BuildProfileContext.asset
│   ├── BuildSettings.asset
│   ├── EditorGridSettings.asset
│   ├── EditorOnlyScriptingSettings.json
│   ├── EditorOnlyVirtualTextureState.json
│   ├── EditorSnapSettings.asset
│   ├── EditorToolsSettings.asset
│   ├── EditorUserBuildSettings.asset
│   ├── expandedItems
│   ├── ilpp.pid
│   ├── InspectorExpandedItems.asset
│   ├── LastSceneManagerSetup.txt
│   ├── LibraryFormatVersion.txt
│   ├── MonoManager.asset
│   ├── SceneVisibilityState.asset
│   ├── ScriptMapper
│   ├── ShaderCache.db
│   ├── SourceAssetDB
│   ├── SourceAssetDB-lock
│   ├── SpriteAtlasDatabase.asset
│   ├── Style.catalog
│   ├── UndoData.bin
│   └── UndoStack.bin
├── Logs/
│   ├── Packages-Update.log
│   └── shadercompiler-UnityShaderCompiler.exe0.log
├── Packages/
│   ├── manifest.json
│   └── packages-lock.json
├── ProjectSettings/
│   ├── AudioManager.asset
│   ├── ClusterInputManager.asset
│   ├── DynamicsManager.asset
│   ├── EditorBuildSettings.asset
│   ├── EditorSettings.asset
│   ├── GraphicsSettings.asset
│   ├── InputManager.asset
│   ├── MemorySettings.asset
│   ├── MultiplayerManager.asset
│   ├── NavMeshAreas.asset
│   ├── PackageManagerSettings.asset
│   ├── Physics2DSettings.asset
│   ├── PresetManager.asset
│   ├── ProjectSettings.asset
│   ├── ProjectVersion.txt
│   ├── QualitySettings.asset
│   ├── TagManager.asset
│   ├── TimeManager.asset
│   ├── UnityConnectSettings.asset
│   ├── VersionControlSettings.asset
│   └── VFXManager.asset
├── UserSettings/
│   ├── Layouts/
│   │   ├── CurrentMaximizeLayout.dwlt
│   │   ├── default-2022.dwlt
│   │   └── default-6000.dwlt
│   ├── EditorUserSettings.asset
│   ├── Search.index
│   └── Search.settings
├── .gitignore
├── ImplementationPlan.docs
├── README.md
├── Thinking.docs
├── unity_build.log
├── UnityImplementationGuide.md
└── walkthrough.docs
```

---

## 🚀 Getting Started

### Prerequisites
- Modern web browser (Chrome, Edge, Firefox, Safari)

### Usage

1. **Clone the repository:**
   ```bash
   git clone https://github.com/WEB-TechWhiz/Arrow-Game.git
   cd Arrow-Game
   ```

2. **Run locally:**
   - Open `index.html` directly in your browser or run a local static server:
   ```bash
   npx serve .
   ```




## 🤝 Contributing
Contributions, feedback, and pull requests are warmly welcomed!
1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'feat: Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

---

## 👤 Author & License
- **Maintainer**: [WEB-TechWhiz](https://github.com/WEB-TechWhiz)
- **License**: Distributed under the MIT License.
