using System.Threading;
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

        [Tooltip("This is the root transform where your AudioObject is spawned. " +
            "Example: Your enemies have Voicelines. The enemy prefab head, would be the root transform you put in here")]
        public Transform CallerTransform;
        [Space]

        [Tooltip("Performance Tooltip:  Check this, if you need the sound to be parented - " +
            "Example: Passing car emitting sounds. Uncheck this mark if the sounds are short")]
        public bool SetCallerAsParent;
        //[Space]

        ////[Tooltip("Performance Tooltip:  Check this box, if the Source could be behind a wall. " +
        ////    "Uncheck this, if you are sure, the sound won´t appear behind wall")]
        ////public bool SourceOriginCouldBeBehindWall;

        public int PoolIndex { get; set; } = -1;
        public bool IsOneShot;
        public bool canHandleAudioSource;
        public CancellationToken cancellationToken;
        public bool UseWallCheck;
    }

}