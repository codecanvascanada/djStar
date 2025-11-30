using System.Collections;
using UnityEngine;

public class PooledParticle : MonoBehaviour, IPooledObject
{
    [Tooltip("The lifetime of the particle effect. It will be returned to the pool after this duration.")]
    public float lifeTime = 2f;

    private Coroutine _returnCoroutine;

    public void OnObjectSpawn()
    {
        // When the object is spawned, start a coroutine to return it to the pool after its lifetime.
        _returnCoroutine = StartCoroutine(ReturnToPoolAfterTime());
    }

    public void OnObjectReturn()
    {
        // If the object is returned manually (e.g., a hold effect being stopped),
        // we need to stop the automatic return coroutine.
        if (_returnCoroutine != null)
        {
            StopCoroutine(_returnCoroutine);
        }
    }

    private IEnumerator ReturnToPoolAfterTime()
    {
        yield return new WaitForSeconds(lifeTime);

        // Find the tag associated with this pooled object.
        string poolTag = GetComponent<PooledObject>()?.poolTag;
        if (!string.IsNullOrEmpty(poolTag))
        {
            PoolManager.Instance.ReturnToPool(poolTag, gameObject);
        }
        else
        {
            // Fallback if the tag is somehow missing
            Debug.LogWarning("PooledObject component or tag is missing. Destroying object instead.", gameObject);
            Destroy(gameObject);
        }
    }
}
