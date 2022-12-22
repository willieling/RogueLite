using UnityEngine;
using UnityEngine.Assertions;

public class WorldTile : MonoBehaviour
{
    private SpriteRenderer _spriteRenderer;
    private WorldController _worldController;

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
}
