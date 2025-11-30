using UnityEngine;

// This new NoteSpawner is much simpler. Its only job is to spawn a note when told to by the Timeline mixer.
public class NoteSpawner : MonoBehaviour
{
    [Header("Spawning Info")]
    public GameObject notePrefab;
    public Transform[] laneSpawnPoints;
    public Transform noteParent; // Assign a parent object to keep the hierarchy clean

    [Header("Gameplay Settings")]
    public float judgmentLineZ = 0f; // The Z-coordinate where notes are judged

    [Header("Note Materials")]
    public Material tapNoteMaterial;
    public Material holdNoteMaterial;

    public float noteTravelTime;

    void Start()
    {
        // Calculate note travel time once at the start.
        if (laneSpawnPoints.Length > 0 && notePrefab != null)
        {
            float distance = laneSpawnPoints[0].position.z;
            float speed = notePrefab.GetComponent<Note>().speed;
            if (speed > 0)
            {
                noteTravelTime = distance / speed;
            }
        }
    }

    void OnDisable()
    {
        // Clear notes when the game stops or the scene changes.
        // Moved to a controlled method for explicit cleanup.
    }

    void ClearOldNotes()
    {
        if (noteParent != null)
        {
            // Return all children to the pool instead of destroying them
            foreach (Transform child in noteParent)
            {
                PoolManager.Instance.ReturnToPool("Note", child.gameObject);
            }
        }
    }

    // This method is called by the NoteTrackMixer.
    public void SpawnNote(int laneIndex, NoteType noteType, double timeToHit, double holdDuration)
    {
        if (!Application.isPlaying) return; // Prevent spawning notes when not in play mode

        if (laneIndex < 0 || laneIndex >= laneSpawnPoints.Length)
        {
            UnityEngine.Debug.LogError(string.Format("Invalid lane index: {0}", laneIndex));
            return;
        }

        // Create the note data from the information passed by the timeline clip
        NoteData noteData = new NoteData
        {
            laneIndex = laneIndex,
            timeToHit = (float)timeToHit,
            noteType = noteType,
            holdDuration = (float)holdDuration,
            noteTravelTime = this.noteTravelTime, // Pass the calculated travel time
            targetZ = this.judgmentLineZ          // Pass the judgment line Z
        };

        Transform spawnPoint = laneSpawnPoints[laneIndex];
        // Get a note from the pool instead of instantiating
        GameObject noteObject = PoolManager.Instance.SpawnFromPool("Note", spawnPoint.position, Quaternion.identity);
        if (noteObject == null) return; // Pool might be empty or tag is wrong

        noteObject.transform.SetParent(noteParent); // Set parent for organization
        
        Note noteScript = noteObject.GetComponent<Note>();
        if (noteScript != null)
        {
            noteScript.Setup(noteData);
            noteScript.SetLaneColor(laneIndex); // Set the color based on lane index
        }
    }

    // Public method to initialize the spawner and clear any existing notes.
    public void InitializeSpawner()
    {
        ClearOldNotes();
    }
}
