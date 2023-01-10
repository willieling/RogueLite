using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Create a base projectile class
public class ProjectileBullet : MonoBehaviour, PooledItem<ProjectileBullet>
{
    private Pool<ProjectileBullet> _owningPool = null;

    private Vector2 _direction;

    // Update is called once per frame
    void Update()
    {
        this.transform.Translate(_direction);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"{other.name}");

        Release();
    }

    public void Initialize(Pool<ProjectileBullet> owningPool)
    {
        _owningPool = owningPool;
    }

    public void Release()
    {
        _owningPool.Release(this);
        _direction= Vector2.zero;
    }

    public void SetDirection(Vector2 direction)
    {
        _direction = direction;
    }
}
