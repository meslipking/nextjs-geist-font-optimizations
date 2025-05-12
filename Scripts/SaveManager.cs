using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private string saveFilePath;
    private const string saveFileName = "game_save.dat";
    private const string settingsFileName = "game_settings.dat";

    [System.Serializable]
    public class GameSaveData
    {
        public List<UnlockedAnimal> unlockedAnimals = new List<UnlockedAnimal>();
        public int playerLevel;
        public int experience;
        public int currency;
        public Dictionary<string, int> achievements = new Dictionary<string, int>();
        public List<string> completedTutorials = new List<string>();
    }

    [System.Serializable]
    public class UnlockedAnimal
    {
        public string animalName;
        public AnimalRank rank;
        public AnimalType type;
        public int level;
        public int experience;
        public List<string> unlockedAbilities = new List<string>();
    }

    [System.Serializable]
    public class GameSettings
    {
        public float masterVolume = 1f;
        public float musicVolume = 0.7f;
        public float sfxVolume = 0.8f;
        public float uiVolume = 0.5f;
        public bool tutorialEnabled = true;
        public bool highQualityEffects = true;
        public int targetFrameRate = 60;
        public int qualityLevel = 2;
        public bool vibrationEnabled = true;
    }

    private GameSaveData currentSaveData;
    private GameSettings currentSettings;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSaveSystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeSaveSystem()
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, saveFileName);
        LoadGame();
        LoadSettings();
    }

    #region Save Operations

    public void SaveGame()
    {
        try
        {
            if (currentSaveData == null)
            {
                currentSaveData = new GameSaveData();
            }

            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream stream = new FileStream(saveFilePath, FileMode.Create))
            {
                formatter.Serialize(stream, currentSaveData);
            }

            Debug.Log("Game saved successfully!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error saving game: {e.Message}");
        }
    }

    public void LoadGame()
    {
        try
        {
            if (File.Exists(saveFilePath))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                using (FileStream stream = new FileStream(saveFilePath, FileMode.Open))
                {
                    currentSaveData = (GameSaveData)formatter.Deserialize(stream);
                }
            }
            else
            {
                currentSaveData = new GameSaveData();
                SaveGame(); // Create initial save file
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading game: {e.Message}");
            currentSaveData = new GameSaveData();
        }
    }

    public void SaveSettings()
    {
        try
        {
            string settingsPath = Path.Combine(Application.persistentDataPath, settingsFileName);
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream stream = new FileStream(settingsPath, FileMode.Create))
            {
                formatter.Serialize(stream, currentSettings);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error saving settings: {e.Message}");
        }
    }

    public void LoadSettings()
    {
        try
        {
            string settingsPath = Path.Combine(Application.persistentDataPath, settingsFileName);
            if (File.Exists(settingsPath))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                using (FileStream stream = new FileStream(settingsPath, FileMode.Open))
                {
                    currentSettings = (GameSettings)formatter.Deserialize(stream);
                }
            }
            else
            {
                currentSettings = new GameSettings();
                SaveSettings(); // Create initial settings file
            }

            ApplySettings();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading settings: {e.Message}");
            currentSettings = new GameSettings();
        }
    }

    #endregion

    #region Game Data Operations

    public void UnlockAnimal(string animalName, AnimalRank rank, AnimalType type)
    {
        if (currentSaveData.unlockedAnimals.Exists(a => a.animalName == animalName))
        {
            Debug.LogWarning($"Animal {animalName} is already unlocked!");
            return;
        }

        UnlockedAnimal newAnimal = new UnlockedAnimal
        {
            animalName = animalName,
            rank = rank,
            type = type,
            level = 1,
            experience = 0
        };

        currentSaveData.unlockedAnimals.Add(newAnimal);
        SaveGame();
    }

    public void AddExperience(int amount)
    {
        currentSaveData.experience += amount;
        // Check for level up
        int newLevel = CalculateLevel(currentSaveData.experience);
        if (newLevel > currentSaveData.playerLevel)
        {
            currentSaveData.playerLevel = newLevel;
            // Trigger level up event or rewards
        }
        SaveGame();
    }

    public void AddCurrency(int amount)
    {
        currentSaveData.currency += amount;
        SaveGame();
    }

    public void UnlockAchievement(string achievementId)
    {
        if (!currentSaveData.achievements.ContainsKey(achievementId))
        {
            currentSaveData.achievements[achievementId] = 1;
        }
        else
        {
            currentSaveData.achievements[achievementId]++;
        }
        SaveGame();
    }

    public void CompleteTutorial(string tutorialId)
    {
        if (!currentSaveData.completedTutorials.Contains(tutorialId))
        {
            currentSaveData.completedTutorials.Add(tutorialId);
            SaveGame();
        }
    }

    #endregion

    #region Settings Operations

    public void UpdateSettings(GameSettings newSettings)
    {
        currentSettings = newSettings;
        ApplySettings();
        SaveSettings();
    }

    private void ApplySettings()
    {
        if (currentSettings == null) return;

        // Apply audio settings
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetVolume("MasterVolume", currentSettings.masterVolume);
            AudioManager.Instance.SetVolume("MusicVolume", currentSettings.musicVolume);
            AudioManager.Instance.SetVolume("SFXVolume", currentSettings.sfxVolume);
            AudioManager.Instance.SetVolume("UIVolume", currentSettings.uiVolume);
        }

        // Apply quality settings
        QualitySettings.SetQualityLevel(currentSettings.qualityLevel);
        Application.targetFrameRate = currentSettings.targetFrameRate;
    }

    #endregion

    #region Utility Methods

    private int CalculateLevel(int experience)
    {
        // Simple level calculation: each level requires 1000 * level XP
        int level = 1;
        int xpRequired = 1000;
        
        while (experience >= xpRequired)
        {
            experience -= xpRequired;
            level++;
            xpRequired = 1000 * level;
        }
        
        return level;
    }

    public bool IsAnimalUnlocked(string animalName)
    {
        return currentSaveData.unlockedAnimals.Exists(a => a.animalName == animalName);
    }

    public bool IsTutorialCompleted(string tutorialId)
    {
        return currentSaveData.completedTutorials.Contains(tutorialId);
    }

    public int GetAchievementProgress(string achievementId)
    {
        return currentSaveData.achievements.ContainsKey(achievementId) ? 
               currentSaveData.achievements[achievementId] : 0;
    }

    public void ResetSaveData()
    {
        currentSaveData = new GameSaveData();
        SaveGame();
    }

    public void ResetSettings()
    {
        currentSettings = new GameSettings();
        ApplySettings();
        SaveSettings();
    }

    #endregion
}
