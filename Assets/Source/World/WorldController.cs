using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.U2D;

struct GridInfo
{
    // Grid size in tiles
    public Vector2Int GridDimensions;
    // Tiles size in world units
    public Vector2 TileSize;
}

struct TilePositionFrameInfo
{
    // The downleft most tile position
    public Vector3 MinPosition;
    // The up right most tile position
    public Vector3 MaxPosition;

    private readonly List<WorldTile> _mapTiles;

    public TilePositionFrameInfo(List<WorldTile> mapTiles) : this()
    {
        _mapTiles = mapTiles ?? throw new ArgumentNullException(nameof(mapTiles));
    }

    public bool HasUpdatedThisFrame { get; private set; }

    public void Clear()
    {
        HasUpdatedThisFrame = false;
    }

    public void Update()
    {
        if(HasUpdatedThisFrame)
        {
            return;
        }

        HasUpdatedThisFrame = true;

        MinPosition = _mapTiles[0].transform.position;
        MaxPosition = _mapTiles[0].transform.position;
        foreach(WorldTile tile in _mapTiles)
        {
            if(tile.transform.position.x < MinPosition.x
                || tile.transform.position.y < MinPosition.y)
            {
                MinPosition = tile.transform.position;
            }

            if (tile.transform.position.x > MaxPosition.x
                || tile.transform.position.y > MaxPosition.y)
            {
                MaxPosition = tile.transform.position;
            }
        }
    }
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

    public static WorldController Instance { get { return _instance; } }
    private static WorldController _instance;

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
    private TilePositionFrameInfo tileFrameInfo;

    // Start is called before the first frame update
    void Awake()
    {
        _instance = this;

        this.transform.position.Set(this.transform.position.x, this.transform.position.y, _tilesData.TileDepth);

        Camera camera = Camera.main;
        camera.aspect = ASPECT_RATIO;
        // half the camera's width in world units
        camera.orthographicSize = WIDTH / PPU / 2;

        PixelPerfectCamera ppCam = camera.GetComponent<PixelPerfectCamera>();
        ppCam.assetsPPU = PPU;
        ppCam.refResolutionX = HEIGHT;
        ppCam.refResolutionY = WIDTH;

        Screen.SetResolution(HEIGHT, WIDTH, SCREEN_MODE, REFRESH_RATE);

        _gridInfo = FillFrustrumWithWorldTiles(camera);

        /*
         * We need to setup colliders encasing the grid
         * if a tile overlaps with a collider, it gets moved to the other side
         */
        CreateSideColliders();

        tileFrameInfo = new TilePositionFrameInfo(_mapTiles);
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

        tileFrameInfo.Clear();
    }

    public void NotifyWorldTilecollided(WorldTile tile, Collider2D hitCollider)
    {
        tileFrameInfo.Update();

        Vector2 position = new Vector2();

        // check each collider because we may hit more than one
        if (IsSameCollider(hitCollider, _leftCollider))
        {
            position.x = tileFrameInfo.MaxPosition.x + _gridInfo.TileSize.x;
            position.y = tile.transform.position.y;
        }
        else if(IsSameCollider(hitCollider, _rightCollider))
        {
            position.x = tileFrameInfo.MinPosition.x - _gridInfo.TileSize.x;
            position.y = tile.transform.position.y;
        }
        else if(IsSameCollider(hitCollider, _upCollider))
        {
            position.x = tile.transform.position.x;
            position.y = tileFrameInfo.MinPosition.y - _gridInfo.TileSize.y;
        }
        else if(IsSameCollider(hitCollider, _downCollider))
        {
            position.x = tile.transform.position.x;
            position.y = tileFrameInfo.MaxPosition.y + _gridInfo.TileSize.y;
        }

        //it's not the right position
        tile.transform.SetPositionAndRotation(position, Quaternion.identity);
    }

    private bool IsSameCollider(Collider2D collider, BoxCollider2D other)
    {
        return collider is BoxCollider2D boxCollider
            && boxCollider.size == other.size
            && boxCollider.offset == other.offset;
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
        , 0);

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

        camera.transform.SetPositionAndRotation(new Vector3(0, 0, -50), Quaternion.identity);

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

    private void CreateSideColliders()
    {
        // width in world units
        Vector2 halfGridWidth = _gridInfo.GridDimensions * _gridInfo.TileSize / 2;

        // We want to add half a tile so the collider is aligned with the world tiles
        // We then want to add another tile to push the colliders outwards by one tile spacing
        // if two collider starts side by side, they are considered touching and the trigger callback is fired
        float absoluteLRPosition = halfGridWidth.y + _gridInfo.TileSize.y * 1.5f;
        float absoluteUDPosition = halfGridWidth.x + _gridInfo.TileSize.x * 1.5f;

        // Left/Right, Up/Down
        Vector2 LRColliderSize = new Vector2(_gridInfo.TileSize.x, halfGridWidth.y * 2 + _gridInfo.TileSize.y * 2);
        Vector2 UDColliderSize = new Vector2(halfGridWidth.x * 2 + _gridInfo.TileSize.x * 2, _gridInfo.TileSize.y);

        AddCollider(out _leftCollider, LRColliderSize, new Vector2(-absoluteUDPosition, 0));
        AddCollider(out _rightCollider, LRColliderSize, new Vector2(absoluteUDPosition, 0));
        AddCollider(out _upCollider, UDColliderSize, new Vector2(0, absoluteLRPosition));
        AddCollider(out _downCollider, UDColliderSize, new Vector2(0, -absoluteLRPosition));
    }

    private void AddCollider(out BoxCollider2D collider, Vector2 size, Vector2 offset)
    {
        collider = this.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = size;
        collider.offset = offset;
    }
}
