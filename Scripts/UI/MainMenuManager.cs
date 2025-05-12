using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject mainPanel;
    public GameObject settingsPanel;
    public GameObject animalCollectionPanel;
    public GameObject creditsPanel;
    public GameObject loadingPanel;

    [Header("Main Menu UI")]
    public Button playButton;
    public Button collectionButton;
    public Button settingsButton;
    public Button creditsButton;
    public Button quitButton;
    public TextMeshProUGUI versionText;
    public TextMeshProUGUI playerLevelText;
    public Image experienceBar;

    [Header("Settings UI")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Slider uiVolumeSlider;
    public Toggle fullscreenToggle;
    public TMP_Dropdown qualityDropdown;
    public Toggle tutorialToggle;
    public Button resetSettingsButton;
    public Button backFromSettingsButton;

    [Header("Animal Collection UI")]
    public Transform animalGridContainer;
    public GameObject animalCardPrefab;
    public Button backFromCollectionButton;
    public TextMeshProUGUI collectionProgressText;

    [Header("Visual Effects")]
    public ParticleSystem backgroundEffect;
    public Animator menuAnimator;
    public float transitionDuration = 0.5f;
    public CanvasGroup fadeCanvasGroup;

    [Header("Audio")]
    public AudioClip menuMusic;
    public AudioClip buttonHoverSound;
    public AudioClip buttonClickSound;
    public AudioClip transitionSound;

    private void Start()
    {
        InitializeMainMenu();
    }

    private void InitializeMainMenu()
    {
        // Set version text
        if (versionText != null)
            versionText.text = $"Version {Application.version}";

        // Initialize buttons
        SetupButtons();

        // Load and apply settings
        LoadSettings();

        // Show main panel
        ShowPanel(mainPanel);
        HidePanel(settingsPanel);
        HidePanel(animalCollectionPanel);
        HidePanel(creditsPanel);
        HidePanel(loadingPanel);

        // Play menu music
        AudioManager.Instance?.PlayMusic(menuMusic);

        // Update player info
        UpdatePlayerInfo();

        // Start background effects
        if (backgroundEffect != null)
            backgroundEffect.Play();
    }

    private void SetupButtons()
    {
        // Main menu buttons
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayButtonClicked);
        
        if (collectionButton != null)
            collectionButton.onClick.AddListener(OnCollectionButtonClicked);
        
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsButtonClicked);
        
        if (creditsButton != null)
            creditsButton.onClick.AddListener(OnCreditsButtonClicked);
        
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitButtonClicked);

        // Settings buttons
        if (resetSettingsButton != null)
            resetSettingsButton.onClick.AddListener(OnResetSettingsClicked);
        
        if (backFromSettingsButton != null)
            backFromSettingsButton.onClick.AddListener(() => ShowPanel(mainPanel));

        // Collection buttons
        if (backFromCollectionButton != null)
            backFromCollectionButton.onClick.AddListener(() => ShowPanel(mainPanel));

        // Add hover sounds to all buttons
        Button[] allButtons = GetComponentsInChildren<Button>(true);
        foreach (Button button in allButtons)
        {
            AddButtonSounds(button);
        }
    }

    private void AddButtonSounds(Button button)
    {
        // Add hover sound
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = button.gameObject.AddComponent<EventTrigger>();

        var pointerEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        pointerEnter.callback.AddListener((data) => OnButtonHover());
        trigger.triggers.Add(pointerEnter);
    }

    private void LoadSettings()
    {
        // Load settings from SaveManager
        var settings = SaveManager.Instance.GetSettings();

        // Apply settings to UI
        if (masterVolumeSlider != null)
            masterVolumeSlider.value = settings.masterVolume;
        
        if (musicVolumeSlider != null)
            musicVolumeSlider.value = settings.musicVolume;
        
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = settings.sfxVolume;
        
        if (uiVolumeSlider != null)
            uiVolumeSlider.value = settings.uiVolume;

        if (fullscreenToggle != null)
            fullscreenToggle.isOn = Screen.fullScreen;

        if (qualityDropdown != null)
            qualityDropdown.value = QualitySettings.GetQualityLevel();

        if (tutorialToggle != null)
            tutorialToggle.isOn = settings.tutorialEnabled;

        // Add listeners for settings changes
        AddSettingsListeners();
    }

    private void AddSettingsListeners()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        
        if (uiVolumeSlider != null)
            uiVolumeSlider.onValueChanged.AddListener(OnUIVolumeChanged);

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggled);

        if (qualityDropdown != null)
            qualityDropdown.onValueChanged.AddListener(OnQualityChanged);

        if (tutorialToggle != null)
            tutorialToggle.onValueChanged.AddListener(OnTutorialToggled);
    }

    private void UpdatePlayerInfo()
    {
        var saveData = SaveManager.Instance.GetSaveData();
        
        if (playerLevelText != null)
            playerLevelText.text = $"Level {saveData.playerLevel}";
        
        if (experienceBar != null)
        {
            int requiredXP = saveData.playerLevel * 1000; // Simple XP calculation
            experienceBar.fillAmount = (float)saveData.experience / requiredXP;
        }
    }

    #region Panel Management

    private void ShowPanel(GameObject panel)
    {
        if (panel == null) return;

        StartCoroutine(TransitionPanels(panel));
    }

    private void HidePanel(GameObject panel)
    {
        if (panel == null) return;
        panel.SetActive(false);
    }

    private IEnumerator TransitionPanels(GameObject newPanel)
    {
        // Play transition sound
        AudioManager.Instance?.PlaySFX(transitionSound);

        // Fade out
        if (fadeCanvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < transitionDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                fadeCanvasGroup.alpha = elapsed / (transitionDuration * 0.5f);
                yield return null;
            }
        }

        // Deactivate all panels
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (animalCollectionPanel != null) animalCollectionPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);

        // Activate new panel
        newPanel.SetActive(true);

        // Fade in
        if (fadeCanvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < transitionDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                fadeCanvasGroup.alpha = 1f - (elapsed / (transitionDuration * 0.5f));
                yield return null;
            }
        }
    }

    #endregion

    #region Button Event Handlers

    private void OnPlayButtonClicked()
    {
        AudioManager.Instance?.PlaySFX(buttonClickSound);
        StartCoroutine(LoadGameScene());
    }

    private void OnCollectionButtonClicked()
    {
        AudioManager.Instance?.PlaySFX(buttonClickSound);
        ShowPanel(animalCollectionPanel);
        PopulateAnimalCollection();
    }

    private void OnSettingsButtonClicked()
    {
        AudioManager.Instance?.PlaySFX(buttonClickSound);
        ShowPanel(settingsPanel);
    }

    private void OnCreditsButtonClicked()
    {
        AudioManager.Instance?.PlaySFX(buttonClickSound);
        ShowPanel(creditsPanel);
    }

    private void OnQuitButtonClicked()
    {
        AudioManager.Instance?.PlaySFX(buttonClickSound);
        StartCoroutine(QuitGame());
    }

    private void OnButtonHover()
    {
        AudioManager.Instance?.PlaySFX(buttonHoverSound);
    }

    #endregion

    #region Settings Event Handlers

    private void OnMasterVolumeChanged(float value)
    {
        AudioManager.Instance?.SetVolume("MasterVolume", value);
        SaveSettings();
    }

    private void OnMusicVolumeChanged(float value)
    {
        AudioManager.Instance?.SetVolume("MusicVolume", value);
        SaveSettings();
    }

    private void OnSFXVolumeChanged(float value)
    {
        AudioManager.Instance?.SetVolume("SFXVolume", value);
        SaveSettings();
    }

    private void OnUIVolumeChanged(float value)
    {
        AudioManager.Instance?.SetVolume("UIVolume", value);
        SaveSettings();
    }

    private void OnFullscreenToggled(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        SaveSettings();
    }

    private void OnQualityChanged(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        SaveSettings();
    }

    private void OnTutorialToggled(bool enabled)
    {
        var settings = SaveManager.Instance.GetSettings();
        settings.tutorialEnabled = enabled;
        SaveManager.Instance.UpdateSettings(settings);
    }

    private void OnResetSettingsClicked()
    {
        SaveManager.Instance.ResetSettings();
        LoadSettings();
    }

    private void SaveSettings()
    {
        var settings = new SaveManager.GameSettings
        {
            masterVolume = masterVolumeSlider.value,
            musicVolume = musicVolumeSlider.value,
            sfxVolume = sfxVolumeSlider.value,
            uiVolume = uiVolumeSlider.value,
            tutorialEnabled = tutorialToggle.isOn,
            qualityLevel = qualityDropdown.value
        };

        SaveManager.Instance.UpdateSettings(settings);
    }

    #endregion

    #region Collection Management

    private void PopulateAnimalCollection()
    {
        if (animalGridContainer == null || animalCardPrefab == null) return;

        // Clear existing cards
        foreach (Transform child in animalGridContainer)
        {
            Destroy(child.gameObject);
        }

        // Get unlocked animals from save data
        var saveData = SaveManager.Instance.GetSaveData();
        
        // Update progress text
        if (collectionProgressText != null)
        {
            int totalAnimals = 20; // Total number of animals in the game
            float percentage = (float)saveData.unlockedAnimals.Count / totalAnimals * 100f;
            collectionProgressText.text = $"Collection Progress: {percentage:F1}%";
        }

        // Create cards for each unlocked animal
        foreach (var animal in saveData.unlockedAnimals)
        {
            GameObject cardObj = Instantiate(animalCardPrefab, animalGridContainer);
            AnimalCard card = cardObj.GetComponent<AnimalCard>();
            if (card != null)
            {
                // Find animal data and initialize card
                AnimalData animalData = Resources.Load<AnimalData>($"Animals/{animal.animalName}");
                if (animalData != null)
                {
                    card.Initialize(animalData);
                }
            }
        }
    }

    #endregion

    #region Scene Management

    private IEnumerator LoadGameScene()
    {
        loadingPanel.SetActive(true);

        // Start loading the game scene asynchronously
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("GameScene");
        asyncLoad.allowSceneActivation = false;

        // Wait until the scene is loaded
        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        // Fade out
        if (fadeCanvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                fadeCanvasGroup.alpha = elapsed / transitionDuration;
                yield return null;
            }
        }

        // Activate the scene
        asyncLoad.allowSceneActivation = true;
    }

    private IEnumerator QuitGame()
    {
        // Fade out
        if (fadeCanvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                fadeCanvasGroup.alpha = elapsed / transitionDuration;
                yield return null;
            }
        }

        // Quit the game
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    #endregion
}
