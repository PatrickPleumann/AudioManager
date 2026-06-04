using UnityEngine;
using UnityEngine.Serialization;

using AudioFramework.Core;
using AudioFramework.Data;
namespace AudioFramework.Configuration
{
    [CreateAssetMenu(fileName = "AudioSystemConfig", menuName = "Audio Tool/System Config")]
    public class AudioSystemConfig : ScriptableObject
    {
        [Header(" --- General values --- ")]
        [Tooltip("--Performance Tooltip-- : The total amount of audio source objects which are instantiated beforehand. " +
                 " More objects = lower performance.")]
        [FormerlySerializedAs("NumbersOfAudioSources")]
        [Range(1, 1000)] public int NumberOfAudioSources = 50;
        [Space]

        [Tooltip("The 'open' (un-occluded) cutoff frequency of the AudioLowPassFilter. This is the value a wall-checked " +
            "sound returns to when no wall is between it and the listener, and the baseline the per-layer reductions are " +
            "subtracted from. Keep it at ~22000 Hz (the top of human hearing) so an un-occluded sound is fully transparent; " +
            "lower values audibly dampen the high end (sounds muffled, as if behind a wall).")]
        [FormerlySerializedAs("defaultCuttoffFreqValue")]
        public float DefaultCutoffFreqValue = 22000f;
        [Space]

        [Tooltip("The minimum cutoff frequency value. The wall check will never reduce the frequency below this value.")]
        public float MinCutoffFreqValue = 100f;
        [Space]

        [Tooltip("How fast the low-pass cutoff glides toward its target when a sound moves in or out of occlusion, " +
            "in Hz per second. Smooths the transition so stepping out from behind a wall does not 'pop'. A higher " +
            "value tracks faster; 0 disables smoothing (the cutoff snaps instantly). Tune by ear — ~50000 transitions " +
            "a typical wall in roughly a third of a second.")]
        public float OcclusionSmoothingSpeed = 50000f;
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
        [Tooltip("This transfer object contains all the audio source volumes which will be handled automatically by the " +
                 " [AudioTool]. Double click on this element to see which audio source volumes are handled at the moment")]
        public AudioVolumesTransferObject TransferObject;
        [Space]

        [Tooltip("This is simply the basic 3D Audio Object which will be pooled and used for this [AudioTool]")]
        public GameObject AudioGameObjectPrefab;
    }
}
