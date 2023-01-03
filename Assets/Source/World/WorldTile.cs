using UnityEngine;
using UnityEngine.Assertions;

public class WorldTile : MonoBehaviour, PooledItem<WorldTile>
{
    private SpriteRenderer _spriteRenderer;
    private WorldController _worldController;
    private Pool<WorldTile> _owningPool;

    public SpriteRenderer SpriteRenderer { get { return _spriteRenderer; } }

    // Start is called before the first frame update
    void Awake()
    {
        _worldController = WorldController.Instance;
        Assert.IsNotNull(_worldController);

        _spriteRenderer = GetComponent<SpriteRenderer>();
        Assert.IsNotNull<SpriteRenderer>(_spriteRenderer, "WorldTile does not have a sprite renderer");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"{other.name}");

        _worldController.NotifyWorldTilecollided(this, other);
    }

    public void Initialize(Pool<WorldTile> owningPool)
    {
        _owningPool = owningPool;
    }

    public void Release()
    {
        _owningPool.Release(this);
    }
}
