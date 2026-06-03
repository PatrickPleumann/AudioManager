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
    }
}