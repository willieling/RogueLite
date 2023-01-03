using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface PooledItem<TItem> where TItem : MonoBehaviour
{
    void Initialize(Pool<TItem> owningPool);
    void Release();
}
