using UnityEngine;

namespace AudioFramework.Core
{
    public struct AudioObject
    {
        public GameObject GameObject;
        public AudioSource Source;
        public AudioLowPassFilter Filter;
        public Transform FollowTarget;
        public float BusyUntilTime;
        public float TargetCutoff;
        public int Generation;
        public bool IsFollowing;
        public bool RespectsGlobalPause;
        public bool IsPaused;
    }
}