using System.Collections.Generic;
using UnityEngine;

public class Pool<TItem> where TItem : MonoBehaviour
{
    private string _name;

    private List<PooledItem<TItem>> _acquiredItems;
    private List<PooledItem<TItem>> _releasedItems;

    private Transform _parent = null;
    // The prefab to instantiate when creating items
    private TItem _prefab = null;

    private bool _disableWhilInactive = false;

    public Pool(TItem prefab, Vector3 origin, int capacity = 0)
    {
        _parent = new GameObject($"{typeof(TItem)} Pool").transform;
        _parent.transform.position = origin;

        _prefab = prefab;

        _acquiredItems = new List<PooledItem<TItem>>(capacity);
        _releasedItems = new List<PooledItem<TItem>>(capacity);

        for (int i = 0; i < capacity; i++)
        {
            _releasedItems.Add(CreateItem());
        }
    }

    public Pool<TItem> SetName(string name)
    {
        _name = name;
        _parent.name = $"{_name} Pool";
        return this;
    }

    public Pool<TItem> DisableOnRelease(bool disableOnRelease)
    {
        _disableWhilInactive = disableOnRelease;
        return this;
    }

    public TItem Get()
    {
        PooledItem<TItem> pooledItem;
        if(_releasedItems.Count > 0)
        {
            pooledItem = _releasedItems[_releasedItems.Count - 1];
            _releasedItems.RemoveAt(_releasedItems.Count - 1);
        }
        else
        {
            pooledItem = CreateItem();
        }

        _acquiredItems.Add(pooledItem);

        TItem item = (TItem)pooledItem;
        item.transform.SetParent(null);

        if(_disableWhilInactive)
        {
            item.enabled = true;
        }

        return item;
    }

    public void Release(PooledItem<TItem> pooledItem)
    {
        if(!_acquiredItems.Contains(pooledItem))
        {
            Debug.LogWarningFormat($"Trying to release item {(pooledItem as MonoBehaviour).gameObject.name} but it's not part of the pool.  Ignoring release request.");
            return;
        }

        _acquiredItems.Remove(pooledItem);
        _releasedItems.Add(pooledItem);

        TItem item = (TItem)pooledItem;
        item.transform.SetParent(_parent);
        item.transform.localPosition= Vector3.zero;
        if (_disableWhilInactive)
        {
            item.enabled = false;
        }
    }

    private PooledItem<TItem> CreateItem()
    {
        GameObject pooledItem = Object.Instantiate(_prefab.gameObject, Vector3.zero, Quaternion.identity, _parent);
        pooledItem.name = $"{_name} {_releasedItems.Count}";
        pooledItem.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        TItem item = pooledItem.GetComponent<TItem>();
        item.enabled = _disableWhilInactive;

        PooledItem<TItem> pooledItemScript = (PooledItem<TItem>)item;
        pooledItemScript.Initialize(this);

        return pooledItemScript;
    }
}
