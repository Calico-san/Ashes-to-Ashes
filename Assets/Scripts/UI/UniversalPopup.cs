using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UniversalPopup : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject panelObject;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private TextMeshProUGUI explanationText;
    [SerializeField] private Button closeButton;

    public static UniversalPopup Instance { get; private set; }
    public static event Action<GameEventData> EventClosed;
    public bool IsOpen => panelObject.activeSelf;

    private readonly List<Button> optionButtons = new();
    private GameEventData currentEvent;
    private GameController gameController;
    private int speedBeforeEvent;
    private bool timePaused;

    private void Awake()
    {
        if(Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        optionButtons.Add(closeButton);
        panelObject.SetActive(false);
    }

    public void OpenEvent(GameEventData gameEvent)
    {
        ShowPopup(gameEvent, true);
    }

    private void ShowPopup(GameEventData gameEvent, bool pauseTime)
    {
        if (gameEvent == null || gameEvent.Options == null || gameEvent.Options.Length == 0)
        {
            Debug.LogWarning("Event needs at least one option.");
            return;
        }

        currentEvent = gameEvent;
        gameController = FindFirstObjectByType<GameController>();

        titleText.text = gameEvent.Title;
        messageText.text = gameEvent.Message;

        int optionCount = Mathf.Min(gameEvent.Options.Length, 3);
        if (gameEvent.Options.Length > 3)
        {
            Debug.LogWarning("Only the first three event options will be shown.");
        }

        SetupOptions(optionCount);
        HideExplanation();

        panelObject.SetActive(true);

        if (pauseTime && !timePaused && gameController != null)
        {
            speedBeforeEvent = gameController.SpeedMultiplier;
            gameController.SetSpeed(0);
            timePaused = true;
        }

        if (UISoundManager.Instance != null)
        {
            if (gameEvent.PopupSound != null)
                UISoundManager.Instance.PlaySound(gameEvent.PopupSound);
            else
                UISoundManager.Instance.PlayPanelOpen();
        }
    }

    public void ClosePopup()
    {
        GameEventData closedEvent = currentEvent;
        HideExplanation();
        panelObject.SetActive(false);

        if (timePaused && gameController != null)
        {
            gameController.SetSpeed(speedBeforeEvent);
            timePaused = false;
        }

        EventClosed?.Invoke(closedEvent);
    }

    private void SetupOptions(int optionCount)
    {
        if (optionButtons.Count == 0)
        {
            optionButtons.Add(closeButton);
        }

        while (optionButtons.Count < optionCount)
        {
            Button newButton = Instantiate(closeButton, closeButton.transform.parent);
            newButton.gameObject.name = "Event Option " + (optionButtons.Count + 1);
            optionButtons.Add(newButton);
        }

        for (int i = 0; i < optionButtons.Count; i++)
        {
            bool isVisible = i < optionCount;
            Button optionButton = optionButtons[i];
            optionButton.gameObject.SetActive(isVisible);

            if (!isVisible)
            {
                continue;
            }

            GameEventOption option = currentEvent.Options[i];
            TextMeshProUGUI optionLabel = optionButton.GetComponentInChildren<TextMeshProUGUI>();
            optionLabel.text = option.Label;
            bool canChoose = option.MeetsRequirements(gameController);
            optionButton.interactable = canChoose;

            Image buttonImage = optionButton.GetComponent<Image>();
            if (buttonImage != null)
                buttonImage.color = canChoose
                    ? Color.white
                    : new Color(0.68f, 0.68f, 0.68f, 0.9f);

            optionButton.onClick.RemoveAllListeners();
            int optionIndex = i;
            optionButton.onClick.AddListener(() => ChooseOption(optionIndex));

            EventOptionButton hoverButton = optionButton.GetComponent<EventOptionButton>();
            if (hoverButton == null)
            {
                hoverButton = optionButton.gameObject.AddComponent<EventOptionButton>();
            }

            hoverButton.Setup(this, i);
        }

        ReserveExplanationSpace(optionCount);
    }

    private void ReserveExplanationSpace(int optionCount)
    {
        LayoutElement layout = explanationText.GetComponent<LayoutElement>();
        float requiredHeight = layout.minHeight;
        for (int i = 0; i < optionCount; i++)
        {
            string explanation = currentEvent.Options[i].Explanation;
            if (!string.IsNullOrWhiteSpace(explanation))
                requiredHeight = Mathf.Max(requiredHeight,
                    explanationText.GetPreferredValues(explanation, layout.preferredWidth, 0f).y);
        }

        layout.preferredHeight = requiredHeight;
    }

    private void ChooseOption(int optionIndex)
    {
        GameEventOption option = currentEvent.Options[optionIndex];

        if (!option.TryPayRequirements(gameController)) return;

        if (option.ChangesHope && gameController != null)
        {
            gameController.ChangeHope(option.HopeChange);
        }

        option.ApplyRewards(gameController);
        currentEvent.RecordSelectedOption(optionIndex);

        ClosePopup();
    }

    public void ShowExplanation(int optionIndex)
    {
        if (currentEvent == null || optionIndex < 0 || optionIndex >= currentEvent.Options.Length)
        {
            return;
        }

        string explanation = currentEvent.Options[optionIndex].Explanation;
        if (string.IsNullOrWhiteSpace(explanation))
        {
            HideExplanation();
            return;
        }

        explanationText.text = explanation;
    }

    public void HideExplanation()
    {
        if (explanationText != null)
        {
            explanationText.text = string.Empty;
        }
    }
}
