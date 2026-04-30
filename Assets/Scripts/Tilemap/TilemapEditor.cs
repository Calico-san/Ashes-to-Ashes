#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom Inspector for TilemapData.
/// Shows a visual grid where Tomislav can click tiles to paint them.
///
/// Usage:
///   1. Select IslandMap.asset in Project window
///   2. Inspector shows the grid + tile type palette
///   3. Click a color in the palette to select it
///   4. Click tiles in the grid to paint
///   5. Ctrl+Z to undo
/// </summary>
[CustomEditor(typeof(TilemapData))]
public class TilemapEditor : Editor
{
    private TileType _paintType = TileType.Land;
    private bool     _painting  = false;

    private static readonly Color[] PaletteColors = new Color[]
    {
        new Color(0.09f, 0.28f, 0.52f),  // Ocean
        new Color(0.72f, 0.62f, 0.38f),  // Shore
        new Color(0.15f, 0.42f, 0.18f),  // Land
        new Color(0.08f, 0.28f, 0.10f),  // Forest
        new Color(0.50f, 0.48f, 0.45f),  // IronMine
        new Color(0.75f, 0.15f, 0.10f),  // Volcano
    };

    public override void OnInspectorGUI()
    {
        var map = (TilemapData)target;

        // Dimensions
        EditorGUILayout.LabelField("Map Settings", EditorStyles.boldLabel);
        int newW = EditorGUILayout.IntField("Width",  map.Width);
        int newH = EditorGUILayout.IntField("Height", map.Height);
        map.TileSize = EditorGUILayout.FloatField("Tile Size", map.TileSize);
        map.Offset   = EditorGUILayout.Vector2Field("Offset",  map.Offset);

        if (newW != map.Width || newH != map.Height)
        {
            Undo.RecordObject(map, "Resize Tilemap");
            map.Width  = Mathf.Max(1, newW);
            map.Height = Mathf.Max(1, newH);
            map.Resize();
            EditorUtility.SetDirty(map);
        }

        EditorGUILayout.Space(8);

        // Palette
        EditorGUILayout.LabelField("Paint Tile Type", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        var names = System.Enum.GetNames(typeof(TileType));
        for (int i = 0; i < names.Length; i++)
        {
            bool selected = (int)_paintType == i;
            var  style    = new GUIStyle(GUI.skin.button);
            style.normal.background = MakeTexture(PaletteColors[i]);
            style.fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
            style.fixedWidth = 72;

            if (GUILayout.Button(names[i], style))
                _paintType = (TileType)i;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);

        // Generate placeholder button
        if (GUILayout.Button("Generate Placeholder Map"))
        {
            Undo.RecordObject(map, "Generate Placeholder");
            map.GeneratePlaceholder();
            EditorUtility.SetDirty(map);
        }

        // Fill all button
        if (GUILayout.Button($"Fill All with: {_paintType}"))
        {
            Undo.RecordObject(map, "Fill Tilemap");
            map.Resize();
            for (int i = 0; i < map.tiles.Length; i++)
                map.tiles[i] = (int)_paintType;
            EditorUtility.SetDirty(map);
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Map Grid (click to paint)", EditorStyles.boldLabel);

        map.Resize();

        // Grid — draw from top row down
        float cellSize = Mathf.Min(12f, (EditorGUIUtility.currentViewWidth - 40f) / map.Width);
        cellSize = Mathf.Max(4f, cellSize);

        var evt = Event.current;

        for (int row = map.Height - 1; row >= 0; row--)
        {
            EditorGUILayout.BeginHorizontal();
            for (int col = 0; col < map.Width; col++)
            {
                TileType t = map.GetTile(col, row);
                var color  = PaletteColors[(int)t];

                var rect = GUILayoutUtility.GetRect(cellSize, cellSize,
                    GUILayout.Width(cellSize), GUILayout.Height(cellSize));

                EditorGUI.DrawRect(rect, color);

                // Paint on click / drag
                if (evt.type == EventType.MouseDown && rect.Contains(evt.mousePosition))
                    _painting = true;
                if (_painting && (evt.type == EventType.MouseDown || evt.type == EventType.MouseDrag)
                    && rect.Contains(evt.mousePosition))
                {
                    if (map.GetTile(col, row) != _paintType)
                    {
                        Undo.RecordObject(map, "Paint Tile");
                        map.SetTile(col, row, _paintType);
                        EditorUtility.SetDirty(map);
                    }
                    evt.Use();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        if (evt.type == EventType.MouseUp) _painting = false;

        // Legend
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Legend:", EditorStyles.miniLabel);
        var legend = new string[]
        {
            "Ocean — more, brodovi, Shipyard",
            "Shore — obala, Shipyard must touch",
            "Land  — kopno, sve zgrade",
            "Forest — suma, -15% produkcija",
            "IronMine — rudnik, samo Steelworks",
            "Volcano — neprolazno",
        };
        foreach (var l in legend)
            EditorGUILayout.LabelField(l, EditorStyles.miniLabel);

        if (GUI.changed) EditorUtility.SetDirty(map);
    }

    private static Texture2D MakeTexture(Color color)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }
}
#endif
