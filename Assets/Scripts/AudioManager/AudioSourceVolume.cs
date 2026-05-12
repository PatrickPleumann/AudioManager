using UnityEngine;

[CreateAssetMenu(fileName = "AudioSourceVolume", menuName = "Scriptable Objects/AudioSourceVolume")]
public class AudioSourceVolume : ScriptableObject
{
    public AudioTypeProvider CurrentAudioType;
    public float Volume;
}
