using UnityEngine;

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance;

    [Header("Effect Prefabs")]
    public GameObject goodEffectPrefab;
    public GameObject perfectEffectPrefab;
    public GameObject missEffectPrefab;
    public GameObject holdEffectPrefab; // New prefab for hold effect

    // Array to keep track of active hold effects for each lane (assuming 4 lanes)
    private GameObject[] activeHoldEffects = new GameObject[4];

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayEffect(Vector3 position, string judgment)
    {
        string tag = "";
        switch (judgment)
        {
            case "Perfect":
                tag = "PerfectEffect";
                break;
            case "Good":
                tag = "GoodEffect";
                break;
            case "Miss":
                tag = "MissEffect";
                break;
        }

        if (!string.IsNullOrEmpty(tag))
        {
            PoolManager.Instance.SpawnFromPool(tag, position, Quaternion.identity);
        }
    }

    // Method to start the looping hold effect
    public void StartHoldEffect(int laneIndex, Vector3 position)
    {
        if (laneIndex < 0 || laneIndex >= activeHoldEffects.Length)
        {
            return;
        }

        // If there's already an effect playing in this lane, stop it first
        if (activeHoldEffects[laneIndex] != null)
        {
            StopHoldEffect(laneIndex);
        }

        // Get the effect from the pool
        activeHoldEffects[laneIndex] = PoolManager.Instance.SpawnFromPool("HoldEffect", position, Quaternion.identity);
    }

    // Method to stop the looping hold effect
    public void StopHoldEffect(int laneIndex)
    {
        if (laneIndex < 0 || laneIndex >= activeHoldEffects.Length || activeHoldEffects[laneIndex] == null)
        {
            return;
        }

        // Return the effect to the pool and clear the reference
        PoolManager.Instance.ReturnToPool("HoldEffect", activeHoldEffects[laneIndex]);
        activeHoldEffects[laneIndex] = null;
    }
}
