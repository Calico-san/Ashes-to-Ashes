using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UniversalPopup : MonoBehaviour
{
    private const float PopupMinHeight       = 300f;
    private const float ContentWidth         = 430f;
    private const float TitleMinHeight       = 32f;
    private const float MessageMinHeight     = 72f;
    private const float ExplanationMinHeight = 34f;
    private const float ButtonWidth          = 260f;
    private const float ButtonHeight         = 32f;
    private const float VerticalPadding      = 24f;
    private const float SectionSpacing       = 10f;
    private const float ButtonSpacing        = 6f;

    [Header("UI Elements")]
    [SerializeField] private GameObject panelObject;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button closeButton;

    public static UniversalPopup Instance { get; private set; }
    public static event Action<GameEventData> EventClosed;
    public bool IsOpen => panelObject.activeSelf;

    private readonly List<Button> optionButtons = new();
    private TextMeshProUGUI explanationText;
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
        CreateExplanationText();
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
            optionLabel.fontSize = 16f;
            bool canChoose = option.MeetsRequirements(gameController);
            optionButton.interactable = canChoose;
            optionLabel.color = canChoose
                ? Color.white
                : new Color(0.68f, 0.68f, 0.68f, 1f);

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

        messageText.fontSize = 16f;
        titleText.fontSize = 23f;
        titleText.fontStyle = FontStyles.Bold;
        LayoutPopup(optionCount);
    }

    /// <summary>
    /// Slaze sadrzaj od vrha prema dnu prema stvarnoj visini poruke i najduljeg
    /// explanationa. Prostor za explanation je uvijek rezerviran, pa panel ne
    /// poskakuje kada igrac prijede misem preko opcije.
    /// </summary>
    private void LayoutPopup(int optionCount)
    {
        float titleHeight = Mathf.Max(TitleMinHeight,
            titleText.GetPreferredValues(titleText.text, ContentWidth, 0f).y + 2f);
        float messageHeight = Mathf.Max(MessageMinHeight,
            messageText.GetPreferredValues(messageText.text, ContentWidth, 0f).y + 4f);

        float explanationHeight = ExplanationMinHeight;
        for (int i = 0; i < optionCount; i++)
        {
            string explanation = currentEvent.Options[i].Explanation;
            if (string.IsNullOrWhiteSpace(explanation)) continue;

            explanationHeight = Mathf.Max(explanationHeight,
                explanationText.GetPreferredValues(explanation, ContentWidth, 0f).y + 4f);
        }

        float buttonsHeight = optionCount * ButtonHeight
                            + Mathf.Max(0, optionCount - 1) * ButtonSpacing;
        float contentHeight = titleHeight + messageHeight + explanationHeight + buttonsHeight
                            + SectionSpacing * 3f;
        float panelHeight = Mathf.Max(PopupMinHeight, contentHeight + VerticalPadding * 2f);

        RectTransform panelTransform = panelObject.GetComponent<RectTransform>();
        panelTransform.sizeDelta = new Vector2(panelTransform.sizeDelta.x, panelHeight);

        float y = panelHeight * 0.5f - VerticalPadding;
        Place(titleText.rectTransform, ContentWidth, titleHeight, ref y, SectionSpacing);
        Place(messageText.rectTransform, ContentWidth, messageHeight, ref y, SectionSpacing);
        Place(explanationText.rectTransform, ContentWidth, explanationHeight, ref y, SectionSpacing);

        for (int i = 0; i < optionCount; i++)
        {
            RectTransform buttonTransform = optionButtons[i].GetComponent<RectTransform>();
            Place(buttonTransform, ButtonWidth, ButtonHeight, ref y,
                i + 1 < optionCount ? ButtonSpacing : 0f);
        }
    }

    private static void Place(RectTransform element, float width, float height,
        ref float y, float spacingAfter)
    {
        element.sizeDelta = new Vector2(width, height);
        element.anchoredPosition = new Vector2(0f, y - height * 0.5f);
        y -= height + spacingAfter;
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
        explanationText.gameObject.SetActive(true);
    }

    public void HideExplanation()
    {
        if (explanationText != null)
        {
            explanationText.gameObject.SetActive(false);
        }
    }

    private void CreateExplanationText()
    {
        GameObject explanationObject = new GameObject("Option Explanation", typeof(RectTransform));
        explanationObject.transform.SetParent(panelObject.transform, false);

        RectTransform explanationTransform = explanationObject.GetComponent<RectTransform>();
        explanationTransform.anchorMin = new Vector2(0.5f, 0.5f);
        explanationTransform.anchorMax = new Vector2(0.5f, 0.5f);
        explanationTransform.anchoredPosition = new Vector2(0f, -5f);
        explanationTransform.sizeDelta = new Vector2(430f, 34f);

        explanationText = explanationObject.AddComponent<TextMeshProUGUI>();
        explanationText.font = messageText.font;
        explanationText.fontSize = 13f;
        explanationText.fontStyle = FontStyles.Italic;
        explanationText.color = Color.white;
        explanationText.alignment = TextAlignmentOptions.Center;
        explanationText.textWrappingMode = TextWrappingModes.Normal;
    }

}
