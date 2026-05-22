using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace ArrowNexus.Core
{
    /// <summary>
    /// Creates a playable runtime scene when the project is opened in a blank Unity scene.
    /// This keeps the current codebase runnable without hand-building a level in the editor first.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameBootstrapper : MonoBehaviour
    {
        private static bool _bootstrapped;
        private int _levelIndex = 1;
        private bool _loadingNextLevel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrap()
        {
            if (_bootstrapped) return;

            if (UnityEngine.Object.FindObjectOfType<GameBootstrapper>() != null)
                return;

            // If a custom authored scene already contains the core game objects,
            // we leave it alone and let that scene drive the experience.
            if (UnityEngine.Object.FindObjectOfType<PlayerArrow>() != null
                || UnityEngine.Object.FindObjectOfType<ArrowNexus.Maze.TileManager>() != null)
                return;

            var go = new GameObject(nameof(GameBootstrapper));
            go.AddComponent<GameBootstrapper>();
        }

        private void Awake()
        {
            if (_bootstrapped)
            {
                Destroy(gameObject);
                return;
            }

            _bootstrapped = true;
            Application.targetFrameRate = 60;

            BuildRuntimeWorld();
        }

        private void BuildRuntimeWorld()
        {
            CleanupRuntimeWorld();
            CreateCoreManagers();

            var worldRoot = new GameObject("RuntimeWorld");
            var gridRoot = new GameObject("Grid");
            gridRoot.transform.SetParent(worldRoot.transform, false);
            gridRoot.AddComponent<Grid>();

            var groundTilemap = CreateTilemapLayer(gridRoot.transform, "Ground Tilemap", 0);
            var overlayTilemap = CreateTilemapLayer(gridRoot.transform, "Overlay Tilemap", 1);

            var tileManagerGO = new GameObject("TileManager");
            var tileManager = tileManagerGO.AddComponent<ArrowNexus.Maze.TileManager>();

            var tileSprite = CreateSolidSprite(16, 16);
            tileManager.ConfigureRuntime(
                groundTilemap,
                overlayTilemap,
                CreateTile(tileSprite, new Color(0.10f, 0.10f, 0.12f, 1f)),
                CreateTile(tileSprite, new Color(0.18f, 0.18f, 0.22f, 1f)),
                CreateTile(tileSprite, new Color(0.15f, 0.85f, 1f, 1f)),
                CreateTile(tileSprite, new Color(0.20f, 1f, 0.35f, 1f)),
                CreateTile(tileSprite, new Color(1f, 0.30f, 0.18f, 1f)),
                CreateTile(tileSprite, new Color(0.72f, 0.25f, 1f, 1f)),
                CreateTile(tileSprite, new Color(0.20f, 0.38f, 1f, 1f)),
                CreateTile(tileSprite, new Color(1f, 0.80f, 0.20f, 1f))
            );

            var settings = ArrowNexus.Maze.MazeGenerator.MazeSettings.Default;
            settings.Width = 31;
            settings.Height = 31;
            settings.Seed = DateTime.UtcNow.Year * 10000 + DateTime.UtcNow.DayOfYear + (_levelIndex * 997);
            settings.HazardDensity = 0f;
            settings.SignalNodeDensity = 0f;
            settings.SecretPathChance = 0f;

            int[,] grid = ArrowNexus.Maze.MazeGenerator.Generate(settings, out Vector2Int startCell, out _);
            startCell = ChoosePlayableStart(grid, startCell);
            NormalizeRuntimeMaze(grid, startCell);
            EnsurePlayableStart(grid, startCell);
            PlaceRuntimeHazards(grid, startCell, FindGoalCell(grid), 0.05f, 4);
            tileManager.BuildFromGrid(grid);
            tileManager.UpdateChunksAroundPlayer(startCell);

            var collisionSystemGO = new GameObject("CollisionSystem");
            var collisionSystem = collisionSystemGO.AddComponent<CollisionSystem>();
            collisionSystem.OnGoalReached += HandleGoalReached;

            var player = CreatePlayer(worldRoot.transform, startCell);
            CreateCamera(worldRoot.transform, grid);

            CreateOptionalManagers();
            CreateRuntimeEndMessages();
            CreateControlGuide();

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.SetMode(GameStateManager.GameMode.Classic);
                GameStateManager.Instance.ChangeState(GameStateManager.GameState.Playing);
            }
        }

        private void CleanupRuntimeWorld()
        {
            DestroyObjectIfFound("RuntimeWorld");
            DestroyObjectIfFound("TileManager");
            DestroyObjectIfFound("CollisionSystem");
            DestroyObjectIfFound("HUDManager");
            DestroyObjectIfFound("Runtime Message Canvas");
        }

        private static void DestroyObjectIfFound(string objectName)
        {
            var go = GameObject.Find(objectName);
            if (go != null)
                Destroy(go);
        }

        private void HandleGoalReached(Vector2Int cell)
        {
            if (_loadingNextLevel)
                return;

            StartCoroutine(LoadNextLevelRoutine());
        }

        private System.Collections.IEnumerator LoadNextLevelRoutine()
        {
            _loadingNextLevel = true;
            yield return new WaitForSeconds(1.25f);

            CleanupRuntimeWorld();
            yield return null;

            _levelIndex++;
            _loadingNextLevel = false;
            BuildRuntimeWorld();
        }

        private static void CreateCoreManagers()
        {
            CreateManager<InputManager>("InputManager");
            CreateManager<GameStateManager>("GameStateManager");
            CreateManager<SaveSystem>("SaveSystem");
        }

        private static void CreateOptionalManagers()
        {
            CreateManager<ArrowNexus.FX.FXManager>("FXManager");
            CreateManager<ArrowNexus.Audio.AudioManager>("AudioManager");
            CreateManager<ArrowNexus.Difficulty.AdaptiveDifficultyEngine>("AdaptiveDifficultyEngine");
            CreateManager<ArrowNexus.Meta.DailyChallengeSystem>("DailyChallengeSystem");
            CreateManager<ArrowNexus.Meta.GhostSystem>("GhostSystem");
            CreateManager<ArrowNexus.Meta.SpeedrunSystem>("SpeedrunSystem");
            CreateManager<ArrowNexus.Meta.MonetizationManager>("MonetizationManager");
            CreateManager<ArrowNexus.Mechanics.PulseTimer>("PulseTimer");
            CreateManager<ArrowNexus.UI.HUDManager>("HUDManager");
        }

        private static T CreateManager<T>(string name) where T : Component
        {
            var existing = UnityEngine.Object.FindObjectOfType<T>();
            if (existing != null)
                return existing;

            var go = new GameObject(name);
            return go.AddComponent<T>();
        }

        private static Tilemap CreateTilemapLayer(Transform parent, string name, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var tilemap = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;

            return tilemap;
        }

        private static PlayerArrow CreatePlayer(Transform parent, Vector2Int startCell)
        {
            var go = new GameObject("Player");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(startCell.x, startCell.y, 0f);

            var spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateArrowSprite(32, 32);
            spriteRenderer.color = new Color(1f, 0.12f, 0.08f, 1f);
            spriteRenderer.sortingOrder = 12;
            go.transform.localScale = Vector3.one * 0.9f;

            var markerGO = new GameObject("Player Marker");
            markerGO.transform.SetParent(go.transform, false);
            markerGO.transform.localScale = Vector3.one * 1.05f;

            var markerRenderer = markerGO.AddComponent<SpriteRenderer>();
            markerRenderer.sprite = CreateRingSprite(64, 64);
            markerRenderer.color = new Color(1f, 1f, 1f, 0.9f);
            markerRenderer.sortingOrder = 11;

            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one * 0.8f;

            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;

            var player = go.AddComponent<PlayerArrow>();
            player.Initialise(startCell);
            return player;
        }

        private static void CreateCamera(Transform parent, int[,] grid)
        {
            Camera camera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
            GameObject go;

            if (camera != null)
            {
                go = camera.gameObject;
            }
            else
            {
                go = new GameObject("Main Camera");
                camera = go.AddComponent<Camera>();
            }

            go.name = "Main Camera";
            go.tag = "MainCamera";
            go.transform.SetParent(parent, false);

            float centerX = (grid.GetLength(0) - 1) * 0.5f;
            float centerY = (grid.GetLength(1) - 1) * 0.5f;
            go.transform.position = new Vector3(centerX, centerY, -10f);

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.84f, 0.90f, 0.96f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(grid.GetLength(1), grid.GetLength(0) / camera.aspect) * 0.55f;

            var followController = go.GetComponent<CameraController>();
            if (followController != null)
                Destroy(followController);

            if (FindObjectOfType<AudioListener>() == null)
                go.AddComponent<AudioListener>();
        }

        private static void CreateControlGuide()
        {
            if (FindObjectOfType<ArrowNexus.UI.ControlGuideOverlay>() != null)
                return;

            var canvasGO = new GameObject("Control Guide Canvas");
            canvasGO.AddComponent<RectTransform>();
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            var group = canvasGO.AddComponent<CanvasGroup>();
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;

            var panelGO = new GameObject("Panel");
            panelGO.transform.SetParent(canvasGO.transform, false);
            var panelRect = panelGO.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(0f, 0f);
            panelRect.pivot = new Vector2(0f, 0f);
            panelRect.sizeDelta = new Vector2(430f, 180f);
            panelRect.anchoredPosition = new Vector2(20f, 20f);

            var panelImage = panelGO.AddComponent<Image>();
            panelImage.color = new Color(0.03f, 0.05f, 0.09f, 0.88f);
            var outline = panelGO.AddComponent<Outline>();
            outline.effectColor = new Color(0.15f, 0.9f, 1f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(panelGO.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = new Vector2(18f, 16f);
            textRect.offsetMax = new Vector2(-18f, -16f);

            var text = textGO.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text =
                "<b>HOW TO PLAY</b>\n" +
                "Move: WASD or Arrow Keys\n" +
                "Dash: Space\n" +
                "Ability: Shift\n" +
                "Pause: Esc\n" +
                "Goal: Reach the green core\n" +
                "Tip: Keep moving to build momentum\n" +
                "This guide hides after you start playing";
            text.fontSize = 20;
            text.color = new Color(0.92f, 0.97f, 1f, 1f);
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = true;

            var guide = canvasGO.AddComponent<ArrowNexus.UI.ControlGuideOverlay>();
            guide.Setup(InputManager.Instance, group);
        }

        private static void CreateRuntimeEndMessages()
        {
            var hud = FindObjectOfType<ArrowNexus.UI.HUDManager>();
            if (hud == null)
                return;

            var canvasGO = new GameObject("Runtime Message Canvas");
            canvasGO.AddComponent<RectTransform>();
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1100;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            GameObject deathScreen = CreateMessagePanel(
                canvasGO.transform,
                "Death Screen",
                "Game Over",
                "You hit a hazard. Stop Play and press Play again to restart."
            );

            GameObject levelCompleteScreen = CreateMessagePanel(
                canvasGO.transform,
                "Level Complete Screen",
                "Level Complete",
                "You reached the green core. Loading the next maze..."
            );

            deathScreen.SetActive(false);
            levelCompleteScreen.SetActive(false);
            hud.ConfigureRuntimeScreens(deathScreen, levelCompleteScreen);
        }

        private static GameObject CreateMessagePanel(Transform parent, string name, string title, string body)
        {
            var panelGO = new GameObject(name);
            panelGO.transform.SetParent(parent, false);

            var rect = panelGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var backdrop = panelGO.AddComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0.68f);

            var boxGO = new GameObject("Message Box");
            boxGO.transform.SetParent(panelGO.transform, false);

            var boxRect = boxGO.AddComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.pivot = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(620f, 260f);
            boxRect.anchoredPosition = Vector2.zero;

            var boxImage = boxGO.AddComponent<Image>();
            boxImage.color = new Color(0.92f, 0.96f, 1f, 0.96f);

            var titleGO = CreateMessageText(boxGO.transform, "Title", title, 52, FontStyle.Bold);
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.55f);
            titleRect.anchorMax = new Vector2(1f, 0.9f);
            titleRect.offsetMin = new Vector2(40f, 0f);
            titleRect.offsetMax = new Vector2(-40f, 0f);

            var bodyGO = CreateMessageText(boxGO.transform, "Body", body, 26, FontStyle.Normal);
            var bodyRect = bodyGO.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0f, 0.12f);
            bodyRect.anchorMax = new Vector2(1f, 0.54f);
            bodyRect.offsetMin = new Vector2(42f, 0f);
            bodyRect.offsetMax = new Vector2(-42f, 0f);

            return panelGO;
        }

        private static GameObject CreateMessageText(Transform parent, string name, string content, int fontSize, FontStyle style)
        {
            var textGO = new GameObject(name);
            textGO.transform.SetParent(parent, false);

            var rect = textGO.AddComponent<RectTransform>();
            rect.anchoredPosition = Vector2.zero;

            var text = textGO.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = new Color(0.04f, 0.06f, 0.09f, 1f);
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            return textGO;
        }

        private static void EnsurePlayableStart(int[,] grid, Vector2Int startCell)
        {
            grid[startCell.x, startCell.y] = ArrowNexus.Maze.MazeGenerator.START;

            Vector2Int[] exits =
            {
                Vector2Int.right,
                Vector2Int.up,
                Vector2Int.left,
                Vector2Int.down
            };

            bool hasExit = false;
            foreach (Vector2Int exit in exits)
            {
                Vector2Int cell = startCell + exit;
                if (!IsOpenCell(grid, cell))
                    continue;

                grid[cell.x, cell.y] = ArrowNexus.Maze.MazeGenerator.PATH;
                hasExit = true;
            }

            if (hasExit)
                return;

            foreach (Vector2Int exit in exits)
            {
                Vector2Int cell = startCell + exit;
                if (!IsInBounds(grid, cell))
                    continue;

                grid[cell.x, cell.y] = ArrowNexus.Maze.MazeGenerator.PATH;
                return;
            }
        }

        private static void NormalizeRuntimeMaze(int[,] grid, Vector2Int startCell)
        {
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    int tile = grid[x, y];
                    if (tile == ArrowNexus.Maze.MazeGenerator.START
                        || tile == ArrowNexus.Maze.MazeGenerator.GOAL
                        || tile == ArrowNexus.Maze.MazeGenerator.HAZARD
                        || tile == ArrowNexus.Maze.MazeGenerator.SIGNAL_NODE
                        || tile == ArrowNexus.Maze.MazeGenerator.SECRET
                        || tile == ArrowNexus.Maze.MazeGenerator.SPEED_ZONE
                        || tile == ArrowNexus.Maze.MazeGenerator.DEAD_END)
                    {
                        grid[x, y] = ArrowNexus.Maze.MazeGenerator.PATH;
                    }
                }
            }

            Vector2Int goalCell = FindFarthestOpenCell(grid, startCell);
            if (IsInBounds(grid, goalCell) && goalCell != startCell)
                grid[goalCell.x, goalCell.y] = ArrowNexus.Maze.MazeGenerator.GOAL;
        }

        private static Vector2Int FindFarthestOpenCell(int[,] grid, Vector2Int startCell)
        {
            Vector2Int farthest = startCell;
            float farthestDistance = 0f;
            Queue<Vector2Int> frontier = new();
            HashSet<Vector2Int> visited = new();

            frontier.Enqueue(startCell);
            visited.Add(startCell);

            while (frontier.Count > 0)
            {
                Vector2Int current = frontier.Dequeue();
                float distance = Vector2Int.Distance(startCell, current);
                if (distance > farthestDistance)
                {
                    farthest = current;
                    farthestDistance = distance;
                }

                TryAddOpenNeighbor(grid, current + Vector2Int.right, frontier, visited);
                TryAddOpenNeighbor(grid, current + Vector2Int.up, frontier, visited);
                TryAddOpenNeighbor(grid, current + Vector2Int.left, frontier, visited);
                TryAddOpenNeighbor(grid, current + Vector2Int.down, frontier, visited);
            }

            return farthest;
        }

        private static void PlaceRuntimeHazards(
            int[,] grid,
            Vector2Int startCell,
            Vector2Int goalCell,
            float density,
            int safeRadius)
        {
            for (int x = 1; x < grid.GetLength(0) - 1; x++)
            {
                for (int y = 1; y < grid.GetLength(1) - 1; y++)
                {
                    Vector2Int cell = new(x, y);
                    if (grid[x, y] != ArrowNexus.Maze.MazeGenerator.PATH)
                        continue;

                    if (cell == goalCell)
                        continue;

                    if (Vector2Int.Distance(startCell, cell) <= safeRadius)
                        continue;

                    if (CountOpenNeighbors(grid, cell) < 2)
                        continue;

                    if (UnityEngine.Random.value > density)
                        continue;

                    grid[x, y] = ArrowNexus.Maze.MazeGenerator.HAZARD;
                    if (!HasSafePath(grid, startCell, goalCell))
                        grid[x, y] = ArrowNexus.Maze.MazeGenerator.PATH;
                }
            }
        }

        private static Vector2Int FindGoalCell(int[,] grid)
        {
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    if (grid[x, y] == ArrowNexus.Maze.MazeGenerator.GOAL)
                        return new Vector2Int(x, y);
                }
            }

            return Vector2Int.zero;
        }

        private static bool HasSafePath(int[,] grid, Vector2Int startCell, Vector2Int goalCell)
        {
            Queue<Vector2Int> frontier = new();
            HashSet<Vector2Int> visited = new();

            frontier.Enqueue(startCell);
            visited.Add(startCell);

            while (frontier.Count > 0)
            {
                Vector2Int current = frontier.Dequeue();
                if (current == goalCell)
                    return true;

                TryAddSafeNeighbor(grid, current + Vector2Int.right, frontier, visited);
                TryAddSafeNeighbor(grid, current + Vector2Int.up, frontier, visited);
                TryAddSafeNeighbor(grid, current + Vector2Int.left, frontier, visited);
                TryAddSafeNeighbor(grid, current + Vector2Int.down, frontier, visited);
            }

            return false;
        }

        private static void TryAddSafeNeighbor(
            int[,] grid,
            Vector2Int cell,
            Queue<Vector2Int> frontier,
            HashSet<Vector2Int> visited)
        {
            if (visited.Contains(cell) || !IsSafePathCell(grid, cell))
                return;

            visited.Add(cell);
            frontier.Enqueue(cell);
        }

        private static bool IsSafePathCell(int[,] grid, Vector2Int cell)
        {
            if (!IsInBounds(grid, cell))
                return false;

            int tile = grid[cell.x, cell.y];
            return tile != ArrowNexus.Maze.MazeGenerator.WALL
                && tile != ArrowNexus.Maze.MazeGenerator.HAZARD;
        }

        private static void TryAddOpenNeighbor(
            int[,] grid,
            Vector2Int cell,
            Queue<Vector2Int> frontier,
            HashSet<Vector2Int> visited)
        {
            if (visited.Contains(cell) || !IsOpenCell(grid, cell))
                return;

            visited.Add(cell);
            frontier.Enqueue(cell);
        }

        private static Vector2Int ChoosePlayableStart(int[,] grid, Vector2Int fallbackStart)
        {
            HashSet<Vector2Int> visited = new();
            List<Vector2Int> largestComponent = new();

            for (int x = 1; x < grid.GetLength(0) - 1; x++)
            {
                for (int y = 1; y < grid.GetLength(1) - 1; y++)
                {
                    Vector2Int cell = new(x, y);
                    if (visited.Contains(cell) || !IsOpenCell(grid, cell))
                        continue;

                    List<Vector2Int> component = CollectOpenComponent(grid, cell, visited);
                    if (component.Count > largestComponent.Count)
                        largestComponent = component;
                }
            }

            if (largestComponent.Count == 0)
                return fallbackStart;

            return ChooseStartFromComponent(grid, largestComponent, fallbackStart);
        }

        private static List<Vector2Int> CollectOpenComponent(
            int[,] grid,
            Vector2Int startCell,
            HashSet<Vector2Int> visited)
        {
            List<Vector2Int> component = new();
            Queue<Vector2Int> frontier = new();

            frontier.Enqueue(startCell);
            visited.Add(startCell);

            while (frontier.Count > 0)
            {
                Vector2Int current = frontier.Dequeue();
                component.Add(current);

                TryAddOpenNeighbor(grid, current + Vector2Int.right, frontier, visited);
                TryAddOpenNeighbor(grid, current + Vector2Int.up, frontier, visited);
                TryAddOpenNeighbor(grid, current + Vector2Int.left, frontier, visited);
                TryAddOpenNeighbor(grid, current + Vector2Int.down, frontier, visited);
            }

            return component;
        }

        private static Vector2Int ChooseStartFromComponent(
            int[,] grid,
            List<Vector2Int> component,
            Vector2Int fallbackStart)
        {
            Vector2Int best = component[0];
            float bestDistance = float.MaxValue;

            foreach (Vector2Int cell in component)
            {
                if (!IsConnectedCorridor(grid, cell))
                    continue;

                float distance = Vector2Int.Distance(fallbackStart, cell);
                if (distance < bestDistance)
                {
                    best = cell;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private static bool IsConnectedCorridor(int[,] grid, Vector2Int cell)
        {
            if (!IsOpenCell(grid, cell))
                return false;

            return CountOpenNeighbors(grid, cell) >= 2;
        }

        private static int CountOpenNeighbors(int[,] grid, Vector2Int cell)
        {
            int count = 0;
            if (IsOpenCell(grid, cell + Vector2Int.right)) count++;
            if (IsOpenCell(grid, cell + Vector2Int.up)) count++;
            if (IsOpenCell(grid, cell + Vector2Int.left)) count++;
            if (IsOpenCell(grid, cell + Vector2Int.down)) count++;
            return count;
        }

        private static bool IsOpenCell(int[,] grid, Vector2Int cell)
        {
            if (!IsInBounds(grid, cell))
                return false;

            return grid[cell.x, cell.y] != ArrowNexus.Maze.MazeGenerator.WALL;
        }

        private static bool IsInBounds(int[,] grid, Vector2Int cell)
        {
            return cell.x >= 0
                && cell.y >= 0
                && cell.x < grid.GetLength(0)
                && cell.y < grid.GetLength(1);
        }

        private static Tile CreateTile(Sprite sprite, Color tint)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.color = tint;
            tile.colliderType = Tile.ColliderType.None;
            return tile;
        }

        private static Sprite CreateSolidSprite(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(255, 255, 255, 255);

            texture.SetPixels32(pixels);
            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                16f
            );
        }

        private static Sprite CreateArrowSprite(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool shaft = x >= 2 && x <= width - 8 && y >= height / 2 - 2 && y <= height / 2 + 2;
                    bool head = x > width - 8 && x < width - 1
                        && y >= (height / 2) - (x - (width - 8))
                        && y <= (height / 2) + (x - (width - 8));

                    pixels[y * width + x] = (shaft || head)
                        ? new Color32(255, 255, 255, 255)
                        : new Color32(255, 255, 255, 0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                16f
            );
        }

        private static Sprite CreateRingSprite(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[width * height];
            Vector2 center = new((width - 1) * 0.5f, (height - 1) * 0.5f);
            float outer = Mathf.Min(width, height) * 0.46f;
            float inner = Mathf.Min(width, height) * 0.34f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    bool inRing = dist <= outer && dist >= inner;
                    pixels[y * width + x] = inRing
                        ? new Color32(255, 255, 255, 255)
                        : new Color32(255, 255, 255, 0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                32f
            );
        }
    }
}
