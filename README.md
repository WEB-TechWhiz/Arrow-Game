# 🎯 Arrow Nexus - Precision Puzzle Labyrinth

**Arrow Nexus** is a minimalist, neon-cyber puzzle game built in **Unity (C#)**. The player controls a directional arrow (or pulse entity) through procedurally generated circuits and labyrinths filled with dynamic route systems, shifting pathways, active hazards, and logical switches.

---

## 🎮 Game Concept & Mechanics

### Visual Aesthetic
Inspired by abstract circuit-board patterns and glowing neon minimalist interfaces, Arrow Nexus drops the player into a clean, responsive layout where every step requires visual planning and precision.

### The Loop
1. **Observe**: Inspect the layout of the procedurally generated maze.
2. **Plan**: Formulate the path to trigger circuit nodes while dodging security nodes.
3. **Move**: Control the directional arrow with swift movements.
4. **Trigger**: Activate logic gates and buttons to unlock passages.
5. **Core Reach**: Navigate to the core node to unlock the next maze.

---

## 🛠️ Technology Stack
* **Game Engine**: Unity (Recommended Version: `2022.3 LTS`)
* **Language**: C# (.NET Core)
* **2D Graphics**: Unity Tilemap System with customized GPU-instanced shaders
* **Audio**: Adaptive, reactive soundtracks configured via audio manager

---

## 📁 Project Structure
* **`Assets/Scripts/Core/`**: Core bootstrap, game managers, and game loops.
* **`Assets/Scripts/Player/`**: Character movement, state machine, and collision behaviors.
* **`Assets/Scripts/Maze/`**: Procedural maze grid generation and pathfinding solvers.
* **`ProjectSettings/`**: Game config, target frames, and package dependencies.

---

## 🚀 How to Run in Unity
1. Install **Unity 2022.3 LTS** via Unity Hub.
2. Clone this repository and open the folder in Unity Hub.
3. Open the main bootstrap script at `Assets/Scripts/Core/GameBootstrapper.cs` to see how the procedural maze is bootstrapped on Play.
4. Press **Play** in an empty scene to automatically generate the grid and start playing!
