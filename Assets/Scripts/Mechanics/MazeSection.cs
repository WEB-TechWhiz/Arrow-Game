using System;
using System.Collections;
using UnityEngine;

namespace ArrowNexus.Mechanics
{
    /// <summary>
    /// Placeholder for a maze sub-region that can be reshuffled by a Layout Changer signal node.
    /// Actual implementation integrates with MazeGenerator to re-run generation on a bounded area.
    /// </summary>
    public class MazeSection : MonoBehaviour
    {
        [SerializeField] private Vector2Int _origin;
        [SerializeField] private Vector2Int _size;

        public void Reshuffle()
        {
            // Re-generate the sub-grid within _origin to _origin+_size
            // and push result to TileManager
            Debug.Log($"[MazeSection] Reshuffling region {_origin} size {_size}");
            // Full implementation: call MazeGenerator.GenerateSubRegion(_origin, _size, seed)
            // then TileManager.Instance.ApplySubGrid(result, _origin);
        }
    }
}
