using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;

namespace ArrowNexus.Core
{
    /// <summary>
    /// Handles local save via PlayerPrefs + JSON.
    /// Firebase sync hookups would go here for Phase 4.
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        public static SaveSystem Instance { get; private set; }

        [System.Serializable]
        public class SaveData
        {
            public int MaxLevelReached = 1;
            public int TotalCoins = 0;
            // Add other persistent data here
        }

        private SaveData _currentData;
        private string _savePath;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            _savePath = Path.Combine(Application.persistentDataPath, "save.json");
            Load();
        }

        public SaveData GetData() => _currentData;

        public void Save()
        {
            string json = Serialize(_currentData);
            File.WriteAllText(_savePath, json);
            // Firebase sync call would go here
        }

        public void Load()
        {
            if (File.Exists(_savePath))
            {
                string json = File.ReadAllText(_savePath);
                _currentData = Deserialize(json);
            }
            else
            {
                _currentData = new SaveData();
                Save();
            }
        }
        
        public void UnlockLevel(int level)
        {
            if (level > _currentData.MaxLevelReached)
            {
                _currentData.MaxLevelReached = level;
                Save();
            }
        }

        private static string Serialize(SaveData data)
        {
            return "{\n"
                + $"  \"MaxLevelReached\": {data.MaxLevelReached},\n"
                + $"  \"TotalCoins\": {data.TotalCoins}\n"
                + "}";
        }

        private static SaveData Deserialize(string json)
        {
            return new SaveData
            {
                MaxLevelReached = ReadIntField(json, "MaxLevelReached", 1),
                TotalCoins = ReadIntField(json, "TotalCoins", 0)
            };
        }

        private static int ReadIntField(string json, string fieldName, int fallback)
        {
            if (string.IsNullOrWhiteSpace(json))
                return fallback;

            Match match = Regex.Match(json, $"\"{fieldName}\"\\s*:\\s*(-?\\d+)");
            return match.Success && int.TryParse(match.Groups[1].Value, out int value)
                ? value
                : fallback;
        }
    }
}
