using UnityEngine;

namespace AudioFramework.Data
{
    [CreateAssetMenu(fileName = "AudioDataObject", menuName = "Scriptable Objects/AudioDataObject")]

    public class AudioDataObject : ScriptableObject
    {
        [Tooltip("All clips which are used for the sound: Only use one AudioDataObject at a time for one specific sound. " +
            "Example: Only use 10 footsteps, or only use Gunshots of the same kind. Don´t mix those up.")]
        public AudioClip[] CurrentClips;
        [Space]

        [Tooltip("You most likely will have different sound volumes like: " +
            "Ambient, SFX, Music etc. This field defines which kind of source volume you wanna use for this AudioDataObject")]
        public AudioTypeProvider CurrentType;
        [Space]

        [Range(0f, 1f)]
        [Tooltip("Spatialization of this sound. 1 = full 3D (positional, attenuated by distance), 0 = full 2D " +
            "(non-positional, same level everywhere). NOTE: This value only takes effect when you play the sound WITH a " +
            "source Transform via PlaySpatial(ado, transform). Calling PlayNonSpatial(ado) forces 2D (0), ignoring this value.")]
        public float SpatialBlend = 1f;
        [Space]

        [Tooltip("Performance Tooltip:  Check this, if you need the sound to be parented - " +
            "Example: Passing car emitting sounds. Uncheck this mark if the sounds are short")]
        public bool SetCallerAsParent;
        //[Space]

        ////[Tooltip("Performance Tooltip:  Check this box, if the Source could be behind a wall. " +
        ////    "Uncheck this, if you are sure, the sound won´t appear behind wall")]
        ////public bool SourceOriginCouldBeBehindWall;

        public bool IsOneShot;
        public bool CanHandleAudioSource;
        public bool UseWallCheck;
    }

}