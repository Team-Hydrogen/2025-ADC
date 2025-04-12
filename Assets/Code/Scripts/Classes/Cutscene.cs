using System;
using UnityEngine.Video;

[Serializable]
public struct Cutscene
{
    public VideoClip clip;
    public float triggerTimeInMinutes;
}
