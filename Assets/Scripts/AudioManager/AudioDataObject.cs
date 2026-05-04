using UnityEngine;

[CreateAssetMenu(fileName = "AudioDataObject", menuName = "Scriptable Objects/AudioDataObject")]
public class AudioDataObject : ScriptableObject
{
    public AudioClip[] CurrentClips;
    public AudioTypeProvider CurrentTypeProvider;
    public Vector3 callerPosition;
}
