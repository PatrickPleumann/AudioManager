using UnityEngine;

namespace AudioFramework.Core
{
    public struct AudioObject
    {
        public GameObject GameObject;
        public AudioSource Source;
        public AudioLowPassFilter Filter;
        public float BusyUntilTime;

        // Set when the current sound should follow a moving emitter. We track the Transform and copy its position
        // each frame (see AudioFollowService) instead of SetParent-ing the slot — parenting would let caller code
        // own and destroy the pooled GameObject. IsFollowing distinguishes "no follow" from "target destroyed",
        // since a destroyed FollowTarget compares == null just like a never-set one.
        public Transform FollowTarget;
        public bool IsFollowing;

        // Pause handling. RespectsGlobalPause is the policy mirrored from the ADO each dispatch (false = ignores the
        // global PauseAll, e.g. UI/music). IsPaused is the runtime state: true only between PauseAll and UnpauseAll for
        // slots we paused. A paused AudioSource reports isPlaying == false, so the pool acquisition must treat IsPaused
        // slots as occupied (not free), and UnpauseAll must resume only the slots it actually paused.
        public bool RespectsGlobalPause;
        public bool IsPaused;
    }
}