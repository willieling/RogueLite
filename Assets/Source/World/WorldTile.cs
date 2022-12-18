using UnityEngine;
using UnityEngine.Assertions;

public class WorldTile : MonoBehaviour
{
    public enum State
    {

    }

    private SpriteRenderer _spriteRenderer;

    public SpriteRenderer SpriteRenderer { get { return _spriteRenderer; } }

    // Start is called before the first frame update
    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        Assert.IsNotNull<SpriteRenderer>(_spriteRenderer, "WorldTile does not have a sprite renderer");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"{other.name}");
    }
}
