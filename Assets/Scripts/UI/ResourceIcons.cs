/// <summary>
/// Inline ikone resursa za TextMeshPro.
///
/// Ikona nije zasebna Image komponenta nego znak unutar teksta. TMP oznaku
/// &lt;sprite name="wood"&gt; zamjenjuje sličicom iz Default Sprite Asseta, pa se
/// ikona sama poravnava s tekstom i skalira s veličinom fonta. Time gornja
/// traka ostaje JEDNO tekstualno polje umjesto niza naizmjeničnih ikona i
/// brojki, a svako buduće mjesto koje ispisuje resurs dobije ikonu jednim
/// pozivom.
///
/// Imena u oznakama moraju se poklapati s imenima spriteova u sheetu:
///   wood, steel, cloth, rope, ships, food, rawfood
///
/// Namjerno BEZ tint=1 — ikone zadrzavaju vlastite boje. S tintom bi poprimile
/// boju teksta i pixel art bi izgubio paletu.
/// </summary>
public static class ResourceIcons
{
    public const string Wood    = "<sprite name=\"wood\">";
    public const string Steel   = "<sprite name=\"steel\">";
    public const string Cloth   = "<sprite name=\"cloth\">";
    public const string Rope    = "<sprite name=\"rope\">";
    public const string Ships   = "<sprite name=\"ships\">";
    public const string Food    = "<sprite name=\"food\">";
    public const string RawFood = "<sprite name=\"rawfood\">";

    /// <summary>Oznaka ikone za tip resursa.</summary>
    public static string Tag(ResourceType type)
    {
        switch (type)
        {
            case ResourceType.Wood:  return Wood;
            case ResourceType.Steel: return Steel;
            case ResourceType.Cloth: return Cloth;
            case ResourceType.Rope:  return Rope;
            case ResourceType.Ships: return Ships;
            case ResourceType.Food:  return Food;
            default:                 return "";
        }
    }

    /// <summary>Ikona pa iznos, npr. "&lt;sprite name="wood"&gt; 560".</summary>
    public static string Line(ResourceType type, int amount)
        => $"{Tag(type)} {amount}";

    /// <summary>Ikona pa iznos za zadanu oznaku, kad tip nije ResourceType.</summary>
    public static string Line(string tag, int amount)
        => $"{tag} {amount}";
}
