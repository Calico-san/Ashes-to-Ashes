using UnityEngine;
using TMPro;

public class UpgradeManager : MonoBehaviour
{
    // Reference to upgrade cost variable
    public int woodCost = BalanceConfig.UpgradeWoodCost;
    public int steelCost = BalanceConfig.UpgradeSteelCost;
    public int clothCost = BalanceConfig.UpgradeClothCost;
    public float bonus = BalanceConfig.UpgradeProductionBonus;

    // Reference to the UI Text element
    [SerializeField] private TMP_Text costText; 

    private void Start()
    {
        UpdateCostUI();
    }

    public void UpdateCostUI()
    {
        // Concatenate the text string with the upgrade cost variable
        costText.text = $"Upgrade Cost: {woodCost} wood, {steelCost} steel, {clothCost} cloth\nBonus: {bonus:F2}";
    }
}