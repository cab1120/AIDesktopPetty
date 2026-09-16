using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AIDesktopPetty.Art.Scenes
{
    [TrackColor(.25f, .7f, .5f)]
    [TrackClipType(typeof(SakuramachiLoopClip))]
    [TrackBindingType(typeof(SakuramachiSceneLoop))]
    public sealed class SakuramachiLoopTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            var mixer = ScriptPlayable<SakuramachiLoopMixer>.Create(graph, inputCount);
            mixer.GetBehaviour().director = go.GetComponent<PlayableDirector>();
            return mixer;
        }
    }

    public sealed class SakuramachiLoopMixer : PlayableBehaviour
    {
        public PlayableDirector director;
        SakuramachiSceneLoop target;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var next = playerData as SakuramachiSceneLoop;
            if (next != target)
            {
                if (target != null) target.RestoreOriginalState();
                target = next;
            }
            if (target == null || !target.isActiveAndEnabled || director == null) return;
            target.Evaluate(director.time, info.evaluationType == FrameData.EvaluationType.Playback &&
                info.effectivePlayState == PlayState.Playing && info.effectiveSpeed > 0);
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (target != null) target.SilenceAudio();
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            if (target != null) target.RestoreOriginalState();
            target = null;
        }
    }
}
