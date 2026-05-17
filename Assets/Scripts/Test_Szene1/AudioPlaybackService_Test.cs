using UnityEngine;

public class AudioPlaybackService_Test
{
    private readonly AudioPoolAcquisitionService_Test poolAcquisitionService;
    private readonly AudioManagerDictionaryProvider dictionaryProvider;
    private readonly IAudioWallCheckService_Test wallCheckService;
    private readonly float _defaultCutoffValue;

    public AudioPlaybackService_Test(
        AudioPoolAcquisitionService_Test _poolAcquisitionService,
        AudioManagerDictionaryProvider _dictionaryProvider,
        IAudioWallCheckService_Test wallCheckService,
        float _defaultCutoffValue)
    {
        this.poolAcquisitionService = _poolAcquisitionService;
        this.dictionaryProvider = _dictionaryProvider;
        this.wallCheckService = wallCheckService;
        this._defaultCutoffValue = _defaultCutoffValue;
    }

    public AudioHandle_Test DispatchAudio(AudioDataObject audioDataObject)
    {
        int poolIndex = poolAcquisitionService.GetFreePoolIndex();
        if (poolIndex == -1) return new AudioHandle_Test(-1);

        AudioObject poolObject = poolAcquisitionService.PoolArray[poolIndex];
        AudioSource source = poolObject.Source;
        AudioLowPassFilter filter = poolObject.Filter;

        AudioClip chosenClip = audioDataObject.CurrentClips[Random.Range(0, audioDataObject.CurrentClips.Length)];
        source.clip = chosenClip;

        if (dictionaryProvider.volumeDictionary.TryGetValue(audioDataObject.CurrentType, out float curVolume))
            source.volume = curVolume;

        if (audioDataObject.SetCallerAsParent)
            poolObject.GameObject.transform.SetParent(audioDataObject.CallerTransform);
        else
            poolObject.GameObject.transform.position = audioDataObject.CallerTransform.position;

        filter.cutoffFrequency = _defaultCutoffValue;

        if (audioDataObject.IsOneShot)
        {
            poolAcquisitionService.SetSlotBusy(poolIndex, chosenClip.length);
            source.PlayOneShot(chosenClip);

            if (audioDataObject.UseWallCheck)
                wallCheckService.StartWallCheckLoop(audioDataObject, poolIndex, chosenClip.length);

            return new AudioHandle_Test(-1);
        }
        else
        {
            poolAcquisitionService.ResetSlotBusy(poolIndex);

            if (audioDataObject.UseWallCheck)
                wallCheckService.StartWallCheckLoop(audioDataObject, poolIndex, chosenClip.length);

            source.Play();
            return new AudioHandle_Test(poolIndex);
        }
    }

    public void StopAudio(AudioHandle_Test handle)
    {
        int targetIndex = handle.PoolIndex;
        if (poolAcquisitionService.PoolArray[targetIndex].Source != null)
            poolAcquisitionService.PoolArray[targetIndex].Source.Stop();

        poolAcquisitionService.ResetSlotBusy(targetIndex);
        wallCheckService.StopActiveCheck(targetIndex);
    }
}
