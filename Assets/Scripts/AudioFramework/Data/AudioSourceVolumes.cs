using UnityEngine;

namespace AudioFramework.Data
{
    [CreateAssetMenu(fileName = "AudioSourceVolume", menuName = "Scriptable Objects/AudioSourceVolume")]
    public class AudioSourceVolumes : ScriptableObject
    {
        public AudioTypeProvider CurrentAudioType;
        public float Volume;
    }
}
