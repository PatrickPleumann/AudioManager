using UnityEngine;

using AudioFramework.Configuration;
using AudioFramework.Core;
using AudioFramework.Interfaces;

namespace AudioFramework.Pooling
{
    public class AudioPoolAcquisitionService  : IGetPoolIndex
    {
        private readonly AudioObject[] poolArray;
        private readonly AudioSystemConfig config;
        private readonly Transform parentTransform;

        public AudioObject[] PoolArray => poolArray;

        public AudioPoolAcquisitionService(AudioSystemConfig _config, Transform _parentTransform)
        {
            config = _config;
            parentTransform = _parentTransform;
            poolArray = new AudioObject[config.NumberOfAudioSources];

            InitializePool();
        }

        private void InitializePool()
        {
            for (int i = 0; i < poolArray.Length; i++)
            {
                var go = Object.Instantiate(config.AudioGameObjectPrefab,parentTransform);
                // Clear, internal-looking name so developers don't mistake a pooled slot (which is moved to a world
                // position for spatial sounds) for one of their own objects and delete it. Index is zero-padded to 3
                // digits; the pool is capped at 1000 (indices 000–999), well above Unity's practical voice limit.
                go.name = $"Pooled Audio Source {i:000}";

                poolArray[i] = new AudioObject
                {
                    GameObject = go,
                    Source = go.GetComponent<AudioSource>(),
                    Filter = go.GetComponent<AudioLowPassFilter>(),
                    BusyUntilTime = 0f
                };
            }
        }

        public int GetFreeAudioSourcePoolIndex()
        {
            float currentTime = Time.time;
            for (int i = 0; i < poolArray.Length; i++)
            {
                // A paused AudioSource reports isPlaying == false, so without the IsPaused guard the acquisition
                // would hand out a paused slot as "free" and overwrite a sound the player paused.
                if (!poolArray[i].Source.isPlaying && currentTime >= poolArray[i].BusyUntilTime && !poolArray[i].IsPaused)
                {
                    // One acquisition = one new generation. Any handle issued for this dispatch carries this value;
                    // handles from the slot's previous occupation are now stale and become no-ops.
                    poolArray[i].Generation++;
                    return i;
                }
            }
            return -1;
        }

        public void SetSlotBusy(int poolIndex, float duration) => poolArray[poolIndex].BusyUntilTime = Time.time + duration;
        public void ResetSlotBusy(int poolIndex) => poolArray[poolIndex].BusyUntilTime = 0f;

        // Mirror the ADO's pause policy onto the slot and clear any stale paused state. Called every dispatch
        // (control-surface): a freshly (re)used slot is never paused.
        public void SetPausePolicy(int poolIndex, bool respectsGlobalPause)
        {
            poolArray[poolIndex].RespectsGlobalPause = respectsGlobalPause;
            poolArray[poolIndex].IsPaused = false;
        }

        // Clear the paused flag when a slot is released by any path other than UnpauseAll (explicit Stop, follow-target
        // destroyed). Otherwise the slot would stay invisible to acquisition and UnpauseAll could revive a dead sound.
        public void ResetPauseState(int poolIndex) => poolArray[poolIndex].IsPaused = false;

        // Mark a freshly dispatched slot as paused because a global PauseAll() is currently in effect. UnpauseAll()
        // will then resume it together with the sounds that were already playing when PauseAll() ran.
        public void MarkSlotPaused(int poolIndex) => poolArray[poolIndex].IsPaused = true;

        // Pass the emitter Transform to make this slot follow it, or null to stop following.
        public void SetFollowTarget(int poolIndex, Transform target)
        {
            poolArray[poolIndex].FollowTarget = target;
            poolArray[poolIndex].IsFollowing = target != null;
        }

        // Seed the occlusion target (control-surface): a freshly (re)used slot starts un-occluded at the open cutoff,
        // so a reused slot never glides from a previous sound's occluded target. The wall-check loop updates it later.
        public void SetTargetCutoff(int poolIndex, float cutoff) => poolArray[poolIndex].TargetCutoff = cutoff;

        // The generation a handle issued for this slot right now must carry. Read when building a handle for a dispatch.
        public int CurrentGeneration(int poolIndex) => poolArray[poolIndex].Generation;

        // True only if the handle still refers to the exact sound currently on its slot: in range AND same generation.
        // Bounds-checked before indexing so a bogus/out-of-range handle can never dereference the pool (P6).
        public bool IsHandleCurrent(AudioHandle handle)
        {
            if (handle.PoolIndex < 0 || handle.PoolIndex >= poolArray.Length) return false;
            return AudioHandleValidator.IsCurrent(handle.PoolIndex, handle.Generation, poolArray[handle.PoolIndex].Generation, poolArray.Length);
        }
    }
}
