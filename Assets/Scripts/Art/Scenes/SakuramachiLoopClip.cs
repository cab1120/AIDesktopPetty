using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AIDesktopPetty.Art.Scenes
{
    [Serializable]
    public sealed class SakuramachiLoopClip : PlayableAsset, ITimelineClipAsset
    {
        public override double duration => SakuramachiSceneLoop.CycleSeconds;
        public ClipCaps clipCaps => ClipCaps.None;
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) =>
            Playable.Create(graph);
    }
}
