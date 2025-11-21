using UnityEngine;
using UnityEngine.Playables;

// Represents the data for a single note on the timeline.
[System.Serializable]
public class NotePlayableAsset : PlayableAsset
{
    [Tooltip("The type of this note (Tap or Hold). The clip's duration determines the hold length.")]
    public NoteType noteType;

    // This method is called by the Timeline when a clip is created.
    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<NotePlayableBehaviour>.Create(graph);
        var noteBehaviour = playable.GetBehaviour();

        // Pass the note type from the asset to the behaviour.
        // The mixer will handle the rest (laneIndex, timeToHit, duration).
        noteBehaviour.noteType = this.noteType;

        return playable;
    }
}
