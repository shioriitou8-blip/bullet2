using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ObjectPool : MonoBehaviour
{
    [Serializable]
    public class PoolDefinition
    {
        public string key;
        public GameObject prefab;
        public int preloadCount = 12;
        public bool expandable = true;
    }

    [SerializeField] private List<PoolDefinition> definitions = new List<PoolDefinition>();

    private readonly Dictionary<string, PoolDefinition> definitionByKey = new Dictionary<string, PoolDefinition>();
    private readonly Dictionary<string, Queue<PooledObject>> availableByKey = new Dictionary<string, Queue<PooledObject>>();
    private readonly HashSet<PooledObject> spawnedObjects = new HashSet<PooledObject>();
    private bool initialized;

    private void Awake()
    {
        InitializeIfNeeded();
    }

    public void RegisterRuntimePool(string key, GameObject prefab, int preloadCount = 8, bool expandable = true)
    {
        if (string.IsNullOrWhiteSpace(key) || prefab == null)
        {
            return;
        }

        InitializeIfNeeded();
        if (definitionByKey.ContainsKey(key))
        {
            return;
        }

        PoolDefinition definition = new PoolDefinition
        {
            key = key,
            prefab = prefab,
            preloadCount = Mathf.Max(0, preloadCount),
            expandable = expandable
        };

        definitions.Add(definition);
        definitionByKey[key] = definition;
        availableByKey[key] = new Queue<PooledObject>(Mathf.Max(1, preloadCount));

        for (int i = 0; i < definition.preloadCount; i++)
        {
            PooledObject pooled = CreateNewPooled(definition);
            availableByKey[key].Enqueue(pooled);
        }
    }

    public bool HasPool(string key)
    {
        InitializeIfNeeded();
        return !string.IsNullOrWhiteSpace(key) && definitionByKey.ContainsKey(key);
    }

    public GameObject Spawn(string key, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        InitializeIfNeeded();
        if (!definitionByKey.TryGetValue(key, out PoolDefinition definition))
        {
            return null;
        }

        if (!availableByKey.TryGetValue(key, out Queue<PooledObject> queue))
        {
            queue = new Queue<PooledObject>();
            availableByKey[key] = queue;
        }

        PooledObject pooled = null;
        while (queue.Count > 0 && pooled == null)
        {
            pooled = queue.Dequeue();
        }

        if (pooled == null)
        {
            if (!definition.expandable)
            {
                return null;
            }

            pooled = CreateNewPooled(definition);
        }

        Transform pooledTransform = pooled.transform;
        pooledTransform.SetParent(parent, false);
        pooledTransform.SetPositionAndRotation(position, rotation);
        pooled.gameObject.SetActive(true);
        spawnedObjects.Add(pooled);
        return pooled.gameObject;
    }

    public void Despawn(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        PooledObject pooled = instance.GetComponent<PooledObject>();
        if (pooled == null || pooled.Owner != this || string.IsNullOrWhiteSpace(pooled.PoolKey))
        {
            instance.SetActive(false);
            return;
        }

        instance.SetActive(false);
        pooled.transform.SetParent(transform, false);
        spawnedObjects.Remove(pooled);

        if (!availableByKey.TryGetValue(pooled.PoolKey, out Queue<PooledObject> queue))
        {
            queue = new Queue<PooledObject>();
            availableByKey[pooled.PoolKey] = queue;
        }

        queue.Enqueue(pooled);
    }

    public void DespawnAll()
    {
        PooledObject[] all = new PooledObject[spawnedObjects.Count];
        spawnedObjects.CopyTo(all);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null)
            {
                Despawn(all[i].gameObject);
            }
        }
    }

    private void InitializeIfNeeded()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        definitionByKey.Clear();
        availableByKey.Clear();

        for (int i = 0; i < definitions.Count; i++)
        {
            PoolDefinition definition = definitions[i];
            if (definition == null || string.IsNullOrWhiteSpace(definition.key) || definition.prefab == null)
            {
                continue;
            }

            if (definitionByKey.ContainsKey(definition.key))
            {
                continue;
            }

            definition.preloadCount = Mathf.Max(0, definition.preloadCount);
            definitionByKey[definition.key] = definition;
            Queue<PooledObject> queue = new Queue<PooledObject>(Mathf.Max(1, definition.preloadCount));
            availableByKey[definition.key] = queue;

            for (int j = 0; j < definition.preloadCount; j++)
            {
                PooledObject pooled = CreateNewPooled(definition);
                queue.Enqueue(pooled);
            }
        }
    }

    private PooledObject CreateNewPooled(PoolDefinition definition)
    {
        GameObject instance = Instantiate(definition.prefab, transform);
        instance.name = definition.prefab.name;

        PooledObject pooled = instance.GetComponent<PooledObject>();
        if (pooled == null)
        {
            pooled = instance.AddComponent<PooledObject>();
        }

        pooled.Bind(this, definition.key);
        instance.SetActive(false);
        return pooled;
    }
}
