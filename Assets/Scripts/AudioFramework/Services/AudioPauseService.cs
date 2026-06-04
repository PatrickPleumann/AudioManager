using UnityEngine;

using AudioFramework.Core;

namespace AudioFramework.Pause
{
    public class AudioPauseService
    {
        private readonly AudioObject[] poolArray;

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
                if (poolArray[i].IsPaused)
                {
                    if (poolArray[i].Source != null) poolArray[i].Source.UnPause();
                    poolArray[i].IsPaused = false;
                }
            }
        }
    }
}