#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TimelineRecorder))]
public class TimelineRecorderEditor : Editor
{
    private void OnEnable()
    {
        // Subscribe to the play mode state change event
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // This event is called when the editor's play mode changes.
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            // Get the target script
            TimelineRecorder recorder = (TimelineRecorder)target;

            // If it was recording, uncheck the box and mark it as dirty to save the change.
            if (recorder.isRecording)
            {
                recorder.isRecording = false;
                recorder.clearExistingNotesOnRecord = false;
                EditorUtility.SetDirty(recorder);
            }
        }
    }
}
#endif
