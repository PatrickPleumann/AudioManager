using UnityEngine;

[CreateAssetMenu(fileName = "AudioDataObject", menuName = "Scriptable Objects/AudioDataObject")]
public class AudioDataObject : ScriptableObject
{
    public AudioClip[] CurrentClips;
    public AudioTypeProvider CurrentTypeProvider;
    public Transform CallerTransform;
    public bool SetCallerAsParent;
}
