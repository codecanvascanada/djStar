using UnityEngine;

public class Note : MonoBehaviour, IPooledObject
{
    [Header("Note Properties")]
    public float speed = 10f;
    
    private GameObject visual; 
    private Rigidbody rb;
    private Vector3 originalVisualScale;
    private Vector3 originalVisualPosition;

    [HideInInspector]
    public NoteData noteData;

    // Public property to safely expose the MeshRenderer of the visual part
    public MeshRenderer VisualRenderer { get; private set; }

    [Header("Lane Coloring")]
    [Tooltip("Assign materials for lane colors. Index 0 for lanes 0 & 3, Index 1 for lanes 1 & 2.")]
    public Material[] laneMaterials;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            UnityEngine.Debug.LogError("Rigidbody component not found on the note prefab!", this.gameObject);
        }

        Transform visualTransform = transform.Find("Visual");
        if (visualTransform == null)
        {
            UnityEngine.Debug.LogError("Could not find a child object named 'Visual' on the note prefab!", this.gameObject);
            return;
        }
        visual = visualTransform.gameObject;

        // Store the original scale and position to reset to
        originalVisualScale = visual.transform.localScale;
        originalVisualPosition = visual.transform.localPosition;

        VisualRenderer = visual.GetComponent<MeshRenderer>();
        if (VisualRenderer == null)
        {
            UnityEngine.Debug.LogError("Could not find a MeshRenderer on the 'Visual' child object!", this.gameObject);
        }
    }

    // This is called by the PoolManager when the object is spawned.
    public void OnObjectSpawn()
    {
        // Reset the scale and position of the visual part to its original prefab state.
        if (visual != null)
        {
            visual.transform.localScale = originalVisualScale;
            visual.transform.localPosition = originalVisualPosition;
        }

        // Reset the rigidbody's state to prevent interpolation artifacts
        if (rb != null)
        {
            // Toggling isKinematic forces a refresh of the rigidbody's state,
            // which can help prevent interpolation issues when re-using pooled objects.
            rb.isKinematic = false;
            rb.isKinematic = true;
        }
    }

    // This is called by the PoolManager when the object is returned to the pool.
    public void OnObjectReturn()
    {
        // Nothing needed here. We will reset on spawn.
    }

    // Sets the color of the note based on its lane index
    public void SetLaneColor(int laneIndex)
    {
        if (VisualRenderer == null)
        {
            UnityEngine.Debug.LogWarning("VisualRenderer is null on note, cannot set lane color.", this);
            return;
        }

        if (laneMaterials == null || laneMaterials.Length < 2)
        {
            UnityEngine.Debug.LogWarning("Lane Materials array is not assigned or too small on note, cannot set lane color.", this);
            return;
        }

        if (laneIndex == 0 || laneIndex == 3) // Lane 1 and Lane 4 (0-indexed)
        {
            VisualRenderer.material = laneMaterials[0];
        }
        else if (laneIndex == 1 || laneIndex == 2) // Lane 2 and Lane 3 (0-indexed)
        {
            VisualRenderer.material = laneMaterials[1];
        }
        else
        {
            UnityEngine.Debug.LogWarning(string.Format("Invalid laneIndex {0} for setting note color.", laneIndex), this);
        }
    }

    // Setup method to initialize the note with data from the spawner
    public void Setup(NoteData data)
    {
        noteData = data;

        // If it's a hold note, adjust the visual scale
        if (noteData.noteType == NoteType.Hold && visual != null)
        {
            // Calculate the length of the hold note in world units
            float length = speed * noteData.holdDuration;

            // Adjust the scale of the visual part
            Vector3 scale = visual.transform.localScale;
            scale.z = length;
            visual.transform.localScale = scale;

            // Adjust the pivot point so it scales away from the player
            // Assuming the default pivot is at the center
            visual.transform.localPosition = new Vector3(0, 0, length / 2);
        }

        // Set the initial Z position so the note reaches the targetZ at timeToHit
        // The note is spawned at spawnPoint.position (which has the correct X and Y)
        // We need to adjust its Z position.
        Vector3 newPosition = transform.position;
        newPosition.z = noteData.targetZ + (noteData.noteTravelTime * speed);
        if (rb != null)
        {
            rb.position = newPosition;
        }
        else
        {
            transform.position = newPosition;
        }
    }

    void Update()
    {
        // Visual updates or non-physics updates can stay here if needed.
    }

    void FixedUpdate()
    {
        // Move the note towards the player using Rigidbody for smooth interpolation
        if (rb != null)
        {
            Vector3 newPosition = rb.position + Vector3.back * speed * Time.fixedDeltaTime;
            rb.MovePosition(newPosition);
        }
    }

    // This will be called by the "Cleanup" trigger zone
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Cleanup"))
        {
            // Return to pool instead of destroying
            PoolManager.Instance.ReturnToPool("Note", gameObject);
        }
    }
}
