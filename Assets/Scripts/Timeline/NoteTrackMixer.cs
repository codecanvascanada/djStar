using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.Collections.Generic;

// The mixer that processes all clips on a NoteTrack.
public class NoteTrackMixer : PlayableBehaviour
{
    public List<TimelineClip> timelineClips;
    // This is passed from the NoteTrack when the mixer is created.
    public int laneIndex;

    // This method is called every frame the timeline is playing.
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        GameObject spawnerGO = playerData as GameObject;
        if (spawnerGO == null) return;
        NoteSpawner spawner = spawnerGO.GetComponent<NoteSpawner>();
        if (spawner == null) return;

        int inputCount = playable.GetInputCount();

        for (int i = 0; i < inputCount; i++)
        {
            float inputWeight = playable.GetInputWeight(i);
            ScriptPlayable<NotePlayableBehaviour> inputPlayable = (ScriptPlayable<NotePlayableBehaviour>)playable.GetInput(i);
            NotePlayableBehaviour noteBehaviour = inputPlayable.GetBehaviour();

            // Check if the clip is active and hasn't been processed yet.
            if (inputWeight > 0 && !noteBehaviour.hasBeenProcessed)
            {
                // This is the first frame this clip is active.
                // Now we can set its properties and spawn it.
                double graphTime = playable.GetTime();
                double clipLocalTime = inputPlayable.GetTime();
                double clipDuration = inputPlayable.GetDuration();
                double clipStartTime = graphTime - clipLocalTime;

                noteBehaviour.laneIndex = this.laneIndex;
                noteBehaviour.timeToHit = clipStartTime;
                noteBehaviour.duration = clipDuration;

                if (noteBehaviour.noteType == NoteType.Tap)
                {
                    noteBehaviour.duration = 0;
                }

                spawner.SpawnNote(noteBehaviour.laneIndex, noteBehaviour.noteType, noteBehaviour.timeToHit, noteBehaviour.duration);
                
                noteBehaviour.hasBeenProcessed = true;
            }
        }
    }

    // This method is called when the PlayableDirector is stopped.
    public override void OnGraphStop(Playable playable)
    {
        // Reset the processed state of all clips when the timeline stops.
        int inputCount = playable.GetInputCount();
        for (int i = 0; i < inputCount; i++)
        {
            Playable input = playable.GetInput(i); // Get the raw Playable

            if (input.IsValid()) // Check if the playable handle is valid
            {
                var scriptPlayable = (ScriptPlayable<NotePlayableBehaviour>)input;
                var noteBehaviour = scriptPlayable.GetBehaviour();
            
                if (noteBehaviour != null)
                {
                    noteBehaviour.hasBeenProcessed = false;
                }
            }
        }
    }
}
