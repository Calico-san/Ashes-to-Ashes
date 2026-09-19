using UnityEngine;

/// <summary>
/// Prenosi namjeru "Continue" kroz ucitavanje scene.
///
/// VAZNO: ova komponenta NE postoji ni u jednoj sceni — ni AshesToAshes ni
/// MainMenu je nemaju. Zbog toga se Start() nikad nije izvrsio i Continue je
/// uvijek pokretao novu igru iako je zastavica bila postavljena.
/// Zato je ucitavanje premjesteno u staticnu ApplyPendingLoad(), koju zove
/// PrototypeBootstrapper.Start() — on u sceni sigurno postoji.
/// Komponenta je zadrzana za slucaj da je netko ipak stavi u scenu.
/// </summary>
public class GameSceneLoader : MonoBehaviour
{
    public static bool ShouldLoadSave { get; set; } = false;
    public static int  LoadSlot       { get; set; } = 0;

    private void Start() => ApplyPendingLoad();

    /// <summary>
    /// Ucita spremljenu igru ako je igrac dosao preko "Continue". Sigurno je
    /// zvati vise puta — zastavica se gasi pri prvom uspjesnom pozivu.
    /// </summary>
    public static void ApplyPendingLoad()
    {
        if (!ShouldLoadSave) return;
        ShouldLoadSave = false;

        var game = Object.FindFirstObjectByType<GameController>();
        if (game == null)
        {
            Debug.LogWarning("[GameSceneLoader] GameController nije pronaden — Continue nije ucitao igru.");
            return;
        }

        bool ok = game.LoadGame(LoadSlot);
        Debug.Log(ok
            ? $"[GameSceneLoader] Ucitan slot {LoadSlot}."
            : $"[GameSceneLoader] Neuspjelo ucitavanje slota {LoadSlot}.");
    }
}
