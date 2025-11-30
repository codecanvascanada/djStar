// A serializable class for a single gameplay note.
// This is now separate from the legacy data structure.
[System.Serializable]
public class NoteData
{
    public int laneIndex;
    public float timeToHit;
    public NoteType noteType;
    public float holdDuration;
    public float noteTravelTime; // How long it takes for the note to travel from spawn to target
    public float targetZ;        // The Z-coordinate of the judgment line
}
