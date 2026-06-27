using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UniversalPopup : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject panelObject;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private Button closeButton;
    public static UniversalPopup Instance { get; private set; }


    private void Awake()
    {
        if(Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        closeButton.onClick.AddListener(ClosePopup);
        panelObject.SetActive(false);
    }

    public void OpenPopup(string title, string message, string buttonLabel = "Continue")
    {
        titleText.text = title;
        messageText.text = message;
        buttonText.text = buttonLabel;

        panelObject.SetActive(true);

        //Time.timeScale = 0f;
    }

    public void ClosePopup()
    {
        panelObject.SetActive(false);
        //Time.timescale = 1f;
    }


    //Za testirat u Inspectoru -> Universal Popup(Script) -> tri tockice -> Test Pop-up

    [ContextMenu("Test Pop-up")]
    public void TestPopupFromInspector()
    {
        OpenPopup("Debug Title", "This is a test message.", "Got It");
    }
}
