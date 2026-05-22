using UnityEngine;

namespace ArrowNexus.Data
{
    /// <summary>
    /// ScriptableObject to store handcrafted level configurations.
    /// Used for the 20 handcrafted MVP levels across 4 worlds.
    /// </summary>
    [CreateAssetMenu(fileName = "NewLevelData", menuName = "ArrowNexus/LevelData")]
    public class LevelDatabase : ScriptableObject
    {
        public int WorldID;
        public int LevelID;
        public string LevelName;
        
        [Header("Level Content")]
        public TextAsset GridData; // e.g. JSON or CSV defining the tile layout
        public float ParTime;
        public int AllowedDeaths = 3;
        
        [Header("Mechanics Enablers")]
        public bool HasDynamicPathways;
        public bool HasGravityChannels;
        public bool HasTeleporters;
    }
}
