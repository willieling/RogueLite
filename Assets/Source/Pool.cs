using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pool<T> where T : Component
{
    string _name;

    private List<T> _acquiredItems;
    private List<T> _releasedItems;

    private Transform _parent = null;
    // The prefab to instantiate when creating items
    private T _prefab = null;

    public Pool(string name, T prefab, Vector3 origin, int capacity = 0)
    {
        _name = name;

        _parent = new GameObject($"{_name} Pool").transform;
        _parent.transform.position = origin;

        _prefab = prefab;

        _acquiredItems = new List<T>(capacity);
        _releasedItems = new List<T>(capacity);

        for (int i = 0; i < capacity; i++)
        {
            _releasedItems.Add(CreateItem());
        }
    }

    public T Get()
    {
        T item;
        if(_releasedItems.Count > 0)
        {
            item = _releasedItems[_releasedItems.Count - 1];
            _releasedItems.RemoveAt(_releasedItems.Count - 1);
        }
        else
        {
            item = CreateItem();
            _acquiredItems.Add(item);
        }

        item.transform.SetParent(null);

        return item;
    }

    public void Release(T item)
    {
        if(!_acquiredItems.Contains(item))
        {
            Debug.LogWarningFormat($"Trying to release item {item.gameObject.name} but it's not part of the pool.  Ignoring release request.");
            return;
        }

        _acquiredItems.Remove(item);
        _releasedItems.Add(item);

        item.transform.SetParent(_parent);
    }

    private T CreateItem()
    {
        GameObject newItem = Object.Instantiate(_prefab.gameObject, Vector3.zero, Quaternion.identity, _parent);
        newItem.name = $"{_name} {_releasedItems.Count}";
        newItem.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        T component = newItem.GetComponent<T>();
        return component;
    }
}
