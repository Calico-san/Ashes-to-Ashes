using UnityEngine;

public static class SimpleShapeFactory
{
    public static Sprite CreateFilledSquareSprite(Color color, int size = 64)
        => CreateFilledSquareSprite(color, new Vector2(0.5f, 0.5f), size);

    /// <summary>
    /// Kvadrat sa zadanim pivotom. Pivot (0, 0.5) koristi ispuna trake napretka:
    /// skaliranjem po x tada raste SAMO udesno od svoje pozicije. S pivotom
    /// (0.5, 0.5) rasla je na obje strane, pa je preko pozadine bila vidljiva
    /// samo desna polovica — traka je prikazivala pola stvarnog postotka.
    /// </summary>
    public static Sprite CreateFilledSquareSprite(Color color, Vector2 pivot, int size = 64)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), pivot, size);
    }

    public static Sprite CreateFilledTriangleSprite(Color color, int size = 64)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var clear = new Color(0f, 0f, 0f, 0f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                texture.SetPixel(x, y, clear);
            }
        }

        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            float t = y / (float)(size - 1);
            int minX = Mathf.RoundToInt(half - half * t);
            int maxX = Mathf.RoundToInt(half + half * t);

            for (int x = minX; x <= maxX; x++)
            {
                if (x >= 0 && x < size)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.1f), size);
    }
}
