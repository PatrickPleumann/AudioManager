using UnityEngine;

using AudioFramework.Core;

namespace AudioFramework.Data
{
    [CreateAssetMenu(fileName = "AudioSourceVolume", menuName = "Scriptable Objects/AudioSourceVolume")]
    public class AudioSourceVolumes : ScriptableObject
    {
        public AudioCategory CurrentAudioType;
        public float Volume;
    }
}
