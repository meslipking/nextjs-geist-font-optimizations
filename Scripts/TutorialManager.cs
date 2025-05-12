using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject tutorialPanel;
    public TextMeshProUGUI tutorialText;
    public Image highlightImage;
    public Button nextButton;
    public Button skipButton;
    public GameObject overlay;

    [Header("Tutorial Settings")]
    public float typingSpeed = 0.05f;
    public float highlightFadeDuration = 0.3f;
    public Color highlightColor = new Color(1f, 1f, 0f, 0.3f);

    [System.Serializable]
    public class TutorialStep
    {
        public string stepId;
        public string message;
        public RectTransform highlightTarget;
        public bool requiresAction;
        public string requiredActionId;
        public AudioClip voiceOver;
        [TextArea(3, 10)]
        public string additionalInfo;
    }

    [System.Serializable]
    public class Tutorial
    {
        public string tutorialId;
        public string tutorialName;
        public List<TutorialStep> steps;
        public bool isRequired = true;
        public string[] prerequisites;
    }

    public List<Tutorial> tutorials = new List<Tutorial>();

    private Tutorial currentTutorial;
    private int currentStepIndex = -1;
    private bool isTutorialActive = false;
    private Coroutine typingCoroutine;
    private Dictionary<string, bool> completedActions = new Dictionary<string, bool>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeTutorial();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeTutorial()
    {
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
        
        if (overlay != null)
            overlay.SetActive(false);

        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextButtonClicked);

        if (skipButton != null)
            skipButton.onClick.AddListener(SkipTutorial);
    }

    #region Tutorial Control

    public void StartTutorial(string tutorialId)
    {
        Tutorial tutorial = tutorials.Find(t => t.tutorialId == tutorialId);
        
        if (tutorial == null)
        {
            Debug.LogError($"Tutorial {tutorialId} not found!");
            return;
        }

        // Check prerequisites
        if (tutorial.prerequisites != null)
        {
            foreach (string prereq in tutorial.prerequisites)
            {
                if (!SaveManager.Instance.IsTutorialCompleted(prereq))
                {
                    Debug.LogWarning($"Prerequisites not met for tutorial {tutorialId}");
                    return;
                }
            }
        }

        // Check if tutorial is already completed
        if (!tutorial.isRequired && SaveManager.Instance.IsTutorialCompleted(tutorialId))
        {
            return;
        }

        currentTutorial = tutorial;
        currentStepIndex = -1;
        isTutorialActive = true;
        completedActions.Clear();

        ShowTutorialUI();
        NextStep();
    }

    private void ShowTutorialUI()
    {
        if (tutorialPanel != null)
            tutorialPanel.SetActive(true);
        
        if (overlay != null)
            overlay.SetActive(true);
    }

    private void HideTutorialUI()
    {
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
        
        if (overlay != null)
            overlay.SetActive(false);

        if (highlightImage != null)
            highlightImage.gameObject.SetActive(false);
    }

    public void NextStep()
    {
        if (!isTutorialActive || currentTutorial == null) return;

        currentStepIndex++;

        if (currentStepIndex >= currentTutorial.steps.Count)
        {
            CompleteTutorial();
            return;
        }

        PresentStep(currentTutorial.steps[currentStepIndex]);
    }

    private void PresentStep(TutorialStep step)
    {
        // Stop any existing typing animation
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        // Start typing animation for new text
        if (tutorialText != null)
            typingCoroutine = StartCoroutine(TypeText(step.message));

        // Update highlight
        UpdateHighlight(step.highlightTarget);

        // Play voice over if available
        if (step.voiceOver != null)
            AudioManager.Instance?.PlaySFX(step.voiceOver);

        // Update button state
        if (nextButton != null)
            nextButton.interactable = !step.requiresAction;
    }

    private IEnumerator TypeText(string message)
    {
        if (tutorialText == null) yield break;

        tutorialText.text = "";
        foreach (char c in message)
        {
            tutorialText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    private void UpdateHighlight(RectTransform target)
    {
        if (highlightImage == null || target == null)
        {
            if (highlightImage != null)
                highlightImage.gameObject.SetActive(false);
            return;
        }

        highlightImage.gameObject.SetActive(true);
        highlightImage.rectTransform.position = target.position;
        highlightImage.rectTransform.sizeDelta = target.sizeDelta;

        // Animate highlight
        StartCoroutine(PulseHighlight());
    }

    private IEnumerator PulseHighlight()
    {
        if (highlightImage == null) yield break;

        Color startColor = highlightColor;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        while (isTutorialActive)
        {
            // Fade out
            float elapsed = 0f;
            while (elapsed < highlightFadeDuration)
            {
                elapsed += Time.deltaTime;
                highlightImage.color = Color.Lerp(startColor, endColor, elapsed / highlightFadeDuration);
                yield return null;
            }

            // Fade in
            elapsed = 0f;
            while (elapsed < highlightFadeDuration)
            {
                elapsed += Time.deltaTime;
                highlightImage.color = Color.Lerp(endColor, startColor, elapsed / highlightFadeDuration);
                yield return null;
            }
        }
    }

    public void CompleteAction(string actionId)
    {
        completedActions[actionId] = true;

        // Check if current step was waiting for this action
        if (currentTutorial != null && 
            currentStepIndex < currentTutorial.steps.Count && 
            currentTutorial.steps[currentStepIndex].requiresAction &&
            currentTutorial.steps[currentStepIndex].requiredActionId == actionId)
        {
            NextStep();
        }
    }

    private void CompleteTutorial()
    {
        if (currentTutorial == null) return;

        SaveManager.Instance.CompleteTutorial(currentTutorial.tutorialId);
        
        isTutorialActive = false;
        HideTutorialUI();
        
        // Notify game manager that tutorial is complete
        GameManager.Instance.OnTutorialCompleted(currentTutorial.tutorialId);
        
        currentTutorial = null;
        currentStepIndex = -1;
    }

    public void SkipTutorial()
    {
        if (currentTutorial != null && !currentTutorial.isRequired)
        {
            CompleteTutorial();
        }
    }

    #endregion

    #region Event Handlers

    private void OnNextButtonClicked()
    {
        if (isTutorialActive)
        {
            // If text is still typing, complete it instantly
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                if (tutorialText != null && currentTutorial != null && currentStepIndex < currentTutorial.steps.Count)
                {
                    tutorialText.text = currentTutorial.steps[currentStepIndex].message;
                }
            }
            else
            {
                NextStep();
            }
        }
    }

    #endregion

    #region Utility Methods

    public bool IsTutorialActive()
    {
        return isTutorialActive;
    }

    public bool IsActionRequired()
    {
        if (!isTutorialActive || currentTutorial == null || 
            currentStepIndex >= currentTutorial.steps.Count)
            return false;

        return currentTutorial.steps[currentStepIndex].requiresAction;
    }

    public string GetCurrentTutorialId()
    {
        return currentTutorial?.tutorialId;
    }

    public void ResetTutorialProgress()
    {
        foreach (var tutorial in tutorials)
        {
            SaveManager.Instance.CompleteTutorial(tutorial.tutorialId);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #endregion
}
