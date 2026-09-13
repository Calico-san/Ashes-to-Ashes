using System.Text;
using TMPro;
using UnityEngine;

public class ObjectivesPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI objectivesText;

    private ObjectiveManager objectiveManager;

    public void Initialize(ObjectiveManager manager)
    {
        if (objectiveManager != null)
            objectiveManager.ObjectivesChanged -= Refresh;

        objectiveManager = manager;
        objectivesText ??= GetComponentInChildren<TextMeshProUGUI>();

        if (objectiveManager != null)
            objectiveManager.ObjectivesChanged += Refresh;

        Refresh();
    }

    private void OnDestroy()
    {
        if (objectiveManager != null)
            objectiveManager.ObjectivesChanged -= Refresh;
    }

    private void Refresh()
    {
        if (objectivesText == null || objectiveManager == null) return;

        if (!objectiveManager.CampaignStarted || objectiveManager.CampaignFinished || objectiveManager.ActivePhase == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        var text = new StringBuilder();
        text.Append(objectiveManager.ActivePhaseTitle.ToUpperInvariant());

        foreach (ObjectiveProgress objective in objectiveManager.ActiveObjectives)
        {
            text.Append("\n\n")
                .Append(objective.IsCompleted ? "[x] " : "[ ] ")
                .Append(objective.Definition.Title)
                .Append(" (")
                .Append(Mathf.Min(objective.CurrentValue, objective.Definition.TargetValue))
                .Append('/')
                .Append(objective.Definition.TargetValue)
                .Append(')');
        }

        objectivesText.text = text.ToString();
    }
}
