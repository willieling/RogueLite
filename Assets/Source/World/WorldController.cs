using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.U2D;

struct GridInfo
{
    public Vector2Int GridDimensions;
    public Vector2 TileSize;
}

/**
 * This class moves the world/map and generates the tiles as needed.
 * This is done because as you move further from the world's origin,
 * the less precise floating point becomes.
 * 
 * We want to keep the player at the center of the world.
 * 
 * We're also going to define some constants for the camera and sprites.
 */

public class WorldController : MonoBehaviour
{
    // We're going to force 1080@60 for now.
    // hopefully I add in a settings screen later
    const int PPU = 8;
    const float ASPECT_RATIO = 16f / 9;
    const int HEIGHT = 1920;
    const int WIDTH = 1080;
    const FullScreenMode SCREEN_MODE = FullScreenMode.ExclusiveFullScreen;
    const int REFRESH_RATE = 60;

    [SerializeField]
    private float _speed = 0.05f;
    [SerializeField]
    private WorldTilesScriptableObject _tilesData = null;

    private Pool<WorldTile> _mapTilePool;
    private List<WorldTile> _mapTiles;

    private BoxCollider2D _leftCollider;
    private BoxCollider2D _rightCollider;
    private BoxCollider2D _upCollider;
    private BoxCollider2D _downCollider;

    private GridInfo _gridInfo;
    // When we move more than tile, we want to move everything that's offscreen to the opposite side.
    // We do this to maintain the illusion that the player is moving in the world.
    private int _internalTilePosition;

    // Start is called before the first frame update
    void Awake()
    {
        this.transform.position.Set(this.transform.position.x, this.transform.position.y, _tilesData.TileDepth);

        Camera camera = Camera.main;
        camera.aspect = ASPECT_RATIO;

        PixelPerfectCamera ppCam = camera.GetComponent<PixelPerfectCamera>();
        ppCam.assetsPPU = PPU;
        ppCam.refResolutionX = HEIGHT;
        ppCam.refResolutionY = WIDTH;

        Screen.SetResolution(HEIGHT, WIDTH, SCREEN_MODE, REFRESH_RATE);

        GridInfo gridInfo = FillFrustrumWithWorldTiles(camera);

        /*
         * We need to setup colliders encasing the grid
         * if a tile overlaps with a collider, it gets moved to the other side
         */
        CreateSideColliders(gridInfo);
    }

    // Update is called once per frame
    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        foreach (WorldTile tile in _mapTiles)
        {
            tile.transform.Translate(horizontal * _speed, vertical * _speed, 0);
        }
    }

    // Cover the entire frustum with world tiles and return the size of the grid (in world tiles)
    private GridInfo FillFrustrumWithWorldTiles(Camera camera)
    {
        const int SIDE_BUFFER = 2;

        PixelPerfectCamera ppCam = camera.GetComponent<PixelPerfectCamera>();

        int height = ppCam.refResolutionY;
        int width = ppCam.refResolutionX;

        Vector2 tileSize = CalculateTileSizeInUnits();

        Vector2Int tileGridDimension = new Vector2Int(Mathf.CeilToInt(width / (tileSize.x * PPU)), Mathf.CeilToInt(height / (tileSize.y * PPU)));
        tileGridDimension.x += SIDE_BUFFER;
        tileGridDimension.y += SIDE_BUFFER;

        _mapTilePool = new Pool<WorldTile>("WorldTile", _tilesData.WorldTilePrefab, new Vector3(0, 0, -100), tileGridDimension.x * tileGridDimension.y);
        _mapTiles = new List<WorldTile>(tileGridDimension.x * tileGridDimension.y);

        // We want the center of the grid to be at the world origin
        Vector3 halfGridSize = new Vector3(
        ((tileGridDimension.x - 1) * tileSize.x) / 2
        , ((tileGridDimension.y - 1) * tileSize.y) / 2
        , -10);

        /*
         * We're going to spawn all the tiles in a grid and then move the camera into the middle of the grid
         */
        for (int i = 0; i < tileGridDimension.x; ++i)
        {
            for (int j = 0; j < tileGridDimension.y; ++j)
            {
                WorldTile tile = _mapTilePool.Get();
                tile.transform.SetParent(this.transform);

                Vector3 position = new Vector3(i * tileSize.x, j * tileSize.y);
                position -= halfGridSize;
                tile.transform.SetPositionAndRotation(position, Quaternion.identity);

                _mapTiles.Add(tile);
            }
        }

        camera.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        return new GridInfo()
        {
            GridDimensions = tileGridDimension,
            TileSize = tileSize,
        };
    }

    // Calculate the tile size in world units (not pixels)
    private Vector2 CalculateTileSizeInUnits()
    {
        Camera cam = Camera.main;

        // Spawn a tile and determine the size of it
        WorldTile worldTile = Instantiate(_tilesData.WorldTilePrefab, new Vector3(0, 0, _tilesData.TileDepth), Quaternion.identity, this.transform);
        SpriteRenderer spriteRenderer = worldTile.GetComponent<SpriteRenderer>();

        Vector3 min = spriteRenderer.bounds.min;
        Vector3 max = spriteRenderer.bounds.max;

        Vector3 screenMin = cam.WorldToScreenPoint(min);
        Vector3 screenMax = cam.WorldToScreenPoint(max);

        Destroy(spriteRenderer.gameObject);

        return new Vector2((screenMax.x - screenMin.x) / PPU, (screenMax.y - screenMin.y) / PPU);
    }

    private void CreateSideColliders(GridInfo gridInfo)
    {
        const int COLLIDER_THICKNESS = 10;

        //use half or full width?
        Vector2 halfWidth = (gridInfo.TileSize * gridInfo.GridDimensions) / 2;

        Vector2 verticalSize = new Vector2(COLLIDER_THICKNESS, halfWidth.y * 2);
        Vector2 horizontalSize = new Vector2(halfWidth.x * 2, COLLIDER_THICKNESS);

        float absoluteHorizontalPosition = halfWidth.x + gridInfo.TileSize.x;
        float absoluteVerticalPosition = halfWidth.y + gridInfo.TileSize.y;

        AddCollider(out _leftCollider, verticalSize, new Vector2(-absoluteHorizontalPosition, 0));
        AddCollider(out _rightCollider, verticalSize, new Vector2(absoluteHorizontalPosition, 0));
        AddCollider(out _upCollider, horizontalSize, new Vector2(0, -absoluteVerticalPosition));
        AddCollider(out _downCollider, horizontalSize, new Vector2(0, absoluteVerticalPosition));
    }

    //inline?
    private void AddCollider(out BoxCollider2D collider, Vector2 size, Vector2 offset)
    {
        collider = this.transform.AddComponent<BoxCollider2D>();
        collider.size = size;
        collider.offset = offset;
    }
}
