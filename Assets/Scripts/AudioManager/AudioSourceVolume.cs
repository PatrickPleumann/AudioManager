using UnityEngine;

[CreateAssetMenu(fileName = "AudioSoruceVolume", menuName = "Scriptable Objects/AudioSoruceVolume")]
public class AudioSourceVolume : ScriptableObject
{
    public AudioTypeProvider CurrentAudioType;
    public float Volume;
}
