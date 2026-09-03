using UnityEngine;

/// <summary>
/// Uklanja ovisnost o Pixels Per Unit postavci uvoza.
///
/// SpriteRenderer u Simple nacinu crta sprite u velicini (pikseli / PPU), pa
/// bi svaka promjena rezolucije arta pomaknula velicinu objekta u svijetu.
/// Sliced nacin crta sprite u pravokutnik zadan preko sr.size u lokalnim
/// jedinicama, sto znaci da PPU postaje nebitan — Tomislav moze isporuciti
/// 16px ili 64px art bez ijedne promjene u kodu.
///
/// VAZNO: sprite mora imati Mesh Type = Full Rect u import postavkama,
/// inace Unity ignorira Sliced nacin i ispise upozorenje.
/// </summary>
public static class SpriteFit
{
    /// <summary>Sprite popuni tocno <paramref name="size"/> lokalnih jedinica (razvlaci se).</summary>
    public static void Fill(SpriteRenderer sr, Vector2 size)
    {
        if (sr == null || sr.sprite == null) return;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size     = size;
    }

    /// <summary>
    /// Cijeli sprite stane unutar kvadrata stranice <paramref name="box"/> lokalnih
    /// jedinica, uz cuvanje omjera stranica. Sira strana dotakne rub, uza ostavi
    /// prazninu — art nikad ne prelazi zadani okvir.
    /// </summary>
    public static void FitInside(SpriteRenderer sr, float box = 1f)
    {
        if (sr == null || sr.sprite == null) return;
        var rect = sr.sprite.rect;
        if (rect.width <= 0f || rect.height <= 0f) return;

        float scale = box / Mathf.Max(rect.width, rect.height);

        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size     = new Vector2(rect.width * scale, rect.height * scale);
    }

    /// <summary>Vraca renderer u obicni nacin — koristi se za placeholder kvadrate.</summary>
    public static void Reset(SpriteRenderer sr)
    {
        if (sr == null) return;
        sr.drawMode = SpriteDrawMode.Simple;
    }
}
