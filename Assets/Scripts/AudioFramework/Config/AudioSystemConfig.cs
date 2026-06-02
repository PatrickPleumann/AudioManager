using UnityEngine;

using AudioFramework.Data;
namespace AudioFramework.Configuration
{
    [CreateAssetMenu(fileName = "AudioSystemConfig", menuName = "Audio Tool Test/System Config")]
    public class AudioSystemConfig : ScriptableObject
    {
        [Header(" --- General values --- ")]
        [Tooltip("--Performance Tooltip-- : The total amount of audio source objects which are instantiated beforehand. " +
                 " More objects = lower performance.")]
        [Range(1, 100)] public int NumbersOfAudioSources = 50;
        [Space]

        [Tooltip("The default Frequency on the AudioLowPassFilter -> This is usually between 5000-5007." +
            " So whenever the [AudioTool] cannot find a specific value  - it uses the default value")]
        public float defaultCuttoffFreqValue = 5000f;
        [Space]

        [Tooltip("The minimum cutoff frequency value. The wall check will never reduce the frequency below this value.")]
        public float MinCutoffFreqValue = 100f;
        [Space]

        [Header(" --- Wallcheck Interval --- ")]
        [Tooltip("--Performance Tooltip-- : The time between two interval checks in seconds -> For human hearing: " +
                 " the sweet spot is between 0.1 to 0.25 -> Higher values means lower performance cost.")]
        [Range(0.01f, 1f)] public float TimeIntervalBetweenPositionChecks = 0.25f;
        [Space]

        [Header(" --- Array of all layer related Cutoff Frequencies --- ")]
        [Tooltip("Create a new element with (+) -> Expand the new Element -> Choose a single Layer -> " +
                 " Add a Cutoff Frequency as value -> Done")]
        public CutoffFreqLayerBehaviour[] CutOffFrequenciesPerLayer;
        [Space]

        [Header(" --- References ---")]
        [Tooltip("This transfer object contains all the audio source volumes which will be handled automaticly by the " +
                 " [AudioTool]. Double click on this element to see which audio source volumes are handled at the moment")]
        public AudioVolumesTransferObject TransferObject;
        [Space]

        [Tooltip("This is simply the basic 3D Audio Object which will be pooled and used for this [AudioTool]")]
        public GameObject AudioGameObjectPrefab;
    }
}
