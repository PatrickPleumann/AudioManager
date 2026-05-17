using UnityEngine;

public class AudioPauseService_Test
{
    private readonly AudioObject[] _poolArray;

    public AudioPauseService_Test(AudioObject[] poolArray)
    {
        _poolArray = poolArray;
    }

    public void PauseAll()
    {
        if (_poolArray == null) return;
        for (int i = 0; i < _poolArray.Length; i++)
        {
            if (_poolArray[i].Source != null && _poolArray[i].Source.isPlaying)
            {
                _poolArray[i].Source.Pause();
            }
        }
    }

    public void UnpauseAll()
    {
        if (_poolArray == null) return;
        for (int i = 0; i < _poolArray.Length; i++)
        {
            AudioSource source = _poolArray[i].Source;
            if (source != null && source.clip != null)
            {
                source.UnPause();
            }
        }
    }
}