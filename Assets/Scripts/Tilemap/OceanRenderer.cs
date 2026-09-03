using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Animirani ocean. Parnjak VolcanoRenderera: frameovi i brzina zive u
/// SpriteRegistry (OceanFrames, OceanFps), a ova klasa drzi samo mehaniku.
///
/// Razlika u izvedbi naspram vulkana je posljedica broja objekata. Vulkan je jedan
/// GameObject pa mu VolcanoRenderer u Update() zamjenjuje sprite. Ocean je nekoliko
/// stotina celija u Tilemapu bez ijednog GameObjecta, pa se koristi Unityjev
/// ugradeni GetTileAnimationData — Tilemap sam vrti frameove, bez naseg Update()
/// poziva i bez dodatnih draw callova. Isti pristup preko Update()-a znacio bi ili
/// vracanje GameObjecta po polju, ili RefreshAllTiles() nad svih 1536 polja u svakom
/// frameu.
///
/// Sva polja dijele istu fazu animacije — animationStartTime se ne postavlja, pa
/// Tilemap sve celije vrti u istom taktu i cijela povrsina mora valja jednoliko.
///
/// Instanca se stvara jednom u IslandTilemapRenderer.BuildTiles() i dijeli izmedu
/// svih oceanskih polja.
///
/// SRP: klasa zna samo kako se ocean animira. Ne poznaje kartu ni tijek pokretanja.
/// </summary>
public class OceanRenderer : Tile
{
    [SerializeField] private Sprite[] _frames;
    [SerializeField] private float    _fps = 2f;

    /// <summary>
    /// Stvara animiranu oceansku pločicu iz registryja. Vraca null ako animacija nije
    /// postavljena — pozivatelj tada koristi staticni TileOcean sprite.
    /// </summary>
    public static OceanRenderer Create(SpriteRegistry registry)
    {
        if (registry == null || !registry.HasOceanAnimation) return null;

        var tile = ScriptableObject.CreateInstance<OceanRenderer>();
        tile._frames = registry.OceanFrames;
        tile._fps    = Mathf.Max(0.01f, registry.OceanFps);

        // Staticki prikaz — Scene view i prvi frame prije nego animacija krene
        tile.sprite       = registry.OceanFrames[0];
        tile.colliderType = ColliderType.None;

        return tile;
    }

    public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap,
                                              ref TileAnimationData data)
    {
        if (_frames == null || _frames.Length < 2) return false;

        data.animatedSprites = _frames;
        data.animationSpeed  = _fps;
        data.animationStartTime = 0f;   // ista faza za sva polja

        return true;
    }
}
