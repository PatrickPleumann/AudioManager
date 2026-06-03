using UnityEngine;

using AudioFramework.Core;

namespace AudioFramework.Pause
{
    public class AudioPauseService
    {
        private readonly AudioObject[] poolArray;

        // True between PauseAll() and UnpauseAll(). Lets newly dispatched sounds that respect the global pause start
        // paused too, so PauseAll() behaves as a sustained pause state and not just a one-time snapshot.
        public bool IsGloballyPaused { get; private set; }

        public AudioPauseService(AudioObject[] _poolArray)
        {
            poolArray = _poolArray;
        }

        public void PauseAll()
        {
            IsGloballyPaused = true;
            if (poolArray == null) return;
            for (int i = 0; i < poolArray.Length; i++)
            {
                // Skip sounds that don't respect the global pause (e.g. UI/music) so they keep playing while paused.
                if (poolArray[i].Source != null && poolArray[i].Source.isPlaying && poolArray[i].RespectsGlobalPause)
                {
                    poolArray[i].Source.Pause();
                    poolArray[i].IsPaused = true;
                }
            }
        }

        public void UnpauseAll()
        {
            IsGloballyPaused = false;
            if (poolArray == null) return;
            for (int i = 0; i < poolArray.Length; i++)
            {
                // Resume only the slots WE paused. A slot can have been reused by a non-pausable sound that started
                // during the pause, so we must not blindly UnPause every slot.
                if (poolArray[i].IsPaused)
                {
                    if (poolArray[i].Source != null) poolArray[i].Source.UnPause();
                    poolArray[i].IsPaused = false;
                }
            }
        }
    }
}