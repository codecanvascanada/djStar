using UnityEngine;

/// <summary>
/// A component attached to pooled GameObjects to identify their original pool tag.
/// This allows objects to return themselves to the correct pool.
/// </summary>
public class PooledObject : MonoBehaviour
{
    public string poolTag;
}