using UnityEngine;

using AudioFramework.Core;

namespace AudioFramework.Pause
{
    public class AudioPauseService
    {
        private readonly AudioObject[] poolArray;

        public AudioPauseService(AudioObject[] _poolArray)
        {
            poolArray = _poolArray;
        }

        public void PauseAll()
        {
            if (poolArray == null) return;
            for (int i = 0; i < poolArray.Length; i++)
            {
                if (poolArray[i].Source != null && poolArray[i].Source.isPlaying)
                {
                    poolArray[i].Source.Pause();
                }
            }
        }

        public void UnpauseAll()
        {
            if (poolArray == null) return;
            for (int i = 0; i < poolArray.Length; i++)
            {
                AudioSource source = poolArray[i].Source;
                if (source != null && source.clip != null)
                {
                    source.UnPause();
                }
            }
        }
    }
}