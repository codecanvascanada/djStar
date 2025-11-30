using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.Linq;

// A custom Timeline track for spawning notes.
[TrackClipType(typeof(NotePlayableAsset))]
[TrackBindingType(typeof(GameObject))] // We will bind this track to a GameObject, and get the spawner from it.
public class NoteTrack : TrackAsset
{
    [Tooltip("The lane this track corresponds to (0-3).")]
    public int laneIndex;

    // This method creates the mixer that will process the clips on this track.
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        var mixer = ScriptPlayable<NoteTrackMixer>.Create(graph, inputCount);
        var mixerBehaviour = mixer.GetBehaviour();
        
        if (mixerBehaviour != null)
        {
            mixerBehaviour.laneIndex = laneIndex;
            mixerBehaviour.timelineClips = GetClips().ToList(); // Pass all clips to the mixer
        }

        return mixer;
    }
}
