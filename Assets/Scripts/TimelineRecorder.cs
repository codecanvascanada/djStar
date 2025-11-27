#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class TimelineRecorder : MonoBehaviour
{
    [Header("Recording Settings")]
    [Tooltip("Check this to start recording when play mode begins.")]
    public bool isRecording = false;
    
    [Tooltip("If true, existing notes on the timeline tracks will be cleared before new notes are applied.")]
    public bool clearExistingNotesOnRecord = true;
    
    [Tooltip("The PlayableDirector that controls the timeline.")]
    public PlayableDirector director;

    [Tooltip("Offset in seconds to apply to recorded note times to compensate for input latency.")]
    public float recordingOffsetSeconds = 0f;

    [Tooltip("The target frames per second for converting frame offset to seconds.")]
    public float framesPerSecond = 60.0f;

    [Header("Input & Quantization")]
    [Tooltip("Assign the 4 TargetController objects from your lanes here, in order.")]
    public TargetController[] targetControllers;
    
    [Tooltip("Minimum duration in seconds to register a note as a Hold note.")]
    public float holdTimeThreshold = 0.2f;

    [Tooltip("Notes recorded within this time of the previous note will be snapped to the same time.")]
    public float quantizationThreshold = 0.05f;

    // --- Private Fields ---
    private enum RecorderState { WaitingForPlay, Recording, Stopped }
    private RecorderState _currentState;
    private Dictionary<int, NoteData> _activeHoldNotes = new Dictionary<int, NoteData>();
    private List<NoteData> _recordedNotes = new List<NoteData>();
    private TimelineAsset _timelineAsset; // Cache the timeline asset

    void Awake()
    {
        // Intentionally left blank.
    }

    void OnEnable()
    {
        if (isRecording)
        {
            // Cache the timeline asset at the start
            if (director != null && director.playableAsset != null)
            {
                _timelineAsset = director.playableAsset as TimelineAsset;
            }

            if (_timelineAsset == null)
            {
                Debug.LogError("TimelineRecorder: Director has no TimelineAsset. Disabling recording.");
                isRecording = false;
                _currentState = RecorderState.Stopped;
                return;
            }

            _activeHoldNotes.Clear();
            _recordedNotes.Clear();
            _currentState = RecorderState.WaitingForPlay;
            Debug.Log("Timeline Recorder: Ready to record. Waiting for director to play...");

            // Subscribe to TargetController events
            if (targetControllers != null)
            {
                for (int i = 0; i < targetControllers.Length; i++)
                {
                    if (targetControllers[i] != null)
                    {
                        targetControllers[i].OnInputPressed += HandleTargetInputPressed;
                        targetControllers[i].OnInputReleased += HandleTargetInputReleased;
                    }
                }
            }
        }
        else
        {
            _currentState = RecorderState.Stopped;
        }
    }

    void OnDisable()
    {
        // Unsubscribe from TargetController events to prevent memory leaks
        if (targetControllers != null)
        {
            for (int i = 0; i < targetControllers.Length; i++)
            {
                if (targetControllers[i] != null)
                {
                    targetControllers[i].OnInputPressed -= HandleTargetInputPressed;
                    targetControllers[i].OnInputReleased -= HandleTargetInputReleased;
                }
            }
        }

        if (!isRecording) return;

        // If play mode stops while keys are held down, process any remaining active notes.
        if (_activeHoldNotes.Count > 0)
        {
            Debug.Log($"Processing {_activeHoldNotes.Count} active notes that were not released before stopping.");
            foreach (var note in _activeHoldNotes.Values)
            {
                // Treat as a tap note since we don't have an end time.
                note.noteType = NoteType.Tap;
                note.holdDuration = 0;
                _recordedNotes.Add(note);
            }
            _activeHoldNotes.Clear();
        }

        if (_recordedNotes.Count > 0)
        {
            Debug.Log($"Recording finished. {_recordedNotes.Count} notes captured. Processing...");
            SortAndQuantizeNotes();
            ApplyNotesToTimeline();
        }
        else
        {
            Debug.Log("No notes were recorded in this session.");
        }
    }

    void Update()
    {
        if (!isRecording || director == null || targetControllers == null)
        {
            return;
        }

        switch (_currentState)
        {
            case RecorderState.WaitingForPlay:
                if (director.state == PlayState.Playing)
                {
                    Debug.Log("Director is now playing. Recording has officially started!");
                    _currentState = RecorderState.Recording;
                }
                break;

            case RecorderState.Recording:
                if (director.state != PlayState.Playing)
                {
                    Debug.Log("Director has stopped. Ending recording logic for this session.");
                    _currentState = RecorderState.Stopped;
                }
                // Input is now handled by events, so no polling here.
                break;

            case RecorderState.Stopped:
                // Do nothing
                break;
        }
    }

    private void HandleTargetInputPressed(int laneIndex)
    {
        if (_currentState != RecorderState.Recording || director == null) return;

        // Start tracking a potential note
        NoteData newNote = new NoteData
        {
            laneIndex = laneIndex,
            timeToHit = (float)director.time - recordingOffsetSeconds, // Apply recording offset
            noteType = NoteType.Tap, // Assume Tap initially
            holdDuration = 0
        };
        _activeHoldNotes[laneIndex] = newNote;
        Debug.Log($"Event: Key Down on lane {laneIndex} at time {(float)director.time}");
    }

    private void HandleTargetInputReleased(int laneIndex)
    {
        if (_currentState != RecorderState.Recording || director == null) return;

        if (_activeHoldNotes.TryGetValue(laneIndex, out NoteData noteToEnd))
        {
            double endTime = director.time;

            // To get the true duration, we must calculate the original, un-offsetted press time.
            double actualPressTime = noteToEnd.timeToHit + recordingOffsetSeconds;
            float duration = (float)(endTime - actualPressTime);

            // Now, decide if it's a hold or a tap based on the true duration.
            if (duration >= holdTimeThreshold)
            {
                noteToEnd.noteType = NoteType.Hold;
                noteToEnd.holdDuration = duration;
            }
            else
            {
                noteToEnd.noteType = NoteType.Tap;
                noteToEnd.holdDuration = 0;
            }
            
            _recordedNotes.Add(noteToEnd);
            _activeHoldNotes.Remove(laneIndex);
            Debug.Log($"Event: Key Up on lane {laneIndex}. Note recorded. Type: {noteToEnd.noteType}, True Duration: {duration}");
        }
    }

    void SortAndQuantizeNotes()
    {
        if (_recordedNotes.Count == 0) return;

        // 1. Sort by time
        _recordedNotes = _recordedNotes.OrderBy(note => note.timeToHit).ToList();

        // 2. Quantize by grouping into chords
        var processedNotes = new List<NoteData>();
        var currentChord = new List<NoteData>();
        currentChord.Add(_recordedNotes[0]);

        for (int i = 1; i < _recordedNotes.Count; i++)
        {
            NoteData anchorNote = currentChord[0];
            NoteData currentNote = _recordedNotes[i];
            float timeDifference = currentNote.timeToHit - anchorNote.timeToHit;

            if (timeDifference < quantizationThreshold)
            {
                currentChord.Add(currentNote);
            }
            else
            {
                ProcessChord(currentChord);
                processedNotes.AddRange(currentChord);
                currentChord.Clear();
                currentChord.Add(currentNote);
            }
        }
        ProcessChord(currentChord);
        processedNotes.AddRange(currentChord);
        _recordedNotes = processedNotes;

        Debug.Log("Quantization complete.");
    }

    void ProcessChord(List<NoteData> chord)
    {
        if (chord.Count <= 1) return;
        NoteData anchorNote = chord[0];
        foreach (var note in chord)
        {
            note.timeToHit = anchorNote.timeToHit;
            if (note.noteType == NoteType.Hold && anchorNote.noteType == NoteType.Hold)
            {
                note.holdDuration = anchorNote.holdDuration;
            }
        }
    }

    void ApplyNotesToTimeline()
    {
        // Use the cached timeline asset
        if (_timelineAsset == null)
        {
            Debug.LogError("TimelineRecorder: Cached TimelineAsset is null. Cannot apply notes.");
            return;
        }

        // Find all our NoteTracks
        var noteTracks = new Dictionary<int, NoteTrack>();
        foreach (var track in _timelineAsset.GetOutputTracks())
        {
            if (track is NoteTrack noteTrack)
            {
                // Clear existing clips on this track before adding new ones, if enabled
                if (clearExistingNotesOnRecord)
                {
                    foreach (var clip in noteTrack.GetClips())
                        noteTrack.DeleteClip(clip);
                }
                noteTracks[noteTrack.laneIndex] = noteTrack;
            }
        }

        if (noteTracks.Count == 0)
        {
            Debug.LogError("TimelineRecorder: No NoteTracks found in the timeline.");
            return;
        }

        // Create a clip for each recorded note
        foreach (var noteData in _recordedNotes)
        {
            if (noteTracks.TryGetValue(noteData.laneIndex, out NoteTrack targetTrack))
            {
                TimelineClip newClip = targetTrack.CreateClip<NotePlayableAsset>();
                NotePlayableAsset asset = newClip.asset as NotePlayableAsset;

                asset.noteType = noteData.noteType;
                newClip.start = noteData.timeToHit;
                newClip.duration = (noteData.noteType == NoteType.Hold) ? noteData.holdDuration : 0.1; // Give taps a small visual duration
            }
        }

        // Mark the timeline asset as dirty to ensure changes are saved
        EditorUtility.SetDirty(_timelineAsset);
        Debug.Log("Timeline updated with recorded notes.");
    }
}
#endif