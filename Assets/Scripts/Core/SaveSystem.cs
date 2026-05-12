using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Handles reading and writing save files as JSON on disk.
///
/// Save location: Application.persistentDataPath/saves/
/// File format:   save_{slot}.json  (slot 0, 1, 2 ...)
///
/// SRP: only file I/O and JSON serialization.
///      Does not know about game logic — callers provide GameStateData.
/// </summary>
public static class SaveSystem
{
    private const string SaveFolder  = "saves";
    private const string FilePrefix  = "save_";
    private const string FileExt     = ".json";
    public  const int    MaxSlots    = 3;

    // ---- Paths ----

    private static string SaveDirectory =>
        Path.Combine(Application.persistentDataPath, SaveFolder);

    private static string SlotPath(int slot) =>
        Path.Combine(SaveDirectory, $"{FilePrefix}{slot}{FileExt}");

    // ---- Save ----

    /// <summary>
    /// Serialize state to JSON and write to slot file.
    /// Returns true on success.
    /// </summary>
    public static bool Save(GameStateData state, int slot = 0)
    {
        if (!ValidateSlot(slot)) return false;

        try
        {
            Directory.CreateDirectory(SaveDirectory);
            state.SaveDateTime = DateTime.UtcNow.ToString("o");
            string json = JsonUtility.ToJson(state, prettyPrint: true);
            File.WriteAllText(SlotPath(slot), json);
            Debug.Log($"[SaveSystem] Saved to slot {slot}: {SlotPath(slot)}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Save failed (slot {slot}): {e.Message}");
            return false;
        }
    }

    // ---- Load ----

    /// <summary>
    /// Read and deserialize save file from slot.
    /// Returns null if file does not exist or is corrupt.
    /// </summary>
    public static GameStateData Load(int slot = 0)
    {
        if (!ValidateSlot(slot)) return null;

        string path = SlotPath(slot);
        if (!File.Exists(path))
        {
            Debug.Log($"[SaveSystem] No save file at slot {slot}.");
            return null;
        }

        try
        {
            string json  = File.ReadAllText(path);
            var    state = JsonUtility.FromJson<GameStateData>(json);
            Debug.Log($"[SaveSystem] Loaded from slot {slot} (saved {state.SaveDateTime}).");
            return state;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Load failed (slot {slot}): {e.Message}");
            return null;
        }
    }

    // ---- Delete ----

    public static bool Delete(int slot = 0)
    {
        if (!ValidateSlot(slot)) return false;
        string path = SlotPath(slot);
        if (!File.Exists(path)) return false;

        try
        {
            File.Delete(path);
            Debug.Log($"[SaveSystem] Deleted slot {slot}.");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Delete failed (slot {slot}): {e.Message}");
            return false;
        }
    }

    // ---- Query ----

    /// <summary>True if a save file exists for the given slot.</summary>
    /// <summary>Returns slot index of most recently saved game, or -1 if none exist.</summary>
    public static int GetLatestSlot()
    {
        int    bestSlot = -1;
        System.DateTime bestTime = System.DateTime.MinValue;
        for (int i = 0; i < MaxSlots; i++)
        {
            var info = GetSlotInfo(i);
            if (info == null) continue;
            if (System.DateTime.TryParse(info.DateTime, out var dt) && dt > bestTime)
            {
                bestTime = dt;
                bestSlot = i;
            }
        }
        return bestSlot;
    }

    public static bool SlotExists(int slot) =>
        ValidateSlot(slot) && File.Exists(SlotPath(slot));

    /// <summary>
    /// Returns save metadata (date, day) without fully loading the state.
    /// Returns null if slot is empty or corrupt.
    /// </summary>
    public static SaveSlotInfo GetSlotInfo(int slot)
    {
        if (!SlotExists(slot)) return null;

        try
        {
            string json  = File.ReadAllText(SlotPath(slot));
            var    state = JsonUtility.FromJson<GameStateData>(json);
            return new SaveSlotInfo
            {
                Slot     = slot,
                DateTime = state.SaveDateTime,
                Day      = state.Day,
                Exists   = true,
            };
        }
        catch
        {
            return null;
        }
    }

    // ---- Helpers ----

    private static bool ValidateSlot(int slot)
    {
        if (slot >= 0 && slot < MaxSlots) return true;
        Debug.LogWarning($"[SaveSystem] Invalid slot {slot}. Must be 0–{MaxSlots - 1}.");
        return false;
    }
}

// -------------------------------------------------------

/// <summary>Lightweight save slot metadata for UI display.</summary>
[System.Serializable]
public class SaveSlotInfo
{
    public int    Slot;
    public string DateTime;
    public int    Day;
    public bool   Exists;

    public string DisplayString =>
        Exists ? $"Slot {Slot + 1}  —  Day {Day}  ({FormatDate()})" : $"Slot {Slot + 1}  —  Empty";

    private string FormatDate()
    {
        if (System.DateTime.TryParse(DateTime, out var dt))
            return dt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
        return DateTime;
    }
}
