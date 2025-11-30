using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
    }

    public static PoolManager Instance;

    public List<Pool> pools;
    public Dictionary<string, Queue<GameObject>> poolDictionary;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                PooledObject pooledObj = obj.AddComponent<PooledObject>();
                pooledObj.poolTag = pool.tag;
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(pool.tag, objectPool);
        }
    }

    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning("Pool with tag " + tag + " doesn't exist.");
            return null;
        }

        if (poolDictionary[tag].Count == 0)
        {
            Pool pool = pools.Find(p => p.tag == tag);
            if (pool != null)
            {
                GameObject obj = Instantiate(pool.prefab);
                PooledObject pooledObj = obj.AddComponent<PooledObject>();
                pooledObj.poolTag = pool.tag;
                poolDictionary[tag].Enqueue(obj);
                Debug.LogWarning("Pool with tag " + tag + " was empty. Expanded pool size by one.");
            }
        }

        GameObject objectToSpawn = poolDictionary[tag].Dequeue();

        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;
        
        IPooledObject pooledObjectInterface = objectToSpawn.GetComponent<IPooledObject>();
        pooledObjectInterface?.OnObjectSpawn();

        return objectToSpawn;
    }

    public void ReturnToPool(string tag, GameObject objectToReturn)
    {
        // If the object is already inactive, it's already in the pool or being returned. Ignore.
        if (!objectToReturn.activeInHierarchy)
        {
            return;
        }

        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning("Pool with tag " + tag + " doesn't exist.");
            Destroy(objectToReturn);
            return;
        }

        IPooledObject pooledObjectInterface = objectToReturn.GetComponent<IPooledObject>();
        pooledObjectInterface?.OnObjectReturn();

        objectToReturn.SetActive(false);
        poolDictionary[tag].Enqueue(objectToReturn);
    }
}

public interface IPooledObject
{
    void OnObjectSpawn();
    void OnObjectReturn();
}
