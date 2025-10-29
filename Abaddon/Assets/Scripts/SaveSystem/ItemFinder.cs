using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class ItemFinder : ScriptableObject
{
    [SerializeField]
    List<GameObject> items;

    private Dictionary<int, GameObject> _idItemPairs;

    void Initialize()
    {
        _idItemPairs = new Dictionary<int, GameObject>();

        foreach (var prefab in items)
        {
            Item item = prefab.GetComponent<Item>();

            if (!_idItemPairs.ContainsKey(item.ItemID))
            {
                _idItemPairs.Add(item.ItemID, prefab);
            }
            else
            {
                GameObject other = _idItemPairs.TryGetValue(item.ItemID, out var h) ? h : null;
                Debug.LogWarning(
                    $"Duplicate item IDs: {item.ItemID}, {prefab.transform.name} {h.transform.name}"
                );
            }
        }
    }

    public GameObject GetItemByID(int id)
    {
        if (_idItemPairs == null)
            Initialize();

        return _idItemPairs.TryGetValue(id, out var item) ? item : null;
    }
}
