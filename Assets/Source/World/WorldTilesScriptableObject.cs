using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class WorldTilesScriptableObject : ScriptableObject
{
    [MenuItem("Assets/Create/World Tiles Scriptable Object")]
    public static void CreateMyAsset()
    {
        WorldTilesScriptableObject asset = ScriptableObject.CreateInstance<WorldTilesScriptableObject>();

        AssetDatabase.CreateAsset(asset, "Assets/Data/WorldTiles/NewScripableObject.asset");
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();

        Selection.activeObject = asset;
    }

    [SerializeField]
    private WorldTile _worldTilePrefab = null;
    [SerializeField]
    private List<Texture> _tiles = new List<Texture>();
    // The Z value of the position of the tile
    [SerializeField]
    private int _tileDepth;

    public WorldTile WorldTilePrefab { get { return _worldTilePrefab; } }
    public List<Texture> Tiles { get { return _tiles; } }
    public int TileDepth { get { return _tileDepth;} }
}
