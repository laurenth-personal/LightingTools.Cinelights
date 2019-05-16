using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using LightUtilities;

[Serializable]
public class CineLightClipPlayable : PlayableBehaviour
{
    [SerializeField]
    public CineLightParameters cinelightParameters;
    [SerializeField]
    public LightParameters lightParameters = new LightParameters(LightShape.Spot,LightmapPresetBakeType.Realtime );
    [SerializeField]
    public ShadowCasterParameters shadowCasterParameters;
}

[Serializable]
public class CineLightClip : PlayableAsset, ITimelineClipAsset {

    public CineLightClipPlayable lightTargetClip = new CineLightClipPlayable();

    // Create the runtime version of the clip, by creating a copy of the template
    public override Playable CreatePlayable(PlayableGraph graph, GameObject go) {
        return ScriptPlayable<CineLightClipPlayable>.Create(graph, lightTargetClip);
    }

    // Use this to tell the Timeline Editor what features this clip supports
    public ClipCaps clipCaps {
        get { return ClipCaps.Blending | ClipCaps.Extrapolation; }
    }
}