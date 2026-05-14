using UnityEngine;

public class AudioPauseService
{
    private readonly PoolAudioObject[] allAudioSources_Array;
    
    public AudioPauseService(PoolAudioObject[] _allAudioSources_Array)
    {
        allAudioSources_Array = _allAudioSources_Array;
    }
    public void PauseAllSources()
    {
        if (allAudioSources_Array == null) return;

        for (int i = 0; i < allAudioSources_Array.Length; i++)
        {
            if (allAudioSources_Array[i].Source.isPlaying)
                allAudioSources_Array[i].Source.Pause();
        }

        Debug.Log($"[AudioTool] All {allAudioSources_Array.Length} AudioSources paused.");
    }

    public void UnpauseAllSources()
    {
        if (allAudioSources_Array == null) return;

        for (int i = 0; i < allAudioSources_Array.Length; i++)
        {
            AudioSource source = allAudioSources_Array[i].Source;

            if (source != null && source.clip != null)
            {
                source.UnPause();
            }
        }

        Debug.Log($"[AudioTool] All {allAudioSources_Array.Length} AudioSources unpaused.");
    }
}
