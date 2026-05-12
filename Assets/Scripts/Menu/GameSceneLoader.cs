using UnityEngine;

/// <summary>
/// Attach to a persistent GameObject in the Game scene.
/// If the player clicked "Continue" in Main Menu, this loads
/// the save file after the scene and Bootstrapper have initialized.
///
/// Uses a static flag set by MainMenuController before scene load.
/// </summary>
public class GameSceneLoader : MonoBehaviour
{
    public static bool ShouldLoadSave { get; set; } = false;
    public static int  LoadSlot       { get; set; } = 0;

    private void Start()
    {
        if (!ShouldLoadSave) return;
        ShouldLoadSave = false;

        var game = FindFirstObjectByType<GameController>();
        if (game == null)
        {
            Debug.LogWarning("[GameSceneLoader] PrototypeGameController not found.");
            return;
        }

        bool ok = game.LoadGame(LoadSlot);
        Debug.Log(ok
            ? $"[GameSceneLoader] Save loaded from slot {LoadSlot}."
            : $"[GameSceneLoader] Failed to load slot {LoadSlot}.");
    }
}
