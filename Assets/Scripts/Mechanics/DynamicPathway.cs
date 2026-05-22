using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ArrowNexus.Maze;

namespace ArrowNexus.Mechanics
{
    /// <summary>
    /// Implements the Dynamic Pathways mechanic:
    /// Maze segments can Open, Close, Rotate, or Collapse on timers or Signal Node triggers.
    /// Works with TileManager to update the grid at runtime.
    /// </summary>
    public class DynamicPathway : MonoBehaviour
    {
        // ─── State Enum ──────────────────────────────────────────────────────────
        public enum PathwayState { Open, Closed, Rotating, Collapsing }

        // ─── Inspector Config ────────────────────────────────────────────────────
        [Header("Pathway Cells")]
        [SerializeField] private List<Vector2Int> _cells = new();   // affected grid cells

        [Header("Behaviour")]
        [SerializeField] private PathwayState _initialState = PathwayState.Open;
        [SerializeField] private float        _cycleInterval = 3f;    // seconds between state change
        [SerializeField] private bool         _timerDriven   = true;  // auto-cycle on timer
        [SerializeField] private bool         _signalDriven  = false; // triggered by SignalNode

        [Header("Collapse Settings")]
        [SerializeField] private float _collapseWarningTime = 1.2f;  // warning flash before collapse
        [SerializeField] private float _reopenDelay         = 4f;    // seconds before reopening

        // ─── Events ──────────────────────────────────────────────────────────────
        public event Action<PathwayState> OnStateChanged;

        // ─── State ───────────────────────────────────────────────────────────────
        public PathwayState CurrentState { get; private set; }

        private Coroutine _cycleCoroutine;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        private void Start()
        {
            CurrentState = _initialState;
            ApplyStateTiles(CurrentState);

            if (_timerDriven)
                _cycleCoroutine = StartCoroutine(TimerCycle());
        }

        // ─── Public Trigger ──────────────────────────────────────────────────────

        /// <summary>Called by SignalNode to trigger a state change.</summary>
        public void TriggerBySignal()
        {
            if (!_signalDriven) return;
            TransitionTo(CurrentState == PathwayState.Open ? PathwayState.Closed : PathwayState.Open);
        }

        public void ForceOpen()  => TransitionTo(PathwayState.Open);
        public void ForceClose() => TransitionTo(PathwayState.Closed);

        // ─── Timer Cycle ─────────────────────────────────────────────────────────

        private IEnumerator TimerCycle()
        {
            while (true)
            {
                yield return new WaitForSeconds(_cycleInterval);

                PathwayState next = CurrentState switch
                {
                    PathwayState.Open     => PathwayState.Collapsing,
                    PathwayState.Collapsing => PathwayState.Closed,
                    PathwayState.Closed   => PathwayState.Open,
                    _                     => PathwayState.Open
                };

                if (next == PathwayState.Collapsing)
                    yield return StartCoroutine(CollapseSequence());
                else
                    TransitionTo(next);
            }
        }

        // ─── Collapse Sequence (Warning + Close) ─────────────────────────────────

        private IEnumerator CollapseSequence()
        {
            TransitionTo(PathwayState.Collapsing);

            // Flash warning via FXManager
            float elapsed = 0f;
            while (elapsed < _collapseWarningTime)
            {
                elapsed += Time.deltaTime;
                // FX: visual warning flash triggered every 0.2s
                yield return new WaitForSeconds(0.2f);
            }

            TransitionTo(PathwayState.Closed);
            yield return new WaitForSeconds(_reopenDelay);
            TransitionTo(PathwayState.Open);
        }

        // ─── State Machine ───────────────────────────────────────────────────────

        private void TransitionTo(PathwayState next)
        {
            CurrentState = next;
            ApplyStateTiles(next);
            OnStateChanged?.Invoke(next);
        }

        private void ApplyStateTiles(PathwayState state)
        {
            if (ArrowNexus.Maze.TileManager.Instance == null) return;

            foreach (Vector2Int cell in _cells)
            {
                int tileType = state == PathwayState.Open ? MazeGenerator.PATH : MazeGenerator.WALL;
                ArrowNexus.Maze.TileManager.Instance.SetCell(cell, tileType);
            }
        }
    }
}
