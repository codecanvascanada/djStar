using UnityEngine;

public class WebGLStart : MonoBehaviour
{
    [Tooltip("The UI object that acts as a screen-wide tap area.")]
    public GameObject tapArea;

    [Tooltip("The NoteSpawner to activate once the screen is tapped.")]
    public NoteSpawner noteSpawner;

    void Start()
    {
        // Ensure the spawner is not running at the start
        if (noteSpawner != null)
        {
            noteSpawner.enabled = false;
        }

        // Ensure the tap area is visible at the start
        if (tapArea != null)
        {
            tapArea.SetActive(true);
        }
    }

    // This method will be called by the EventTrigger on the TapArea
    public void StartGame()
    {
        // Check if the game has already been started to prevent multiple triggers
        if (noteSpawner != null && noteSpawner.enabled)
        {
            return;
        }

        Debug.Log("Screen tapped. Unlocking audio and starting game sequence...");

        // Enable the spawner, which will then trigger its Start() method
        if (noteSpawner != null)
        {
            noteSpawner.enabled = true;
        }

        // Hide the tap area
        if (tapArea != null)
        {
            tapArea.SetActive(false);
        }
    }
}
