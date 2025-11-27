using UnityEngine;
using UnityEngine.Playables;

// The runtime behaviour of a Note clip.
[System.Serializable]
public class NotePlayableBehaviour : PlayableBehaviour
{
    // Data passed from the clip asset
    public int laneIndex;
    public NoteType noteType;
    public double timeToHit;
    public double duration;

    // State for the mixer to use
    public bool hasBeenProcessed = false;
}
